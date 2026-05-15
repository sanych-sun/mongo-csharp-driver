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
using MongoDB.Driver;
using MongoDB.Driver.MqlTemplating;

namespace MongoDB.Driver.MqlTemplating.Benchmarks;

/// <summary>
/// Integration benchmarks for filter-based Find operations.
/// All three approaches execute a real Find command against a local MongoDB instance
/// and return List&lt;Order&gt;, making the comparison end-to-end and type-equivalent.
///
/// Run with: dotnet run -c Release -- --filter "*IntegrationFilter*"
/// Requires MongoDB at mongodb://localhost:27017 (override with MONGODB_URI env var).
/// </summary>
[IterationCount(20)]
[MemoryDiagnoser]
public class IntegrationFilterBenchmarks
{
    private const string DatabaseName = "mql_templating_benchmarks";
    private const string CollectionName = "filter_orders";
    private const int DocumentCount = 1_000;
    private const int Iterations = 1_000;

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

    /// <summary>Baseline: Builders API with typed expression field.</summary>
    [Benchmark(Baseline = true, Description = "Builders filter")]
    public void Builders_Find()
    {
        for(var i = 0; i < Iterations; i++)
        {
            _collection
                .Find(Builders<Order>.Filter.Eq("Status", "active"))
                .ToList();
        }
    }

    /// <summary>LINQ via collection.AsQueryable().</summary>
    [Benchmark(Description = "LINQ AsQueryable filter")]
    public void Linq_Find()
    {
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .AsQueryable()
                .Where(x => x.Status == "active")
                .ToList();
        }
    }

    /// <summary>MQL templating — template string with @placeholder substitution.</summary>
    [Benchmark(Description = "MQL templating filter")]
    public void MqlTemplating_Find()
    {
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .Find("{ 'Status': @s }", new { s = "active" })
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
