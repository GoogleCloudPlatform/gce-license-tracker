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

using System.Reflection;
using System.Text;

namespace Google.Solutions.LicenseTracker.Util
{
    public static class ExceptionExtensions
    {
        public static Exception Unwrap(this Exception e)
        {
            if (e is AggregateException aggregate && aggregate.InnerException != null)
            {
                e = aggregate.InnerException;
            }

            if (e is TargetInvocationException target && target.InnerException != null)
            {
                e = target.InnerException;
            }

            return e;
        }

        public static bool Is<T>(this Exception e) where T : Exception
        {
            return e.Unwrap() is T;
        }

        public static bool IsAccessDeniedError(this Exception e)
        {
            return e.Unwrap() is GoogleApiException apiEx && apiEx.Error?.Code == 403;
        }

        public static bool IsNotFoundError(this Exception e)
        {
            return e.Unwrap() is GoogleApiException apiEx && apiEx.Error?.Code == 404;
        }

        public static string FullMessage(this Exception exception)
        {
            var fullMessage = new StringBuilder();

            for (var ex = exception; ex != null; ex = ex.InnerException)
            {
                if (fullMessage.Length > 0)
                {
                    fullMessage.Append(": ");
                }

                fullMessage.Append(ex.Message);
            }

            return fullMessage.ToString();
        }
    }
}
