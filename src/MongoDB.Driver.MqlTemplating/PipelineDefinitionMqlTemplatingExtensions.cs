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
    /// MQL-templating extension methods for <see cref="PipelineDefinition{TInput, TOutput}"/>.
    /// </summary>
    public static class PipelineDefinitionMqlTemplatingExtensions
    {
        /// <summary>
        /// Appends a stage built from a template to the pipeline.
        /// </summary>
        /// <typeparam name="TInput">The type of the input documents.</typeparam>
        /// <typeparam name="TIntermediate">The type of the intermediate documents.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="stageTemplate">A JSON object representing a single pipeline stage. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <returns>A new pipeline with the appended stage whose output type is <see cref="BsonDocument"/>.</returns>
        public static PipelineDefinition<TInput, BsonDocument> AppendStage<TInput, TIntermediate>(
            this PipelineDefinition<TInput, TIntermediate> pipeline,
            string stageTemplate,
            object parameters)
        {
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            var stageDocument = MqlTemplateParser.ParseDocument(stageTemplate, parameters);
            PipelineStageDefinition<TIntermediate, BsonDocument> stage = stageDocument;
            return pipeline.AppendStage(stage);
        }
    }
}
