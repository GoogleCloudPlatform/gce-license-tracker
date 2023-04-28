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

using Google.Apis.Bigquery.v2.Data;
using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Test.Services
{
    [TestFixture]
    public class TestReportDatasetService
    {
        //---------------------------------------------------------------------
        // PrepareDatasetAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDataSetExists_ThenPrepareDatasetDoesNothing()
        {
            var bigQuery = new Mock<IBigQueryAdapter>();
            bigQuery.Setup(a => a.IsDatasetAvailableAsync(
                    It.IsAny<DatasetLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = new ReportDatasetService(
                bigQuery.Object,
                new NullLogger<ReportDatasetService>());

            var dataset = new DatasetLocator("project-1", "dataset-1");
            await service.PrepareDatasetAsync(
                    dataset,
                    CancellationToken.None)
                .ConfigureAwait(false);

            bigQuery.Verify(a => a.CreateDatasetAsync(
                It.IsAny<DatasetLocator>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task WhenDataSetDoesNotExist_ThenPrepareDatasetCreatesDataSetAndTables()
        {
            var bigQuery = new Mock<IBigQueryAdapter>();
            bigQuery.Setup(a => a.IsDatasetAvailableAsync(
                    It.IsAny<DatasetLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var service = new ReportDatasetService(
                bigQuery.Object,
                new NullLogger<ReportDatasetService>());

            var dataset = new DatasetLocator("project-1", "dataset-1");
            await service.PrepareDatasetAsync(
                    dataset,
                    CancellationToken.None)
                .ConfigureAwait(false);

            bigQuery.Verify(a => a.CreateDatasetAsync(
                It.IsAny<DatasetLocator>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
            bigQuery.Verify(a => a.CreateOrPatchTableAsync(
                It.IsAny<TableLocator>(),
                It.IsAny<IList<TableFieldSchema>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
    }
}
