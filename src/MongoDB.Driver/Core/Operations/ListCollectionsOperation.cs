/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.OperationExecutors;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ListCollectionsOperation : IReadOperation<IAsyncCursor<BsonDocument>, BsonDocument>
    {
        public ListCollectionsOperation(
            DatabaseNamespace databaseNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            DatabaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            MessageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public bool? AuthorizedCollections { get; init; }

        public int? BatchSize { get; init; }

        public BsonValue Comment { get; init; }

        public BsonDocument Filter { get; init; }

        public DatabaseNamespace DatabaseNamespace { get; }

        public MessageEncoderSettings MessageEncoderSettings { get; }

        public bool? NameOnly { get; init; }

        public bool RetryRequested { get; init; }

        string IOperation.OperationName => "listCollections";
        DatabaseNamespace IOperation.DatabaseNamespace => DatabaseNamespace;
        bool IReadOperation<IAsyncCursor<BsonDocument>, BsonDocument>.IsRetriable => RetryRequested;
        IBsonSerializer<BsonDocument> IReadOperation<IAsyncCursor<BsonDocument>, BsonDocument>.ResultSerializer
            => BsonDocumentSerializer.Instance;
        BsonDocument IReadOperation<IAsyncCursor<BsonDocument>, BsonDocument>.CreateCommand()
            => new BsonDocument
            {
                { "listCollections", 1 },
                { "filter", Filter, Filter != null },
                { "nameOnly", () => NameOnly.Value, NameOnly.HasValue },
                { "cursor", () => new BsonDocument("batchSize", BatchSize.Value), BatchSize.HasValue },
                { "authorizedCollections", () => AuthorizedCollections.Value, AuthorizedCollections.HasValue },
                { "comment", Comment, Comment != null }
            };

        IAsyncCursor<BsonDocument> IReadOperation<IAsyncCursor<BsonDocument>, BsonDocument>.HandleResult(IOperationExecutorContext context, BsonDocument serverResponse)
        {
            var cursorDocument = serverResponse["cursor"].AsBsonDocument;
            var cursorId = cursorDocument["id"].ToInt64();
            var getMoreChannelSource = ChannelPinningHelper.CreateGetMoreChannelSource(context.ChannelSource, context.Channel, cursorId);
            var cursor = new AsyncCursor<BsonDocument>(
                getMoreChannelSource,
                CollectionNamespace.FromFullName(cursorDocument["ns"].AsString),
                Comment,
                cursorDocument["firstBatch"].AsBsonArray.OfType<BsonDocument>().ToList(),
                cursorId,
                batchSize: BatchSize ?? 0,
                0,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings);

            return cursor;
        }

        IAsyncCursor<BsonDocument> IReadOperation<IAsyncCursor<BsonDocument>, BsonDocument>.HandleException(IOperationExecutorContext context, Exception exception)
        {
            throw exception;
        }
    }
}
