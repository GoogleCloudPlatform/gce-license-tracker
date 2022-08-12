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
using Google.Apis.ServiceUsage.v1;
using Google.Solutions.LicenseTracker.Data.Locator;

namespace Google.Solutions.LicenseTracker.Adapters
{
    public interface IServiceUsageAdapter
    {
        Task<bool> IsServiceEnabledAsync(
            ProjectLocator project,
            string service,
            CancellationToken cancellationToken);
    }

    internal class ServiceUsageAdapter : IServiceUsageAdapter
    {
        private readonly ServiceUsageService service;

        public ServiceUsageAdapter(ICredential credential)
        {
            this.service = new ServiceUsageService(
                new Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = UserAgent.Default.ToString()
                });
        }

        public async Task<bool> IsServiceEnabledAsync(
            ProjectLocator project,
            string service,
            CancellationToken cancellationToken)
        {
            var response = await this.service.Services.Get(
                $"projects/{project.ProjectId}/services/{service}")
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return response.State == "ENABLED";
        }
    }
}
