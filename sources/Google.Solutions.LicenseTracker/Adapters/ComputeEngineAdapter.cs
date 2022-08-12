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
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Apis.Requests;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;

namespace Google.Solutions.LicenseTracker.Adapters
{
    public interface IComputeEngineAdapter
    {
        Task<IEnumerable<Disk>> ListDisksAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken);

        Task<Image> GetImageAsync(
            IImageLocator image,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroup>> ListNodeGroupsAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken);

        Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            ZoneLocator zone,
            string nodeGroup,
            CancellationToken cancellationToken);

        Task<IEnumerable<Instance>> ListInstancesAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken);
    }

    internal class ComputeEngineAdapter : IComputeEngineAdapter
    {
        internal static readonly string[] AllRequiredPermissions = new[] {
            "compute.disks.list",
            "compute.images.get",
            "compute.nodeGroups.list",
            "compute.instances.list",
        };

        private readonly ComputeService service;

        public ComputeEngineAdapter(
            ICredential credential)
        {
            this.service = new ComputeService(
                new Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = UserAgent.Default.ToString()
                });
        }

        //---------------------------------------------------------------------
        // Disks.
        //---------------------------------------------------------------------

        public async Task<IEnumerable<Disk>> ListDisksAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken)
        {
            try
            {
                var disksByZone = await new PageStreamer<
                    DisksScopedList,
                    DisksResource.AggregatedListRequest,
                    DiskAggregatedList,
                    string>(
                        (req, token) => req.PageToken = token,
                        response => response.NextPageToken,
                        response => response.Items.Values.Where(v => v != null))
                    .FetchAllAsync(
                        this.service.Disks.AggregatedList(projectId.ProjectId),
                        cancellationToken)
                    .ConfigureAwait(false);

                var result = disksByZone
                    .Where(z => z.Disks != null)    // API returns null for empty zones.
                    .SelectMany(zone => zone.Disks);

                return result;
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Access to disks in project {projectId} has been denied",
                    e);
            }
        }

        //---------------------------------------------------------------------
        // Images.
        //---------------------------------------------------------------------

        public async Task<Image> GetImageAsync(
            IImageLocator image,
            CancellationToken cancellationToken)
        {
            try
            {
                if (image is ImageFamilyViewLocator zonalImage)
                {
                    var view = await this.service.ImageFamilyViews
                        .Get(zonalImage.ProjectId, zonalImage.Zone, zonalImage.Name)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                    return view.Image;
                }
                else if (image.Name.StartsWith("family/"))
                {
                    return await this.service.Images
                        .GetFromFamily(image.ProjectId, image.Name.Substring(7))
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    return await this.service.Images
                        .Get(image.ProjectId, image.Name)
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e) when (
                e.Unwrap() is GoogleApiException apiEx && apiEx.IsNotFoundError())
            {
                throw new ResourceNotFoundException($"Image {image} not found", e);
            }
            catch (Exception e) when (
                e.Unwrap() is GoogleApiException apiEx && apiEx.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException($"Access to {image} denied", e);
            }
        }

        //---------------------------------------------------------------------
        // Nodes.
        //---------------------------------------------------------------------

        public async Task<IEnumerable<NodeGroup>> ListNodeGroupsAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken)
        {
            try
            {
                var groupsByZone = await new PageStreamer<
                    NodeGroupsScopedList,
                    NodeGroupsResource.AggregatedListRequest,
                    NodeGroupAggregatedList,
                    string>(
                        (req, token) => req.PageToken = token,
                        response => response.NextPageToken,
                        response => response.Items.Values.Where(v => v != null))
                    .FetchAllAsync(
                        this.service.NodeGroups.AggregatedList(projectId.ProjectId),
                        cancellationToken)
                    .ConfigureAwait(false);

                var result = groupsByZone
                    .Where(z => z.NodeGroups != null)    // API returns null for empty zones.
                    .SelectMany(zone => zone.NodeGroups);

                return result;
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Access to node groups in project {projectId} has been denied",
                    e);
            }
        }
        public async Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            ZoneLocator zone,
            string nodeGroup,
            CancellationToken cancellationToken)
        {
            try
            {
                return await new PageStreamer<
                    NodeGroupNode,
                    NodeGroupsResource.ListNodesRequest,
                    NodeGroupsListNodes,
                    string>(
                        (req, token) => req.PageToken = token,
                        response => response.NextPageToken,
                        response => response.Items)
                    .FetchAllAsync(
                        this.service.NodeGroups.ListNodes(zone.ProjectId, zone.Name, nodeGroup),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Access to nodes in project {zone.ProjectId} has been denied",
                    e);
            }
        }

        public async Task<IEnumerable<NodeGroupNode>> ListNodesAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken)
        {
            var nodeGroups = await ListNodeGroupsAsync(
                    projectId,
                    cancellationToken)
                .ConfigureAwait(false);

            var nodesAcrossGroups = Enumerable.Empty<NodeGroupNode>();

            foreach (var nodeGroup in nodeGroups)
            {
                nodesAcrossGroups = nodesAcrossGroups.Concat(await ListNodesAsync(
                        ZoneLocator.FromString(nodeGroup.Zone),
                        nodeGroup.Name,
                        cancellationToken)
                    .ConfigureAwait(false));
            }

            return nodesAcrossGroups;
        }


        //---------------------------------------------------------------------
        // Instances.
        //---------------------------------------------------------------------
        public async Task<IEnumerable<Instance>> ListInstancesAsync(
            ProjectLocator projectId,
            CancellationToken cancellationToken)
        {
            try
            {
                var instancesByZone = await new PageStreamer<
                    InstancesScopedList,
                    InstancesResource.AggregatedListRequest,
                    InstanceAggregatedList,
                    string>(
                        (req, token) => req.PageToken = token,
                        response => response.NextPageToken,
                        response => response.Items.Values.Where(v => v != null))
                    .FetchAllAsync(
                        this.service.Instances.AggregatedList(projectId.ProjectId),
                        cancellationToken)
                    .ConfigureAwait(false);

                var result = instancesByZone
                    .Where(z => z.Instances != null)    // API returns null for empty zones.
                    .SelectMany(zone => zone.Instances);

                return result;
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    "You do not have sufficient permissions to list VM instances in " +
                    $"project {projectId}. " +
                    "You need the 'Compute Viewer' role (or an equivalent custom role) " +
                    "to perform this action.",
                    e);
            }
        }
    }
}
