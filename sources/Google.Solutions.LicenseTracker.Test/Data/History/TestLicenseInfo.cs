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
