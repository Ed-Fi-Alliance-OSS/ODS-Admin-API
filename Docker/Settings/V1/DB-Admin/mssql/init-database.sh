#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e
set +x
MSSQL_USER=${SQLSERVER_USER:-edfi}
MSSQL_PASSWORD=${SQLSERVER_PASSWORD:-P@55w0rd}
MSSQL_SA_PASSWORD=${SA_PASSWORD:-P@55w0rd}

function does_edfi_admin_db_exist() {
    until /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1
    do
        >&2 echo "MSSQL is unavailable - sleeping"
        sleep 10
    done
    local result=$(/opt/mssql-tools18/bin/sqlcmd -S "(local)" -U "sa" -P "$MSSQL_SA_PASSWORD" -C -Q "IF EXISTS (SELECT name FROM sys.databases WHERE name = 'EdFi_Admin') PRINT 'Database exists' ELSE PRINT 'Database does not exist'" -h -1)

    if [[ "$result" == *"Database exists"* ]]; then
        return 0
    else
        return 1
    fi
}


echo "Database initialization..."
echo "Using SQLSERVER_USER: $MSSQL_USER"
echo "Using SA_PASSWORD environment variable for SA authentication"
if ! does_edfi_admin_db_exist; then
    echo "Creating base Admin and Security databases..."
    /opt/sqlpackage/sqlpackage /Action:Import /tsn:"localhost" /tdn:"EdFi_Security" /tu:"sa" /tp:"$MSSQL_SA_PASSWORD" /sf:"/tmp/EdFi_Security.bacpac" /ttsc:true
    if [ $? -ne 0 ]; then
        echo "ERROR: Failed to import EdFi_Security database"
        exit 1
    fi

    /opt/sqlpackage/sqlpackage /Action:Import /tsn:"localhost" /tdn:"EdFi_Admin" /tu:"sa" /tp:"$MSSQL_SA_PASSWORD" /sf:"/tmp/EdFi_Admin.bacpac" /ttsc:true
    if [ $? -ne 0 ]; then
        echo "ERROR: Failed to import EdFi_Admin database"
        exit 1
    fi

    echo "Creating database users..."
    echo "Creating SQL Server login for user: $MSSQL_USER"

    # Create the login
    /opt/mssql-tools18/bin/sqlcmd -S "localhost" -C -U sa -P "$MSSQL_SA_PASSWORD" -Q "
    IF NOT EXISTS (SELECT * FROM master.sys.server_principals WHERE name = '$MSSQL_USER')
    BEGIN
        CREATE LOGIN [$MSSQL_USER] WITH PASSWORD = '$MSSQL_PASSWORD';
        PRINT 'Login $MSSQL_USER created successfully';
    END
    ELSE
    BEGIN
        PRINT 'Login $MSSQL_USER already exists';
    END"

    if [ $? -ne 0 ]; then
        echo "ERROR: Failed to create SQL Server login for $MSSQL_USER"
        exit 1
    fi

    # Set database ownership
    /opt/mssql-tools18/bin/sqlcmd -S "localhost" -C -U sa -P "$MSSQL_SA_PASSWORD" -Q "
    ALTER AUTHORIZATION ON DATABASE::EdFi_Security TO [$MSSQL_USER];
    ALTER AUTHORIZATION ON DATABASE::EdFi_Admin TO [$MSSQL_USER];
    PRINT 'Database ownership set for $MSSQL_USER';"

    if [ $? -ne 0 ]; then
        echo "ERROR: Failed to set database ownership for $MSSQL_USER"
        exit 1
    fi

    echo "SQL Server login creation completed"
else
    echo "EdFi_Admin database already exists, skipping database creation and user setup"
fi

# Force sorting by name following C language sort ordering, so that the sql scripts are run
# sequentially in the correct alphanumeric order
echo "Running Admin Api database migration scripts..."
for FILE in `LANG=C ls /tmp/AdminApiScripts/MsSql/*.sql | sort -V`
do
    echo "Running script: ${FILE}..."
    /opt/mssql-tools18/bin/sqlcmd -S "localhost" -U "sa" -P "$MSSQL_SA_PASSWORD" -d "EdFi_Admin" -i $FILE -C
done
echo "Finished Admin Api database migration scripts..."

