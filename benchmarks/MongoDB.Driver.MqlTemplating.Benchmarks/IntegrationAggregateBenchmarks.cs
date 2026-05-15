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
using System.Linq;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.MqlTemplating;

namespace MongoDB.Driver.MqlTemplating.Benchmarks;

/// <summary>
/// Integration benchmarks for aggregation pipelines.
/// All three approaches run a real aggregate command against a local MongoDB instance.
///
/// Note on output types: Builders and LINQ return List&lt;Order&gt;; MQL templating
/// returns List&lt;BsonDocument&gt; (inherent to the current API design — the pipeline
/// output type is fixed to BsonDocument). This reflects the real-world tradeoff.
///
/// Run with: dotnet run -c Release -- --filter "*IntegrationAggregate*"
/// Requires MongoDB at mongodb://localhost:27017 (override with MONGODB_URI env var).
/// </summary>
[IterationCount(100)]
[MemoryDiagnoser]
public class IntegrationAggregateBenchmarks
{
    private const string DatabaseName = "mql_templating_benchmarks";
    private const string CollectionName = "aggregate_orders";
    private const int DocumentCount = 1_000;
    private const int Iterations = 1_000;
    private const int ResultLimit = 1;

    private IMongoClient _client = null!;
    private IMongoCollection<Order> _collection = null!;

    [GlobalSetup]
    public void Setup()
    {
        var uri = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";
        _client = new MongoClient(uri);

        var db = _client.GetDatabase(DatabaseName);
        db.DropCollection(CollectionName);
        _collection = db.GetCollection<Order>(CollectionName);
        _collection.InsertMany(GenerateOrders());
    }

    /// <summary>Baseline: Builders fluent aggregate — $match + $limit.</summary>
    [Benchmark(Description = "Builders — $match + $limit")]
    public void Builders_MatchSortLimit()
    {
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .Aggregate<Order>()
                .Match(Builders<Order>.Filter.And(
                    Builders<Order>.Filter.Eq("Status", "active"),
                    Builders<Order>.Filter.Gte("Amount", 100.0)))
                .Limit(ResultLimit)
                .ToList();
        }
    }

    /// <summary>LINQ AsQueryable — Where + Take.</summary>
    [Benchmark(Description = "LINQ — Where + Take")]
    public void Linq_MatchSortLimit()
    {
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .AsQueryable()
                .Where(x => x.Status == "active" && x.Amount >= 100.0)
                .Take(ResultLimit)
                .ToList();
        }
    }

    /// <summary>MQL templating — multi-stage template. Returns BsonDocument.</summary>
    [Benchmark(Description = "MQL templating (single template) — $match + $limit")]
    public void MqlTemplating_MatchSortLimit()
    {
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .Aggregate<Order, BsonDocument>(
                    "[{ '$match': { 'Status': @s, 'Amount': { '$gte': @min } } }, { '$limit': @n }]",
                    new { s = "active", min = 100.0, n = ResultLimit })
                .ToList();
        }
    }

    /// <summary>MQL templating — multi-stage template. Returns BsonDocument.</summary>
    [Benchmark(Description = "MQL templating (multiple stages) — $match + $limit")]
    public void MqlTemplatingAppendStage_MatchSortLimit()
    {
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .Aggregate()
                .Match("{ 'Status': @s, 'Amount': { '$gte': @min } } }", new { s = "active", min = 100.0 })
                .AppendStage("{ '$limit': @n }", new { n = ResultLimit })
                .ToList();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client.GetDatabase(DatabaseName).DropCollection(CollectionName);
        _client.Dispose();
    }

    private static IEnumerable<Order> GenerateOrders()
    {
        var statuses = new[] { "active", "inactive", "pending" };
        var rng = new Random(42);
        return Enumerable.Range(1, DocumentCount).Select(i => new Order
        {
            Id = i,
            Status = statuses[i % statuses.Length],
            Amount = Math.Round(rng.NextDouble() * 1_000, 2)
        });
    }
}
