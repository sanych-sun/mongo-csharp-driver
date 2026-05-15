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
    public class IFindFluentMqlTemplatingExtensionsTests
    {
        private static BsonDocument RenderProjection(ProjectionDefinition<BsonDocument, BsonDocument> projection) =>
            projection.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).Document;

        [Fact]
        public void Project_passes_correct_projection()
        {
            var mock = new Mock<IFindFluent<BsonDocument, BsonDocument>>();
            ProjectionDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(f => f.Project<BsonDocument>(It.IsAny<ProjectionDefinition<BsonDocument, BsonDocument>>()))
                .Callback<ProjectionDefinition<BsonDocument, BsonDocument>>(p => captured = p)
                .Returns(new Mock<IFindFluent<BsonDocument, BsonDocument>>().Object);

            mock.Object.Project("{ 'name': 1, 'age': 1, '_id': 0 }", null);

            var rendered = RenderProjection(captured);
            ((BsonValue)rendered).Should().Be(BsonDocument.Parse("{ 'name': 1, 'age': 1, '_id': 0 }"));
        }

        [Fact]
        public void Project_with_aggregation_expression_substitutes_parameter()
        {
            var mock = new Mock<IFindFluent<BsonDocument, BsonDocument>>();
            ProjectionDefinition<BsonDocument, BsonDocument> captured = null;
            mock.Setup(f => f.Project<BsonDocument>(It.IsAny<ProjectionDefinition<BsonDocument, BsonDocument>>()))
                .Callback<ProjectionDefinition<BsonDocument, BsonDocument>>(p => captured = p)
                .Returns(new Mock<IFindFluent<BsonDocument, BsonDocument>>().Object);

            mock.Object.Project("{ 'discounted': { '$multiply': ['$price', @factor] } }", new { factor = 0.9 });

            var rendered = RenderProjection(captured);
            ((BsonValue)rendered).Should().Be(BsonDocument.Parse("{ 'discounted': { '$multiply': ['$price', 0.9] } }"));
        }

        [Fact]
        public void Project_null_find_throws_ArgumentNullException()
        {
            IFindFluent<BsonDocument, BsonDocument> find = null;
            var ex = Record.Exception(() => { find.Project("{ 'x': 1 }", null); });
            ex.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("find");
        }

        [Fact]
        public void Project_propagates_FormatException_for_unknown_parameter()
        {
            var mock = new Mock<IFindFluent<BsonDocument, BsonDocument>>();
            var ex = Record.Exception(() => { mock.Object.Project("{ 'score': @missing }", new { other = 1 }); });
            ex.Should().BeOfType<FormatException>().Which.Message.Should().Contain("@missing");
        }
    }
}
