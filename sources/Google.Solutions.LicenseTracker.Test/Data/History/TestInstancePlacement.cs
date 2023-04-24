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

using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.History;
using NUnit.Framework;
using System;

namespace Google.Solutions.LicenseTracker.Test.Data.History
{
    [TestFixture]
    public class TestInstancePlacement 
    {
        private static readonly NodeTypeLocator SampleNodeType
            = NodeTypeLocator.FromString("projects/project-1/zones/us-central1-a/nodeTypes/c2-node-60-240");

        [Test]
        public void WhenTwoPlacementsCloseAndNoneHasServer_ThenPlacementIsMerged()
        {
            var p1 = new Placement(
                null,
                null,
                new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 11, 0, 0, DateTimeKind.Utc));
            var p2 = new Placement(
                null,
                null,
                new DateTime(2020, 1, 1, 11, 0, 50, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc));

            Assert.IsTrue(p1.IsAdjacent(p2));

            var merged = p1.Merge(p2);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                merged.From);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                merged.To);
            Assert.IsNull(merged.ServerId);
        }

        [Test]
        public void WhenTwoPlacementsCloseAndOneHasServer_ThenPlacementIsMerged()
        {
            var p1 = new Placement(
                null,
                null,
                new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 11, 0, 0, DateTimeKind.Utc));
            var p2 = new Placement(
                "server-1",
                SampleNodeType,
                new DateTime(2020, 1, 1, 11, 0, 50, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc));

            Assert.IsTrue(p1.IsAdjacent(p2));

            var merged = p1.Merge(p2);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                merged.From);
            Assert.AreEqual(
                new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                merged.To);
            Assert.AreEqual("server-1", merged.ServerId);
            Assert.AreEqual(SampleNodeType, merged.NodeType);
        }

        [Test]
        public void WhenTwoPlacementsCloseAndBothHaveDifferentServers_ThenPlacementIsNotMerged()
        {
            var p1 = new Placement(
                "server2",
                null,
                new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 11, 0, 0, DateTimeKind.Utc));
            var p2 = new Placement(
                "server1",
                null,
                new DateTime(2020, 1, 1, 11, 0, 50, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc));

            Assert.IsFalse(p1.IsAdjacent(p2));
        }

        [Test]
        public void WhenTwoPlacementsNotClose_ThenPlacementIsNotMerged()
        {
            var p1 = new Placement(
                null,
                null,
                new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 11, 0, 0, DateTimeKind.Utc));
            var p2 = new Placement(
                null,
                null,
                new DateTime(2020, 1, 1, 11, 2, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc));

            Assert.IsFalse(p1.IsAdjacent(p2));
        }
    }
}
