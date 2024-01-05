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

using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represents a SASL step.
    /// </summary>
    internal interface ISaslStep
    {
        // properties
        /// <summary>
        /// Gets the bytes to send to server.
        /// </summary>
        /// <value>
        /// The bytes to send to server.
        /// </value>
        byte[] BytesToSendToServer { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is complete.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
        /// </value>
        bool IsComplete { get; }

        // methods
        /// <summary>
        /// Transitions the SASL conversation to the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <returns>The next SASL step.</returns>
        ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer);

        /// <summary>
        /// Transitions the SASL conversation to the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next SASL step.</returns>
        Task<ISaslStep> TransitionAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken = default);
    }
}

