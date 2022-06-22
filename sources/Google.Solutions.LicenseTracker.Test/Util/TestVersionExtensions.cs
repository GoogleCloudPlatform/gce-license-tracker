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

using Google.Solutions.LicenseTracker.Util;
using NUnit.Framework;

namespace Google.Solutions.LicenseTracker.Test
{
    [TestFixture]
    public class TestVersionExtensions
    {
        [Test]
        public void ToInt64()
        {
            var version = new Version(0x1234, 0x5678, 0x9ABC, 0xDEF0);
            Assert.AreEqual(
                0x123456789ABCDEF0,
                version.ToInt64());
        }
    }
}
