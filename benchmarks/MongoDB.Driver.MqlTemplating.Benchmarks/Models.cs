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

using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.MqlTemplating.Benchmarks;

public class Order
{
    [BsonId]
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public double Amount { get; set; }
}

public class Product
{
    [BsonId]
    public int Id { get; set; }
    // Stored with surrounding whitespace to make $trim meaningful in queries.
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public double Price { get; set; }
}
