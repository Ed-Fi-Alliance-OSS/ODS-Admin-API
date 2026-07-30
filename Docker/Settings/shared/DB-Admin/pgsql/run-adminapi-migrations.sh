#!/bin/sh
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e
set +x

if [ -z "${POSTGRES_PORT}" ]; then
  export POSTGRES_PORT=5432
fi

if [ -z "${POSTGRES_USER}" ]; then
  export POSTGRES_USER=postgres
fi

create_db_if_missing() {
  db_name="$1"
  exists=""

  exists=$(psql --no-password --username "$POSTGRES_USER" --port "$POSTGRES_PORT" --dbname "postgres" -tAc "SELECT 1 FROM pg_database WHERE datname='${db_name}'")

  if [ "$exists" != "1" ]; then
    echo "Creating database ${db_name}..."
    psql --no-password --username "$POSTGRES_USER" --port "$POSTGRES_PORT" --dbname "postgres" -c "CREATE DATABASE \"${db_name}\";" 1> /dev/null
  fi
}

create_db_if_missing "EdFi_Admin"
create_db_if_missing "EdFi_Security"

# Force sorting by name following C language sort ordering, so that the sql scripts are run
# sequentially in the correct alphanumeric order
echo "Running Admin Api database migration scripts..."

for FILE in $(LANG=C ls /tmp/AdminApiScripts/Admin/PgSql/*.sql | sort)
do
  psql --no-password --username "$POSTGRES_USER" --port "$POSTGRES_PORT" --dbname "EdFi_Admin" --file "$FILE" 1> /dev/null
done

for FILE in $(LANG=C ls /tmp/AdminApiScripts/Security/PgSql/*.sql | sort)
do
  psql --no-password --username "$POSTGRES_USER" --port "$POSTGRES_PORT" --dbname "EdFi_Security" --file "$FILE" 1> /dev/null
done
