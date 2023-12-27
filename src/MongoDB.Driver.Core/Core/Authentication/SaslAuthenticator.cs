/* Copyright 2015-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Base class for a SASL authenticator.
    /// </summary>
    public abstract class SaslAuthenticator : IAuthenticator
    {
        // fields
        private protected readonly ISaslMechanism _mechanism;
        private protected readonly ServerApi _serverApi;
        private protected ISaslStep _speculativeFirstStep;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticator"/> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        [Obsolete("Use the newest overload instead.")]
        private protected SaslAuthenticator(ISaslMechanism mechanism)
            : this(mechanism, serverApi: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticator"/> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        /// <param name="serverApi">The server API.</param>
        private protected SaslAuthenticator(ISaslMechanism mechanism, ServerApi serverApi)
        {
            _mechanism = Ensure.IsNotNull(mechanism, nameof(mechanism));
            _serverApi = serverApi; // can be null
        }

        // properties
        /// <inheritdoc/>
        public string Name
        {
            get { return _mechanism.Name; }
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        public abstract string DatabaseName { get; }

        // methods
        /// <inheritdoc/>
        public virtual void Authenticate(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            using (var conversation = new SaslConversation(description.ConnectionId))
            {
                ISaslStep currentStep;
                BsonDocument command;
                var speculativeAuthenticateResult = description.HelloResult.SpeculativeAuthenticate;
                if (_speculativeFirstStep != null && speculativeAuthenticateResult != null)
                {
                    currentStep = Transition(conversation, _speculativeFirstStep, speculativeAuthenticateResult, out command);
                }
                else
                {
                    currentStep = _mechanism.Initialize(connection, conversation, description);
                    command = CreateStartCommand(currentStep);
                }

                while (currentStep != null)
                {
                    BsonDocument result;
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = protocol.Execute(connection, cancellationToken);
                    }
                    catch (MongoException ex)
                    {
                        throw CreateException(connection, ex);
                    }

                    currentStep = Transition(conversation, currentStep, result, out command);
                }
            }
        }

        /// <inheritdoc/>
        public virtual async Task AuthenticateAsync(IConnection connection, ConnectionDescription description, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(description, nameof(description));

            using (var conversation = new SaslConversation(description.ConnectionId))
            {
                ISaslStep currentStep;
                BsonDocument command;
                var speculativeAuthenticateResult = description.HelloResult.SpeculativeAuthenticate;
                if (_speculativeFirstStep != null && speculativeAuthenticateResult != null)
                {
                    currentStep = Transition(conversation, _speculativeFirstStep, speculativeAuthenticateResult, out command);
                }
                else
                {
                    currentStep = _mechanism.Initialize(connection, conversation, description);
                    command = CreateStartCommand(currentStep);
                }

                while (currentStep != null)
                {
                    BsonDocument result;
                    try
                    {
                        var protocol = CreateCommandProtocol(command);
                        result = await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
                    }
                    catch (MongoException ex)
                    {
                        throw CreateException(connection, ex);
                    }

                    currentStep = Transition(conversation, currentStep, result, out command);
                }
            }
        }

        /// <inheritdoc/>
        public virtual BsonDocument CustomizeInitialHelloCommand(BsonDocument helloCommand)
        {
            return helloCommand;
        }

        private protected virtual BsonDocument CreateStartCommand(ISaslStep currentStep)
        {
            var startCommand = new BsonDocument
            {
                { "saslStart", 1 },
                { "mechanism", _mechanism.Name },
                { "payload", currentStep.BytesToSendToServer }
            };

            return startCommand;
        }

        private CommandWireProtocol<BsonDocument> CreateCommandProtocol(BsonDocument command)
        {
            return new CommandWireProtocol<BsonDocument>(
                databaseNamespace: new DatabaseNamespace(DatabaseName),
                command: command,
                secondaryOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: _serverApi);
        }

        private BsonDocument CreateContinueCommand(ISaslStep currentStep, BsonDocument result)
        {
            return new BsonDocument
            {
                { "saslContinue", 1 },
                { "conversationId", result["conversationId"].AsInt32 },
                { "payload", currentStep.BytesToSendToServer }
            };
        }

        private MongoAuthenticationException CreateException(IConnection connection, Exception ex)
        {
            var message = string.Format("Unable to authenticate using sasl protocol mechanism {0}.", Name);
            return new MongoAuthenticationException(connection.ConnectionId, message, ex);
        }

        private ISaslStep Transition(
            SaslConversation conversation,
            ISaslStep currentStep,
            BsonDocument result,
            out BsonDocument command)
        {
            // we might be done here if the client is not expecting a reply from the server
            if (result.GetValue("done", false).ToBoolean() && currentStep.IsComplete)
            {
                command = null;
                return null;
            }

            currentStep = currentStep.Transition(conversation, result["payload"].AsByteArray);

            // we might be done here if the client had some final verification it needed to do
            if (result.GetValue("done", false).ToBoolean() && currentStep.IsComplete)
            {
                command = null;
                return null;
            }

            command = CreateContinueCommand(currentStep, result);
            return currentStep;
        }
    }
}
