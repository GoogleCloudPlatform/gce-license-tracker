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

using Google.Solutions.LicenseTracker.Data.Locator;

namespace Google.Solutions.LicenseTracker.Data.History
{
    /// <summary>
    /// Placement history for a specific VM instance.
    /// </summary>
    public class PlacementHistory
    {
        public ulong InstanceId { get; }

        public InstanceLocator? Reference { get; }

        public IEnumerable<Placement> Placements { get; }

        public IImageLocator? Image { get; }

        public InstanceHistoryState State { get; }

        internal PlacementHistory(
            ulong instanceId,
            InstanceLocator? reference,
            InstanceHistoryState state,
            IImageLocator? image,
            IEnumerable<Placement> placements)
        {
            this.InstanceId = instanceId;
            this.Reference = reference;
            this.State = state;
            this.Image = image;
            this.Placements = placements;
        }

        public override string ToString()
        {
            return $"{this.Reference} ({this.InstanceId})";
        }
    }

    public enum InstanceHistoryState
    {
        Complete,
        MissingTenancy,
        MissingName,
        MissingImage,
        MissingStopEvent
    }
}
