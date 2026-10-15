# E2E Workflow Consolidation Design

## Problem

The repo has 10 separate GitHub Actions E2E workflow files, each on its own
runner/VM:

- `api-v1-e2e-mssql.yml`, `api-v1-e2e-pgsql.yml`
- `api-v2-e2e-mssql-multitenant.yml`, `api-v2-e2e-mssql-singletenant.yml`,
  `api-v2-e2e-pgsql-multitenant.yml`, `api-v2-e2e-pgsql-singletenant.yml`
- `api-v3-e2e-mssql-multitenant.yml`, `api-v3-e2e-mssql-singletenant.yml`,
  `api-v3-e2e-pgsql-multitenant.yml`, `api-v3-e2e-pgsql-singletenant.yml`

All ten share the same trigger set (`push` to `main`, `schedule: "0 5 * * 1"`,
`workflow_dispatch`, `pull_request`), so they can run concurrently. The pgsql
combos authenticate to Docker Hub via `docker/login-action` using one shared
account (`DOCKER_USERNAME`/`DOCKER_HUB_TOKEN`), which has a 200-pulls/6h quota
**per account**, not per job. Running many of these jobs concurrently draws
against that same shared quota at once, risking a Docker Hub pull rate limit.
mssql combos don't authenticate (SQL Server image comes from
`mcr.microsoft.com`, not Docker Hub, so it isn't subject to this limit).

Each of the 10 jobs also runs on a fresh runner VM, so even same-named base
images (e.g. `postgres`) get pulled from scratch by every job — no image
layer cache is shared across jobs today.

## Goal

Reduce the number of concurrent jobs drawing against the shared Docker Hub
quota, and let same-runner image pulls share a cache, without losing any
existing test coverage or per-combo JIRA reporting granularity.

## Design

### Workflow grouping

Replace the 10 files with 3, one per API version:

| File | Combos |
|---|---|
| `api-v1-e2e.yml` | mssql, pgsql (single-tenant only — v1 has no multitenant mode) |
| `api-v2-e2e.yml` | mssql × single/multi-tenant, pgsql × single/multi-tenant |
| `api-v3-e2e.yml` | mssql × single/multi-tenant, pgsql × single/multi-tenant |

Each file runs **one job, one runner**, executing its combos **sequentially**:
`compose up` combo A → test → `compose down -v` → combo B → test →
`compose down -v` → … This means:

- Total E2E job-runs per trigger event drops from 10 to 3.
- Docker's local image cache on each runner is shared across that version's
  combos, so a base image used by two combos in the same workflow
  (e.g. `mcr.microsoft.com/mssql/server` used by both single- and
  multi-tenant mssql combos) is pulled once per workflow run instead of once
  per combo.

Triggers consolidate to the broadest existing set per version: v1 keeps its
`pull_request` branch pattern of `[main, "*-hotfix"]`; v2/v3 use
`branches: [main]`. All three keep `cron: "0 5 * * 1"` and
`workflow_dispatch`. JIRA env vars (`PROJECT_ID`, `CYCLE_NAME`, `TASK_NAME`,
`FOLDER_NAME`) are identical across all 10 existing files, so merging them
introduces no conflict.

### Compose files

No changes to `Docker/V{1,2,3}/Compose/**/*.yml`. Because combos run
sequentially with `compose down -v` between each, containers can keep their
existing names (`adminapi`, `ed-fi-db-admin-adminapi-tenant1`/`tenant2`,
etc.) — only one combo's containers exist on the runner at a time.

### Per-combo execution via `eng/run-bruno-e2e.ps1`

`eng/run-bruno-e2e.ps1` already implements, parametrized by
`-ApiVersion`/`-TenantMode`/`-DbEngine`, the full local/CI test flow: copying
the Docker build context, `compose up`, waiting for the `adminapi` container
to report healthy, registering a client and obtaining a token, writing the
Bruno `local.bru` environment, and running the Bruno suite with HTML/JUnit
reporters.

This logic is equivalent to what the current CI workflows do by hand:

- Its health-wait loop (poll `docker inspect -f {{.State.Health.Status}}
  adminapi`, 5-minute timeout, then `GET /adminapi/health` expecting 200) is
  the same logic as `admin_inspect.sh` (same poll target, same timeout, same
  health-endpoint check), just in PowerShell instead of bash/wget.
- Its token-acquisition block (register client, request token, leave
  `TOKEN_TENANT2` blank) is the same logic as `get_token.sh` for all three
  API versions — verified by diff of both scripts' output `vars` blocks.

The consolidated workflows will call this script once per combo instead of
reimplementing copy-context/health-wait/token steps inline. Two additions
are needed to the script:

1. A new `-ReportSuffix <string>` parameter that renames `results.html` /
   `report.xml` to `results-<suffix>.html` / `report-<suffix>.xml` after the
   Bruno run completes. Without this, four sequential combos in one job
   would overwrite each other's report files before upload.
2. CI will **not** pass `-TearDown`. The script's `-TearDown` runs inside its
   own `finally` block, before CI has a chance to capture `docker logs`/
   `docker cp` output on failure. CI keeps an explicit `compose down -v` step
   of its own, placed *after* the failure-log-capture step (see below).

### Per-combo step structure (per version workflow)

One job, steps:

1. Checkout, copy Docker build context (once — shared across all combos in
   that version).
2. Docker Hub login (once, if any pgsql combo is present).
3. For each combo:
   a. `./eng/run-bruno-e2e.ps1 -ApiVersion <v> -TenantMode <t> -DbEngine <d> -ReportSuffix <d>-<t>`
   b. On failure: capture `docker logs` / `docker cp` into
      `docker-logs/<d>-<t>/` before tearing down, so one combo's failure
      doesn't lose diagnostics for the remaining combos.
   c. `docker compose -f <composeFile> --env-file <envFile> down -v`
      (explicit teardown step, always runs, independent of combo
      pass/fail).
4. Upload artifacts once at the end: `test-html-results` and
   `test-xml-results`, each containing every combo's suffixed report files,
   plus `docker-logs` if any combo failed.

### Reporting

The existing `report` job (downloads `test-xml-results`, parses JUnit XML,
uploads to JIRA) stays, but loops over every `report-<suffix>.xml` file found
in the artifact and uploads each as its own JIRA result set — same per-combo
granularity as today, driven by a loop instead of by separate workflow runs.

### Known regression risk

`docker compose down -v` must fully release networks and port bindings
before the next combo's `up -d`, or a leftover network/bound port from combo
N could make combo N+1 fail to start or silently bind to stale state. If
testing surfaces this, add `--remove-orphans` to the `down` command and/or an
explicit `docker network prune -f` between combos.

## Rollout plan

Testing happens in a fork rather than side-by-side in this repo, since the
10 existing workflows will be deleted outright rather than kept for
rollback:

1. Build and iterate the 3 new workflow files in a fork, using
   `workflow_dispatch` to trigger runs freely without touching this repo's
   live CI or schedule.
2. Add the secrets needed to validate the full flow to the fork
   (`JIRA_ACCESS_TOKEN`, `DOCKER_USERNAME`, `DOCKER_HUB_TOKEN`), or skip/stub
   the JIRA-upload and Docker Hub login steps if not duplicating credentials
   into the fork — the mechanics under test are the sequential-combo
   execution, teardown-between-combos, per-combo report naming, and
   failure-log capture, not the JIRA integration itself.
3. Force one combo to fail deliberately (bad env var, wrong port) to confirm
   failure-log capture lands in the right per-combo subfolder, teardown
   still runs, and the next combo in the sequence still starts clean.
4. Once a full run looks right in the fork, open the 3 new files as a PR
   directly against this repo's main, deleting the 10 old workflow files in
   the same PR.
5. Update `AGENTS.md` / `docs/developer.md` references to the old per-combo
   workflow file names.

## Out of scope

- Changes to the Bruno test collections themselves.
- Changes to `Docker/V{1,2,3}/Compose/**/*.yml` compose file contents.
- Parallelizing combos within a workflow (would restore separate-runner pull
  behavior, defeating the purpose of this change).
