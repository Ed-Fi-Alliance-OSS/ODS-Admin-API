# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# First two layers use a dotnet/sdk image to build the Admin API from source
# code. The next two layers use the dotnet/aspnet image to run the built code.
# The extra layers in the middle support caching of base layers.

# Define assets stage using Alpine 3.22 to match the version used in other stages
FROM alpine:3.22@sha256:310c62b5e7ca5b08167e4384c68db0fd2905dd9c7493756d356e893909057601 AS assets

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.22@sha256:ddae163c8bd4d12df3bd767bdea4afc248abfd1148cdbb8ac549fa80bbb397dc AS build
RUN apk add --no-cache musl=~1.2.5-r12 && \
    rm -rf /var/cache/apk/*

ARG ASPNETCORE_ENVIRONMENT="Production"
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}

WORKDIR /source
COPY --from=assets ./Application/NuGet.Config ./
COPY --from=assets ./Application/Directory.Packages.props ./
COPY --from=assets ./Application/NuGet.Config EdFi.Ods.AdminApi/
COPY --from=assets ./Application/EdFi.Ods.AdminApi EdFi.Ods.AdminApi/
RUN rm -f EdFi.Ods.AdminApi/appsettings.Development.json

COPY --from=assets ./Application/NuGet.Config EdFi.Ods.AdminApi.Common/
COPY --from=assets ./Application/EdFi.Ods.AdminApi.Common EdFi.Ods.AdminApi.Common/
COPY --from=assets ./Application/EdFi.Ods.AdminApi.InstanceManagement EdFi.Ods.AdminApi.InstanceManagement/

COPY --from=assets ./Application/EdFi.Ods.AdminApi.V1 EdFi.Ods.AdminApi.V1/

WORKDIR /source/EdFi.Ods.AdminApi
RUN export ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT
RUN dotnet restore && dotnet build -c Release
RUN dotnet publish -c Release /p:EnvironmentName=$ASPNETCORE_ENVIRONMENT --no-build -o /app/EdFi.Ods.AdminApi

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.22-amd64@sha256:86b43b7250c683781587f9e8d30a2315c5684f1b1fb788a9aa74e86bc06df4a5 AS runtimebase
RUN apk add --no-cache \
        bash=~5 \
        dos2unix=~7 \
        gettext=~0 \
        icu=~76.1-r1 \
        musl=~1.2.5-r12 \
        openssl=3.5.6-r0 \
        postgresql15-client=~15 && \
    rm -rf /var/cache/apk/* && \
    addgroup -S edfi && adduser -S edfi -G edfi

FROM runtimebase AS setup
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

# Alpine image does not contain Globalization Cultures library so we need to install ICU library to get for LINQ expression to work
# Disable the globaliztion invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_HTTP_PORTS=80
ENV DB_FOLDER=pgsql

WORKDIR /app
COPY --from=build /app/EdFi.Ods.AdminApi .

COPY --chmod=500 --from=assets Docker/Settings/dev/${DB_FOLDER}/run.sh /app/run.sh
COPY --from=assets Docker/Settings/dev/log4net.config /app/log4net.txt

RUN cp /app/log4net.txt /app/log4net.config && \
    dos2unix /app/*.json && \
    dos2unix /app/*.sh && \
    dos2unix /app/log4net.config && \
    chmod 500 /app/*.sh -- ** && \
    chown -R edfi /app && \
    apk del dos2unix

EXPOSE ${ASPNETCORE_HTTP_PORTS}
USER edfi

ENTRYPOINT ["/app/run.sh"]
