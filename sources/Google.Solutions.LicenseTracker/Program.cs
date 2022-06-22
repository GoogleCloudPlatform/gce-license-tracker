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
using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Data.Services.Adapters;
using Google.Solutions.LicenseTracker.Services;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Google.Solutions.LicenseTracker.Test")]

namespace Google.Solutions.LicenseTracker
{
    internal class Program : IConsoleHandler
    {
        /// <summary>
        /// Name of BigQuery dataset. Making the dataset name
        /// customizable makes it more difficult to maintain 
        /// the queries and views used by the dashboard, so we
        /// don't allow that.
        /// </summary>
        private const string DatasetName = "license_usage";

        private const string RequiredApiForAnalysis = "compute.googleapis.com";
        private static readonly string[] RequiredPermissionsForAnalysis = Enumerable.Empty<string>()
            .Concat(ComputeEngineAdapter.AllRequiredPermissions)
            .Concat(AuditLogAdapter.AllRequiredPermissions)
            .ToArray();

        private readonly CommandLineOptions commandLineOptions;
        private readonly ILogger logger;
        private readonly IReportDatasetService reportDatasetService;
        private readonly IPlacementReportService placementReportService;
        private readonly IProjectAutodiscoveryService projectAutodiscoveryService;

        public Program(
            CommandLineOptions commandLineOptions,
            IReportDatasetService reportDatasetService,
            IPlacementReportService placementReportService,
            IProjectAutodiscoveryService projectAutodiscoveryService,
            ILogger<Program> logger)
        {
            this.commandLineOptions = commandLineOptions;
            this.reportDatasetService = reportDatasetService;
            this.placementReportService = placementReportService;
            this.projectAutodiscoveryService = projectAutodiscoveryService;
            this.logger = logger;
        }

        public async Task RunAsync()
        {
            //
            // Determine projects to analyze.
            //
            var projectsToAnalyze = this.commandLineOptions.AutodiscoverProjects
                ? await this.projectAutodiscoveryService.DiscoverAccessibleProjects(
                        RequiredPermissionsForAnalysis,
                        RequiredApiForAnalysis,
                        CancellationToken.None)
                    .ConfigureAwait(false)
                : this.commandLineOptions
                    .Projects
                    .Select(p => new ProjectLocator(p));

            this.logger.LogInformation(
                "Projects to analyze: {projects}",
                projectsToAnalyze.Any()
                    ? string.Join(", ", projectsToAnalyze)
                    : "none");

            //
            // Determine dataset to write data to. In dry-run mode,
            // we might not have a dataset.
            //
            var dataset = string.IsNullOrEmpty(this.commandLineOptions.DataSetProject)
                ? null
                : new DatasetLocator(this.commandLineOptions.DataSetProject!, DatasetName);

            DateTime reportEndDate = this.commandLineOptions.EndDate;
            DateTime? reportStartDateInclusive = null;
            if (dataset != null)
            {
                await this.reportDatasetService.PrepareDatasetAsync(
                        dataset,
                        CancellationToken.None)
                    .ConfigureAwait(false);

                //
                // Start report where we last left off.
                //
                reportStartDateInclusive = await this.reportDatasetService.TryGetLastRunDateAsync(
                        dataset,
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                this.logger.LogWarning("No dataset specified");
            }

            if (reportStartDateInclusive == null)
            {
                //
                // No previous report - start the report as far back as the
                // analysis window allows.
                //
                reportStartDateInclusive = reportEndDate
                    .AddDays(-((int)this.commandLineOptions.AnalysisWindowSizeInDays) + 1)
                    .Date;
            }
            else if (reportStartDateInclusive >= reportEndDate)
            {
                //
                // Nothing to do.
                //
                this.logger.LogInformation("Dataset is up to date");
                return;
            }

            Debug.Assert(reportStartDateInclusive != null);

            this.logger.LogInformation(
                "Reporting window: {start} - {end}",
                reportStartDateInclusive,
                reportEndDate);
            this.logger.LogInformation(
                "Analysis window size: {days} days",
                this.commandLineOptions.AnalysisWindowSizeInDays);
            if (this.commandLineOptions.DryRun)
            {
                this.logger.LogInformation("Dry run, not saving any report data.");
            }
            else
            {
                this.logger.LogInformation("Writing report data to {dataset}", dataset);
            }

            //
            // Analyze logs and create report.
            //
            var report = await this.placementReportService.ListPlacementEvents(
                    projectsToAnalyze,
                    this.commandLineOptions.AnalysisWindowSizeInDays,
                    reportStartDateInclusive.Value,
                    this.commandLineOptions.EndDate,
                    CancellationToken.None)
                .ConfigureAwait(false);

            //
            // Show or save report.
            //
            if (!this.commandLineOptions.DryRun && dataset != null)
            {
                await this.reportDatasetService.SubmitPlacementReportAsync(
                        dataset,
                        reportEndDate,
                        report,
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                Console.WriteLine(new String('-', 80));
                Console.WriteLine("Placement started events");
                Console.WriteLine(new String('-', 80));

                foreach (var placement in report.StartedPlacements)
                {
                    Console.WriteLine(
                        "{0} ({1}) on {2} ({3}) @ {4:u}: {5}/{6}",
                        placement.InstanceId,
                        placement.Instance?.Name,
                        placement.Placement.ServerId,
                        placement.Placement.NodeType?.Name ?? "-",
                        placement.Placement.From,
                        placement.License?.OperatingSystem,
                        placement.License?.LicenseType);
                }


                Console.WriteLine(new String('-', 80));
                Console.WriteLine("Placement ended events");
                Console.WriteLine(new String('-', 80));

                foreach (var placement in report.EndedPlacements)
                {
                    Console.WriteLine(
                        "{0} ({1}) on {2} ({3}) @ {4:u}: {5}/{6}",
                        placement.InstanceId,
                        placement.Instance?.Name,
                        placement.Placement.ServerId,
                        placement.Placement.NodeType?.Name ?? "-",
                        placement.Placement.To,
                        placement.License?.OperatingSystem,
                        placement.License?.LicenseType);
                }
            }
        }

        /// <summary>
        /// Main entry point.
        /// </summary>
        private static async Task Main(string[] args)
        {
            try
            {
                //
                // Use a modern TLS version.
                //
                System.Net.ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 |
                    SecurityProtocolType.Tls11;

                //
                // Parse command line and infer missing parameters
                // from environment.
                //
                var cmdLine = CommandLineOptions.FromCommandLine(args);
                await cmdLine
                    .InferMissingArgumentsFromEnvironmentAsync()
                    .ConfigureAwait(false);
                cmdLine.Validate();

                //
                // Use application default credentials.
                //
                var credential = await GoogleCredential
                    .GetApplicationDefaultAsync()
                    .ConfigureAwait(false);
                if (cmdLine.ImpersonateServiceAccount != null)
                {
                    credential = credential.Impersonate(
                        new ImpersonatedCredential.Initializer(cmdLine.ImpersonateServiceAccount)
                        {
                            Scopes = new[] { "https://www.googleapis.com/auth/cloud-platform" }
                        });
                }

                //
                // Set up a host for dependency injection and run
                // the actual "main" class.
                //
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddSingleton(cmdLine);
                        services.AddSingleton(credential.UnderlyingCredential);

                        services.AddTransient<IComputeEngineAdapter, ComputeEngineAdapter>();
                        services.AddTransient<IResourceManagerAdapter, ResourceManagerAdapter>();
                        services.AddTransient<IAuditLogAdapter, AuditLogAdapter>();
                        services.AddTransient<IBigQueryAdapter, BigQueryAdapter>();
                        services.AddTransient<IServiceUsageAdapter, ServiceUsageAdapter>();

                        services.AddTransient<IInstanceHistoryService, InstanceHistoryService>();
                        services.AddTransient<IPlacementReportService, PlacementReportService>();
                        services.AddTransient<ILicenseService, LicenseService>();
                        services.AddTransient<IReportDatasetService, ReportDatasetService>();
                        services.AddTransient<IProjectAutodiscoveryService, ProjectAutodiscoveryService>();
                    })
                    .ConfigureLogging(logBuilder =>
                    {
                        //
                        // Don't show framework logs unless it's a warning.
                        //
                        logBuilder.AddFilter("Microsoft", LogLevel.Warning);
                        logBuilder.SetMinimumLevel(cmdLine.LogLevel);

                        if (cmdLine.LogAsJson)
                        {
                            logBuilder.AddConsole(options =>
                            {
                                options.FormatterName = JsonConsoleFormatter.FormatterName;
                            });
                            logBuilder.AddConsoleFormatter<JsonConsoleFormatter, JsonConsoleFormatter.Options>(options =>
                            {
                                options.IncludeStackTraces = cmdLine.LogLevel == LogLevel.Debug;
                            });
                        }
                        else
                        {
                            logBuilder.AddSimpleConsole(options =>
                            {
                                options.SingleLine = true;
                            });
                        }
                    });
                try
                {
                    await host
                        .RunConsoleAsync<Program>()
                        .ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unhandled exception during analysis: {0}", e.FullMessage());
                    Environment.ExitCode = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Initializing application failed: {0}", ex.FullMessage());
                Environment.ExitCode = 1;
            }
        }
    }
}
