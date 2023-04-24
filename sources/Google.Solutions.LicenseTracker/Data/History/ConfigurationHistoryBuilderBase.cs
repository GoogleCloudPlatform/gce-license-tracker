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

namespace Google.Solutions.LicenseTracker.Data.History
{
    public abstract class ConfigurationHistoryBuilderBase<TConfigurationItem>
        where TConfigurationItem : class
    {
        protected readonly LinkedList<ConfigurationChange<TConfigurationItem>> changes =
            new LinkedList<ConfigurationChange<TConfigurationItem>>();
        private readonly TConfigurationItem? currentMachineType;

        public ulong InstanceId { get; }

        public ConfigurationHistoryBuilderBase(
            ulong instanceId,
            TConfigurationItem? currentMachineType)
        {
            this.InstanceId = instanceId;
            this.currentMachineType = currentMachineType;
        }

        public ConfigurationHistory<TConfigurationItem> Build()
        {
            return new ConfigurationHistory<TConfigurationItem>(
                this.InstanceId,
                this.currentMachineType,
                this.changes);
        }

        public abstract void ProcessEvent(EventBase e);
    }
}
