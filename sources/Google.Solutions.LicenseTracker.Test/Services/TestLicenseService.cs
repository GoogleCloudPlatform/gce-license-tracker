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
using Google.Solutions.LicenseTracker.Services;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test.Services
{
    [TestFixture]
    public class TestLicenseService
    {
        private const string ByolLicense = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-byol";

        [Test]
        public async Task WhenGlobalImageFound_ThenLicenseInfoIsInferredFromLicenseString()
        {
            var imageLocator = new ImageLocator("project-1", "my-byol-image");
            var gceAdapter = new Mock<IComputeEngineAdapter>();
            gceAdapter.Setup(a => a.GetImageAsync(
                    It.IsAny<ImageLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Apis.Compute.v1.Data.Image()
                {
                    Licenses = new []
                    {
                        "/compute/projects/my-project/global/licenses/some-license",
                        ByolLicense
                    }
                });

            var service = new LicenseService(
                gceAdapter.Object,
                new NullLogger<LicenseService>());

            var licenses = await service.LookupLicenseInfoAsync(
                    new[] { imageLocator },
                    CancellationToken.None)
                .ConfigureAwait(false);

            var licenseInfo = licenses.TryGet(imageLocator);
            Assert.IsNotNull(licenseInfo);

            Assert.AreEqual(OperatingSystemTypes.Windows, licenseInfo?.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Byol, licenseInfo?.LicenseType);
        }

        [Test]
        public async Task WhenZonalImageFound_ThenLicenseInfoIsInferredFromLicenseString()
        {
            var imageLocator = new ImageFamilyViewLocator("project-1", "-", "my-byol-image");
            var gceAdapter = new Mock<IComputeEngineAdapter>();
            gceAdapter.Setup(a => a.GetImageAsync(
                    It.IsAny<ImageFamilyViewLocator>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Apis.Compute.v1.Data.Image()
                {
                    Licenses = new[]
                    {
                        "/compute/projects/my-project/global/licenses/some-license",
                        ByolLicense
                    }
                });

            var service = new LicenseService(
                gceAdapter.Object,
                new NullLogger<LicenseService>());

            var licenses = await service.LookupLicenseInfoAsync(
                    new[] { imageLocator },
                    CancellationToken.None)
                .ConfigureAwait(false);

            var licenseInfo = licenses.TryGet(imageLocator);
            Assert.IsNotNull(licenseInfo);

            Assert.AreEqual(OperatingSystemTypes.Windows, licenseInfo?.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Byol, licenseInfo?.LicenseType);
        }

        [Test]
        public async Task WhenImageNotFound_ThenImageIsIgnored()
        {
            var imageLocator = new ImageLocator("project-1", "my-byol-image");
            var gceAdapter = new Mock<IComputeEngineAdapter>();
            gceAdapter.Setup(a => a.GetImageAsync(
                    It.IsAny<ImageLocator>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceNotFoundException("test", new Exception()));

            var service = new LicenseService(
                gceAdapter.Object,
                new NullLogger<LicenseService>());

            var licenses = await service.LookupLicenseInfoAsync(
                    new[] { imageLocator },
                    CancellationToken.None)
                .ConfigureAwait(false);

            var licenseInfo = licenses.TryGet(imageLocator);
            Assert.IsNull(licenseInfo);
        }

        [Test]
        public async Task WhenGlobalImageNotFoundButFromKnownProject_ThenLicenseInfoIsInferred()
        {
            var imageLocator = new ImageLocator("windows-cloud", "windows-2000");
            var gceAdapter = new Mock<IComputeEngineAdapter>();
            gceAdapter.Setup(a => a.GetImageAsync(
                    It.IsAny<ImageLocator>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceNotFoundException("test", new Exception()));

            var service = new LicenseService(
                gceAdapter.Object,
                new NullLogger<LicenseService>());
            
            var licenses = await service.LookupLicenseInfoAsync(
                    new[] { imageLocator },
                    CancellationToken.None)
                .ConfigureAwait(false);

            var licenseInfo = licenses.TryGet(imageLocator);
            Assert.IsNotNull(licenseInfo);

            Assert.AreEqual(OperatingSystemTypes.Windows, licenseInfo?.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Spla, licenseInfo?.LicenseType);
        }

        [Test]
        public async Task WhenZobalImageNotFoundButFromKnownProject_ThenLicenseInfoIsInferred()
        {
            var imageLocator = new ImageFamilyViewLocator("windows-cloud", "-", "windows-2000");
            var gceAdapter = new Mock<IComputeEngineAdapter>();
            gceAdapter.Setup(a => a.GetImageAsync(
                    It.IsAny<ImageFamilyViewLocator>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ResourceNotFoundException("test", new Exception()));

            var service = new LicenseService(
                gceAdapter.Object,
                new NullLogger<LicenseService>());

            var licenses = await service.LookupLicenseInfoAsync(
                    new[] { imageLocator },
                    CancellationToken.None)
                .ConfigureAwait(false);

            var licenseInfo = licenses.TryGet(imageLocator);
            Assert.IsNotNull(licenseInfo);

            Assert.AreEqual(OperatingSystemTypes.Windows, licenseInfo?.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Spla, licenseInfo?.LicenseType);
        }
    }
}
