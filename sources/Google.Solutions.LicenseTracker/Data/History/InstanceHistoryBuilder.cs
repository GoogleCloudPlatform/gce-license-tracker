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
        private readonly PlacementHistoryBuilder placementHistoryBuilder;
        private readonly MachineTypeConfigurationHistoryBuilder machineTypeConfigurationHistoryBuilder;
        private readonly SchedulingPolicyHistoryBuilder schedulingPolicyHistoryBuilder;

        private InstanceHistoryBuilder(
            PlacementHistoryBuilder placementHistoryBuilder, 
            MachineTypeConfigurationHistoryBuilder machineTypeConfigurationHistoryBuilder,
            SchedulingPolicyHistoryBuilder schedulingPolicyHistoryBuilder)
        {
            this.placementHistoryBuilder = placementHistoryBuilder;
            this.machineTypeConfigurationHistoryBuilder = machineTypeConfigurationHistoryBuilder;
            this.schedulingPolicyHistoryBuilder = schedulingPolicyHistoryBuilder;
        }

        public PlacementHistory BuildPlacementHistory(DateTime startDate)
        {
            return this.placementHistoryBuilder.Build(startDate);
        }

        public ConfigurationHistory<MachineTypeLocator> BuildMachineTypeHistory()
        {
            return this.machineTypeConfigurationHistoryBuilder.Build();
        }

        public ConfigurationHistory<SchedulingPolicy> BuildSchedulingPolicyHistory()
        {
            return this.schedulingPolicyHistoryBuilder.Build();
        }

        internal void ProcessEvent(EventBase e)
        {
            //
            // Let all builders process the event so that they can construct
            // their individual perspective of history.
            //
            this.placementHistoryBuilder.ProcessEvent(e);
            this.machineTypeConfigurationHistoryBuilder.ProcessEvent(e);
            this.schedulingPolicyHistoryBuilder.ProcessEvent(e);
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
            ILogger logger)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(state != InstanceState.Deleted);

            return new InstanceHistoryBuilder(
                PlacementHistoryBuilder.ForExistingInstance(
                    instanceId,
                    reference,
                    image,
                    state,
                    lastSeen,
                    tenancy,
                    serverId,
                    nodeType,
                    logger),
                new MachineTypeConfigurationHistoryBuilder(
                    instanceId,
                    machineType),
                new SchedulingPolicyHistoryBuilder(
                    instanceId,
                    schedulingPolicy));
        }

        internal static InstanceHistoryBuilder ForDeletedInstance(
            ulong instanceId,
            ILogger logger)
        {
            return new InstanceHistoryBuilder(
                PlacementHistoryBuilder.ForDeletedInstance(
                    instanceId,
                    logger),
                new MachineTypeConfigurationHistoryBuilder(
                    instanceId,
                    null),
                new SchedulingPolicyHistoryBuilder(
                    instanceId,
                    null));
        }
    }
}
