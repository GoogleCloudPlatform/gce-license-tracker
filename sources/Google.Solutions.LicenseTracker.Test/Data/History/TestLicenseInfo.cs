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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Test.Data.History
{

    [TestFixture]
    public class TestLicenseInfo
    {
        [Test]
        public void WhenLocatorIsNull_ThenFromLicenseReturnsUnknown()
        {
            var annotation = LicenseInfo.FromLicense(null);
            Assert.AreEqual(LicenseTypes.Unknown, annotation.LicenseType);
            Assert.AreEqual(OperatingSystemTypes.Unknown, annotation.OperatingSystem);
        }

        [Test]
        public void WhenLocatorIsWindowsByol_ThenFromLicenseReturnsWindowsByol()
        {
            var annotation = LicenseInfo.FromLicense(
                new LicenseLocator("windows-cloud", "windows-10-enterprise-byol"));
            Assert.AreEqual(LicenseTypes.Byol, annotation.LicenseType);
            Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
        }

        [Test]
        public void WhenLocatorIsWindowsSpla_ThenFromLicenseReturnsWindowsSpla()
        {
            var annotation = LicenseInfo.FromLicense(
                new LicenseLocator("windows-cloud", "windows-2016-dc"));
            Assert.AreEqual(LicenseTypes.Spla, annotation.LicenseType);
            Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
        }

        [Test]
        public void WhenLocatorIsNotWindwosAndNotNull_ThenFromLicenseReturnsLinux()
        {

            var annotation = LicenseInfo.FromLicense(
                new LicenseLocator("my-project", "my-distro"));
            Assert.AreEqual(LicenseTypes.Unknown, annotation.LicenseType);
            Assert.AreEqual(OperatingSystemTypes.Linux, annotation.OperatingSystem);
        }
    }
}
