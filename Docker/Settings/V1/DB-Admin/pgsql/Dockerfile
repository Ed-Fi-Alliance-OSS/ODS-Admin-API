# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Base image for the Ed-Fi ODS/API 6.2 Admin database setup
FROM edfialliance/ods-api-db-admin:v2.3.6@sha256:b0c958e6628985252d2eba5fd99ec6b01b81b61b7e063f75eddfc4f236a597a2 AS base
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ARG POSTGRES_USER=postgres
ENV POSTGRES_USER=${POSTGRES_USER}
ENV POSTGRES_DB=postgres

ARG VERSION
ARG ADMIN_API_VERSION
ENV VERSION="${ADMIN_API_VERSION:-2.2.0}"

USER root
COPY run-adminapi-migrations.sh /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh
RUN apk add --no-cache dos2unix=~7 unzip=~6 openssl=~3
USER ${POSTGRES_USER}

FROM base AS setup

USER root
RUN wget -nv -O /tmp/EdFi_AdminApi_Scripts.zip "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.ODS.AdminApi/versions/${VERSION}/content" && \
    unzip /tmp/EdFi_AdminApi_Scripts.zip AdminApi/Artifacts/PgSql/Structure/Admin/* -d /tmp/AdminApiScripts/ && \
    cp -r /tmp/AdminApiScripts/AdminApi/Artifacts/PgSql/Structure/Admin/. /tmp/AdminApiScripts/PgSql/ && \
    rm -f /tmp/EdFi_AdminApi_Scripts.zip && \
    rm -r /tmp/AdminApiScripts/AdminApi &&\
    dos2unix /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh && \
    dos2unix /tmp/AdminApiScripts/PgSql/* && \
    chmod -R 700 /tmp/AdminApiScripts/PgSql && \
    chmod 700 /docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh
USER ${POSTGRES_USER}

EXPOSE 5432

CMD ["docker-entrypoint.sh", "postgres"]
