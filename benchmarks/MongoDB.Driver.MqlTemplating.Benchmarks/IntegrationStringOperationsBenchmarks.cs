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
using System.Globalization;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.MqlTemplating;

namespace MongoDB.Driver.MqlTemplating.Benchmarks;

/// <summary>
/// Integration benchmarks for pipelines that use aggregation string expressions:
/// $trim (filter and projection), $concat (projection), $replaceAll (projection).
///
/// Three approaches are compared:
///   1. Raw pre-parsed BsonDocument pipeline (baseline) — params baked in at setup;
///      represents the fastest possible raw-document approach (no per-call string formatting).
///   2. MQL templating — single pipeline template string with @parameter substitution.
///   3. MQL templating — fluent per-stage chain (.Match / .Project extension methods).
///
/// The pipeline executed by all three approaches is equivalent:
///   $match  — products whose Category (after $trim) equals a target string
///   $project — DisplayName ($concat of trimmed Name + separator + trimmed Category),
///              Slug ($replaceAll spaces with '-' in lowercased Name),
///              PriceWithTax ($multiply of Price by a tax rate)
///   $limit  — cap results
///
/// Run with: dotnet run -c Release -- --filter "*IntegrationStringOperations*"
/// Requires MongoDB at mongodb://localhost:27017 (override with MONGODB_URI env var).
/// </summary>
[IterationCount(50)]
[MemoryDiagnoser]
public class IntegrationStringOperationsBenchmarks
{
    private const string DatabaseName    = "mql_templating_benchmarks";
    private const string CollectionName  = "string_ops_products";
    private const int    DocumentCount   = 1_000;
    private const int    Iterations      = 1_000;
    private const int    ResultLimit     = 20;

    // Query parameters — fixed for all iterations.
    private const string TargetCategory  = "electronics";
    private const string DisplayNameSep  = " — ";
    private const double TaxRate         = 1.2;

    // Pre-parsed pipeline reused by the baseline benchmark.
    private static readonly BsonDocument[] __preParsedPipeline = BuildPreParsedPipeline();

    // MQL pipeline template (all three stages in one array literal).
    private const string PipelineTemplate =
        "[ " +
        "  { '$match': { '$expr': { '$eq': [{ '$trim': { 'input': '$Category' } }, @category] } } }, " +
        "  { '$project': { " +
        "      'DisplayName': { '$concat': [{ '$trim': { 'input': '$Name' } }, @sep, { '$trim': { 'input': '$Category' } }] }, " +
        "      'Slug': { '$replaceAll': { 'input': { '$toLower': { '$trim': { 'input': '$Name' } } }, 'find': ' ', 'replacement': '-' } }, " +
        "      'PriceWithTax': { '$multiply': ['$Price', @taxRate] } " +
        "  } }, " +
        "  { '$limit': @n } " +
        "]";

    // Per-stage templates used by the fluent benchmark.
    private const string MatchTemplate =
        "{ '$expr': { '$eq': [{ '$trim': { 'input': '$Category' } }, @category] } }";

    private const string ProjectTemplate =
        "{ 'DisplayName': { '$concat': [{ '$trim': { 'input': '$Name' } }, @sep, { '$trim': { 'input': '$Category' } }] }, " +
        "  'Slug': { '$replaceAll': { 'input': { '$toLower': { '$trim': { 'input': '$Name' } } }, 'find': ' ', 'replacement': '-' } }, " +
        "  'PriceWithTax': { '$multiply': ['$Price', @taxRate] } }";

    private IMongoClient _client = null!;
    private IMongoCollection<Product> _collection = null!;

    [GlobalSetup]
    public void Setup()
    {
        var uri = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";
        _client = new MongoClient(uri);

        var db = _client.GetDatabase(DatabaseName);
        db.DropCollection(CollectionName);
        _collection = db.GetCollection<Product>(CollectionName);
        _collection.InsertMany(GenerateProducts());

        Thread.Sleep(1000);
    }

    /// <summary>
    /// Baseline: pre-parsed BsonDocument pipeline with parameters baked in.
    /// Represents the best case for raw-document queries — no per-call string allocation or parsing.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Raw BsonDocument (pre-parsed, baked params)")]
    public void RawBsonDocument_PreParsed()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var pipeline = PipelineDefinition<Product, BsonDocument>.Create(__preParsedPipeline);
            _collection.Aggregate(pipeline).ToList();
        }
    }

    /// <summary>
    /// LINQ AsQueryable — Where + Select + Take with string expressions.
    /// MongoDB LINQ3 translates Trim() → $trim, ToLower() → $toLower,
    /// Replace() → $replaceAll, string concatenation → $concat, and * → $multiply.
    /// Returns a list of anonymous objects rather than BsonDocuments.
    /// </summary>
    [Benchmark(Description = "LINQ — Where + Select + Take (string expressions)")]
    public void Linq_StringOperations()
    {
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .AsQueryable()
                .Where(x => x.Category.Trim() == TargetCategory)
                .Select(x => new
                {
                    DisplayName  = x.Name.Trim() + DisplayNameSep + x.Category.Trim(),
                    Slug         = x.Name.Trim().ToLower().Replace(" ", "-"),
                    PriceWithTax = x.Price * TaxRate
                })
                .Take(ResultLimit)
                .ToList();
        }
    }

    /// <summary>
    /// MQL templating — single pipeline template.
    /// The full three-stage pipeline is expressed as one template string;
    /// @category, @sep, @taxRate, and @n are substituted per call.
    /// </summary>
    [Benchmark(Description = "MQL templating — single pipeline template")]
    public void MqlTemplating_Pipeline()
    {
        var parameters = new { category = TargetCategory, sep = DisplayNameSep, taxRate = TaxRate, n = ResultLimit };
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .Aggregate<Product, BsonDocument>(PipelineTemplate, parameters)
                .ToList();
        }
    }

    /// <summary>
    /// MQL templating — per-stage fluent chain.
    /// Each stage is expressed as a separate template; parameters are scoped per stage.
    /// </summary>
    [Benchmark(Description = "MQL templating — fluent per-stage")]
    public void MqlTemplating_FluentStages()
    {
        var matchParams   = new { category = TargetCategory };
        var projectParams = new { sep = DisplayNameSep, taxRate = TaxRate };
        for (var i = 0; i < Iterations; i++)
        {
            _collection
                .Aggregate()
                .Match(MatchTemplate, matchParams)
                .Project(ProjectTemplate, projectParams)
                .AppendStage("{ '$limit': @n }", new { n = ResultLimit })
                .ToList();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        //_client.GetDatabase(DatabaseName).DropCollection(CollectionName);
        _client.Dispose();
    }

    private static BsonDocument[] BuildPreParsedPipeline()
    {
        var taxStr = TaxRate.ToString("R", CultureInfo.InvariantCulture);
        return
        [
            BsonDocument.Parse(
                "{ '$match': { '$expr': { '$eq': [{ '$trim': { 'input': '$Category' } }, '" + TargetCategory + "'] } } }"),
            BsonDocument.Parse(
                "{ '$project': {" +
                "  'DisplayName': { '$concat': [{ '$trim': { 'input': '$Name' } }, '" + DisplayNameSep + "', { '$trim': { 'input': '$Category' } }] }," +
                "  'Slug': { '$replaceAll': { 'input': { '$toLower': { '$trim': { 'input': '$Name' } } }, 'find': ' ', 'replacement': '-' } }," +
                "  'PriceWithTax': { '$multiply': ['$Price', " + taxStr + "] }" +
                "} }"),
            BsonDocument.Parse("{ '$limit': " + ResultLimit + " }"),
        ];
    }

    private static IEnumerable<Product> GenerateProducts()
    {
        // Category stored with surrounding spaces — $trim in queries strips them.
        var categories = new[] { " electronics ", " clothing ", " books " };
        var names = new[]
        {
            "Laptop Pro", "Wireless Headset", "Smart Watch", "USB C Hub", "Mechanical Keyboard",
            "Cotton Shirt", "Denim Jacket", "Running Shoes", "Wool Sweater", "Linen Trousers",
            "C Sharp Guide", "Clean Code", "Design Patterns", "Domain Driven Design", "Refactoring"
        };
        var rng = new Random(42);
        return Enumerable.Range(1, DocumentCount).Select(i => new Product
        {
            Id       = i,
            // Name also padded so $trim is exercised in the projection.
            Name     = "  " + names[i % names.Length] + "  ",
            Category = categories[i % categories.Length],
            Price    = Math.Round(rng.NextDouble() * 490 + 10, 2)
        });
    }
}
