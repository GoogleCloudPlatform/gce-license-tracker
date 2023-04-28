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
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;

namespace Google.Solutions.LicenseTracker.Services
{
    public interface ILicenseService
    {
        Task<IDictionary<IImageLocator, LicenseInfo>> LookupLicenseInfoAsync(
            IEnumerable<IImageLocator> images,
            CancellationToken cancellationToken);
    }

    internal class LicenseService : ILicenseService
    {
        private readonly ILogger logger;
        private readonly IComputeEngineAdapter computeEngineAdapter;

        public LicenseService(
            IComputeEngineAdapter computeEngineAdapter,
            ILogger<LicenseService> logger)
        {
            this.computeEngineAdapter = computeEngineAdapter;
            this.logger = logger;
        }


        private static LicenseLocator? TryGetRelevantLicenseFromImage(Image imageInfo)
        {
            var locators = imageInfo.Licenses
                .EnsureNotNull()
                .Select(license => LicenseLocator.FromString(license));

            //
            // Images can contain more than one license, and liceses like 
            // "/compute/v1/projects/compute-image-tools/global/licenses/virtual-disk-import"
            // are not helpful here. So do some filtering.
            //
            if (locators.FirstOrDefault(l => l.IsWindowsByolLicense()) is LicenseLocator byolLocator)
            {
                return byolLocator;
            }
            else if (locators.FirstOrDefault(l => l.IsWindowsLicense()) is LicenseLocator winLocator)
            {
                return winLocator;
            }
            else
            {
                return locators.FirstOrDefault();
            }
        }

        public async Task<IDictionary<IImageLocator, LicenseInfo>> LookupLicenseInfoAsync(
            IEnumerable<IImageLocator> images,
            CancellationToken cancellationToken)
        {
            var licenses = new Dictionary<IImageLocator, LicenseInfo>();
            foreach (var image in images.Distinct())
            {
                try
                {
                    Image imageInfo = await this.computeEngineAdapter
                        .GetImageAsync(image, cancellationToken)
                        .ConfigureAwait(false);

                    // Images can contain more than one license, and liceses like 
                    // "/compute/v1/projects/compute-image-tools/global/licenses/virtual-disk-import"
                    // are not helpful here. So do some filtering.

                    licenses[image] = LicenseInfo.FromLicense(
                        TryGetRelevantLicenseFromImage(imageInfo));
                }
                catch (ResourceNotFoundException) when (image.ProjectId == "windows-cloud")
                {
                    //
                    // That image might not exist anymore, but we know it's
                    // a Windows SPLA image.
                    //

                    licenses[image] = new LicenseInfo(
                        null,
                        OperatingSystemTypes.Windows,
                        LicenseTypes.Spla);

                    this.logger.LogWarning(
                        "License for {0} could not be found, but must be Windows/SPLA", image);
                }
                catch (ResourceNotFoundException e)
                {
                    // Unknown or inaccessible image, skip.
                    this.logger.LogWarning(
                        "License for {0} could not be found: {0}", image, e);
                }
                catch (ResourceAccessDeniedException e)
                {
                    // Unknown or inaccessible image, skip.
                    this.logger.LogWarning(
                        "License for {0} could not be accessed: {0}", image, e);
                }
            }

            return licenses;
        }
    }
}
