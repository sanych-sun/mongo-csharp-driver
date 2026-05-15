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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.MqlTemplating.Tests
{
    public class IMongoCollectionMqlTemplatingExtensionsTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private static IList<BsonDocument> GetStages<TInput, TOutput>(
            PipelineDefinition<TInput, TOutput> pipeline) =>
            ((BsonDocumentStagePipelineDefinition<TInput, TOutput>)pipeline).Documents;

        private static BsonDocument RenderFilter(FilterDefinition<BsonDocument> filter) =>
            filter.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));

        private static BsonDocument RenderUpdate(UpdateDefinition<BsonDocument> update) =>
            (BsonDocument)update.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));

        private static Mock<IMongoCollection<BsonDocument>> CreateCollection() =>
            new Mock<IMongoCollection<BsonDocument>>();

        // ── A. Aggregate (sync happy path) ───────────────────────────────────

        [Fact]
        public void Aggregate_passes_correct_pipeline()
        {
            var collection = CreateCollection();
            PipelineDefinition<BsonDocument, BsonDocument> captured = null;
            collection
                .Setup(c => c.Aggregate<BsonDocument>(
                    It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<BsonDocument, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, o, ct) => captured = p);

            collection.Object.Aggregate("[{ '$match': { 'x': @val } }]", new { val = 42 });

            var stage = GetStages(captured)[0];
            ((BsonValue)stage["$match"]["x"]).Should().Be((BsonValue)42);
        }

        [Fact]
        public void Aggregate_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.Aggregate("[{ '$match': {} }]", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── A. Aggregate (async happy path) ──────────────────────────────────

        [Fact]
        public async Task AggregateAsync_passes_correct_pipeline()
        {
            var collection = CreateCollection();
            PipelineDefinition<BsonDocument, BsonDocument> captured = null;
            collection
                .Setup(c => c.AggregateAsync<BsonDocument>(
                    It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<BsonDocument, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, _, _) => captured = p)
                .Returns(Task.FromResult(Mock.Of<IAsyncCursor<BsonDocument>>()));

            await collection.Object.AggregateAsync("[{ '$match': { 'y': @val } }]", new { val = 99 });

            var stage = GetStages(captured)[0];
            ((BsonValue)stage["$match"]["y"]).Should().Be((BsonValue)99);
        }

        [Fact]
        public void AggregateAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.AggregateAsync("[{ '$match': {} }]", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── B. AggregateToCollection ──────────────────────────────────────────

        [Fact]
        public void AggregateToCollection_passes_correct_pipeline()
        {
            var collection = CreateCollection();
            PipelineDefinition<BsonDocument, BsonDocument> captured = null;
            collection
                .Setup(c => c.AggregateToCollection<BsonDocument>(
                    It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<BsonDocument, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, _, _) => captured = p);

            collection.Object.AggregateToCollection("[{ '$out': 'dest' }]", new { });

            var stage = GetStages(captured)[0];
            stage.Contains("$out").Should().BeTrue();
        }

        [Fact]
        public void AggregateToCollection_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.AggregateToCollection("[{ '$out': 'dest' }]", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async Task AggregateToCollectionAsync_passes_correct_pipeline()
        {
            var collection = CreateCollection();
            PipelineDefinition<BsonDocument, BsonDocument> captured = null;
            collection
                .Setup(c => c.AggregateToCollectionAsync<BsonDocument>(
                    It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<BsonDocument, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, _, _) => captured = p)
                .Returns(Task.CompletedTask);

            await collection.Object.AggregateToCollectionAsync("[{ '$out': 'dest' }]", new { });

            var stage = GetStages(captured)[0];
            stage.Contains("$out").Should().BeTrue();
        }

        [Fact]
        public void AggregateToCollectionAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.AggregateToCollectionAsync("[{ '$out': 'dest' }]", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── C. CountDocuments ─────────────────────────────────────────────────

        [Fact]
        public void CountDocuments_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.CountDocuments(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, CountOptions, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(5L);

            collection.Object.CountDocuments("{ 'status': @s }", new { s = "active" });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["status"]).Should().Be((BsonValue)"active");
        }

        [Fact]
        public void CountDocuments_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.CountDocuments("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async Task CountDocumentsAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, CountOptions, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(Task.FromResult(7L));

            await collection.Object.CountDocumentsAsync("{ 'age': { '$gt': @min } }", new { min = 18 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["age"]["$gt"]).Should().Be((BsonValue)18);
        }

        [Fact]
        public void CountDocumentsAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.CountDocumentsAsync("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── D. DeleteMany ─────────────────────────────────────────────────────

        [Fact]
        public void DeleteMany_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.DeleteMany(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DeleteOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, DeleteOptions, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(new DeleteResult.Acknowledged(1));

            collection.Object.DeleteMany("{ 'x': @v }", new { v = 10 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["x"]).Should().Be((BsonValue)10);
        }

        [Fact]
        public void DeleteMany_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.DeleteMany("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteManyAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.DeleteManyAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DeleteOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, DeleteOptions, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(2)));

            await collection.Object.DeleteManyAsync("{ 'x': @v }", new { v = 20 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["x"]).Should().Be((BsonValue)20);
        }

        [Fact]
        public void DeleteManyAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.DeleteManyAsync("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── E. DeleteOne ──────────────────────────────────────────────────────

        [Fact]
        public void DeleteOne_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.DeleteOne(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DeleteOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, DeleteOptions, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(new DeleteResult.Acknowledged(1));

            collection.Object.DeleteOne("{ '_id': @id }", new { id = 42 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)42);
        }

        [Fact]
        public void DeleteOne_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.DeleteOne("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void DeleteOneAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.DeleteOneAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DeleteOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, DeleteOptions, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));

            collection.Object.DeleteOneAsync("{ '_id': @id }", new { id = 99 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)99);
        }

        [Fact]
        public void DeleteOneAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.DeleteOneAsync("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── F. Distinct ───────────────────────────────────────────────────────

        [Fact]
        public void Distinct_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.Distinct<string>(
                    It.IsAny<FieldDefinition<BsonDocument, string>>(),
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DistinctOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FieldDefinition<BsonDocument, string>, FilterDefinition<BsonDocument>, DistinctOptions, CancellationToken>(
                    (field, f, o, ct) => captured = f)
                .Returns(Mock.Of<IAsyncCursor<string>>());

            collection.Object.Distinct<BsonDocument, string>("name", "{ 'active': @a }", new { a = true });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["active"]).Should().Be(BsonBoolean.True);
        }

        [Fact]
        public void Distinct_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.Distinct<BsonDocument, string>("name", "{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void DistinctAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.DistinctAsync<string>(
                    It.IsAny<FieldDefinition<BsonDocument, string>>(),
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DistinctOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FieldDefinition<BsonDocument, string>, FilterDefinition<BsonDocument>, DistinctOptions, CancellationToken>(
                    (field, f, o, ct) => captured = f)
                .Returns(Task.FromResult(Mock.Of<IAsyncCursor<string>>()));

            collection.Object.DistinctAsync<BsonDocument, string>("name", "{ 'active': @a }", new { a = false });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["active"]).Should().Be(BsonBoolean.False);
        }

        [Fact]
        public void DistinctAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.DistinctAsync<BsonDocument, string>("name", "{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── G. DistinctMany ───────────────────────────────────────────────────

        [Fact]
        public void DistinctMany_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.DistinctMany<string>(
                    It.IsAny<FieldDefinition<BsonDocument, IEnumerable<string>>>(),
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DistinctOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FieldDefinition<BsonDocument, IEnumerable<string>>, FilterDefinition<BsonDocument>, DistinctOptions, CancellationToken>(
                    (field, f, o, ct) => captured = f)
                .Returns(Mock.Of<IAsyncCursor<string>>());

            collection.Object.DistinctMany<BsonDocument, string>("tags", "{ 'published': @p }", new { p = true });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["published"]).Should().Be(BsonBoolean.True);
        }

        [Fact]
        public void DistinctMany_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.DistinctMany<BsonDocument, string>("tags", "{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void DistinctManyAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.DistinctManyAsync<string>(
                    It.IsAny<FieldDefinition<BsonDocument, IEnumerable<string>>>(),
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<DistinctOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FieldDefinition<BsonDocument, IEnumerable<string>>, FilterDefinition<BsonDocument>, DistinctOptions, CancellationToken>(
                    (field, f, o, ct) => captured = f)
                .Returns(Task.FromResult(Mock.Of<IAsyncCursor<string>>()));

            collection.Object.DistinctManyAsync<BsonDocument, string>("tags", "{ 'published': @p }", new { p = false });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["published"]).Should().Be(BsonBoolean.False);
        }

        [Fact]
        public void DistinctManyAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.DistinctManyAsync<BsonDocument, string>("tags", "{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── H. FindSync / FindAsync ───────────────────────────────────────────

        [Fact]
        public void FindSync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.FindSync<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, FindOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(Mock.Of<IAsyncCursor<BsonDocument>>());

            collection.Object.FindSync("{ 'score': { '$gte': @min } }", new { min = 50 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["score"]["$gte"]).Should().Be((BsonValue)50);
        }

        [Fact]
        public void FindSync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.FindSync("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async Task FindAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.FindAsync<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<FindOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, FindOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(Task.FromResult(Mock.Of<IAsyncCursor<BsonDocument>>()));

            await collection.Object.FindAsync("{ 'name': @n }", new { n = "Alice" });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["name"]).Should().Be((BsonValue)"Alice");
        }

        [Fact]
        public void FindAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.FindAsync("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── I. FindOneAndDelete ────────────────────────────────────────────────

        [Fact]
        public void FindOneAndDelete_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.FindOneAndDelete<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<FindOneAndDeleteOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, FindOneAndDeleteOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns((BsonDocument)null);

            collection.Object.FindOneAndDelete("{ '_id': @id }", new { id = 7 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)7);
        }

        [Fact]
        public void FindOneAndDelete_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.FindOneAndDelete("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void FindOneAndDeleteAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> captured = null;
            collection
                .Setup(c => c.FindOneAndDeleteAsync<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<FindOneAndDeleteOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, FindOneAndDeleteOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, o, ct) => captured = f)
                .Returns(Task.FromResult<BsonDocument>(null));

            collection.Object.FindOneAndDeleteAsync("{ '_id': @id }", new { id = 8 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)8);
        }

        [Fact]
        public void FindOneAndDeleteAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.FindOneAndDeleteAsync("{ }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── J. FindOneAndReplace ──────────────────────────────────────────────

        [Fact]
        public void FindOneAndReplace_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            var replacement = new BsonDocument("x", 1);
            collection
                .Setup(c => c.FindOneAndReplace<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<BsonDocument>(),
                    It.IsAny<FindOneAndReplaceOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, BsonDocument, FindOneAndReplaceOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, r, o, ct) => capturedFilter = f)
                .Returns((BsonDocument)null);

            collection.Object.FindOneAndReplace("{ '_id': @id }", new { id = 3 }, replacement);

            var rendered = RenderFilter(capturedFilter);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)3);
        }

        [Fact]
        public void FindOneAndReplace_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var rep = new BsonDocument();
            var ex = Record.Exception(() => { nullCollection.FindOneAndReplace("{ }", new { }, rep); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void FindOneAndReplaceAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            var replacement = new BsonDocument("x", 1);
            collection
                .Setup(c => c.FindOneAndReplaceAsync<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<BsonDocument>(),
                    It.IsAny<FindOneAndReplaceOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, BsonDocument, FindOneAndReplaceOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, r, o, ct) => capturedFilter = f)
                .Returns(Task.FromResult<BsonDocument>(null));

            collection.Object.FindOneAndReplaceAsync("{ '_id': @id }", new { id = 5 }, replacement);

            var rendered = RenderFilter(capturedFilter);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)5);
        }

        [Fact]
        public void FindOneAndReplaceAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var rep = new BsonDocument();
            var ex = Record.Exception(() => { nullCollection.FindOneAndReplaceAsync("{ }", new { }, rep); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── K. FindOneAndUpdate ────────────────────────────────────────────────

        [Fact]
        public void FindOneAndUpdate_passes_correct_filter_and_update()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            UpdateDefinition<BsonDocument> capturedUpdate = null;
            collection
                .Setup(c => c.FindOneAndUpdate<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<FindOneAndUpdateOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, UpdateDefinition<BsonDocument>, FindOneAndUpdateOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, u, o, ct) => { capturedFilter = f; capturedUpdate = u; })
                .Returns((BsonDocument)null);

            collection.Object.FindOneAndUpdate(
                "{ '_id': @id }", new { id = 1 },
                "{ '$set': { 'v': @val } }", new { val = 42 });

            var renderedFilter = RenderFilter(capturedFilter);
            ((BsonValue)renderedFilter["_id"]).Should().Be((BsonValue)1);

            var renderedUpdate = RenderUpdate(capturedUpdate);
            ((BsonValue)renderedUpdate["$set"]["v"]).Should().Be((BsonValue)42);
        }

        [Fact]
        public void FindOneAndUpdate_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.FindOneAndUpdate("{ }", new { }, "{ '$set': {} }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void FindOneAndUpdateAsync_passes_correct_filter_and_update()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            UpdateDefinition<BsonDocument> capturedUpdate = null;
            collection
                .Setup(c => c.FindOneAndUpdateAsync<BsonDocument>(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<FindOneAndUpdateOptions<BsonDocument, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, UpdateDefinition<BsonDocument>, FindOneAndUpdateOptions<BsonDocument, BsonDocument>, CancellationToken>(
                    (f, u, o, ct) => { capturedFilter = f; capturedUpdate = u; })
                .Returns(Task.FromResult<BsonDocument>(null));

            collection.Object.FindOneAndUpdateAsync(
                "{ '_id': @id }", new { id = 2 },
                "{ '$set': { 'v': @val } }", new { val = 100 });

            var renderedFilter = RenderFilter(capturedFilter);
            ((BsonValue)renderedFilter["_id"]).Should().Be((BsonValue)2);

            var renderedUpdate = RenderUpdate(capturedUpdate);
            ((BsonValue)renderedUpdate["$set"]["v"]).Should().Be((BsonValue)100);
        }

        [Fact]
        public void FindOneAndUpdateAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.FindOneAndUpdateAsync("{ }", new { }, "{ '$set': {} }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── L. ReplaceOne ─────────────────────────────────────────────────────

        [Fact]
        public void ReplaceOne_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            var replacement = new BsonDocument("x", 1);
            collection
                .Setup(c => c.ReplaceOne(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<BsonDocument>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, BsonDocument, ReplaceOptions, CancellationToken>(
                    (f, r, o, ct) => capturedFilter = f)
                .Returns(new ReplaceOneResult.Acknowledged(1, 1, null));

            collection.Object.ReplaceOne("{ '_id': @id }", new { id = 6 }, replacement);

            var rendered = RenderFilter(capturedFilter);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)6);
        }

        [Fact]
        public void ReplaceOne_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var rep = new BsonDocument();
            var ex = Record.Exception(() => { nullCollection.ReplaceOne("{ }", new { }, rep); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void ReplaceOneAsync_passes_correct_filter()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            var replacement = new BsonDocument("x", 1);
            collection
                .Setup(c => c.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<BsonDocument>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, BsonDocument, ReplaceOptions, CancellationToken>(
                    (f, r, o, ct) => capturedFilter = f)
                .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));

            collection.Object.ReplaceOneAsync("{ '_id': @id }", new { id = 9 }, replacement);

            var rendered = RenderFilter(capturedFilter);
            ((BsonValue)rendered["_id"]).Should().Be((BsonValue)9);
        }

        [Fact]
        public void ReplaceOneAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var rep = new BsonDocument();
            var ex = Record.Exception(() => { nullCollection.ReplaceOneAsync("{ }", new { }, rep); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── M. UpdateMany ─────────────────────────────────────────────────────

        [Fact]
        public void UpdateMany_passes_correct_filter_and_update()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            UpdateDefinition<BsonDocument> capturedUpdate = null;
            collection
                .Setup(c => c.UpdateMany(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, UpdateDefinition<BsonDocument>, UpdateOptions, CancellationToken>(
                    (f, u, o, ct) => { capturedFilter = f; capturedUpdate = u; })
                .Returns(new UpdateResult.Acknowledged(5, 5, null));

            collection.Object.UpdateMany(
                "{ 'status': @s }", new { s = "pending" },
                "{ '$set': { 'status': @ns } }", new { ns = "done" });

            var renderedFilter = RenderFilter(capturedFilter);
            ((BsonValue)renderedFilter["status"]).Should().Be((BsonValue)"pending");

            var renderedUpdate = RenderUpdate(capturedUpdate);
            ((BsonValue)renderedUpdate["$set"]["status"]).Should().Be((BsonValue)"done");
        }

        [Fact]
        public void UpdateMany_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.UpdateMany("{ }", new { }, "{ '$set': {} }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void UpdateManyAsync_passes_correct_filter_and_update()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            UpdateDefinition<BsonDocument> capturedUpdate = null;
            collection
                .Setup(c => c.UpdateManyAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, UpdateDefinition<BsonDocument>, UpdateOptions, CancellationToken>(
                    (f, u, o, ct) => { capturedFilter = f; capturedUpdate = u; })
                .Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(3, 3, null)));

            collection.Object.UpdateManyAsync(
                "{ 'a': @a }", new { a = 1 },
                "{ '$inc': { 'cnt': @n } }", new { n = 1 });

            var renderedFilter = RenderFilter(capturedFilter);
            ((BsonValue)renderedFilter["a"]).Should().Be((BsonValue)1);

            var renderedUpdate = RenderUpdate(capturedUpdate);
            ((BsonValue)renderedUpdate["$inc"]["cnt"]).Should().Be((BsonValue)1);
        }

        [Fact]
        public void UpdateManyAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.UpdateManyAsync("{ }", new { }, "{ '$set': {} }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── N. Watch ──────────────────────────────────────────────────────────

        [Fact]
        public void Watch_passes_correct_pipeline()
        {
            var collection = CreateCollection();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            collection
                .Setup(c => c.Watch<ChangeStreamDocument<BsonDocument>>(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (p, o, ct) => captured = p)
                .Returns(Mock.Of<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>());

            collection.Object.Watch("[{ '$match': { 'operationType': @op } }]", new { op = "insert" });

            var stage = GetStages(captured)[0];
            ((BsonValue)stage["$match"]["operationType"]).Should().Be((BsonValue)"insert");
        }

        [Fact]
        public void Watch_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.Watch("[{ '$match': {} }]", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async Task WatchAsync_passes_correct_pipeline()
        {
            var collection = CreateCollection();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            collection
                .Setup(c => c.WatchAsync<ChangeStreamDocument<BsonDocument>>(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (p, o, ct) => captured = p)
                .Returns(Task.FromResult(Mock.Of<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>()));

            await collection.Object.WatchAsync("[{ '$match': { 'operationType': @op } }]", new { op = "delete" });

            var stage = GetStages(captured)[0];
            ((BsonValue)stage["$match"]["operationType"]).Should().Be((BsonValue)"delete");
        }

        [Fact]
        public void WatchAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.WatchAsync("[{ '$match': {} }]", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── O. UpdateOne ──────────────────────────────────────────────────────

        [Fact]
        public void UpdateOne_passes_correct_filter_and_update()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            UpdateDefinition<BsonDocument> capturedUpdate = null;
            collection
                .Setup(c => c.UpdateOne(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, UpdateDefinition<BsonDocument>, UpdateOptions, CancellationToken>(
                    (f, u, o, ct) => { capturedFilter = f; capturedUpdate = u; })
                .Returns(new UpdateResult.Acknowledged(1, 1, null));

            collection.Object.UpdateOne(
                "{ '_id': @id }", new { id = 11 },
                "{ '$set': { 'done': @v } }", new { v = true });

            var renderedFilter = RenderFilter(capturedFilter);
            ((BsonValue)renderedFilter["_id"]).Should().Be((BsonValue)11);

            var renderedUpdate = RenderUpdate(capturedUpdate);
            ((BsonValue)renderedUpdate["$set"]["done"]).Should().Be(BsonBoolean.True);
        }

        [Fact]
        public void UpdateOne_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.UpdateOne("{ }", new { }, "{ '$set': {} }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public async Task UpdateOneAsync_passes_correct_filter_and_update()
        {
            var collection = CreateCollection();
            FilterDefinition<BsonDocument> capturedFilter = null;
            UpdateDefinition<BsonDocument> capturedUpdate = null;
            collection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<BsonDocument>>(),
                    It.IsAny<UpdateDefinition<BsonDocument>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<BsonDocument>, UpdateDefinition<BsonDocument>, UpdateOptions, CancellationToken>(
                    (f, u, o, ct) => { capturedFilter = f; capturedUpdate = u; })
                .Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null)));

            await collection.Object.UpdateOneAsync(
                "{ '_id': @id }", new { id = 12 },
                "{ '$set': { 'val': @v } }", new { v = 55 });

            var renderedFilter = RenderFilter(capturedFilter);
            ((BsonValue)renderedFilter["_id"]).Should().Be((BsonValue)12);

            var renderedUpdate = RenderUpdate(capturedUpdate);
            ((BsonValue)renderedUpdate["$set"]["val"]).Should().Be((BsonValue)55);
        }

        [Fact]
        public void UpdateOneAsync_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.UpdateOneAsync("{ }", new { }, "{ '$set': {} }", new { }); });
            ex.Should().BeOfType<ArgumentNullException>();
        }

        // ── P. Find (fluent) ──────────────────────────────────────────────────

        [Fact]
        public void Find_passes_correct_filter()
        {
            var collection = CreateCollection();

            var fluent = collection.Object.Find("{ 'score': { '$gte': @min } }", new { min = 80 });

            var rendered = RenderFilter(fluent.Filter);
            ((BsonValue)rendered["score"]["$gte"]).Should().Be((BsonValue)80);
        }

        [Fact]
        public void Find_with_session_passes_correct_filter()
        {
            var collection = CreateCollection();
            var session = new Mock<IClientSessionHandle>().Object;

            var fluent = collection.Object.Find(session, "{ 'active': @flag }", new { flag = "true" });

            var rendered = RenderFilter(fluent.Filter);
            ((BsonValue)rendered["active"]).Should().Be(BsonBoolean.True);
        }

        [Fact]
        public void Find_null_collection_throws()
        {
            IMongoCollection<BsonDocument> nullCollection = null;
            var ex = Record.Exception(() => { nullCollection.Find("{ }", null); });
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("collection");
        }

        [Fact]
        public void Find_with_session_null_session_throws()
        {
            var collection = CreateCollection();
            var ex = Record.Exception(() => { collection.Object.Find((IClientSessionHandle)null, "{ }", null); });
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("session");
        }
    }
}
