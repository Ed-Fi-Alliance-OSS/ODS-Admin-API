# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

#edfialliance/ods-api-db-admin:7.3.1
FROM edfialliance/ods-api-db-admin:7.3.1@sha256:9d6c6ad298f5eb2ea58d7b2c1c7ea5f6bdfcd12d90028b98fbfea4237a5610f2 AS base
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ARG POSTGRES_USER=postgres
ENV POSTGRES_USER=${POSTGRES_USER}
ENV POSTGRES_DB=postgres

ARG VERSION
ARG ADMIN_API_VERSION
ENV VERSION="${ADMIN_API_VERSION:-2.2.0}"

USER root
COPY run-adminapi-migrations.sh /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh
RUN apk add --no-cache dos2unix=7.5.2-r0 unzip=6.0-r15 && rm -rf /var/cache/apk/*
USER ${POSTGRES_USER}

FROM base AS setup

USER root
RUN wget -nv -O /tmp/EdFi_AdminApi_Scripts.zip "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.ODS.AdminApi/versions/${VERSION}/content" && \
    # Admin
    mkdir -p /tmp/AdminApiScripts/Admin/ && \
    unzip /tmp/EdFi_AdminApi_Scripts.zip AdminApi/Artifacts/PgSql/Structure/Admin/* -d /tmp/AdminApiScripts/Admin/ && \
    cp -r /tmp/AdminApiScripts/Admin/AdminApi/Artifacts/PgSql/Structure/Admin/. /tmp/AdminApiScripts/Admin/PgSql/ && \
    rm -r /tmp/AdminApiScripts/Admin/AdminApi && \
    dos2unix /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh && \
    dos2unix /tmp/AdminApiScripts/Admin/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Admin/PgSql && \
    # Security
    mkdir -p /tmp/AdminApiScripts/Security/ && \
    unzip /tmp/EdFi_AdminApi_Scripts.zip AdminApi/Artifacts/PgSql/Structure/Security/* -d /tmp/AdminApiScripts/Security/ && \
    cp -r /tmp/AdminApiScripts/Security/AdminApi/Artifacts/PgSql/Structure/Security/. /tmp/AdminApiScripts/Security/PgSql/ && \
    rm -r /tmp/AdminApiScripts/Security/AdminApi && \
    dos2unix /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh && \
    dos2unix /tmp/AdminApiScripts/Security/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Security/PgSql && \
    # Clean up
    rm -f /tmp/EdFi_AdminApi_Scripts.zip && \
    apk del dos2unix unzip

USER ${POSTGRES_USER}

EXPOSE 5432

CMD ["docker-entrypoint.sh", "postgres"]
