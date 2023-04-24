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
        private readonly ILicenseService licenseService;

        public PlacementReportService(
            IInstanceHistoryService historyService,
            ILicenseService licenseService,
            ILogger<PlacementReportService> logger)
        {
            this.historyService = historyService;
            this.licenseService = licenseService;
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
            // Find out which licenses were used.
            //
            var licenses = await this.licenseService.LookupLicenseInfoAsync(
                    instanceSetHistory.PlacementHistories
                        .Where(i => i.Image != null)
                        .Select(i => i.Image!),
                    cancellationToken)
                .ConfigureAwait(false);

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
                return new PlacementEvent()
                {
                    Instance = i.Reference,
                    InstanceId = i.InstanceId,
                    Image = i.Image,
                    Placement = p,
                    License = i.Image != null ? licenses?.TryGet(i.Image) : null,
                    MachineType = instanceSetHistory?
                        .MachineTypeHistories
                        .TryGet(i.InstanceId)?
                        .GetHistoricValue(p.From)
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
        /// Machine type, if known.
        /// </summary>
        public MachineTypeLocator? MachineType { get; init; }
    }
}
