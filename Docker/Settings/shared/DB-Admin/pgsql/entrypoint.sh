#!/bin/sh
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e

ORIGINAL_ENTRYPOINT="/usr/local/bin/docker-entrypoint.sh"
MIGRATIONS_SCRIPT="/docker-entrypoint-initdb.d/3-run-adminapi-migrations.sh"
BASE_BOOTSTRAP_SCRIPT="/docker-entrypoint-initdb.d/1-init-database.sh"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
PGDATA_DIR="${PGDATA:-/var/lib/postgresql/data}"

if [ "${1:-postgres}" != "postgres" ]; then
  exec "$ORIGINAL_ENTRYPOINT" "$@"
fi

if [ ! -s "$PGDATA_DIR/PG_VERSION" ]; then
  "$ORIGINAL_ENTRYPOINT" "$@" &
  postgres_pid=$!

  trap 'kill "$postgres_pid" 2>/dev/null || true; wait "$postgres_pid" 2>/dev/null || true' INT TERM

  until pg_isready -h 127.0.0.1 -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d postgres > /dev/null 2>&1; do
    sleep 1
  done

  sh "$BASE_BOOTSTRAP_SCRIPT"
  sh "$MIGRATIONS_SCRIPT"

  wait "$postgres_pid"
  exit $?
fi

exec "$ORIGINAL_ENTRYPOINT" "$@"
