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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Google.Solutions.LicenseTracker.Util
{
    public interface IConsoleHandler
    {
        Task RunAsync();
    }

    internal static class ConsoleHostExtensions
    {
        public static async Task RunConsoleAsync<THandler>(
            this IHostBuilder hostBuilder)
            where THandler : class, IConsoleHandler
        {
            await hostBuilder
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<BootstrapService<THandler>>();
                    services.AddTransient<THandler>();
                })
                .RunConsoleAsync();
        }


        /// <summary>
        /// Hosted service that invokes the handler
        /// with dependency injection enabled.
        /// </summary>
        internal sealed class BootstrapService<THandler> : IHostedService
            where THandler : IConsoleHandler
        {
            private readonly IHostApplicationLifetime lifetime;
            private readonly THandler application;
            private readonly ILogger logger;

            public BootstrapService(
                THandler application,
                IHostApplicationLifetime appLifetime,
                ILogger<BootstrapService<THandler>> logger)
            {
                this.application = application;
                this.lifetime = appLifetime;
                this.logger = logger;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                this.lifetime.ApplicationStarted.Register(() =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await this.application
                                .RunAsync()
                                .ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            this.logger.LogError(e.Message);
                            for (var nestedException = e.InnerException;
                                nestedException != null;
                                nestedException = nestedException.InnerException)
                            {
                                this.logger.LogError("...caused by: {ex}", nestedException.Message);
                            }

                            this.logger.LogError(e, "Exception details");

                            Environment.ExitCode = 1;
                        }
                        finally
                        {
                            this.lifetime.StopApplication();
                        }
                    });
                });

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
