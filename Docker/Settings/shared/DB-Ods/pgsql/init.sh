#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e

POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-P@ssw0rd}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
PGDATA="${PGDATA:-/var/lib/postgresql/data}"
MINIMAL_SQL_PATH="${MINIMAL_SQL_PATH:-}"
POPULATED_SQL_PATH="${POPULATED_SQL_PATH:-}"

if [ ! -s "$PGDATA/PG_VERSION" ]; then
    # Validate SQL files are provided and accessible before doing any work
    if [ -z "$MINIMAL_SQL_PATH" ]; then
        echo "ERROR: MINIMAL_SQL_PATH environment variable is not set."
        echo "Mount a directory containing your SQL backup files and set MINIMAL_SQL_PATH to the file path inside the container."
        exit 1
    fi
    if [ ! -f "$MINIMAL_SQL_PATH" ]; then
        echo "ERROR: Minimal SQL file not found at '$MINIMAL_SQL_PATH'."
        echo "Ensure the file is mounted into the container at the path specified by MINIMAL_SQL_PATH."
        exit 1
    fi
    if [ -z "$POPULATED_SQL_PATH" ]; then
        echo "ERROR: POPULATED_SQL_PATH environment variable is not set."
        echo "Mount a directory containing your SQL backup files and set POPULATED_SQL_PATH to the file path inside the container."
        exit 1
    fi
    if [ ! -f "$POPULATED_SQL_PATH" ]; then
        echo "ERROR: Populated SQL file not found at '$POPULATED_SQL_PATH'."
        echo "Ensure the file is mounted into the container at the path specified by POPULATED_SQL_PATH."
        exit 1
    fi

    echo "Initializing PostgreSQL data directory..."
    su-exec postgres initdb \
        --username="$POSTGRES_USER" \
        --auth-local=trust \
        --auth-host=md5 \
        -D "$PGDATA"

    # Allow connections from any host (e.g. Docker bridge, host machine)
    echo "host all all 0.0.0.0/0 md5" >> "$PGDATA/pg_hba.conf"

    echo "Starting PostgreSQL for initial setup..."
    su-exec postgres postgres -D "$PGDATA" -p "$POSTGRES_PORT" -c listen_addresses='*' &
    BG_PID=$!

    retries=0
    until su-exec postgres pg_isready -U "$POSTGRES_USER" -p "$POSTGRES_PORT" -h localhost; do
        retries=$((retries + 1))
        if [ "$retries" -ge 30 ]; then
            echo "ERROR: PostgreSQL did not become ready within 60 seconds."
            exit 1
        fi
        echo "Waiting for PostgreSQL to be ready... ($retries/30)"
        sleep 2
    done

    echo "Setting user password..."
    su-exec postgres psql -p "$POSTGRES_PORT" -U "$POSTGRES_USER" \
        -c "ALTER USER \"${POSTGRES_USER}\" WITH PASSWORD '${POSTGRES_PASSWORD}';"

    echo "Creating and restoring Ods_Minimal_Template from $MINIMAL_SQL_PATH..."
    su-exec postgres psql -p "$POSTGRES_PORT" -U "$POSTGRES_USER" \
        -c "CREATE DATABASE \"Ods_Minimal_Template\";"
    PGPASSWORD="$POSTGRES_PASSWORD" su-exec postgres psql \
        --host=localhost \
        --port="$POSTGRES_PORT" \
        --username="$POSTGRES_USER" \
        --dbname="Ods_Minimal_Template" \
        -f "$MINIMAL_SQL_PATH"
    su-exec postgres psql -p "$POSTGRES_PORT" -U "$POSTGRES_USER" \
        -c "UPDATE pg_database SET datistemplate = true WHERE datname = 'Ods_Minimal_Template';"

    echo "Creating and restoring Ods_Populated_Template from $POPULATED_SQL_PATH..."
    su-exec postgres psql -p "$POSTGRES_PORT" -U "$POSTGRES_USER" \
        -c "CREATE DATABASE \"Ods_Populated_Template\";"
    PGPASSWORD="$POSTGRES_PASSWORD" su-exec postgres psql \
        --host=localhost \
        --port="$POSTGRES_PORT" \
        --username="$POSTGRES_USER" \
        --dbname="Ods_Populated_Template" \
        -f "$POPULATED_SQL_PATH"
    su-exec postgres psql -p "$POSTGRES_PORT" -U "$POSTGRES_USER" \
        -c "UPDATE pg_database SET datistemplate = true WHERE datname = 'Ods_Populated_Template';"

    echo "Stopping setup instance..."
    su-exec postgres pg_ctl stop -D "$PGDATA" -m fast
    wait $BG_PID 2>/dev/null || true
    echo "Initial setup complete."
fi

echo "Starting PostgreSQL in foreground..."
exec su-exec postgres postgres -D "$PGDATA" -p "$POSTGRES_PORT" -c listen_addresses='*'