/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    /// <summary>
    /// Represents a base class for messages.
    /// </summary>
    [Obsolete("This class will be made internal in a later release.")]
    public abstract class MongoDBMessage : IEncodableMessage
    {
        // properties
        /// <summary>
        /// Gets the flag whether the message may be compressed or not.
        /// </summary>
        public virtual bool MayBeCompressed => false;

        /// <summary>
        /// Gets the type of the message.
        /// </summary>
        public abstract MongoDBMessageType MessageType { get; }

        // methods        
        /// <inheritdoc/>
        public abstract IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory);
    }
}
