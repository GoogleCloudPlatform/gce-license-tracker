namespace Google.Solutions.LicenseTracker.Data.Locator
{
    public class TableLocator : ResourceLocator
    {
        public override string ResourceType => "datasets";

        private readonly string dataset;

        public DatasetLocator Dataset => new DatasetLocator(this.ProjectId, this.dataset);

        public TableLocator(string projectId, string dataset, string name)
            : base(projectId, name)
        {
            this.dataset = dataset;
        }

        public TableLocator(DatasetLocator dataset, string name)
            : this(dataset.ProjectId, dataset.Name, name)
        {
        }

        public override int GetHashCode()
        {
            return
                this.ProjectId.GetHashCode() ^
                this.dataset.GetHashCode() ^
                this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return $"projects/{this.ProjectId}/{this.ResourceType}/datasets/{this.dataset}/tables/{this.Name}";
        }

        public bool Equals(TableLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.dataset == other.dataset &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(object? obj)
        {
            return obj is TableLocator locator && Equals(locator);
        }

        public static bool operator ==(TableLocator? obj1, TableLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(TableLocator? obj1, TableLocator? obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
