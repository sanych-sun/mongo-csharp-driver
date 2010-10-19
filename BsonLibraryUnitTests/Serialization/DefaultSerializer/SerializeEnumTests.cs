﻿/* Copyright 2010 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.BsonLibrary.DefaultSerializer;
using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.UnitTests.Serialization {
    [TestFixture]
    public class SerializeEnumTests {
        // TODO: add unit tests for other underlying types
        private enum E {
            A = 1,
            B = 2
        }

        private class C {
            public E PE { get; set; }
        }

        [Test]
        public void TestSerializeZero() {
            C c = new C { PE = 0 };
            var json = c.ToJson();
            var expected = ("{ 'PE' : 0 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeA() {
            C c = new C { PE = E.A };
            var json = c.ToJson();
            var expected = ("{ 'PE' : 1 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeB() {
            C c = new C { PE = E.B };
            var json = c.ToJson();
            var expected = ("{ 'PE' : 2 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void TestSerializeInvalid() {
            C c = new C { PE = (E) 123 };
            var json = c.ToJson();
            var expected = ("{ 'PE' : 123 }").Replace("'", "\"");
            Assert.AreEqual(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.DeserializeDocument<C>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
