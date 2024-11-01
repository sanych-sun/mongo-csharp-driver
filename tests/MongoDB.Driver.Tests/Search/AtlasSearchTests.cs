﻿/* Copyright 2010-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Search;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;
using Builders = MongoDB.Driver.Builders<MongoDB.Driver.Tests.Search.AtlasSearchTests.HistoricalDocument>;
using GeoBuilders = MongoDB.Driver.Builders<MongoDB.Driver.Tests.Search.AtlasSearchTests.AirbnbListing>;

namespace MongoDB.Driver.Tests.Search
{
    [Trait("Category", "AtlasSearch")]
    public class AtlasSearchTests : LoggableTestClass
    {
        #region static

        private static readonly GeoJsonPolygon<GeoJson2DGeographicCoordinates> __testPolygon =
            new(new(new(new GeoJson2DGeographicCoordinates[]
            {
                new(-8.6131, 41.14),
                new(-8.6131, 41.145),
                new(-8.60308, 41.145),
                new(-8.60308, 41.14),
                new(-8.6131, 41.14),
            })));

        private static readonly GeoWithinBox<GeoJson2DGeographicCoordinates> __testBox =
            new(new(new(-8.6131, 41.14)), new(new(-8.60308, 41.145)));

        private static readonly GeoWithinCircle<GeoJson2DGeographicCoordinates> __testCircle =
            new(new(new(-8.61308, 41.1413)), 273);

        #endregion

        private readonly IMongoClient _mongoClient;

        public AtlasSearchTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_SEARCH_TESTS_ENABLED");

            var atlasSearchUri = Environment.GetEnvironmentVariable("ATLAS_SEARCH");
            Ensure.IsNotNullOrEmpty(atlasSearchUri, nameof(atlasSearchUri));

            _mongoClient = new MongoClient(atlasSearchUri);
        }

        protected override void DisposeInternal() => _mongoClient.Dispose();

        [Fact]
        public void Autocomplete()
        {
            var result = SearchSingle(Builders.Search.Autocomplete(x => x.Title, "Declaration of Ind"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Compound()
        {
            const int score = 42;
            var searchDefinition = Builders.Search.Compound(Builders.SearchScore.Constant(score))
                .Must(Builders.Search.Text(x => x.Body, "life"), Builders.Search.Text(x => x.Body, "liberty"))
                .MustNot(Builders.Search.Text(x => x.Body, "property"))
                .Must(Builders.Search.Text(x => x.Body, "pursuit of happiness"));

            var projectionDefinition = Builders.Projection
                .Include(x => x.Body)
                .Include(x => x.Title)
                .MetaSearchScore(x => x.Score);

            var result = SearchSingle(searchDefinition, projectionDefinition);
            result.Title.Should().Be("Declaration of Independence");
            result.Score.Should().Be(score);
        }

        [Fact]
        public void Count_total()
        {
            var results = GetTestCollection().Aggregate()
                .Search(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    count: new SearchCountOptions()
                    {
                        Type = SearchCountType.Total
                    })
                .Project<HistoricalDocument>(Builders.Projection.SearchMeta(x => x.MetaResult))
                .Limit(1)
                .ToList();
            results.Should().ContainSingle().Which.MetaResult.Count.Total.Should().Be(108);
        }

        [Fact]
        public void EmbeddedDocument()
        {
            var builderHistoricalDocument = Builders<HistoricalDocumentWithCommentsOnly>.Search;
            var builderComments = Builders<Comment>.Search;

            var result = GetTestCollection< HistoricalDocumentWithCommentsOnly>()
                .Aggregate()
                .Search(builderHistoricalDocument.EmbeddedDocument(
                    p => p.Comments,
                    builderComments.Text(p => p.Author, "Corliss Zuk")))
                .Limit(10)
                .ToList();

            foreach (var document in result)
            {
                document.Comments.Should().Contain(c => c.Author == "Corliss Zuk");
            }
        }

        [Fact]
        public void Exists()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Must(
                    Builders.Search.Text(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    Builders.Search.Exists(x => x.Title)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Filter()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Filter(
                    Builders.Search.Phrase(x => x.Body, "life, liberty"),
                    Builders.Search.Wildcard(x => x.Body, "happ*", true)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Theory]
        [InlineData("add")]
        [InlineData("constant")]
        [InlineData("gauss")]
        [InlineData("log")]
        [InlineData("log1p")]
        [InlineData("multiply")]
        [InlineData("path")]
        [InlineData("relevance")]
        public void FunctionScore(string functionScoreType)
        {
            var scoreFunction = functionScoreType switch
            {
                "add" => Builders.SearchScoreFunction.Add(Constant(1), Constant(2)),
                "constant" => Constant(1),
                "gauss" => Builders.SearchScoreFunction.Gauss(x => x.Score, 100, 1, 0.1, 1),
                "log" => Builders.SearchScoreFunction.Log(Constant(1)),
                "log1p" => Builders.SearchScoreFunction.Log1p(Constant(1)),
                "multiply" => Builders.SearchScoreFunction.Multiply(Constant(1), Constant(2)),
                "path" => Builders.SearchScoreFunction.Path(x => x.Score, 1),
                "relevance" => Builders.SearchScoreFunction.Relevance(),
                _ => throw new ArgumentOutOfRangeException(nameof(functionScoreType), functionScoreType, "Invalid score function")
            };

            var result = SearchSingle(Builders.Search.Phrase(
                x => x.Body,
                "life, liberty, and the pursuit of happiness",
                score: Builders.SearchScore.Function(scoreFunction)));

            result.Title.Should().Be("Declaration of Independence");

            SearchScoreFunction<HistoricalDocument> Constant(double value) =>
                Builders.SearchScoreFunction.Constant(value);
        }

        [Fact]
        public void GeoShape()
        {
            var results = GeoSearch(
                GeoBuilders.Search.GeoShape(
                    x => x.Address.Location,
                    GeoShapeRelation.Intersects,
                    __testPolygon));

            results.Count.Should().Be(25);
            results.First().Name.Should().Be("Ribeira Charming Duplex");
        }

        [Theory]
        [InlineData("box")]
        [InlineData("circle")]
        [InlineData("polygon")]
        public void GeoWithin(string geometryType)
        {
            GeoWithinArea<GeoJson2DGeographicCoordinates> geoArea = geometryType switch
            {
                "box" => __testBox,
                "circle" => __testCircle,
                "polygon" => new GeoWithinGeometry<GeoJson2DGeographicCoordinates>(__testPolygon),
                _ => throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, "Invalid geometry type")
            };

            var results = GeoSearch(GeoBuilders.Search.GeoWithin(x => x.Address.Location, geoArea));

            results.Count.Should().Be(25);
            results.First().Name.Should().Be("Ribeira Charming Duplex");
        }

        [Fact]
        public void In()
        {
            var results = GetSynonymTestCollection()
                .Aggregate()
                .Search(
                    Builders<Movie>.Search.In(x => x.Runtime, new[] { 31, 231 }),
                    new() { Sort = Builders<Movie>.Sort.Descending(x => x.Runtime)})
                .Limit(10)
                .ToList();

            results.Count.Should().Be(2);
            results[0].Runtime.Should().Be(231);
            results[1].Runtime.Should().Be(31);
        }

        [Fact]
        public void MoreLikeThis()
        {
            var likeThisDocument = new HistoricalDocument
            {
                Title = "Declaration of Independence",
                Body = "We hold these truths to be self-evident that all men are created equal..."
            };
            var result = SearchSingle(Builders.Search.MoreLikeThis(likeThisDocument));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Must()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Must(
                    Builders.Search.Phrase(x => x.Body, "life, liberty"),
                    Builders.Search.Wildcard(x => x.Body, "happ*", true)));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void MustNot()
        {
            var result = SearchSingle(
                Builders.Search.Compound().MustNot(
                    Builders.Search.Phrase(x => x.Body, "life, liberty")),
                sort: Builders.Sort.Descending(x => x.Title));
            result.Title.Should().Be("US Constitution");
        }

        [Fact]
        public void Near()
        {
            var results = GetGeoTestCollection().Aggregate()
                .Search(GeoBuilders.Search.Near(x => x.Address.Location, __testCircle.Center, 1000))
                .Limit(1)
                .ToList();

            results.Should().ContainSingle().Which.Name.Should().Be("Ribeira Charming Duplex");
        }

        [Fact]
        public void Phrase()
        {
            // This test case exercises the indexName and returnStoredSource arguments. The
            // remaining test cases omit them.
            var coll = GetTestCollection();
            var results = GetTestCollection().Aggregate()
                .Search(Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    new SearchHighlightOptions<HistoricalDocument>(x => x.Body),
                    indexName: "default",
                    returnStoredSource: true,
                    scoreDetails: true)
                .Limit(1)
                .Project<HistoricalDocument>(Builders.Projection
                    .Include(x => x.Title)
                    .Include(x => x.Body)
                    .MetaSearchScore(x => x.Score)
                    .MetaSearchHighlights(x => x.Highlights)
                    .MetaSearchScoreDetails(x => x.ScoreDetails))
                .ToList();

            var result = results.Should().ContainSingle().Subject;
            result.Title.Should().Be("Declaration of Independence");
            result.Score.Should().NotBe(0);

            var highlightTexts = result.Highlights.Should().ContainSingle().Subject.Texts;
            highlightTexts.Should().HaveCount(15);

            foreach (var highlight in highlightTexts)
            {
                var expectedType = char.IsLetter(highlight.Value[0]) ? HighlightTextType.Hit : HighlightTextType.Text;
                highlight.Type.Should().Be(expectedType);
            }

            var highlightRangeStr = string.Join(string.Empty, highlightTexts.Skip(1).Select(x => x.Value));
            highlightRangeStr.Should().Be("Life, Liberty and the pursuit of Happiness.");

            result.ScoreDetails.Description.Should().Contain("life liberty and the pursuit of happiness");
            result.ScoreDetails.Value.Should().NotBe(0);

            var scoreDetail = result.ScoreDetails.Details.Should().ContainSingle().Subject;
            scoreDetail.Description.Should().NotBeNullOrEmpty();
            scoreDetail.Value.Should().NotBe(0);
            scoreDetail.Details.Should().NotBeEmpty();
        }

        [Fact]
        public void PhraseMultiPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    Builders.SearchPath.Multi(x => x.Title, x => x.Body),
                    "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void PhraseAnalyzerPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    Builders.SearchPath.Analyzer(x => x.Body, "english"),
                    "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void PhraseWildcardPath()
        {
            var result = SearchSingle(
                Builders.Search.Phrase(
                    Builders.SearchPath.Wildcard("b*"),
                    "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void QueryString()
        {
            var result = SearchSingle(Builders.Search.QueryString(x => x.Body, "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Range()
        {
            var results = GeoSearch(
                GeoBuilders.Search.Compound().Must(
                    GeoBuilders.Search.Range(x => x.Bedrooms, SearchRangeBuilder.Gt(2).Lt(4)),
                    GeoBuilders.Search.Range(x => x.Beds, SearchRangeBuilder.Gte(14).Lte(14))));

            results.Should().ContainSingle().Which.Name.Should().Be("House close to station & direct to opera house....");
        }

        [Fact]
        public void Search_count_lowerBound()
        {
            var results = GetTestCollection().Aggregate()
                .Search(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    count: new SearchCountOptions()
                    {
                        Type = SearchCountType.LowerBound,
                        Threshold = 128
                    })
                .Project<HistoricalDocument>(Builders.Projection.SearchMeta(x => x.MetaResult))
                .Limit(1)
                .ToList();
            results.Should().ContainSingle().Which.MetaResult.Count.LowerBound.Should().Be(108);
        }

        [Fact]
        public void SearchMeta_count()
        {
            var result = GetTestCollection().Aggregate()
                .SearchMeta(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    "default",
                    new SearchCountOptions() { Type = SearchCountType.Total })
                .Single();

            result.Should().NotBeNull();
            result.Count.Should().NotBeNull();
            result.Count.Total.Should().Be(108);
        }

        [Fact]
        public void SearchMeta_facet()
        {
            var result = GetTestCollection().Aggregate()
                .SearchMeta(Builders.Search.Facet(
                    Builders.Search.Phrase(x => x.Body, "life, liberty, and the pursuit of happiness"),
                    Builders.SearchFacet.String("string", x => x.Author, 100),
                    Builders.SearchFacet.Number("number", x => x.Index, 0, 100),
                    Builders.SearchFacet.Date("date", x => x.Date, DateTime.MinValue, DateTime.MaxValue)))
                .Single();

            result.Should().NotBeNull();

            var bucket = result.Facet["string"].Buckets.Should().NotBeNull().And.ContainSingle().Subject;
            bucket.Id.Should().Be((BsonString)"machine");
            bucket.Count.Should().Be(108);

            bucket = result.Facet["number"].Buckets.Should().NotBeNull().And.ContainSingle().Subject;
            bucket.Id.Should().Be((BsonInt32)0);
            bucket.Count.Should().Be(0);

            bucket = result.Facet["date"].Buckets.Should().NotBeNull().And.ContainSingle().Subject;
            bucket.Id.Should().Be((BsonDateTime)DateTime.MinValue);
            bucket.Count.Should().Be(108);
        }

        [Fact]
        public void Should()
        {
            var result = SearchSingle(
                Builders.Search.Compound().Should(
                    Builders.Search.Phrase(x => x.Body, "life, liberty"),
                    Builders.Search.Wildcard(x => x.Body, "happ*", true))
                .MinimumShouldMatch(2));
            result.Title.Should().Be("Declaration of Independence");
        }

        [Fact]
        public void Sort()
        {
            var result = SearchSingle(
                Builders.Search.Text(x => x.Body, "liberty"),
                Builders.Projection.Include(x => x.Title),
                Builders.Sort.Descending(x => x.Title));

            result.Title.Should().Be("US Constitution");
        }

        [Fact]
        public void Sort_MetaSearchScore()
        {
            var results = GetSynonymTestCollection().Aggregate()
                .Search(
                    Builders<Movie>.Search.QueryString(x => x.Title, "dance"),
                    new() { Sort = Builders<Movie>.Sort.MetaSearchScoreAscending() })
                .Project<Movie>(Builders<Movie>.Projection
                    .Include(x => x.Title)
                    .MetaSearchScore(x => x.Score))
                .Limit(10)
                .ToList();
            results.First().Title.Should().Be("Invitation to the Dance");
            results.Should().BeInAscendingOrder(m => m.Score);
        }

        [Theory]
        [InlineData("first")]
        [InlineData("near")]
        [InlineData("or")]
        [InlineData("subtract")]
        public void Span(string spanType)
        {
            var spanDefinition = spanType switch
            {
                "first" => Builders.SearchSpan.First(Term("happiness"), 250),
                "near" => Builders.SearchSpan.Near(new[] { Term("life"), Term("liberty"), Term("pursuit"), Term("happiness") }, 3, true),
                "or" => Builders.SearchSpan.Or(Term("unalienable"), Term("inalienable")),
                "subtract" => Builders.SearchSpan.Subtract(Term("unalienable"), Term("inalienable")),
                _ => throw new ArgumentOutOfRangeException(nameof(spanType), spanType, "Invalid span type")
            };

            var result = SearchSingle(Builders.Search.Span(spanDefinition));
            result.Title.Should().Be("Declaration of Independence");

            SearchSpanDefinition<HistoricalDocument> Term(string term) => Builders.SearchSpan.Term(x => x.Body, term);
        }

        [Fact]
        public void Text()
        {
            var result = SearchSingle(Builders.Search.Text(x => x.Body, "life, liberty, and the pursuit of happiness"));

            result.Title.Should().Be("Declaration of Independence");
        }

        [Theory]
        [InlineData("automobile", "transportSynonyms", "Blue Car")]
        [InlineData("boat", "transportSynonyms", "And the Ship Sails On")]
        public void Synonyms(string query, string synonym, string expected)
        {
            var sortDefinition = Builders<Movie>.Sort.Ascending(x => x.Title);
            var result =
                GetSynonymTestCollection().Aggregate()
                    .Search(Builders<Movie>.Search.Text(x => x.Title, query, synonym), indexName: "synonyms-tests")
                    .Sort(sortDefinition)
                    .Project<Movie>(Builders<Movie>.Projection.Include("Title").Exclude("_id"))
                    .Limit(1)
                    .Single();

            result.Title.Should().Be(expected);
        }

        [Fact]
        public void SynonymsMappings()
        {
            var automobileAndAttireSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "automobile", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "attire", "attireSynonyms"));

            var vehicleAndDressSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "vehicle", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "dress", "attireSynonyms"));

            var boatAndHatSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "boat", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "hat", "attireSynonyms"));

            var vesselAndFedoraSearchResults = SearchMultipleSynonymMapping(
                Builders<Movie>.Search.Text(x => x.Title, "vessel", "transportSynonyms"),
                Builders<Movie>.Search.Text(x => x.Title, "fedora", "attireSynonyms"));

            automobileAndAttireSearchResults.Should().NotBeNull();
            vehicleAndDressSearchResults.Should().NotBeNull();
            boatAndHatSearchResults.Should().NotBeNull();
            vesselAndFedoraSearchResults.Should().NotBeNull();

            automobileAndAttireSearchResults.Should().BeEquivalentTo(vehicleAndDressSearchResults);
            boatAndHatSearchResults.Should().NotBeEquivalentTo(vesselAndFedoraSearchResults);
        }

        [Fact]
        public void VectorSearch()
        {
            var expectedTitles = new[]
            {
                "Willy Wonka & the Chocolate Factory",
                "Pinocchio",
                "Time Bandits",
                "Harry Potter and the Sorcerer's Stone",
                "The Witches"
            };

            var vector = new[] { -0.0072121937, -0.030757688, 0.014948666, -0.018497631, -0.019035352, 0.028149737, -0.0019593239, -0.02012424, -0.025649332, -0.007985169, 0.007830574, 0.023726976, -0.011507247, -0.022839734, 0.00027999343, -0.010431803, 0.03823202, -0.025756875, -0.02074262, -0.0042883316, -0.010841816, 0.010552791, 0.0015266258, -0.01791958, 0.018430416, -0.013980767, 0.017247427, -0.010525905, 0.0126230195, 0.009255537, 0.017153326, 0.008260751, -0.0036060968, -0.019210111, -0.0133287795, -0.011890373, -0.0030599732, -0.0002904958, -0.001310697, -0.020715732, 0.020890493, 0.012428096, 0.0015837587, -0.006644225, -0.028499257, -0.005098275, -0.0182691, 0.005760345, -0.0040665213, 0.00075491105, 0.007844017, 0.00040791242, 0.0006780336, 0.0027037326, -0.0041370974, -0.022275126, 0.004775642, -0.0045235846, -0.003659869, -0.0020567859, 0.021602973, 0.01010917, -0.011419867, 0.0043689897, -0.0017946466, 0.000101610516, -0.014061426, -0.002626435, -0.00035540052, 0.0062174085, 0.020809835, 0.0035220778, -0.0071046497, -0.005041142, 0.018067453, 0.012569248, -0.021683631, 0.020245226, 0.017247427, 0.017032338, 0.01037131, -0.036296222, -0.026334926, 0.041135717, 0.009625221, 0.032155763, -0.025057837, 0.027827105, -0.03323121, 0.0055721425, 0.005716655, 0.01791958, 0.012078577, -0.011117399, -0.0016005626, -0.0033254733, -0.007702865, 0.034306653, 0.0063854465, -0.009524398, 0.006069535, 0.012696956, -0.0042883316, -0.013167463, -0.0024667988, -0.02356566, 0.00052721944, -0.008858967, 0.039630096, -0.0064593833, -0.0016728189, -0.0020366213, 0.00622413, -0.03739855, 0.0028616884, -0.0102301575, 0.017717933, -0.0041068504, -0.0060896995, -0.01876649, 0.0069903834, 0.025595559, 0.029762903, -0.006388807, 0.017247427, 0.0022080203, -0.029117636, -0.029870447, -0.0049739266, -0.011809715, 0.023243025, 0.009510955, 0.030004878, 0.0015837587, -0.018524516, 0.007931396, -0.03589293, 0.013590919, -0.026361812, 0.002922182, 0.025743432, 0.014894894, 0.0012989342, -0.0016232478, 0.006251016, 0.029789789, -0.004664737, 0.017812036, -0.013436324, -0.0102301575, 0.016884465, -0.017220542, 0.010156221, 0.00014503786, 0.03933435, 0.018658947, 0.016897907, 0.0076961434, -0.029843561, -0.02021834, 0.015056211, 0.01002179, -0.0031994449, -0.03796316, -0.008133043, 0.03707592, 0.032128878, 9.483648E-05, 0.0017627194, -0.0007544909, 0.006647586, 0.020903936, -0.032559056, 0.025272924, -0.012804501, 0.019210111, 0.0022987607, 0.013301893, -0.0047218697, -0.022853177, -0.02162986, 0.006788738, 0.0092286505, 0.024184039, -0.015419173, -0.006479548, -0.00180977, 0.0060728956, -0.0030919004, 0.0022449887, -0.004046357, 0.012663349, -0.028579915, 0.0047722813, -0.6775295, -0.018779935, -0.018484188, -0.017449073, -0.01805401, 0.026630674, 0.008018777, 0.013436324, -0.0034683058, 0.00070912065, -0.005027699, 0.009658828, -0.0031792803, -0.010478854, 0.0034951917, -0.011594627, 0.02441257, -0.042533796, -0.012414653, 0.006261098, -0.012266779, 0.026630674, -0.017852364, -0.02184495, 0.02176429, 0.019263884, 0.00984031, -0.012609577, -0.01907568, -0.020231783, -0.002886894, 0.02706085, -0.0042345594, 0.02265153, 0.05769755, 0.021522315, -0.014195856, 0.011144285, 0.0038077426, 0.024573887, -0.03578539, -0.004476534, 0.016521502, -0.019815048, 0.00071836275, 0.008173372, 0.013436324, 0.021885278, -0.0147604635, -0.021777734, 0.0052595916, -0.011668564, -0.02356566, -0.0049974523, 0.03473683, -0.0255149, 0.012831387, -0.009658828, -0.0031036632, -0.001386314, -0.01385978, 0.008294359, -0.02512505, -0.0012308789, 0.008711093, 0.03610802, 0.016225755, 0.014034539, 0.0032431346, -0.017852364, 0.017906137, 0.005787231, -0.03514012, 0.017207097, -0.0019542826, -0.010189828, 0.010808208, -0.017408744, -0.0074944976, 0.011009854, 0.00887241, 0.009652107, -0.0062409337, 0.009766373, 0.009759651, -0.0020819916, -0.02599885, 0.0040665213, 0.016064439, -0.019035352, -0.013604362, 0.020231783, -0.025272924, -0.01196431, -0.01509654, 0.0010233518, -0.00869765, -0.01064017, 0.005249509, -0.036807057, 0.00054570363, 0.0021777733, -0.009302587, -0.00039362916, 0.011386259, 0.013382551, 0.03046194, 0.0032380936, 0.037801843, -0.036807057, -0.006244295, 0.002392862, -0.01346321, -0.008953068, -0.0025861058, -0.022853177, 0.018242212, -0.0031624765, 0.009880639, -0.0017341529, 0.0072054723, 0.014693249, 0.026630674, 0.008435511, -0.012562525, 0.011581183, -0.0028768117, -0.01059312, -0.027746446, 0.0077969665, 2.468059E-05, -0.011151006, 0.0152712995, -0.01761039, 0.023256468, 0.0076625356, 0.0026163526, -0.028795004, 0.0025877862, -0.017583502, -0.016588718, 0.017556617, 0.00075491105, 0.0075885993, -0.011722336, -0.010620005, -0.017274313, -0.008025498, -0.036376882, 0.009457182, -0.007265966, -0.0048663826, -0.00494368, 0.003616179, 0.0067820163, 0.0033775652, -0.016037554, 0.0043320213, -0.007978448, -0.012925488, 0.029413383, -0.00016583256, -0.018040568, 0.004180787, -0.011453475, -0.013886666, -0.0072121937, 0.006486269, 0.008005333, -0.01412864, -0.00061796, -0.025635887, -0.006630782, 0.02074262, -0.007192029, 0.03906549, -0.0030885397, -0.00088976155, -0.022033151, -0.008758144, 0.00049361185, 0.009342916, -0.014988995, -0.008704372, 0.014276514, -0.012300386, -0.0020063745, 0.030892119, -0.010532626, 0.019653732, 0.0028583275, 0.006163636, 0.0071517, -0.017489402, -0.008448954, -0.004352186, 0.013201071, 0.01090231, 0.0004110631, 0.03306989, 0.006916447, 0.002922182, 0.023888292, -0.009067334, 0.012434817, -0.051298663, 0.016279528, -0.02741037, 0.026227381, -0.005182294, 0.008153207, -0.026603786, 0.0045571923, 0.018067453, 0.038016934, 0.028042194, 0.0077431942, 0.015499831, -0.020298999, 0.0013123773, -0.021334114, -0.026281154, -0.0012720482, -0.0045571923, 0.006086339, 0.0028952959, -0.003041489, 0.007931396, -0.0005406625, -0.023444671, -0.0038715971, 0.0070374343, -0.0019979726, 0.024089938, 0.0020903936, -0.024210924, 0.007319738, -0.005995598, 0.032478396, 0.020998036, 0.01654839, 0.033876475, 0.025098165, 0.021132467, -0.017099554, -0.013516982, 0.01306664, 0.010525905, -0.02335057, -0.013543868, -0.03583916, 0.021172797, -0.033607613, -0.0036094578, -0.007911232, -0.0054578763, 0.013227956, 0.00993441, 0.025810648, 0.02255743, -0.013678298, 0.012273501, 0.00040497174, 0.0019072321, 0.0008170851, 0.01540573, 0.015580489, 0.005239427, 0.003989224, -0.013254843, 0.024708318, 0.0046680975, -0.034360424, -0.0041942303, 0.0077095865, -0.0053503322, -0.024399128, -0.02644247, 0.0062476555, 0.021885278, -0.0010922474, -0.014209299, 0.018295985, 0.0135640325, 0.0033842868, 0.0017812036, 0.004735313, 0.006486269, -0.008072549, 0.009551284, 0.007938119, 0.0101696635, 0.021750847, 0.014034539, 0.0071449787, -0.008448954, 0.010841816, -0.008274195, -0.014531932, -0.0024785616, 0.0018601815, 0.009564727, -0.011130841, -0.020581303, 0.012985982, 0.019976366, -0.030542599, -0.021818062, -0.018551402, -0.0092286505, -0.024385685, 0.0036901159, -0.0061367503, -0.00034048714, -0.007057599, -0.014558818, -0.022221355, 0.023377456, 0.026119838, -0.0008813597, 0.004520224, 0.0027843907, -0.022382671, 0.0018248934, 0.13313992, 0.013685021, -6.170148E-05, 0.015876237, 0.005417547, -0.008314524, -0.019169783, -0.016494617, 0.016844137, -0.0046412116, 0.024305027, -0.027827105, 0.023162367, 0.0143034, -0.0029893972, -0.014626034, -0.018215327, 0.0073264595, 0.024331912, -0.0070777633, -0.0004259765, -0.00042345593, -0.0034262962, -0.00423792, -0.016185427, -0.017946465, -5.9706024E-05, 0.016467731, -0.014773907, -0.022664975, -0.009322752, -0.027585128, 0.0020651878, -0.010532626, -0.010546069, 0.009174879, -0.0011098915, 0.026469355, 0.022006266, -0.013039754, 0.023458114, 0.005481402, -0.00050705485, -0.012092019, 0.0055990284, -0.007057599, -0.012266779, 0.03253217, 0.007071042, -0.01699201, 0.06597847, -0.013436324, 0.0070038266, -0.009981461, 0.024829306, 0.0067383265, 0.0056292755, 0.0018534599, -0.020057024, 0.011735778, 0.0025491375, -0.022194467, 0.0012468424, -0.0051621296, -0.018457301, -0.008509448, -0.011594627, -0.0152712995, -0.001858501, -0.014921781, -0.0056696045, -0.0066979975, -0.02008391, 0.0040093884, 0.032935463, -0.0032935461, -0.0074205613, -0.014088311, -0.0014762144, -0.011218221, 0.011984475, -0.01898158, -0.027208723, -0.008072549, 0.010942639, 0.0183632, 0.04148524, -0.0009922648, -0.017086111, 0.013483374, 0.019841935, 0.024264697, 0.011601348, -0.0077431942, -0.020258669, -0.005770427, 0.013429603, -0.011554297, -0.012831387, -1.4752561E-06, 0.011594627, -0.012683514, -0.012824666, 0.02180462, 0.011023297, 0.012468425, -0.0029860365, -0.0076289284, -0.021293784, 0.005068028, 0.017812036, 0.0007708746, -0.008684208, 0.0048126103, -0.0076558143, 0.019169783, -0.0076558143, 0.028579915, -0.011574462, -0.03196756, -0.0011334168, -0.030219967, 0.023901735, 0.014021097, -0.016776921, 0.0030045207, -0.0019257163, -0.023579102, 0.004197591, 0.00012497831, -0.016803807, 0.01915634, -0.010472132, -0.042130504, -0.038016934, -0.007702865, -0.0025861058, -0.010512462, -0.013537147, -0.013382551, -0.0036397045, 0.0053032814, 0.0046277684, -0.021952493, -0.016588718, -0.031886905, 0.0058208387, -0.00043689896, -0.01337583, 0.018349757, 0.015244413, 0.00900684, -0.017677605, 0.01523097, 0.010337702, -0.024426013, -0.021965936, -0.014182413, 0.008596827, 0.029628472, 0.058611676, -0.015446059, 0.021374442, -0.0095042335, 0.00091748784, 0.021132467, -0.011285436, -0.0035724894, -0.027907763, 0.027302826, 0.004184148, 0.026281154, -0.0026802071, -0.015163755, 0.005699851, 0.023122039, 0.0075415485, -0.020057024, -0.0109359175, -0.018309427, 0.017529732, 0.0020685487, -0.012441538, 0.0023239665, 0.012038247, -0.017543174, 0.029332725, 0.01399421, -0.0092488155, -1.0607403E-05, 0.019371428, -0.0315105, 0.023471557, -0.009430297, 0.00022097006, 0.013301893, -0.020110795, -0.0072928523, 0.007649093, 0.011547576, 0.026805433, -0.01461259, -0.018968137, -0.0104250815, 0.0005646079, 0.031456728, -0.0020147765, -0.024224367, 0.002431511, -0.019371428, -0.025017507, -0.02365976, -0.004318578, -0.04457714, 0.0029826758, -0.020473758, -0.016118212, -0.00068181445, -0.03446797, -0.020715732, -0.04256068, -0.013792564, 0.013873223, 0.011413146, -0.002419748, 0.0123877665, -0.0011115718, 0.007978448, 0.021441657, 0.004405958, 0.0042480025, 0.022920392, -0.0067920987, 0.011083791, -0.017529732, -0.03659197, -0.0066005355, -0.023888292, -0.016521502, 0.009591613, -0.0008590946, 0.013846337, -0.021092137, -0.012562525, -0.0028415236, 0.02882189, 5.3378342E-05, -0.006943333, -0.012226449, -0.035570297, -0.024547001, 0.022355784, -0.018416973, 0.014209299, 0.010035234, 0.0046916227, 0.009672271, -0.00067635323, -0.024815861, 0.0007049197, 0.0017055863, -0.0051251613, 0.0019391594, 0.027665788, -0.007306295, -0.013369109, 0.006308149, 0.009699157, 0.000940173, 0.024842748, 0.017220542, -0.0053032814, -0.008395182, 0.011359373, 0.013214514, 0.0062711807, 0.004110211, -0.019277327, -0.01412864, -0.009322752, 0.007124814, 0.0035119955, -0.024036165, -0.012831387, -0.006734966, -0.0019694061, -0.025367027, -0.006630782, 0.016010666, 0.0018534599, -0.0030717358, -0.017717933, 0.008489283, 0.010875423, -0.0028700903, 0.0121323485, 0.004930237, 0.009947853, -0.02992422, 0.021777734, 0.00015081417, 0.010344423, 0.0017543174, 0.006166997, -0.0015467904, 0.010089005, 0.0111711705, -0.010740994, -0.016965123, -0.006771934, 0.014464716, 0.007192029, -0.0006175399, -0.010855259, -0.003787578, 0.015647706, 0.01002179, -0.015378844, -0.01598378, 0.015741806, -0.0039119264, -0.008422068, 0.03253217, -0.019210111, -0.014975552, 0.0025810648, 0.0035556855, 8.449164E-05, -0.034172222, -0.006395529, -0.0036867552, 0.020769505, 0.009766373, -0.017543174, -0.013557311, 0.0031994449, -0.0014577302, 0.01832287, -0.009907524, -0.024654545, 0.0049940916, 0.016965123, 0.004476534, 0.022261683, -0.009369803, 0.0015308268, -0.010102449, -0.001209874, -0.023807634, -0.008348132, -0.020312442, 0.030892119, -0.0058309208, -0.005128522, -0.02437224, 0.01478735, -0.011016576, -0.010290652, -0.00503106, 0.016884465, 0.02132067, -0.014236185, -0.004903351, 0.01902191, 0.0028179984, 0.019505858, -0.021535758, -0.0038514326, 0.0112115, 0.0038682362, 0.003217929, -0.0012770894, -0.013685021, -0.008381739, 0.0025256122, 0.029386498, 0.018645504, 0.005323446, -0.0032784226, -0.0043253, 0.0007998612, 0.019949479, 0.025770318, -0.0030868594, 0.018968137, -0.010236879, -0.005370497, -0.024748646, -0.014047982, 0.005760345, -0.03610802, 0.0042009517, -0.0034817487, 0.003385967, 0.006560206, -0.006294706, -0.02400928, -0.006140111, -0.0017980073, -0.012481867, -0.0033960494, -0.00097210024, 0.014061426, -0.017596947, -0.023202697, 0.0028499255, -0.016010666, -0.028149737, 0.0024752007, -0.018941252, 0.0056158323, -0.012912045, 0.0054410724, 0.003054932, 0.019559631, -0.0048932685, -0.007823853, -0.017099554, 0.025662774, 0.02572999, 0.004379072, -0.010223436, 0.0031036632, -0.011755943, -0.025622444, -0.030623257, 0.019895706, -0.02052753, -0.006637504, -0.001231719, -0.013980767, -0.02706085, -0.012071854, -0.0041370974, -0.008885853, 0.0001885177, 0.2460615, -0.009389968, -0.010714107, 0.0326666, 0.0009561366, 0.022624645, 0.009793258, 0.019452088, -0.004493338, -0.007097928, -0.0022298652, 0.012401209, -0.0036229007, -0.00023819396, -0.017502844, -0.014209299, -0.030542599, -0.004863022, 0.005128522, -0.03081146, 0.02118624, -0.0042177555, 0.0032448152, -0.019936036, 0.015311629, 0.0070508774, -0.02021834, 0.0016148458, 0.04317906, 0.01385978, 0.004211034, -0.02534014, -0.00030309867, -0.011930703, -0.00207527, -0.021643303, 0.01575525, -0.0042883316, 0.0069231684, 0.017946465, 0.03081146, 0.0043857936, 3.646951E-05, -0.0214551, 0.0089933975, 0.022785962, -0.008106156, 0.00082884775, -0.0006717322, -0.0025457768, -0.017059224, -0.035113234, 0.054982055, 0.021266898, -0.0071046497, -0.012636462, 0.016965123, 0.01902191, -0.0061737187, 0.00076247274, 0.0002789432, 0.030112421, -0.0026768465, 0.0015207445, -0.004926876, 0.0067551304, -0.022624645, 0.0005003333, 0.0035523248, -0.0041337362, 0.011634956, -0.0183632, -0.02820351, -0.0061737187, -0.022355784, -0.03796316, 0.041888528, 0.019626847, 0.02211381, 0.001474534, 0.0037640526, 0.0085228905, 0.013140577, 0.012616298, -0.010599841, -0.022920392, 0.011278715, -0.011493804, -0.0044966987, -0.028741231, 0.015782135, -0.011500525, -0.00027621258, -0.0046378504, -0.003280103, 0.026993636, 0.0109359175, 0.027168395, 0.014370616, -0.011890373, -0.020648519, -0.03465617, 0.001964365, 0.034064677, -0.02162986, -0.01081493, 0.014397502, 0.008038941, 0.029789789, -0.012044969, 0.0038379894, -0.011245107, 0.0048193317, -0.0048563, 0.0142899575, 0.009779816, 0.0058510853, -0.026845763, 0.013281729, -0.0005818318, 0.009685714, -0.020231783, -0.004197591, 0.015593933, -0.016319858, -0.019492416, -0.008314524, 0.014693249, 0.013617805, -0.02917141, -0.0052058194, -0.0061838008, 0.0072726877, -0.010149499, -0.019035352, 0.0070374343, -0.0023138842, 0.0026583623, -0.00034111727, 0.0019038713, 0.025945077, -0.014693249, 0.009820145, -0.0037506097, 0.00041127318, -0.024909964, 0.008603549, -0.0041707046, 0.019398315, -0.024022723, -0.013409438, -0.027880875, 0.0023558936, -0.024237812, 0.034172222, -0.006251016, -0.048152987, -0.01523097, -0.002308843, -0.013691742, -0.02688609, 0.007810409, 0.011513968, -0.006647586, -0.011735778, 0.0017408744, -0.17422187, 0.01301959, 0.018860593, -0.00068013405, 0.008791751, -0.031618044, 0.017946465, 0.011735778, -0.03129541, 0.0033607613, 0.0072861305, 0.008227143, -0.018443858, -0.014007653, 0.009961297, 0.006284624, -0.024815861, 0.012676792, 0.014222742, 0.0036632298, 0.0028364826, -0.012320551, -0.0050478633, 0.011729057, 0.023135481, 0.025945077, 0.005676326, -0.007192029, 0.0015308268, -0.019492416, -0.008932903, -0.021737404, 0.012925488, 0.008092714, 0.03245151, -0.009457182, -0.018524516, 0.0025188907, -0.008569942, 0.0022769158, -0.004617686, 0.01315402, 0.024291582, -0.001880346, 0.0014274834, 0.04277577, 0.010216715, -0.018699275, 0.018645504, 0.008059106, 0.02997799, -0.021576088, 0.004846218, 0.015741806, 0.0023542133, 0.03142984, 0.01372535, 0.01598378, 0.001151901, -0.012246614, -0.004184148, -0.023605987, 0.008657321, -0.025770318, -0.019048795, -0.023054823, 0.005535174, -0.018161554, -0.019761277, 0.01385978, -0.016655933, 0.01416897, 0.015311629, 0.008919461, 0.0077499156, 0.023888292, 0.015257857, 0.009087498, 0.0017845642, 0.0013762318, -0.023713533, 0.027464142, -0.014021097, -0.024681432, -0.006741687, 0.0016450927, -0.005804035, -0.002821359, 0.0056796866, -0.023189254, 0.00723908, -0.013483374, -0.018390086, -0.018847149, 0.0061905226, 0.033365637, 0.008489283, 0.015257857, 0.019694062, -0.03019308, -0.012253336, 0.0021744126, -0.00754827, 0.01929077, 0.025044393, 0.017677605, 0.02503095, 0.028579915, 0.01774482, 0.0029961187, -0.019895706, 0.001165344, -0.0075281053, 0.02105181, -0.009221929, 0.023404341, -0.0028079161, -0.0037237236, 0.02847237, 0.0009821824, 0.04629785, -0.017771706, -0.038904175, 0.00869765, 0.0016249281, 0.020984594, -0.10867358, -0.008395182, -0.0010830053, 0.008059106, -0.020097353, 0.0020383017, 0.008038941, -0.009047169, -0.007252523, 0.0286068, -0.0037774958, -0.024923407, 0.005279756, -0.009524398, 0.011527412, -0.0020198175, 0.019452088, 0.014384058, -0.025609002, 0.006025845, -0.030542599, 0.016790364, 0.019223554, -0.012434817, 0.003901844, -0.007817131, -0.027612016, 0.008314524, 0.007938119, -0.0004868903, 0.014747021, -0.009457182, 0.014706692, -0.018847149, 0.015311629, 0.015647706, -0.0031288688, -0.0032717013, 0.008879132, -0.034629285, 0.0090337265, 0.004382433, 0.011305601, -0.028391711, 0.0053268066, 0.0003566608, -0.019169783, 0.011507247, 0.023592545, -0.006603896, -0.009685714, 0.010714107, -0.027907763, 0.006412333, 0.0045706355, -0.029816674, 0.0047958065, 0.0018500991, -0.011500525, 0.0030179636, 0.015997224, -0.022140697, -0.0001849469, -0.014263071, 0.011540854, -0.006607257, -0.01871272, -0.0038480717, -0.0024903242, -0.031214751, -0.0050478633, 0.021481987, -0.012912045, 0.028122852, -0.018605174, -0.00723908, 0.0023609349, -0.0073331813, 0.014935223, -0.005699851, -0.0068895607, -0.015244413, 0.029789789, -0.02458733, 0.0004453009, 0.0015577129, 0.0048596608, 0.009376524, -0.011984475, -0.014518489, 0.015647706, 0.0068794787, 0.0065534846, 0.003107024, -0.01973439, 0.027383484, -0.015459502, -0.006318231, 0.020863606, -0.0021357639, -0.0076692575, -0.021266898, -0.046862457, 0.025326697, 0.016521502, -0.0036833945, 0.0029860365, -0.016306413, 0.026496243, -0.016803807, 0.008724537, -0.0025407355, -0.027302826, 0.017798591, 0.0060796174, -0.014007653, -0.01650806, -0.0095042335, 0.009242094, -0.009342916, 0.010330981, 0.009544563, 0.018591732, 0.0036867552, 0.0194252, 0.0092488155, -0.007823853, 0.0015501512, -0.012031525, 0.010203271, -0.0074272826, -0.020258669, 0.025662774, -0.03032751, 0.014854565, 0.010835094, 0.0007708746, 0.0009989863, -0.014007653, -0.012871716, 0.023444671, 0.03323121, -0.034575514, -0.024291582, 0.011634956, -0.025958521, -0.01973439, 0.0029742739, 0.0067148013, 0.0022399474, 0.011802994, 0.011151006, -0.0116416775, 0.030166194, 0.013039754, -0.022517102, -0.011466918, -0.0033053088, 0.006156915, 0.004829414, 0.006029206, -0.016534945, 0.015325071, -0.0109359175, 0.032854803, -0.001010749, 0.0021155993, -0.011702171, -0.009766373, 0.00679882, 0.0040900465, -0.019438643, -0.006758491, -0.0040060277, 0.022436442, 0.025850976, 0.006150193, 0.018632062, -0.0077230297, -0.015298186, -0.017381858, 0.01911601, -0.005763706, -0.0022281848, -0.031994447, 0.0015972018, 0.028848775, 0.014572261, -0.0073264595, -0.009551284, -0.0052058194, 0.014518489, -0.0041068504, 0.010754436, 0.0055519775, -0.005804035, -0.0054007433, 0.028579915, -0.01791958, -0.015284742, 0.036807057, 0.015069654, -0.0023810994, -0.0038648755, 0.0015467904, -0.0037136413, 0.0023458113, 0.019008467, -0.011547576, -0.010001626, 0.012347437, 0.0155267175, 0.01907568, -0.003041489, -0.0132414, 0.017449073, 0.00060073606, -0.008536334, 0.008233866, -0.0085430555, -0.02365976, 0.024089938, -0.0034615842, -0.006580371, 0.008327967, -0.01509654, 0.009692436, 0.025635887, 0.0020282194, -0.04022159, -0.0021290423, -0.012407931, -0.0021727323, 0.006506434, -0.005320085, -0.008240587, 0.020984594, -0.014491603, 0.003592654, 0.0072121937, -0.03081146, 0.043770555, 0.009302587, -0.003217929, 0.019008467, -0.011271994, 0.02917141, 0.0019576435, -0.0077431942, -0.0030448497, -0.023726976, 0.023377456, -0.006382086, 0.025716545, -0.017341528, 0.0035556855, -0.019129453, -0.004311857, -0.003253217, -0.014935223, 0.0036363439, 0.018121226, -0.0066543072, 0.02458733, 0.0035691285, 0.0039085653, -0.014209299, 0.020191453, 0.0357585, 0.007830574, -0.024130266, -0.008912739, 0.008314524, -0.0346024, -0.0014005973, -0.006788738, -0.021777734, 0.010465411, -0.004012749, -0.00679882, 0.009981461, -0.026227381, 0.027033964, -0.015567047, -0.0063115098, 0.0023071626, 0.01037131, 0.015741806, -0.020635074, -0.012945653 };

            var options = new VectorSearchOptions<EmbeddedMovie>()
            {
                Filter = Builders<EmbeddedMovie>.Filter.Gt("runtime", 1) & Builders<EmbeddedMovie>.Filter.Gt("year", 1900),
                IndexName = "sample_mflix__embedded_movies"
            };

            var results = GetEmbeddedMoviesCollection()
                .Aggregate()
                .VectorSearch(m => m.Embedding, vector, 5, options)
                .Project<EmbeddedMovie>(Builders<EmbeddedMovie>.Projection
                    .Include(m => m.Title)
                    .MetaVectorSearchScore(p => p.Score))
                .ToList();

            results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
            results.Should().OnlyContain(m => m.Score > 0.9);
        }

        [Fact]
        public void VectorSearchExact()
        {
            var expectedTitles = new[]
            {
                "Red Dawn",
                "Sands of Iwo Jima",
                "White Tiger",
                "P-51 Dragon Fighter",
                "When Trumpets Fade"
            };

            var vector = new [] { -0.006954097,-0.009932499,-0.001311474,-0.021280076,-0.014608995,-0.008227668,-0.013274779,-0.0069271433,-0.009521453,-0.030943038,0.017304381,0.015094165,-0.010397454,0.010943269,0.012230316,0.025943095,0.02583528,0.001368751,0.009777515,-0.016024074,-0.013989056,0.00685639,0.030242238,-0.023881124,-0.011057823,-0.0056906347,-0.00055929273,-0.03199424,0.0072168973,-0.023166848,0.0033490178,-0.024069803,-0.023557678,-0.020862292,0.007452744,-0.019002475,0.007850314,-0.032856762,-0.012951332,0.003005356,-0.003739849,0.010053792,0.019541552,0.007702067,-0.035498243,-0.00918453,-0.0143529335,0.000249955,-0.011866439,0.019703276,0.0076481593,0.025161434,-0.015714103,-0.0076818517,-0.023180325,-0.0032883717,-0.02315337,0.015188503,0.031347346,-0.008739791,0.01454161,0.014824626,-0.025107525,0.0012558816,-0.012803086,-0.013146748,-0.01652272,0.01283004,-0.019352876,-0.010444623,0.04706145,0.030754361,0.008820653,0.011657547,0.014878534,-0.010181823,0.00041673204,-0.009103668,-0.0055255424,0.00579845,0.024110233,-0.020404076,-0.0066272817,-0.005299804,0.019649368,0.007729021,-0.0066845585,0.025592696,0.00575465,-0.014824626,0.00033334352,-0.008591545,0.004164372,0.028085928,-0.020875769,-0.00448108,-0.009258653,0.02535011,-0.0025538788,-0.059082873,-0.0074055744,0.005950066,-0.014164257,-0.016185796,-0.015768012,-0.01309284,0.0030222023,0.0028436328,0.0037095258,-0.0068462817,-0.010963485,0.018988999,0.010929792,-0.03967609,0.016994413,-0.023503771,-0.0037903874,-0.012075332,-0.005923112,-0.01902943,0.017978229,-0.005993866,0.024015894,-0.017722167,0.010875884,0.0153637035,-0.04045775,-0.0016568204,-0.0074190516,0.0011084777,0.033018485,0.021536138,0.025794849,-0.019622413,-0.041724585,0.014743765,-0.011111731,0.0065666353,-0.019851523,-0.035174794,0.007270805,0.02698082,-0.010929792,-0.020148015,-0.009689915,0.032182917,0.015700627,0.013786903,-0.0021647324,-0.0063644815,0.027317744,0.004403588,0.03158993,-0.0039116796,0.02029626,0.04191326,-0.0050504804,0.008416344,-0.0034602026,0.010485054,-0.0030996946,-0.0212666,0.0043934803,-0.016765304,-0.00039441086,0.025538787,0.008483729,0.017479582,-0.0061454815,-0.018220814,-0.0025589326,0.0102829,-0.045498125,0.029784022,0.017169613,0.020592753,0.012445947,0.021630477,-0.031050853,-0.011327362,-0.024689741,0.0048988652,0.022129124,0.0114621315,-0.026091343,0.015067211,0.004723665,0.017722167,0.014608995,-0.016805734,0.0042250184,0.027843343,0.03264113,0.007034959,-0.6852751,-0.019986292,0.003827449,0.0032462562,0.006115158,-0.001972686,0.001937309,0.015956689,-0.010437884,0.00803899,-0.01401601,-0.00026869634,-0.033207163,-0.00623982,-0.0048449575,-0.013193917,-0.00426208,-0.013207395,-0.014379887,0.017991705,0.0007264909,0.023517247,-0.00725059,0.0071832053,-0.003466941,-0.0072842822,0.0028234175,-0.02460888,0.0044608647,-0.008746529,-0.025713988,0.012499855,0.004882019,0.010525485,0.05250613,-0.003387764,-0.021212691,0.026549557,0.034123596,0.014325979,-0.0080592055,-0.012189886,-0.005970281,0.0115093,0.0021731553,0.033557564,0.020929677,-0.02130703,0.022991648,-0.03123953,0.0102829,-0.0067721587,0.008510683,0.010013361,0.031320393,0.010646777,0.015215457,-0.019797614,0.0022186402,-0.0010351969,0.0015523742,0.021630477,0.00751339,0.0062162355,-0.016468812,0.018840753,-0.02544445,0.030943038,0.008295052,-0.005700743,-0.0033557562,0.020053675,-0.012722225,-0.016778782,0.025511835,0.025538787,0.022802971,-0.017708689,-0.009615792,-0.00733819,0.0049224496,-0.021590047,-0.021158785,0.022034785,0.046872772,-0.021064445,-0.024811033,0.0043429416,-0.006169066,-0.0044406494,-0.003357441,0.006445343,-0.01801866,-0.0082074525,0.0037802798,0.02258734,0.0018598167,-0.0101548685,0.020134538,-0.008214191,-0.0004830638,0.00421828,0.0048719114,0.0087869605,-0.008335483,0.023503771,-0.030161375,0.0055929273,0.054069456,-0.010006622,0.018975522,0.015956689,-0.018530782,0.003669095,0.014662903,-0.023126416,0.011300408,0.021563092,-0.00106552,-0.03269504,0.020282784,-0.023665493,0.025242295,-0.01801866,-0.008537637,-0.00083472754,0.02130703,-0.008780221,0.0080592055,-0.0012971548,0.014838103,0.0056704194,0.015794965,0.001546478,0.013072625,0.0057310658,0.011711455,-0.020997062,0.010289638,0.0041070953,-0.025592696,0.012661578,-0.013436502,0.013348902,0.014312503,-0.023894602,-0.018517306,-0.0105928695,-0.02140137,0.0076885903,0.03220987,-0.0042283875,-0.04649542,-0.000022110593,0.00013803327,-0.0046293265,-0.0036286642,-0.028598052,-0.0042856648,-0.029406667,0.026765188,0.0027711943,-0.0059298505,-0.00311991,0.016293611,-0.027182974,-0.011623855,0.0030508407,-0.0035747564,-0.018032135,0.03045787,0.0011337469,-0.004780942,0.012055117,-0.0081333285,0.02908322,-0.008092898,-0.0015944897,0.014716811,0.014325979,-0.0037061565,-0.0074325283,-0.01854426,-0.0070821284,-0.003598341,-0.01718309,-0.0108826235,0.014959396,-0.019366352,0.017722167,0.009528192,0.022439092,-0.01630709,-0.003625295,-0.00413068,-0.007311236,-0.008503945,-0.006024189,0.038867474,0.030943038,0.015956689,0.007196682,0.013699302,0.0025471402,0.026792143,-0.031832516,-0.018867707,-0.03897529,0.04679191,0.00421828,0.01634752,-0.004120572,-0.009696653,-0.011414962,0.011980994,0.045659848,-0.010080745,-0.0045720492,-0.020794908,-0.0030205175,-0.00896216,-0.0071427743,0.006559897,0.0045147724,-0.032991532,0.0059332196,0.0033153256,0.00426208,0.0063779587,-0.025943095,-0.021495707,-0.008268098,-0.002429217,0.0102829,-0.0048786495,0.003584864,0.004737142,-0.029244944,0.027169496,0.010599608,-0.008517422,-0.0016652435,0.023544202,-0.022398662,0.010202038,0.0029211252,0.021185739,-0.01708875,-0.03396187,0.025121003,-0.009838161,0.015633242,-0.015525427,0.003743218,0.0044676033,-0.021819154,-0.006802482,-0.023126416,0.028112883,0.03045787,0.015255888,-0.008685883,-0.010943269,-0.0039386335,-0.0014950972,-0.009043022,-0.0023264554,-0.0028975406,-0.0021377786,0.002860479,-0.0010166661,-0.01173167,0.03040396,-0.028005067,-0.00509765,-0.008463514,0.019959338,-0.012533547,-0.012782871,-0.0055558654,0.004807896,-0.018800322,0.005582819,0.017991705,-0.005505327,-0.008382652,0.0011404854,-0.0035073718,-0.017587397,0.012890686,-0.0015363704,-0.000308706,0.011603639,-0.0042452337,-0.027129065,-0.0086387135,0.023786787,-0.024352817,-0.015175027,0.017546967,0.0064318664,0.0100874845,-0.008605022,-0.006529574,0.014703333,-0.0010318276,-0.012290963,-0.014622472,-0.008382652,-0.000661212,0.0044170646,0.009386684,-0.0030660022,0.0033102715,-0.007095605,0.007978344,-0.016980935,-0.0040262337,0.022344755,0.0077357595,0.0063341586,-0.016482288,-0.028220698,-0.026899958,0.08291009,0.014002534,0.00075428706,0.013018717,0.0013485356,-0.0223178,-0.016697919,-0.018584691,0.0057378043,0.007924437,0.0032850024,-0.01735829,0.029730113,0.021684386,-0.008382652,0.017021365,0.0030811639,-0.008025514,0.0043564187,-0.000044694985,-0.0038375566,-0.012924379,-0.012230316,0.0345279,-0.014689857,-0.019406782,0.008780221,0.0092923455,-0.01555238,-0.01920463,-0.01827472,-0.00305421,-0.017573921,0.016320566,0.02548488,0.02105097,0.012823301,0.03431227,0.026589988,-0.025606172,0.007755975,0.0019693167,0.0096494835,-0.034069687,0.0067721587,0.0004190484,0.005508696,0.033207163,-0.0011792317,-0.012648102,0.022829924,0.015821919,-0.035956457,-0.038732704,-0.020740999,0.0063880663,-0.010471577,-0.006933882,0.005936589,0.015889304,-0.00852416,-0.031455163,0.009305822,0.0051178653,-0.027452512,-0.015700627,-0.017762598,-0.005869204,-0.0065531586,-0.011879916,-0.008220929,-0.0053503425,-0.0026920172,-0.00038830412,0.019959338,-0.010444623,-0.00047548304,-0.015040257,0.0008330429,0.008005298,-0.0075336057,-0.02117226,-0.027250359,-0.018962044,-0.006138743,0.014918964,-0.014757241,-0.013867764,0.0006961678,0.01726395,-0.0039352644,0.005724327,0.019878477,0.009494499,0.0071832053,-0.0023652017,0.004865173,0.029784022,0.002390471,0.0005854043,0.00034387235,0.0053975116,-0.011650808,0.005218942,0.007136036,-0.0091440985,0.005879312,0.028193744,-0.024662787,-0.0029447097,0.009636007,-0.029730113,0.00025206077,0.006745205,0.011522777,0.02035017,0.026131773,0.029972699,0.008012037,-0.012466162,0.012459424,-0.01836906,0.0051515577,0.022196509,-0.015970165,0.0037465873,-0.008396129,-0.009494499,-0.0036657257,0.011219546,-0.01823429,0.013524102,0.015134595,0.020269306,-0.0028891175,0.008456775,0.0051650344,0.011131947,-0.0038948336,-0.010909577,-0.010485054,-0.026859527,-0.005475004,-0.0036589873,0.0034602026,-0.026253065,-0.010350284,0.0137734255,0.0030828484,0.012526809,-0.026131773,-0.02363854,0.00024321652,-0.015956689,-0.0042991415,-0.027762482,-0.012782871,-0.016940504,0.01256724,0.03598341,0.026387835,0.0013763318,0.0041946955,0.0017520012,-0.007755975,-0.011179116,0.003537695,0.011522777,-0.035929505,0.021037493,0.041859355,0.034770485,-0.012769394,0.0059534353,0.02851719,0.017749121,-0.011017392,0.0059568044,-0.02105097,-0.011233023,0.0095079765,-0.0019002475,-0.006472297,-0.00826136,-0.011495824,-0.024056325,0.00628362,0.0028689022,0.0050808038,0.00632742,0.020228876,-0.0029767177,0.0327759,-0.016859643,0.0006439447,-0.01516155,0.015889304,0.0032765793,-0.0017570552,0.016509242,0.0076481593,0.0033254332,-0.012513332,-0.0073718824,-0.012237055,0.025997004,-0.0052122036,-0.0007530236,-0.01739872,-0.020188445,-0.0005908793,-0.011212808,0.008840868,-0.015741058,0.0034197718,-0.0033456485,-0.010262684,0.010080745,-0.016751828,-0.04415043,0.010202038,0.0058557275,0.023369001,0.009238438,0.04029603,0.009743823,-0.006802482,-0.01502678,0.01634752,-0.018301675,0.011313885,0.030080514,0.009501237,0.000942543,-0.007755975,-0.015242411,0.003898203,-0.017236996,-0.026172204,0.02394851,0.0065834816,0.021617,0.0054514194,-0.039487414,-0.021549616,0.018813798,-0.025848757,0.0026566405,-0.011199331,-0.024635833,-0.01129367,0.010181823,-0.033638425,0.033072393,-0.008537637,-0.02597005,0.010444623,-0.037169382,-0.0003693522,0.008793699,-0.006647497,0.014716811,-0.026023958,-0.011455393,0.0012853625,-0.0063072047,-0.01103087,-0.0034736795,-0.010390716,0.021913493,-0.021509185,0.0022085323,-0.003756695,0.009656223,0.016549673,0.0022186402,-0.020687092,-0.029056268,-0.00065447355,-0.0035309563,0.009305822,0.007958129,-0.0086387135,-0.02035017,-0.029298851,-0.007479698,-0.0205658,-0.0051178653,-0.003770172,-0.02394851,-0.020174969,0.010053792,-0.014325979,0.014811149,-0.0015481627,-0.0060073426,-0.0028116251,0.04091597,-0.026873004,0.00038851472,0.0037769105,0.017344812,-0.003067687,0.023099463,0.0015094165,-0.030296145,0.045012955,-0.035147842,-0.0067553124,0.0090025915,0.0063408967,0.020538846,-0.0011295355,0.0006153062,0.03870575,0.03840926,0.007486436,-0.040619474,-0.0059466967,0.033934917,0.018813798,0.026037434,0.0026195787,-0.010424407,0.011960777,-0.0032277254,0.007924437,0.011617116,-0.028328514,-0.024622357,0.0014639319,0.025147956,-0.00003919366,-0.008800437,-0.0345279,0.009757299,-0.003044102,0.023786787,0.008989114,-0.018207336,-0.0035612795,0.0025185018,-0.00079050637,-0.00048643304,0.0041778493,-0.00927213,-0.014285549,-0.018705983,-0.024150664,-0.021509185,-0.0032193023,0.028220698,-0.005720958,0.00070711784,-0.016953982,-0.0071629896,-0.02702125,0.0040093875,0.00628362,-0.0021411476,0.029110175,0.018220814,0.0071562515,0.022924263,0.0076144673,-0.002565671,0.0020771322,0.004016126,-0.007836836,0.0016509243,0.005485112,0.0061050504,-0.018988999,0.0060713585,-0.0017823244,0.0054413117,-0.0059837583,-0.0060342965,0.013348902,-0.0223178,-0.01300524,-0.013261302,-0.0058995276,0.030377006,-0.002319717,-0.02948753,0.0110982545,0.000072175295,-0.006657605,-0.031131715,0.00733819,-0.0033186947,-0.02649565,0.0060983123,0.011441916,-0.014555087,0.007291021,-0.017735643,0.03654944,0.024622357,0.0003013358,0.02227737,0.0105457,0.025026664,-0.0013965472,-0.012836779,-0.017803028,0.0021849477,0.025997004,0.00009791834,0.007048436,-0.00032555216,0.026131773,-0.025053618,0.01361844,-0.023517247,-0.0023230864,-0.014892011,0.016239705,0.022021309,-0.016967459,0.019056384,0.0047539882,0.0029598714,-0.0013519048,-0.0045585725,0.0015439511,0.021509185,-0.0050909114,0.029999653,0.00074712746,-0.011354316,-0.0037735412,0.0031805562,-0.0055962964,0.019298967,0.18652076,-0.010640038,0.0029514483,0.018962044,-0.0013190547,0.011852963,0.020444507,-0.004720296,0.0021344093,-0.0016542935,-0.008005298,0.013854287,0.0020181707,0.007210159,-0.0037162642,-0.0023129785,-0.03415055,-0.03202119,-0.023665493,0.012621148,0.00927213,-0.006630651,-0.007958129,-0.009379946,0.026172204,-0.006947359,-0.008679145,-0.0073516667,0.01739872,0.0014209741,-0.000361982,0.012142717,-0.0030036713,0.0080592055,-0.021873062,-0.0064116507,0.008227668,-0.028598052,0.0013156856,0.018476875,-0.014231641,0.010929792,0.0066373893,0.03840926,0.015565857,0.009777515,-0.0049628806,0.006920405,0.019528076,0.030646546,-0.028948452,-0.008335483,0.012412256,0.040942922,0.016482288,0.022021309,0.0041946955,0.0058759428,-0.011219546,-0.022021309,-0.016401427,0.020660138,-0.02632045,0.023355525,-0.018759891,0.014299026,-0.029406667,0.0076346826,0.019891953,-0.021387892,-0.0071629896,-0.014110349,-0.026684327,-0.004797788,-0.015606288,-0.006570005,0.034986116,0.006738466,-0.009838161,0.0011303778,-0.010538962,-0.03444704,0.00685639,-0.003888095,-0.009494499,-0.013800379,0.017236996,0.017614352,-0.017546967,0.017614352,0.0024612248,-0.0006982736,-0.008800437,0.0016677704,-0.004666388,-0.0050605885,0.018975522,0.009602315,-0.0034416718,-0.01401601,-0.018247766,-0.00032576272,0.005114496,0.009016068,-0.009609053,-0.0049662497,-0.001726732,0.0019339399,-0.010774808,-0.004016126,-0.0065127276,-0.018759891,0.0020063785,-0.0052661113,-0.0073449286,0.0017671628,-0.034905255,-0.014218165,0.004235126,-0.019649368,0.00010865777,-0.025727466,0.030781314,-0.00861176,-0.016158843,-0.016617058,-0.01454161,-0.0020333321,0.002092294,-0.032533314,0.006206128,-0.029433621,0.004663019,-0.023625063,-0.021199215,0.0073988363,0.007715544,0.00680922,-0.01103087,0.012270748,0.012614409,-0.034420088,-0.003898203,0.015848873,-0.0030828484,-0.0054244655,0.030107468,0.002491548,-0.009993146,-0.014824626,-0.026266541,-0.000074912794,-0.0048719114,-0.012277486,0.014770718,-0.021428322,-0.03506698,-0.013086102,-0.0028571098,0.024554972,-0.03439313,-0.010902839,0.04474342,-0.017196566,0.0026819096,0.00081240636,-0.17476887,0.004710188,0.024231525,-0.00033229063,0.025902664,0.027762482,0.008268098,0.00016561887,0.00027080212,0.0032934255,0.011799055,0.004161003,-0.037519783,0.007243851,-0.0018901399,-0.00799856,-0.024460632,0.008295052,-0.010821977,0.0101548685,0.017681736,0.0082748365,-0.008840868,-0.01722352,0.013793641,0.015983643,0.003409664,0.041589815,-0.010896101,0.0017722166,-0.03282981,-0.0031249637,0.02359811,0.01058613,-0.0003004935,-0.0016248127,-0.027924204,-0.016360996,-0.0048516956,0.014905488,0.0036724643,0.009063237,0.030781314,-0.023854172,-0.013706041,0.0202154,0.011340839,-0.013604964,0.013692563,-0.005879312,0.0024157402,0.0071764668,-0.01630709,0.0032850024,0.003343964,-0.00404308,0.012095547,-0.009898807,0.004764096,-0.0030744253,-0.031428207,-0.015821919,-0.0012407202,-0.024447156,-0.011718193,-0.013658872,-0.011542993,-0.0046832343,-0.0051347115,0.0032277254,0.004464234,-0.002718971,-0.0057613887,-0.016953982,0.01419121,0.0064891432,-0.012668317,0.0038914643,-0.0018193859,-0.004652911,-0.022910787,0.026953865,0.012890686,-0.009993146,-0.0041407878,-0.0024629096,-0.009541669,0.020687092,-0.015175027,-0.022735586,0.01485158,-0.029945744,-0.020377122,-0.010714161,0.008160283,0.010714161,-0.022439092,0.010330069,-0.016563151,-0.012836779,0.017439151,0.014258595,-0.029891837,0.00896216,0.03506698,0.005936589,0.02548488,0.0067283586,0.035821687,0.0056569427,0.005993866,0.028490236,0.011320624,0.021792201,0.012904163,0.025377065,0.007742498,0.01634752,0.016590104,0.01792432,0.04517468,0.0029935637,-0.019689798,0.0025892558,-0.0055524963,-0.0068867127,-0.10786937,-0.030484822,0.008146806,0.025916142,-0.0038375566,0.014918964,-0.0076481593,-0.003224356,-0.0068833437,0.010559177,-0.0007584986,-0.020242354,-0.021738293,-0.007884006,0.05703438,0.0015296319,-0.008706098,-0.01331521,-0.0153637035,0.03862489,-0.010437884,0.015121119,-0.0012070278,-0.007553821,-0.0088543445,-0.0060511427,-0.01349041,0.018220814,-0.018557737,-0.011987732,-0.012358348,-0.0053503425,-0.000028559516,-0.020148015,-0.011866439,-0.004289034,-0.010896101,-0.012412256,0.011482347,-0.0310239,0.01813995,0.03827449,-0.009406899,-0.022991648,0.009838161,-0.02262777,0.023678971,0.013375856,-0.009171053,-0.020094106,-0.01753349,-0.025188388,-0.03291067,0.0043463106,0.009689915,-0.00725059,0.0070147435,0.0063948045,-0.016940504,0.032991532,0.000852416,-0.002139463,-0.028948452,0.014689857,-0.0056434656,0.014299026,-0.013827333,-0.007958129,0.024864942,-0.0134836715,-0.018422967,0.013908194,-0.021037493,0.004336203,-0.03911006,-0.012627886,-0.019002475,0.0013670664,0.0411316,-0.009514715,-0.031455163,-0.020377122,0.0021647324,-0.01906986,0.027843343,0.018854229,-0.0076616365,0.0032614178,-0.000111395275,-0.010693946,-0.003945372,0.022209985,0.040349938,-0.03870575,0.008692621,0.02043103,0.0026044173,-0.019460691,-0.0032614178,0.010040315,-0.025026664,-0.0015102588,-0.047142312,0.043665264,-0.0054985886,-0.007243851,-0.0024831248,0.00084441405,0.025956573,-0.010889362,-0.00733819,0.018759891,-0.011866439,0.014959396,-0.008578068,0.0027206559,-0.025067095,-0.012499855,0.023180325,0.014865057,0.012452686,-0.0034905255,0.0030727407,-0.0048752804,0.028840637,0.01718309,-0.010444623,-0.020498415,-0.018921614,0.012392039,-0.0086387135,-0.0219674,0.024285434,-0.019083338,-0.013086102,0.01801866,0.02029626,-0.025471402,-0.011482347,0.0011126893,0.015754534,0.04846305,-0.026118295,-0.020336691,0.017075274,-0.007850314,0.014177733,0.030862177,0.007958129,0.018881183,0.013375856,0.001256724,0.006981051,0.028705867,-0.0033405947,-0.010309854,-0.020511892,-0.008254621,0.03695375,0.0015549011,0.024730172,0.0038375566,0.025740942,0.024528017,0.005171773,0.00013319,0.009588838,-0.0029733484,-0.004454126,-0.01199447,-0.012135978,-0.02830156,0.007958129,0.006859759,0.009110407,-0.005505327,-0.0067519434,0.005225681,0.011772101,0.014218165,-0.024878418,0.008955422,0.00900933,0.015094165,-0.038867474,0.018503828,0.012243793,0.012843517,-0.020727523,0.008193975,-0.013935149,0.014838103,0.012695271,0.0004902234,0.005950066,0.015929734,-0.006994528,0.021374416,-0.0093597295,-0.009440592,0.017749121,0.004908973,0.0036926796,-0.005458158,-0.00799856,-0.015646718,-0.008517422,0.0074729593,-0.012991764,-0.0061623277,-0.00944733,0.0032529947,0.013261302,-0.003827449,0.0026212635,0.0037769105,-0.0029143868,0.0013325317,0.0024797556,-0.005124604,-0.03989172,0.019568507,0.0100874845,0.016280135,-0.0003554541,-0.025565742,0.012560502,-0.010491792,0.008631975,-0.00904976,-0.00016856696,0.006010712,0.010781546,-0.0020670246,-0.021873062,-0.011859701,-0.00040346567,0.008665668,0.016199274,0.04204803,-0.036091227,0.032102056,0.02043103,0.008402867,0.013766687,-0.020201921,0.0054682656,-0.0037027872,-0.01221684,0.0030036713,-0.027573805,0.0054076193,0.001863186,-0.006357743,-0.029137129,-0.0096697,0.007675113,-0.0153637035,-0.014878534,0.0060612503,0.001300524,0.030484822,0.003800495,0.018908137,0.01840949,-0.00082461984,-0.030808268,0.014918964,-0.013254563,-0.0036050796,-0.009319299,-0.011920347,-0.0059062657,-0.020714046,-0.0051347115,0.02737165,0.005198727,-0.0059601734,-0.027519897,0.026967343,0.0007357563,0.010431146,0.017654782,-0.019797614,-0.039137013,0.033099346,0.0023129785,-0.011017392,-0.0032816331,-0.020848814 };

            var options = new VectorSearchOptions<EmbeddedMovie>
            {
                IndexName = "sample_mflix__embedded_movies",
                Exact = true
            };

            var results = GetEmbeddedMoviesCollection()
                .Aggregate()
                .VectorSearch(m => m.Embedding, vector, 5, options)
                .Project<EmbeddedMovie>(Builders<EmbeddedMovie>.Projection
                    .Include(m => m.Title)
                    .MetaVectorSearchScore(p => p.Score))
                .ToList();

            results.Select(m => m.Title).ShouldBeEquivalentTo(expectedTitles);
            results.Should().OnlyContain(m => m.Score > 0.9);
        }

        [Fact]
        public void Wildcard()
        {
            var result = SearchSingle(Builders.Search.Wildcard(x => x.Body, "tranquil*", true));

            result.Title.Should().Be("US Constitution");
        }

        private List<AirbnbListing> GeoSearch(SearchDefinition<AirbnbListing> searchDefinition) =>
            GetGeoTestCollection().Aggregate().Search(searchDefinition).ToList();

        private HistoricalDocument SearchSingle(
            SearchDefinition<HistoricalDocument> searchDefinition,
            ProjectionDefinition<HistoricalDocument, HistoricalDocument> projectionDefinition = null,
            SortDefinition<HistoricalDocument> sort = null)
        {
            var fluent = GetTestCollection().Aggregate().Search(searchDefinition, new() { Sort = sort });

            if (projectionDefinition != null)
            {
                fluent = fluent.Project(projectionDefinition);
            }

            return fluent.Limit(1).Single();
        }

        private List<BsonDocument> SearchMultipleSynonymMapping(params SearchDefinition<Movie>[] clauses) =>
            GetSynonymTestCollection().Aggregate()
                .Search(Builders<Movie>.Search.Compound().Should(clauses), indexName: "synonyms-tests")
                .Project(Builders<Movie>.Projection.Include("Title").Exclude("_id"))
                .ToList();

        private IMongoCollection<HistoricalDocument> GetTestCollection() => _mongoClient
            .GetDatabase("sample_training")
            .GetCollection<HistoricalDocument>("posts");

        private IMongoCollection<T> GetTestCollection<T>() => _mongoClient
            .GetDatabase("sample_training")
            .GetCollection<T>("posts");

        private IMongoCollection<Movie> GetSynonymTestCollection() => _mongoClient
            .GetDatabase("sample_mflix")
            .GetCollection<Movie>("movies");

        private IMongoCollection<AirbnbListing> GetGeoTestCollection() => _mongoClient
            .GetDatabase("sample_airbnb")
            .GetCollection<AirbnbListing>("listingsAndReviews");

        private IMongoCollection<EmbeddedMovie> GetEmbeddedMoviesCollection() => _mongoClient
            .GetDatabase("sample_mflix")
            .GetCollection<EmbeddedMovie>("embedded_movies");

        [BsonIgnoreExtraElements]
        public class Comment
        {
            [BsonElement("author")]
            public string Author { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class Movie
        {
            [BsonElement("title")]
            public string Title { get; set; }

            [BsonElement("runtime")]
            public int Runtime { get; set; }

            [BsonElement("score")]
            public double Score { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class HistoricalDocumentWithCommentsOnly
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("comments")]
            public Comment[] Comments { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class HistoricalDocument
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("body")]
            public string Body { get; set; }

            [BsonElement("author")]
            public string Author { get; set; }

            [BsonElement("title")]
            public string Title { get; set; }

            [BsonElement("highlights")]
            public List<SearchHighlight> Highlights { get; set; }

            [BsonElement("score")]
            public double Score { get; set; }

            [BsonElement("date")]
            public DateTime Date { get; set; }

            [BsonElement("index")]
            public int Index { get; set; }

            [BsonElement("metaResult")]
            public SearchMetaResult MetaResult { get; set; }

            [BsonElement("scoreDetails")]
            public SearchScoreDetails ScoreDetails { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class Address
        {
            [BsonElement("location")]
            public GeoJsonObject<GeoJson2DGeographicCoordinates> Location { get; set; }

            [BsonElement("street")]
            public string Street { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class AirbnbListing
        {
            [BsonElement("address")]
            public Address Address { get; set; }

            [BsonElement("bedrooms")]
            public int Bedrooms { get; set; }

            [BsonElement("beds")]
            public int Beds { get; set; }

            [BsonElement("description")]
            public string Description { get; set; }

            [BsonElement("space")]
            public string Space { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }
        }

        [BsonIgnoreExtraElements]
        public class EmbeddedMovie
        {
            [BsonElement("title")]
            public string Title { get; set; }

            [BsonElement("plot_embedding")]
            public double[] Embedding { get; set; }

            [BsonElement("score")]
            public double Score { get; set; }
        }
    }
}
