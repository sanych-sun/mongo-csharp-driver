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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.OperationExecutors;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class DistinctOperation<TValue> : ReadOperationBase<IAsyncCursor<TValue>, DistinctOperation<TValue>.DistinctResult>
    {
        private TimeSpan? _maxTime;
        private ReadConcern _readConcern = ReadConcern.Default;
        private IBsonSerializer<TValue> _valueSerializer;

        public DistinctOperation(CollectionNamespace collectionNamespace, IBsonSerializer<TValue> valueSerializer, string fieldName)
            : base("distinct", collectionNamespace?.DatabaseNamespace, new DistinctResultDeserializer(valueSerializer))
        {
            CollectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _valueSerializer = Ensure.IsNotNull(valueSerializer, nameof(valueSerializer));
            FieldName = Ensure.IsNotNullOrEmpty(fieldName, nameof(fieldName));
        }

        public Collation Collation { get; set; }

        public BsonValue Comment { get; set; }

        public CollectionNamespace CollectionNamespace { get; }

        public BsonDocument Filter { get; set; }

        public string FieldName { get; }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        public IBsonSerializer<TValue> ValueSerializer
        {
            get { return _valueSerializer; }
        }

        public override BsonDocument CreateCommand(OperationContext operationContext, CommandExecutorContext context)
        {
            var readConcern = ReadConcernHelper.GetReadConcernForCommand(context.Session, context.ConnectionDescription, _readConcern);
            return new BsonDocument
            {
                { "distinct", CollectionNamespace.CollectionName },
                { "key", FieldName },
                { "query", Filter, Filter != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue && !operationContext.IsRootContextTimeoutConfigured() },
                { "collation", () => Collation.ToBsonDocument(), Collation != null },
                { "comment", Comment, Comment != null },
                { "readConcern", readConcern, readConcern != null }
            };
        }

        public override IAsyncCursor<TValue> HandleServerResponse(OperationContext operationContext, CommandExecutorContext context, DistinctResult serverResponse)
        {
            context.Session.SetSnapshotTimeIfNeeded(serverResponse.AtClusterTime);
            return new SingleBatchAsyncCursor<TValue>(serverResponse.Values);
        }

        internal sealed class DistinctResult
        {
            public BsonTimestamp AtClusterTime;
            public TValue[] Values;
        }

        internal sealed class DistinctResultDeserializer : SerializerBase<DistinctResult>
        {
            private readonly IBsonSerializer<TValue> _valueSerializer;

            public DistinctResultDeserializer(IBsonSerializer<TValue> valuesSerializer)
            {
                _valueSerializer = valuesSerializer;
            }

            public override DistinctResult Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                var result = new DistinctResult();
                reader.ReadStartDocument();
                while (reader.ReadBsonType() != 0)
                {
                    var elementName = reader.ReadName();
                    switch (elementName)
                    {
                        case "atClusterTime":
                            result.AtClusterTime = BsonTimestampSerializer.Instance.Deserialize(context);
                            break;

                        case "values":
                            var arraySerializer = new ArraySerializer<TValue>(_valueSerializer);
                            result.Values = arraySerializer.Deserialize(context);
                            break;

                        default:
                            reader.SkipValue();
                            break;
                    }
                }
                reader.ReadEndDocument();
                return result;
            }

            public override bool Equals(object obj)
            {
                if (object.ReferenceEquals(obj, null)) { return false; }
                if (object.ReferenceEquals(this, obj)) { return true; }
                return
                    base.Equals(obj) &&
                    obj is DistinctResultDeserializer other &&
                    object.Equals(_valueSerializer, other._valueSerializer);
            }

            public override int GetHashCode() => 0;
        }
    }
}
