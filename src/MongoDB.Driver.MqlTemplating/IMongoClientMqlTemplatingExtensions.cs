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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.MqlTemplating
{
    /// <summary>
    /// MQL-templating extension methods for <see cref="IMongoClient"/>.
    /// </summary>
    public static class IMongoClientMqlTemplatingExtensions
    {
        /// <summary>
        /// Watches changes on all collections in all databases using a pipeline built from a template.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> Watch(
            this IMongoClient client,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            return client.Watch(ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collections in all databases using a pipeline built from a template.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> Watch(
            this IMongoClient client,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return client.Watch(session, ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collections in all databases using a pipeline built from a template.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> WatchAsync(
            this IMongoClient client,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            return client.WatchAsync(ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collections in all databases using a pipeline built from a template.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> WatchAsync(
            this IMongoClient client,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return client.WatchAsync(session, ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        private static PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> ParsePipeline(
            string template,
            object parameters) =>
            MqlTemplateParser.ParsePipeline<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>(template, parameters);
    }
}
