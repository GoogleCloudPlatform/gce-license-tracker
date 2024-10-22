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

using Google.Apis.Util;

namespace Google.Solutions.LicenseTracker.Util
{
    internal static class LinqExtensions
    {
        public static IEnumerable<T> EnsureNotNull<T>(this IEnumerable<T>? e)
        {
            return e ?? Enumerable.Empty<T>();
        }

        public static V? TryGet<K, V>(
            this IDictionary<K, V> dict,
            K key)
            where V : class
        {
            if (dict.TryGetValue(key, out V? value))
            {
                return value;
            }
            else
            {
                return default(V);
            }
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, ushort chunkSize)
        {
            Utilities.ThrowIfNull(source, nameof(source));

            var chunk = new List<T>(chunkSize);
            foreach (var x in source)
            {
                chunk.Add(x);
                if (chunk.Count == chunkSize)
                {
                    yield return chunk;
                    chunk = new List<T>(chunkSize);
                }
            }

            if (chunk.Any())
            {
                yield return chunk;
            }
        }

        public static string ToShortString<K, V>(
            this IDictionary<K, V> dict)
        {
            var items = dict
                .EnsureNotNull()
                .Select(kvp => kvp.Key + "=" + kvp.Value);
            return "{" + string.Join(", ", items) + "}";
        }
    }
}
