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
    public class TestLinqExtensions
    {
        //---------------------------------------------------------------------
        // EnsureNotNull.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEnumIsNull_EnsureNotNullReturnsEmpty()
        {
            IEnumerable<string>? e = null;
            Assert.IsNotNull(e.EnsureNotNull());
            Assert.AreEqual(0, e.EnsureNotNull().Count());
        }

        //---------------------------------------------------------------------
        // TryGet.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKeyFound_ThenTryGetReturnsValue()
        {
            var dict = new Dictionary<string, string>()
            {
                { "key", "value" },
            };
            Assert.AreEqual("value", dict.TryGet("key"));
        }

        [Test]
        public void WhenKeyNotFound_ThenTryGetReturnsNull()
        {
            var dict = new Dictionary<string, string>();
            Assert.IsNull(dict.TryGet("key"));
        }

        //---------------------------------------------------------------------
        // Chunk.
        //---------------------------------------------------------------------

        [Test]
        public void WhenListSmallerThanChunk_ThenChunkReturnsSingleList()
        {
            var list = new[] { "a", "b", "c" };
            var chunks = list.Chunk(4);

            Assert.AreEqual(1, chunks.Count());
            Assert.AreEqual(3, chunks.First().Count());
        }

        [Test]
        public void WhenListFillsTwoChunks_ThenChunkReturnsTwoLists()
        {
            var list = new[] { "a", "b", "c", "d" };
            var chunks = list.Chunk(2);

            Assert.AreEqual(2, chunks.Count());
            CollectionAssert.AreEqual(new[] { "a", "b" }, chunks.First());
            CollectionAssert.AreEqual(new[] { "c", "d" }, chunks.Skip(1).First());
        }

        [Test]
        public void WhenListLargerThanSingleChunk_ThenChunkReturnsTwoLists()
        {
            var list = new[] { "a", "b", "c" };
            var chunks = list.Chunk(2);

            Assert.AreEqual(2, chunks.Count());
            CollectionAssert.AreEqual(new[] { "a", "b" }, chunks.First());
            CollectionAssert.AreEqual(new[] { "c" }, chunks.Skip(1).First());
        }
    }
}