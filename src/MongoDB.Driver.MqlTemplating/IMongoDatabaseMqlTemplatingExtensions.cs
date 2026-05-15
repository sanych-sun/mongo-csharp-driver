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
    /// MQL-templating extension methods for <see cref="IMongoDatabase"/>.
    /// </summary>
    public static class IMongoDatabaseMqlTemplatingExtensions
    {
        // Aggregate

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<BsonDocument> Aggregate(
            this IMongoDatabase database,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.Aggregate(ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<BsonDocument> Aggregate(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.Aggregate(session, ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<BsonDocument>> AggregateAsync(
            this IMongoDatabase database,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.AggregateAsync(ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<BsonDocument>> AggregateAsync(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.AggregateAsync(session, ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        // AggregateToCollection

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AggregateToCollection(
            this IMongoDatabase database,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            database.AggregateToCollection(ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AggregateToCollection(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            database.AggregateToCollection(session, ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static Task AggregateToCollectionAsync(
            this IMongoDatabase database,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.AggregateToCollectionAsync(ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static Task AggregateToCollectionAsync(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.AggregateToCollectionAsync(session, ParsePipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        // CreateView

        /// <summary>
        /// Creates a view whose defining pipeline is built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="viewOn">The name of the collection that the view is on.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void CreateView(
            this IMongoDatabase database,
            string viewName,
            string viewOn,
            string pipelineTemplate,
            object parameters,
            CreateViewOptions<BsonDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            database.CreateView<BsonDocument, BsonDocument>(
                viewName, viewOn,
                MqlTemplateParser.ParsePipeline<BsonDocument, BsonDocument>(pipelineTemplate, parameters),
                options, cancellationToken);
        }

        /// <summary>
        /// Creates a view whose defining pipeline is built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="viewOn">The name of the collection that the view is on.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void CreateView(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string viewName,
            string viewOn,
            string pipelineTemplate,
            object parameters,
            CreateViewOptions<BsonDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            database.CreateView<BsonDocument, BsonDocument>(
                session, viewName, viewOn,
                MqlTemplateParser.ParsePipeline<BsonDocument, BsonDocument>(pipelineTemplate, parameters),
                options, cancellationToken);
        }

        /// <summary>
        /// Creates a view whose defining pipeline is built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="viewOn">The name of the collection that the view is on.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        public static Task CreateViewAsync(
            this IMongoDatabase database,
            string viewName,
            string viewOn,
            string pipelineTemplate,
            object parameters,
            CreateViewOptions<BsonDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.CreateViewAsync<BsonDocument, BsonDocument>(
                viewName, viewOn,
                MqlTemplateParser.ParsePipeline<BsonDocument, BsonDocument>(pipelineTemplate, parameters),
                options, cancellationToken);
        }

        /// <summary>
        /// Creates a view whose defining pipeline is built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="viewOn">The name of the collection that the view is on.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        public static Task CreateViewAsync(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string viewName,
            string viewOn,
            string pipelineTemplate,
            object parameters,
            CreateViewOptions<BsonDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.CreateViewAsync<BsonDocument, BsonDocument>(
                session, viewName, viewOn,
                MqlTemplateParser.ParsePipeline<BsonDocument, BsonDocument>(pipelineTemplate, parameters),
                options, cancellationToken);
        }

        // Watch

        /// <summary>
        /// Watches changes on all collections in the database using a pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> Watch(
            this IMongoDatabase database,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.Watch(ParseChangeStreamPipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collections in the database using a pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> Watch(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.Watch(session, ParseChangeStreamPipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collections in the database using a pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> WatchAsync(
            this IMongoDatabase database,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.WatchAsync(ParseChangeStreamPipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on all collections in the database using a pipeline built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> WatchAsync(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.WatchAsync(session, ParseChangeStreamPipeline(pipelineTemplate, parameters), options, cancellationToken);
        }

        // RunCommand

        /// <summary>
        /// Runs a command built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="commandTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the command.</returns>
        public static BsonDocument RunCommand(
            this IMongoDatabase database,
            string commandTemplate,
            object parameters,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.RunCommand<BsonDocument>(
                MqlTemplateParser.ParseDocument(commandTemplate, parameters),
                readPreference, cancellationToken);
        }

        /// <summary>
        /// Runs a command built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="commandTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the command.</returns>
        public static BsonDocument RunCommand(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string commandTemplate,
            object parameters,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.RunCommand<BsonDocument>(
                session, MqlTemplateParser.ParseDocument(commandTemplate, parameters),
                readPreference, cancellationToken);
        }

        /// <summary>
        /// Runs a command built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="commandTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the command.</returns>
        public static Task<BsonDocument> RunCommandAsync(
            this IMongoDatabase database,
            string commandTemplate,
            object parameters,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            return database.RunCommandAsync<BsonDocument>(
                MqlTemplateParser.ParseDocument(commandTemplate, parameters),
                readPreference, cancellationToken);
        }

        /// <summary>
        /// Runs a command built from a template.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="session">The session.</param>
        /// <param name="commandTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the command.</returns>
        public static Task<BsonDocument> RunCommandAsync(
            this IMongoDatabase database,
            IClientSessionHandle session,
            string commandTemplate,
            object parameters,
            ReadPreference readPreference = null,
            CancellationToken cancellationToken = default)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (session == null) throw new ArgumentNullException(nameof(session));
            return database.RunCommandAsync<BsonDocument>(
                session, MqlTemplateParser.ParseDocument(commandTemplate, parameters),
                readPreference, cancellationToken);
        }

        // Private helpers

        private static PipelineDefinition<NoPipelineInput, BsonDocument> ParsePipeline(
            string template, object parameters) =>
            MqlTemplateParser.ParsePipeline<NoPipelineInput, BsonDocument>(template, parameters);

        private static PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> ParseChangeStreamPipeline(
            string template, object parameters) =>
            MqlTemplateParser.ParsePipeline<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>(template, parameters);
    }
}
