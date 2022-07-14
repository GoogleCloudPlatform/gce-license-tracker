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
