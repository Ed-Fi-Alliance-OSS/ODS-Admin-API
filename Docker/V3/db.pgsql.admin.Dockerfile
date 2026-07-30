# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

ARG POSTGRES_BASE_IMAGE=dhi.io/postgres:16
FROM edfialliance/ods-api-db-admin:7.3.1 AS legacy_assets

FROM alpine:3.20 AS prep

COPY --from=legacy_assets /docker-entrypoint-initdb.d/1-init-database.sh /tmp/1-init-database.sh
COPY --from=legacy_assets /tmp/EdFi_Admin.sql /tmp/EdFi_Admin.sql
COPY --from=legacy_assets /tmp/EdFi_Security.sql /tmp/EdFi_Security.sql
COPY Settings/shared/DB-Admin/pgsql/entrypoint.sh /tmp/entrypoint.sh
COPY Settings/shared/DB-Admin/pgsql/run-adminapi-migrations.sh /tmp/3-run-adminapi-migrations.sh
COPY --from=assets Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Admin/ /tmp/AdminApiScripts/Admin/PgSql
COPY --from=assets Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Security/ /tmp/AdminApiScripts/Security/PgSql
COPY Settings/dev/adminapi-test-seeddata.sql /tmp/AdminApiScripts/Admin/PgSql/adminapi-test-seeddata.sql

RUN sed -i 's/\r$//' /tmp/entrypoint.sh && \
    chmod 755 /tmp/entrypoint.sh && \
    sed -i 's/\r$//' /tmp/1-init-database.sh && \
    sed -i 's/\r$//' /tmp/3-run-adminapi-migrations.sh && \
    sed -i 's/\r$//' /tmp/AdminApiScripts/Admin/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Admin/PgSql/* && \
    sed -i 's/\r$//' /tmp/AdminApiScripts/Security/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Security/PgSql/*

FROM ${POSTGRES_BASE_IMAGE} AS base
USER root

FROM base AS setup
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

USER root

COPY --from=prep /tmp/entrypoint.sh /usr/local/bin/adminapi-db-entrypoint.sh
COPY --from=prep /tmp/1-init-database.sh /docker-entrypoint-initdb.d/1-init-database.sh
COPY --from=prep /tmp/3-run-adminapi-migrations.sh /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh
COPY --from=prep /tmp/EdFi_Admin.sql /tmp/EdFi_Admin.sql
COPY --from=prep /tmp/EdFi_Security.sql /tmp/EdFi_Security.sql
COPY --from=prep /tmp/AdminApiScripts /tmp/AdminApiScripts

USER postgres

EXPOSE 5432

ENTRYPOINT ["/usr/local/bin/adminapi-db-entrypoint.sh"]


