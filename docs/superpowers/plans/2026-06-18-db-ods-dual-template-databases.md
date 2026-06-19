# db-ods Dual Template Databases Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the `db-ods` container to restore two template databases (`Ods_Minimal_Template` and `Ods_Populated_Template`) instead of the single `EdFi_Ods` database it restores today.

**Architecture:** The Dockerfile (V2 variant) downloads four `.bak` files at build time — both plain and TPDM variants of minimal and populated templates — so `TPDM_ENABLED` can remain a runtime switch with no rebuild required. At startup, `setup-db.sh` picks the correct pair based on `TPDM_ENABLED` and restores each database using a `restore_database` helper that discovers logical file names dynamically via `RESTORE FILELISTONLY`, making it version-agnostic. The compose file's build context is updated from `shared/` to `V2/` to isolate this change.

**Tech Stack:** Bash, Docker (multi-stage build), SQL Server 2022 (`sqlcmd`), NuGet package feed (Azure Artifacts)

---

## File Map

| Action | File |
|--------|------|
| Modify | `Docker/Settings/V2/DB-Ods/mssql/Dockerfile` |
| Modify | `Docker/Settings/V2/DB-Ods/mssql/setup-db.sh` |
| Modify | `Docker/V2/Compose/mssql/SingleTenant/compose-build-ods.yml` |

---

## Task 1: Update Dockerfile to download all four backup variants

**Files:**
- Modify: `Docker/Settings/V2/DB-Ods/mssql/Dockerfile`

**Context:** The current Dockerfile has two download `RUN` blocks. The second one removes `unzip` with `apt-get --purge autoremove unzip -y` — so populated downloads must be inserted *before* that cleanup. We restructure: each of the four downloads gets its own `RUN` layer (good for build cache), and only the fourth and final one does the cleanup.

- [ ] **Step 1: Note the `.bak` filenames inside the populated ZIPs**

  Before editing, confirm the actual filenames inside the populated NuGet ZIPs. The minimal template ZIPs contain `EdFi.Ods.Minimal.Template.bak` and `EdFi.Ods.Minimal.Template.TPDM.Core.bak`. The populated packages are expected to contain `EdFi.Ods.Populated.Template.bak` and `EdFi.Ods.Populated.Template.TPDM.Core.bak` — verify by downloading and listing one ZIP during a test build:

  ```bash
  # Run from any Linux/WSL shell to verify (replace version values as needed)
  curl -sL "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Populated.Template.Standard.5.2.0/versions/7.3.478/content" -o /tmp/pop.zip
  unzip -l /tmp/pop.zip | grep '\.bak'
  ```

  If the filename differs from `EdFi.Ods.Populated.Template.bak`, update the `unzip -p` calls in Steps 3–4 below accordingly.

- [ ] **Step 2: Replace `Docker/Settings/V2/DB-Ods/mssql/Dockerfile` with the following content**

  ```dockerfile
  # SPDX-License-Identifier: Apache-2.0
  # Licensed to the Ed-Fi Alliance under one or more agreements.
  # The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  # See the LICENSE and NOTICES files in the project root for more information.

  # Base image with additional packages
  FROM mcr.microsoft.com/mssql/server:2022-CU15-ubuntu-22.04@sha256:804e527187b16fe05c4321b58d76850966cbaf73e31942416ce0bbefe1b0eb63 AS base
  USER root

  RUN ACCEPT_EULA=Y apt-get update -y && \
      ACCEPT_EULA=Y apt-get upgrade -y && \
      ACCEPT_EULA=Y apt-get -y install --no-install-recommends apt-utils -y unzip=6.0-26ubuntu3 dos2unix=7.4.2-2 && \
      rm -rf /var/lib/apt/lists/* && \
      echo 'debconf debconf/frontend select Noninteractive' | debconf-set-selections && \
      # These steps are needed in order to mount the database to a host volume
      mkdir -p /var/opt/mssql/data /var/opt/mssql/log && \
      chown -R mssql: /var/opt/mssql/data /var/opt/mssql/log
  USER mssql

  # Start a new layer so that the above layer can be cached
  FROM base AS build
  ARG ODS_VERSION
  ARG TPDM_VERSION
  ARG STANDARD_VERSION
  ARG EXTENSION_VERSION
  ARG ODS_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Minimal.Template.Standard.${STANDARD_VERSION}/versions/${ODS_VERSION}/content
  ARG TPDM_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Minimal.Template.TPDM.Core.${EXTENSION_VERSION}.Standard.${STANDARD_VERSION}/versions/${TPDM_VERSION}/content
  ARG POPULATED_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Populated.Template.Standard.${STANDARD_VERSION}/versions/${ODS_VERSION}/content
  ARG TPDM_POPULATED_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Populated.Template.TPDM.Core.${EXTENSION_VERSION}.Standard.${STANDARD_VERSION}/versions/${TPDM_VERSION}/content

  LABEL maintainer="Ed-Fi Alliance, LLC and Contributors <techsupport@ed-fi.org>"

  # These variables can be overwritten at runtime
  ENV MSSQL_PID=Express

  USER root
  WORKDIR /app

  # Download and extract Minimal Template for core Ed-Fi Data Model
  RUN umask 0077 && \
      mkdir backups && \
      wget -q -O ./backups/OdsMinimalDatabase.zip ${ODS_URL} && \
      unzip -p ./backups/OdsMinimalDatabase.zip EdFi.Ods.Minimal.Template.bak > ./backups/EdFi_Ods_Minimal_Template.bak

  # Download and extract Minimal Template for Teacher Prep Data Model (TPDM)
  RUN wget -q -O ./backups/TPDMOdsMinimalDatabase.zip ${TPDM_URL} && \
      unzip -p ./backups/TPDMOdsMinimalDatabase.zip EdFi.Ods.Minimal.Template.TPDM.Core.bak > ./backups/EdFi_Ods_Minimal_Template_TPDM_Core.bak

  # Download and extract Populated Template for core Ed-Fi Data Model
  RUN wget -q -O ./backups/OdsPopulatedDatabase.zip ${POPULATED_URL} && \
      unzip -p ./backups/OdsPopulatedDatabase.zip EdFi.Ods.Populated.Template.bak > ./backups/EdFi_Ods_Populated_Template.bak

  # Download and extract Populated Template for Teacher Prep Data Model (TPDM)
  RUN wget -q -O ./backups/TPDMOdsPopulatedDatabase.zip ${TPDM_POPULATED_URL} && \
      unzip -p ./backups/TPDMOdsPopulatedDatabase.zip EdFi.Ods.Populated.Template.TPDM.Core.bak > ./backups/EdFi_Ods_Populated_Template_TPDM_Core.bak && \
      rm -f ./backups/*.zip && \
      apt-get --purge autoremove unzip -y

  COPY --chmod=500 ./*.sh .

  RUN dos2unix ./*.sh && \
      chown -R mssql . && \
      apt-get --purge autoremove dos2unix -y

  EXPOSE 1433
  USER mssql
  ENTRYPOINT ["/app/init.sh"]
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add Docker/Settings/V2/DB-Ods/mssql/Dockerfile
  git commit -m "feat: download populated template backups in V2 db-ods Dockerfile

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Task 2: Update setup-db.sh to restore both template databases

**Files:**
- Modify: `Docker/Settings/V2/DB-Ods/mssql/setup-db.sh`

**Context:** The current script restores one backup as `[EdFi_Ods]` with hard-coded logical file names (`EdFi_Ods_Populated_Template_Test`). The new script introduces a `restore_database` function that uses `RESTORE FILELISTONLY` to discover logical file names dynamically, then calls it twice — once for `Ods_Minimal_Template` and once for `Ods_Populated_Template`. Each restore is guarded by checking whether the `.mdf` file already exists on the mounted volume (idempotency on container restart).

- [ ] **Step 1: Replace `Docker/Settings/V2/DB-Ods/mssql/setup-db.sh` with the following content**

  ```bash
  #!/bin/bash
  # SPDX-License-Identifier: Apache-2.0
  # Licensed to the Ed-Fi Alliance under one or more agreements.
  # The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  # See the LICENSE and NOTICES files in the project root for more information.

  set -e
  set +x

  STATUS_SA=1
  STATUS_USER=1
  while [[ $STATUS_SA -ne 0 && $STATUS_USER -ne 0 ]]; do
    >&2 echo "Waiting for server to be online... "
    STATUS_SA=$(/opt/mssql-tools18/bin/sqlcmd -C -W -h -1 -U sa -P "${SQLSERVER_PASSWORD}" -Q "SET NOCOUNT ON; SELECT SUM(state) FROM sys.databases" > /dev/null 2>&1 || echo 1)

    STATUS_USER=$(/opt/mssql-tools18/bin/sqlcmd -C -W -h -1 -U ${SQLSERVER_USER} -P "${SQLSERVER_PASSWORD}" -Q "SET NOCOUNT ON; SELECT SUM(state) FROM sys.databases" > /dev/null 2>&1 || echo 1)

    sleep 10
  done

  echo "Configuring user..."
  # If connection fails, it means we already have configured logins, so we can redirect the error to /dev/null
  /opt/mssql-tools18/bin/sqlcmd -C -U sa -P "${SQLSERVER_PASSWORD}" -Q "
      CREATE LOGIN ${SQLSERVER_USER} WITH PASSWORD = '${SQLSERVER_PASSWORD}';
      CREATE USER ${SQLSERVER_USER} FOR LOGIN ${SQLSERVER_USER};
      ALTER SERVER ROLE [sysadmin] ADD MEMBER ${SQLSERVER_USER};
      ALTER LOGIN [SA] DISABLE;" > /dev/null 2>&1

  export MINIMAL_BACKUP=EdFi_Ods_Minimal_Template.bak
  export POPULATED_BACKUP=EdFi_Ods_Populated_Template.bak

  if [[ "$TPDM_ENABLED" = true ]]; then
    export MINIMAL_BACKUP=EdFi_Ods_Minimal_Template_TPDM_Core.bak
    export POPULATED_BACKUP=EdFi_Ods_Populated_Template_TPDM_Core.bak
  fi

  restore_database() {
    local db_name=$1
    local backup_file=$2
    local target_mdf=$3
    local target_ldf=$4

    echo "Getting logical file names from ${backup_file}..."
    local file_list
    file_list=$(/opt/mssql-tools18/bin/sqlcmd -C -U "${SQLSERVER_USER}" -P "${SQLSERVER_PASSWORD}" \
      -h -1 -W -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'/app/backups/${backup_file}'" 2>/dev/null)

    local logical_data logical_log
    logical_data=$(echo "${file_list}" | grep -v '^ *$' | sed -n '1p' | awk '{print $1}')
    logical_log=$(echo "${file_list}" | grep -v '^ *$' | sed -n '2p' | awk '{print $1}')

    echo "Restoring [${db_name}] (logical files: ${logical_data}, ${logical_log})..."
    /opt/mssql-tools18/bin/sqlcmd -C -U "${SQLSERVER_USER}" -P "${SQLSERVER_PASSWORD}" -Q "
      RESTORE DATABASE [${db_name}] FROM DISK = N'/app/backups/${backup_file}'
      WITH MOVE '${logical_data}' TO '${target_mdf}',
           MOVE '${logical_log}' TO '${target_ldf}';"
  }

  if [[ ! -f "/var/opt/mssql/data/Ods_Minimal_Template.mdf" ]]; then
    echo "Loading Ods_Minimal_Template database from backup..."
    restore_database "Ods_Minimal_Template" "${MINIMAL_BACKUP}" \
      "/var/opt/mssql/data/Ods_Minimal_Template.mdf" \
      "/var/opt/mssql/log/Ods_Minimal_Template_log.ldf"
  fi

  if [[ ! -f "/var/opt/mssql/data/Ods_Populated_Template.mdf" ]]; then
    echo "Loading Ods_Populated_Template database from backup..."
    restore_database "Ods_Populated_Template" "${POPULATED_BACKUP}" \
      "/var/opt/mssql/data/Ods_Populated_Template.mdf" \
      "/var/opt/mssql/log/Ods_Populated_Template_log.ldf"
  fi
  ```

  > **Note on `RESTORE FILELISTONLY` parsing:** `SET NOCOUNT ON` suppresses the `(N rows affected)` footer. The `-h -1` flag suppresses column headers. After filtering blank lines, line 1 is the data file row and line 2 is the log file row; `awk '{print $1}'` extracts the `LogicalName` column. If a backup unexpectedly has more than one data file, the `logical_data` variable will hold the first one — sufficient for standard Ed-Fi ODS templates.

- [ ] **Step 2: Commit**

  ```bash
  git add Docker/Settings/V2/DB-Ods/mssql/setup-db.sh
  git commit -m "feat: restore Ods_Minimal_Template and Ods_Populated_Template in V2 db-ods

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Task 3: Update compose to use V2 build context and verify

**Files:**
- Modify: `Docker/V2/Compose/mssql/SingleTenant/compose-build-ods.yml`

- [ ] **Step 1: Change the build context in `compose-build-ods.yml`**

  Find this block (lines 8–15):

  ```yaml
      build:
        context: ../../../../Settings/shared/DB-Ods/mssql/
        dockerfile: Dockerfile
  ```

  Replace with:

  ```yaml
      build:
        context: ../../../../Settings/V2/DB-Ods/mssql/
        dockerfile: Dockerfile
  ```

  No other changes needed — the `args` block (ODS_VERSION, TPDM_VERSION, STANDARD_VERSION, EXTENSION_VERSION) remains identical.

- [ ] **Step 2: Verify the relative path resolves correctly**

  The compose file lives at `Docker/V2/Compose/mssql/SingleTenant/`. Four `../` up = `Docker/`. So:
  - `../../../../Settings/V2/DB-Ods/mssql/` → `Docker/Settings/V2/DB-Ods/mssql/` ✓

  Confirm the folder exists:
  ```bash
  ls Docker/Settings/V2/DB-Ods/mssql/
  # Expected: Dockerfile  init.sh  setup-db.sh
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add Docker/V2/Compose/mssql/SingleTenant/compose-build-ods.yml
  git commit -m "feat: point db-ods compose build context to V2 settings folder

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Task 4: Manual end-to-end verification

> This is a verification-only task — no code changes. Run from the directory containing `compose-build-ods.yml`.

- [ ] **Step 1: Build the image**

  ```bash
  cd Docker/V2/Compose/mssql/SingleTenant
  docker compose -f compose-build-ods.yml build db-ods
  ```

  Expected: build completes successfully with four `.bak` files downloaded. If the populated ZIP does not contain `EdFi.Ods.Populated.Template.bak`, the `unzip -p` command will exit non-zero and the build will fail with an error like `caution: filename not matched`. In that case, run `unzip -l` on the downloaded ZIP (see Task 1, Step 1) and update the filename in the Dockerfile.

- [ ] **Step 2: Start the container**

  ```bash
  docker compose -f compose-build-ods.yml up db-ods
  ```

  Watch logs. Expected sequence:
  1. `Waiting for server to be online...`
  2. `Configuring user...`
  3. `Loading Ods_Minimal_Template database from backup...` (with logical file names echoed)
  4. `Loading Ods_Populated_Template database from backup...` (with logical file names echoed)

- [ ] **Step 3: Verify both databases exist**

  ```bash
  docker exec ed-fi-db-ods /opt/mssql-tools18/bin/sqlcmd \
    -C -U edfi -P "P@55w0rd" \
    -Q "SELECT name FROM sys.databases WHERE name IN ('Ods_Minimal_Template','Ods_Populated_Template') ORDER BY name"
  ```

  Expected output:
  ```
  name
  -------------------------
  Ods_Minimal_Template
  Ods_Populated_Template

  (2 rows affected)
  ```

- [ ] **Step 4: Verify idempotency (restart)**

  ```bash
  docker compose -f compose-build-ods.yml restart db-ods
  docker logs ed-fi-db-ods --tail 30
  ```

  Expected: no `Loading ... database from backup...` lines appear — both `.mdf` guards prevent re-restore.

- [ ] **Step 5: Verify TPDM mode**

  Set `TPDM_ENABLED=true` in your `.env` file (or export it), then:

  ```bash
  docker compose -f compose-build-ods.yml down -v
  docker compose -f compose-build-ods.yml up db-ods
  docker exec ed-fi-db-ods /opt/mssql-tools18/bin/sqlcmd \
    -C -U edfi -P "P@55w0rd" \
    -Q "SELECT name FROM sys.databases WHERE name IN ('Ods_Minimal_Template','Ods_Populated_Template') ORDER BY name"
  ```

  Expected: same two databases exist. Check container logs to confirm the `_TPDM_Core.bak` variants were used.
