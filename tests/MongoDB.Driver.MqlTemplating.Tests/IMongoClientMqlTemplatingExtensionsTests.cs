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
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.MqlTemplating.Tests
{
    public class IMongoClientMqlTemplatingExtensionsTests
    {
        private const string MatchTemplate = "[{ '$match': { 'fullDocument.userId': @userId } }]";

        // Watch (no session) — happy path

        [Fact]
        public void Watch_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoClient>();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            mock.Setup(c => c.Watch(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null);

            mock.Object.Watch(MatchTemplate, new { userId = 42 });

            mock.Verify(c => c.Watch(
                It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                null,
                default), Times.Once);
            var stages = RenderStages(captured);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'fullDocument.userId': 42 } }"));
        }

        [Fact]
        public void Watch_passes_options_and_cancellation_token_through()
        {
            var mock = new Mock<IMongoClient>();
            var options = new ChangeStreamOptions { MaxAwaitTime = TimeSpan.FromSeconds(5) };
            var cts = new CancellationTokenSource();
            mock.Setup(c => c.Watch(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    options,
                    cts.Token))
                .Returns((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null);

            mock.Object.Watch("[]", null, options, cts.Token);

            mock.Verify(c => c.Watch(
                It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                options,
                cts.Token), Times.Once);
        }

        [Fact]
        public void Watch_null_client_throws_ArgumentNullException()
        {
            IMongoClient client = null;
            var ex = Record.Exception(() => client.Watch(MatchTemplate, new { userId = 1 }));
            ex.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("client");
        }

        // Watch (with session) — happy path

        [Fact]
        public void Watch_with_session_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoClient>();
            var session = new Mock<IClientSessionHandle>().Object;
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            mock.Setup(c => c.Watch(
                    session,
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IClientSessionHandle, PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (_, p, __, ___) => captured = p)
                .Returns((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null);

            mock.Object.Watch(session, MatchTemplate, new { userId = 99 });

            var stages = RenderStages(captured);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'fullDocument.userId': 99 } }"));
        }

        [Fact]
        public void Watch_with_session_null_client_throws_ArgumentNullException()
        {
            IMongoClient client = null;
            var session = new Mock<IClientSessionHandle>().Object;
            var ex = Record.Exception(() => client.Watch(session, MatchTemplate, new { userId = 1 }));
            ex.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("client");
        }

        [Fact]
        public void Watch_with_session_null_session_throws_ArgumentNullException()
        {
            var mock = new Mock<IMongoClient>();
            var ex = Record.Exception(() => mock.Object.Watch((IClientSessionHandle)null, MatchTemplate, new { userId = 1 }));
            ex.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("session");
        }

        // WatchAsync (no session) — happy path

        [Fact]
        public async Task WatchAsync_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoClient>();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            mock.Setup(c => c.WatchAsync(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns(Task.FromResult((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null));

            await mock.Object.WatchAsync(MatchTemplate, new { userId = 7 });

            var stages = RenderStages(captured);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'fullDocument.userId': 7 } }"));
        }

        [Fact]
        public void WatchAsync_null_client_throws_ArgumentNullException()
        {
            IMongoClient client = null;
            var ex = Record.Exception(() => { client.WatchAsync(MatchTemplate, new { userId = 1 }); });
            ex.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("client");
        }

        // WatchAsync (with session) — happy path

        [Fact]
        public async Task WatchAsync_with_session_builds_pipeline_from_template_and_parameters()
        {
            var mock = new Mock<IMongoClient>();
            var session = new Mock<IClientSessionHandle>().Object;
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            mock.Setup(c => c.WatchAsync(
                    session,
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IClientSessionHandle, PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (_, p, __, ___) => captured = p)
                .Returns(Task.FromResult((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null));

            await mock.Object.WatchAsync(session, MatchTemplate, new { userId = 55 });

            var stages = RenderStages(captured);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'fullDocument.userId': 55 } }"));
        }

        [Fact]
        public void WatchAsync_with_session_null_client_throws_ArgumentNullException()
        {
            IMongoClient client = null;
            var session = new Mock<IClientSessionHandle>().Object;
            var ex = Record.Exception(() => { client.WatchAsync(session, MatchTemplate, new { userId = 1 }); });
            ex.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("client");
        }

        [Fact]
        public void WatchAsync_with_session_null_session_throws_ArgumentNullException()
        {
            var mock = new Mock<IMongoClient>();
            var ex = Record.Exception(() => { mock.Object.WatchAsync((IClientSessionHandle)null, MatchTemplate, new { userId = 1 }); });
            ex.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("session");
        }

        // Error propagation

        [Fact]
        public void Watch_propagates_FormatException_for_unknown_parameter()
        {
            var mock = new Mock<IMongoClient>();
            var ex = Record.Exception(() => mock.Object.Watch("[{ '$match': { 'x': @missing } }]", new { other = 1 }));
            ex.Should().BeOfType<FormatException>().Which.Message.Should().Contain("@missing");
        }

        [Fact]
        public void Watch_propagates_exception_for_malformed_template()
        {
            var mock = new Mock<IMongoClient>();
            Record.Exception(() => mock.Object.Watch("not valid json", null)).Should().NotBeNull();
        }

        // Multi-stage pipeline

        [Fact]
        public void Watch_builds_multi_stage_pipeline()
        {
            var mock = new Mock<IMongoClient>();
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> captured = null;
            mock.Setup(c => c.Watch(
                    It.IsAny<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
                    It.IsAny<ChangeStreamOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>, ChangeStreamOptions, CancellationToken>(
                    (p, _, __) => captured = p)
                .Returns((IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)null);

            var template = "[{ '$match': { 'operationType': @op } }, { '$project': { 'fullDocument': 1 } }]";
            mock.Object.Watch(template, new { op = "insert" });

            var stages = RenderStages(captured);
            stages.Should().HaveCount(2);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'operationType': 'insert' } }"));
            ((BsonValue)stages[1]).Should().Be(BsonDocument.Parse("{ '$project': { 'fullDocument': 1 } }"));
        }

        private static IList<BsonDocument> RenderStages(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline)
        {
            return ((BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>)pipeline).Documents;
        }
    }
}
