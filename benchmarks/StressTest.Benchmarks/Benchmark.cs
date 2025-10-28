using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace StressTest.Benchmarks;

[SimpleJob(runtimeMoniker: RuntimeMoniker.Net60)]
public class Benchmark
{
    private IMongoCollection<Planet> _collection;

    public Benchmark()
    {
        var connection = "mongodb://localhost";
        var settings = MongoClientSettings.FromConnectionString(connection);
        settings.MinConnectionPoolSize = 100;

        var mongoClient = new MongoClient(settings);
        _collection = mongoClient.GetDatabase("sample_guides").GetCollection<Planet>("planets");
    }

    [Benchmark(Baseline = true)]
    public void DoBenchmark()
    {
        for (var i = 0; i < 1000; i++)
        {
            using var cursor = _collection.FindSync(Builders<Planet>.Filter.Empty, new FindOptions<Planet, Planet>() { BatchSize = 5 });
            var results = cursor.ToList();
        }
    }
}

public class Planet
{
    public ObjectId _id { get; set; }
    public string name { get; set; }
    public int orderFromSun { get; set; }
    public bool hasRings { get; set; }
    public string[] mainAtmosphere { get; set; }
    public TempData surfaceTemperatureC { get; set; }
}

public class TempData
{
    public decimal? min { get; set; }
    public decimal? max { get; set; }
    public decimal? mean { get; set; }
}
