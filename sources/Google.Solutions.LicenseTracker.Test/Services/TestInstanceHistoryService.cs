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

using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.History;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Services.Adapters;
using Google.Solutions.LicenseTracker.Services;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test.Services
{
    [TestFixture]
    public class TestInstanceHistoryService
    {
        //---------------------------------------------------------------------
        // BuildInstanceSetHistoryAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenProjectInaccessible_ThenProjectIsIgnored()
        {
            var computeEngineAdapter = new Mock<IComputeEngineAdapter>();
            computeEngineAdapter
                .Setup(a => a.ListNodesAsync(
                    It.IsAny<ProjectLocator>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceAccessDeniedException("test", new Exception()));

            var auditLogAdapter = new Mock<IAuditLogAdapter>();

            var service = new InstanceHistoryService(
                auditLogAdapter.Object,
                computeEngineAdapter.Object,
                new NullLogger<InstanceHistoryService>());

            await service.BuildInstanceSetHistoryAsync(
                new[] { new ProjectLocator("project-1") },
                DateTime.UtcNow,
                30,
                CancellationToken.None);

            auditLogAdapter.Verify(
                a => a.ProcessInstanceEventsAsync(
                    It.Is<IEnumerable<ProjectLocator>>(loc => loc.Any()),
                    It.IsAny<DateTime>(),
                    It.IsAny<IEventProcessor>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
