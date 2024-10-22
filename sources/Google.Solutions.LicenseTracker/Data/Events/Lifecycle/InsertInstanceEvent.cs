﻿//
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
using Google.Solutions.LicenseTracker.Data.Logs;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Data.Events.Lifecycle
{
    public class InsertInstanceEvent : InstanceOperationEventBase, IInstanceStateChangeEvent
    {
        public const string Method = "v1.compute.instances.insert";
        public const string BetaMethod = "beta.compute.instances.insert";

        public IImageLocator? Image { get; }
        public MachineTypeLocator? MachineType { get; }

        public SchedulingPolicy? SchedulingPolicy { get; }
        public IDictionary<string, string>? Labels { get; }

        internal InsertInstanceEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsInsertInstanceEvent(logRecord));

            var request = logRecord.ProtoPayload?.Request;
            if (request != null)
            {
                var disks = request["disks"];
                if (disks != null)
                {
                    foreach (var disk in ((JArray)disks))
                    {
                        if (disk["boot"] != null && (bool)disk["boot"]!)
                        {
                            var sourceImage = disk?["initializeParams"]?["sourceImage"]?.Value<string>();
                            if (sourceImage != null)
                            {
                                //
                                // NB. The insert event contains the "raw" source of the image,
                                // which can be:
                                // - a specific (global) image
                                // - a (global) image family
                                // - a zonal image family
                                //
                                // This is unlike the disks.* API which returns the "resolved"
                                // source of the image.
                                //
                                if (sourceImage.Contains("/imageFamilyViews/"))
                                {
                                    this.Image = ImageFamilyViewLocator.FromString(
                                        sourceImage,
                                        logRecord.Resource?.Labels?["zone"]);
                                }
                                else
                                {
                                    this.Image = ImageLocator.FromString(sourceImage);
                                }
                            }
                        }
                    }
                }

                if (request.Value<string>("machineType") is var machineType &&
                    !string.IsNullOrEmpty(machineType) &&
                    this.InstanceReference != null)
                {
                    if (machineType.StartsWith("zones/"))
                    {
                        // b/281762203.
                        machineType = $"projects/{this.InstanceReference.ProjectId}/{machineType}";
                    }

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
                            Key = (string?)item.Value<string?>("key"),
                            Value = (string?)item.Value<string?>("value")
                        })
                        .Where(item => item.Key != null && item.Value != null)
                        .DistinctBy(item => item.Key)
                        .ToDictionary(kvp => kvp.Key!, kvp => kvp.Value!);
                }
            }
        }

        public static bool IsInsertInstanceEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                (record.ProtoPayload?.MethodName == Method ||
                 record.ProtoPayload?.MethodName == BetaMethod);
        }

        //---------------------------------------------------------------------
        // IInstanceStateChangeEvent.
        //---------------------------------------------------------------------

        public bool IsStartingInstance => !this.IsError;

        public bool IsTerminatingInstance => false;
    }
}