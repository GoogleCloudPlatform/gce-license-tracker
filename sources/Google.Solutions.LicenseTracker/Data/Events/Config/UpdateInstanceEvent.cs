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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Logs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Data.Events.Config
{
    internal class UpdateInstanceEvent : InstanceOperationEventBase
    {
        public const string Method = "v1.compute.instances.update";
        public const string BetaMethod = "beta.compute.instances.update";

        public MachineTypeLocator? MachineType { get; }
        public SchedulingPolicy? SchedulingPolicy { get; }
        public IDictionary<string, string>? Labels { get; }

        public UpdateInstanceEvent(LogRecord logRecord) : base(logRecord)
        {
            var request = logRecord.ProtoPayload?.Request;
            if (request != null)
            {
                if (request?.Value<string>("machineType") is var machineType &&
                    !string.IsNullOrEmpty(machineType))
                {
                    this.MachineType = MachineTypeLocator.FromString(machineType);
                }

                if (request?["scheduling"] is var schedulingPolicy &&
                    schedulingPolicy != null)
                {
                    this.SchedulingPolicy = new SchedulingPolicy(
                        schedulingPolicy.Value<string>("onHostMaintenance") ?? "TERMINATE",
                        schedulingPolicy.Value<uint?>("minNodeCpus"));
                }

                if (request?["labels"] is var labels && labels != null)
                {
                    this.Labels = labels
                        .OfType<JObject>()
                        .Select(item => new {
                            Key = (string?)item.PropertyValues().ElementAtOrDefault(0),
                            Value = (string?)item.PropertyValues().ElementAtOrDefault(1)
                        })
                        .Where(item => item.Key != null && item.Value != null)
                        .ToDictionary(kvp => kvp.Key!, kvp => kvp.Value!);
                }
            }
        }

        public static bool IsUpdateInstanceEvent(LogRecord record)
        {
            return record.IsActivityEvent && 
                (record.ProtoPayload?.MethodName == Method || record.ProtoPayload?.MethodName == BetaMethod);
        }
    }
}
