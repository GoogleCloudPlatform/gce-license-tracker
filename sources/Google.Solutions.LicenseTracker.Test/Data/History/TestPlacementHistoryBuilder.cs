﻿//
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

using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.History;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;

namespace Google.Solutions.LicenseTracker.Test.Data.History
{
    [TestFixture]
    public class TestPlacementHistoryBuilder 
    {
        private static readonly InstanceLocator SampleReference = new InstanceLocator("pro", "zone", "name");
        private static readonly NodeTypeLocator SampleNodeType
            = NodeTypeLocator.FromString("projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240");

        private readonly ILogger logger = new Mock<ILogger>().Object;

        //---------------------------------------------------------------------
        // Placements for existing instances.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRedundantPlacementsRegistered_ThenSecondPlacementIsIgnored()
        {
            var b = PlacementHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                InstanceState.Terminated,
                new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Tenancies.SoleTenant,
                "server-1",
                SampleNodeType,
                this.logger);

            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));
            b.OnSetPlacement("server-1", SampleNodeType, new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc));

            var placements = b.Build(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc)).Placements.ToList();
            Assert.AreEqual(1, placements.Count);
            Assert.AreEqual(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placements[0].To);
            Assert.AreEqual(SampleNodeType, placements[0].NodeType);
        }

        [Test]
        public void WhenPlacementsWithSameServerIdAfterStopRegistered_ThenPlacementsAreKept()
        {
            var b = PlacementHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                InstanceState.Running,
                new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Tenancies.SoleTenant,
                "server-1",
                null,
                this.logger);

            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));
            b.OnStop(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 28, 0, 0, 0, DateTimeKind.Utc));

            var placements = b.Build(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc)).Placements.ToList();
            Assert.AreEqual(2, placements.Count);
            Assert.AreEqual(new DateTime(2019, 12, 28, 0, 0, 0, DateTimeKind.Utc), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), placements[0].To);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placements[1].To);
        }

        [Test]
        public void WhenPlacementsWithDifferentServerIdsRegistered_ThenPlacementsAreKept()
        {
            var b = PlacementHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                InstanceState.Running,
                new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Tenancies.SoleTenant,
                "server-1",
                SampleNodeType,
                this.logger);
            b.OnSetPlacement("server-1", SampleNodeType, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc));

            var placements = b.Build(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc)).Placements.ToList();
            Assert.AreEqual(2, placements.Count);
            Assert.AreEqual("server-2", placements[0].ServerId);
            Assert.IsNull(placements[0].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[0].To);
            Assert.AreEqual("server-1", placements[1].ServerId);
            Assert.AreEqual(SampleNodeType, placements[1].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placements[1].To);
        }

        [Test]
        public void WhenInstanceRunningAndSinglePlacementRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = PlacementHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                InstanceState.Running,
                new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Tenancies.SoleTenant,
                "server-1",
                null,
                this.logger);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));

            var i = b.Build(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(1, i.InstanceId);
            Assert.AreEqual(1, i.Placements.Count());

            var placement = i.Placements.First();
            Assert.AreEqual("server-1", placement.ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placement.From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placement.To);
        }

        [Test]
        public void WhenInstanceRunningAndMultiplePlacementsRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = PlacementHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                InstanceState.Running,
                new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Tenancies.SoleTenant,
                "server-2",
                null,
                this.logger);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));
            b.OnSetPlacement("server-1", SampleNodeType, new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc));

            var i = b.Build(new DateTime(2019, 12, 1));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(SampleNodeType, placements[0].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.IsNull(placements[1].NodeType);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placements[1].To);
        }

        [Test]
        public void WhenInstanceRunningAndMultiplePlacementWithStopsInBetweenRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = PlacementHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                InstanceState.Running,
                new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Tenancies.SoleTenant,
                "server-2",
                null,
                this.logger);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));
            b.OnStop(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 28, 0, 0, 0, DateTimeKind.Utc));

            var i = b.Build(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 28, 0, 0, 0, DateTimeKind.Utc), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placements[1].To);
        }

        [Test]
        public void WhenInstanceRunningAndNoPlacementRegistered_ThenInstanceHasSyntheticPlacementSpanningEntirePeriod()
        {
            var reportStartDate = new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastSeen = new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            var b = PlacementHistoryBuilder.ForExistingInstance(
                1,
                SampleReference,
                InstanceState.Running,
                lastSeen,
                Tenancies.SoleTenant,
                "server-2",
                null,
                this.logger);

            var i = b.Build(reportStartDate);

            var placements = i.Placements.ToList();
            Assert.AreEqual(1, i.Placements.Count());

            Assert.AreEqual("server-2", placements[0].ServerId);
            Assert.AreEqual(reportStartDate, placements[0].From);
            Assert.AreEqual(lastSeen, placements[0].To);
        }

        //---------------------------------------------------------------------
        // Placement events for deleted instances.
        //---------------------------------------------------------------------


        [Test]
        public void WhenInstanceDeletedAndSinglePlacementRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = PlacementHistoryBuilder.ForDeletedInstance(1, this.logger);
            b.OnStop(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(Tenancies.SoleTenant, b.Tenancy);
            var i = b.Build(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(1, i.InstanceId);
            Assert.AreEqual(1, i.Placements.Count());

            var placement = i.Placements.First();
            Assert.AreEqual("server-1", placement.ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placement.From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placement.To);
        }

        [Test]
        public void WhenInstanceDeletedAndMultiplePlacementsRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = PlacementHistoryBuilder.ForDeletedInstance(1, this.logger);
            b.OnStop(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), SampleReference);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(Tenancies.SoleTenant, b.Tenancy);
            var i = b.Build(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placements[1].To);
        }

        [Test]
        public void WhenInstanceDeletedAndMultiplePlacementWithStopsInBetweenRegistered_ThenInstanceContainsRightPlacements()
        {
            var b = PlacementHistoryBuilder.ForDeletedInstance(1, this.logger);
            b.OnStop(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), SampleReference);
            b.OnSetPlacement("server-2", null, new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc));
            b.OnStop(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), SampleReference);
            b.OnSetPlacement("server-1", null, new DateTime(2019, 12, 28, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(Tenancies.SoleTenant, b.Tenancy);
            var i = b.Build(new DateTime(2019, 12, 1));

            var placements = i.Placements.ToList();
            Assert.AreEqual(2, i.Placements.Count());

            Assert.AreEqual("server-1", placements[0].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 28, 0, 0, 0, DateTimeKind.Utc), placements[0].From);
            Assert.AreEqual(new DateTime(2019, 12, 29, 0, 0, 0, DateTimeKind.Utc), placements[0].To);

            Assert.AreEqual("server-2", placements[1].ServerId);
            Assert.AreEqual(new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc), placements[1].From);
            Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc), placements[1].To);
        }
    }
}
