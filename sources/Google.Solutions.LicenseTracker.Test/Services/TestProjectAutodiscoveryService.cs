//
// Copyright 2022 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Services;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test.Services
{
    [TestFixture]
    public class TestProjectAutodiscoveryService
    {
        //---------------------------------------------------------------------
        // DiscoverAccessibleProjects.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPermissionMissing_ThenProjectIsFiltered()
        {
            var resourceManager = new Mock<IResourceManagerAdapter>();
            resourceManager
                .Setup(r => r.ListProjectsAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Project()
                    {
                        ProjectId = "inaccessible"
                    },
                    new Project()
                    {
                        ProjectId = "accessible"
                    }
                });

            resourceManager.Setup(r => r.IsGrantedPermissionsAsync(
                    It.Is<ProjectLocator>(id => id.ProjectId == "accessible"),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            resourceManager.Setup(r => r.IsGrantedPermissionsAsync(
                    It.Is<ProjectLocator>(id => id.ProjectId == "inaccessible"),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);


            var serviceUsageAdapter = new Mock<IServiceUsageAdapter>();
            serviceUsageAdapter
                .Setup(a => a.IsServiceEnabledAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = new ProjectAutodiscoveryService(
                resourceManager.Object,
                serviceUsageAdapter.Object);

            var accessibleProjects = await service.DiscoverAccessibleProjects(
                    new[] { "permission-1", "permission-2" },
                    "compute.googleapis.com",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, accessibleProjects.ToList().Count);
        }

        [Test]
        public async Task WhenServiceNotEnabled_ThenProjectIsFiltered()
        {
            var resourceManager = new Mock<IResourceManagerAdapter>();
            resourceManager
                .Setup(r => r.ListProjectsAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Project()
                    {
                        ProjectId = "inaccessible"
                    },
                    new Project()
                    {
                        ProjectId = "accessible"
                    }
                });

            resourceManager.Setup(r => r.IsGrantedPermissionsAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);


            var serviceUsageAdapter = new Mock<IServiceUsageAdapter>();
            serviceUsageAdapter
                .Setup(a => a.IsServiceEnabledAsync(
                    It.Is<ProjectLocator>(id => id.ProjectId == "accessible"),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            serviceUsageAdapter
                .Setup(a => a.IsServiceEnabledAsync(
                    It.Is<ProjectLocator>(id => id.ProjectId == "inaccessible"),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var service = new ProjectAutodiscoveryService(
                resourceManager.Object,
                serviceUsageAdapter.Object);

            var accessibleProjects = await service.DiscoverAccessibleProjects(
                    new[] { "permission-1" },
                    "compute.googleapis.com",
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, accessibleProjects.ToList().Count);
        }
    }
}
