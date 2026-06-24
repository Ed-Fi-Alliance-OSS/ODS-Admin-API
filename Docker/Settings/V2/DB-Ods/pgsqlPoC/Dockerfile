# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

FROM postgres:16-alpine

LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

WORKDIR /app

COPY --chmod=500 ./init.sh .

# Normalize line endings in case the file was edited on Windows
RUN sed -i 's/\r$//' ./init.sh

EXPOSE 5432
ENTRYPOINT ["/app/init.sh"]
