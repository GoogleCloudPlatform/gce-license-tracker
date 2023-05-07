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
using NUnit.Framework;
using System;

namespace Google.Solutions.LicenseTracker.Test.Data.Locator
{
    [TestFixture]
    public class TestImageFamilyViewLocator 
    {

        [Test]
        public void WhenPathIsValid_FromStringReturnsObject()
        {
            var ref1 = ImageFamilyViewLocator.FromString(
                "projects/windows-cloud/zones/-/imageFamilyViews/windows-2022");

            Assert.AreEqual("imageFamilyViews", ref1.ResourceType);
            Assert.AreEqual("windows-2022", ref1.Name);
            Assert.AreEqual("-", ref1.Zone);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByComputeGoogleapisHost_FromStringReturnsObject()
        {
            var ref1 = ImageFamilyViewLocator.FromString(
                "https://compute.googleapis.com/compute/v1/projects/windows-cloud/zones/-/imageFamilyViews/windows-2022");

            Assert.AreEqual("imageFamilyViews", ref1.ResourceType);
            Assert.AreEqual("windows-2022", ref1.Name);
            Assert.AreEqual("-", ref1.Zone);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void WhenQualifiedByGoogleapisHost_FromStringReturnsObject()
        {
            var ref1 = ImageFamilyViewLocator.FromString(
                "https://www.googleapis.com/compute/v1/projects/windows-cloud/zones/-/imageFamilyViews/windows-2022");

            Assert.AreEqual("imageFamilyViews", ref1.ResourceType);
            Assert.AreEqual("windows-2022", ref1.Name);
            Assert.AreEqual("-", ref1.Zone);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void WhenUsingBetaApi_FromStringReturnsObject()
        {
            var ref1 = ImageFamilyViewLocator.FromString(
                 "https://compute.googleapis.com/compute/beta/projects/windows-cloud/zones/-/imageFamilyViews/windows-2022");

            Assert.AreEqual("imageFamilyViews", ref1.ResourceType);
            Assert.AreEqual("windows-2022", ref1.Name);
            Assert.AreEqual("-", ref1.Zone);
            Assert.AreEqual("windows-cloud", ref1.ProjectId);
        }

        [Test]
        public void WhenPathLacksProject_FromStringThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ImageFamilyViewLocator.FromString(
                "windows-cloud/zones/-/imageFamilyViews/windows-2022"));
        }

        [Test]
        public void WhenPathInvalid_FromStringThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => ImageFamilyViewLocator.FromString(
                "projects/windows-cloud/zones/-/imageFamilyViews"));
            Assert.Throws<ArgumentException>(() => ImageFamilyViewLocator.FromString(
                "/"));
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenEqualsReturnsTrue()
        {
            var ref1 = new ImageFamilyViewLocator("proj", "zone", "windows-2022");
            var ref2 = new ImageFamilyViewLocator("proj", "zone", "windows-2022");

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreEquivalent_ThenGetHasCodeIsSame()
        {
            var ref1 = new ImageFamilyViewLocator("proj", "zone", "windows-2022");
            var ref2 = new ImageFamilyViewLocator("proj", "zone", "windows-2022");

            Assert.AreEqual(ref1.GetHashCode(), ref2.GetHashCode());
        }

        [Test]
        public void WhenReferencesAreSame_ThenEqualsReturnsTrue()
        {
            var ref1 = new ImageFamilyViewLocator("proj", "zone", "windows-2022");
            var ref2 = ref1;

            Assert.IsTrue(ref1.Equals(ref2));
            Assert.IsTrue(ref1.Equals((object)ref2));
            Assert.IsTrue(ref1 == ref2);
            Assert.IsFalse(ref1 != ref2);
        }

        [Test]
        public void WhenReferencesAreNotEquivalent_ThenEqualsReturnsFalse()
        {
            var ref1 = new ImageFamilyViewLocator("proj", "zone1", "windows-2022");
            var ref2 = new ImageFamilyViewLocator("proj", "zone2", "windows-2022");

            Assert.IsFalse(ref1.Equals(ref2));
            Assert.IsFalse(ref1.Equals((object)ref2));
            Assert.IsFalse(ref1 == ref2);
            Assert.IsTrue(ref1 != ref2);
        }

        [Test]
        public void TestEqualsNull()
        {
            var ref1 = new ImageFamilyViewLocator("proj", "zone", "windows-2022");

            Assert.IsFalse(ref1.Equals(null));
            Assert.IsFalse(ref1!.Equals((object?)null));
            Assert.IsFalse(ref1 == null);
            Assert.IsFalse(null == ref1);
            Assert.IsTrue(ref1 != null);
            Assert.IsTrue(null != ref1);
        }

        [Test]
        public void WhenCreatedFromPath_ThenToStringReturnsPath()
        {
            var path = "projects/windows-cloud/zones/-/imageFamilyViews/windows-2022";

            Assert.AreEqual(
                path,
                ImageFamilyViewLocator.FromString(path).ToString());
        }

        [Test]
        public void WhenCreatedFromUrl_ThenToStringReturnsPath()
        {
            var path = "projects/windows-cloud/zones/-/imageFamilyViews/windows-2022";

            Assert.AreEqual(
                path,
                ImageFamilyViewLocator.FromString(
                    "https://www.googleapis.com/compute/v1/" + path).ToString());
        }
    }
}
