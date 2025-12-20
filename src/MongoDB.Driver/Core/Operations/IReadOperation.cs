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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.OperationExecutors;

namespace MongoDB.Driver.Core.Operations;

internal abstract class ReadOperationBase<TResult, TServerResponse> : IOperation
{
    public DatabaseNamespace DatabaseNamespace { get; }

    protected ReadOperationBase(string operationName, DatabaseNamespace databaseNamespace, IBsonSerializer<TServerResponse> resultSerializer)
    {
        DatabaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
        OperationName = Ensure.IsNotNullOrEmpty(operationName, nameof(operationName));
        ResultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
    }

    public string OperationName { get; }

    public bool RetryRequested { get; init; }

    public IBsonSerializer<TServerResponse> ResultSerializer { get; }

    public abstract BsonDocument CreateCommand(OperationContext operationContext, CommandExecutorContext context);

    public abstract TResult HandleServerResponse(OperationContext operationContext, CommandExecutorContext context, TServerResponse serverResponse);

    public virtual bool TryHandleException(OperationContext operationContext, CommandExecutorContext context, Exception exception, out TResult result)
    {
        result = default;
        return false;
    }
}

internal abstract class ReadOperationBase<TResult> : ReadOperationBase<TResult, TResult>
{
    protected ReadOperationBase(string operationName, DatabaseNamespace databaseNamespace, IBsonSerializer<TResult> resultSerializer)
        : base(operationName, databaseNamespace, resultSerializer)
    {
    }

    public override TResult HandleServerResponse(OperationContext operationContext, CommandExecutorContext context, TResult serverResponse) => serverResponse;
}
