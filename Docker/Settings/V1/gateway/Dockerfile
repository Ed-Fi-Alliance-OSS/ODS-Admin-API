# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Tag nginx:1.31.6-alpine3.24
FROM nginx:1.31.6-alpine3.24@sha256:adad2ae9204d0fd7a34f40299bc838c3782be1293b10005eac4315ff5a1abf4e
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ARG ADMIN_API_VIRTUAL_NAME=adminapi
ARG ODS_VIRTUAL_NAME=api

ENV ADMIN_API_VIRTUAL_NAME=${ADMIN_API_VIRTUAL_NAME}
ENV ODS_VIRTUAL_NAME=${ODS_VIRTUAL_NAME}

COPY ./default.conf.template /etc/nginx/templates/

EXPOSE 443
