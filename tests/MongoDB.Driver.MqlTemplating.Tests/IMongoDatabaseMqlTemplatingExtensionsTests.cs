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
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.MqlTemplating.Tests
{
    public class IMongoDatabaseMqlTemplatingExtensionsTests
    {
        private static readonly IClientSessionHandle s_session = new Mock<IClientSessionHandle>().Object;

        // ---- Aggregate ----

        [Fact]
        public void Aggregate_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<NoPipelineInput, BsonDocument> captured = null;
            mock.Setup(db => db.Aggregate(
                    It.IsAny<PipelineDefinition<NoPipelineInput, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<NoPipelineInput, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns((IAsyncCursor<BsonDocument>)null);

            mock.Object.Aggregate("[{ '$match': { 'status': @s } }]", new { s = "active" });

            var stages = GetStages(captured);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'status': 'active' } }"));
        }

        [Fact]
        public void Aggregate_with_session_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<NoPipelineInput, BsonDocument> captured = null;
            mock.Setup(db => db.Aggregate(
                    s_session,
                    It.IsAny<PipelineDefinition<NoPipelineInput, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IClientSessionHandle, PipelineDefinition<NoPipelineInput, BsonDocument>, AggregateOptions, CancellationToken>(
                    (_, p, __, ___) => captured = p)
                .Returns((IAsyncCursor<BsonDocument>)null);

            mock.Object.Aggregate(s_session, "[{ '$match': { 'x': @v } }]", new { v = 1 });

            var stages = GetStages(captured);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'x': 1 } }"));
        }

        [Fact]
        public async Task AggregateAsync_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<NoPipelineInput, BsonDocument> captured = null;
            mock.Setup(db => db.AggregateAsync(
                    It.IsAny<PipelineDefinition<NoPipelineInput, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<NoPipelineInput, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns(Task.FromResult((IAsyncCursor<BsonDocument>)null));

            await mock.Object.AggregateAsync("[{ '$count': 'total' }]", null);

            GetStages(captured).Should().HaveCount(1);
            ((BsonValue)GetStages(captured)[0]).Should().Be(BsonDocument.Parse("{ '$count': 'total' }"));
        }

        [Fact]
        public void Aggregate_null_database_throws_ArgumentNullException()
        {
            IMongoDatabase db = null;
            var ex = Record.Exception(() => db.Aggregate("[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("database");
        }

        [Fact]
        public void Aggregate_with_session_null_database_throws_ArgumentNullException()
        {
            IMongoDatabase db = null;
            var ex = Record.Exception(() => db.Aggregate(s_session, "[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("database");
        }

        [Fact]
        public void Aggregate_with_session_null_session_throws_ArgumentNullException()
        {
            var mock = new Mock<IMongoDatabase>();
            var ex = Record.Exception(() => mock.Object.Aggregate((IClientSessionHandle)null, "[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("session");
        }

        // ---- AggregateToCollection ----

        [Fact]
        public void AggregateToCollection_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<NoPipelineInput, BsonDocument> captured = null;
            mock.Setup(db => db.AggregateToCollection(
                    It.IsAny<PipelineDefinition<NoPipelineInput, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<NoPipelineInput, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, _, __) => captured = p);

            mock.Object.AggregateToCollection("[{ '$out': 'results' }]", null);

            ((BsonValue)GetStages(captured)[0]).Should().Be(BsonDocument.Parse("{ '$out': 'results' }"));
        }

        [Fact]
        public async Task AggregateToCollectionAsync_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<NoPipelineInput, BsonDocument> captured = null;
            mock.Setup(db => db.AggregateToCollectionAsync(
                    It.IsAny<PipelineDefinition<NoPipelineInput, BsonDocument>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<NoPipelineInput, BsonDocument>, AggregateOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns(Task.CompletedTask);

            await mock.Object.AggregateToCollectionAsync("[{ '$out': 'results' }]", null);

            ((BsonValue)GetStages(captured)[0]).Should().Be(BsonDocument.Parse("{ '$out': 'results' }"));
        }

        [Fact]
        public void AggregateToCollection_null_database_throws_ArgumentNullException()
        {
            IMongoDatabase db = null;
            var ex = Record.Exception(() => db.AggregateToCollection("[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("database");
        }

        [Fact]
        public void AggregateToCollection_with_session_null_session_throws_ArgumentNullException()
        {
            var mock = new Mock<IMongoDatabase>();
            var ex = Record.Exception(() => mock.Object.AggregateToCollection((IClientSessionHandle)null, "[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("session");
        }

        // ---- CreateView ----

        [Fact]
        public void CreateView_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            string capturedViewName = null, capturedViewOn = null;
            PipelineDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(db => db.CreateView<BsonDocument, BsonDocument>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(),
                    It.IsAny<CreateViewOptions<BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, PipelineDefinition<BsonDocument, BsonDocument>, CreateViewOptions<BsonDocument>, CancellationToken>(
                    (vn, vo, p, _, __) => { capturedViewName = vn; capturedViewOn = vo; captured = p; });

            mock.Object.CreateView("myView", "sourceCol", "[{ '$project': { 'name': @f } }]", new { f = 1 });

            capturedViewName.Should().Be("myView");
            capturedViewOn.Should().Be("sourceCol");
            ((BsonValue)GetStages(captured)[0]).Should().Be(BsonDocument.Parse("{ '$project': { 'name': 1 } }"));
        }

        [Fact]
        public async Task CreateViewAsync_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(db => db.CreateViewAsync<BsonDocument, BsonDocument>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(),
                    It.IsAny<CreateViewOptions<BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, PipelineDefinition<BsonDocument, BsonDocument>, CreateViewOptions<BsonDocument>, CancellationToken>(
                    (_, __, p, ___, ____) => captured = p)
                .Returns(Task.CompletedTask);

            await mock.Object.CreateViewAsync("v", "c", "[{ '$match': { 'active': @a } }]", new { a = true });

            ((BsonValue)GetStages(captured)[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'active': true } }"));
        }

        [Fact]
        public void CreateView_null_database_throws_ArgumentNullException()
        {
            IMongoDatabase db = null;
            var ex = Record.Exception(() => db.CreateView("v", "c", "[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("database");
        }

        [Fact]
        public void CreateView_with_session_null_session_throws_ArgumentNullException()
        {
            var mock = new Mock<IMongoDatabase>();
            var ex = Record.Exception(() => mock.Object.CreateView((IClientSessionHandle)null, "v", "c", "[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("session");
        }

        // ---- Watch ----

        [Fact]
        public void Watch_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            mock.Setup(db => db.Watch(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null);

            mock.Object.Watch("[{ '$match': { 'operationType': @op } }]", new { op = "insert" });

            ((BsonValue)GetStages(captured)[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'operationType': 'insert' } }"));
        }

        [Fact]
        public async Task WatchAsync_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            mock.Setup(db => db.WatchAsync(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns(Task.FromResult((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null));

            await mock.Object.WatchAsync("[{ '$match': { 'operationType': @op } }]", new { op = "delete" });

            ((BsonValue)GetStages(captured)[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'operationType': 'delete' } }"));
        }

        [Fact]
        public void Watch_null_database_throws_ArgumentNullException()
        {
            IMongoDatabase db = null;
            var ex = Record.Exception(() => db.Watch("[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("database");
        }

        [Fact]
        public void Watch_with_session_null_session_throws_ArgumentNullException()
        {
            var mock = new Mock<IMongoDatabase>();
            var ex = Record.Exception(() => mock.Object.Watch((IClientSessionHandle)null, "[]", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("session");
        }

        // ---- RunCommand ----

        [Fact]
        public void RunCommand_builds_command_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            Command<BsonDocument> captured = null;
            mock.Setup(db => db.RunCommand<BsonDocument>(
                    It.IsAny<Command<BsonDocument>>(),
                    It.IsAny<ReadPreference>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Command<BsonDocument>, ReadPreference, CancellationToken>(
                    (cmd, _, __) => captured = cmd)
                .Returns(new BsonDocument());

            mock.Object.RunCommand("{ ping: @v }", new { v = 1 });

            ((BsonValue)RenderCommand(captured)).Should().Be(BsonDocument.Parse("{ ping: 1 }"));
        }

        [Fact]
        public void RunCommand_with_session_builds_command_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            Command<BsonDocument> captured = null;
            mock.Setup(db => db.RunCommand<BsonDocument>(
                    s_session,
                    It.IsAny<Command<BsonDocument>>(),
                    It.IsAny<ReadPreference>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IClientSessionHandle, Command<BsonDocument>, ReadPreference, CancellationToken>(
                    (_, cmd, __, ___) => captured = cmd)
                .Returns(new BsonDocument());

            mock.Object.RunCommand(s_session, "{ dbStats: @v }", new { v = 1 });

            ((BsonValue)RenderCommand(captured)).Should().Be(BsonDocument.Parse("{ dbStats: 1 }"));
        }

        [Fact]
        public async Task RunCommandAsync_builds_command_from_template_and_parameters()
        {
            var mock = new Mock<IMongoDatabase>();
            Command<BsonDocument> captured = null;
            mock.Setup(db => db.RunCommandAsync<BsonDocument>(
                    It.IsAny<Command<BsonDocument>>(),
                    It.IsAny<ReadPreference>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Command<BsonDocument>, ReadPreference, CancellationToken>(
                    (cmd, _, __) => captured = cmd)
                .Returns(Task.FromResult(new BsonDocument()));

            await mock.Object.RunCommandAsync("{ ping: @v }", new { v = 1 });

            ((BsonValue)RenderCommand(captured)).Should().Be(BsonDocument.Parse("{ ping: 1 }"));
        }

        [Fact]
        public void RunCommand_null_database_throws_ArgumentNullException()
        {
            IMongoDatabase db = null;
            var ex = Record.Exception(() => db.RunCommand("{ ping: 1 }", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("database");
        }

        [Fact]
        public void RunCommand_with_session_null_database_throws_ArgumentNullException()
        {
            IMongoDatabase db = null;
            var ex = Record.Exception(() => db.RunCommand(s_session, "{ ping: 1 }", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("database");
        }

        [Fact]
        public void RunCommand_with_session_null_session_throws_ArgumentNullException()
        {
            var mock = new Mock<IMongoDatabase>();
            var ex = Record.Exception(() => mock.Object.RunCommand((IClientSessionHandle)null, "{ ping: 1 }", null));
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("session");
        }

        // ---- Helpers ----

        private static IList<BsonDocument> GetStages<TInput, TOutput>(
            PipelineDefinition<TInput, TOutput> pipeline) =>
            ((BsonDocumentStagePipelineDefinition<TInput, TOutput>)pipeline).Documents;

        private static BsonDocument RenderCommand(Command<BsonDocument> command) =>
            command.Render(BsonSerializer.SerializerRegistry).Document;
    }
}
