# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# First two layers use a dotnet/sdk image to build the Admin API from source
# code. The next two layers use the dotnet/aspnet image to run the built code.
# The extra layers in the middle support caching of base layers.

FROM mcr.microsoft.com/dotnet/sdk:8.0.403-alpine3.20@sha256:07cb8622ca6c4d7600b42b2eccba968dff4b37d41b43a9bf4bd800aa02fab117 AS build
RUN apk upgrade --no-cache && apk add --no-cache musl=~1.2.5-r1
ARG ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-"Production"}

WORKDIR /source
COPY --from=assets ./Application/NuGet.Config ./
COPY --from=assets ./Application/Directory.Packages.props ./
COPY --from=assets ./Application/NuGet.Config EdFi.Ods.AdminApi/
COPY --from=assets ./Application/EdFi.Ods.AdminApi EdFi.Ods.AdminApi/
RUN rm -f EdFi.Ods.AdminApi/appsettings.Development.json

COPY --from=assets ./Application/NuGet.Config EdFi.Ods.AdminApi.AdminConsole/
COPY --from=assets ./Application/EdFi.Ods.AdminApi.AdminConsole EdFi.Ods.AdminApi.AdminConsole/

COPY --from=assets ./Application/NuGet.Config EdFi.Ods.AdminApi.Common/
COPY --from=assets ./Application/EdFi.Ods.AdminApi.Common EdFi.Ods.AdminApi.Common/

WORKDIR /source/EdFi.Ods.AdminApi
RUN export ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT
RUN dotnet restore && dotnet build -c Release
RUN dotnet publish -c Release /p:EnvironmentName=$ASPNETCORE_ENVIRONMENT --no-build -o /app/EdFi.Ods.AdminApi

FROM mcr.microsoft.com/dotnet/aspnet:8.0.10-alpine3.20-amd64@sha256:1659f678b93c82db5b42fb1fb12d98035ce482b85747c2c54e514756fa241095 AS runtimebase
RUN apk upgrade --no-cache && \
    apk add --no-cache bash=~5 dos2unix=~7 gettext=~0 icu=~74 musl=~1.2.5-r1 openssl=3.3.4-r0 postgresql14-client=~14 && \
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

COPY --chmod=500 Settings/dev/${DB_FOLDER}/run.sh /app/run.sh
COPY Settings/dev/log4net.config /app/log4net.txt

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
