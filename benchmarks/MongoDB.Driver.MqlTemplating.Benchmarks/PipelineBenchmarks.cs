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

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.MqlTemplating;

namespace MongoDB.Driver.MqlTemplating.Benchmarks;

/// <summary>
/// Compares pipeline construction + rendering across three approaches:
///   - Raw BsonDocument array → PipelineDefinition.Create (baseline)
///   - PipelineDefinitionBuilder / EmptyPipelineDefinition fluent chain
///   - MQL templating (template string + parameter substitution)
///
/// Each method constructs a pipeline definition then renders all stages to BsonDocuments,
/// covering the full path that runs before an aggregate command is sent.
/// </summary>
[MemoryDiagnoser]
public class PipelineBenchmarks
{
    private static readonly RenderArgs<BsonDocument> s_renderArgs =
        new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);

    // ── Simple pipeline: [{ '$match': ... }, { '$limit': 10 }] ───────────────

    [Benchmark(Baseline = true, Description = "Simple — raw BsonDocument array")]
    public IList<BsonDocument> SimplePipeline_RawBsonDocument()
    {
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(new[]
        {
            BsonDocument.Parse("{ '$match': { 'status': 'active' } }"),
            BsonDocument.Parse("{ '$limit': 10 }")
        });
        return pipeline.Render(s_renderArgs).Documents;
    }

    [Benchmark(Description = "Simple — builder chain")]
    public IList<BsonDocument> SimplePipeline_BuilderChain()
    {
        var pipeline = new EmptyPipelineDefinition<BsonDocument>()
            .Match(Builders<BsonDocument>.Filter.Eq("status", "active"))
            .Limit(10);
        return pipeline.Render(s_renderArgs).Documents;
    }

    [Benchmark(Description = "Simple — MQL templating")]
    public IList<BsonDocument> SimplePipeline_MqlTemplating()
    {
        using var reader = new ExtendedJsonReader(
            "[{ '$match': { 'status': @s } }, { '$limit': @n }]",
            new { s = "active", n = 10 });
        var stages = BsonSerializer.Deserialize<BsonArray>(reader).Cast<BsonDocument>().ToArray();
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
        return pipeline.Render(s_renderArgs).Documents;
    }

    // ── Multi-stage pipeline: match + project + sort + limit ─────────────────

    [Benchmark(Description = "Multi-stage — raw BsonDocument array")]
    public IList<BsonDocument> MultiStagePipeline_RawBsonDocument()
    {
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(new[]
        {
            BsonDocument.Parse("{ '$match': { 'status': 'active', 'amount': { '$gte': 100.0 } } }"),
            BsonDocument.Parse("{ '$project': { 'status': 1, 'amount': 1 } }"),
            BsonDocument.Parse("{ '$sort': { 'amount': -1 } }"),
            BsonDocument.Parse("{ '$limit': 20 }")
        });
        return pipeline.Render(s_renderArgs).Documents;
    }

    [Benchmark(Description = "Multi-stage — builder chain")]
    public IList<BsonDocument> MultiStagePipeline_BuilderChain()
    {
        var pipeline = new EmptyPipelineDefinition<BsonDocument>()
            .Match(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("status", "active"),
                Builders<BsonDocument>.Filter.Gte("amount", 100.0)))
            .Project(Builders<BsonDocument>.Projection.Include("status").Include("amount"))
            .Sort(Builders<BsonDocument>.Sort.Descending("amount"))
            .Limit(20);
        return pipeline.Render(s_renderArgs).Documents;
    }

    [Benchmark(Description = "Multi-stage — MQL templating")]
    public IList<BsonDocument> MultiStagePipeline_MqlTemplating()
    {
        const string template =
            "[" +
            "{ '$match': { 'status': @s, 'amount': { '$gte': @min } } }," +
            "{ '$project': { 'status': 1, 'amount': 1 } }," +
            "{ '$sort': { 'amount': -1 } }," +
            "{ '$limit': @limit }" +
            "]";
        using var reader = new ExtendedJsonReader(template, new { s = "active", min = 100.0, limit = 20 });
        var stages = BsonSerializer.Deserialize<BsonArray>(reader).Cast<BsonDocument>().ToArray();
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
        return pipeline.Render(s_renderArgs).Documents;
    }
}
