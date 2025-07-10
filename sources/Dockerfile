#
# Copyright 2022 Google LLC
#
# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
# 
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.
#

#------------------------------------------------------------------------------
# Build.
#------------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy local code to the container image.
COPY . ./

# Build a release artifact for main project, skipping test project.
RUN dotnet publish -c Release -o publish Google.Solutions.LicenseTracker/Google.Solutions.LicenseTracker.csproj

#------------------------------------------------------------------------------
# Run.
#------------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish ./

RUN groupadd -r runner && useradd -rm -g runner runner
RUN chown -R runner /app && \
    chmod ugo+r -R /app/*
USER runner

ENTRYPOINT ["dotnet", "Google.Solutions.LicenseTracker.dll", "--autodiscover", "--log-format=json", "-v", "--analysis-window=90"]

