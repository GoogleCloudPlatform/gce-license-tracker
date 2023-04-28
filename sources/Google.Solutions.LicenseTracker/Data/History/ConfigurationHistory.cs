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

using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Data.History
{
    /// <summary>
    /// Machine type configuration history for a specific VM instance.
    /// </summary>
    public class ConfigurationHistory<TConfigurationItem>
        where TConfigurationItem : class
    {
        //
        // Sequence of change events, newest first.
        //
        private readonly List<ConfigurationChange<TConfigurationItem>> changes;

        //
        // Current value (at time of analysis, if the instance still exists).
        //
        private readonly TConfigurationItem? currentValue;

        public ulong InstanceId { get; }

        public ConfigurationHistory(
            ulong instanceId,
            TConfigurationItem? currentValue,
            IEnumerable<ConfigurationChange<TConfigurationItem>> changes)
        {
            this.InstanceId = instanceId;
            this.currentValue = currentValue;
            this.changes = changes.OrderByDescending(c => c.ChangeDate).ToList();
        }

        public TConfigurationItem? GetHistoricValue(DateTime dateTime)
        {
            if (!this.changes.Any())
            {
                return this.currentValue;
            }
            else
            {
                return this.changes
                    .Where(c => c.ChangeDate <= dateTime)
                    .FirstOrDefault()?
                    .NewValue;
            }
        }

        public IEnumerable<TConfigurationItem> AllValues => this.changes
            .EnsureNotNull()
            .Select(c => c.NewValue)
            .Concat(this.currentValue == null
                ? Enumerable.Empty<TConfigurationItem>()
                : new TConfigurationItem[] { this.currentValue });
    }

    public class ConfigurationChange<TConfigurationItem>
    {
        public DateTime ChangeDate { get; }
        public TConfigurationItem NewValue { get; }

        public ConfigurationChange(DateTime changeDate, TConfigurationItem newValue)
        {
            this.ChangeDate = changeDate;
            this.NewValue = newValue;
        }
    }
}
