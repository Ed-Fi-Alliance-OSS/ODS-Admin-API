# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM mcr.microsoft.com/mssql/server@sha256:d7f2c670f0cd807b4dc466b8887bd2b39a4561f624c154896f5564ea38efd13a AS base
USER root
RUN apt-get update && apt-get install unzip -y dos2unix busybox openssl libxml2
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ENV MSSQL_USER=${SQLSERVER_USER}
ENV MSSQL_PASSWORD=${SQLSERVER_PASSWORD}
ENV MSSQL_DB=master

ARG STANDARD_VERSION="5.0.0"
ARG ADMIN_VERSION="7.2.49"
ARG SECURITY_VERSION="7.2.48"
ARG ADMIN_API_VERSION
ENV ADMIN_API_VERSION="${ADMIN_API_VERSION:-2.2.2}"

USER root
COPY healthcheck.sh /usr/local/bin/healthcheck.sh
RUN chmod +x /usr/local/bin/healthcheck.sh

COPY init-database.sh /tmp/init/3-init-database.sh
COPY entrypoint.sh /tmp/init/entrypoint.sh

RUN useradd -M -s /bin/bash -u 10099 -g 0 edfi  && \
    mkdir -p -m 770 /var/opt/edfi && chgrp -R 0 /var/opt/edfi && \
    wget -nv -O /tmp/sqlpackage.zip "https://aka.ms/sqlpackage-linux" && \
    unzip -o /tmp/sqlpackage.zip -d /opt/sqlpackage && \
    chmod +x /opt/sqlpackage/sqlpackage
FROM base AS setup

USER root
RUN wget -q -O /tmp/EdFi_Admin.zip "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Database.Admin.BACPAC.Standard.${STANDARD_VERSION}/versions/${ADMIN_VERSION}/content" && \
    wget -q -O /tmp/EdFi_Security.zip "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Database.Security.BACPAC.Standard.${STANDARD_VERSION}/versions/${SECURITY_VERSION}/content" && \
    wget -nv -O /tmp/EdFi_AdminApi_Scripts.zip "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.ODS.AdminApi/versions/${ADMIN_API_VERSION}/content" && \
    unzip -p /tmp/EdFi_Admin.zip EdFi_Admin.bacpac > /tmp/EdFi_Admin.bacpac && \
    unzip -p /tmp/EdFi_Security.zip EdFi_Security.bacpac > /tmp/EdFi_Security.bacpac && \
    dos2unix /tmp/EdFi_Admin.bacpac && \
    dos2unix /tmp/EdFi_Security.bacpac && \
    dos2unix /tmp/init/3-init-database.sh && \
    chmod +x /tmp/init/3-init-database.sh && \
    # Admin
    mkdir -p /tmp/AdminApiScripts/Admin/ && \
    unzip -o /tmp/EdFi_AdminApi_Scripts.zip AdminApi/Artifacts/MsSql/Structure/Admin/* -d  /tmp/AdminApiScripts/Admin/ && \
    cp -r /tmp/AdminApiScripts/Admin/AdminApi/Artifacts/MsSql/Structure/Admin/. /tmp/AdminApiScripts/Admin/MsSql/ && \
    dos2unix /tmp/AdminApiScripts/Admin/MsSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Admin/MsSql && \
    # Security
    mkdir -p /tmp/AdminApiScripts/Security/ && \
    unzip -o /tmp/EdFi_AdminApi_Scripts.zip AdminApi/Artifacts/MsSql/Structure/Security/* -d  /tmp/AdminApiScripts/Security/ && \
    cp -r /tmp/AdminApiScripts/Security/AdminApi/Artifacts/MsSql/Structure/Security/. /tmp/AdminApiScripts/Security/MsSql/ && \
    dos2unix /tmp/AdminApiScripts/Security/MsSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Security/MsSql && \
    # Clean up
    rm -f /tmp/EdFi_Admin.zip  && \
    rm -f /tmp/EdFi_Security.zip && \
    rm -f /tmp/EdFi_AdminApi_Scripts.zip

EXPOSE 1433

USER edfi


CMD ["/bin/bash", "/tmp/init/entrypoint.sh"]
