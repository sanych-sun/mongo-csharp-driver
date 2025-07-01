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

        public TResult Execute<TResult, TServerResponse>(OperationContext operationContext,
            IClientSessionHandle session,
            IReadBindingHandle binding,
            IReadOperation<TResult, TServerResponse> operation)
        {
            using var operationExecutorContext = new ReadOperationExecutorContext(binding, MessageEncoderSettings);
            operationExecutorContext.AcquireChannel(operationContext);

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
        }

        public async Task<TResult> ExecuteAsync<TResult, TServerResponse>(
            OperationContext operationContext,
            IClientSessionHandle session,
            IReadBindingHandle binding,
            IReadOperation<TResult, TServerResponse> operation)
        {
            using var operationExecutorContext = new ReadOperationExecutorContext(binding, MessageEncoderSettings);
            await operationExecutorContext.AcquireChannelAsync(operationContext).ConfigureAwait(false);

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
        }

        private sealed class ReadOperationExecutorContext : IOperationExecutorContext, IDisposable
        {
            public ReadOperationExecutorContext(IReadBindingHandle binding, MessageEncoderSettings messageEncoderSettings)
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

            public void AcquireChannel(OperationContext operationContext)
            {
                // TODO: apply server selection timeout here
                ChannelSource = Binding.GetReadChannelSource(operationContext);
                Channel = ChannelSource.GetChannel(operationContext);
            }

            public async Task AcquireChannelAsync(OperationContext operationContext)
            {
                // TODO: apply server selection timeout here
                ChannelSource = await Binding.GetReadChannelSourceAsync(operationContext).ConfigureAwait(false);
                Channel = await ChannelSource.GetChannelAsync(operationContext).ConfigureAwait(false);
            }
        }


    }
}

