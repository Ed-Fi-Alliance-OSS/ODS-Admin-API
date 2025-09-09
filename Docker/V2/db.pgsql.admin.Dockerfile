# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM edfialliance/ods-api-db-admin:7.3@sha256:0a25a039e575464de1fdf09b2db270f00910f937cb782dfa809ac3c192509233 AS base
USER root
RUN apk --upgrade --no-cache add dos2unix=~7 unzip=~6

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
