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

# Set backup path defaults (use built-in backups if not provided)
export MINIMAL_BAK_PATH=${MINIMAL_BAK_PATH:-/app/backups/EdFi_Ods_Minimal_Template.bak}
export POPULATED_BAK_PATH=${POPULATED_BAK_PATH:-/app/backups/EdFi_Ods_Populated_Template.bak}

# Log backup configuration
if [[ -n "$SQL_BACKUPS_FOLDER" ]]; then
  >&2 echo "Using custom backups from: $SQL_BACKUPS_FOLDER"
else
  >&2 echo "Using built-in backups from container image"
fi

/app/setup-db.sh &

/opt/mssql/bin/sqlservr