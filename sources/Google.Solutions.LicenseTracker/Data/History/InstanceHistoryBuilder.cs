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
using Google.Solutions.LicenseTracker.Data.Locator;

namespace Google.Solutions.LicenseTracker.Data.History
{
    internal class InstanceHistoryBuilder
    {
        private readonly PlacementHistoryBuilder placementHistoryBuilder;
        private readonly MachineTypeConfigurationHistoryBuilder machineTypeConfigurationHistoryBuilder;

        public InstanceHistoryBuilder(
            PlacementHistoryBuilder placementHistoryBuilder, 
            MachineTypeConfigurationHistoryBuilder machineTypeConfigurationHistoryBuilder)
        {
            this.placementHistoryBuilder = placementHistoryBuilder;
            this.machineTypeConfigurationHistoryBuilder = machineTypeConfigurationHistoryBuilder;
        }

        public PlacementHistory BuildPlacementHistory(DateTime startDate)
        {
            return this.placementHistoryBuilder.Build(startDate);
        }

        public ConfigurationHistory<MachineTypeLocator> BuildMachineTypeHistory()
        {
            return this.machineTypeConfigurationHistoryBuilder.Build();
        }

        internal void ProcessEvent(EventBase e)
        {
            //
            // Let all builders process the event so that they can construct
            // their individual perspective of history.
            //
            this.placementHistoryBuilder.ProcessEvent(e);
            this.machineTypeConfigurationHistoryBuilder.ProcessEvent(e);
        }
    }
}
