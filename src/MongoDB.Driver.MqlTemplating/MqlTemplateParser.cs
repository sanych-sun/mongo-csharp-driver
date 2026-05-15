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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.MqlTemplating
{
    internal static class MqlTemplateParser
    {
        internal static PipelineDefinition<TInput, TOutput> ParsePipeline<TInput, TOutput>(
            string template,
            object parameters)
        {
            using var reader = new ExtendedJsonReader(template, parameters);
            return PipelineDefinition<TInput, TOutput>.Create(
                BsonSerializer.Deserialize<BsonArray>(reader).Cast<BsonDocument>());
        }

        internal static BsonDocument ParseDocument(string template, object parameters)
        {
            using var reader = new ExtendedJsonReader(template, parameters);
            return BsonSerializer.Deserialize<BsonDocument>(reader);
        }

        internal static FilterDefinition<TDocument> ParseFilter<TDocument>(string template, object parameters)
        {
            BsonDocument doc = ParseDocument(template, parameters);
            return doc; // implicit conversion BsonDocument → FilterDefinition<TDocument>
        }

        internal static UpdateDefinition<TDocument> ParseUpdate<TDocument>(string template, object parameters)
        {
            BsonDocument doc = ParseDocument(template, parameters);
            return doc; // implicit conversion BsonDocument → UpdateDefinition<TDocument>
        }

        internal static ProjectionDefinition<TSource> ParseProjection<TSource>(string template, object parameters)
        {
            BsonDocument doc = ParseDocument(template, parameters);
            return doc; // implicit conversion BsonDocument → ProjectionDefinition<TSource>
        }
    }
}
