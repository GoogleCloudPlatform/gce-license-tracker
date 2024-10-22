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
using Google.Solutions.LicenseTracker.Data.Events.Lifecycle;
using Google.Solutions.LicenseTracker.Data.Events.System;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Logs;
using Google.Solutions.LicenseTracker.Data.History;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using Google.Solutions.LicenseTracker.Data.Events.Config;

namespace Google.Solutions.LicenseTracker.Test.Data.History
{
    [TestFixture]
    public class TestInstanceSetHistoryBuilder 
    {
        private static readonly InstanceLocator SampleReference = new InstanceLocator("pro", "zone", "name");
        private static readonly ImageLocator SampleImage
            = ImageLocator.FromString("projects/project-1/global/images/image-1");
        private static readonly MachineTypeLocator SampleMachineType
            = MachineTypeLocator.FromString("projects/project-1/zones/asia-southeast1-b/machineTypes/e2-medium");

        private readonly ILogger logger = new Mock<ILogger>().Object;

        private InstanceSetHistory BuildHistoryFromResource(string resourceName)
        {
            var testDataResource = Assembly.GetExecutingAssembly()
                .GetManifestResourceNames()
                .First(n => n.EndsWith(resourceName));

            var b = new InstanceSetHistoryBuilder(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                this.logger);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(testDataResource)!)
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var events = new JsonSerializer().Deserialize<LogRecord[]>(reader)!
                    .Select(rec => rec.ToEvent())
                    .OrderByDescending(e => e.Timestamp);

                foreach (var e in events)
                {
                    b.Process(e);
                }
            }

            return b.Build();
        }

        [Test]
        public void WhenFleetInstanceAdded_ThenInstanceIncludedInSet()
        {
            var b = new InstanceSetHistoryBuilder(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                this.logger);

            b.AddExistingInstance(
                1,
                SampleReference,
                SampleImage,
                SampleMachineType,
                new SchedulingPolicy("TERMINATE", null),
                InstanceState.Running,
                DateTime.UtcNow,
                Tenancies.Fleet,
                null,
                null,
                null);

            var set = b.Build();

            Assert.AreEqual(1, set.PlacementHistories.Count());
            Assert.AreEqual(1, set.MachineTypeHistories.Count());
            Assert.AreEqual(1, set.SchedulingPolicyHistories.Count());

            Assert.AreEqual(1, set.PlacementHistories.First().InstanceId);
            Assert.AreEqual(1, set.MachineTypeHistories.First().Value.InstanceId);
            Assert.AreEqual(1, set.SchedulingPolicyHistories.First().Value.InstanceId);
        }

        [Test]
        public void WhenSoleTenantInstanceAdded_ThenInstanceIncludedInSet()
        {
            var b = new InstanceSetHistoryBuilder(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                this.logger);

            b.AddExistingInstance(
                1,
                SampleReference,
                SampleImage,
                SampleMachineType,
                new SchedulingPolicy("TERMINATE", null),
                InstanceState.Running,
                DateTime.UtcNow,
                Tenancies.SoleTenant,
                "server-1",
                new NodeTypeLocator("project-1", "zone-1", "type-1"),
                null);

            var set = b.Build();

            Assert.AreEqual(1, set.PlacementHistories.Count());
            Assert.AreEqual(1, set.PlacementHistories.First().InstanceId);

            Assert.AreEqual("server-1", set.PlacementHistories.First().Placements.First().ServerId);
            Assert.AreEqual("type-1", set.PlacementHistories.First().Placements.First().NodeType?.Name);
        }

        [Test]
        public void WhenInstanceNotAddedButStopEventRecorded_ThenInstanceIncludedInSetAsMissingTenancy()
        {
            var b = new InstanceSetHistoryBuilder(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                this.logger);

            b.Process(new StopInstanceEvent(new LogRecord()
            {
                LogName = "projects/project-1/logs/cloudaudit.googleapis.com%2Factivity",
                ProtoPayload = new AuditLogRecord()
                {
                    MethodName = StopInstanceEvent.Method,
                    ResourceName = "projects/project-1/zones/us-central1-a/instances/instance-1"
                },
                Resource = new ResourceRecord()
                {
                    Labels = new Dictionary<string, string>
                    {
                        { "instance_id", "123" }
                    }
                },
                Timestamp = new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc)
            }));

            var set = b.Build();

            Assert.AreEqual(1, set.PlacementHistories.Count());
            Assert.AreEqual(123, set.PlacementHistories.First().InstanceId);
        }

        [Test]
        public void WhenInstanceNotAddedButInsertEventRecorded_ThenInstanceIncludedInSet()
        {
            var b = new InstanceSetHistoryBuilder(
                new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                this.logger);

            b.Process(new TerminateOnHostMaintenanceEvent(new LogRecord()
            {
                LogName = "projects/project-1/logs/cloudaudit.googleapis.com%2Fsystem_event",
                ProtoPayload = new AuditLogRecord()
                {
                    MethodName = TerminateOnHostMaintenanceEvent.Method,
                    ResourceName = "projects/project-1/zones/us-central1-a/instances/instance-1",
                },
                Resource = new ResourceRecord()
                {
                    Labels = new Dictionary<string, string>
                    {
                        { "instance_id", "123" }
                    }
                },
                Timestamp = new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Severity = "INFO"
            }));
            b.Process(new InsertInstanceEvent(new LogRecord()
            {
                LogName = "projects/project-1/logs/cloudaudit.googleapis.com%2Factivity",
                ProtoPayload = new AuditLogRecord()
                {
                    MethodName = InsertInstanceEvent.Method,
                    ResourceName = "projects/project-1/zones/us-central1-a/instances/instance-1",
                },
                Resource = new ResourceRecord()
                {
                    Labels = new Dictionary<string, string>
                    {
                        { "instance_id", "123" }
                    }
                },
                Timestamp = new DateTime(2019, 12, 31, 0, 0, 0, DateTimeKind.Utc)
            }));

            var set = b.Build();

            Assert.AreEqual(1, set.PlacementHistories.Count());
            Assert.AreEqual(123, set.PlacementHistories.First().InstanceId);
        }

        [Test]
        public void WhenReadingSample1_ThenHistoryIsRestored()
        {
            var set = BuildHistoryFromResource("instance-1.json");

            Assert.AreEqual(1, set.PlacementHistories.Count());

            var instance = set.PlacementHistories.First();
            Assert.AreEqual(1, instance.Placements.Count());

            var placement = instance.Placements.First();
            Assert.AreEqual("15934ff9aee7d8c5719fad1053b7fc7d", placement.ServerId);
            Assert.AreEqual(Tenancies.SoleTenant, placement.Tenancy);

            // Insert..
            Assert.AreEqual(DateTime.Parse("2020-05-06T14:58:53.077Z").ToUniversalTime(), placement.From);

            // ..till delete
            Assert.AreEqual(DateTime.Parse("2020-05-15T10:57:06.997Z").ToUniversalTime(), placement.To);
        }

        [Test]
        public void WhenReadingSample2_ThenHistoryIsRestored()
        {
            var set = BuildHistoryFromResource("instance-2.json");
            Assert.AreEqual(1, set.PlacementHistories.Count());

            var instance = set.PlacementHistories.First();
            Assert.AreEqual(1, instance.Placements.Count());

            var placement = instance.Placements.First();
            Assert.AreEqual("15934ff9aee7d8c5719fad1053b7fc7d", placement.ServerId);
            Assert.AreEqual(Tenancies.SoleTenant, placement.Tenancy);

            // Insert..
            Assert.AreEqual(DateTime.Parse("2020-05-06T14:57:46.557Z").ToUniversalTime(), placement.From);

            // ..till GuestTerminate.
            Assert.AreEqual(DateTime.Parse("2020-05-06T16:03:06.484Z").ToUniversalTime(), placement.To);
        }

        [Test]
        public void WhenReadingSample3_ThenHistoryIsRestored()
        {
            var set = BuildHistoryFromResource("instance-3.json");
            Assert.AreEqual(1, set.PlacementHistories.Count());

            var instance = set.PlacementHistories.First();
            Assert.AreEqual(2, instance.Placements.Count());

            var firstPlacement = instance.Placements.First();
            Assert.AreEqual("413db7b32a208e7ccb4ee62acedee725", firstPlacement.ServerId);
            Assert.AreEqual(Tenancies.SoleTenant, firstPlacement.Tenancy);

            // Insert..
            Assert.AreEqual(DateTime.Parse("2020-05-05T08:31:40.864Z").ToUniversalTime(), firstPlacement.From);

            // ..till TerminateOnHostMaintenance.
            Assert.AreEqual(DateTime.Parse("2020-05-06T16:10:46.781Z").ToUniversalTime(), firstPlacement.To);


            var secondPlacement = instance.Placements.Last();
            Assert.AreEqual("413db7b32a208e7ccb4ee62acedee725", secondPlacement.ServerId);
            Assert.AreEqual(Tenancies.SoleTenant, secondPlacement.Tenancy);

            // Insert..
            Assert.AreEqual(DateTime.Parse("2020-05-06T16:36:01.441Z").ToUniversalTime(), secondPlacement.From);

            // ..till GuestTerminate.
            Assert.AreEqual(DateTime.Parse("2020-05-06T17:39:34.635Z").ToUniversalTime(), secondPlacement.To);
        }

        [Test]
        public void WhenReadingSample4_ThenHistoryIsRestoredWithMixedTenancy()
        {
            var set = BuildHistoryFromResource("instance-4.json");
            Assert.AreEqual(1, set.PlacementHistories.Count());

            var instance = set.PlacementHistories.First();
            Assert.AreEqual(2, instance.Placements.Count());

            var firstPlacement = instance.Placements.First();
            Assert.AreEqual(Tenancies.Fleet, firstPlacement.Tenancy);

            // Insert..
            Assert.AreEqual(DateTime.Parse("2020-04-23T09:08:19.023Z").ToUniversalTime(), firstPlacement.From);

            // ..till Stop.
            Assert.AreEqual(DateTime.Parse("2020-05-05T13:23:26.488Z").ToUniversalTime(), firstPlacement.To);


            var secondPlacement = instance.Placements.Last();
            Assert.AreEqual("15934ff9aee7d8c5719fad1053b7fc7d", secondPlacement.ServerId);
            Assert.AreEqual(Tenancies.SoleTenant, secondPlacement.Tenancy);

            // Start..
            Assert.AreEqual(DateTime.Parse("2020-05-19T08:17:01.685Z").ToUniversalTime(), secondPlacement.From);

            // ..till Stop.
            Assert.AreEqual(DateTime.Parse("2020-05-19T09:06:27.455Z").ToUniversalTime(), secondPlacement.To);
        }

        [Test]
        public void SupportedMethodsIncludeSystemAndLifecycleEvents()
        {
            var b = new InstanceSetHistoryBuilder(
                DateTime.UtcNow.AddDays(-1), 
                DateTime.UtcNow,
                this.logger);

            CollectionAssert.Contains(b.SupportedMethods, NotifyInstanceLocationEvent.Method);
            CollectionAssert.Contains(b.SupportedMethods, HostErrorEvent.Method);

            CollectionAssert.Contains(b.SupportedMethods, InsertInstanceEvent.Method);
            CollectionAssert.Contains(b.SupportedMethods, DeleteInstanceEvent.Method);
        }
    }
}
