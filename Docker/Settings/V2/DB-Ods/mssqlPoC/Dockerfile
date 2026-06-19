# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Base image with additional packages
FROM mcr.microsoft.com/mssql/server:2022-CU15-ubuntu-22.04@sha256:804e527187b16fe05c4321b58d76850966cbaf73e31942416ce0bbefe1b0eb63 AS base
USER root

RUN ACCEPT_EULA=Y apt-get update -y && \
    ACCEPT_EULA=Y apt-get upgrade -y && \
    ACCEPT_EULA=Y apt-get -y install --no-install-recommends apt-utils -y unzip=6.0-26ubuntu3 dos2unix=7.4.2-2 && \
    rm -rf /var/lib/apt/lists/* && \
    echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections && \
    # These steps are needed in order to mount the database to a host volume
    mkdir -p /var/opt/mssql/data /var/opt/mssql/log && \
    chown -R mssql: /var/opt/mssql/data /var/opt/mssql/log
USER mssql

# Start a new layer so that the above layer can be cached
FROM base AS build
ARG MINIMAL_ODS_VERSION
ARG POPULATED_ODS_VERSION
ARG STANDARD_VERSION
ARG ODS_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Minimal.Template.Standard.${STANDARD_VERSION}/versions/${MINIMAL_ODS_VERSION}/content
ARG POPULATED_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Populated.Template.Standard.${STANDARD_VERSION}/versions/${POPULATED_ODS_VERSION}/content

LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

# These variables can be overwritten at runtime
ENV MSSQL_PID=Express

USER root
WORKDIR /app

# Download and extract Minimal Template for core Ed-Fi Data Model
RUN umask 0077 && \
    mkdir backups && \
    mkdir -p /var/opt/mssql/data/backups && \
    chown mssql:mssql /var/opt/mssql/data/backups && \
    wget -q -O ./backups/OdsMinimalDatabase.zip ${ODS_URL} && \
    unzip -p ./backups/OdsMinimalDatabase.zip EdFi.Ods.Minimal.Template.bak > /var/opt/mssql/data/backups/EdFi_Ods_Minimal_Template.bak && \
    rm -f ./backups/OdsMinimalDatabase.zip

    # Download and extract Populated Template for core Ed-Fi Data Model
RUN wget -q -O ./backups/OdsPopulatedDatabase.zip ${POPULATED_URL} && \
    unzip -p ./backups/OdsPopulatedDatabase.zip EdFi.Ods.Populated.Template.bak > /var/opt/mssql/data/backups/EdFi_Ods_Populated_Template.bak && \
    rm -f ./backups/OdsPopulatedDatabase.zip && \
    apt-get --purge autoremove unzip -y

COPY --chmod=500 ./init.sh .

RUN dos2unix ./init.sh && \
    chown -R mssql . && \
    apt-get --purge autoremove dos2unix -y

EXPOSE 1433
USER mssql
ENTRYPOINT ["/app/init.sh"]