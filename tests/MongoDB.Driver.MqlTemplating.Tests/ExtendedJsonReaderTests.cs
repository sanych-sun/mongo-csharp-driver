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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Driver.MqlTemplating.Tests
{
    public class ExtendedJsonReaderTests
    {
        public static object[][] ParameterSubstitutionTestCases() =>
        [
            [42, (BsonInt32)42],
            [long.MaxValue, (BsonInt64)long.MaxValue],
            [3.14, (BsonDouble)3.14],
            [double.NaN, (BsonDouble)double.NaN],
            [double.PositiveInfinity, (BsonDouble)double.PositiveInfinity],
            [double.NegativeInfinity, (BsonDouble)double.NegativeInfinity],
            [true, BsonBoolean.True],
            [false, BsonBoolean.False],
            ["hello world", (BsonString)"hello world"],
            [BsonNull.Value, BsonNull.Value],
            [ObjectId.Parse("507f1f77bcf86cd799439011"), (BsonObjectId)ObjectId.Parse("507f1f77bcf86cd799439011")],
            [new BsonDateTime(1234567890000L), new BsonDateTime(1234567890000L)],
            [new BsonDecimal128(Decimal128.Parse("1.23456789")), new BsonDecimal128(Decimal128.Parse("1.23456789"))],
            [new BsonRegularExpression("abc.*", "i"), new BsonRegularExpression("abc.*", "i")],
            [new BsonTimestamp(12345, 67), new BsonTimestamp(12345, 67)],
            [new BsonBinaryData(new byte[] { 1, 2, 3, 4 }, BsonBinarySubType.Binary), new BsonBinaryData(new byte[] { 1, 2, 3, 4 }, BsonBinarySubType.Binary)],
            [BsonMaxKey.Value, BsonMaxKey.Value],
            [BsonMinKey.Value, BsonMinKey.Value],
            [new BsonDocument { { "a", 1 }, { "b", "two" } }, new BsonDocument { { "a", 1 }, { "b", "two" } }],
            [new BsonArray { 1, "two", 3.0 }, new BsonArray { 1, "two", 3.0 }],
        ];

        [Theory]
        [MemberData(nameof(ParameterSubstitutionTestCases))]
        public void Parameter_is_substituted(object paramValue, BsonValue expected)
        {
            var result = Parse("{ 'x': @val }", new { val = paramValue });
            result["x"].Should().Be(expected);
        }

        [Fact]
        public void Multiple_parameters_are_substituted()
        {
            var oid = ObjectId.Parse("507f1f77bcf86cd799439011");
            var result = Parse(
                "{ 'a': @a, 'b': @b, 'c': @c }",
                new { a = 1, b = "hello", c = oid });

            result["a"].Should().Be((BsonInt32)1);
            result["b"].Should().Be((BsonString)"hello");
            result["c"].Should().Be((BsonObjectId)oid);
        }

        [Fact]
        public void Same_parameter_used_twice_works()
        {
            var result = Parse("{ 'x': @val, 'y': @val }", new { val = 42 });
            result["x"].Should().Be((BsonInt32)42);
            result["y"].Should().Be((BsonInt32)42);
        }

        [Fact]
        public void Parameters_mixed_with_literal_values_work()
        {
            var result = Parse("{ 'a': @a, 'b': 100, 'c': 'literal' }", new { a = 42 });
            result["a"].Should().Be((BsonInt32)42);
            result["b"].Should().Be((BsonInt32)100);
            result["c"].Should().Be((BsonString)"literal");
        }

        public static object[][] ValidParameterNameTestCases() =>
        [
            ["{ 'x': @lower }", (object)new { lower = 42 }],
            ["{ 'x': @UPPER }", (object)new { UPPER = 42 }],
            ["{ 'x': @_leadingUnderscore }", (object)new { _leadingUnderscore = 42 }],
            ["{ 'x': @trailingUnderscore_ }", (object)new { trailingUnderscore_ = 42 }],
            ["{ 'x': @withDigit9 }", (object)new { withDigit9 = 42 }],
            ["{ 'x': @_aA1 }", (object)new { _aA1 = 42 }],
        ];

        [Theory]
        [MemberData(nameof(ValidParameterNameTestCases))]
        public void Parameter_name_with_valid_characters_is_substituted(string json, object parameters)
        {
            var result = Parse(json, parameters);
            result["x"].Should().Be((BsonInt32)42);
        }

        [Theory]
        [InlineData("{ 'x': @1startWithDigit }")]  // digit is not a valid identifier start
        [InlineData("{ 'x': @a$b }")]              // $ terminates identifier; a$b not a valid C# property
        [InlineData("{ 'x': @a@b }")]              // @ terminates identifier; a not found
        public void Parameter_name_with_invalid_characters_throws_FormatException(string json)
        {
            Record.Exception(() => Parse(json, new { })).Should().BeOfType<FormatException>();
        }

        [Fact]
        public void Whitespace_before_at_sign_is_handled()
        {
            var result = Parse("{ 'x' :   @val }", new { val = 99 });
            result["x"].Should().Be((BsonInt32)99);
        }

        [Fact]
        public void Missing_parameter_throws_FormatException()
        {
            Record.Exception(() => Parse("{ 'x': @missing }", new { other = 1 }))
                .Should().BeOfType<FormatException>()
                .Which.Message.Should().Contain("@missing");
        }

        [Fact]
        public void At_sign_not_followed_by_identifier_throws_FormatException()
        {
            Record.Exception(() => Parse("{ 'x': @123 }", new { })).Should().BeOfType<FormatException>();
        }

        [Fact]
        public void Bookmark_restore_replays_substituted_value()
        {
            using var reader = new ExtendedJsonReader("{ 'x': @val }", new { val = 42 });

            reader.ReadStartDocument();
            var bookmark = reader.GetBookmark();

            reader.ReadBsonType();
            var name1 = reader.ReadName();
            var value1 = reader.ReadInt32();

            reader.ReturnToBookmark(bookmark);

            reader.ReadBsonType();
            var name2 = reader.ReadName();
            var value2 = reader.ReadInt32();

            name1.Should().Be(name2);
            value1.Should().Be(value2);
        }

        [Fact]
        public void Bookmark_mid_injected_tokens_is_restored_correctly()
        {
            // ObjectId substitution produces multiple injected tokens; bookmark should capture them
            var oid = ObjectId.Parse("507f1f77bcf86cd799439011");
            using var reader = new ExtendedJsonReader("{ 'x': @val }", new { val = oid });

            reader.ReadStartDocument();
            var bookmark = reader.GetBookmark();

            reader.ReadBsonType();
            var name1 = reader.ReadName();
            var value1 = reader.ReadObjectId();

            reader.ReturnToBookmark(bookmark);

            reader.ReadBsonType();
            var name2 = reader.ReadName();
            var value2 = reader.ReadObjectId();

            name1.Should().Be(name2);
            value1.Should().Be(value2);
        }

        // ── Type annotation syntax: @(paramName:bsonType) ──────────────────────

        [Fact]
        public void TypeAnnotation_objectId_converts_string_to_ObjectId()
        {
            var hex = "507f1f77bcf86cd799439011";
            var result = Parse("{ 'id': @(id:objectId) }", new { id = hex });
            result["id"].BsonType.Should().Be(BsonType.ObjectId);
            result["id"].AsObjectId.ToString().Should().Be(hex);
        }

        [Fact]
        public void TypeAnnotation_oid_alias_converts_string_to_ObjectId()
        {
            var hex = "507f1f77bcf86cd799439011";
            var result = Parse("{ 'id': @(id:oid) }", new { id = hex });
            result["id"].BsonType.Should().Be(BsonType.ObjectId);
        }

        [Fact]
        public void TypeAnnotation_date_converts_long_millis_to_DateTime()
        {
            var result = Parse("{ 'ts': @(ts:date) }", new { ts = 1234567890000L });
            result["ts"].BsonType.Should().Be(BsonType.DateTime);
            result["ts"].AsBsonDateTime.MillisecondsSinceEpoch.Should().Be(1234567890000L);
        }

        [Fact]
        public void TypeAnnotation_date_converts_iso_string_to_DateTime()
        {
            var result = Parse("{ 'ts': @(ts:date) }", new { ts = "2009-02-13T23:31:30Z" });
            result["ts"].BsonType.Should().Be(BsonType.DateTime);
            result["ts"].AsBsonDateTime.MillisecondsSinceEpoch.Should().Be(1234567890000L);
        }

        [Fact]
        public void TypeAnnotation_decimal128_converts_string_to_Decimal128()
        {
            var result = Parse("{ 'price': @(price:decimal128) }", new { price = "1.99" });
            result["price"].BsonType.Should().Be(BsonType.Decimal128);
            result["price"].AsDecimal128.Should().Be(Decimal128.Parse("1.99"));
        }

        [Fact]
        public void TypeAnnotation_int32_converts_int64_to_Int32()
        {
            var result = Parse("{ 'n': @(n:int32) }", new { n = 42L });
            result["n"].BsonType.Should().Be(BsonType.Int32);
            result["n"].AsInt32.Should().Be(42);
        }

        [Fact]
        public void TypeAnnotation_int64_converts_int_to_Int64()
        {
            var result = Parse("{ 'n': @(n:int64) }", new { n = 42 });
            result["n"].BsonType.Should().Be(BsonType.Int64);
            result["n"].AsInt64.Should().Be(42L);
        }

        [Fact]
        public void TypeAnnotation_double_converts_int_to_Double()
        {
            var result = Parse("{ 'n': @(n:double) }", new { n = 42 });
            result["n"].BsonType.Should().Be(BsonType.Double);
            result["n"].AsDouble.Should().Be(42.0);
        }

        [Fact]
        public void TypeAnnotation_string_converts_int_to_String()
        {
            var result = Parse("{ 'v': @(v:string) }", new { v = 99 });
            result["v"].BsonType.Should().Be(BsonType.String);
            result["v"].AsString.Should().Be("99");
        }

        [Fact]
        public void TypeAnnotation_bool_converts_numeric_to_Boolean()
        {
            var result = Parse("{ 'v': @(v:bool) }", new { v = 1 });
            result["v"].BsonType.Should().Be(BsonType.Boolean);
            result["v"].AsBoolean.Should().BeTrue();
        }

        [Fact]
        public void TypeAnnotation_type_name_is_case_insensitive()
        {
            var hex = "507f1f77bcf86cd799439011";
            var result = Parse("{ 'id': @(id:OBJECTID) }", new { id = hex });
            result["id"].BsonType.Should().Be(BsonType.ObjectId);
        }

        [Fact]
        public void TypeAnnotation_no_op_when_value_already_correct_type()
        {
            var oid = ObjectId.Parse("507f1f77bcf86cd799439011");
            var result = Parse("{ 'id': @(id:objectId) }", new { id = oid });
            result["id"].AsObjectId.Should().Be(oid);
        }

        [Fact]
        public void TypeAnnotation_and_plain_parameter_can_mix_in_same_template()
        {
            var hex = "507f1f77bcf86cd799439011";
            var result = Parse("{ 'id': @(id:objectId), 'status': @s }", new { id = hex, s = "active" });
            result["id"].BsonType.Should().Be(BsonType.ObjectId);
            result["status"].AsString.Should().Be("active");
        }

        [Fact]
        public void TypeAnnotation_unknown_type_name_throws_FormatException()
        {
            Record.Exception(() => Parse("{ 'x': @(v:nosuchtype) }", new { v = 1 }))
                .Should().BeOfType<FormatException>()
                .Which.Message.Should().Contain("nosuchtype");
        }

        [Fact]
        public void TypeAnnotation_missing_colon_throws_FormatException()
        {
            Record.Exception(() => Parse("{ 'x': @(v) }", new { v = 1 }))
                .Should().BeOfType<FormatException>();
        }

        [Fact]
        public void TypeAnnotation_missing_closing_paren_throws_FormatException()
        {
            Record.Exception(() => Parse("{ 'x': @(v:int32 }", new { v = 1 }))
                .Should().BeOfType<FormatException>();
        }

        private static BsonDocument Parse(string json, object parameters = null)
        {
            using var reader = new ExtendedJsonReader(json, parameters);
            return BsonSerializer.Deserialize<BsonDocument>(reader);
        }
    }
}
