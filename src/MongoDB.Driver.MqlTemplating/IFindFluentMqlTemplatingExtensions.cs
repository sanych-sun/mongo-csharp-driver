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
    /// MQL-templating extension methods for <see cref="IFindFluent{TDocument,TProjection}"/>.
    /// </summary>
    public static class IFindFluentMqlTemplatingExtensions
    {
        /// <summary>
        /// Projects the result using an expression built from a template.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TProjection">The type of the current projection.</typeparam>
        /// <param name="find">The fluent find interface.</param>
        /// <param name="projectionTemplate">A JSON document template for the projection expression. Use @identifier placeholders for parameter substitution.</param>
        /// <param name="parameters">An object whose public properties supply values for @identifier placeholders.</param>
        /// <returns>The fluent find interface with output type <see cref="BsonDocument"/>.</returns>
        public static IFindFluent<TDocument, BsonDocument> Project<TDocument, TProjection>(
            this IFindFluent<TDocument, TProjection> find,
            string projectionTemplate,
            object parameters)
        {
            if (find == null) throw new ArgumentNullException(nameof(find));
            return find.Project<BsonDocument>(MqlTemplateParser.ParseProjection<TDocument>(projectionTemplate, parameters));
        }
    }
}
