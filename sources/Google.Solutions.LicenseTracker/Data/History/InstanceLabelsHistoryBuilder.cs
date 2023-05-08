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
using Google.Solutions.LicenseTracker.Data.Events.Lifecycle;
using Google.Solutions.LicenseTracker.Data.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Data.History
{
    /// <summary>
    /// Reconstructs the history of machine type configurations
    /// for a given instance by analyzing events in reverse 
    /// chronological order.
    /// </summary>
    public class InstanceLabelsHistoryBuilder
        : ConfigurationHistoryBuilderBase<IDictionary<string, string>>
    {
        public InstanceLabelsHistoryBuilder(
            ulong instanceId,
            IDictionary<string, string>? currentLabels)
            : base(instanceId, currentLabels)
        {
        }

        public override void ProcessEvent(EventBase e)
        {
            if (e is InsertInstanceEvent insert && !insert.IsError && insert.Labels != null)
            {
                this.changes.AddLast(new ConfigurationChange<IDictionary<string, string>>(
                    insert.Timestamp,
                    insert.Labels));
            }
            else if (e is SetLabelsEvent setType && !setType.IsError && setType.Labels != null)
            {
                this.changes.AddLast(new ConfigurationChange<IDictionary<string, string>>(
                    setType.Timestamp,
                    setType.Labels));
            }
            else if (e is UpdateInstanceEvent update && !update.IsError && update.Labels != null)
            {
                this.changes.AddLast(new ConfigurationChange<IDictionary<string, string>>(
                    update.Timestamp,
                    update.Labels));
            }
            else
            {
                //
                // This event is not relevant for us.
                //
            }
        }
    }
}
