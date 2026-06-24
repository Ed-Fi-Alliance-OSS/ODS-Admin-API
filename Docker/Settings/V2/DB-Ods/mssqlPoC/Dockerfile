# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM mcr.microsoft.com/mssql/server:2022-CU15-ubuntu-22.04@sha256:804e527187b16fe05c4321b58d76850966cbaf73e31942416ce0bbefe1b0eb63

USER root

RUN ACCEPT_EULA=Y apt-get update -y && \
    ACCEPT_EULA=Y apt-get upgrade -y && \
    ACCEPT_EULA=Y apt-get -y install --no-install-recommends dos2unix=7.4.2-2 && \
    rm -rf /var/lib/apt/lists/* && \
    echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections && \
    # These steps are needed in order to mount the database to a host volume
    mkdir -p /var/opt/mssql/data /var/opt/mssql/log && \
    chown -R mssql: /var/opt/mssql/data /var/opt/mssql/log

LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ENV MSSQL_PID=Express

WORKDIR /app

COPY --chmod=500 ./init.sh .

RUN dos2unix ./init.sh && \
    chown mssql ./init.sh && \
    apt-get --purge autoremove dos2unix -y

EXPOSE 1433
USER mssql
ENTRYPOINT ["/app/init.sh"]
