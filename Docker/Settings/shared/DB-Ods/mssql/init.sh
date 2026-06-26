#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e
set +x

# Export default values
export MSSQL_SA_PASSWORD=$SQLSERVER_PASSWORD
export ACCEPT_EULA=Y

# Validate required backup env variables
if [[ -z "$SQL_BACKUPS_FOLDER" ]]; then
  echo "ERROR: SQL_BACKUPS_FOLDER is not set. Please set it to the host path containing the .bak files." >&2
  exit 1
fi

if [[ -z "$MINIMAL_BAK_PATH" ]]; then
  echo "ERROR: MINIMAL_BAK_PATH is not set. Please set it to the container path of the minimal backup file." >&2
  exit 1
fi

if [[ -z "$POPULATED_BAK_PATH" ]]; then
  echo "ERROR: POPULATED_BAK_PATH is not set. Please set it to the container path of the populated backup file." >&2
  exit 1
fi

/app/setup-db.sh &

/opt/mssql/bin/sqlservr