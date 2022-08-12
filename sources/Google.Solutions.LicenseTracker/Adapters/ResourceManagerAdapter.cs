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

using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Util;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;

namespace Google.Solutions.LicenseTracker.Adapters
{
    public interface IResourceManagerAdapter : IDisposable
    {
        Task<IEnumerable<Project>> ListProjectsAsync(
            CancellationToken cancellationToken);

        Task<bool> IsGrantedPermissionsAsync(
            ProjectLocator project,
            IEnumerable<string> permissions,
            CancellationToken cancellationToken);
    }

    internal class ResourceManagerAdapter : IResourceManagerAdapter
    {
        private readonly CloudResourceManagerService service;

        public ResourceManagerAdapter(ICredential credential)
        {
            this.service = new CloudResourceManagerService(
                new Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = UserAgent.Default.ToString()
                });
        }

        public async Task<IEnumerable<Project>> ListProjectsAsync(
            CancellationToken cancellationToken)
        {
            var request = new ProjectsResource.ListRequest(this.service)
            {
                PageSize = 1000
            };

            var projects = await new PageStreamer<
                Project,
                ProjectsResource.ListRequest,
                ListProjectsResponse,
                string>(
                    (req, token) => req.PageToken = token,
                    response => response.NextPageToken,
                    response => response.Projects)
                .FetchAllAsync(request, cancellationToken)
                .ConfigureAwait(false);

            //
            // Ignore projects in deleted/pending delete state.
            //
            return projects
                .EnsureNotNull()
                .Where(p => p.LifecycleState == "ACTIVE")
                .ToList();
        }

        public async Task<bool> IsGrantedPermissionsAsync(
            ProjectLocator project,
            IEnumerable<string> permissions,
            CancellationToken cancellationToken)
        {
            Utilities.ThrowIfNull(project, nameof(project));
            Utilities.ThrowIfNull(permissions, nameof(permissions));

            var response = await this.service.Projects.TestIamPermissions(
                    new TestIamPermissionsRequest()
                    {
                        Permissions = permissions.ToList()
                    },
                    project.ProjectId)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return response != null &&
                response.Permissions != null &&
                permissions.All(p => response.Permissions.Contains(p));
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.service.Dispose();
            }
        }
    }
}
