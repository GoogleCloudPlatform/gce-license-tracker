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

using Google.Apis.Requests;
using Google.Apis.Util;
using Microsoft.Extensions.Logging;

namespace Google.Solutions.LicenseTracker.Util
{
    public static class ExecuteAsStreamExtensions
    {
        /// <summary>
        /// Like ExecuteAsStream, but catch non-success HTTP codes and convert them
        /// into an exception.
        /// </summary>
        public async static Task<Stream> ExecuteAsStreamOrThrowAsync<TResponse>(
            this IClientServiceRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            using (var httpRequest = request.CreateRequest())
            {
                var httpResponse = await request.Service.HttpClient.SendAsync(
                    httpRequest,
                    cancellationToken).ConfigureAwait(false);

                // NB. ExecuteAsStream does not do this check.
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var error = await request.Service.DeserializeError(httpResponse).ConfigureAwait(false);
                    throw new GoogleApiException(request.Service.Name, error.ToString())
                    {
                        Error = error,
                        HttpStatusCode = httpResponse.StatusCode
                    };
                }

                cancellationToken.ThrowIfCancellationRequested();
                return await httpResponse.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async static Task<Stream> ExecuteAsStreamWithRetryAsync<TResponse>(
            this IClientServiceRequest<TResponse> request,
            ExponentialBackOff backOff,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            int retries = 0;
            while (true)
            {
                try
                {
                    return await request
                        .ExecuteAsStreamOrThrowAsync(cancellationToken)
                        .ConfigureAwait(false); ;
                }
                catch (GoogleApiException e) when (e.Error != null && (e.Error.Code == 429 || e.Error.Code == 500))
                {
                    // Too many requests.
                    if (retries < backOff.MaxNumOfRetries)
                    {
                        logger.LogWarning(
                            "Too many requests - backing of and retrying...", retries);

                        retries++;
                        await Task
                            .Delay(backOff.GetNextBackOff(retries), cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        // Retried too often already.
                        logger.LogWarning("Giving up after {retries} retries", retries);
                        throw;
                    }
                }
            }
        }
    }
}
