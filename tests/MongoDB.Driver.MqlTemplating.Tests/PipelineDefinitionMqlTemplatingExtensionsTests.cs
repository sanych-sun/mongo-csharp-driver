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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.MqlTemplating.Tests
{
    public class PipelineDefinitionMqlTemplatingExtensionsTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private static IList<BsonDocument> RenderStages(PipelineDefinition<BsonDocument, BsonDocument> pipeline) =>
            pipeline.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).Documents;

        private static PipelineDefinition<BsonDocument, BsonDocument> EmptyPipeline() =>
            PipelineDefinition<BsonDocument, BsonDocument>.Create(System.Array.Empty<BsonDocument>());

        // ── happy path ───────────────────────────────────────────────────────

        [Fact]
        public void AppendStage_appends_stage_to_empty_pipeline()
        {
            var pipeline = EmptyPipeline()
                .AppendStage("{ '$match': { 'status': @status } }", new { status = "active" });

            var stages = RenderStages(pipeline);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'status': 'active' } }"));
        }

        [Fact]
        public void AppendStage_appends_stage_to_existing_pipeline()
        {
            var initial = PipelineDefinition<BsonDocument, BsonDocument>.Create(new[]
            {
                BsonDocument.Parse("{ '$match': { 'x': 1 } }")
            });

            var pipeline = initial.AppendStage("{ '$project': { 'name': @include } }", new { include = 1 });

            var stages = RenderStages(pipeline);
            stages.Should().HaveCount(2);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$match': { 'x': 1 } }"));
            ((BsonValue)stages[1]).Should().Be(BsonDocument.Parse("{ '$project': { 'name': 1 } }"));
        }

        [Fact]
        public void AppendStage_substitutes_numeric_parameter()
        {
            var pipeline = EmptyPipeline()
                .AppendStage("{ '$limit': @n }", new { n = 10 });

            var stages = RenderStages(pipeline);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$limit': 10 }"));
        }

        [Fact]
        public void AppendStage_null_parameters_accepted()
        {
            var pipeline = EmptyPipeline()
                .AppendStage("{ '$limit': 5 }", null);

            var stages = RenderStages(pipeline);
            stages.Should().HaveCount(1);
            ((BsonValue)stages[0]).Should().Be(BsonDocument.Parse("{ '$limit': 5 }"));
        }

        // ── null guard ───────────────────────────────────────────────────────

        [Fact]
        public void AppendStage_null_pipeline_throws_ArgumentNullException()
        {
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = null;
            var ex = Record.Exception(() => { pipeline.AppendStage("{ '$limit': 1 }", null); });
            ex.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("pipeline");
        }

        // ── error propagation ────────────────────────────────────────────────

        [Fact]
        public void AppendStage_propagates_FormatException_for_unknown_parameter()
        {
            var ex = Record.Exception(() =>
            {
                EmptyPipeline().AppendStage("{ '$match': { 'x': @missing } }", new { other = 1 });
            });
            ex.Should().BeOfType<FormatException>().Which.Message.Should().Contain("@missing");
        }

        [Fact]
        public void AppendStage_propagates_exception_for_malformed_template()
        {
            Record.Exception(() => { EmptyPipeline().AppendStage("not valid json", null); })
                .Should().NotBeNull();
        }
    }
}
