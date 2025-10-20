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
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations.OperationExecutors
{
    internal class ReadOperationExecutor : IReadOperationExecutor
    {
        public ReadOperationExecutor(MessageEncoderSettings messageEncoderSettings)
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
            using var operationExecutorContext = CreateOperationExecutorContext(operationContext, binding);

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
        }

        public async Task<TResult> ExecuteAsync<TResult, TServerResponse>(
            OperationContext operationContext,
            IClientSessionHandle session,
            IReadBindingHandle binding,
            IReadOperation<TResult, TServerResponse> operation)
        {
            using var operationExecutorContext = await CreateOperationExecutorContextAsync(operationContext, binding).ConfigureAwait(false);

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
        }

        private CommandExecutorContext CreateOperationExecutorContext(OperationContext operationContext, IReadBinding binding)
        {
            IChannelSourceHandle channelSource = null;
            IChannelHandle channel = null;

            try
            {
                channelSource = binding.GetReadChannelSource(operationContext);
                channel = channelSource.GetChannel(operationContext);
                return new CommandExecutorContext(channel, channelSource, MessageEncoderSettings);
            }
            catch
            {
                channelSource?.Dispose();
                channel?.Dispose();
                throw;
            }
        }

        private async Task<CommandExecutorContext> CreateOperationExecutorContextAsync(OperationContext operationContext, IReadBinding binding)
        {
            IChannelSourceHandle channelSource = null;
            IChannelHandle channel = null;

            try
            {
                channelSource = await binding.GetReadChannelSourceAsync(operationContext).ConfigureAwait(false);
                channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false);
                return new CommandExecutorContext(channel, channelSource, MessageEncoderSettings);
            }
            catch
            {
                channelSource?.Dispose();
                channel?.Dispose();
                throw;
            }
        }
    }
}

