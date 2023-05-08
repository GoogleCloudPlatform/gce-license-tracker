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

using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.Events.Config;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Data.History
{
    internal class InstanceHistoryBuilder
    {
        public PlacementHistoryBuilder Placements { get; }
        public InstanceMachineTypeHistoryBuilder MachineType { get; }
        public InstanceSchedulingPolicyHistoryBuilder SchedulingPolicy { get; }
        public InstanceImageHistoryBuilder Image { get; }
        public InstanceLabelsHistoryBuilder Labels { get; }

        private InstanceHistoryBuilder(
            PlacementHistoryBuilder placements, 
            InstanceMachineTypeHistoryBuilder machineType,
            InstanceSchedulingPolicyHistoryBuilder schedulingPolicy,
            InstanceImageHistoryBuilder image,
            InstanceLabelsHistoryBuilder labels)
        {
            this.Placements = placements;
            this.MachineType = machineType;
            this.SchedulingPolicy = schedulingPolicy;
            this.Image = image;
            this.Labels = labels;
        }

        internal void ProcessEvent(EventBase e)
        {
            //
            // Let all builders process the event so that they can construct
            // their individual perspective of history.
            //
            this.Placements.ProcessEvent(e);
            this.MachineType.ProcessEvent(e);
            this.SchedulingPolicy.ProcessEvent(e);
            this.Image.ProcessEvent(e);
            this.Labels.ProcessEvent(e);
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        internal static InstanceHistoryBuilder ForExistingInstance(
            ulong instanceId,
            InstanceLocator reference,
            ImageLocator? image,
            MachineTypeLocator? machineType,
            SchedulingPolicy? schedulingPolicy,
            InstanceState state,
            DateTime lastSeen,
            Tenancies tenancy,
            string? serverId,
            NodeTypeLocator? nodeType,
            IDictionary<string, string>? labels,
            ILogger logger)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(state != InstanceState.Deleted);

            return new InstanceHistoryBuilder(
                PlacementHistoryBuilder.ForExistingInstance(
                    instanceId,
                    reference,
                    state,
                    lastSeen,
                    tenancy,
                    serverId,
                    nodeType,
                    logger),
                new InstanceMachineTypeHistoryBuilder(
                    instanceId,
                    machineType),
                new InstanceSchedulingPolicyHistoryBuilder(
                    instanceId,
                    schedulingPolicy),
                new InstanceImageHistoryBuilder(
                    instanceId,
                    image),
                new InstanceLabelsHistoryBuilder(
                    instanceId,
                    labels));
        }

        internal static InstanceHistoryBuilder ForDeletedInstance(
            ulong instanceId,
            ILogger logger)
        {
            return new InstanceHistoryBuilder(
                PlacementHistoryBuilder.ForDeletedInstance(
                    instanceId,
                    logger),
                new InstanceMachineTypeHistoryBuilder(
                    instanceId,
                    null),
                new InstanceSchedulingPolicyHistoryBuilder(
                    instanceId,
                    null),
                new InstanceImageHistoryBuilder(
                    instanceId,
                    null),
                new InstanceLabelsHistoryBuilder(
                    instanceId,
                    new Dictionary<string, string>()));
        }
    }
}
