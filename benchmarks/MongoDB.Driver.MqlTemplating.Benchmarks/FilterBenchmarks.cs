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

using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.MqlTemplating;

namespace MongoDB.Driver.MqlTemplating.Benchmarks;

/// <summary>
/// Compares filter construction + rendering across three approaches:
///   - Raw BsonDocument (baseline, no abstraction overhead)
///   - Builders API string-based and expression-based (LINQ-style)
///   - MQL templating (template string + parameter substitution)
///
/// Each method constructs a filter definition then renders it to a BsonDocument,
/// covering the full path that runs before any server command is issued.
/// </summary>
[MemoryDiagnoser]
public class FilterBenchmarks
{
    private static readonly RenderArgs<BsonDocument> s_bsonRenderArgs =
        new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry);

    private RenderArgs<Order> _orderRenderArgs;

    [GlobalSetup]
    public void Setup()
    {
        // Warm the serializer registry so first-call overhead doesn't skew results.
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Order>();
        _orderRenderArgs = new((IBsonSerializer<Order>)serializer, BsonSerializer.SerializerRegistry);
    }

    // ── Simple filter: { 'status': 'active' } ────────────────────────────────

    [Benchmark(Baseline = true, Description = "Simple — raw BsonDocument")]
    public BsonDocument SimpleFilter_RawBsonDocument()
    {
        FilterDefinition<BsonDocument> filter = new BsonDocument("status", "active");
        return filter.Render(s_bsonRenderArgs);
    }

    [Benchmark(Description = "Simple — Builders string field")]
    public BsonDocument SimpleFilter_BuildersString()
    {
        var filter = Builders<BsonDocument>.Filter.Eq("status", "active");
        return filter.Render(s_bsonRenderArgs);
    }

    [Benchmark(Description = "Simple — Builders expression (LINQ-style)")]
    public BsonDocument SimpleFilter_BuildersExpression()
    {
        var filter = Builders<Order>.Filter.Eq(x => x.Status, "active");
        return filter.Render(_orderRenderArgs);
    }

    [Benchmark(Description = "Simple — MQL templating")]
    public BsonDocument SimpleFilter_MqlTemplating()
    {
        using var reader = new ExtendedJsonReader("{ 'status': @s }", new { s = "active" });
        FilterDefinition<BsonDocument> filter = BsonSerializer.Deserialize<BsonDocument>(reader);
        return filter.Render(s_bsonRenderArgs);
    }

    // ── Compound filter: { 'status': 'active', 'amount': { '$gte': 100.0 } } ──

    [Benchmark(Description = "Compound — raw BsonDocument")]
    public BsonDocument CompoundFilter_RawBsonDocument()
    {
        FilterDefinition<BsonDocument> filter = new BsonDocument
        {
            { "status", "active" },
            { "amount", new BsonDocument("$gte", 100.0) }
        };
        return filter.Render(s_bsonRenderArgs);
    }

    [Benchmark(Description = "Compound — Builders string field")]
    public BsonDocument CompoundFilter_BuildersString()
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("status", "active"),
            Builders<BsonDocument>.Filter.Gte("amount", 100.0));
        return filter.Render(s_bsonRenderArgs);
    }

    [Benchmark(Description = "Compound — Builders expression (LINQ-style)")]
    public BsonDocument CompoundFilter_BuildersExpression()
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(x => x.Status, "active"),
            Builders<Order>.Filter.Gte(x => x.Amount, 100.0));
        return filter.Render(_orderRenderArgs);
    }

    [Benchmark(Description = "Compound — MQL templating")]
    public BsonDocument CompoundFilter_MqlTemplating()
    {
        using var reader = new ExtendedJsonReader(
            "{ 'status': @s, 'amount': { '$gte': @min } }",
            new { s = "active", min = 100.0 });
        FilterDefinition<BsonDocument> filter = BsonSerializer.Deserialize<BsonDocument>(reader);
        return filter.Render(s_bsonRenderArgs);
    }
}
