//
// Copyright 2023 Google LLC
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


using Google.Solutions.LicenseTracker.Data.History;
using Google.Solutions.LicenseTracker.Data.Locator;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test.Data.History
{
    [TestFixture]
    public class TestConfigurationHistory
    {
        //---------------------------------------------------------------------
        // GetHistoricValue.
        //---------------------------------------------------------------------

        [Test]
        public void WhenHistoryEmpty_ThenGetHistoricValueReturnsNull()
        {
            var history = new ConfigurationHistory<MachineTypeLocator>(
                1,
                Enumerable.Empty<ConfigurationChange<MachineTypeLocator>>());

            Assert.IsNull(history.GetHistoricValue(DateTime.UtcNow));
        }

        [Test]
        public void WhenDatePredatesHistory_ThenGetHistoricValueReturnsNull()
        {
            var changes = new ConfigurationChange<MachineTypeLocator>[]
            {
                new ConfigurationChange<MachineTypeLocator>(
                    new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new MachineTypeLocator("project-1", "zone-1", "type-1")),
                new ConfigurationChange<MachineTypeLocator>(
                    new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    new MachineTypeLocator("project-1", "zone-1", "type-2")),
                new ConfigurationChange<MachineTypeLocator>(
                    new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    new MachineTypeLocator("project-1", "zone-1", "type-3")),
            };

            var history = new ConfigurationHistory<MachineTypeLocator>(1, changes);

            Assert.IsNull(history.GetHistoricValue(new DateTime(2021, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
        }

        [Test]
        public void WhenDateOverlapsHistory_ThenGetHistoricValueReturnsValue()
        {
            var changes = new ConfigurationChange<MachineTypeLocator>[]
            {
                new ConfigurationChange<MachineTypeLocator>(
                    new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new MachineTypeLocator("project-1", "zone-1", "type-1")),
                new ConfigurationChange<MachineTypeLocator>(
                    new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    new MachineTypeLocator("project-1", "zone-1", "type-2")),
                new ConfigurationChange<MachineTypeLocator>(
                    new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    new MachineTypeLocator("project-1", "zone-1", "type-3")),
            };

            var history = new ConfigurationHistory<MachineTypeLocator>(1, changes);

            Assert.AreEqual(
                new MachineTypeLocator("project-1", "zone-1", "type-1"),
                history.GetHistoricValue(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(
                new MachineTypeLocator("project-1", "zone-1", "type-1"),
                history.GetHistoricValue(new DateTime(2022, 1, 1, 23, 59, 59, DateTimeKind.Utc)));

            Assert.AreEqual(
                new MachineTypeLocator("project-1", "zone-1", "type-2"),
                history.GetHistoricValue(new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(
                new MachineTypeLocator("project-1", "zone-1", "type-2"),
                history.GetHistoricValue(new DateTime(2022, 1, 2, 23, 59, 59, DateTimeKind.Utc)));

            Assert.AreEqual(
                new MachineTypeLocator("project-1", "zone-1", "type-3"),
                history.GetHistoricValue(new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(
                new MachineTypeLocator("project-1", "zone-1", "type-3"),
                history.GetHistoricValue(new DateTime(2023, 1, 1, 2, 3, 4, DateTimeKind.Utc)));
        }
    }
}
