
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class NullableIntWithCustomSerializerTests
    {
        private static readonly CSerializer __documentSerializer = new CSerializer();
        private static readonly RenderArgs<C> __args = new(__documentSerializer, BsonSerializer.SerializerRegistry);
        private static readonly FilterDefinitionBuilder<C> __subject = Builders<C>.Filter;

        [Fact]
        public void Round_trip_with_null_value_should_work()
        {
            var document = new C { Id = 1, Value = null };
            var json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell }, serializer: __documentSerializer);
            json.Should().Be("{ \"_id\" : 1, \"Value\" : null }");

            var bson = document.ToBson(serializer: __documentSerializer);
            var rehydrated = BsonSerializer.Deserialize<C>(bson, configurator: b => { });
            rehydrated.Value.Should().NotHaveValue();
        }

        [Fact]
        public void Round_trip_with_non_null_value_should_work()
        {
            var document = new C { Id = 1, Value = 42 };
            var json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell }, serializer: __documentSerializer);
            json.Should().Be("{ \"_id\" : 1, \"Value\" : 42 }");

            var bson = document.ToBson(serializer: __documentSerializer);
            var rehydrated = BsonSerializer.Deserialize<C>(bson, configurator: b => { });
            rehydrated.Value.Should().Be(42);
        }

        [Fact]
        public void Eq_with_non_null_value_should_render_correctly()
        {
            var filter = __subject.Eq(x => x.Value, 42);

            filter.Render(__args).Should().Be("{ Value : 42 }");
        }

        [Fact]
        public void Eq_with_null_value_should_render_correctly()
        {
            var filter = __subject.Eq(x => x.Value, null);

            filter.Render(__args).Should().Be("{ Value : null }");
        }

        [Fact]
        public void Gt_with_non_null_value_should_render_correctly()
        {
            var filter = __subject.Gt(x => x.Value, 10);

            filter.Render(__args).Should().Be("{ Value : { $gt : 10 } }");
        }

        [Fact]
        public void Ne_with_null_value_should_render_correctly()
        {
            var filter = __subject.Ne(x => x.Value, null);

            filter.Render(__args).Should().Be("{ Value : { $ne : null } }");
        }

        [Fact]
        public void Where_operator_equals_with_non_null_value_should_render_correctly()
        {
            var filter = __subject.Where(x => x.Value == 42);

            filter.Render(__args).Should().Be("{ Value : 42 }");
        }

        [Fact]
        public void Where_operator_not_equals_with_non_null_value_should_render_correctly()
        {
            var filter = __subject.Where(x => x.Value != 42);

            filter.Render(__args).Should().Be("{ Value : { $ne : 42 } }");
        }

        public class C
        {
            public int Id { get; set; }
            public int? Value { get; set; }
        }

        /// <summary>
        /// A custom document serializer for C that resolves member serializers.
        /// For the nullable int? Value member, it returns a non-nullable IBsonSerializer{int}
        /// that handles null through the non-generic IBsonSerializer interface.
        /// </summary>
        private class CSerializer : IBsonSerializer<C>, IBsonDocumentSerializer
        {
            private static readonly IBsonSerializer __valueSerializer = BsonSerializer.LookupSerializer<int>();

            public Type ValueType => typeof(C);

            public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
            {
                switch (memberName)
                {
                    case "Id":
                        serializationInfo = new BsonSerializationInfo("_id", Int32Serializer.Instance, typeof(int));
                        return true;
                    case "Value":
                        serializationInfo = new BsonSerializationInfo("Value", __valueSerializer, typeof(int?));
                        return true;
                    default:
                        serializationInfo = null;
                        return false;
                }
            }

            public C Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                var result = new C();

                reader.ReadStartDocument();
                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var name = reader.ReadName();
                    switch (name)
                    {
                        case "_id":
                            result.Id = reader.ReadInt32();
                            break;
                        case "Value":
                            if (reader.CurrentBsonType == BsonType.Null)
                            {
                                reader.ReadNull();
                                result.Value = null;
                            }
                            else
                            {
                                result.Value = reader.ReadInt32();
                            }
                            break;
                        default:
                            reader.SkipValue();
                            break;
                    }
                }
                reader.ReadEndDocument();

                return result;
            }

            public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, C value)
            {
                var writer = context.Writer;
                writer.WriteStartDocument();
                writer.WriteName("_id");
                writer.WriteInt32(value.Id);
                writer.WriteName("Value");
                if (value.Value == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    writer.WriteInt32(value.Value.Value);
                }
                writer.WriteEndDocument();
            }

            object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
                => Deserialize(context, args);

            void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
                => Serialize(context, args, (C)value);
        }
    }
}
