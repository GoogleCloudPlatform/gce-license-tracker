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

using Google.Solutions.LicenseTracker.Data.Events.Config;
using Google.Solutions.LicenseTracker.Data.Locator;

namespace Google.Solutions.LicenseTracker.Data.History
{
    /// <summary>
    /// History for a set of VMs in a certain time window.
    /// </summary>
    public class InstanceSetHistory
    {
        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public IEnumerable<PlacementHistory> PlacementHistories { get; }

        public IDictionary<ulong, ConfigurationHistory<MachineTypeLocator>> MachineTypeHistories { get; }

        public IDictionary<ulong, ConfigurationHistory<SchedulingPolicy>> SchedulingPolicyHistories { get; }

        public IDictionary<ulong, ConfigurationHistory<IImageLocator>> ImageHistories { get; }

        internal InstanceSetHistory(
            DateTime startDate,
            DateTime endDate,
            IEnumerable<PlacementHistory> instances,
            IDictionary<ulong, ConfigurationHistory<MachineTypeLocator>> machineTypeHistories,
            IDictionary<ulong, ConfigurationHistory<SchedulingPolicy>> schedulingPolicyHistories,
            IDictionary<ulong, ConfigurationHistory<IImageLocator>> imageHistories)
        {
            this.StartDate = startDate;
            this.EndDate = endDate;
            this.PlacementHistories = instances;
            this.MachineTypeHistories = machineTypeHistories;
            this.SchedulingPolicyHistories = schedulingPolicyHistories;
            this.ImageHistories = imageHistories;
        }
    }
}
