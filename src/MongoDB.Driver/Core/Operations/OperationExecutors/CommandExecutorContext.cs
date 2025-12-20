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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations.OperationExecutors
{
    internal sealed class CommandExecutorContext : IDisposable
    {
        public CommandExecutorContext(IChannelHandle channel, IChannelSourceHandle channelSource, MessageEncoderSettings messageEncoderSettings)
        {
            Channel = channel;
            ChannelSource = channelSource;
            MessageEncoderSettings = messageEncoderSettings;
        }

        public void Dispose()
        {
            Channel.Dispose();
            ChannelSource.Dispose();
        }

        public IChannelHandle Channel { get; }
        // TODO: Probably could be replaced with IServer
        public IChannelSourceHandle ChannelSource { get; }
        public MessageEncoderSettings MessageEncoderSettings { get; }
        public ICoreSession Session => ChannelSource.Session;
        public ConnectionDescription ConnectionDescription => Channel.ConnectionDescription;
    }
}
