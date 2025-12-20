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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.OperationExecutors;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class CountOperation : ReadOperationBase<long, BsonDocument>
    {
        private TimeSpan? _maxTime;
        private ReadConcern _readConcern = ReadConcern.Default;

        public CountOperation(CollectionNamespace collectionNamespace)
            : base("count", collectionNamespace?.DatabaseNamespace, BsonDocumentSerializer.Instance)
        {
            CollectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        }

        public Collation Collation { get; init; }

        public CollectionNamespace CollectionNamespace { get; }

        public BsonValue Comment { get; init; }

        public BsonDocument Filter { get; init; }

        public BsonValue Hint { get; init; }

        public long? Limit { get; init; }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            init { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            init { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        public long? Skip { get; init; }

        public override BsonDocument CreateCommand(OperationContext operationContext, CommandExecutorContext context)
        {
            var readConcern = ReadConcernHelper.GetReadConcernForCommand(context.ChannelSource.Session, context.Channel.ConnectionDescription, _readConcern);
            return new BsonDocument
            {
                { "count", CollectionNamespace.CollectionName },
                { "query", Filter, Filter != null },
                { "limit", () => Limit.Value, Limit.HasValue },
                { "skip", () => Skip.Value, Skip.HasValue },
                { "hint", Hint, Hint != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(MaxTime.Value), MaxTime.HasValue && !operationContext.IsRootContextTimeoutConfigured() },
                { "collation", () => Collation.ToBsonDocument(), Collation != null },
                { "comment", Comment, Comment != null },
                { "readConcern", readConcern, readConcern != null }
            };
        }

        public override long HandleServerResponse(OperationContext operationContext, CommandExecutorContext context, BsonDocument serverResponse)
            => serverResponse["n"].ToInt64();
    }
}
