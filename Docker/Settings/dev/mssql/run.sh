#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e
set +x

if [[ -z "$ADMIN_WAIT_MSSQL_HOSTS" ]]; then
  # if there are no hosts to wait then fallback to $ADMIN_MSSQL_HOST
  export ADMIN_WAIT_MSSQL_HOSTS=$ADMIN_MSSQL_HOST
fi

export ADMIN_WAIT_MSSQL_HOSTS_ARR=($ADMIN_WAIT_MSSQL_HOSTS)
for HOST in ${ADMIN_WAIT_MSSQL_HOSTS_ARR[@]}
do
  until /opt/mssql-tools18/bin/sqlcmd -C -S "$HOST" -U "$SQLSERVER_USER" -P "$SQLSERVER_PASSWORD" -d "EdFi_Admin" -Q "IF EXISTS (SELECT * FROM sys.schemas WHERE name = '$schema') SELECT 1" > /dev/null 2>&1
  do
    >&2 echo "EdFi_Admin is unavailable - sleeping"
    sleep 10
  done
done

>&2 echo "MSSQL is up - executing command"
exec $cmd

if [[ -f /ssl/server.crt ]]; then
 cp /ssl/server.crt /usr/local/share/ca-certificates/
 update-ca-certificates
fi

# Writing permissions for multitenant environment so the user can create tenants
chmod 664 /app/appsettings.json

dotnet EdFi.Ods.AdminApi.dll
