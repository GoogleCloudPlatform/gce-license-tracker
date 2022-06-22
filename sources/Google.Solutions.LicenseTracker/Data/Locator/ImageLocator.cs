using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LicenseTracker.Data.Locator
{
    /// <summary>
    /// Common base interface for global and zonal image locators.
    /// </summary>
    public interface IImageLocator
    {
        string ProjectId { get; }
        string Name { get; }
        string ToString();
    }

    public partial class ImageLocator : IImageLocator
    {
    }

    public partial class ImageFamilyViewLocator : IImageLocator
    {
        public static ImageFamilyViewLocator FromString(
            string resourceReference,
            string? defaultZone)
        {
            var locator = FromString(resourceReference);
            if (locator.Zone == "-")
            {
                //
                // The zone is often not set and must be
                // derived from the context.
                //
                locator.Zone = defaultZone ?? throw new ArgumentException(
                    $"Locator {resourceReference} does not contain a zone, and " +
                    $"no contextual zone was provided");
            }

            return locator;
        }
    }
}
