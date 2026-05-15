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

using MongoDB.Bson.IO;

namespace MongoDB.Driver.MqlTemplating
{
    internal sealed class ExtendedJsonReaderBookmark : BsonReaderBookmark
    {
        private readonly BsonReaderBookmark _baseBookmark;
        private readonly JsonToken[] _injectedTokens;

        internal ExtendedJsonReaderBookmark(BsonReaderBookmark baseBookmark, JsonToken[] injectedTokens)
            : base(baseBookmark.State, baseBookmark.CurrentBsonType, baseBookmark.CurrentName)
        {
            _baseBookmark = baseBookmark;
            _injectedTokens = injectedTokens;
        }

        internal BsonReaderBookmark BaseBookmark => _baseBookmark;
        internal JsonToken[] InjectedTokens => _injectedTokens;
    }
}
