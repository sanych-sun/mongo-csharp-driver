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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;

namespace MongoDB.Driver.MqlTemplating
{
    internal static class ParameterDictionaryBuilder
    {
        private static readonly ConcurrentDictionary<Type, Func<object, IReadOnlyDictionary<string, BsonValue>>> __factoryCache =
            new ConcurrentDictionary<Type, Func<object, IReadOnlyDictionary<string, BsonValue>>>();

        internal static IReadOnlyDictionary<string, BsonValue> Build(object parameters)
        {
            if (parameters == null) return new Dictionary<string, BsonValue>(StringComparer.Ordinal);
            return __factoryCache.GetOrAdd(parameters.GetType(), BuildFactory)(parameters);
        }

        private static Func<object, IReadOnlyDictionary<string, BsonValue>> BuildFactory(Type type)
        {
            var props = Array.FindAll(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                p => p.CanRead);

            var objParam = Expression.Parameter(typeof(object), "obj");
            var typedVar = Expression.Variable(type, "typed");
            var dictVar  = Expression.Variable(typeof(Dictionary<string, BsonValue>), "dict");

            var dictCtor = typeof(Dictionary<string, BsonValue>)
                .GetConstructor(new[] { typeof(int), typeof(IEqualityComparer<string>) })
                ?? throw new InvalidOperationException("Dictionary<string,BsonValue>(int,IEqualityComparer<string>) constructor not found.");
            var dictAdd = typeof(Dictionary<string, BsonValue>)
                .GetMethod("Add", new[] { typeof(string), typeof(BsonValue) })
                ?? throw new InvalidOperationException("Dictionary<string,BsonValue>.Add(string,BsonValue) method not found.");
            var bsonValueCreate = typeof(BsonValue)
                .GetMethod("Create", new[] { typeof(object) })
                ?? throw new InvalidOperationException("BsonValue.Create(object) method not found.");

            var statements = new List<Expression>();
            statements.Add(Expression.Assign(typedVar, Expression.Convert(objParam, type)));
            statements.Add(Expression.Assign(dictVar, Expression.New(
                dictCtor,
                Expression.Constant(props.Length),
                Expression.Constant(StringComparer.Ordinal))));

            foreach (var prop in props)
            {
                var bsonExpr = MakeBsonValueExpression(Expression.Property(typedVar, prop), prop.PropertyType, bsonValueCreate);
                statements.Add(Expression.Call(dictVar, dictAdd, Expression.Constant(prop.Name), bsonExpr));
            }

            statements.Add(Expression.Convert(dictVar, typeof(IReadOnlyDictionary<string, BsonValue>)));

            var body = Expression.Block(
                typeof(IReadOnlyDictionary<string, BsonValue>),
                new[] { typedVar, dictVar },
                statements);

            return Expression.Lambda<Func<object, IReadOnlyDictionary<string, BsonValue>>>(body, objParam).Compile();
        }

        private static Expression MakeBsonValueExpression(Expression propExpr, Type propType, MethodInfo bsonValueCreate)
        {
            Expression expr;
            if (propType == typeof(int))
                expr = Expression.New(
                    typeof(BsonInt32).GetConstructor(new[] { typeof(int) })
                        ?? throw new InvalidOperationException("BsonInt32(int) constructor not found."),
                    propExpr);
            else if (propType == typeof(long))
                expr = Expression.New(
                    typeof(BsonInt64).GetConstructor(new[] { typeof(long) })
                        ?? throw new InvalidOperationException("BsonInt64(long) constructor not found."),
                    propExpr);
            else if (propType == typeof(double))
                expr = Expression.New(
                    typeof(BsonDouble).GetConstructor(new[] { typeof(double) })
                        ?? throw new InvalidOperationException("BsonDouble(double) constructor not found."),
                    propExpr);
            else if (propType == typeof(float))
                expr = Expression.New(
                    typeof(BsonDouble).GetConstructor(new[] { typeof(double) })
                        ?? throw new InvalidOperationException("BsonDouble(double) constructor not found."),
                    Expression.Convert(propExpr, typeof(double)));
            else if (propType == typeof(bool))
            {
                var trueProp  = typeof(BsonBoolean).GetProperty("True",  BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("BsonBoolean.True property not found.");
                var falseProp = typeof(BsonBoolean).GetProperty("False", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("BsonBoolean.False property not found.");
                expr = Expression.Condition(propExpr, Expression.Property(null, trueProp), Expression.Property(null, falseProp));
            }
            else if (propType == typeof(string))
                expr = Expression.New(
                    typeof(BsonString).GetConstructor(new[] { typeof(string) })
                        ?? throw new InvalidOperationException("BsonString(string) constructor not found."),
                    propExpr);
            else if (propType == typeof(ObjectId))
                expr = Expression.New(
                    typeof(BsonObjectId).GetConstructor(new[] { typeof(ObjectId) })
                        ?? throw new InvalidOperationException("BsonObjectId(ObjectId) constructor not found."),
                    propExpr);
            else if (typeof(BsonValue).IsAssignableFrom(propType))
                expr = propExpr;
            else
                expr = Expression.Call(bsonValueCreate, Expression.Convert(propExpr, typeof(object)));

            return expr.Type == typeof(BsonValue) ? expr : Expression.Convert(expr, typeof(BsonValue));
        }
    }
}
