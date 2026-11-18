# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM postgres:16.16-alpine3.24@sha256:81e77a4833487b8132fe4b00bedd39dc3f74b8641e387f19312a48f7de218c2e

LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

WORKDIR /app

COPY --chmod=500 ./init.sh .

# Normalize line endings in case the file was edited on Windows
RUN sed -i 's/\r$//' ./init.sh

EXPOSE 5432
ENTRYPOINT ["/app/init.sh"]