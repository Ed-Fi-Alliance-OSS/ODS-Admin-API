# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

ARG POSTGRES_BASE_IMAGE=dhi.io/postgres:16
FROM edfialliance/ods-api-db-admin:7.3.1 AS legacy_assets

FROM alpine:3.20 AS assets

ARG ADMIN_API_VERSION
ENV VERSION="${ADMIN_API_VERSION:-2.2.0}"

COPY --from=shared Settings/shared/DB-Admin/pgsql/run-adminapi-migrations.sh /tmp/3-run-adminapi-migrations.sh
COPY --from=shared Settings/shared/DB-Admin/pgsql/entrypoint.sh /tmp/entrypoint.sh
COPY --from=legacy_assets /docker-entrypoint-initdb.d/1-init-database.sh /tmp/1-init-database.sh
COPY --from=legacy_assets /tmp/EdFi_Admin.sql /tmp/EdFi_Admin.sql
COPY --from=legacy_assets /tmp/EdFi_Security.sql /tmp/EdFi_Security.sql

RUN apk add --no-cache dos2unix=7.5.2-r0 unzip=6.0-r15 wget=1.24.5-r0 && \
    wget -nv -O /tmp/EdFi_AdminApi_Scripts.zip "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.ODS.AdminApi/versions/${VERSION}/content" && \
    # Admin
    mkdir -p /tmp/AdminApiScripts/Admin/ && \
    unzip /tmp/EdFi_AdminApi_Scripts.zip AdminApi/Artifacts/PgSql/Structure/Admin/* -d /tmp/AdminApiScripts/Admin/ && \
    cp -r /tmp/AdminApiScripts/Admin/AdminApi/Artifacts/PgSql/Structure/Admin/. /tmp/AdminApiScripts/Admin/PgSql/ && \
    rm -r /tmp/AdminApiScripts/Admin/AdminApi && \
    dos2unix /tmp/AdminApiScripts/Admin/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Admin/PgSql && \
    # Security
    mkdir -p /tmp/AdminApiScripts/Security/ && \
    unzip /tmp/EdFi_AdminApi_Scripts.zip AdminApi/Artifacts/PgSql/Structure/Security/* -d /tmp/AdminApiScripts/Security/ && \
    cp -r /tmp/AdminApiScripts/Security/AdminApi/Artifacts/PgSql/Structure/Security/. /tmp/AdminApiScripts/Security/PgSql/ && \
    rm -r /tmp/AdminApiScripts/Security/AdminApi && \
    dos2unix /tmp/AdminApiScripts/Security/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Security/PgSql && \
    dos2unix /tmp/1-init-database.sh && \
    dos2unix /tmp/entrypoint.sh && \
    chmod 755 /tmp/entrypoint.sh && \
    dos2unix /tmp/3-run-adminapi-migrations.sh && \
    rm -f /tmp/EdFi_AdminApi_Scripts.zip

FROM ${POSTGRES_BASE_IMAGE} AS setup
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ARG POSTGRES_USER=postgres
ENV POSTGRES_USER=${POSTGRES_USER}
ENV POSTGRES_DB=postgres

USER root
COPY --from=assets /tmp/entrypoint.sh /usr/local/bin/adminapi-db-entrypoint.sh
COPY --from=assets /tmp/1-init-database.sh /docker-entrypoint-initdb.d/1-init-database.sh
COPY --from=assets /tmp/3-run-adminapi-migrations.sh /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh
COPY --from=assets /tmp/EdFi_Admin.sql /tmp/EdFi_Admin.sql
COPY --from=assets /tmp/EdFi_Security.sql /tmp/EdFi_Security.sql
COPY --from=assets /tmp/AdminApiScripts /tmp/AdminApiScripts

USER ${POSTGRES_USER}

EXPOSE 5432

ENTRYPOINT ["/usr/local/bin/adminapi-db-entrypoint.sh"]
