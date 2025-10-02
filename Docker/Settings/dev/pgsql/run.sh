#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e
set +x

if [[ -z "$ADMIN_WAIT_POSTGRES_HOSTS" ]]; then
  # if there are no hosts to wait then fallback to $ODS_POSTGRES_HOST
  export ADMIN_WAIT_POSTGRES_HOSTS=$ADMIN_POSTGRES_HOST
fi

export ADMIN_WAIT_POSTGRES_HOSTS_ARR=($ADMIN_WAIT_POSTGRES_HOSTS)
for HOST in ${ADMIN_WAIT_POSTGRES_HOSTS_ARR[@]}
do
  until PGPASSWORD=$POSTGRES_PASSWORD \
      PGHOST=$HOST \
      PGPORT=$POSTGRES_PORT \
      PGUSER=$POSTGRES_USER \
      pg_isready > /dev/null
  do
    >&2 echo "Admin '$HOST' is unavailable - sleeping"
    sleep 10
  done
done

>&2 echo "Postgres is up - executing command"
exec $cmd

if [[ -z "$ADMIN_WAIT_POSTGRES_HOSTS" ]]; then
  # if there are no hosts to wait then fallback to $ODS_POSTGRES_HOST
  export ADMIN_WAIT_POSTGRES_HOSTS=$ADMIN_POSTGRES_HOST
fi

export ADMIN_WAIT_POSTGRES_HOSTS_ARR=($ADMIN_WAIT_POSTGRES_HOSTS)
for HOST in ${ADMIN_WAIT_POSTGRES_HOSTS_ARR[@]}
do
  until PGPASSWORD=$POSTGRES_PASSWORD \
      PGHOST=$HOST \
      PGPORT=$POSTGRES_PORT \
      PGUSER=$POSTGRES_USER \
      pg_isready > /dev/null
  do
    >&2 echo "Admin '$HOST' is unavailable - sleeping"
    sleep 10
  done
done

>&2 echo "Postgres is up - executing command"
exec $cmd

if [[ -f /ssl/server.crt ]]; then
 cp /ssl/server.crt /usr/local/share/ca-certificates/
 update-ca-certificates
fi

# Writing permissions for multitenant environment so the user can create tenants
chmod 664 /app/appsettings.json

dotnet EdFi.Ods.AdminApi.dll
