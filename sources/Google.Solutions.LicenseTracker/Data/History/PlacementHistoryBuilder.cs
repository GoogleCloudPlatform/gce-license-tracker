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

using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.Events.Lifecycle;
using Google.Solutions.LicenseTracker.Data.Events.System;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Data.History
{
    /// <summary>
    /// Reconstructs the placement history of an instance by analyzing
    /// events in reverse chronological order.
    /// </summary>
    internal class PlacementHistoryBuilder
    {
        //
        // NB. Instance IDs stay unique throughout the history while VmInstanceReferences
        // become ambiguous. Therefore, it is important to use instance ID as primary
        // key, even though the reference is more user-friendly and meaningful.
        //

        private readonly ILogger logger;

        public ulong InstanceId { get; }

        public string? ProjectId => this.reference?.ProjectId;

        public Tenancies Tenancy => this.placements.Any()
            ? this.placements.First().Tenancy
            : Tenancies.Unknown;

        //
        // Information accumulated as we go thru history.
        //
        private readonly LinkedList<Placement> placements = new LinkedList<Placement>();

        private InstanceLocator? reference;

        private DateTime? lastStoppedOn;
        private DateTime lastEventDate = DateTime.MaxValue;

        private void AddPlacement(Placement placement)
        {
            if (this.placements.Any())
            {
                var subsequentPlacement = this.placements.First();
                if (placement.IsAdjacent(subsequentPlacement))
                {
                    //
                    // Placement are adjacent -> merge.
                    //
                    placement = placement.Merge(subsequentPlacement);
                    this.placements.RemoveFirst();
                }
            }

            this.placements.AddFirst(placement);
        }

        public void AddPlacement(
            Tenancies tenancy,
            string? serverId,
            NodeTypeLocator? nodeType,
            DateTime date)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(date <= this.lastEventDate);
            Debug.Assert(tenancy == Tenancies.SoleTenant || (serverId == null && nodeType == null));
            this.lastEventDate = date;

            DateTime placedUntil;
            if (this.placements.Count == 0)
            {
                if (this.lastStoppedOn.HasValue)
                {
                    Debug.Assert(this.lastStoppedOn != null);
                    Debug.Assert(date <= this.lastStoppedOn);

                    placedUntil = this.lastStoppedOn.Value;
                }
                else
                {
                    this.logger.LogWarning(
                        "Instance {id} was placed, but never stopped, " +
                        "and yet is not running anymore. Flagging as defunct.",
                        this.InstanceId);
                    return;
                }
            }
            else
            {
                if (this.lastStoppedOn.HasValue)
                {
                    Debug.Assert(date <= this.lastStoppedOn);
                    Debug.Assert(date <= this.placements.First().From);

                    placedUntil = DateTimeUtil.Min(
                        this.lastStoppedOn.Value,
                        this.placements.First().From);
                }
                else
                {
                    Debug.Assert(date <= this.placements.First().From);
                    placedUntil = this.placements.First().From;
                }
            }

            if (tenancy == Tenancies.SoleTenant)
            {
                AddPlacement(new Placement(serverId, nodeType, date, placedUntil));
            }
            else
            {
                AddPlacement(new Placement(date, placedUntil));
            }
        }

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        private PlacementHistoryBuilder(
            ulong instanceId,
            InstanceLocator? reference,
            InstanceState state,
            DateTime? lastSeen,
            Tenancies tenancy,
            string? serverId,
            NodeTypeLocator? nodeType,
            ILogger logger)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(tenancy == Tenancies.SoleTenant || (serverId == null && nodeType == null));

            if (instanceId == 0)
            {
                throw new ArgumentException("Instance ID cannot be 0");
            }

            this.logger = logger;
            this.InstanceId = instanceId;
            this.reference = reference;
            this.lastStoppedOn = lastSeen;

            if (state == InstanceState.Running)
            {
                Debug.Assert(tenancy != Tenancies.Unknown);
                Debug.Assert(lastSeen != null);

                AddPlacement(new Placement(
                    tenancy,
                    serverId,
                    nodeType,
                    lastSeen.Value,
                    lastSeen.Value));
            }
        }

        internal static PlacementHistoryBuilder ForExistingInstance(
            ulong instanceId,
            InstanceLocator reference,
            InstanceState state,
            DateTime lastSeen,
            Tenancies tenancy,
            string? serverId,
            NodeTypeLocator? nodeType,
            ILogger logger)
        {
            Debug.Assert(!tenancy.IsFlagCombination());
            Debug.Assert(state != InstanceState.Deleted);

            return new PlacementHistoryBuilder(
                instanceId,
                reference,
                state,
                lastSeen,
                tenancy,
                serverId,
                nodeType,
                logger);
        }

        internal static PlacementHistoryBuilder ForDeletedInstance(
            ulong instanceId,
            ILogger logger)
        {
            return new PlacementHistoryBuilder(
                instanceId,
                null,
                InstanceState.Deleted,
                (DateTime?)null,    // Not clear yet when it was stopped
                Tenancies.Unknown,
                null,
                null,
                logger);
        }

        public PlacementHistory Build(DateTime reportStartDate)
        {
            IEnumerable<Placement> sanitizedPlacements;

            if (this.placements.Count == 1 &&
                this.placements.First() is Placement firstPlacement &&
                firstPlacement.From == this.lastStoppedOn &&
                firstPlacement.To == this.lastStoppedOn)
            {
                //
                // This instance is running, but we did not see a 
                // start event -- so the instance must have been started
                // even earlier.
                //
                // Keeping the (synthetic) placement would cause statistics
                // to count this instance as not running - therefore, extend
                // the placement so that it covers the entire analyzed time
                // frame. 
                //
                sanitizedPlacements = new[]
                {
                    new Placement(
                        firstPlacement.Tenancy,
                        firstPlacement.ServerId,
                        firstPlacement.NodeType,
                        reportStartDate,
                        firstPlacement.To)
                };
            }
            else if (lastEventDate != DateTime.MaxValue &&
                this.lastStoppedOn != null &&
                (!this.placements.Any() || this.lastStoppedOn < this.placements.First()?.From))
            {
                //
                // We received an event indicating that the instance was
                // stopped, but we did not see a corresponding start
                // event -- so the instance must have been started
                // before the analysis window.
                //
                sanitizedPlacements = new[]
                {
                    new Placement(
                        Tenancies.Unknown,
                        null,
                        null,
                        reportStartDate,
                        lastStoppedOn.Value)
                }.Concat(this.placements);
            }
            else
            {
                 sanitizedPlacements = this.placements;
            }

            Debug.Assert(sanitizedPlacements.All(p => p.From != p.To));

            return new PlacementHistory(
                this.InstanceId,
                this.reference,
                sanitizedPlacements);
        }

        //---------------------------------------------------------------------
        // Lifecycle events that construct the history.
        //---------------------------------------------------------------------

        public void OnStart(DateTime date, InstanceLocator reference)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            if (this.reference == null)
            {
                this.reference = reference;
            }

            //
            // Register Fleet placement - this might be merged with an existing
            // SoleTenant placement if there has been one registerd before.
            //
            AddPlacement(Tenancies.Fleet, null, null, date);
        }

        public void OnStop(DateTime date, InstanceLocator reference)
        {
            Debug.Assert(date <= this.lastEventDate);
            this.lastEventDate = date;

            this.lastStoppedOn = date;

            if (this.reference == null)
            {
                this.reference = reference;
            }
        }

        public void OnSetPlacement(string serverId, NodeTypeLocator? nodeType, DateTime date)
        {
            Debug.Assert(date <= this.lastEventDate);
            Debug.Assert(serverId != null);
            this.lastEventDate = date;

            //
            // NB. While the serverId is always populated, the nodeType
            // is null for events emitted before August 2020.
            //

            AddPlacement(Tenancies.SoleTenant, serverId, nodeType, date);
        }

        public void ProcessEvent(EventBase e)
        {
            if (e is NotifyInstanceLocationEvent notifyLocation && notifyLocation.ServerId != null)
            {
                OnSetPlacement(
                    notifyLocation.ServerId,
                    notifyLocation.NodeType,
                    notifyLocation.Timestamp);
            }
            else if (e is IInstanceStateChangeEvent stateChange)
            {
                if (stateChange.IsStartingInstance)
                {
                    OnStart(e.Timestamp, ((InstanceEventBase)e).InstanceReference!);
                }
                else if (stateChange.IsTerminatingInstance)
                {
                    OnStop(e.Timestamp, ((InstanceEventBase)e).InstanceReference!);
                }
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
