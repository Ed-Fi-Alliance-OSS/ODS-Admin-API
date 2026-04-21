# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.22-amd64@sha256:86b43b7250c683781587f9e8d30a2315c5684f1b1fb788a9aa74e86bc06df4a5 AS base
RUN apk upgrade --no-cache && \
    apk add --no-cache unzip=~6 dos2unix=~7 bash=~5 gettext=~0 jq=~1 icu=~76.1-r1 openssl=3.5.6-r0 musl=~1.2.5-r12 && \
    addgroup -S edfi && adduser -S edfi -G edfi

FROM base AS build
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

# Alpine image does not contain Globalization Cultures library so we need to install ICU library to get for LINQ expression to work
# Disable the globaliztion invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ARG ADMIN_API_VERSION
ENV ADMIN_API_VERSION="${ADMIN_API_VERSION:-2.2.0}"
ENV ASPNETCORE_HTTP_PORTS=80

WORKDIR /app

COPY --chmod=500 Settings/mssql/run.sh /app/run.sh
COPY Settings/mssql/log4net.config /app/log4net.txt

RUN umask 0077  && \
    wget -nv -O /tmp/msodbcsql18_18.4.1.1-1_amd64.apk https://download.microsoft.com/download/7/6/d/76de322a-d860-4894-9945-f0cc5d6a45f8/msodbcsql18_18.4.1.1-1_amd64.apk && \
    wget -nv -O /tmp/mssql-tools18_18.4.1.1-1_amd64.apk https://download.microsoft.com/download/7/6/d/76de322a-d860-4894-9945-f0cc5d6a45f8/mssql-tools18_18.4.1.1-1_amd64.apk && \
    apk --no-cache add --allow-untrusted /tmp/msodbcsql18_18.4.1.1-1_amd64.apk  && \
    apk --no-cache add --allow-untrusted /tmp/mssql-tools18_18.4.1.1-1_amd64.apk && \
    wget -nv -O /app/AdminApi.zip "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.ODS.AdminApi/versions/${ADMIN_API_VERSION}/content" && \
    unzip /app/AdminApi.zip AdminApi/* -d /app/ && \
    cp -r /app/AdminApi/. /app/ && \
    rm -f /app/AdminApi.zip && \
    rm -r /app/AdminApi && \
    cp /app/log4net.txt /app/log4net.config && \
    dos2unix /app/*.json && \
    dos2unix /app/*.sh && \
    dos2unix /app/log4net.config && \
    chmod 700 /app/*.sh -- ** && \
    rm -f /app/*.exe && \
    apk del unzip dos2unix && \
    chown -R edfi /app

EXPOSE ${ASPNETCORE_HTTP_PORTS}
USER edfi

ENTRYPOINT [ "/app/run.sh" ]
