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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.Events.Config;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Data.History
{
    public class InstanceSetHistoryBuilder : IEventProcessor
    {
        private readonly ILogger logger;
        private readonly IDictionary<ulong, InstanceHistoryBuilder> instanceBuilders =
            new Dictionary<ulong, InstanceHistoryBuilder>();

        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        private static string ShortZoneIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        internal InstanceHistoryBuilder GetInstanceHistoryBuilder(ulong instanceId)
        {
            if (this.instanceBuilders.TryGetValue(instanceId, out InstanceHistoryBuilder? builder))
            {
                return builder;
            }
            else
            {
                var newBuilder = InstanceHistoryBuilder.ForDeletedInstance(
                    instanceId,
                    this.logger);
                this.instanceBuilders[instanceId] = newBuilder;
                return newBuilder;
            }
        }

        public InstanceSetHistoryBuilder(
            DateTime startDate,
            DateTime endDate,
            ILogger logger)
        {
            if (startDate.Kind != DateTimeKind.Utc ||
                endDate.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Start/end date must be in UTC time");
            }

            if (startDate > endDate)
            {
                throw new ArgumentException("Start date and end date are reversed");
            }

            this.logger = logger;
            this.StartDate = startDate;
            this.EndDate = endDate;
        }

        public void AddExistingInstance(
            ulong instanceId,
            InstanceLocator reference,
            ImageLocator? image,
            MachineTypeLocator? machineType,
            SchedulingPolicy? schedulingPolicy,
            InstanceState state,
            DateTime lastSeen,
            Tenancies tenancy,
            string? serverId,
            NodeTypeLocator? nodeType)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            this.instanceBuilders[instanceId] = InstanceHistoryBuilder.ForExistingInstance(
                instanceId,
                reference,
                image,
                machineType,
                schedulingPolicy,
                state,
                lastSeen,
                tenancy,
                serverId,
                nodeType,
                this.logger);
        }

        public void AddExistingInstances(
            IEnumerable<Instance> instances,
            IEnumerable<NodeGroupNode> nodes,
            IEnumerable<Disk> disks,
            ProjectLocator projectId)
        {
            //
            // NB. Instances.list returns the disks associated with each
            // instance, but lacks the information about the source image.
            // Therefore, we load disks first and then join the data.
            //
            var sourceImagesByDisk = disks
                .EnsureNotNull()
                .ToDictionary(d => d.SelfLink, d => d.SourceImage);

            this.logger.LogDebug("Found {count} existing disks", sourceImagesByDisk.Count);
            this.logger.LogDebug("Found {count} existing instances", instances.Count());

            foreach (var instance in instances)
            {
                var instanceLocator = new InstanceLocator(
                    projectId,
                    ShortZoneIdFromUrl(instance.Zone),
                    instance.Name);

                this.logger.LogInformation("Found {locator} ({id})", instanceLocator, instance.Id);

                var bootDiskUrl = instance.Disks
                    .EnsureNotNull()
                    .Where(d => d.Boot != null && d.Boot.Value)
                    .EnsureNotNull()
                    .Select(d => d.Source)
                    .EnsureNotNull()
                    .FirstOrDefault();
                ImageLocator? image = null;
                if (bootDiskUrl != null &&
                    sourceImagesByDisk.TryGetValue(bootDiskUrl, out string? imageUrl) &&
                    imageUrl != null)
                {
                    image = ImageLocator.FromString(imageUrl);
                }

                Tenancies tenancy;
                string? serverId;
                NodeTypeLocator? nodeType;
                if (instance.Scheduling.NodeAffinities != null && instance.Scheduling.NodeAffinities.Any())
                {
                    // This VM runs on a sole-tenant node.
                    var node = nodes.FirstOrDefault(n => n.Instances
                        .EnsureNotNull()
                        .Select(uri => InstanceLocator.FromString(uri))
                        .Any(locator => locator == instanceLocator));
                    if (node == null && instance.Status == "RUNNING")
                    {
                        this.logger.LogWarning(
                            "Could not identify node {locator} is scheduled on",
                            instanceLocator);
                    }

                    tenancy = Tenancies.SoleTenant;
                    serverId = node?.ServerId;
                    nodeType = node?.NodeType != null
                        ? NodeTypeLocator.FromString(node.NodeType)
                        : null;
                }
                else
                {
                    // Fleet VM.
                    tenancy = Tenancies.Fleet;
                    serverId = null;
                    nodeType = null;
                }

                AddExistingInstance(
                    (ulong)instance.Id!.Value,
                    instanceLocator,
                    image,
                    MachineTypeLocator.FromString(instance.MachineType),
                    new SchedulingPolicy(
                        instance.Scheduling.OnHostMaintenance,
                        (uint?)instance.Scheduling.MinNodeCpus),
                    instance.Status == "RUNNING"
                        ? InstanceState.Running
                        : InstanceState.Terminated,
                    this.EndDate,
                    tenancy,
                    serverId,
                    nodeType);
            }
        }

        public InstanceSetHistory Build()
        {
            return new InstanceSetHistory(
                this.StartDate,
                this.EndDate,
                this.instanceBuilders.Values
                    .Select(b => b.BuildPlacementHistory(this.StartDate))
                    .ToList(),
                this.instanceBuilders.Values
                    .Select(b => b.BuildMachineTypeHistory())
                    .ToDictionary(h => h.InstanceId, h => h),
                this.instanceBuilders.Values
                    .Select(b => b.BuildSchedulingPolicyHistory())
                    .ToDictionary(h => h.InstanceId, h => h));
        }

        //---------------------------------------------------------------------
        // IEventProcessor
        //---------------------------------------------------------------------

        //
        // NB. Error events are not relevant for building the history, we only need
        // informational records.
        //

        public EventOrder ExpectedOrder => EventOrder.NewestFirst;

        public IEnumerable<string> SupportedSeverities => new[] { "NOTICE", "INFO" };

        public IEnumerable<string> SupportedMethods => EventFactory.EventMethods;

        public void Process(EventBase e)
        {
            //
            // NB. Some events (such as recreateInstance) might not have an instance ID.
            // These are useless for our purpose.
            //
            if (e is InstanceEventBase instanceEvent && instanceEvent.InstanceId != 0)
            {
                GetInstanceHistoryBuilder(instanceEvent.InstanceId).ProcessEvent(e);
            }
        }
    }
}
