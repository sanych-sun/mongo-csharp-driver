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
using FluentAssertions;
using Moq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.MqlTemplating.Tests
{
    public class IAggregateFluentMqlTemplatingExtensionsTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private static BsonDocument RenderFilter(FilterDefinition<BsonDocument> filter) =>
            filter.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));

        private static BsonDocument RenderStage(PipelineStageDefinition<BsonDocument, BsonDocument> stage) =>
            stage.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).Document;

        // ── Match ─────────────────────────────────────────────────────────────

        [Fact]
        public void Match_passes_correct_filter()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            FilterDefinition<BsonDocument> captured = null;
            mock.Setup(a => a.Match(It.IsAny<FilterDefinition<BsonDocument>>()))
                .Callback<FilterDefinition<BsonDocument>>(f => captured = f)
                .Returns(mock.Object);

            mock.Object.Match("{ 'status': @s }", new { s = "active" });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["status"]).Should().Be((BsonValue)"active");
        }

        [Fact]
        public void Match_substitutes_numeric_parameter()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            FilterDefinition<BsonDocument> captured = null;
            mock.Setup(a => a.Match(It.IsAny<FilterDefinition<BsonDocument>>()))
                .Callback<FilterDefinition<BsonDocument>>(f => captured = f)
                .Returns(mock.Object);

            mock.Object.Match("{ 'score': { '$gte': @min } }", new { min = 100 });

            var rendered = RenderFilter(captured);
            ((BsonValue)rendered["score"]["$gte"]).Should().Be((BsonValue)100);
        }

        [Fact]
        public void Match_null_aggregate_throws_ArgumentNullException()
        {
            IAggregateFluent<BsonDocument> aggregate = null;
            var ex = Record.Exception(() => { aggregate.Match("{ 'x': 1 }", null); });
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("aggregate");
        }

        [Fact]
        public void Match_propagates_FormatException_for_unknown_parameter()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            var ex = Record.Exception(() => { mock.Object.Match("{ 'x': @missing }", new { other = 1 }); });
            ex.Should().BeOfType<FormatException>().Which.Message.Should().Contain("@missing");
        }

        // ── Project ───────────────────────────────────────────────────────────

        private static BsonDocument RenderProjection(ProjectionDefinition<BsonDocument, BsonDocument> projection) =>
            projection.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).Document;

        [Fact]
        public void Project_passes_correct_projection()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            ProjectionDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(a => a.Project<BsonDocument>(It.IsAny<ProjectionDefinition<BsonDocument, BsonDocument>>()))
                .Callback<ProjectionDefinition<BsonDocument, BsonDocument>>(p => captured = p)
                .Returns(new Mock<IAggregateFluent<BsonDocument>>().Object);

            mock.Object.Project("{ 'name': 1, 'age': 1, '_id': 0 }", null);

            var rendered = RenderProjection(captured);
            ((BsonValue)rendered).Should().Be(BsonDocument.Parse("{ 'name': 1, 'age': 1, '_id': 0 }"));
        }

        [Fact]
        public void Project_with_aggregation_expression_substitutes_parameter()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            ProjectionDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(a => a.Project<BsonDocument>(It.IsAny<ProjectionDefinition<BsonDocument, BsonDocument>>()))
                .Callback<ProjectionDefinition<BsonDocument, BsonDocument>>(p => captured = p)
                .Returns(new Mock<IAggregateFluent<BsonDocument>>().Object);

            mock.Object.Project("{ 'score': { '$multiply': ['$base', @multiplier] } }", new { multiplier = 2 });

            var rendered = RenderProjection(captured);
            ((BsonValue)rendered).Should().Be(BsonDocument.Parse("{ 'score': { '$multiply': ['$base', 2] } }"));
        }

        [Fact]
        public void Project_null_aggregate_throws_ArgumentNullException()
        {
            IAggregateFluent<BsonDocument> aggregate = null;
            var ex = Record.Exception(() => { aggregate.Project("{ 'x': 1 }", null); });
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("aggregate");
        }

        [Fact]
        public void Project_propagates_FormatException_for_unknown_parameter()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            var ex = Record.Exception(() => { mock.Object.Project("{ 'score': { '$multiply': ['$base', @missing] } }", new { other = 1 }); });
            ex.Should().BeOfType<FormatException>().Which.Message.Should().Contain("@missing");
        }

        // ── AppendStage ───────────────────────────────────────────────────────

        [Fact]
        public void AppendStage_passes_correct_stage()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            PipelineStageDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(a => a.AppendStage(It.IsAny<PipelineStageDefinition<BsonDocument, BsonDocument>>()))
                .Callback<PipelineStageDefinition<BsonDocument, BsonDocument>>(s => captured = s)
                .Returns(new Mock<IAggregateFluent<BsonDocument>>().Object);

            mock.Object.AppendStage("{ '$limit': @n }", new { n = 10 });

            var rendered = RenderStage(captured);
            ((BsonValue)rendered).Should().Be(BsonDocument.Parse("{ '$limit': 10 }"));
        }

        [Fact]
        public void AppendStage_substitutes_string_parameter()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            PipelineStageDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(a => a.AppendStage(It.IsAny<PipelineStageDefinition<BsonDocument, BsonDocument>>()))
                .Callback<PipelineStageDefinition<BsonDocument, BsonDocument>>(s => captured = s)
                .Returns(new Mock<IAggregateFluent<BsonDocument>>().Object);

            mock.Object.AppendStage("{ '$match': { 'op': @op } }", new { op = "insert" });

            var rendered = RenderStage(captured);
            ((BsonValue)rendered).Should().Be(BsonDocument.Parse("{ '$match': { 'op': 'insert' } }"));
        }

        [Fact]
        public void AppendStage_null_aggregate_throws_ArgumentNullException()
        {
            IAggregateFluent<BsonDocument> aggregate = null;
            var ex = Record.Exception(() => { aggregate.AppendStage("{ '$limit': 1 }", null); });
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("aggregate");
        }

        [Fact]
        public void AppendStage_propagates_FormatException_for_unknown_parameter()
        {
            var mock = new Mock<IAggregateFluent<BsonDocument>>();
            var ex = Record.Exception(() => { mock.Object.AppendStage("{ '$match': { 'x': @missing } }", new { other = 1 }); });
            ex.Should().BeOfType<FormatException>().Which.Message.Should().Contain("@missing");
        }
    }
}
