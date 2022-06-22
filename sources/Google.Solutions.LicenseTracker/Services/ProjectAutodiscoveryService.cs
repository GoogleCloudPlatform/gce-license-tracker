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

using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.Locator;

namespace Google.Solutions.LicenseTracker.Services
{
    public interface IProjectAutodiscoveryService
    {
        Task<IEnumerable<ProjectLocator>> DiscoverAccessibleProjects(
            IEnumerable<string> requiredPermissions,
            string requiredService,
            CancellationToken cancellationToken);
    }

    internal class ProjectAutodiscoveryService : IProjectAutodiscoveryService
    {
        private readonly IResourceManagerAdapter resourceManagerAdapter;
        private readonly IServiceUsageAdapter serviceUsageAdapter;

        public ProjectAutodiscoveryService(
            IResourceManagerAdapter resourceManagerAdapter,
            IServiceUsageAdapter serviceUsageAdapter)
        {
            this.resourceManagerAdapter = resourceManagerAdapter;
            this.serviceUsageAdapter = serviceUsageAdapter;
        }

        private async Task<bool> IsProjectAccessible(
            ProjectLocator project,
            IEnumerable<string> requiredPermissions,
            string requiredService,
            CancellationToken cancellationToken)
        {
            var checkPermissionsTask = this.resourceManagerAdapter
                .IsGrantedPermissionsAsync(
                    project,
                    requiredPermissions,
                    cancellationToken);

            var checkApiTask = this.serviceUsageAdapter
                .IsServiceEnabledAsync(
                    project,
                    requiredService,
                    cancellationToken);


            return
                await checkPermissionsTask.ConfigureAwait(false) &&
                await checkApiTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Discover projects for which the user has a set of permissions.
        /// </summary>
        public async Task<IEnumerable<ProjectLocator>> DiscoverAccessibleProjects(
            IEnumerable<string> requiredPermissions,
            string requiredService,
            CancellationToken cancellationToken)
        {
            var projects = await this.resourceManagerAdapter
                .ListProjectsAsync(cancellationToken)
                .ConfigureAwait(false);

            //
            // Kick of a permisison check for all projects.
            //

            var accessibleProjects = new List<ProjectLocator>();
            foreach (var chunk in projects.Chunk(10))
            {
                var checkAccessTasksByProjectId = chunk
                    .Select(project => KeyValuePair.Create(
                        project,
                        IsProjectAccessible(
                            new ProjectLocator(project.ProjectId),
                            requiredPermissions,
                            requiredService,
                            cancellationToken)))
                    .ToDictionary(k => k.Key.ProjectId, k => k.Value);

                await Task
                    .WhenAll(checkAccessTasksByProjectId.Values)
                    .ConfigureAwait(false);

                //
                // Filter by projects for which we have sufficient permissions.
                //
                accessibleProjects.AddRange(checkAccessTasksByProjectId
                    .Where(kvp => kvp.Value.Result)
                    .Select(kvp => new ProjectLocator(kvp.Key))
                    .ToList());
            }

            return accessibleProjects;
        }
    }
}
