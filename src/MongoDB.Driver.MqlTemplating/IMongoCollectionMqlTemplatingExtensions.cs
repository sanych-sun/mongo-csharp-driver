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
    /// MQL-templating extension methods for <see cref="IMongoCollection{TDocument}"/>.
    /// </summary>
    public static class IMongoCollectionMqlTemplatingExtensions
    {
        // Aggregate

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<TResult> Aggregate<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            return collection.Aggregate<TResult>(pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<TResult> Aggregate<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            return collection.Aggregate<TResult>(session, pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<TResult>> AggregateAsync<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            return collection.AggregateAsync<TResult>(pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<TResult>> AggregateAsync<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            return collection.AggregateAsync<TResult>(session, pipeline, options, cancellationToken);
        }

        // AggregateToCollection

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AggregateToCollection<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            collection.AggregateToCollection<TResult>(pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AggregateToCollection<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            collection.AggregateToCollection<TResult>(session, pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static Task AggregateToCollectionAsync<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            return collection.AggregateToCollectionAsync<TResult>(pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Runs an aggregation pipeline built from a template, writing the results to a collection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TResult">The type of the result document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static Task AggregateToCollectionAsync<TDocument, TResult>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var pipeline = MqlTemplateParser.ParsePipeline<TDocument, TResult>(pipelineTemplate, parameters);
            return collection.AggregateToCollectionAsync<TResult>(session, pipeline, options, cancellationToken);
        }

        // Aggregate — single-type-parameter convenience overloads (TDocument == TResult)

        /// <summary>Runs an aggregation pipeline built from a template, returning documents of the collection's own type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<TDocument> Aggregate<TDocument>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.Aggregate<TDocument, TDocument>(pipelineTemplate, parameters, options, cancellationToken);

        /// <summary>Runs an aggregation pipeline built from a template, returning documents of the collection's own type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<TDocument> Aggregate<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.Aggregate<TDocument, TDocument>(session, pipelineTemplate, parameters, options, cancellationToken);

        /// <summary>Runs an aggregation pipeline built from a template, returning documents of the collection's own type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<TDocument>> AggregateAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.AggregateAsync<TDocument, TDocument>(pipelineTemplate, parameters, options, cancellationToken);

        /// <summary>Runs an aggregation pipeline built from a template, returning documents of the collection's own type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<TDocument>> AggregateAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.AggregateAsync<TDocument, TDocument>(session, pipelineTemplate, parameters, options, cancellationToken);

        /// <summary>Runs an aggregation pipeline built from a template, writing results to a collection, using the collection's own document type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AggregateToCollection<TDocument>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.AggregateToCollection<TDocument, TDocument>(pipelineTemplate, parameters, options, cancellationToken);

        /// <summary>Runs an aggregation pipeline built from a template, writing results to a collection, using the collection's own document type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AggregateToCollection<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.AggregateToCollection<TDocument, TDocument>(session, pipelineTemplate, parameters, options, cancellationToken);

        /// <summary>Runs an aggregation pipeline built from a template, writing results to a collection, using the collection's own document type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static Task AggregateToCollectionAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.AggregateToCollectionAsync<TDocument, TDocument>(pipelineTemplate, parameters, options, cancellationToken);

        /// <summary>Runs an aggregation pipeline built from a template, writing results to a collection, using the collection's own document type.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        public static Task AggregateToCollectionAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default) =>
            collection.AggregateToCollectionAsync<TDocument, TDocument>(session, pipelineTemplate, parameters, options, cancellationToken);

        // CountDocuments

        /// <summary>
        /// Counts documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of documents matching the filter.</returns>
        public static long CountDocuments<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            CountOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.CountDocuments(filter, options, cancellationToken);
        }

        /// <summary>
        /// Counts documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of documents matching the filter.</returns>
        public static long CountDocuments<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            CountOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.CountDocuments(session, filter, options, cancellationToken);
        }

        /// <summary>
        /// Counts documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the count of documents matching the filter.</returns>
        public static Task<long> CountDocumentsAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            CountOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.CountDocumentsAsync(filter, options, cancellationToken);
        }

        /// <summary>
        /// Counts documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the count of documents matching the filter.</returns>
        public static Task<long> CountDocumentsAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            CountOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.CountDocumentsAsync(session, filter, options, cancellationToken);
        }

        // DeleteMany

        /// <summary>
        /// Deletes documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the delete operation.</returns>
        public static DeleteResult DeleteMany<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteMany(filter, options, cancellationToken);
        }

        /// <summary>
        /// Deletes documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the delete operation.</returns>
        public static DeleteResult DeleteMany<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteMany(session, filter, options, cancellationToken);
        }

        /// <summary>
        /// Deletes documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the delete operation.</returns>
        public static Task<DeleteResult> DeleteManyAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteManyAsync(filter, options, cancellationToken);
        }

        /// <summary>
        /// Deletes documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the delete operation.</returns>
        public static Task<DeleteResult> DeleteManyAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteManyAsync(session, filter, options, cancellationToken);
        }

        // DeleteOne

        /// <summary>
        /// Deletes a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the delete operation.</returns>
        public static DeleteResult DeleteOne<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteOne(filter, options, cancellationToken);
        }

        /// <summary>
        /// Deletes a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the delete operation.</returns>
        public static DeleteResult DeleteOne<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteOne(session, filter, options, cancellationToken);
        }

        /// <summary>
        /// Deletes a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the delete operation.</returns>
        public static Task<DeleteResult> DeleteOneAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteOneAsync(filter, options, cancellationToken);
        }

        /// <summary>
        /// Deletes a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the delete operation.</returns>
        public static Task<DeleteResult> DeleteOneAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            DeleteOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DeleteOneAsync(session, filter, options, cancellationToken);
        }

        // Distinct

        /// <summary>
        /// Gets the distinct values for a specified field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor of distinct values.</returns>
        public static IAsyncCursor<TField> Distinct<TDocument, TField>(
            this IMongoCollection<TDocument> collection,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.Distinct<TField>(fieldName, filter, options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor of distinct values.</returns>
        public static IAsyncCursor<TField> Distinct<TDocument, TField>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.Distinct<TField>(session, fieldName, filter, options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor of distinct values.</returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(
            this IMongoCollection<TDocument> collection,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DistinctAsync<TField>(fieldName, filter, options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor of distinct values.</returns>
        public static Task<IAsyncCursor<TField>> DistinctAsync<TDocument, TField>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DistinctAsync<TField>(session, fieldName, filter, options, cancellationToken);
        }

        // DistinctMany

        /// <summary>
        /// Gets the distinct values for a specified array field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the items in the array field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor of distinct values.</returns>
        public static IAsyncCursor<TItem> DistinctMany<TDocument, TItem>(
            this IMongoCollection<TDocument> collection,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DistinctMany<TItem>(fieldName, filter, options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified array field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the items in the array field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor of distinct values.</returns>
        public static IAsyncCursor<TItem> DistinctMany<TDocument, TItem>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DistinctMany<TItem>(session, fieldName, filter, options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified array field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the items in the array field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor of distinct values.</returns>
        public static Task<IAsyncCursor<TItem>> DistinctManyAsync<TDocument, TItem>(
            this IMongoCollection<TDocument> collection,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DistinctManyAsync<TItem>(fieldName, filter, options, cancellationToken);
        }

        /// <summary>
        /// Gets the distinct values for a specified array field using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the items in the array field.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor of distinct values.</returns>
        public static Task<IAsyncCursor<TItem>> DistinctManyAsync<TDocument, TItem>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string fieldName,
            string filterTemplate,
            object parameters,
            DistinctOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.DistinctManyAsync<TItem>(session, fieldName, filter, options, cancellationToken);
        }

        // FindSync

        /// <summary>
        /// Finds documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<TDocument> FindSync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            FindOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindSync<TDocument>(filter, options, cancellationToken);
        }

        /// <summary>
        /// Finds documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A cursor.</returns>
        public static IAsyncCursor<TDocument> FindSync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            FindOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindSync<TDocument>(session, filter, options, cancellationToken);
        }

        /// <summary>
        /// Finds documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<TDocument>> FindAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            FindOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindAsync<TDocument>(filter, options, cancellationToken);
        }

        /// <summary>
        /// Finds documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a cursor.</returns>
        public static Task<IAsyncCursor<TDocument>> FindAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            FindOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindAsync<TDocument>(session, filter, options, cancellationToken);
        }

        // FindOneAndDelete

        /// <summary>
        /// Finds a single document matching a filter built from a template and deletes it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deleted document.</returns>
        public static TDocument FindOneAndDelete<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            FindOneAndDeleteOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndDelete<TDocument>(filter, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and deletes it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deleted document.</returns>
        public static TDocument FindOneAndDelete<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            FindOneAndDeleteOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndDelete<TDocument>(session, filter, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and deletes it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the deleted document.</returns>
        public static Task<TDocument> FindOneAndDeleteAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            FindOneAndDeleteOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndDeleteAsync<TDocument>(filter, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and deletes it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the deleted document.</returns>
        public static Task<TDocument> FindOneAndDeleteAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            FindOneAndDeleteOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndDeleteAsync<TDocument>(session, filter, options, cancellationToken);
        }

        // FindOneAndReplace

        /// <summary>
        /// Finds a single document matching a filter built from a template and replaces it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The document before or after replacement, depending on options.</returns>
        public static TDocument FindOneAndReplace<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            FindOneAndReplaceOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndReplace<TDocument>(filter, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and replaces it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The document before or after replacement, depending on options.</returns>
        public static TDocument FindOneAndReplace<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            FindOneAndReplaceOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndReplace<TDocument>(session, filter, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and replaces it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the document before or after replacement, depending on options.</returns>
        public static Task<TDocument> FindOneAndReplaceAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            FindOneAndReplaceOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndReplaceAsync<TDocument>(filter, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and replaces it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the document before or after replacement, depending on options.</returns>
        public static Task<TDocument> FindOneAndReplaceAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            FindOneAndReplaceOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.FindOneAndReplaceAsync<TDocument>(session, filter, replacement, options, cancellationToken);
        }

        // FindOneAndUpdate

        /// <summary>
        /// Finds a single document matching a filter built from a template and updates it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The document before or after update, depending on options.</returns>
        public static TDocument FindOneAndUpdate<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            FindOneAndUpdateOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.FindOneAndUpdate<TDocument>(filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and updates it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The document before or after update, depending on options.</returns>
        public static TDocument FindOneAndUpdate<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            FindOneAndUpdateOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.FindOneAndUpdate<TDocument>(session, filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and updates it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the document before or after update, depending on options.</returns>
        public static Task<TDocument> FindOneAndUpdateAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            FindOneAndUpdateOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.FindOneAndUpdateAsync<TDocument>(filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Finds a single document matching a filter built from a template and updates it.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the document before or after update, depending on options.</returns>
        public static Task<TDocument> FindOneAndUpdateAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            FindOneAndUpdateOptions<TDocument, TDocument> options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.FindOneAndUpdateAsync<TDocument>(session, filter, update, options, cancellationToken);
        }

        // ReplaceOne

        /// <summary>
        /// Replaces a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the replace operation.</returns>
        public static ReplaceOneResult ReplaceOne<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            ReplaceOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.ReplaceOne(filter, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Replaces a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the replace operation.</returns>
        public static ReplaceOneResult ReplaceOne<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            ReplaceOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.ReplaceOne(session, filter, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Replaces a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the replace operation.</returns>
        public static Task<ReplaceOneResult> ReplaceOneAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            ReplaceOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.ReplaceOneAsync(filter, replacement, options, cancellationToken);
        }

        /// <summary>
        /// Replaces a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="replacement">The replacement document.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the replace operation.</returns>
        public static Task<ReplaceOneResult> ReplaceOneAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            TDocument replacement,
            ReplaceOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
        }

        // UpdateMany

        /// <summary>
        /// Updates documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the update operation.</returns>
        public static UpdateResult UpdateMany<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateMany(filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Updates documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the update operation.</returns>
        public static UpdateResult UpdateMany<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateMany(session, filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Updates documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the update operation.</returns>
        public static Task<UpdateResult> UpdateManyAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateManyAsync(filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Updates documents matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the update operation.</returns>
        public static Task<UpdateResult> UpdateManyAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateManyAsync(session, filter, update, options, cancellationToken);
        }

        // UpdateOne

        /// <summary>
        /// Updates a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the update operation.</returns>
        public static UpdateResult UpdateOne<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateOne(filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Updates a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the update operation.</returns>
        public static UpdateResult UpdateOne<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateOne(session, filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Updates a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the update operation.</returns>
        public static Task<UpdateResult> UpdateOneAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateOneAsync(filter, update, options, cancellationToken);
        }

        /// <summary>
        /// Updates a single document matching a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="filterParams">An object whose public properties supply values for @identifier placeholders in the filter.</param>
        /// <param name="updateTemplate">A JSON document template for the update. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="updateParams">An object whose public properties supply values for @identifier placeholders in the update.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the result of the update operation.</returns>
        public static Task<UpdateResult> UpdateOneAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object filterParams,
            string updateTemplate,
            object updateParams,
            UpdateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, filterParams);
            var update = MqlTemplateParser.ParseUpdate<TDocument>(updateTemplate, updateParams);
            return collection.UpdateOneAsync(session, filter, update, options, cancellationToken);
        }

        // Find (fluent)

        /// <summary>
        /// Begins a fluent find interface using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <returns>A fluent find interface.</returns>
        public static IFindFluent<TDocument, TDocument> Find<TDocument>(
            this IMongoCollection<TDocument> collection,
            string filterTemplate,
            object parameters,
            FindOptions options = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.Find(filter, options);
        }

        /// <summary>
        /// Begins a fluent find interface using a filter built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <returns>A fluent find interface.</returns>
        public static IFindFluent<TDocument, TDocument> Find<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string filterTemplate,
            object parameters,
            FindOptions options = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var filter = MqlTemplateParser.ParseFilter<TDocument>(filterTemplate, parameters);
            return collection.Find(session, filter, options);
        }

        // Watch

        /// <summary>
        /// Watches changes on the collection using a pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static IChangeStreamCursor<ChangeStreamDocument<TDocument>> Watch<TDocument>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var pipeline = MqlTemplateParser.ParsePipeline<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>(pipelineTemplate, parameters);
            return collection.Watch(pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on the collection using a pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A change stream.</returns>
        public static IChangeStreamCursor<ChangeStreamDocument<TDocument>> Watch<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var pipeline = MqlTemplateParser.ParsePipeline<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>(pipelineTemplate, parameters);
            return collection.Watch(session, pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on the collection using a pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a change stream.</returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<TDocument>>> WatchAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var pipeline = MqlTemplateParser.ParsePipeline<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>(pipelineTemplate, parameters);
            return collection.WatchAsync(pipeline, options, cancellationToken);
        }

        /// <summary>
        /// Watches changes on the collection using a pipeline built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="session">The session.</param>
        /// <param name="pipelineTemplate">A JSON array of pipeline stage documents. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a change stream.</returns>
        public static Task<IChangeStreamCursor<ChangeStreamDocument<TDocument>>> WatchAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            string pipelineTemplate,
            object parameters,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var pipeline = MqlTemplateParser.ParsePipeline<ChangeStreamDocument<TDocument>, ChangeStreamDocument<TDocument>>(pipelineTemplate, parameters);
            return collection.WatchAsync(session, pipeline, options, cancellationToken);
        }
    }
}
