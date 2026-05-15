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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using static MongoDB.Driver.MqlTemplating.JsonTokens;

namespace MongoDB.Driver.MqlTemplating
{
    /// <summary>
    /// Represents a BSON reader for a JSON string template with @identifier placeholder substitution.
    /// Placeholders are sequences starting with '@' followed by an alphanumeric identifier that matches
    /// a property name on the parameters object passed to the constructor.
    /// </summary>
    public class ExtendedJsonReader : JsonReader
    {
        private static readonly Dictionary<string, Func<BsonValue, BsonValue>> __typeAnnotationHandlers =
            new Dictionary<string, Func<BsonValue, BsonValue>>(StringComparer.OrdinalIgnoreCase)
            {
                ["objectid"]  = ConvertToObjectId,
                ["oid"]       = ConvertToObjectId,
                ["date"]      = ConvertToDateTime,
                ["datetime"]  = ConvertToDateTime,
                ["decimal128"] = ConvertToDecimal128,
                ["decimal"]   = ConvertToDecimal128,
                ["int32"]     = ConvertToInt32,
                ["int"]       = ConvertToInt32,
                ["int64"]     = ConvertToInt64,
                ["long"]      = ConvertToInt64,
                ["double"]    = ConvertToDouble,
                ["string"]    = ConvertToString,
                ["bool"]      = ConvertToBoolean,
                ["boolean"]   = ConvertToBoolean,
            };

        private readonly IReadOnlyDictionary<string, BsonValue> _parameters;
        private Queue<JsonToken> _injectedTokens;

        /// <summary>
        /// Initializes a new instance of the ExtendedJsonReader class.
        /// </summary>
        /// <param name="json">The JSON template string, which may contain @identifier placeholders.</param>
        /// <param name="parameters">An object whose properties provide values for @identifier substitution.</param>
        public ExtendedJsonReader(string json, object parameters)
            : this(json, parameters, JsonReaderSettings.Defaults)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ExtendedJsonReader class.
        /// </summary>
        /// <param name="json">The JSON template string, which may contain @identifier placeholders.</param>
        /// <param name="parameters">An object whose properties provide values for @identifier substitution.</param>
        /// <param name="settings">The reader settings.</param>
        public ExtendedJsonReader(string json, object parameters, JsonReaderSettings settings)
            : base(json, settings)
        {
            _parameters = ParameterDictionaryBuilder.Build(parameters);
        }

        /// <inheritdoc/>
        public override BsonReaderBookmark GetBookmark()
        {
            var snapshot = _injectedTokens?.ToArray() ?? Array.Empty<JsonToken>();
            return new ExtendedJsonReaderBookmark(base.GetBookmark(), snapshot);
        }

        /// <inheritdoc/>
        public override void ReturnToBookmark(BsonReaderBookmark bookmark)
        {
            if (Disposed) { ThrowObjectDisposedException(); }
            var ejrb = (ExtendedJsonReaderBookmark)bookmark;
            base.ReturnToBookmark(ejrb.BaseBookmark);
            _injectedTokens?.Clear();
            var tokens = ejrb.InjectedTokens;
            if (tokens.Length > 0)
            {
                if (_injectedTokens == null) _injectedTokens = new Queue<JsonToken>(tokens.Length);
                foreach (var token in tokens)
                    _injectedTokens.Enqueue(token);
            }
        }

        /// <inheritdoc/>
        protected override JsonToken ScanNextToken()
        {
            if (_injectedTokens != null && _injectedTokens.Count > 0)
                return _injectedTokens.Dequeue();

            // Skip leading whitespace.
            while(TryConsumeNextChar(char.IsWhiteSpace))
            {}

            if (!TryConsumeNextChar(c => c == '@'))
                return base.ScanNextToken();

            // '@' was consumed. Two forms:
            //   @identifier           — plain parameter
            //   @(identifier:type)    — parameter with explicit BSON type annotation
            string identifier;
            string bsonTypeName = null;
            if (TryConsumeNextChar(c => c == '('))
            {
                // Annotated form: @(paramName:bsonType)
                var nameToken = base.ScanNextToken();
                if (nameToken.Type != JsonTokenType.UnquotedString)
                    throw new FormatException("JSON reader encountered '@(' not followed by a valid parameter name.");
                identifier = nameToken.Lexeme;

                var colonToken = base.ScanNextToken();
                if (colonToken.Type != JsonTokenType.Colon)
                    throw new FormatException($"JSON reader expected ':' after '@({identifier}' but found '{colonToken.Lexeme}'.");

                var typeToken = base.ScanNextToken();
                if (typeToken.Type != JsonTokenType.UnquotedString)
                    throw new FormatException($"JSON reader expected a BSON type name after '@({identifier}:' but found '{typeToken.Lexeme}'.");
                bsonTypeName = typeToken.Lexeme;

                while (TryConsumeNextChar(char.IsWhiteSpace)) {}
                if (!TryConsumeNextChar(c => c == ')'))
                    throw new FormatException($"JSON reader expected ')' to close '@({identifier}:{bsonTypeName}' but none was found.");
            }
            else
            {
                // Plain form: @identifier
                var identifierToken = base.ScanNextToken();
                if (identifierToken.Type != JsonTokenType.UnquotedString)
                    throw new FormatException("JSON reader encountered '@' not followed by a valid identifier.");
                identifier = identifierToken.Lexeme;
            }

            if (!_parameters.TryGetValue(identifier, out var bsonValue))
                throw new FormatException($"Parameter '@{identifier}' was not found in the provided parameters object.");

            if (bsonTypeName != null)
                bsonValue = ApplyTypeAnnotation(bsonValue, bsonTypeName, identifier);

            var singleToken = TryMakeSingleToken(bsonValue);
            if (singleToken != null)
                return singleToken;

            if (_injectedTokens == null) _injectedTokens = new Queue<JsonToken>();
            EnqueueTokensForBsonValue(bsonValue, _injectedTokens);
            return _injectedTokens.Dequeue();
        }

        private static BsonValue ApplyTypeAnnotation(BsonValue value, string typeName, string paramName)
        {
            if (!__typeAnnotationHandlers.TryGetValue(typeName, out var handler))
                throw new FormatException($"Unknown BSON type annotation '{typeName}' on parameter '@{paramName}'. Supported: objectId, oid, date, dateTime, decimal128, decimal, int32, int, int64, long, double, string, bool, boolean.");
            try
            {
                return handler(value);
            }
            catch (FormatException) { throw; }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to convert parameter '@{paramName}' to '{typeName}': {ex.Message}", ex);
            }
        }

        private static BsonValue ConvertToObjectId(BsonValue v)
            => v.BsonType == BsonType.ObjectId ? v : new BsonObjectId(ObjectId.Parse(v.AsString));

        private static BsonValue ConvertToDateTime(BsonValue v)
        {
            if (v.BsonType == BsonType.DateTime) return v;
            if (v.IsNumeric) return new BsonDateTime(v.ToInt64());
            return new BsonDateTime(DateTime.Parse(v.AsString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal));
        }

        private static BsonValue ConvertToDecimal128(BsonValue v)
        {
            if (v.BsonType == BsonType.Decimal128) return v;
            if (v.BsonType == BsonType.String) return new BsonDecimal128(Decimal128.Parse(v.AsString));
            if (v.IsNumeric) return new BsonDecimal128(new Decimal128(v.ToDouble()));
            throw new FormatException($"Cannot convert {v.BsonType} to Decimal128.");
        }

        private static BsonValue ConvertToInt32(BsonValue v)
            => v.BsonType == BsonType.Int32 ? v : new BsonInt32(v.ToInt32());

        private static BsonValue ConvertToInt64(BsonValue v)
            => v.BsonType == BsonType.Int64 ? v : new BsonInt64(v.ToInt64());

        private static BsonValue ConvertToDouble(BsonValue v)
            => v.BsonType == BsonType.Double ? v : new BsonDouble(v.ToDouble());

        private static BsonValue ConvertToString(BsonValue v)
            => v.BsonType == BsonType.String ? v : new BsonString(v.ToString());

        private static BsonValue ConvertToBoolean(BsonValue v)
            => v.BsonType == BsonType.Boolean ? v : BsonBoolean.Create(v.ToBoolean());

        private static void EnqueueTokensForBsonValue(BsonValue value, Queue<JsonToken> queue)
        {
            switch (value.BsonType)
            {
                case BsonType.Null:
                    queue.Enqueue(__tokenNull);
                    break;
                case BsonType.Undefined:
                    queue.Enqueue(__tokenUndefined);
                    break;
                case BsonType.Boolean:
                    queue.Enqueue(((BsonBoolean)value).Value ? __tokenTrue : __tokenFalse);
                    break;
                case BsonType.Int32:
                    var i32 = ((BsonInt32)value).Value;
                    queue.Enqueue(new Int32JsonToken(i32.ToString(CultureInfo.InvariantCulture), i32));
                    break;
                case BsonType.Int64:
                    var i64 = ((BsonInt64)value).Value;
                    queue.Enqueue(new Int64JsonToken(i64.ToString(CultureInfo.InvariantCulture), i64));
                    break;
                case BsonType.Double:
                    EnqueueDoubleToken(((BsonDouble)value).Value, queue);
                    break;
                case BsonType.Decimal128:
                    EnqueueExtendedJsonPair(queue, __keyNumberDecimal, MakeString(((BsonDecimal128)value).Value.ToString()));
                    break;
                case BsonType.String:
                    queue.Enqueue(MakeString(((BsonString)value).Value));
                    break;
                case BsonType.ObjectId:
                    EnqueueExtendedJsonPair(queue, __keyOid, MakeString(((BsonObjectId)value).Value.ToString()));
                    break;
                case BsonType.DateTime:
                    var millis = ((BsonDateTime)value).MillisecondsSinceEpoch;
                    EnqueueExtendedJsonPair(queue, __keyDate, new Int64JsonToken(millis.ToString(CultureInfo.InvariantCulture), millis));
                    break;
                case BsonType.RegularExpression:
                    var re = (BsonRegularExpression)value;
                    queue.Enqueue(new RegularExpressionJsonToken(re.Pattern, re));
                    break;
                case BsonType.MaxKey:
                    queue.Enqueue(__tokenMaxKey);
                    break;
                case BsonType.MinKey:
                    queue.Enqueue(__tokenMinKey);
                    break;
                case BsonType.Document:
                    EnqueueDocumentTokens((BsonDocument)value, queue);
                    break;
                case BsonType.Array:
                    EnqueueArrayTokens((BsonArray)value, queue);
                    break;
                case BsonType.Binary:
                    EnqueueBinaryDataTokens((BsonBinaryData)value, queue);
                    break;
                case BsonType.JavaScript:
                    EnqueueExtendedJsonPair(queue, __keyCode, MakeString(((BsonJavaScript)value).Code));
                    break;
                case BsonType.JavaScriptWithScope:
                    EnqueueJavaScriptWithScopeTokens((BsonJavaScriptWithScope)value, queue);
                    break;
                case BsonType.Symbol:
                    EnqueueExtendedJsonPair(queue, __keySymbol, MakeString(((BsonSymbol)value).Name));
                    break;
                case BsonType.Timestamp:
                    EnqueueTimestampTokens((BsonTimestamp)value, queue);
                    break;
                default:
                    throw new BsonInternalException($"Unexpected BsonType: {value.BsonType}.");
            }
        }

        private static void EnqueueDoubleToken(double d, Queue<JsonToken> queue)
        {
            if (double.IsNaN(d))
                queue.Enqueue(__tokenNaN);
            else if (double.IsPositiveInfinity(d))
                queue.Enqueue(__tokenInfinity);
            else if (double.IsNegativeInfinity(d))
                EnqueueExtendedJsonPair(queue, __keyNumberDouble, __tokenMinusInfinity);
            else
                queue.Enqueue(new DoubleJsonToken(d.ToString("R", CultureInfo.InvariantCulture), d));
        }

        private static void EnqueueDocumentTokens(BsonDocument doc, Queue<JsonToken> queue)
        {
            queue.Enqueue(__tokenBeginObject);
            var first = true;
            foreach (var element in doc)
            {
                if (!first) queue.Enqueue(__tokenComma);
                first = false;
                queue.Enqueue(MakeString(element.Name));
                queue.Enqueue(__tokenColon);
                EnqueueTokensForBsonValue(element.Value, queue);
            }
            queue.Enqueue(__tokenEndObject);
        }

        private static void EnqueueArrayTokens(BsonArray array, Queue<JsonToken> queue)
        {
            queue.Enqueue(__tokenBeginArray);
            var first = true;
            foreach (var element in array)
            {
                if (!first) queue.Enqueue(__tokenComma);
                first = false;
                EnqueueTokensForBsonValue(element, queue);
            }
            queue.Enqueue(__tokenEndArray);
        }

        private static void EnqueueBinaryDataTokens(BsonBinaryData binaryData, Queue<JsonToken> queue)
        {
            // { "$binary": { "base64": "...", "subType": "xx" } }
            var base64 = Convert.ToBase64String(binaryData.Bytes);
            var subType = ((int)binaryData.SubType).ToString("x2");
            queue.Enqueue(__tokenBeginObject);
            queue.Enqueue(__keyBinary);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(__tokenBeginObject);
            queue.Enqueue(__keyBase64);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(MakeString(base64));
            queue.Enqueue(__tokenComma);
            queue.Enqueue(__keySubType);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(MakeString(subType));
            queue.Enqueue(__tokenEndObject);
            queue.Enqueue(__tokenEndObject);
        }

        private static void EnqueueTimestampTokens(BsonTimestamp timestamp, Queue<JsonToken> queue)
        {
            // { "$timestamp": { "t": t, "i": i } }
            queue.Enqueue(__tokenBeginObject);
            queue.Enqueue(__keyTimestamp);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(__tokenBeginObject);
            queue.Enqueue(__keyT);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(new Int32JsonToken(timestamp.Timestamp.ToString(CultureInfo.InvariantCulture), timestamp.Timestamp));
            queue.Enqueue(__tokenComma);
            queue.Enqueue(__keyI);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(new Int32JsonToken(timestamp.Increment.ToString(CultureInfo.InvariantCulture), timestamp.Increment));
            queue.Enqueue(__tokenEndObject);
            queue.Enqueue(__tokenEndObject);
        }

        private static void EnqueueJavaScriptWithScopeTokens(BsonJavaScriptWithScope jsWithScope, Queue<JsonToken> queue)
        {
            // { "$code": "code", "$scope": { ... } }
            queue.Enqueue(__tokenBeginObject);
            queue.Enqueue(__keyCode);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(MakeString(jsWithScope.Code));
            queue.Enqueue(__tokenComma);
            queue.Enqueue(__keyScope);
            queue.Enqueue(__tokenColon);
            EnqueueDocumentTokens(jsWithScope.Scope, queue);
            queue.Enqueue(__tokenEndObject);
        }

        private static void EnqueueExtendedJsonPair(Queue<JsonToken> queue, StringJsonToken keyToken, JsonToken valueToken)
        {
            queue.Enqueue(__tokenBeginObject);
            queue.Enqueue(keyToken);
            queue.Enqueue(__tokenColon);
            queue.Enqueue(valueToken);
            queue.Enqueue(__tokenEndObject);
        }
    }
}
