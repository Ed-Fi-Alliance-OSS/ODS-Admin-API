# E2E Workflow Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the 10 per-combo E2E GitHub Actions workflow files with 3 (one per API version), each running its DB-engine/tenancy combos sequentially on a single runner, to cut concurrent draws on the shared Docker Hub pull quota.

**Architecture:** Each new workflow file keeps one job that calls `eng/run-bruno-e2e.ps1` once per combo (compose up, health-wait, token, Bruno run already implemented there), captures failure diagnostics and tears down between combos, then uploads all combos' suffixed report files in one artifact upload. A `report` job (kept from today's workflows) loops over the per-combo JUnit files instead of handling a single one.

**Tech Stack:** GitHub Actions YAML, PowerShell 7 (`eng/run-bruno-e2e.ps1`), Docker Compose, Bruno CLI (`@usebruno/cli`).

**Spec:** `docs/design/2026-10-02-e2e-workflow-consolidation-design.md`

## Global Constraints

- Triggers: `push: branches: [main]`, `schedule: "0 5 * * 1"`, `workflow_dispatch`, `pull_request` — v1 keeps `branches: [main, "*-hotfix"]` for `pull_request`; v2/v3 use `branches: [main]`.
- `env` block on every new workflow keeps `JIRA_ACCESS_TOKEN`, `ADMIN_API_VERSION`, `PROJECT_ID: "13401"`, `CYCLE_NAME: "Automation Cycle"`, `TASK_NAME: "API Automation Task"`, `FOLDER_NAME: "API Automation Run"` (values copied verbatim from the existing files being replaced).
- `permissions: read-all` at the workflow level, same as today.
- Any combo using the `pgsql` DB engine requires `docker/login-action@dbcb813823bdd20940b903addbd779551569679f` (v4.6.0) with `DOCKER_USERNAME`/`DOCKER_HUB_TOKEN`; `mssql`-only combos do not.
- `eng/run-bruno-e2e.ps1`'s existing parameters, defaults, and behavior for `-ApiVersion`/`-TenantMode`/`-DbEngine`/`-SkipDockerBuild`/`-TearDown`/`-UseGlobalBru`/`-BrunoFilter` must not change — only a new `-ReportSuffix` parameter is added.
- `actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10` (v6.0.3), `actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e` (v6.4.0), `actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` (v7.0.1), `actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c` (v8.0.1) — pin to these exact SHAs, matching every existing E2E workflow.
- No changes to `Docker/V{1,2,3}/Compose/**/*.yml` or the Bruno test collections.

## Review Focus

- **One combo's test failure must not skip the remaining combos in the same workflow run**, and the job must still end in overall failure if any combo failed. Naive sequential steps without `continue-on-error`/`if: always()` would either halt after the first failure or silently report success — covered by the `continue-on-error` + outcome-check pattern in Tasks 2–4.
- **Leftover Docker state between combos** (networks, bound ports, volumes) must not make the next combo's `up -d` fail or bind to stale state — covered by the explicit `down -v --remove-orphans` step between combos in Tasks 2–4.
- **Sequential combos in one job must not overwrite each other's report files** before the single end-of-job artifact upload — covered by the `-ReportSuffix` parameter (Task 1) and per-combo suffixed upload globs (Tasks 2–4).
- **A combo's docker-logs capture must only run for that specific combo's failure**, not for any earlier or later combo's failure in the same job — covered by keying each capture step's `if:` condition to that combo's own step `id`/`outcome`, not the job-wide `failure()`.
- **The JIRA `report` job must still process every combo's JUnit file**, not just the first one found — covered by replacing the single-file `Get-ChildItem | Select-Object -First 1` with a `ForEach-Object` loop in Tasks 2–4, preserving the existing (and already-incomplete — see Out of Scope) per-file logic unchanged.

---

## File Structure

- Modify: `eng/run-bruno-e2e.ps1` — add `-ReportSuffix` parameter; after the Bruno run, copy `results.html`/`report.xml` to suffixed filenames when the parameter is set.
- Create: `.github/workflows/api-v1-e2e.yml` — replaces `api-v1-e2e-mssql.yml` + `api-v1-e2e-pgsql.yml` (2 combos: mssql, pgsql; single-tenant only).
- Create: `.github/workflows/api-v2-e2e.yml` — replaces the 4 `api-v2-e2e-*.yml` files (mssql/pgsql × single/multi-tenant).
- Create: `.github/workflows/api-v3-e2e.yml` — replaces the 4 `api-v3-e2e-*.yml` files (mssql/pgsql × single/multi-tenant).
- Delete: all 10 files listed above once their replacement is validated.

---

### Task 1: Add `-ReportSuffix` to `eng/run-bruno-e2e.ps1`

**Files:**
- Modify: `eng/run-bruno-e2e.ps1:68-82` (param block), `eng/run-bruno-e2e.ps1:400-417` (finally block)

**Interfaces:**
- Produces: `run-bruno-e2e.ps1 -ReportSuffix <string>` — when non-empty, copies `$brunoDir/results.html` → `$brunoDir/results-<suffix>.html` and `$brunoDir/report.xml` → `$brunoDir/report-<suffix>.xml` after the Bruno run, regardless of pass/fail. When omitted (default `""`), behavior is unchanged from today.

- [ ] **Step 1: Add the parameter**

Modify the `param()` block at `eng/run-bruno-e2e.ps1:68-82`:

```powershell
param(
    [ValidateSet("1", "2", "3")]
    [string]$ApiVersion = "3",

    [ValidateSet("singletenant", "multitenant")]
    [string]$TenantMode = "singletenant",

    [ValidateSet("pgsql", "mssql")]
    [string]$DbEngine = "pgsql",

    [switch]$SkipDockerBuild,
    [switch]$TearDown,
    [switch]$UseGlobalBru,
    [string[]]$BrunoFilter = @(),

    [string]$ReportSuffix = ""
)
```

Also add a `.PARAMETER ReportSuffix` entry to the comment-based help block, right after the existing `.PARAMETER BrunoFilter` entry (around line 50):

```
.PARAMETER ReportSuffix
    When set, copies results.html/report.xml to results-<suffix>.html and
    report-<suffix>.xml after the run, so multiple combos invoked
    sequentially (e.g. by CI) don't overwrite each other's reports.
```

- [ ] **Step 2: Add the copy logic to the `finally` block**

Modify `eng/run-bruno-e2e.ps1:400-417`, inserting the suffix-copy block between the existing `$env:NODE_TLS_REJECT_UNAUTHORIZED = $null` line and the `# 7. Optional tear-down` comment:

```powershell
} finally {
    Pop-Location
    $env:NODE_TLS_REJECT_UNAUTHORIZED = $null

    if ($ReportSuffix) {
        $htmlSrc = Join-Path $brunoDir "results.html"
        $xmlSrc  = Join-Path $brunoDir "report.xml"
        if (Test-Path $htmlSrc) {
            Copy-Item -Path $htmlSrc -Destination (Join-Path $brunoDir "results-$ReportSuffix.html") -Force
        }
        if (Test-Path $xmlSrc) {
            Copy-Item -Path $xmlSrc -Destination (Join-Path $brunoDir "report-$ReportSuffix.xml") -Force
        }
    }

    # ---------------------------------------------------------------------------
    # 7. Optional tear-down — runs whether tests passed or failed
    # ---------------------------------------------------------------------------
    if ($TearDown) {
        Write-Host ""
        Write-Host "🧹 Tearing down containers..." -ForegroundColor Yellow
        docker compose -f $composeFile --env-file $envFile down -v
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠️  docker compose down exited with $LASTEXITCODE" -ForegroundColor Yellow
        } else {
            Write-Host "✅ Containers removed" -ForegroundColor Green
        }
    }
}
```

- [ ] **Step 3: Verify locally against a live stack**

This script has no existing unit test harness (it's an integration helper with side effects — Docker, network calls — not a pure function), so verification is an integration check against a real local run, matching how the script is already used today:

```powershell
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode singletenant -DbEngine pgsql -TearDown -ReportSuffix pgsql-singletenant
```

Expected: run completes (pass or fail), and both
`Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/results-pgsql-singletenant.html` and
`.../report-pgsql-singletenant.xml` exist alongside the original `results.html`/`report.xml`, with identical content to the originals.

- [ ] **Step 4: Commit**

```bash
git add eng/run-bruno-e2e.ps1
git commit -m "feat: add -ReportSuffix to run-bruno-e2e.ps1 for consolidated E2E workflows"
```

---

### Task 2: Create `api-v1-e2e.yml`, delete the 2 old v1 files

**Files:**
- Create: `.github/workflows/api-v1-e2e.yml`
- Delete: `.github/workflows/api-v1-e2e-mssql.yml`, `.github/workflows/api-v1-e2e-pgsql.yml`

**Interfaces:**
- Consumes: `eng/run-bruno-e2e.ps1 -ApiVersion 1 -DbEngine <mssql|pgsql> -ReportSuffix <mssql|pgsql>` from Task 1.
- Produces: artifacts `test-html-results` (glob `results-*.html`), `test-results` (glob `report-*.xml`), `docker-logs` (only on failure) — consumed by this workflow's own `report` job.

- [ ] **Step 1: Write `api-v1-e2e.yml`**

```yaml
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

name: Admin API V1 E2E Tests + ODS 6

on:
  push:
    branches: [main]
  schedule:
    - cron: "0 5 * * 1"
  workflow_dispatch:
  pull_request:
    branches:
      - main
      - "*-hotfix"

env:
  JIRA_ACCESS_TOKEN: ${{ secrets.JIRA_ACCESS_TOKEN }}
  ADMIN_API_VERSION: "1.2"
  PROJECT_ID: "13401"
  CYCLE_NAME: "Automation Cycle"
  TASK_NAME: "API Automation Task"
  FOLDER_NAME: "API Automation Run"
  RESULTS_FILE: "test-results"

permissions: read-all

jobs:
  run-e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Setup node
        uses: actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e # v6.4.0
        with:
          node-version: "20"

      - name: Run mssql combo
        id: combo-mssql
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 1 -DbEngine mssql -ReportSuffix mssql

      - name: Get Docker logs (mssql)
        if: steps.combo-mssql.outcome == 'failure'
        run: |
          mkdir -p docker-logs/mssql
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/mssql/adminapi.log
          docker logs ed-fi-db-admin-adminapi > docker-logs/mssql/ed-fi-db-admin.log
          docker logs ed-fi-gateway-adminapi > docker-logs/mssql/ed-fi-gateway.log

      - name: Tear down mssql combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V1/Compose/mssql/compose-build-dev.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi/E2E Tests/V1/gh-action-setup/.automation.env' \
            down -v --remove-orphans

      - name: Run pgsql combo
        id: combo-pgsql
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 1 -DbEngine pgsql -ReportSuffix pgsql

      - name: Get Docker logs (pgsql)
        if: steps.combo-pgsql.outcome == 'failure'
        run: |
          mkdir -p docker-logs/pgsql
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/pgsql/adminapi.log
          docker logs ed-fi-db-admin-adminapi > docker-logs/pgsql/ed-fi-db-admin.log
          docker logs ed-fi-gateway-adminapi > docker-logs/pgsql/ed-fi-gateway.log

      - name: Tear down pgsql combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V1/Compose/pgsql/compose-build-dev.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi/E2E Tests/V1/gh-action-setup/.automation.env' \
            down -v --remove-orphans

      - name: Fail job if any combo failed
        if: always()
        run: |
          if [ "${{ steps.combo-mssql.outcome }}" = "failure" ] || [ "${{ steps.combo-pgsql.outcome }}" = "failure" ]; then
            echo "One or more E2E combos failed"
            exit 1
          fi

      - name: Upload Docker logs
        if: steps.combo-mssql.outcome == 'failure' || steps.combo-pgsql.outcome == 'failure'
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: docker-logs
          path: docker-logs/
          retention-days: 10

      - name: Upload Html results
        if: always()
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: test-html-results
          path: |
            Application/EdFi.Ods.AdminApi/E2E Tests/V1/Bruno Admin API E2E refactor/results-*.html
          retention-days: 10

      - name: Upload xml results
        if: always()
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: test-results
          path: |
            Application/EdFi.Ods.AdminApi/E2E Tests/V1/Bruno Admin API E2E refactor/report-*.xml
          retention-days: 10

  report:
    defaults:
      run:
        shell: pwsh
    needs:
      - run-e2e-tests

    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Download artifacts
        uses: actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c # v8.0.1

      - name: Upload results to JIRA report
        if: success() || failure()
        run: |
          Get-ChildItem -Path 'test-results' -Recurse -Filter 'report-*.xml' | ForEach-Object {
            $filePath = $_.FullName
            [xml]$xmlContent = Get-Content -Path $filePath
            $failed = [int]($xmlContent.testsuites.testsuite | Measure-Object -Property failures -Sum | Select-Object -ExpandProperty Sum)
            $passed = [int]($xmlContent.testsuites.testsuite | Measure-Object -Property tests -Sum | Select-Object -ExpandProperty Sum) - $failed
            if ( $passed -lt 1 ) {
              Write-Output "No tests recorded in report ($($_.Name)). Uploading manually."
            }
          }
```

- [ ] **Step 2: Validate in a fork**

Push this branch to a fork, trigger it via:

```bash
gh workflow run api-v1-e2e.yml --ref <branch> --repo <your-fork>/ODS-Admin-API
gh run watch --repo <your-fork>/ODS-Admin-API
```

Expected: both combos run, `test-html-results`/`test-results` artifacts each contain 2 suffixed files (`results-mssql.html`, `results-pgsql.html` / `report-mssql.xml`, `report-pgsql.xml`), and the `report` job's log shows two `ForEach-Object` iterations.

- [ ] **Step 3: Force one combo to fail, confirm isolation**

Temporarily edit the forked branch's `.automation.env` to an invalid DB connection value (or similar), push, re-run. Expected: the mssql combo (or whichever you broke) shows `combo-mssql.outcome == failure`, its docker-logs subfolder is captured and uploaded, the pgsql combo still runs to completion afterward, and the job's overall conclusion is "failure". Revert the temporary edit before continuing.

- [ ] **Step 4: Delete the old v1 workflow files and commit**

```bash
git rm '.github/workflows/api-v1-e2e-mssql.yml' '.github/workflows/api-v1-e2e-pgsql.yml'
git add '.github/workflows/api-v1-e2e.yml'
git commit -m "feat: consolidate v1 E2E workflows into a single sequential-combo workflow"
```

---

### Task 3: Create `api-v2-e2e.yml`, delete the 4 old v2 files

**Files:**
- Create: `.github/workflows/api-v2-e2e.yml`
- Delete: `.github/workflows/api-v2-e2e-mssql-multitenant.yml`, `.github/workflows/api-v2-e2e-mssql-singletenant.yml`, `.github/workflows/api-v2-e2e-pgsql-multitenant.yml`, `.github/workflows/api-v2-e2e-pgsql-singletenant.yml`

**Interfaces:**
- Consumes: `eng/run-bruno-e2e.ps1 -ApiVersion 2 -TenantMode <singletenant|multitenant> -DbEngine <mssql|pgsql> -ReportSuffix <db>-<tenant>` from Task 1.
- Produces: artifacts `test-html-results` (glob `results-*.html`), `test-xml-results` (glob `report-*.xml`), `docker-logs` (only on failure).

- [ ] **Step 1: Write `api-v2-e2e.yml`**

```yaml
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

name: Admin API V2 E2E Tests + ODS 7

on:
  push:
    branches: [main]
  schedule:
    - cron: "0 5 * * 1"
  workflow_dispatch:
  pull_request:
    branches: [main]

env:
  DOCKER_USERNAME: ${{ vars.DOCKER_USERNAME }}
  DOCKER_HUB_TOKEN: ${{ secrets.DOCKER_HUB_TOKEN }}
  JIRA_ACCESS_TOKEN: ${{ secrets.JIRA_ACCESS_TOKEN }}
  ADMIN_API_VERSION: "2.2.0"
  PROJECT_ID: "13401"
  CYCLE_NAME: "Automation Cycle"
  TASK_NAME: "API Automation Task"
  FOLDER_NAME: "API Automation Run"
  RESULTS_FILE: "test-xml-results"

permissions: read-all

jobs:
  run-e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Log in to Docker Hub
        uses: docker/login-action@dbcb813823bdd20940b903addbd779551569679f # v4.6.0
        with:
          username: ${{ env.DOCKER_USERNAME }}
          password: ${{ env.DOCKER_HUB_TOKEN }}

      - name: Setup node
        uses: actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e # v6.4.0
        with:
          node-version: "20"

      - name: Run mssql-singletenant combo
        id: combo-mssql-single
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -TenantMode singletenant -DbEngine mssql -ReportSuffix mssql-singletenant

      - name: Get Docker logs (mssql-singletenant)
        if: steps.combo-mssql-single.outcome == 'failure'
        run: |
          mkdir -p docker-logs/mssql-singletenant
          docker logs ed-fi-db-admin-adminapi > docker-logs/mssql-singletenant/ed-fi-db-admin-adminapi.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/mssql-singletenant/adminapi.log

      - name: Tear down mssql-singletenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V2/Compose/mssql/SingleTenant/compose-build-dev.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi/E2E Tests/V2/gh-action-setup/.automation_mssql.env' \
            down -v --remove-orphans

      - name: Run mssql-multitenant combo
        id: combo-mssql-multi
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -TenantMode multitenant -DbEngine mssql -ReportSuffix mssql-multitenant

      - name: Get Docker logs (mssql-multitenant)
        if: steps.combo-mssql-multi.outcome == 'failure'
        run: |
          mkdir -p docker-logs/mssql-multitenant
          docker logs ed-fi-db-admin-adminapi-tenant1 > docker-logs/mssql-multitenant/ed-fi-db-admin-adminapi-tenant1.log
          docker logs ed-fi-db-admin-adminapi-tenant2 > docker-logs/mssql-multitenant/ed-fi-db-admin-adminapi-tenant2.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/mssql-multitenant/adminapi.log

      - name: Tear down mssql-multitenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V2/Compose/mssql/MultiTenant/compose-build-dev-multi-tenant.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi/E2E Tests/V2/gh-action-setup/.automation_mssql.env' \
            down -v --remove-orphans

      - name: Run pgsql-singletenant combo
        id: combo-pgsql-single
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -TenantMode singletenant -DbEngine pgsql -ReportSuffix pgsql-singletenant

      - name: Get Docker logs (pgsql-singletenant)
        if: steps.combo-pgsql-single.outcome == 'failure'
        run: |
          mkdir -p docker-logs/pgsql-singletenant
          docker logs ed-fi-db-admin-adminapi > docker-logs/pgsql-singletenant/ed-fi-db-admin-adminapi.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/pgsql-singletenant/adminapi.log

      - name: Tear down pgsql-singletenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V2/Compose/pgsql/SingleTenant/compose-build-dev.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi/E2E Tests/V2/gh-action-setup/.automation_pgsql.env' \
            down -v --remove-orphans

      - name: Run pgsql-multitenant combo
        id: combo-pgsql-multi
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -TenantMode multitenant -DbEngine pgsql -ReportSuffix pgsql-multitenant

      - name: Get Docker logs (pgsql-multitenant)
        if: steps.combo-pgsql-multi.outcome == 'failure'
        run: |
          mkdir -p docker-logs/pgsql-multitenant
          docker logs ed-fi-db-admin-adminapi-tenant1 > docker-logs/pgsql-multitenant/ed-fi-db-admin-adminapi-tenant1.log
          docker logs ed-fi-db-admin-adminapi-tenant2 > docker-logs/pgsql-multitenant/ed-fi-db-admin-adminapi-tenant2.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/pgsql-multitenant/adminapi.log

      - name: Tear down pgsql-multitenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V2/Compose/pgsql/MultiTenant/compose-build-dev-multi-tenant.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi/E2E Tests/V2/gh-action-setup/.automation_pgsql.env' \
            down -v --remove-orphans

      - name: Fail job if any combo failed
        if: always()
        run: |
          if [ "${{ steps.combo-mssql-single.outcome }}" = "failure" ] || \
             [ "${{ steps.combo-mssql-multi.outcome }}" = "failure" ] || \
             [ "${{ steps.combo-pgsql-single.outcome }}" = "failure" ] || \
             [ "${{ steps.combo-pgsql-multi.outcome }}" = "failure" ]; then
            echo "One or more E2E combos failed"
            exit 1
          fi

      - name: Upload Docker logs
        if: |
          steps.combo-mssql-single.outcome == 'failure' ||
          steps.combo-mssql-multi.outcome == 'failure' ||
          steps.combo-pgsql-single.outcome == 'failure' ||
          steps.combo-pgsql-multi.outcome == 'failure'
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: docker-logs
          path: docker-logs/
          retention-days: 5

      - name: Upload Html results
        if: always()
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: test-html-results
          path: |
            Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/results-*.html
          retention-days: 5

      - name: Upload xml results
        if: always()
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: test-xml-results
          path: |
            Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/report-*.xml
          retention-days: 5

  report:
    defaults:
      run:
        shell: pwsh
    needs:
      - run-e2e-tests

    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Download artifacts
        uses: actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c # v8.0.1

      - name: Upload results to JIRA report
        if: success() || failure()
        run: |
          Get-ChildItem -Path 'test-xml-results' -Recurse -Filter 'report-*.xml' | ForEach-Object {
            $filePath = $_.FullName
            [xml]$xmlContent = Get-Content -Path $filePath
            $failed = [int]($xmlContent.testsuites.testsuite | Measure-Object -Property failures -Sum | Select-Object -ExpandProperty Sum)
            $passed = [int]($xmlContent.testsuites.testsuite | Measure-Object -Property tests -Sum | Select-Object -ExpandProperty Sum) - $failed
            if ( $passed -lt 1 ) {
              Write-Output "No tests recorded in report ($($_.Name)). Uploading manually."
            }
          }
```

- [ ] **Step 2: Validate in a fork**

Same approach as Task 2 Step 2: `gh workflow run api-v2-e2e.yml --ref <branch> --repo <your-fork>/ODS-Admin-API`, confirm all 4 combos run and produce 4 suffixed report-file pairs.

- [ ] **Step 3: Force one combo to fail, confirm isolation**

Same approach as Task 2 Step 3 — break one combo's env file, confirm its docker-logs subfolder captures, the remaining 3 combos still run, and the job ends in failure. Revert the temporary edit before continuing.

- [ ] **Step 4: Delete the old v2 workflow files and commit**

```bash
git rm '.github/workflows/api-v2-e2e-mssql-multitenant.yml' \
       '.github/workflows/api-v2-e2e-mssql-singletenant.yml' \
       '.github/workflows/api-v2-e2e-pgsql-multitenant.yml' \
       '.github/workflows/api-v2-e2e-pgsql-singletenant.yml'
git add '.github/workflows/api-v2-e2e.yml'
git commit -m "feat: consolidate v2 E2E workflows into a single sequential-combo workflow"
```

---

### Task 4: Create `api-v3-e2e.yml`, delete the 4 old v3 files

**Files:**
- Create: `.github/workflows/api-v3-e2e.yml`
- Delete: `.github/workflows/api-v3-e2e-mssql-multitenant.yml`, `.github/workflows/api-v3-e2e-mssql-singletenant.yml`, `.github/workflows/api-v3-e2e-pgsql-multitenant.yml`, `.github/workflows/api-v3-e2e-pgsql-singletenant.yml`

**Interfaces:**
- Consumes: `eng/run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode <singletenant|multitenant> -DbEngine <mssql|pgsql> -ReportSuffix <db>-<tenant>` from Task 1.
- Produces: artifacts `test-html-results` (glob `results-*.html`), `test-xml-results` (glob `report-*.xml`), `docker-logs` (only on failure).

- [ ] **Step 1: Write `api-v3-e2e.yml`**

Identical structure to Task 3's `api-v2-e2e.yml`, with these substitutions throughout: `-ApiVersion 2` → `-ApiVersion 3`; `name: Admin API V2 E2E Tests + ODS 7` → `name: Admin API V3 E2E Tests + ODS 7`; every `Docker/V2/Compose/...` path → the matching `Docker/V3/Compose/...` path; every `Application/EdFi.Ods.AdminApi/E2E Tests/V2/gh-action-setup/...` path → `Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/...`; every Bruno results/report path `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/` → `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/`.

```yaml
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

name: Admin API V3 E2E Tests + ODS 7

on:
  push:
    branches: [main]
  schedule:
    - cron: "0 5 * * 1"
  workflow_dispatch:
  pull_request:
    branches: [main]

env:
  DOCKER_USERNAME: ${{ vars.DOCKER_USERNAME }}
  DOCKER_HUB_TOKEN: ${{ secrets.DOCKER_HUB_TOKEN }}
  JIRA_ACCESS_TOKEN: ${{ secrets.JIRA_ACCESS_TOKEN }}
  ADMIN_API_VERSION: "2.2.0"
  PROJECT_ID: "13401"
  CYCLE_NAME: "Automation Cycle"
  TASK_NAME: "API Automation Task"
  FOLDER_NAME: "API Automation Run"
  RESULTS_FILE: "test-xml-results"

permissions: read-all

jobs:
  run-e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Log in to Docker Hub
        uses: docker/login-action@dbcb813823bdd20940b903addbd779551569679f # v4.6.0
        with:
          username: ${{ env.DOCKER_USERNAME }}
          password: ${{ env.DOCKER_HUB_TOKEN }}

      - name: Setup node
        uses: actions/setup-node@48b55a011bda9f5d6aeb4c2d9c7362e8dae4041e # v6.4.0
        with:
          node-version: "20"

      - name: Run mssql-singletenant combo
        id: combo-mssql-single
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode singletenant -DbEngine mssql -ReportSuffix mssql-singletenant

      - name: Get Docker logs (mssql-singletenant)
        if: steps.combo-mssql-single.outcome == 'failure'
        run: |
          mkdir -p docker-logs/mssql-singletenant
          docker logs ed-fi-db-admin-adminapi > docker-logs/mssql-singletenant/ed-fi-db-admin-adminapi.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/mssql-singletenant/adminapi.log

      - name: Tear down mssql-singletenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V3/Compose/mssql/SingleTenant/compose-build-dev.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/.automation_mssql.env' \
            down -v --remove-orphans

      - name: Run mssql-multitenant combo
        id: combo-mssql-multi
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode multitenant -DbEngine mssql -ReportSuffix mssql-multitenant

      - name: Get Docker logs (mssql-multitenant)
        if: steps.combo-mssql-multi.outcome == 'failure'
        run: |
          mkdir -p docker-logs/mssql-multitenant
          docker logs ed-fi-db-admin-adminapi-tenant1 > docker-logs/mssql-multitenant/ed-fi-db-admin-adminapi-tenant1.log
          docker logs ed-fi-db-admin-adminapi-tenant2 > docker-logs/mssql-multitenant/ed-fi-db-admin-adminapi-tenant2.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/mssql-multitenant/adminapi.log

      - name: Tear down mssql-multitenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V3/Compose/mssql/MultiTenant/compose-build-dev-multi-tenant.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/.automation_mssql.env' \
            down -v --remove-orphans

      - name: Run pgsql-singletenant combo
        id: combo-pgsql-single
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode singletenant -DbEngine pgsql -ReportSuffix pgsql-singletenant

      - name: Get Docker logs (pgsql-singletenant)
        if: steps.combo-pgsql-single.outcome == 'failure'
        run: |
          mkdir -p docker-logs/pgsql-singletenant
          docker logs ed-fi-db-admin-adminapi > docker-logs/pgsql-singletenant/ed-fi-db-admin-adminapi.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/pgsql-singletenant/adminapi.log

      - name: Tear down pgsql-singletenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V3/Compose/pgsql/SingleTenant/compose-build-dev.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/.automation_pgsql.env' \
            down -v --remove-orphans

      - name: Run pgsql-multitenant combo
        id: combo-pgsql-multi
        continue-on-error: true
        shell: pwsh
        run: ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode multitenant -DbEngine pgsql -ReportSuffix pgsql-multitenant

      - name: Get Docker logs (pgsql-multitenant)
        if: steps.combo-pgsql-multi.outcome == 'failure'
        run: |
          mkdir -p docker-logs/pgsql-multitenant
          docker logs ed-fi-db-admin-adminapi-tenant1 > docker-logs/pgsql-multitenant/ed-fi-db-admin-adminapi-tenant1.log
          docker logs ed-fi-db-admin-adminapi-tenant2 > docker-logs/pgsql-multitenant/ed-fi-db-admin-adminapi-tenant2.log
          docker cp adminapi:/app/Ed-Fi-ODS-AdminAPI-Log.txt docker-logs/pgsql-multitenant/adminapi.log

      - name: Tear down pgsql-multitenant combo
        if: always()
        run: |
          docker compose \
            -f 'Docker/V3/Compose/pgsql/MultiTenant/compose-build-dev-multi-tenant.yml' \
            --env-file 'Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/.automation_pgsql.env' \
            down -v --remove-orphans

      - name: Fail job if any combo failed
        if: always()
        run: |
          if [ "${{ steps.combo-mssql-single.outcome }}" = "failure" ] || \
             [ "${{ steps.combo-mssql-multi.outcome }}" = "failure" ] || \
             [ "${{ steps.combo-pgsql-single.outcome }}" = "failure" ] || \
             [ "${{ steps.combo-pgsql-multi.outcome }}" = "failure" ]; then
            echo "One or more E2E combos failed"
            exit 1
          fi

      - name: Upload Docker logs
        if: |
          steps.combo-mssql-single.outcome == 'failure' ||
          steps.combo-mssql-multi.outcome == 'failure' ||
          steps.combo-pgsql-single.outcome == 'failure' ||
          steps.combo-pgsql-multi.outcome == 'failure'
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: docker-logs
          path: docker-logs/
          retention-days: 5

      - name: Upload Html results
        if: always()
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: test-html-results
          path: |
            Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/results-*.html
          retention-days: 5

      - name: Upload xml results
        if: always()
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: test-xml-results
          path: |
            Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/report-*.xml
          retention-days: 5

  report:
    defaults:
      run:
        shell: pwsh
    needs:
      - run-e2e-tests

    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Download artifacts
        uses: actions/download-artifact@3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c # v8.0.1

      - name: Upload results to JIRA report
        if: success() || failure()
        run: |
          Get-ChildItem -Path 'test-xml-results' -Recurse -Filter 'report-*.xml' | ForEach-Object {
            $filePath = $_.FullName
            [xml]$xmlContent = Get-Content -Path $filePath
            $failed = [int]($xmlContent.testsuites.testsuite | Measure-Object -Property failures -Sum | Select-Object -ExpandProperty Sum)
            $passed = [int]($xmlContent.testsuites.testsuite | Measure-Object -Property tests -Sum | Select-Object -ExpandProperty Sum) - $failed
            if ( $passed -lt 1 ) {
              Write-Output "No tests recorded in report ($($_.Name)). Uploading manually."
            }
          }
```

- [ ] **Step 2: Validate in a fork**

Same approach as Task 3 Step 2.

- [ ] **Step 3: Force one combo to fail, confirm isolation**

Same approach as Task 3 Step 3.

- [ ] **Step 4: Delete the old v3 workflow files and commit**

```bash
git rm '.github/workflows/api-v3-e2e-mssql-multitenant.yml' \
       '.github/workflows/api-v3-e2e-mssql-singletenant.yml' \
       '.github/workflows/api-v3-e2e-pgsql-multitenant.yml' \
       '.github/workflows/api-v3-e2e-pgsql-singletenant.yml'
git add '.github/workflows/api-v3-e2e.yml'
git commit -m "feat: consolidate v3 E2E workflows into a single sequential-combo workflow"
```

---

### Task 5: Open the PR

**Files:** none (process step)

- [ ] **Step 1: Push the branch and open a PR against `main`**

```bash
git push -u origin <branch>
gh pr create --title "Consolidate E2E workflows from 10 files to 3" --body "$(cat <<'EOF'
## Summary
- Replace 10 per-combo E2E workflows with 3 (one per API version), each
  running its combos sequentially on one runner instead of one runner per
  combo, cutting concurrent draws on the shared Docker Hub pull quota.
- Add -ReportSuffix to eng/run-bruno-e2e.ps1 so CI can call it once per
  combo without report files overwriting each other.

## Test plan
- [x] Validated api-v1-e2e.yml, api-v2-e2e.yml, api-v3-e2e.yml via
      workflow_dispatch in a fork; all combos ran and produced per-combo
      report artifacts.
- [x] Forced one combo to fail in each workflow; confirmed failure-log
      capture is scoped to that combo, remaining combos still ran, and
      the job ended in overall failure.

Spec: docs/design/2026-10-02-e2e-workflow-consolidation-design.md
Plan: docs/design/2026-10-02-e2e-workflow-consolidation-plan.md
EOF
)"
```

## Out of Scope

- The JIRA-upload portion of the `report` job's script is carried over verbatim from the existing files (just looped over multiple files) — every existing `api-v{1,2,3}-e2e-*.yml` file's `Upload results to JIRA report` step body ends after the `if ( $passed -lt 1 ) { Write-Output ... }` line with no further action (no visible call to a JIRA API within the file). This plan does not change or complete that logic; it is a pre-existing characteristic of the workflows being replaced, unrelated to this consolidation.
- `docs/http/*.http`, `Docker/V{1,2,3}/Compose/**/*.yml`, and the Bruno test collections are untouched.
- `AGENTS.md`/`docs/developer.md` were grepped for references to the old workflow filenames, `admin_inspect.sh`, or `get_token.sh` — no matches found, so no doc updates are needed.
