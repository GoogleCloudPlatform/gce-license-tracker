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
using Google.Solutions.LicenseTracker.Data.Logs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Data.Events.Config
{
    internal class SetLabelsEvent : InstanceOperationEventBase
    {
        public const string Method = "v1.compute.instances.setLabels";

        public IDictionary<string, string>? Labels { get; }

        public SetLabelsEvent(LogRecord logRecord) : base(logRecord)
        {
            if (logRecord.ProtoPayload?.Request?["labels"] is var labels &&
                labels != null)
            {
                this.Labels = labels
                    .OfType<JObject>()
                    .Select(item => new {
                        Key = (string?)item.Value<string?>("key"),
                        Value = (string?)item.Value<string?>("value")
                    })
                    .Where(item => item.Key != null && item.Value != null)
                    .DistinctBy(item => item.Key)
                    .ToDictionary(kvp => kvp.Key!, kvp => kvp.Value!);
            }
        }

        public static bool IsSetLabelsEvent(LogRecord record)
        {
            return record.IsActivityEvent && record.ProtoPayload?.MethodName == Method;
        }
    }
}
