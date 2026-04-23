#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.
#

set -e
set +x
MSSQL_USER=$SQLSERVER_USER
MSSQL_PASSWORD=$SQLSERVER_PASSWORD
MSSQL_SA_PASSWORD=$SA_PASSWORD

function does_edfi_admin_db_exist() {
    until /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $MSSQL_SA_PASSWORD -Q "SELECT 1" > /dev/null 2>&1
    do
        >&2 echo "MSSQL is unavailable - sleeping"
        sleep 10
    done
    local result=$(/opt/mssql-tools18/bin/sqlcmd -S "(local)" -U "sa" -P $MSSQL_SA_PASSWORD -C -Q "IF EXISTS (SELECT name FROM sys.databases WHERE name = 'EdFi_Admin') PRINT 'Database exists' ELSE PRINT 'Database does not exist'" -h -1)

    if [[ "$result" == *"Database exists"* ]]; then
        return 0
    else
        return 1
    fi
}
echo "Database initialization..."
if ! does_edfi_admin_db_exist; then
    echo "Creating base Admin and Security databases..."
    /opt/sqlpackage/sqlpackage /Action:Import /tsn:"localhost" /tdn:"EdFi_Security" /tu:"sa" /tp:"$MSSQL_SA_PASSWORD" /sf:"/tmp/EdFi_Security.bacpac" /ttsc:true
    /opt/sqlpackage/sqlpackage /Action:Import /tsn:"localhost" /tdn:"EdFi_Admin" /tu:"sa" /tp:"$MSSQL_SA_PASSWORD" /sf:"/tmp/EdFi_Admin.bacpac" /ttsc:true
    # Force sorting by name following C language sort ordering, so that the sql scripts are run
    # sequentially in the correct alphanumeric order
    echo "Running Admin Api database migration scripts..."
    
    for FILE in `LANG=C ls /tmp/AdminApiScripts/Admin/MsSql/*.sql | sort -V`
    do
        echo "Running script: ${FILE}..."
        /opt/mssql-tools18/bin/sqlcmd -S "localhost" -U "sa" -P "$MSSQL_SA_PASSWORD" -d "EdFi_Admin" -i $FILE -C
    done
    
    for FILE in `LANG=C ls /tmp/AdminApiScripts/Security/MsSql/*.sql | sort -V`
    do
        echo "Running script: ${FILE}..."
        /opt/mssql-tools18/bin/sqlcmd -S "localhost" -U "sa" -P "$MSSQL_SA_PASSWORD" -d "EdFi_Security" -i $FILE -C
    done
    
    echo "Finish Admin Api database migration scripts..."

    echo "Creating database users..."
    /opt/mssql-tools18/bin/sqlcmd -S "localhost" -C -U sa -P $MSSQL_SA_PASSWORD -Q "IF NOT EXISTS (SELECT * FROM master.sys.server_principals WHERE name = '$MSSQL_USER') BEGIN CREATE LOGIN [$MSSQL_USER] WITH PASSWORD = '$SQLSERVER_PASSWORD'; END; ALTER AUTHORIZATION ON DATABASE::EdFi_Security TO [$MSSQL_USER]; ALTER AUTHORIZATION ON DATABASE::EdFi_Admin TO [$MSSQL_USER];"
fi
echo "Database is initialized and ready to use."
