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

using Google.Apis.Bigquery.v2.Data;
using Google.Solutions.LicenseTracker.Adapters;
using Google.Solutions.LicenseTracker.Data.History;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Google.Solutions.LicenseTracker.Services
{
    public interface IReportDatasetService
    {
        Task PrepareDatasetAsync(
           DatasetLocator dataset,
           CancellationToken cancellationToken);

        Task<DateTime?> TryGetLastRunDateAsync(
            DatasetLocator dataset,
            CancellationToken cancellationToken);

        Task SubmitPlacementReportAsync(
            DatasetLocator dataset,
            DateTime date,
            PlacementReport report,
            CancellationToken cancellationToken);
    }

    internal class ReportDatasetService : IReportDatasetService
    {
        private readonly IBigQueryAdapter bigQueryAdapter;
        private readonly ILogger logger;

#if DEBUG
        private const int MaxRowsPerInsert = 10;
#else
        private const int MaxRowsPerInsert = 1000;
#endif

        public ReportDatasetService(
            IBigQueryAdapter bigQueryAdapter,
            ILogger<ReportDatasetService> logger)
        {
            this.bigQueryAdapter = bigQueryAdapter;
            this.logger = logger;
        }

        public async Task PrepareDatasetAsync(
            DatasetLocator dataset,
            CancellationToken cancellationToken)
        {
            if (await this.bigQueryAdapter
                .IsDatasetAvailableAsync(dataset, cancellationToken)
                .ConfigureAwait(false))
            {
                //
                // Dataset exists, nothing to do.
                //
                this.logger.LogInformation("Found existing dataset {name}", dataset);
            }
            else
            {
                //
                // Create new dataset.
                //
                this.logger.LogInformation("Dataset {name} not found, creating new...", dataset);
                try
                {
                    await this.bigQueryAdapter
                        .CreateDatasetAsync(dataset, "License Tracking", cancellationToken)
                        .ConfigureAwait(false);

                    this.logger.LogInformation("Dataset {name} created", dataset);
                }
                catch (Exception)
                {
                    this.logger.LogError("Failed to create dataset {name}", dataset);
                    throw;
                }
            }

            //
            // Ensure that tables exist and their schema are up-to-date.
            //
            try
            {
                foreach (var table in new Dictionary<string, IList<TableFieldSchema>>()
                    {
                        { TableNames.PlacementStartedEvents, TableSchemas.PlacementStartedEvents },
                        { TableNames.PlacementEndedEvents, TableSchemas.PlacementEndedEvents},
                        { TableNames.AnalyisRuns, TableSchemas.AnalysisRuns }
                    })
                {
                    await this.bigQueryAdapter
                        .CreateOrPatchTableAsync(
                            new TableLocator(
                                dataset.ProjectId,
                                dataset.Name,
                                table.Key),
                            table.Value,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                this.logger.LogError("Failed to create or update tables in dataset {name}", dataset);
                throw;
            }

            //
            // Ensure that views exist and are up-to-date.
            //
            try
            {
                foreach (var view in new Dictionary<string, string>()
                    {
                        { ViewNames.Placements, ViewDefinitions.Placements(dataset.Name) },
                        { ViewNames.NodeTypeDetails, ViewDefinitions.NodeTypeDetails() }
                    })
                {
                    await this.bigQueryAdapter
                        .CreateOrPatchViewAsync(
                            new TableLocator(
                                dataset.ProjectId,
                                dataset.Name,
                                view.Key),
                            view.Value,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                this.logger.LogError("Failed to create or update views in dataset {name}", dataset);
                throw;
            }
        }

        public async Task<DateTime?> TryGetLastRunDateAsync(
            DatasetLocator dataset,
            CancellationToken cancellationToken)
        {
            var result = await this.bigQueryAdapter.QueryAsync(
                    dataset,
                    $"SELECT UNIX_MILLIS(TIMESTAMP (MAX(date))) FROM analysis_runs",
                    cancellationToken)
                .ConfigureAwait(false);
            var date = result.FirstOrDefault()?.F?.FirstOrDefault()?.V as string;

            if (date == null)
            {
                //
                // No prior runs.
                //
                return null;
            }
            else
            {
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(date)).DateTime;
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }

        public async Task SubmitPlacementReportAsync(
            DatasetLocator dataset,
            DateTime reportEndDate,
            PlacementReport report,
            CancellationToken cancellationToken)
        {
            //
            // Use the UNIX timestamp as unique ID for the run.
            //
            var runId = new DateTimeOffset(reportEndDate).ToUnixTimeMilliseconds();

            if (report.StartedPlacements.Any())
            {
                var rows = report.StartedPlacements
                    .Where(p => p.Instance != null)
                    .Select(p => new Dictionary<string, object?>
                    {
                        { FieldSchemas.RunId.Name, runId },
                        { FieldSchemas.InstanceId.Name, p.InstanceId },
                        { FieldSchemas.InstanceProjectId.Name, p.Instance!.ProjectId },
                        { FieldSchemas.InstanceZone.Name, p.Instance.Zone },
                        { FieldSchemas.InstanceName.Name, p.Instance.Name },
                        { FieldSchemas.ImageName.Name, p.Image?.Name },
                        { FieldSchemas.Date.Name, p.Placement.From },
                        { FieldSchemas.Tenancy.Name, p.Placement.Tenancy switch
                            {
                                Tenancies.Fleet => "F",
                                Tenancies.SoleTenant => "S",
                                _ => null
                            } },
                        { FieldSchemas.ServerId.Name, p.Placement.ServerId },
                        { FieldSchemas.NodeType.Name, p.Placement.NodeType?.Name },
                        { FieldSchemas.NodeProjectId.Name, p.Placement.NodeType?.ProjectId },
                        { FieldSchemas.OperatingSystemFamily.Name, p.License?.OperatingSystem switch
                            {
                                OperatingSystemTypes.Windows => "WIN",
                                OperatingSystemTypes.Linux => "LINUX",
                                _ => null
                            }
                        },
                        { FieldSchemas.License.Name, p.License?.License?.ToString() },
                        { FieldSchemas.LicenseType.Name, p.License?.LicenseType switch
                            {
                                LicenseTypes.Byol => "BYOL",
                                LicenseTypes.Spla => "SPLA",
                                _ => null
                            }
                        },
                        { FieldSchemas.MachineType.Name, p.Machine?.Type?.Name },
                        { FieldSchemas.VcpuCount.Name, p.Machine?.VirtualCpuCount },
                        { FieldSchemas.Memory.Name, p.Machine?.MemoryMb },
                        { FieldSchemas.MaintenancePolicy.Name, p.SchedulingPolicy?.MaintenancePolicy },
                        { FieldSchemas.VcpuMinAllocated.Name, p.SchedulingPolicy?.MinNodeCpus },
                        { FieldSchemas.Labels.Name, p.Labels
                            .EnsureNotNull()
                            .Select(kvp => new { key = kvp.Key, value = kvp.Value })
                            .ToArray()
                        }
                    })
                    .ToList();

                //
                // Split rows into smaller chunks so that we don't exceed
                // the maximum request size.
                //
                foreach (var chunkOfRows in rows.Chunk(MaxRowsPerInsert))
                {
                    await this.bigQueryAdapter
                        .InsertAsync(
                            new TableLocator(dataset, TableNames.PlacementStartedEvents),
                            chunkOfRows,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            if (report.EndedPlacements.Any())
            {
                var rows = report.EndedPlacements
                    .Where(p => p.Instance != null)
                    .Select(p => new Dictionary<string, object?>
                    {
                        { FieldSchemas.RunId.Name, runId },
                        { FieldSchemas.InstanceId.Name, p.InstanceId },
                        { FieldSchemas.Date.Name, p.Placement.To }
                    })
                    .ToList();

                //
                // Split rows into smaller chunks so that we don't exceed
                // the maximum request size.
                //
                foreach (var chunkOfRows in rows.Chunk(MaxRowsPerInsert))
                {
                    await this.bigQueryAdapter
                        .InsertAsync(
                            new TableLocator(dataset, TableNames.PlacementEndedEvents),
                            chunkOfRows,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            //
            // Now that all events have been added, append a record to the
            // analysis table to mark the run as completed.
            //
            var assemblyVersion = GetType().Assembly.GetName().Version;
            Debug.Assert(assemblyVersion != null);

            await this.bigQueryAdapter
                .InsertAsync(
                    new TableLocator(dataset, TableNames.AnalyisRuns),
                    new[] {
                        new Dictionary<string, object?>
                        {
                            { FieldSchemas.RunId.Name, runId },
                            { FieldSchemas.Date.Name, reportEndDate },
                            { FieldSchemas.Version.Name, assemblyVersion.ToInt64() }
                        }
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // Schema.
        //---------------------------------------------------------------------

        private static class ViewNames
        {
            public const string Placements = "placements";
            public const string NodeTypeDetails = "node_type_details";
        }

        private static class TableNames
        {
            public const string PlacementStartedEvents = "placement_started_events";
            public const string PlacementEndedEvents = "placement_ended_events";
            public const string AnalyisRuns = "analysis_runs";
        }

        private static class FieldSchemas
        {
            public static readonly TableFieldSchema RunId = new TableFieldSchema()
            {
                Name = "run_id",
                Type = "INT64",
                Mode = "REQUIRED"
            };

            public static readonly TableFieldSchema InstanceId = new TableFieldSchema()
            {
                Name = "instance_id",
                Type = "INT64",
                Mode = "REQUIRED"
            };

            public static readonly TableFieldSchema InstanceProjectId = new TableFieldSchema()
            {
                Name = "instance_project_id",
                Type = "STRING",
                Mode = "REQUIRED",
                MaxLength = 64
            };

            public static readonly TableFieldSchema InstanceZone = new TableFieldSchema()
            {
                Name = "instance_zone",
                Type = "STRING",
                Mode = "REQUIRED",
                MaxLength = 64
            };

            public static readonly TableFieldSchema InstanceName = new TableFieldSchema()
            {
                Name = "instance_name",
                Type = "STRING",
                Mode = "REQUIRED",
                MaxLength = 64
            };

            public static readonly TableFieldSchema ImageName = new TableFieldSchema()
            {
                Name = "image_name",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 128
            };

            public static readonly TableFieldSchema Date = new TableFieldSchema()
            {
                Name = "date",
                Type = "TIMESTAMP",
                Mode = "REQUIRED"
            };

            public static readonly TableFieldSchema Tenancy = new TableFieldSchema()
            {
                Name = "tenancy",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 1,
            };

            public static readonly TableFieldSchema ServerId = new TableFieldSchema()
            {
                Name = "server_id",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 128
            };

            public static readonly TableFieldSchema NodeType = new TableFieldSchema()
            {
                Name = "node_type",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 128
            };

            public static readonly TableFieldSchema OperatingSystemFamily = new TableFieldSchema()
            {
                Name = "operating_system_family",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 5,
            };

            public static readonly TableFieldSchema LicenseType = new TableFieldSchema()
            {
                Name = "license_type",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 4
            };

            public static readonly TableFieldSchema License = new TableFieldSchema()
            {
                Name = "license",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 128
            };

            public static readonly TableFieldSchema Version = new TableFieldSchema()
            {
                Name = "version",
                Type = "INT64",
                Mode = "REQUIRED"
            };

            public static readonly TableFieldSchema MachineType = new TableFieldSchema()
            {
                Name = "machine_type",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 128
            };

            public static readonly TableFieldSchema VcpuCount = new TableFieldSchema()
            {
                Name = "vcpu_count",
                Type = "INT64",
                Mode = "NULLABLE"
            };

            public static readonly TableFieldSchema VcpuMinAllocated = new TableFieldSchema()
            {
                Name = "vcpu_min_allocated",
                Type = "INT64",
                Mode = "NULLABLE"
            };

            public static readonly TableFieldSchema Memory = new TableFieldSchema()
            {
                Name = "memory_mb",
                Type = "INT64",
                Mode = "NULLABLE"
            };

            public static readonly TableFieldSchema MaintenancePolicy = new TableFieldSchema()
            {
                Name = "maintenance_policy",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 128
            };

            public static readonly TableFieldSchema Labels = new TableFieldSchema()
            {
                Name = "labels",
                Type = "STRUCT",
                Mode = "REPEATED",
                Fields = new[]
                {
                    new TableFieldSchema()
                    {
                        Name = "key",
                        Type = "STRING",
                        Mode = "NULLABLE",
                        MaxLength = 128
                    },
                    new TableFieldSchema()
                    {
                        Name = "value",
                        Type = "STRING",
                        Mode = "NULLABLE",
                        MaxLength = 256
                    }
                }
            };

            public static readonly TableFieldSchema NodeProjectId = new TableFieldSchema()
            {
                Name = "node_project_id",
                Type = "STRING",
                Mode = "NULLABLE",
                MaxLength = 64
            };
        }

        private static class TableSchemas
        {
            //
            // NB. All schema changes must be backward-compatible!
            //

            public static readonly IList<TableFieldSchema> PlacementStartedEvents = new TableFieldSchema[]
            {
                //
                // v1.0.0
                //
                FieldSchemas.RunId,
                FieldSchemas.InstanceId,
                FieldSchemas.InstanceProjectId,
                FieldSchemas.InstanceZone,
                FieldSchemas.InstanceName,
                FieldSchemas.ImageName,
                FieldSchemas.Date,
                FieldSchemas.Tenancy,
                FieldSchemas.ServerId,
                FieldSchemas.NodeType,
                FieldSchemas.OperatingSystemFamily,
                FieldSchemas.LicenseType,
                FieldSchemas.License,

                //
                // v1.1.0
                //
                FieldSchemas.MachineType,
                FieldSchemas.VcpuCount,
                FieldSchemas.Memory,
                FieldSchemas.MaintenancePolicy,
                FieldSchemas.VcpuMinAllocated,
                FieldSchemas.Labels,
                FieldSchemas.NodeProjectId,
            };

            public static readonly IList<TableFieldSchema> PlacementEndedEvents = new TableFieldSchema[]
            {
                FieldSchemas.RunId,
                FieldSchemas.InstanceId,
                FieldSchemas.Date,
            };

            public static readonly IList<TableFieldSchema> AnalysisRuns = new TableFieldSchema[]
            {
                FieldSchemas.RunId,
                FieldSchemas.Date,
                FieldSchemas.Version
            };
        }

        private static class ViewDefinitions
        {
            public static string Placements(string dataset)
            {
                return @$"
                    WITH raw_placements AS (
                        --
                        -- Match placement-started and placement-ended events
                        --
                        -- Note that we might have multiple placement-started events
                        -- for each placement-ended event because of staggered
                        -- analysis windows.
                        --
                        SELECT 
                            started.instance_id,
                            started.instance_name,
                            started.instance_zone,
                            started.instance_project_id,
                            started.tenancy,
                            started.server_id,
                            started.node_project_id,
                            started.node_type,
                            started.operating_system_family,
                            started.license,
                            started.license_type,    
                            started.machine_type,
                            started.vcpu_count,
                            started.memory_mb,
                            started.maintenance_policy,
                            started.vcpu_min_allocated,
                            started.date AS start_date,
                            MIN(ended.date) AS end_date
                        FROM `{dataset}.analysis_runs` runs
                        INNER JOIN `{dataset}.placement_started_events` started ON runs.run_id=started.run_id
                        LEFT OUTER JOIN `{dataset}.placement_ended_events` ended ON started.instance_id=ended.instance_id AND started.date<ended.date
                        GROUP BY
                            started.instance_id,
                            started.date,
                            started.instance_name,
                            started.instance_zone,
                            started.instance_project_id,
                            started.tenancy,
                            started.server_id,
                            started.node_project_id,
                            started.node_type,
                            started.operating_system_family,
                            started.license,
                            started.license_type,
                            started.machine_type,
                            started.vcpu_count,
                            started.memory_mb,
                            started.maintenance_policy,
                            started.vcpu_min_allocated)
                  
                        --
                        -- Filter out the redundant entries
                        --
                        -- When a delta analysis finds a VM to be running at the start of the
                        -- reporting window, it writes a placement_started event (typically
                        -- with a 00:00h timestamp. These entries need to be coalesced with
                        -- the ones produced by previous analysis runs and we do that by
                        -- aggregating them.
                        --
                        SELECT
                            r.instance_id,
                            r.instance_name,
                            r.instance_zone,
                            r.instance_project_id,
                            MAX(r.tenancy) AS tenancy,
                            MAX(r.server_id) AS server_id,
                            MAX(r.operating_system_family) AS operating_system_family,
                            MAX(r.license) AS license,
                            MAX(r.license_type) AS license_type,    
                            MAX(r.node_type) AS node_type,    
                            MAX(r.node_project_id) AS node_project_id,
                            MAX(r.machine_type) AS machine_type,
                            MAX(r.vcpu_count) AS vcpu_count,
                            MAX(r.memory_mb) AS memory_mb,
                            MAX(r.maintenance_policy) AS maintenance_policy,
                            MAX(r.vcpu_min_allocated) AS vcpu_min_allocated,

                            MIN(r.start_date) AS start_date,
                            r.end_date
                        FROM raw_placements r
                        GROUP BY
                            r.instance_id,
                            r.instance_name,
                            r.instance_zone,
                            r.instance_project_id,
                            r.end_date";
            }

            public static string NodeTypeDetails()
            {
                //
                // Information taken from https://cloud.google.com/compute/docs/nodes/sole-tenant-nodes#node_types,
                // the same data is currently not available via API.
                //
                var nodeTypeDetails = new[]
                {
                    new { NodeType = "c2-node-60-240", CoreCount = 36 },
                    new { NodeType = "c3-node-176-1408", CoreCount = 96 },
                    new { NodeType = "c3-node-176-352", CoreCount = 96 },
                    new { NodeType = "c3-node-176-704", CoreCount = 96 },
                    new { NodeType = "c3d-node-360-1440", CoreCount = 192 },
                    new { NodeType = "c3d-node-360-2880", CoreCount = 192 },
                    new { NodeType = "c3d-node-360-708", CoreCount = 192 },
                    new { NodeType = "c4-node-192-1488", CoreCount = 120 },
                    new { NodeType = "c4-node-192-1536", CoreCount = 104 },
                    new { NodeType = "c4-node-192-384", CoreCount = 104 },
                    new { NodeType = "c4-node-192-384", CoreCount = 120 },
                    new { NodeType = "c4-node-192-720", CoreCount = 120 },
                    new { NodeType = "c4-node-192-768", CoreCount = 104 },
                    new { NodeType = "c4a-node-72-144", CoreCount = 80 },
                    new { NodeType = "c4a-node-72-288", CoreCount = 80 },
                    new { NodeType = "c4a-node-72-576", CoreCount = 80 },
                    new { NodeType = "c4d-node-384-1488", CoreCount = 192 },
                    new { NodeType = "c4d-node-384-3024", CoreCount = 192 },
                    new { NodeType = "c4d-node-384-720", CoreCount = 192 },
                    new { NodeType = "g2-node-96-384", CoreCount = 56 },
                    new { NodeType = "g2-node-96-432", CoreCount = 56 },
                    new { NodeType = "h3-node-88-352", CoreCount = 96 },
                    new { NodeType = "m1-node-160-3844", CoreCount = 88 },
                    new { NodeType = "m1-node-96-1433", CoreCount = 56 },
                    new { NodeType = "m2-node-416-11776", CoreCount = 224 },
                    new { NodeType = "m2-node-416-8832", CoreCount = 224 },
                    new { NodeType = "m3-node-128-1952", CoreCount = 72 },
                    new { NodeType = "m3-node-128-3904", CoreCount = 72 },
                    new { NodeType = "m4-node-224-2976", CoreCount = 224 },
                    new { NodeType = "m4-node-224-5952", CoreCount = 224 },
                    new { NodeType = "n1-node-96-1433", CoreCount = 56 },
                    new { NodeType = "n1-node-96-624", CoreCount = 56 },
                    new { NodeType = "n2-node-128-864", CoreCount = 72 },
                    new { NodeType = "n2-node-80-640", CoreCount = 48 },
                    new { NodeType = "n2d-node-224-1792", CoreCount = 128 },
                    new { NodeType = "n2d-node-224-896", CoreCount = 128 },
                    new { NodeType = "n4-node-224-1372", CoreCount = 120 },
                    new { NodeType = "n4-node-224-1456", CoreCount = 60 },
                    new { NodeType = "n4-node-224-448", CoreCount = 60 },
                    new { NodeType = "n4-node-224-728", CoreCount = 60 },
                };

                return string.Join(
                    " UNION ALL ",
                    nodeTypeDetails.Select(d => $"SELECT '{d.NodeType}' as node_type, {d.CoreCount} as core_count"));
            }
        }
    }
}
