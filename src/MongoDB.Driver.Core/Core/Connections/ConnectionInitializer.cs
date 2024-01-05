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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection initializer (opens and authenticates connections).
    /// </summary>
    internal class ConnectionInitializer : IConnectionInitializer
    {
        private readonly BsonDocument _clientDocument;
        private readonly IReadOnlyList<CompressorConfiguration> _compressors;
        private readonly ServerApi _serverApi;

        public ConnectionInitializer(
            string applicationName,
            IReadOnlyList<CompressorConfiguration> compressors,
            ServerApi serverApi,
            LibraryInfo libraryInfo)
        {
            _clientDocument = ClientDocumentHelper.CreateClientDocument(applicationName, libraryInfo);
            _compressors = Ensure.IsNotNull(compressors, nameof(compressors));
            _serverApi = serverApi;
        }

        public ConnectionInitializerContext Authenticate(IConnection connection, ConnectionInitializerContext connectionInitializerContext, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(connectionInitializerContext, nameof(connectionInitializerContext));
            var authenticators = Ensure.IsNotNull(connectionInitializerContext.Authenticators, nameof(connectionInitializerContext.Authenticators));
            var description = Ensure.IsNotNull(connectionInitializerContext.Description, nameof(connectionInitializerContext.Description));

            AuthenticationHelper.Authenticate(connection, description, authenticators, cancellationToken);

            if (!description.ConnectionId.LongServerValue.HasValue)
            {
                var connectionIdServerValue = description.HelloResult.ConnectionIdServerValue;
                if (connectionIdServerValue.HasValue)
                {
                    description = UpdateConnectionIdWithServerValue(description, connectionIdServerValue.Value);
                }
                else if (!description.HelloResult.IsMongocryptd) // mongocryptd doesn't provide ConnectionId
                {
                    try
                    {
                        var getLastErrorProtocol = CreateGetLastErrorProtocol(_serverApi);
                        var getLastErrorResult = getLastErrorProtocol.Execute(connection, cancellationToken);

                        description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
                    }
                    catch
                    {
                        // if we couldn't get the server's connection id, so be it.
                    }
                }
            }

            return new ConnectionInitializerContext(description, authenticators);
        }

        public async Task<ConnectionInitializerContext> AuthenticateAsync(IConnection connection, ConnectionInitializerContext connectionInitializerContext, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            Ensure.IsNotNull(connectionInitializerContext, nameof(connectionInitializerContext));
            var authenticators = Ensure.IsNotNull(connectionInitializerContext.Authenticators, nameof(connectionInitializerContext.Authenticators));
            var description = Ensure.IsNotNull(connectionInitializerContext.Description, nameof(connectionInitializerContext.Description));

            await AuthenticationHelper.AuthenticateAsync(connection, description, authenticators, cancellationToken).ConfigureAwait(false);

            if (!description.ConnectionId.LongServerValue.HasValue)
            {
                var connectionIdServerValue = description.HelloResult.ConnectionIdServerValue;
                if (connectionIdServerValue.HasValue)
                {
                    description = UpdateConnectionIdWithServerValue(description, connectionIdServerValue.Value);
                }
                else if (!description.HelloResult.IsMongocryptd) // mongocryptd doesn't provide ConnectionId
                {
                    try
                    {
                        var getLastErrorProtocol = CreateGetLastErrorProtocol(_serverApi);
                        var getLastErrorResult = await getLastErrorProtocol
                            .ExecuteAsync(connection, cancellationToken)
                            .ConfigureAwait(false);

                        description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
                    }
                    catch
                    {
                        // if we couldn't get the server's connection id, so be it.
                    }
                }
            }

            return new ConnectionInitializerContext(description, authenticators);
        }

        public ConnectionInitializerContext SendHello(IConnection connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            var authenticators = GetAuthenticators(connection);
            var helloCommand = CreateInitialHelloCommand(authenticators, connection.Settings.LoadBalanced);
            var helloProtocol = HelloHelper.CreateProtocol(helloCommand, _serverApi);
            var helloResult = HelloHelper.GetResult(connection, helloProtocol, cancellationToken);
            if (connection.Settings.LoadBalanced && !helloResult.ServiceId.HasValue)
            {
                throw new InvalidOperationException("Driver attempted to initialize in load balancing mode, but the server does not support this mode.");
            }

            return new (new ConnectionDescription(connection.ConnectionId, helloResult), authenticators);
        }

        public async Task<ConnectionInitializerContext> SendHelloAsync(IConnection connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));
            var authenticators = GetAuthenticators(connection);
            var helloCommand = CreateInitialHelloCommand(authenticators, connection.Settings.LoadBalanced);
            var helloProtocol = HelloHelper.CreateProtocol(helloCommand, _serverApi);
            var helloResult = await HelloHelper.GetResultAsync(connection, helloProtocol, cancellationToken).ConfigureAwait(false);
            if (connection.Settings.LoadBalanced && !helloResult.ServiceId.HasValue)
            {
                throw new InvalidOperationException("Driver attempted to initialize in load balancing mode, but the server does not support this mode.");
            }

            return new (new ConnectionDescription(connection.ConnectionId, helloResult), authenticators);
        }

        // private methods
        private CommandWireProtocol<BsonDocument> CreateGetLastErrorProtocol(ServerApi serverApi)
        {
            var getLastErrorCommand = new BsonDocument("getLastError", 1);
            var getLastErrorProtocol = new CommandWireProtocol<BsonDocument>(
                databaseNamespace: DatabaseNamespace.Admin,
                command: getLastErrorCommand,
                secondaryOk: true,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi: serverApi);
            return getLastErrorProtocol;
        }

        private BsonDocument CreateInitialHelloCommand(IReadOnlyList<IAuthenticator> authenticators, bool loadBalanced = false)
        {
            var command = HelloHelper.CreateCommand(_serverApi, loadBalanced: loadBalanced);
            HelloHelper.AddClientDocumentToCommand(command, _clientDocument);
            HelloHelper.AddCompressorsToCommand(command, _compressors);
            return HelloHelper.CustomizeCommand(command, authenticators);
        }

        private List<IAuthenticator> GetAuthenticators(IConnection connection, IEnumerable<IAuthenticator> previousAuthenticators = null)
        {
            var endPoint = connection.EndPoint;
            var authenticatorFactories = connection.Settings.AuthenticatorFactories;
            if (!connection.IsInitialized)
            {
                // use regular workflow to support speculative authentication
                return (previousAuthenticators ?? authenticatorFactories.Select(c => c.Create(new AuthenticationContext(endPoint)))).ToList();
            }

            // reauthentication case
            // TODO: use a single authentication instance for connection all the time?
            Ensure.That(previousAuthenticators == null || authenticatorFactories.Count == previousAuthenticators.Count(), $"The {nameof(previousAuthenticators)} count must match to {authenticatorFactories.Count}."); // should not be reached

            var result = new List<IAuthenticator>();
            for (int i = 0; i < authenticatorFactories.Count; i++)
            {
                var previousAuthenticatorContext = (previousAuthenticators?.ElementAt(i) as IWithAuthenticationContext)?.AuthenticationContext;
                var factory = authenticatorFactories[i];
                var newAuthenticator = factory.Create(previousAuthenticatorContext ?? new AuthenticationContext(endPoint));
                result.Add(newAuthenticator);
            }

            return result;

        }

        private ConnectionDescription UpdateConnectionIdWithServerValue(ConnectionDescription description, BsonDocument getLastErrorResult)
        {
            if (getLastErrorResult.TryGetValue("connectionId", out var connectionIdBsonValue))
            {
                description = UpdateConnectionIdWithServerValue(description, connectionIdBsonValue.ToInt32());
            }

            return description;
        }

        private ConnectionDescription UpdateConnectionIdWithServerValue(ConnectionDescription description, long serverValue)
        {
            var connectionId = description.ConnectionId.WithServerValue(serverValue);
            description = description.WithConnectionId(connectionId);

            return description;
        }
    }
}
