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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Google.Solutions.LicenseTracker.Util
{
    public class JsonConsoleFormatter : ConsoleFormatter
    {
        public static readonly string FormatterName = typeof(JsonConsoleFormatter).Name;

        private static readonly IDictionary<LogLevel, string> Severities = new Dictionary<LogLevel, string>
        {
            { LogLevel.Trace, "DEBUG" },
            { LogLevel.Debug, "DEBUG" },
            { LogLevel.Information, "INFO" },
            { LogLevel.Warning, "warning" },
            { LogLevel.Error, "ERROR" },
            { LogLevel.Critical, "CRITICAL" },
        };

        private readonly Options options;

        public JsonConsoleFormatter(IOptionsMonitor<Options> options)
            : base(FormatterName)
        {
            this.options = options.CurrentValue;
        }

        public override void Write<TState>(
            in LogEntry<TState> logEntry,
            IExternalScopeProvider? scopeProvider,
            TextWriter textWriter)
        {
            var entry = new StructuredLogEntry()
            {
                Severity = Severities.TryGet(logEntry.LogLevel) ?? "DEFAULT",
                Component = logEntry.Category,
                Message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty
            };

            if (logEntry.Exception != null)
            {
                if (this.options.IncludeStackTraces)
                {
                    entry.Message += $": {logEntry.Exception.FullMessage()}: {logEntry.Exception.Unwrap().StackTrace}";
                }
                else
                {
                    entry.Message += $": {logEntry.Exception.FullMessage()}";
                }
            }

            textWriter.WriteLine(entry.ToString());
        }


        /// <summary>
        /// Structured log entry that complies with the Cloud Run conventions, see
        /// https://cloud.google.com/run/docs/logging#writing_structured_logs.
        /// </summary>
        public class StructuredLogEntry
        {
            [JsonProperty("message")]
            public string? Message { get; set; }

            [JsonProperty("severity")]
            public string? Severity { get; set; }

            [JsonProperty("component")]
            public string? Component { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public class Options : ConsoleFormatterOptions
        {
            public bool IncludeStackTraces { get; set; }
        }
    }
}
