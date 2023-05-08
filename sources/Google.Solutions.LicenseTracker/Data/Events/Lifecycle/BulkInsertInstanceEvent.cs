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
using Google.Solutions.LicenseTracker.Data.Logs;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Data.Events.Lifecycle
{
    public class BulkInsertInstanceEvent : InstanceOperationEventBase, IInstanceStateChangeEvent
    {
        public const string Method = "v1.compute.instances.bulkInsert";

        internal BulkInsertInstanceEvent(LogRecord logRecord) : base(logRecord)
        {
            //
            // NB. For bulk-inserts, there's a single log record with start=true
            // and multiple log records with (first=true, last=true). The initial 
            // record contains configuration details, but no instance IDs. The 
            // subsequent records contain instance IDs, but no configuration details.
            //

            Debug.Assert(IsBulkInsertInstanceEvent(logRecord));
        }

        public static bool IsBulkInsertInstanceEvent(LogRecord record)
        {
            return record.IsActivityEvent && (record.ProtoPayload?.MethodName == Method);
        }

        //---------------------------------------------------------------------
        // IInstanceStateChangeEvent.
        //---------------------------------------------------------------------

        public bool IsStartingInstance => !this.IsError && this.IsFirst && this.IsLast;

        public bool IsTerminatingInstance => false;
    }
}