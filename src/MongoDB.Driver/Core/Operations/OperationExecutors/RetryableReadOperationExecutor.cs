/* Copyright 2010-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations.OperationExecutors
{
    internal class RetryableReadOperationExecutor : IReadOperationExecutor
    {
        public RetryableReadOperationExecutor(MessageEncoderSettings messageEncoderSettings)
        {
            MessageEncoderSettings = messageEncoderSettings;
        }

        private MessageEncoderSettings MessageEncoderSettings { get; }

        public TResult Execute<TResult, TServerResponse>(
            OperationContext operationContext,
            IClientSessionHandle session,
            IReadBindingHandle binding,
            IReadOperation<TResult, TServerResponse> operation)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            Exception lastException = null;

            do
            {
                operationContext.ThrowIfTimedOutOrCanceled();

                attempt++;
                using var operationExecutorContext = CreateOperationExecutorContext(operationContext, binding, deprioritizedServers);
                var server = operationExecutorContext.ChannelSource.ServerDescription;
                var command = operation.CreateCommand(operationExecutorContext);

                try
                {
                    var response = operationExecutorContext.Channel.Command(
                        operationContext,
                        session.WrappedCoreSession,
                        binding.ReadPreference,
                        operation.DatabaseNamespace,
                        command,
                        null, // commandPayloads
                        NoOpElementNameValidator.Instance, // commandValidator - it seems to be not supported by CommandMessageProtocol, should we remove this at all?
                        null, // additionalOptions - it seems to be not supported by CommandMessageProtocol, should we remove this at all?
                        null, // postWriteAction,
                        CommandResponseHandling.Return,
                        operation.ResultSerializer,
                        MessageEncoderSettings);

                    return operation.HandleResult(operationExecutorContext, response);
                }
                catch (Exception ex) when (operation.TryHandleException(operationExecutorContext, ex, out var result))
                {
                    return result;
                }
                catch (Exception ex)
                {
                    deprioritizedServers ??= new HashSet<ServerDescription>();
                    deprioritizedServers.Add(server);
                    originalException ??= ex;
                    lastException = ex;
                }

            } while (RetryableReadHelper.ShouldRetryOperation(operationContext, lastException, attempt));

            throw originalException;
        }

        public async Task<TResult> ExecuteAsync<TResult, TServerResponse>(
            OperationContext operationContext,
            IClientSessionHandle session,
            IReadBindingHandle binding,
            IReadOperation<TResult, TServerResponse> operation)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            Exception lastException = null;

            do
            {
                operationContext.ThrowIfTimedOutOrCanceled();

                attempt++;
                using var operationExecutorContext = await CreateOperationExecutorContextAsync(operationContext, binding, deprioritizedServers).ConfigureAwait(false);
                var server = operationExecutorContext.ChannelSource.ServerDescription;
                var command = operation.CreateCommand(operationExecutorContext);

                try
                {
                    var response = await operationExecutorContext.Channel.CommandAsync(
                        operationContext,
                        session.WrappedCoreSession,
                        binding.ReadPreference,
                        operation.DatabaseNamespace,
                        command,
                        null, // commandPayloads
                        NoOpElementNameValidator.Instance, // commandValidator - it seems to be not supported by CommandMessageProtocol, should we remove this at all?
                        null, // additionalOptions - it seems to be not supported by CommandMessageProtocol, should we remove this at all?
                        null, // postWriteAction,
                        CommandResponseHandling.Return,
                        operation.ResultSerializer,
                        MessageEncoderSettings).ConfigureAwait(false);

                    return operation.HandleResult(operationExecutorContext, response);
                }
                catch (Exception ex) when (operation.TryHandleException(operationExecutorContext, ex, out var result))
                {
                    return result;
                }
                catch (Exception ex)
                {
                    deprioritizedServers ??= new HashSet<ServerDescription>();
                    deprioritizedServers.Add(server);
                    originalException ??= ex;
                    lastException = ex;
                }

            } while (RetryableReadHelper.ShouldRetryOperation(operationContext, lastException, attempt));

            throw originalException;
        }

        private CommandExecutorContext CreateOperationExecutorContext(OperationContext operationContext, IReadBinding binding, HashSet<ServerDescription> deprioritizedServers)
        {
            IChannelSourceHandle channelSource = null;
            IChannelHandle channel = null;

            var attempt = 1;
            while (true)
            {

                try
                {
                    operationContext.ThrowIfTimedOutOrCanceled();
                    channelSource = binding.GetReadChannelSource(operationContext, deprioritizedServers);
                    channel = channelSource.GetChannel(operationContext);
                }
                catch (Exception ex)
                {
                    channelSource?.Dispose();
                    channel?.Dispose();

                    if (RetryableReadHelper.ShouldConnectionAcquireBeRetried(operationContext, ex, attempt))
                    {
                        attempt++;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private async Task<CommandExecutorContext> CreateOperationExecutorContextAsync(OperationContext operationContext, IReadBinding binding, HashSet<ServerDescription> deprioritizedServers)
        {
            IChannelSourceHandle channelSource = null;
            IChannelHandle channel = null;

            var attempt = 1;
            while (true)
            {

                try
                {
                    operationContext.ThrowIfTimedOutOrCanceled();
                    channelSource = await binding.GetReadChannelSourceAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                    channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    channelSource?.Dispose();
                    channel?.Dispose();

                    if (RetryableReadHelper.ShouldConnectionAcquireBeRetried(operationContext, ex, attempt))
                    {
                        attempt++;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static class RetryableReadHelper
        {
            public static bool ShouldConnectionAcquireBeRetried(OperationContext operationContext, Exception exception, int attempt)
            {
                var innerException = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;
                return ShouldRetryOperation(operationContext, innerException, attempt);
            }

            public static bool ShouldRetryOperation(OperationContext operationContext, Exception exception, int attempt)
            {
                // Move this checks to the OperationExecutor factory: if retries was not requested or IsInTransaction - create non-retriable executor
                // if (!context.RetryRequested || context.Binding.Session.IsInTransaction)
                // {
                //     return false;
                // }

                if (!RetryabilityHelper.IsRetryableReadException(exception))
                {
                    return false;
                }

                return operationContext.IsRootContextTimeoutConfigured() || attempt < 2;
            }
        }
    }
}

