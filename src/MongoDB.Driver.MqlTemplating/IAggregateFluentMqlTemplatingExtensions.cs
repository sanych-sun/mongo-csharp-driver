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
using MongoDB.Bson;

namespace MongoDB.Driver.MqlTemplating
{
    /// <summary>
    /// MQL-templating extension methods for <see cref="IAggregateFluent{TResult}"/>.
    /// </summary>
    public static class IAggregateFluentMqlTemplatingExtensions
    {
        /// <summary>
        /// Appends a $match stage using a filter built from a template.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="aggregate">The fluent aggregate interface.</param>
        /// <param name="filterTemplate">A JSON document template. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <returns>The fluent aggregate interface.</returns>
        public static IAggregateFluent<TResult> Match<TResult>(
            this IAggregateFluent<TResult> aggregate,
            string filterTemplate,
            object parameters)
        {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            return aggregate.Match(MqlTemplateParser.ParseFilter<TResult>(filterTemplate, parameters));
        }

        /// <summary>
        /// Appends a $project stage built from a template to the fluent pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the current result.</typeparam>
        /// <param name="aggregate">The fluent aggregate interface.</param>
        /// <param name="projectionTemplate">A JSON document template for the $project expression. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <returns>The fluent aggregate interface with output type <see cref="BsonDocument"/>.</returns>
        public static IAggregateFluent<BsonDocument> Project<TResult>(
            this IAggregateFluent<TResult> aggregate,
            string projectionTemplate,
            object parameters)
        {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            return aggregate.Project<BsonDocument>(MqlTemplateParser.ParseProjection<TResult>(projectionTemplate, parameters));
        }

        /// <summary>
        /// Appends a stage built from a template to the fluent pipeline.
        /// </summary>
        /// <typeparam name="TResult">The type of the current result.</typeparam>
        /// <param name="aggregate">The fluent aggregate interface.</param>
        /// <param name="stageTemplate">A JSON object representing a single pipeline stage. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <returns>The fluent aggregate interface with output type <see cref="BsonDocument"/>.</returns>
        public static IAggregateFluent<BsonDocument> AppendStage<TResult>(
            this IAggregateFluent<TResult> aggregate,
            string stageTemplate,
            object parameters)
        {
            if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));
            var stageDocument = MqlTemplateParser.ParseDocument(stageTemplate, parameters);
            PipelineStageDefinition<TResult, BsonDocument> stage = stageDocument;
            return aggregate.AppendStage(stage);
        }
    }
}
