﻿//
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

using Google.Solutions.LicenseTracker.Data.Logs;

namespace Google.Solutions.LicenseTracker.Data.Events.Config
{
    internal class SetSchedulingEvent : InstanceOperationEventBase
    {
        public const string Method = "v1.compute.instances.setScheduling";
        public const string BetaMethod = "beta.compute.instances.setScheduling";

        public SchedulingPolicy? SchedulingPolicy { get; }

        public SetSchedulingEvent(LogRecord logRecord) : base(logRecord)
        {
            if (logRecord.ProtoPayload?.Request?.Value<string>("onHostMaintenance") 
                is var maintenancePolicy && !string.IsNullOrEmpty(maintenancePolicy))
            {
                this.SchedulingPolicy = new SchedulingPolicy(
                    maintenancePolicy,
                    logRecord.ProtoPayload?.Request?.Value<uint?>("minNodeCpus"));
            }
        }

        public static bool IsSetSchedulingEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                (record.ProtoPayload?.MethodName == Method || record.ProtoPayload?.MethodName == BetaMethod);
        }
    }
}