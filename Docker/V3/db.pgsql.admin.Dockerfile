# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

ARG POSTGRES_BASE_IMAGE=dhi.io/postgres:16@sha256:6a16c62599f5f6d560685d56a733496b0c8451c8afb9255e31fefdd32c7d6d52
FROM edfialliance/ods-api-db-admin:7.3.1@sha256:acc254de6cf385c23c9e6149c0cdc730ca414c7d04435df4cd78fc5540d4b176 AS legacy_assets

FROM alpine:3.20@sha256:e9a2035f9d0d7cee1cdd445f5bfa0c5c646455ee26f14565dce23cf2d2de7570 AS prep

COPY --from=legacy_assets /docker-entrypoint-initdb.d/1-init-database.sh /tmp/1-init-database.sh
COPY --from=legacy_assets /tmp/EdFi_Admin.sql /tmp/EdFi_Admin.sql
COPY --from=legacy_assets /tmp/EdFi_Security.sql /tmp/EdFi_Security.sql
# hadolint ignore=DL3022
COPY --from=assets Docker/Settings/shared/DB-Admin/pgsql/entrypoint.sh /tmp/entrypoint.sh
# hadolint ignore=DL3022
COPY --from=assets Docker/Settings/shared/DB-Admin/pgsql/run-adminapi-migrations.sh /tmp/3-run-adminapi-migrations.sh
# hadolint ignore=DL3022
COPY --from=assets Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Admin/ /tmp/AdminApiScripts/Admin/PgSql
# hadolint ignore=DL3022
COPY --from=assets Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Security/ /tmp/AdminApiScripts/Security/PgSql
# hadolint ignore=DL3022
COPY --from=assets Docker/Settings/dev/adminapi-test-seeddata.sql /tmp/AdminApiScripts/Admin/PgSql/adminapi-test-seeddata.sql

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


