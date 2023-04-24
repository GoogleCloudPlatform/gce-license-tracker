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
using Google.Solutions.LicenseTracker.Services;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test.Services
{
    [TestFixture]
    public class TestPlacementReportService
    {
        //---------------------------------------------------------------------
        // CreateReport.
        //---------------------------------------------------------------------

        [Test]
        public void WhenStartAndEndDateReversed_ThenCreateReportThrowsException()
        {
            var service = new PlacementReportService(
                new Mock<IInstanceHistoryService>().Object,
                new Mock<ILicenseService>().Object,
                new NullLogger<PlacementReportService>());

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => service.CreateReport(
                    new[] { new ProjectLocator("project-1") },
                    180,
                    new DateTime(2020, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    CancellationToken.None).Wait());
        }

        [Test]
        public void WhenStartDateEscapesAnalysisWindow_ThenCreateReportThrowsException()
        {
            var service = new PlacementReportService(
                new Mock<IInstanceHistoryService>().Object,
                new Mock<ILicenseService>().Object,
                new NullLogger<PlacementReportService>());

            ExceptionAssert.ThrowsAggregateException<ArgumentException>(
                () => service.CreateReport(
                    new[] { new ProjectLocator("project-1") },
                    180,
                    DateTime.UtcNow.AddDays(-181),
                    DateTime.UtcNow,
                    CancellationToken.None).Wait());
        }

        [Test]
        public async Task WhenImageUnknown_ThenPlacementsHaveNullLicense()
        {
            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.LookupLicenseInfoAsync(
                    It.IsAny<IEnumerable<IImageLocator>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<IImageLocator, LicenseInfo>());

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            var historyService = new Mock<IInstanceHistoryService>();
            historyService.Setup(s => s.BuildInstanceSetHistoryAsync(
                    It.IsAny<IEnumerable<ProjectLocator>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<uint>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InstanceSetHistory(
                    startDate,
                    endDate,
                    new[] { new PlacementHistory(
                        1,
                        new InstanceLocator("project-1", "zone-1", "instance-1"),
                        InstanceHistoryState.Complete,
                        new ImageLocator("project-1", "unknown-image"),
                        new []
                        {
                            new Placement(endDate.AddDays(-1), endDate)
                        }) },
                    new Dictionary<ulong, ConfigurationHistory<MachineTypeLocator>>()));
            var service = new PlacementReportService(
                historyService.Object,
                licenseService.Object,
                new NullLogger<PlacementReportService>());

            var report = await service.CreateReport(
                    new[] { new ProjectLocator("project-1") },
                    180,
                    startDate,
                    endDate,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, report.StartedPlacements.Count);
            Assert.IsNotNull(report.StartedPlacements.First().Image);
            Assert.IsNull(report.StartedPlacements.First().License);
        }

        [Test]
        public async Task WhenPlacementStraddlesEndDate_ThenReportContainsPlacementStartEvent()
        {
            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.LookupLicenseInfoAsync(
                    It.IsAny<IEnumerable<IImageLocator>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<IImageLocator, LicenseInfo>());

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            var historyService = new Mock<IInstanceHistoryService>();
            historyService.Setup(s => s.BuildInstanceSetHistoryAsync(
                    It.IsAny<IEnumerable<ProjectLocator>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<uint>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InstanceSetHistory(
                    startDate,
                    endDate,
                    new[] { new PlacementHistory(
                        1,
                        new InstanceLocator("project-1", "zone-1", "instance-1"),
                        InstanceHistoryState.Complete,
                        new ImageLocator("project-1", "unknown-image"),
                        new []
                        {
                            new Placement(endDate.AddDays(-1), endDate) // till end
                        }) },
                    new Dictionary<ulong, ConfigurationHistory<MachineTypeLocator>>()));
            var service = new PlacementReportService(
                historyService.Object,
                licenseService.Object,
                new NullLogger<PlacementReportService>());

            var report = await service.CreateReport(
                    new[] { new ProjectLocator("project-1") },
                    180,
                    startDate,
                    endDate,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, report.StartedPlacements.Count);
            Assert.AreEqual(0, report.EndedPlacements.Count);
        }

        [Test]
        public async Task WhenPlacementDoesNotStraddleEndDate_ThenReportContainsPlacementStartAndEndEvent()
        {
            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.LookupLicenseInfoAsync(
                    It.IsAny<IEnumerable<IImageLocator>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<IImageLocator, LicenseInfo>());

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            var historyService = new Mock<IInstanceHistoryService>();
            historyService.Setup(s => s.BuildInstanceSetHistoryAsync(
                    It.IsAny<IEnumerable<ProjectLocator>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<uint>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InstanceSetHistory(
                    startDate,
                    endDate,
                    new[] { new PlacementHistory(
                        1,
                        new InstanceLocator("project-1", "zone-1", "instance-1"),
                        InstanceHistoryState.Complete,
                        new ImageLocator("project-1", "unknown-image"),
                        new []
                        {
                            new Placement(endDate.AddDays(-2), endDate.AddDays(-1))
                        }) },
                    new Dictionary<ulong, ConfigurationHistory<MachineTypeLocator>>()));
            var service = new PlacementReportService(
                historyService.Object,
                licenseService.Object,
                new NullLogger<PlacementReportService>());

            var report = await service.CreateReport(
                    new[] { new ProjectLocator("project-1") },
                    180,
                    startDate,
                    endDate,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, report.StartedPlacements.Count);
            Assert.AreEqual(1, report.EndedPlacements.Count);
        }
    }
}