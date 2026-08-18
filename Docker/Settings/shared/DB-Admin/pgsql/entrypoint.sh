#!/bin/sh
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e

ORIGINAL_ENTRYPOINT="/usr/local/bin/docker-entrypoint.sh"
INITDB_DIR="/docker-entrypoint-initdb.d"
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

  # Run every script under /docker-entrypoint-initdb.d/, sorted lexicographically,
  # the same way the vanilla Postgres entrypoint scans that directory on first init.
  # This image's own init scripts (1-init-database.sh, 3-run-adminapi-migrations.sh)
  # live here under their numeric prefixes so any script a downstream consumer mounts
  # alongside them (e.g. Ed-Fi-AdminApp's 2-bootstrap.sh, which seeds tenant
  # OdsInstances rows) sorts in between them and runs in the intended order.
  # Mirror the vanilla entrypoint's own handling of *.sh files: run it directly
  # (honoring its shebang, e.g. #!/bin/bash) if it's executable, otherwise source
  # it into this shell, instead of forcing every script through "sh".
  for f in "$INITDB_DIR"/*.sh; do
    if [ -f "$f" ]; then
      if [ -x "$f" ]; then
        "$f"
      else
        . "$f"
      fi
    fi
  done

  wait "$postgres_pid"
  exit $?
fi

exec "$ORIGINAL_ENTRYPOINT" "$@"
