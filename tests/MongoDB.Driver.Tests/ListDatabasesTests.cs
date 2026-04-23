/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ListDatabasesTests : IntegrationTest<ListDatabasesTests.DatabaseFixture>
    {
        public ListDatabasesTests(DatabaseFixture fixture)
            : base(fixture, server => server.Supports(Feature.ListDatabasesAuthorizedDatabases).Authentication(true))
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_return_the_expected_result_when_AuthorizedDatabases_is_used(
            [Values(null, false, true)] bool? authorizedDatabases)
        {
            var settings = Fixture.Client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(mechanism: null, source: null, username: Fixture.UserName, password: Fixture.Password);

            using var testClient = new MongoClient(settings);
            var options = new ListDatabasesOptions
            {
                AuthorizedDatabases = authorizedDatabases,
                NameOnly = true,
            };
            var result = testClient.ListDatabases(options).ToList();

            if (authorizedDatabases == true)
            {
                result.Should().BeEquivalentTo(new BsonArray { new BsonDocument { { "name", Fixture.DatabaseName } } });
            }
            else
            {
                result.Count.Should().BeGreaterThan(1);
            }
        }

        public class DatabaseFixture : MongoDatabaseFixture
        {
            public string DatabaseName { get; } = $"authorizedDatabases_{Guid.NewGuid()}";
            public string UserName => DatabaseName;
            public string Password => "authorizedDatabases";

            protected override IMongoDatabase CreateDatabase()
                => Client.GetDatabase(DatabaseName);

            protected override void InitializeFixture()
            {
                base.InitializeFixture();

                var roleName = $"listDatabases_{DatabaseName}";
                CreateListDatabasesRole(roleName);
                CreateListDatabasesUser(UserName, Password, DatabaseName, roleName);

                Client.GetDatabase(DatabaseName).GetCollection<BsonDocument>("test").InsertOne(new BsonDocument());
            }

            private void CreateListDatabasesRole(string roleName)
            {
                var privileges = new BsonArray
                {
                    new BsonDocument { { "resource", new BsonDocument { { "cluster", true } } }, { "actions", new BsonArray { "listDatabases" } } },
                };
                var command = new BsonDocument
                {
                    { "createRole", roleName },
                    { "privileges", privileges },
                    { "roles", new BsonArray() },
                };

                Client.GetDatabase("admin").RunCommand<BsonDocument>(command);
            }

            private void CreateListDatabasesUser(string username, string password, string databaseName, string roleName)
            {
                var roles = new BsonArray
                {
                    new BsonDocument { { "role", "read" }, { "db", databaseName } },
                    new BsonDocument { { "role", roleName }, { "db", "admin" } },
                };
                var command = new BsonDocument
                {
                    { "createUser", username },
                    { "pwd", password },
                    { "roles", roles },
                };

                Client.GetDatabase("admin").RunCommand<BsonDocument>(command);
            }
        }
    }
}
