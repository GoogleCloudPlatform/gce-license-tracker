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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Services
{
    public class MachineInfo
    {
        /// <summary>
        /// Machine type.
        /// </summary>
        public MachineTypeLocator Type { get; }

        /// <summary>
        /// Number of vCPU.
        /// </summary>
        public uint VirtualCpuCount { get; }

        /// <summary>
        /// RAM, in MB.
        /// </summary>
        public uint MemoryMb { get; }

        internal MachineInfo(
            MachineTypeLocator machineType,
            uint virtualCpuCount, 
            uint memoryMb)
        {
            this.Type = machineType;
            this.VirtualCpuCount = virtualCpuCount;
            this.MemoryMb = memoryMb;
        }

        public override string ToString()
        {
            return $"{this.Type.Name} ({this.VirtualCpuCount} vCPU, {this.MemoryMb} RAM)";
        }
    }
}
