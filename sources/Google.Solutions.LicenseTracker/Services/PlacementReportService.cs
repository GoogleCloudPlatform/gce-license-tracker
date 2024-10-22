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
using Google.Solutions.LicenseTracker.Data.History;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;

namespace Google.Solutions.LicenseTracker.Services
{
    public interface IPlacementReportService
    {
        Task<PlacementReport> CreateReport(
            IEnumerable<ProjectLocator> projects,
            uint analysisWindowInDays,
            DateTime startDateInclusive,
            DateTime endDateExclusive,
            CancellationToken cancellationToken);
    }

    public class PlacementReportService : IPlacementReportService
    {
        private readonly ILogger logger;
        private readonly IInstanceHistoryService historyService;
        private readonly ILookupService lookupService;

        public PlacementReportService(
            IInstanceHistoryService historyService,
            ILookupService lookupService,
            ILogger<PlacementReportService> logger)
        {
            this.historyService = historyService;
            this.lookupService = lookupService;
            this.logger = logger;
        }

        public async Task<PlacementReport> CreateReport(
            IEnumerable<ProjectLocator> projects,
            uint analysisWindowInDays,
            DateTime startDateInclusive,
            DateTime endDateExclusive,
            CancellationToken cancellationToken)
        {
            //
            // The reporting window is the time range for which we 
            // want to find placement events.
            //
            // The reporting window might be relatively short and usually
            // ends at or briefly before the current time (for example,
            // last midnight).
            //
            // When analyzing the audit log, it's not sufficient to look
            // at events that happened during the reporting window:
            //
            // (1) If an instance is in running state at the beginning of
            //     the reporting window, we must find out which node it's
            //     running on. The audit log record that contains that 
            //     information will predate the reporting window, so we 
            //     must go further back in history.
            //
            //     We don't know fur sure how far back we need to go,
            //     but 60-90 days is plenty in practice.
            //
            // (2) Audit log records show up with a delay. If an instance
            //     is de-placed (i.e., shut down, moved to another node, or
            //     so) just before the end of the reporting window, we might
            //     miss that event. 
            //
            // To account for these issues, we use an analysis window that:
            //  *  Ends at the current time to address (2)
            //  *  Starts before the reporting window to address (1)
            //
            //                                 Reporting window
            //                                 +---------------+
            //                                 |               |
            // --------------------------------------------------------> time
            //      |                                             |
            //      +---------------------------------------------+
            //                   Analysis window                  ^
            //                                                   NOW
            if (startDateInclusive >= endDateExclusive ||
                endDateExclusive > DateTime.UtcNow)
            {
                throw new ArgumentException("Invalid reporting window");
            }
            else if (startDateInclusive <
                DateTime.UtcNow.AddDays(-((int)analysisWindowInDays)))
            {
                throw new ArgumentException(
                    "Analysis window must begin prior to reporting window");
            }

            this.logger.LogInformation(
                "Analyzing {projects} in date range {start}-{end}, window size {w}",
                projects,
                startDateInclusive,
                endDateExclusive,
                analysisWindowInDays);

            //
            // Reconstruct the history for the analysis window.
            //
            var instanceSetHistory = await this.historyService
                .BuildInstanceSetHistoryAsync(
                    projects,
                    startDateInclusive,
                    analysisWindowInDays,
                    cancellationToken)
                .ConfigureAwait(false);

            //
            // Find out which licenses and machine types were used.
            //
            var licensesTask = this.lookupService.LookupLicenseInfoAsync(
                instanceSetHistory.ImageHistories
                    .Values
                    .SelectMany(history => history.AllValues),
                cancellationToken);
            var machineTypesTask = this.lookupService.LookupMachineInfoAsync(
                instanceSetHistory.MachineTypeHistories
                    .Values
                    .SelectMany(history => history.AllValues),
                cancellationToken);

            var licenseInfoByImage = await licensesTask.ConfigureAwait(false);
            var machineInfoByType = await machineTypesTask.ConfigureAwait(false);

            return new PlacementReport()
            {
                StartedPlacements = instanceSetHistory
                    .PlacementHistories
                    .SelectMany(i => i.Placements
                        .Where(p => p.From >= startDateInclusive)
                        .Select(p => CreatePlacementEvent(i, p)))
                    .ToList(),
                EndedPlacements = instanceSetHistory
                    .PlacementHistories
                    .SelectMany(i => i.Placements
                        .Where(p => p.To < endDateExclusive)
                        .Select(p => CreatePlacementEvent(i, p)))
                    .ToList()
            };

            PlacementEvent CreatePlacementEvent(PlacementHistory i, Placement p)
            {
                var image = instanceSetHistory?
                    .ImageHistories
                    .TryGet(i.InstanceId)?
                    .GetHistoricValue(DateTime.UtcNow);

                var machineType = instanceSetHistory?
                    .MachineTypeHistories
                    .TryGet(i.InstanceId)?
                    .GetHistoricValue(p.From);

                return new PlacementEvent()
                {
                    Instance = i.Reference,
                    InstanceId = i.InstanceId,
                    Image = image,
                    Placement = p,
                    License = image != null 
                        ? licenseInfoByImage?.TryGet(image) 
                        : null,
                    Machine = machineType != null
                        ? machineInfoByType.TryGet(machineType)
                        : null,
                    SchedulingPolicy = instanceSetHistory?
                        .SchedulingPolicyHistories
                        .TryGet(i.InstanceId)?
                        .GetHistoricValue(p.From),
                    Labels = instanceSetHistory?
                        .LabelHistories
                        .TryGet(i.InstanceId)?
                        .GetHistoricValue(p.From),
                };
            }
        }
    }

    public struct PlacementReport
    {
        /// <summary>
        /// Unordered list of placements that started during the analysis window.
        /// </summary>
        public IList<PlacementEvent> StartedPlacements { get; init; }

        /// <summary>
        /// Unordered list of placements that ended during the analysis window.
        /// </summary>
        public IList<PlacementEvent> EndedPlacements { get; init; }
    }

    /// <summary>
    /// A placement event is when an instance started
    /// running on a certain server (= hardware). Examples for actions
    /// that trigger placement events include:
    /// 
    /// * Starting or resuming an instance
    /// * Migrating a sole-tenant instance to a different server
    /// 
    /// </summary>
    public struct PlacementEvent
    {
        /// <summary>
        /// Instance ID, uniquely identifies a VM across
        /// space and time.
        /// </summary>
        public ulong InstanceId { get; init; }

        /// <summary>
        /// Instance name, if known. Names can be reused, so they're
        /// not unique across time.
        /// </summary>
        public InstanceLocator? Instance { get; init; }

        /// <summary>
        /// Image from which the boot disk was created, if known.
        /// </summary>
        public IImageLocator? Image { get; init; }

        /// <summary>
        /// Details for placement.
        /// </summary>
        public Placement Placement { get; init; }

        /// <summary>
        /// License of boot disk, if known.
        /// </summary>
        public LicenseInfo? License { get; init; }

        /// <summary>
        /// Machine info, if known.
        /// </summary>
        public MachineInfo? Machine { get; init; }

        /// <summary>
        /// Scheduling policy, if known.
        /// </summary>
        public SchedulingPolicy? SchedulingPolicy { get; init; }

        /// <summary>
        /// Labels, if known.
        /// </summary>
        public IDictionary<string, string>? Labels { get; init; }
    }
}
