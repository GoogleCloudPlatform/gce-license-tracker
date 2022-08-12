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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Logging.v2;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Util;
using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.Events;
using Google.Solutions.LicenseTracker.Data.History;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Logs;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Data.Services.Adapters
{
    public interface IAuditLogAdapter
    {
        Task ProcessInstanceEventsAsync(
            IEnumerable<ProjectLocator> projectIds,
            DateTime startTime,
            IEventProcessor processor,
            CancellationToken cancellationToken);
    }

    public class AuditLogAdapter : IAuditLogAdapter
    {
        internal static readonly string[] AllRequiredPermissions = new[] {
            "logging.logEntries.list"
        };

        private const int MaxPageSize = 1000;
        private const int MaxRetries = 10;
        private static readonly TimeSpan initialBackOff = TimeSpan.FromMilliseconds(100);

        private readonly ILogger logger;
        private readonly LoggingService service;

        public AuditLogAdapter(
            ICredential credential,
            ILogger<AuditLogAdapter> logger)
        {
            this.logger = logger;
            this.service = new LoggingService(new Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = UserAgent.Default.ToString()
            });
        }

        internal async Task ListEventsAsync(
            ListLogEntriesRequest request,
            Action<EventBase> callback,
            ExponentialBackOff backOff,
            CancellationToken cancellationToken)
        {
            try
            {
                string? nextPageToken = null;
                do
                {
                    request.PageToken = nextPageToken;

                    using (var stream = await this.service.Entries
                        .List(request)
                        .ExecuteAsStreamWithRetryAsync(backOff, this.logger, cancellationToken)
                        .ConfigureAwait(false))
                    using (var reader = new JsonTextReader(new StreamReader(stream)))
                    {
                        nextPageToken = ListLogEntriesParser.Read(reader, callback);
                    }
                }
                while (nextPageToken != null);
            }
            catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 403)
            {
                throw new ResourceAccessDeniedException(
                    "You do not have sufficient permissions to view logs. " +
                    "You need the 'Logs Viewer' role (or an equivalent custom role) " +
                    "to perform this action.",
                    e);
            }
        }

        internal static string CreateFilterString(
            IEnumerable<string> methods,
            IEnumerable<string> severities,
            DateTime startTime)
        {
            Debug.Assert(startTime.Kind == DateTimeKind.Utc);

            var criteria = new LinkedList<string>();

            if (methods != null && methods.Any())
            {
                criteria.AddLast($"protoPayload.methodName=(\"{string.Join("\" OR \"", methods)}\")");
            }

            if (severities != null && severities.Any())
            {
                criteria.AddLast($"severity=(\"{string.Join("\" OR \"", severities)}\")");
            }

            // NB. Some instance-related events use project scope, for example
            // setCommonInstanceMetadata events.
            criteria.AddLast($"resource.type=(\"gce_instance\" OR \"gce_project\" OR \"audited_resource\")");
            criteria.AddLast($"timestamp > \"{startTime:o}\"");

            return string.Join(" AND ", criteria);
        }

        //---------------------------------------------------------------------
        // IAuditLogAdapter
        //---------------------------------------------------------------------


        public async Task ProcessInstanceEventsAsync(
            IEnumerable<ProjectLocator> projectIds,
            DateTime startTime,
            IEventProcessor processor,
            CancellationToken cancellationToken)
        {
            Utilities.ThrowIfNull(projectIds, nameof(projectIds));

            var request = new ListLogEntriesRequest()
            {
                ResourceNames = projectIds.Select(p => "projects/" + p.Name).ToList(),
                Filter = CreateFilterString(
                    processor.SupportedMethods,
                    processor.SupportedSeverities,
                    startTime),
                PageSize = MaxPageSize,
                OrderBy = "timestamp desc"
            };

            await ListEventsAsync(
                request,
                processor.Process,
                new ExponentialBackOff(initialBackOff, MaxRetries),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public static class LogSinkExtensions
    {
        private const string CloudStorageDestinationPrefix = "storage.googleapis.com/";

        public static bool IsCloudStorageSink(this LogSink sink)
        {
            return sink.Destination.StartsWith(CloudStorageDestinationPrefix);
        }

        public static string GetDestinationBucket(this LogSink sink)
        {
            if (sink.Destination.StartsWith(CloudStorageDestinationPrefix))
            {
                return sink.Destination.Substring(CloudStorageDestinationPrefix.Length);
            }
            else
            {
                throw new ArgumentException("Not a Cloud Storage sink");
            }
        }
    }
}
