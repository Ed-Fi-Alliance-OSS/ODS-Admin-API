# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM alpine:3.20@sha256:187cce89a2fdd4eaf457a0af45f5ce27672f35ce0f6df49b5b0ee835afe0561b AS assets

FROM edfialliance/ods-api-db-admin:7.3.1@sha256:9d6c6ad298f5eb2ea58d7b2c1c7ea5f6bdfcd12d90028b98fbfea4237a5610f2 AS base
USER root
RUN apk add --no-cache dos2unix=7.5.2-r0 unzip=6.0-r15 && rm -rf /var/cache/apk/*

FROM base AS setup
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

USER root

COPY --from=assets Docker/Settings/V2/DB-Admin/pgsql/run-adminapi-migrations.sh /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh
COPY --from=assets Application/EdFi.Ods.AdminApi/Artifacts/PgSql/Structure/Admin/ /tmp/AdminApiScripts/Admin/PgSql
COPY --from=assets Application/EdFi.Ods.AdminApi/Artifacts/PgSql/Structure/Security/ /tmp/AdminApiScripts/Security/PgSql
COPY --from=assets Docker/Settings/dev/adminapi-test-seeddata.sql /tmp/AdminApiScripts/Admin/PgSql/adminapi-test-seeddata.sql

RUN dos2unix /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh && \
    #Admin
    dos2unix /tmp/AdminApiScripts/Admin/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Admin/PgSql/* && \
    #Security
    dos2unix /tmp/AdminApiScripts/Security/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/Security/PgSql/* && \
    # Clean up
    apk del unzip dos2unix

USER postgres

EXPOSE 5432

CMD ["docker-entrypoint.sh", "postgres"]
