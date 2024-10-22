﻿//
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

using System.Reflection;

namespace Google.Solutions.LicenseTracker
{
    /// <summary>
    /// User agent for HTTP requests.
    /// </summary>
    internal class UserAgent
    {
        public string Name { get; private set; }
        public string Version { get; private set; }

        public override string ToString()
            => $"{this.Name}/{this.Version}";

        public static UserAgent Default { get; }
            = new UserAgent(
                "LicenseTracker",
                Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0");

        private UserAgent(
            string name,
            string version)
        {
            this.Name = name;
            this.Version = version;
        }
    }
}
