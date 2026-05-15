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

using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.MqlTemplating
{
    // Cannot be named JsonToken — that name is taken by MongoDB.Bson.IO.JsonToken.
    internal static class JsonTokens
    {
        // Unquoted-string value tokens
        internal static readonly StringJsonToken __tokenNull      = MakeUnquotedString("null");
        internal static readonly StringJsonToken __tokenUndefined = MakeUnquotedString("undefined");
        internal static readonly StringJsonToken __tokenTrue      = MakeUnquotedString("true");
        internal static readonly StringJsonToken __tokenFalse     = MakeUnquotedString("false");
        internal static readonly StringJsonToken __tokenNaN       = MakeUnquotedString("NaN");
        internal static readonly StringJsonToken __tokenInfinity  = MakeUnquotedString("Infinity");
        internal static readonly StringJsonToken __tokenMaxKey    = MakeUnquotedString("MaxKey");
        internal static readonly StringJsonToken __tokenMinKey    = MakeUnquotedString("MinKey");

        // Structural tokens
        internal static readonly JsonToken __tokenBeginObject = new JsonToken(JsonTokenType.BeginObject, "{");
        internal static readonly JsonToken __tokenEndObject   = new JsonToken(JsonTokenType.EndObject,   "}");
        internal static readonly JsonToken __tokenBeginArray  = new JsonToken(JsonTokenType.BeginArray,  "[");
        internal static readonly JsonToken __tokenEndArray    = new JsonToken(JsonTokenType.EndArray,    "]");
        internal static readonly JsonToken __tokenColon       = new JsonToken(JsonTokenType.Colon,       ":");
        internal static readonly JsonToken __tokenComma       = new JsonToken(JsonTokenType.Comma,       ",");

        // Extended JSON key string tokens
        internal static readonly StringJsonToken __keyOid           = MakeString("$oid");
        internal static readonly StringJsonToken __keyDate          = MakeString("$date");
        internal static readonly StringJsonToken __keyNumberDecimal = MakeString("$numberDecimal");
        internal static readonly StringJsonToken __keyNumberDouble  = MakeString("$numberDouble");
        internal static readonly StringJsonToken __keyCode          = MakeString("$code");
        internal static readonly StringJsonToken __keyScope         = MakeString("$scope");
        internal static readonly StringJsonToken __keySymbol        = MakeString("$symbol");
        internal static readonly StringJsonToken __keyBinary        = MakeString("$binary");
        internal static readonly StringJsonToken __keyBase64        = MakeString("base64");
        internal static readonly StringJsonToken __keySubType       = MakeString("subType");
        internal static readonly StringJsonToken __keyTimestamp     = MakeString("$timestamp");
        internal static readonly StringJsonToken __keyT             = MakeString("t");
        internal static readonly StringJsonToken __keyI             = MakeString("i");

        // Extended JSON value tokens that are also constants
        internal static readonly StringJsonToken __tokenMinusInfinity = MakeString("-Infinity");

        internal static StringJsonToken MakeString(string value) =>
            new StringJsonToken(JsonTokenType.String, value, value);

        internal static StringJsonToken MakeUnquotedString(string lexeme) =>
            new StringJsonToken(JsonTokenType.UnquotedString, lexeme, lexeme);

        internal static JsonToken TryMakeSingleToken(BsonValue value)
        {
            switch (value.BsonType)
            {
                case BsonType.Null:      return __tokenNull;
                case BsonType.Undefined: return __tokenUndefined;
                case BsonType.Boolean:   return ((BsonBoolean)value).Value ? __tokenTrue : __tokenFalse;
                case BsonType.MaxKey:    return __tokenMaxKey;
                case BsonType.MinKey:    return __tokenMinKey;
                case BsonType.Int32:
                    var i32 = ((BsonInt32)value).Value;
                    return new Int32JsonToken(i32.ToString(CultureInfo.InvariantCulture), i32);
                case BsonType.Int64:
                    var i64 = ((BsonInt64)value).Value;
                    return new Int64JsonToken(i64.ToString(CultureInfo.InvariantCulture), i64);
                case BsonType.Double:
                    var d = ((BsonDouble)value).Value;
                    if (double.IsNaN(d))              return __tokenNaN;
                    if (double.IsPositiveInfinity(d)) return __tokenInfinity;
                    if (double.IsNegativeInfinity(d)) return null; // needs extended JSON pair — falls through to queue
                    return new DoubleJsonToken(d.ToString("R", CultureInfo.InvariantCulture), d);
                case BsonType.String:
                    return MakeString(((BsonString)value).Value);
                case BsonType.RegularExpression:
                    var re = (BsonRegularExpression)value;
                    return new RegularExpressionJsonToken(re.Pattern, re);
                default:
                    return null; // Document, Array, Binary, ObjectId, DateTime, etc. — need the queue
            }
        }
    }
}
