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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.History;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Services.Adapters;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Services
{
    public interface IInstanceHistoryService
    {
        /// <summary>
        /// Reconstruct the history for VMs in a given set of projects.
        /// </summary>
        Task<InstanceSetHistory> BuildInstanceSetHistoryAsync(
            IEnumerable<ProjectLocator> projectIds,
            DateTime startDate,
            uint analysisWindowSizeInDays,
            CancellationToken cancellationToken);
    }

    internal class InstanceHistoryService : IInstanceHistoryService
    {
        private readonly ILogger logger;
        private readonly IAuditLogAdapter auditLogAdapter;
        private readonly IComputeEngineAdapter computeEngineAdapter;

        public InstanceHistoryService(
            IAuditLogAdapter auditLogAdapter,
            IComputeEngineAdapter computeEngineAdapter,
            ILogger<InstanceHistoryService> logger)
        {
            this.auditLogAdapter = auditLogAdapter;
            this.computeEngineAdapter = computeEngineAdapter;
            this.logger = logger;
        }

        private async Task<IList<NodeGroupNode>> ListNodesAsync(
            IEnumerable<ProjectLocator> projectIds,
            CancellationToken cancellationToken)
        {
            var tasks = new Dictionary<ProjectLocator, Task<IEnumerable<NodeGroupNode>>>();
            foreach (var projectId in projectIds)
            {
                tasks[projectId] = this.computeEngineAdapter.ListNodesAsync(
                    projectId,
                    cancellationToken);
            }

            Debug.Assert(tasks.Count == projectIds.Count());

            var nodes = new List<NodeGroupNode>();
            foreach (var task in tasks)
            {
                try
                {
                    nodes.AddRange(await task.Value.ConfigureAwait(false));
                }
                catch (ResourceAccessDeniedException e)
                {
                    this.logger.LogWarning(
                        "Ignoring project {project} as it is inaccessible: {ex}",
                        task.Key.ProjectId,
                        e.FullMessage());
                }
            }

            return nodes;
        }

        public async Task<InstanceSetHistory> BuildInstanceSetHistoryAsync(
            IEnumerable<ProjectLocator> projectIds,
            DateTime startDate,
            uint analysisWindowSizeInDays,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            Debug.Assert(startDate.Kind == DateTimeKind.Utc);
            Debug.Assert(startDate > now.AddDays(-((int)analysisWindowSizeInDays)));

            var builder = new InstanceSetHistoryBuilder(
                startDate,
                now,
                this.logger);

            //
            // (1) Take inventory of all sole-tenant nodes. These nodes
            //     might be shared across projects.
            //
            var nodes = ListNodesAsync(projectIds, cancellationToken);

            //
            // (2) Take inventory of all instances, and try to associate
            //     them with sole-tenant nodes.
            //
            foreach (var projectId in projectIds)
            {
                this.logger.LogInformation(
                    "Analyzing placement history for project {project}...",
                    projectId);

                try
                {
                    //
                    // Load disks.
                    //
                    // NB. Instances.list returns the disks associated with each
                    // instance, but lacks the information about the source image.
                    // Therefore, we load disks first and then join the data.
                    //
                    var disks = this.computeEngineAdapter.ListDisksAsync(
                        projectId,
                        cancellationToken);

                    //
                    // Load instances.
                    //
                    var instances = this.computeEngineAdapter.ListInstancesAsync(
                        projectId,
                        cancellationToken);

                    builder.AddExistingInstances(
                        await instances.ConfigureAwait(false),
                        await nodes.ConfigureAwait(false),
                        await disks.ConfigureAwait(false),
                        projectId);

                    //
                    // Query logs to replay history.
                    // 
                    // NB. We could query mutliple projects at once, but the API
                    // quickly becomes unreliable (read: throws random 500 errors)
                    // when doing so. Therefore, we're conservative and only query
                    // one project at a time.
                    //
                    await this.auditLogAdapter
                        .ProcessInstanceEventsAsync(
                            new[] { projectId },
                            builder.StartDate,
                            builder,
                            cancellationToken)
                        .ConfigureAwait(false);

                    this.logger.LogInformation(
                        "Finished analyzing placement history for project {project}...",
                        projectId);
                }
                catch (ResourceAccessDeniedException e)
                {
                    this.logger.LogWarning(
                        "Ignoring project {project} as it is inaccessible: {ex}",
                        projectId,
                        e.FullMessage());
                }
            }

            return builder.Build();
        }
    }
}
