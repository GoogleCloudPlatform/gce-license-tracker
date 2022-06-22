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
using Google.Apis.Bigquery.v2;
using Google.Apis.Bigquery.v2.Data;
using Google.Solutions.LicenseTracker.Data.Locator;
using Google.Solutions.LicenseTracker.Util;

namespace Google.Solutions.LicenseTracker.Adapters
{
    public interface IBigQueryAdapter
    {
        Task<bool> IsDatasetAvailableAsync(
            DatasetLocator dataset,
            CancellationToken cancellationToken);

        Task<Dataset> CreateDatasetAsync(
            DatasetLocator dataset,
            string description,
            CancellationToken cancellationToken);

        Task<Table> CreateTableAsync(
            TableLocator table,
            IList<TableFieldSchema> fields,
            CancellationToken cancellationToken);

        Task<Table> CreateViewAsync(
            TableLocator view,
            string query,
            CancellationToken cancellationToken);

        Task InsertAsync(
            TableLocator table,
            IEnumerable<IDictionary<string, object?>> rows,
            CancellationToken cancellationToken);

        Task<IList<TableRow>> QueryAsync(
            DatasetLocator dataset,
            string query,
            CancellationToken cancellationToken);
    }

    internal class BigQueryAdapter : IBigQueryAdapter
    {
        private readonly BigqueryService service;

        public BigQueryAdapter(
            ICredential credential)
        {
            this.service = new BigqueryService(
               new Apis.Services.BaseClientService.Initializer()
               {
                   HttpClientInitializer = credential
               });
        }

        public async Task<bool> IsDatasetAvailableAsync(
            DatasetLocator dataset,
            CancellationToken cancellationToken)
        {
            try
            {
                await this.service.Datasets
                    .Get(dataset.ProjectId, dataset.Name)
                    .ExecuteAsync(cancellationToken);

                return true;
            }
            catch (GoogleApiException e) when (e.IsNotFoundError())
            {
                return false;
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Insufficient permissions to access dataset {dataset}",
                    e);
            }
        }

        public async Task<Dataset> CreateDatasetAsync(
            DatasetLocator dataset,
            string description,
            CancellationToken cancellationToken)
        {
            try
            {
                return await this.service.Datasets
                    .Insert(new Apis.Bigquery.v2.Data.Dataset()
                    {
                        DatasetReference = new Apis.Bigquery.v2.Data.DatasetReference()
                        {
                            DatasetId = dataset.Name,
                            ProjectId = dataset.ProjectId
                        },
                        Description = description
                    },
                        dataset.ProjectId)
                    .ExecuteAsync(cancellationToken);
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Insufficient permissions to create dataset {dataset}",
                    e);
            }
        }

        public async Task<Table> CreateTableAsync(
            TableLocator table,
            IList<TableFieldSchema> fields,
            CancellationToken cancellationToken)
        {
            try
            {
                var dataset = table.Dataset;
                return await this.service.Tables
                        .Insert(new Table()
                        {
                            TableReference = new TableReference()
                            {
                                DatasetId = dataset.Name,
                                ProjectId = dataset.ProjectId,
                                TableId = table.Name
                            },
                            Schema = new TableSchema()
                            {
                                Fields = fields
                            }
                        },
                        dataset.ProjectId,
                        dataset.Name)
                    .ExecuteAsync(cancellationToken);
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Insufficient permissions to create table in dataset {table.Dataset}",
                    e);
            }
        }

        public async Task<Table> CreateViewAsync(
            TableLocator view,
            string query,
            CancellationToken cancellationToken)
        {
            try
            {
                var dataset = view.Dataset;
                return await this.service.Tables
                        .Insert(new Table()
                        {
                            TableReference = new TableReference()
                            {
                                DatasetId = dataset.Name,
                                ProjectId = dataset.ProjectId,
                                TableId = view.Name
                            },
                            View = new ViewDefinition()
                            {
                                Query = query,
                                UseLegacySql = false
                            }
                        },
                        dataset.ProjectId,
                        dataset.Name)
                    .ExecuteAsync(cancellationToken);
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Insufficient permissions to create view in dataset {view.Dataset}",
                    e);
            }
        }

        public async Task<IList<TableRow>> QueryAsync(
            DatasetLocator dataset,
            string query,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.service.Jobs
                        .Query(new QueryRequest()
                        {
                            DefaultDataset = new DatasetReference()
                            {
                                DatasetId = dataset.Name,
                                ProjectId = dataset.ProjectId
                            },
                            UseLegacySql = false,
                            Query = query
                        },
                        dataset.ProjectId)
                    .ExecuteAsync(cancellationToken);

                if (response.Errors?.Any() == true)
                {
                    throw new BigQueryException(
                        "Insert failed: " + string.Join(", ",
                            response.Errors
                                .SelectMany(e => e.Message)
                                .ToList()));
                }
                else
                {
                    return response.Rows;
                }
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Insufficient permissions to query dataset {dataset}",
                    e);
            }
        }

        public async Task InsertAsync(
            TableLocator table,
            IEnumerable<IDictionary<string, object?>> rows,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.service.Tabledata
                        .InsertAll(new TableDataInsertAllRequest()
                        {
                            Rows = rows
                                .Select(r => new TableDataInsertAllRequest.RowsData()
                                {
                                    Json = r
                                })
                                .ToList()
                        },
                        table.ProjectId,
                        table.Dataset.Name,
                        table.Name)
                    .ExecuteAsync(cancellationToken);

                if (response.InsertErrors?.Any() == true)
                {
                    throw new BigQueryException(
                        "Insert failed: " + string.Join(", ",
                            response.InsertErrors
                                .SelectMany(e => e.Errors)
                                .SelectMany(e => e.Message)
                                .ToList()));
                }
            }
            catch (GoogleApiException e) when (e.IsAccessDeniedError())
            {
                throw new ResourceAccessDeniedException(
                    $"Insufficient permissions to insert into table {table}",
                    e);
            }
        }
    }

    public class BigQueryException : Exception
    {
        public BigQueryException(string message) : base(message)
        {
        }
    }
}
