namespace Google.Solutions.LicenseTracker.Data.Locator
{
    public class DatasetLocator : ResourceLocator
    {
        public override string ResourceType => "datasets";

        public DatasetLocator(string projectId, string name)
            : base(projectId, name)
        {
        }

        public override int GetHashCode()
        {
            return
                this.ProjectId.GetHashCode() ^
                this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return $"projects/{this.ProjectId}/{this.ResourceType}/{this.Name}";
        }

        public bool Equals(DatasetLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(object? obj)
        {
            return obj is DatasetLocator locator && Equals(locator);
        }

        public static bool operator ==(DatasetLocator? obj1, DatasetLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(DatasetLocator? obj1, DatasetLocator? obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
