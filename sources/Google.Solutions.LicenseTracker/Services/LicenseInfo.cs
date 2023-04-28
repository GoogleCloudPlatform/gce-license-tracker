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

using Google.Solutions.LicenseTracker.Data.Locator;
using System.ComponentModel;

namespace Google.Solutions.LicenseTracker.Services
{
    public class LicenseInfo
    {
        public static readonly LicenseInfo Default =
            new LicenseInfo(null, OperatingSystemTypes.Unknown, LicenseTypes.Unknown);

        public LicenseLocator? License { get; }

        public LicenseTypes LicenseType { get; }

        public OperatingSystemTypes OperatingSystem { get; }

        internal LicenseInfo(
            LicenseLocator? license,
            OperatingSystemTypes osType,
            LicenseTypes licenseType)
        {
            this.License = license;
            this.LicenseType = licenseType;
            this.OperatingSystem = osType;
        }

        internal static LicenseInfo FromLicense(LicenseLocator? license)
        {
            if (license != null && license.IsWindowsByolLicense())
            {
                return new LicenseInfo(
                    license,
                    OperatingSystemTypes.Windows,
                    LicenseTypes.Byol);
            }
            else if (license != null && license.IsWindowsLicense())
            {
                return new LicenseInfo(
                    license,
                    OperatingSystemTypes.Windows,
                    LicenseTypes.Spla);
            }
            else if (license != null)
            {
                return new LicenseInfo(
                    license,
                    OperatingSystemTypes.Linux,
                    LicenseTypes.Unknown);
            }
            else
            {
                return new LicenseInfo(
                    null,
                    OperatingSystemTypes.Unknown,
                    LicenseTypes.Unknown);
            }
        }
    }

    [Flags]
    public enum LicenseTypes
    {
        Unknown = 1,

        [Description("BYOL")]
        Byol = 2,

        [Description("SPLA")]
        Spla = 4
    }

    [Flags]
    public enum OperatingSystemTypes
    {
        Unknown = 1,
        Windows = 2,
        Linux = 4
    }
}
