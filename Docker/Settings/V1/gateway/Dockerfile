# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Tag nginx:alpine3.23
FROM nginx:alpine3.23@sha256:f46cb72c7df02710e693e863a983ac42f6a9579058a59a35f1ae36c9958e4ce0
LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

ARG ADMIN_API_VIRTUAL_NAME=adminapi
ARG ODS_VIRTUAL_NAME=api

ENV ADMIN_API_VIRTUAL_NAME=${ADMIN_API_VIRTUAL_NAME}
ENV ODS_VIRTUAL_NAME=${ODS_VIRTUAL_NAME}

COPY ./default.conf.template /etc/nginx/templates/

EXPOSE 443
