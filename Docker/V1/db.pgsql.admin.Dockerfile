# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM alpine:3.20@sha256:187cce89a2fdd4eaf457a0af45f5ce27672f35ce0f6df49b5b0ee835afe0561b AS assets

# Base image for the Ed-Fi ODS/API 6.2 Admin database setup
FROM edfialliance/ods-api-db-admin:v2.3.5@sha256:c9a3b50f16f60e6a126d3bd37b2cb1d52e1fb0014f88d67193fb03e4414b9d98 AS base
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ARG POSTGRES_USER=postgres
ENV POSTGRES_USER=${POSTGRES_USER}
ENV POSTGRES_DB=postgres

USER root
COPY --from=assets Application/EdFi.Ods.AdminApi/Artifacts/PgSql/Structure/Admin/ /tmp/AdminApiScripts/PgSql
COPY --from=assets Docker/Settings/V1/DB-Admin/pgsql/run-adminapi-migrations.sh /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh

RUN apk upgrade --no-cache && apk add --no-cache dos2unix=~7 unzip=~6 openssl=~3 musl=~1
USER ${POSTGRES_USER}

FROM base AS setup

USER root
RUN dos2unix /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh && \
    dos2unix /tmp/AdminApiScripts/PgSql/* && \
    chmod -R 777 /tmp/AdminApiScripts/PgSql/*
USER ${POSTGRES_USER}

EXPOSE 5432

CMD ["docker-entrypoint.sh", "postgres"]
