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

        public TResult Execute<TResult, TServerResponse>(OperationContext operationContext,
            IClientSessionHandle session,
            IReadBindingHandle binding,
            IReadOperation<TResult, TServerResponse> operation)
        {
            using var operationExecutorContext = new RetryableReadOperationExecutorContext(binding, MessageEncoderSettings);
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            Exception lastException = null;

            do
            {
                operationContext.ThrowIfTimedOutOrCanceled();

                attempt++;
                operationExecutorContext.AcquireOrReplaceChannel(operationContext, deprioritizedServers);
                var server = operationExecutorContext.ChannelSource.ServerDescription;
                var command = operation.CreateCommand(operationExecutorContext);

                try
                {
                    var response = operationExecutorContext.Channel.Command(
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
                        MessageEncoderSettings,
                        operationContext.CancellationToken);

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
            using var operationExecutorContext = new RetryableReadOperationExecutorContext(binding, MessageEncoderSettings);
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            Exception lastException = null;

            do
            {
                operationContext.ThrowIfTimedOutOrCanceled();

                attempt++;
                await operationExecutorContext.AcquireOrReplaceChannelAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                var server = operationExecutorContext.ChannelSource.ServerDescription;
                var command = operation.CreateCommand(operationExecutorContext);

                try
                {
                    var response = await operationExecutorContext.Channel.CommandAsync(
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
                        MessageEncoderSettings,
                        operationContext.CancellationToken).ConfigureAwait(false);

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

        private sealed class RetryableReadOperationExecutorContext : IOperationExecutorContext, IDisposable
        {
            public RetryableReadOperationExecutorContext(IReadBindingHandle binding, MessageEncoderSettings messageEncoderSettings)
            {
                Binding = binding;
                MessageEncoderSettings = messageEncoderSettings;
            }

            public IReadBindingHandle Binding { get; }
            public IChannelHandle Channel { get; private set; }
            public IChannelSourceHandle ChannelSource { get; private set; }
            public MessageEncoderSettings MessageEncoderSettings { get; }

            public void Dispose()
            {
                ChannelSource?.Dispose();
                Channel?.Dispose();
            }

            public void AcquireOrReplaceChannel(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
            {
                // TODO: apply server selection timeout here
                var attempt = 1;
                while (true)
                {
                    operationContext.ThrowIfTimedOutOrCanceled();
                    ReplaceChannelSource(Binding.GetReadChannelSource(operationContext, deprioritizedServers));
                    try
                    {
                        ReplaceChannel(ChannelSource.GetChannel(operationContext));
                        return;
                    }
                    catch (Exception ex) when (RetryableReadHelper.ShouldConnectionAcquireBeRetried(operationContext, ex, attempt))
                    {
                        attempt++;
                    }
                }
            }

            public async Task AcquireOrReplaceChannelAsync(OperationContext operationContext, IReadOnlyCollection<ServerDescription> deprioritizedServers)
            {
                // TODO: apply server selection timeout here
                var attempt = 1;
                while (true)
                {
                    operationContext.ThrowIfTimedOutOrCanceled();
                    ReplaceChannelSource(await Binding.GetReadChannelSourceAsync(operationContext, deprioritizedServers).ConfigureAwait(false));
                    try
                    {
                        ReplaceChannel(await ChannelSource.GetChannelAsync(operationContext).ConfigureAwait(false));
                        return;
                    }
                    catch (Exception ex) when (RetryableReadHelper.ShouldConnectionAcquireBeRetried(operationContext, ex, attempt))
                    {
                        attempt++;
                    }
                }
            }

            private void ReplaceChannel(IChannelHandle channel)
            {
                Channel?.Dispose();
                Channel = channel;
            }

            private void ReplaceChannelSource(IChannelSourceHandle channelSource)
            {
                ChannelSource?.Dispose();
                Channel?.Dispose();
                ChannelSource = channelSource;
                Channel = null;
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

