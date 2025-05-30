#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e
set +x

if [[ -z "$MSSQL_PORT" ]]; then
  export MSSQL_PORT=1433
fi

# Force sorting by name following C language sort ordering, so that the sql scripts are run
# sequentially in the correct alphanumeric order
echo "Running Admin Api database migration scripts..."

for FILE in `LANG=C ls /tmp/AdminApiScripts/Admin/MsSql/*.sql | sort -V`
do
    /opt/mssql-tools18/bin/sqlcmd -S localhost,$MSSQL_PORT -U "$MSSQL_USER" -P "$MSSQL_PASSWORD"  -d "EdFi_Admin" -i  --file $FILE 1> /dev/null
done

for FILE in `LANG=C ls /tmp/AdminApiScripts/Security/MsSql/*.sql | sort -V`
do
    /opt/mssql-tools18/bin/sqlcmd -S localhost,$MSSQL_PORT -U "$MSSQL_USER" -P "$MSSQL_PASSWORD"  -d "EdFi_Security" -i  --file $FILE 1> /dev/null
done

