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
