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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class FindOperation<TDocument> : ReadOperationBase<IAsyncCursor<TDocument>, BsonDocument>
    {
        #region static
        // private static fields
        private static IBsonSerializer<BsonDocument> __findCommandResultSerializer = new PartiallyRawBsonDocumentSerializer(
            "cursor", new PartiallyRawBsonDocumentSerializer(
                "firstBatch", new RawBsonArraySerializer()));
        #endregion

        // fields
        private int? _batchSize;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool? _oplogReplay;
        private ReadConcern _readConcern = ReadConcern.Default;
        private bool _retryRequested;
        private bool? _singleBatch;
        private int? _skip;
        private BsonDocument _sort;

        public FindOperation(
            CollectionNamespace collectionNamespace,
            IBsonSerializer<TDocument> resultSerializer)
            : base("find", collectionNamespace?.DatabaseNamespace, BsonDocumentSerializer.Instance)
        {
            CollectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            ItemSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            CursorType = CursorType.NonTailable;
        }

        public bool? AllowDiskUse { get; set; }

        public bool? AllowPartialResults { get; set; }

        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public Collation Collation { get; set; }

        public CollectionNamespace CollectionNamespace { get; }

        public BsonValue Comment { get; set; }

        public CursorType CursorType { get; set; }

        public BsonDocument Filter { get; set; }

        public BsonValue Hint { get; set; }

        public BsonDocument Let { get; set; }

        public int? Limit { get; set; }

        public BsonDocument Max { get; set; }

        public TimeSpan? MaxAwaitTime { get; set; }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public BsonDocument Min { get; set; }

        public bool? NoCursorTimeout { get; set; }

        [Obsolete("OplogReplay is ignored by server versions 4.4.0 and newer.")]
        public bool? OplogReplay
        {
            get { return _oplogReplay; }
            set { _oplogReplay = value; }
        }

        public BsonDocument Projection { get; set; }

        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        public IBsonSerializer<TDocument> ItemSerializer { get; }

        public bool? ReturnKey { get; set; }

        public bool? ShowRecordId { get; set; }

        public bool? SingleBatch
        {
            get { return _singleBatch; }
            set { _singleBatch = value; }
        }

        public int? Skip
        {
            get { return _skip; }
            set { _skip = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public BsonDocument CreateCommand(OperationContext operationContext, ICoreSession session, ConnectionDescription connectionDescription)
        {
            var wireVersion = connectionDescription.MaxWireVersion;
            FindProjectionChecker.ThrowIfAggregationExpressionIsUsedWhenNotSupported(Projection, wireVersion);

            var batchSize = _batchSize;
            // https://github.com/mongodb/specifications/blob/668992950d975d3163e538849dd20383a214fc37/source/crud/crud.md?plain=1#L803
            if (batchSize.HasValue && batchSize == Limit)
            {
                batchSize = Limit + 1;
            }

            var isShardRouter = connectionDescription.HelloResult.ServerType == ServerType.ShardRouter;
            var readConcern = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern);
            return new BsonDocument
            {
                { "find", CollectionNamespace.CollectionName },
                { "filter", Filter, Filter != null },
                { "sort", _sort, _sort != null },
                { "projection", Projection, Projection != null },
                { "hint", Hint, Hint != null },
                { "skip", () => _skip.Value, _skip.HasValue },
                { "limit", () => Math.Abs(Limit.Value), Limit.HasValue && Limit != 0 },
                { "batchSize", () => batchSize.Value, batchSize.HasValue && batchSize > 0 },
                { "singleBatch", () => Limit < 0 || _singleBatch.Value, Limit < 0 || _singleBatch.HasValue },
                { "comment", Comment, Comment != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue && !operationContext.IsRootContextTimeoutConfigured() },
                { "max", Max, Max != null },
                { "min", Min, Min != null },
                { "returnKey", () => ReturnKey.Value, ReturnKey.HasValue },
                { "showRecordId", () => ShowRecordId.Value, ShowRecordId.HasValue },
                { "tailable", true, CursorType == CursorType.Tailable || CursorType == CursorType.TailableAwait },
                { "oplogReplay", () => _oplogReplay.Value, _oplogReplay.HasValue },
                { "noCursorTimeout", () => NoCursorTimeout.Value, NoCursorTimeout.HasValue },
                { "awaitData", true, CursorType == CursorType.TailableAwait },
                { "allowDiskUse", () => AllowDiskUse.Value, AllowDiskUse.HasValue },
                { "allowPartialResults", () => AllowPartialResults.Value, AllowPartialResults.HasValue && isShardRouter },
                { "collation", () => Collation.ToBsonDocument(), Collation != null },
                { "readConcern", readConcern, readConcern != null },
                { "let", Let, Let != null }
            };
        }

        public IAsyncCursor<TDocument> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                return Execute(operationContext, context);
            }
        }

        public IAsyncCursor<TDocument> Execute(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginFind(_batchSize, Limit))
            {
                var operation = CreateOperation(operationContext, context);
                var commandResult = operation.Execute(operationContext, context);
                return CreateCursor(context.ChannelSource, context.Channel, commandResult);
            }
        }

        public async Task<IAsyncCursor<TDocument>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        public async Task<IAsyncCursor<TDocument>> ExecuteAsync(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginFind(_batchSize, Limit))
            {
                var operation = CreateOperation(operationContext, context);
                var commandResult = await operation.ExecuteAsync(operationContext, context).ConfigureAwait(false);
                return CreateCursor(context.ChannelSource, context.Channel, commandResult);
            }
        }

        private AsyncCursor<TDocument> CreateCursor(IChannelSourceHandle channelSource, IChannelHandle channel, BsonDocument commandResult)
        {
            var cursorDocument = commandResult["cursor"].AsBsonDocument;
            var collectionNamespace = CollectionNamespace.FromFullName(cursorDocument["ns"].AsString);
            var firstBatch = CreateFirstCursorBatch(cursorDocument);
            var getMoreChannelSource = ChannelPinningHelper.CreateGetMoreChannelSource(channelSource, channel, firstBatch.CursorId);

            if (cursorDocument.TryGetValue("atClusterTime", out var atClusterTime))
            {
                channelSource.Session.SetSnapshotTimeIfNeeded(atClusterTime.AsBsonTimestamp);
            }

            return new AsyncCursor<TDocument>(
                getMoreChannelSource,
                collectionNamespace,
                Comment,
                firstBatch.Documents,
                firstBatch.CursorId,
                _batchSize,
                Limit < 0 ? Math.Abs(Limit.Value) : Limit,
                ItemSerializer,
                _messageEncoderSettings,
                CursorType == CursorType.TailableAwait ? MaxAwaitTime : null);
        }

        private CursorBatch<TDocument> CreateFirstCursorBatch(BsonDocument cursorDocument)
        {
            var cursorId = cursorDocument["id"].ToInt64();
            var batch = (RawBsonArray)cursorDocument["firstBatch"];

            using (batch)
            {
                var documents = CursorBatchDeserializationHelper.DeserializeBatch(batch, ItemSerializer, _messageEncoderSettings);
                return new CursorBatch<TDocument>(cursorId, documents);
            }
        }

        private IDisposable BeginOperation() => EventContext.BeginOperation(null, "find");

        private ReadCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, RetryableReadContext context)
        {
            var command = CreateCommand(operationContext, context.Binding.Session, context.Channel.ConnectionDescription);
            var operation = new ReadCommandOperation<BsonDocument>(
                CollectionNamespace.DatabaseNamespace,
                command,
                __findCommandResultSerializer,
                _messageEncoderSettings)
            {
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
            return operation;
        }
    }
}
