using Google.Apis.Util;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Data.Locator
{
    public abstract class ResourceLocator
    {
        private const string ComputeGoogleapisPrefix = "https://compute.googleapis.com/compute/v1/";
        private const string GoogleapisUrlPrefix = "https://www.googleapis.com/compute/v1/";

        public string ProjectId { get; }
        public abstract string ResourceType { get; }
        public string Name { get; }

        protected static string StripUrlPrefix(string resourceReference)
        {
            if (resourceReference.StartsWith(ComputeGoogleapisPrefix))
            {
                return resourceReference.Substring(ComputeGoogleapisPrefix.Length);
            }
            else if (resourceReference.StartsWith(GoogleapisUrlPrefix))
            {
                return resourceReference.Substring(GoogleapisUrlPrefix.Length);
            }
            else
            {
                return resourceReference;
            }
        }

        protected ResourceLocator(
            string projectId,
            string resourceName)
        {
            Utilities.ThrowIfNull(projectId, nameof(projectId));
            Utilities.ThrowIfNull(resourceName, nameof(resourceName));

            Debug.Assert(!long.TryParse(projectId, out long _));
            Debug.Assert(!long.TryParse(resourceName, out long _));
            Debug.Assert(!projectId.Contains("/"));

            this.ProjectId = projectId;
            this.Name = resourceName;
        }
    }
}
