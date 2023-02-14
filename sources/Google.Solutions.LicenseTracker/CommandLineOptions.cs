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
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace Google.Solutions.LicenseTracker
{
    internal class CommandLineOptions
    {
        private const int DefaultAnalysisWindowSizeInDays = 180;

        public string? DataSetProject { get; private set; }

        public uint AnalysisWindowSizeInDays { get; private set; } = DefaultAnalysisWindowSizeInDays;

        public DateTime EndDate { get; private set; } = DateTime.UtcNow.Date;

        public List<string> Projects { get; private set; } = new List<string>();

        public LogLevel LogLevel { get; private set; } = LogLevel.Warning;

        public bool DryRun { get; private set; } = false;

        public bool AutodiscoverProjects { get; private set; } = false;

        public bool LogAsJson { get; private set; } = false;

        public string? ImpersonateServiceAccount { get; private set; }

        public async Task InferMissingArgumentsFromEnvironmentAsync()
        {
            if (await ComputeCredential
                .IsRunningOnComputeEngine()
                .ConfigureAwait(false))
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Metadata-Flavor", "Google");

                    var projectId = await client
                        .GetStringAsync(
                            $"{ComputeCredential.MetadataServerUrl}/computeMetadata/v1/project/project-id",
                            CancellationToken.None)
                        .ConfigureAwait(false);

                    if (string.IsNullOrEmpty(this.DataSetProject))
                    {
                        this.DataSetProject = projectId;
                    }

                    if (!this.Projects.Any())
                    {
                        this.Projects.Add(projectId);
                    }
                }
            }
        }

        public void Validate()
        {
            if (!this.Projects.Any() && !this.AutodiscoverProjects)
            {
                throw new ArgumentException("Specify at least one project to analyze");
            }

            if (this.EndDate < DateTime.Now.AddDays(-7))
            {
                throw new ArgumentException("End date must be within last 7 days to get accurate results");
            }

            if (this.AnalysisWindowSizeInDays < 30)
            {
                throw new ArgumentException("Analysis window must be at least 30 days to get accurate results");
            }

            if (!this.DryRun && string.IsNullOrEmpty(this.DataSetProject))
            {
                throw new ArgumentException("Specify a project for the BigQuery dataset");
            }
        }

        public static CommandLineOptions FromCommandLine(string[] args)
        {
            var commandLine = new CommandLineOptions();
            bool showHelp = false;
            var options = new OptionSet() {
                {
                    "dataset-project=",
                    "Project to use for BigQuery dataset",
                    v => commandLine.DataSetProject = v
                },
                {
                    "end-date=",
                    "End date for analysis (Default: today, 00:00)",
                    v => commandLine.EndDate = DateOnly.Parse(v).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                },
                {
                    "analysis-window=",
                    $"Size of analysis window (in days, default: {DefaultAnalysisWindowSizeInDays})",
                    v => commandLine.AnalysisWindowSizeInDays = uint.Parse(v)
                },
                {
                    "?|help",
                    "Show usage information",
                    v => showHelp = v != null
                },
                {
                    "v|verbose",
                    "Show verbose output",
                    v => commandLine.LogLevel = LogLevel.Debug
                },
                {
                    "dryrun",
                    "Dry run",
                    v => commandLine.DryRun = true
                },
                {
                    "autodiscover",
                    "Auto-discover projects to analyze",
                    v => commandLine.AutodiscoverProjects = true
                },
                {
                    "impersonate-service-account=",
                    "Impersonate service account to access Google Cloud API",
                    v => commandLine.ImpersonateServiceAccount = v
                },
                {
                    "log-format=",
                    "Log format (json | text)",
                    v => commandLine.LogAsJson = v == "json"
                }
            };

            try
            {
                commandLine.Projects.AddRange(options.Parse(args));

                if (showHelp)
                {
                    ShowHelpAndExit(options);
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
                ShowHelpAndExit(options);
            }

            return commandLine;
        }

        private static void ShowHelpAndExit(OptionSet options)
        {
            Console.WriteLine("BYOL License Tracker");
            Console.WriteLine();
            Console.WriteLine("Usage: program [options] project-ids...");
            Console.WriteLine();
            Console.WriteLine("Options:");

            options.WriteOptionDescriptions(Console.Out);
            Environment.Exit(2);
        }
    }
}
