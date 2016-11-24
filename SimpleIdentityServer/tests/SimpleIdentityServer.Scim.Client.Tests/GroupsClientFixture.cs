﻿#region copyright
// Copyright 2016 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Moq;
using SimpleIdentityServer.Scim.Client.Factories;
using SimpleIdentityServer.Scim.Client.Groups;
using System.Threading.Tasks;
using Xunit;

namespace SimpleIdentityServer.Scim.Client.Tests
{
    public class GroupsClientFixture : IClassFixture<TestScimServerFixture>
    {
        private readonly TestScimServerFixture _testScimServerFixture;
        private Mock<IHttpClientFactory> _httpClientFactoryStub;
        private IGroupsClient _groupsClient;

        public GroupsClientFixture(TestScimServerFixture testScimServerFixture)
        {
            _testScimServerFixture = testScimServerFixture;
        }

        [Fact]
        public async Task When_Adding_Group_Then_201_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();
            _httpClientFactoryStub.Setup(h => h.GetHttpClient()).Returns(_testScimServerFixture.Client);

            // ACT
            var result = await _groupsClient.AddGroup("http://localhost:5555/Groups").SetCommonAttributes("external_id").Execute();

            // ASSERT
            Assert.NotNull(result);
        }

        private void InitializeFakeObjects()
        {
            _httpClientFactoryStub = new Mock<IHttpClientFactory>();
            _groupsClient = new GroupsClient(_httpClientFactoryStub.Object);
        }
    }
}