# Plan: V3 CI/CD GitHub Actions Integration

**Date:** April 21, 2026  
**Author:** Architecture review  
**Goal:** Integrate `EdFi.Ods.AdminApi.V3` into the GitHub Actions E2E pipeline, mirroring the existing V2 CI/CD structure.

The core differentiator throughout is `ADMINAPI_MODE=V3` (vs `V2`) and artifact/project paths pointing to `EdFi.Ods.AdminApi.V3`.

---

## Phase 1 — Rename Existing V2 Workflow Files

The four current workflow files have no version indicator in their names. Rename them to make room for the parallel V3 workflows.

| Current name | New name |
|---|---|
| `api-e2e-mssql-multitenant.yml` | `api-v2-e2e-mssql-multitenant.yml` |
| `api-e2e-mssql-singletenant.yml` | `api-v2-e2e-mssql-singletenant.yml` |
| `api-e2e-pgsql-multitenant.yml` | `api-v2-e2e-pgsql-multitenant.yml` |
| `api-e2e-pgsql-singletenant.yml` | `api-v2-e2e-pgsql-singletenant.yml` |

**Additional change:** Update the `name:` field inside each file to include `V2`, e.g.:
```
name: Admin API V2 Multi Tenant E2E Tests + ODS 7 + Mssql
```

### ✅ Checkpoint — After Phase 1

Before continuing, verify:
- [ ] All 4 renamed workflow files are visible in `.github/workflows/` with the new `api-v2-e2e-*` names
- [ ] The solution still builds: `./build.ps1 -Command build`
- [ ] Unit tests still pass: `./build.ps1 -Command UnitTest`
- [ ] Push to a branch and confirm GitHub Actions picks up the renamed workflow files correctly (no duplicate runs, triggers fire as expected)

---

## Phase 2 — Introduce `Docker/Settings/shared/` and `Docker/Settings/V3/DB-Admin/`

### File analysis

Reviewing `Docker/Settings/V2/`, files fall into two categories:

**Version-specific** (contain hardcoded `ADMIN_API_VERSION` defaults and download the `EdFi.Suite3.ODS.AdminApi` NuGet package — must stay in per-version folders):

| File | Reason |
|---|---|
| `DB-Admin/mssql/Dockerfile` | `ENV ADMIN_API_VERSION="${ADMIN_API_VERSION:-2.2.2}"` |
| `DB-Admin/pgsql/Dockerfile` | `ENV VERSION="${ADMIN_API_VERSION:-2.2.0}"` |

**Shareable** (reference only env vars, no V2-specific content):

| File | Why shareable |
|---|---|
| `DB-Admin/mssql/entrypoint.sh` | Forks init script and `sqlservr`; no version refs |
| `DB-Admin/mssql/healthcheck.sh` | Generic MSSQL check via `$SA_PASSWORD` |
| `DB-Admin/mssql/init-database.sh` | Driven entirely by `$SQLSERVER_*` env vars |
| `DB-Admin/mssql/run-adminapi-migrations.sh` | Driven entirely by `$MSSQL_*` env vars |
| `DB-Admin/pgsql/run-adminapi-migrations.sh` | Driven entirely by `$POSTGRES_*` env vars |
| `DB-Ods/mssql/Dockerfile` | ODS database — unrelated to AdminApi version; versions are ARGs |
| `DB-Ods/mssql/init.sh` | Generic env-driven startup |
| `DB-Ods/mssql/setup-db.sh` | Generic env-driven setup |
| `gateway/Dockerfile` | Plain nginx image copy |
| `gateway/IDP.Dockerfile` | Plain nginx image copy |
| `gateway/default.conf.template` | Uses `${ADMIN_API_VIRTUAL_NAME}` env var only |
| `gateway/default_idp.conf.template` | Uses env vars only |

12 of the 14 files are shareable.

---

### Phase 2a — Create `Docker/Settings/shared/`

Create a new `shared/` folder mirroring the `V2/` structure, containing only the 12 shareable files:

```
Docker/Settings/shared/
  DB-Admin/
    mssql/
      entrypoint.sh
      healthcheck.sh
      init-database.sh
      run-adminapi-migrations.sh
    pgsql/
      run-adminapi-migrations.sh
  DB-Ods/
    mssql/
      Dockerfile
      init.sh
      setup-db.sh
  gateway/
    default.conf.template
    default_idp.conf.template
    Dockerfile
    IDP.Dockerfile
```

All files are **direct copies** from their `V2/` equivalents — no content changes.

Once the shared folder exists, update the existing V2 artifacts to reference it:

- **`Docker/V2/db.mssql.admin.Dockerfile`** — update all `COPY Settings/V2/DB-Admin/mssql/...` paths to `Settings/shared/DB-Admin/mssql/...`
- **`Docker/V2/db.pgsql.admin.Dockerfile`** — update all `COPY Settings/V2/DB-Admin/pgsql/...` paths to `Settings/shared/DB-Admin/pgsql/...`
- **All V2 compose files** — update `context: ../../../../Settings/V2/gateway/` to `context: ../../../../Settings/shared/gateway/`
- **All V2 compose files** — update any `Settings/V2/DB-Ods/` references to `Settings/shared/DB-Ods/`

> After this step, `Docker/Settings/V2/` retains only 2 files: `DB-Admin/mssql/Dockerfile` and `DB-Admin/pgsql/Dockerfile`.

---

### Phase 2b — Create `Docker/Settings/V3/DB-Admin/`

Create only the 2 version-specific Dockerfiles for V3:

**`Docker/Settings/V3/DB-Admin/mssql/Dockerfile`** — copy from `Docker/Settings/V2/DB-Admin/mssql/Dockerfile`, change the version default:
```diff
- ENV ADMIN_API_VERSION="${ADMIN_API_VERSION:-2.2.2}"
+ ENV ADMIN_API_VERSION="${ADMIN_API_VERSION:-3.x.x-placeholder}"
```

**`Docker/Settings/V3/DB-Admin/pgsql/Dockerfile`** — copy from `Docker/Settings/V2/DB-Admin/pgsql/Dockerfile`, change the version default:
```diff
- ENV VERSION="${ADMIN_API_VERSION:-2.2.0}"
+ ENV VERSION="${ADMIN_API_VERSION:-3.x.x-placeholder}"
```

All shell scripts come from `shared/` — no per-version script copies needed.

### ✅ Checkpoint — After Phase 2

Before continuing, verify:
- [ ] `Docker/Settings/shared/` exists with all 12 files in the correct sub-folders
- [ ] `Docker/Settings/V2/` now contains only `DB-Admin/mssql/Dockerfile` and `DB-Admin/pgsql/Dockerfile`
- [ ] `Docker/Settings/V3/DB-Admin/mssql/Dockerfile` and `pgsql/Dockerfile` exist with the V3 version placeholder
- [ ] Existing V2 DB Dockerfiles (`Docker/V2/db.mssql.admin.Dockerfile`, `db.pgsql.admin.Dockerfile`) have been updated to reference `Settings/shared/`
- [ ] All existing V2 compose files have been updated to reference `Settings/shared/gateway/`
- [ ] Validate a V2 compose stack still starts correctly after the path updates:
  ```bash
  docker compose -f Docker/V2/Compose/mssql/SingleTenant/compose-build-dev.yml config
  ```
- [ ] Solution still builds: `./build.ps1 -Command build`

---

## Phase 3 — Docker/V3 DB Dockerfiles + Compose Files

### 3a. DB Dockerfiles

**`Docker/V3/db.mssql.admin.Dockerfile`** — copy from `Docker/V2/db.mssql.admin.Dockerfile` (which will already reference `Settings/shared/` after Phase 2a) with one additional change:

```diff
- COPY --from=assets Application/EdFi.Ods.AdminApi/Artifacts/MsSql/...
+ COPY --from=assets Application/EdFi.Ods.AdminApi.V3/Artifacts/MsSql/...
```

The `Settings/shared/DB-Admin/mssql/` script references are inherited unchanged from the V2 Dockerfile.

**`Docker/V3/db.pgsql.admin.Dockerfile`** — copy from `Docker/V2/db.pgsql.admin.Dockerfile` with the same artifact path change (`EdFi.Ods.AdminApi.V3`). Script references from `Settings/shared/` are inherited unchanged.

### 3b. Compose Files — Full Parity with V2

Create all **20 compose files** (10 mssql + 10 pgsql) mirroring the V2 structure:

```
Docker/V3/Compose/
  mssql/
    env.example
    env-idp.example
    SingleTenant/
      compose-build-binaries.yml
      compose-build-dev.yml                  ← used by CI/CD
      compose-build-idp-binaries.yml
      compose-build-idp-dev.yml
      compose-build-ods.yml
    MultiTenant/
      compose-build-binaries-multi-tenant.yml
      compose-build-dev-multi-tenant.yml     ← used by CI/CD
      compose-build-idp-binaries-multi-tenant.yml
      compose-build-idp-dev-multi-tenant.yml
      compose-build-ods-multi-tenant.yml
  pgsql/
    (same structure as mssql above)
```

**Changes vs V2 in every compose file:**

| V2 value | V3 value |
|---|---|
| `AppSettings__AdminApiMode: ${ADMINAPI_MODE:-V2}` | `AppSettings__AdminApiMode: ${ADMINAPI_MODE:-V3}` |
| `dockerfile: V2/db.mssql.admin.Dockerfile` | `dockerfile: V3/db.mssql.admin.Dockerfile` |
| `dockerfile: V2/db.pgsql.admin.Dockerfile` | `dockerfile: V3/db.pgsql.admin.Dockerfile` |

**Unchanged in V3 compose files vs V2 compose files (after Phase 2a updates):**
- `context: ../../../../Settings/shared/gateway/` — shared nginx gateway (both V2 and V3 point here after Phase 2a)
- All other environment variables

### ✅ Checkpoint — After Phase 3

Before continuing, verify:
- [ ] `Docker/V3/db.mssql.admin.Dockerfile` and `db.pgsql.admin.Dockerfile` exist and reference `Settings/V3` and `EdFi.Ods.AdminApi.V3` paths
- [ ] All 20 compose files exist under `Docker/V3/Compose/` (10 mssql + 10 pgsql)
- [ ] Validate YAML for the two compose files that CI/CD will use:
  ```bash
  docker compose -f Docker/V3/Compose/mssql/SingleTenant/compose-build-dev.yml config
  docker compose -f Docker/V3/Compose/pgsql/SingleTenant/compose-build-dev.yml config
  ```
- [ ] Spin up one compose stack locally and confirm `ADMINAPI_MODE=V3` is set:
  ```bash
  docker exec adminapi env | grep ADMINAPI_MODE
  ```
- [ ] Solution still builds: `./build.ps1 -Command build`

---

## Phase 4 — E2E Tests/V3 Collection and Setup

### Where to host the V3 E2E tests

The V2 E2E tests live inside `Application/EdFi.Ods.AdminApi/E2E Tests/`. Moving the V3 tests to `Application/EdFi.Ods.AdminApi.V3/E2E Tests/` is a natural ownership boundary — the V3 test collection belongs to the V3 project.

**Feasibility:** The V2 workflow sets `working-directory: ./Application/EdFi.Ods.AdminApi/`. The V3 workflow will set `working-directory: ./Application/EdFi.Ods.AdminApi.V3/`. Since both projects are siblings at the same directory depth, all relative paths used in the workflow steps (`../../Docker/`, `../../eng/`, `../NuGet.Config`, `../EdFi.Ods.AdminApi`, etc.) resolve identically — **the move is fully viable with no path arithmetic changes**.

**Conclusion:** V3 E2E tests will live in `Application/EdFi.Ods.AdminApi.V3/E2E Tests/`.

### E2E test structure (V2 reference)

The V2 Bruno collection is structured as:
```
EdFi.Ods.AdminApi/E2E Tests/V2/
  gh-action-setup/
    .automation_mssql.env       ← env vars for CI run (ADMINAPI_MODE, DB creds, etc.)
    .automation_pgsql.env
    admin_inspect.sh            ← polls Docker health + HTTP /health check
    ods_inspect.sh
  Bruno Admin API E2E 2.0 refactor/
    bruno.json                  ← Bruno collection root (name, version, ignore)
    collection.bru              ← pre-request script (adds Tenant header if multitenant)
    environments/
      local.bru                 ← generated at runtime by get_token.sh
    get_token.sh                ← registers client, fetches JWT, writes local.bru
    v2/                         ← all .bru request files, organised by resource
    README.md
```

The Bruno CLI requires `bruno.json` to be present in the directory it is run from. `get_token.sh` writes `environments/local.bru` relative to its own location.

### 4a. gh-action-setup files

**`Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/`**

| File | Action |
|---|---|
| `.automation_mssql.env` | Copy from V2; change `ADMINAPI_MODE=V3` |
| `.automation_pgsql.env` | Copy from V2; change `ADMINAPI_MODE=V3` |
| `admin_inspect.sh` | Direct copy (no changes) |
| `ods_inspect.sh` | Direct copy (no changes) |

> Note: no `V2/` or `V3/` sub-folder inside `gh-action-setup/` — the V3 project already provides the version scope.

### 4b. Bruno test collection

**`Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/`**

| File/folder | Action |
|---|---|
| `bruno.json` | Copy from V2; update `name` to `Admin API E2E 3.0` |
| `collection.bru` | Direct copy (pre-request script is version-agnostic) |
| `get_token.sh` | Copy from V2 (no functional changes needed initially) |
| `environments/local.bru` | Placeholder — generated at runtime by `get_token.sh` |
| `v3/` | Placeholder directory; actual `.bru` test requests are a separate task |
| `README.md` | Copy from V2 README, update `v2/` references to `v3/` |

> **Note:** The actual V3 Bruno request files (`.bru`) covering the V3 API surface are out of scope for this plan and will be created in a follow-up task.

### ✅ Checkpoint — After Phase 4

Before continuing, verify:
- [ ] `Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/` contains all 4 files
- [ ] Both `.automation_*.env` files have `ADMINAPI_MODE=V3`
- [ ] `Bruno Admin API E2E 3.0/get_token.sh` is executable and runs without errors against a locally running V3 stack:
  ```bash
  bash './E2E Tests/Bruno Admin API E2E 3.0/get_token.sh' singletenant mssql
  ```
  *(run from `Application/EdFi.Ods.AdminApi.V3/`)*
- [ ] `environments/local.bru` is generated correctly by `get_token.sh`
- [ ] Bruno CLI dry-run succeeds (zero test files is acceptable at this stage):
  ```bash
  cd './E2E Tests/Bruno Admin API E2E 3.0/'
  npx @usebruno/cli@latest run --env local --sandbox=developer --insecure -r 'v3/'
  ```

---

## Phase 5 — New V3 GitHub Actions Workflow Files

Create four new workflow files under `.github/workflows/`:

| New file | Based on |
|---|---|
| `api-v3-e2e-mssql-singletenant.yml` | `api-v2-e2e-mssql-singletenant.yml` |
| `api-v3-e2e-mssql-multitenant.yml` | `api-v2-e2e-mssql-multitenant.yml` |
| `api-v3-e2e-pgsql-singletenant.yml` | `api-v2-e2e-pgsql-singletenant.yml` |
| `api-v3-e2e-pgsql-multitenant.yml` | `api-v2-e2e-pgsql-multitenant.yml` |

**Changes vs V2 in every V3 workflow file:**

```diff
- name: Admin API V2 Single Tenant E2E Tests + ODS 7 + Mssql
+ name: Admin API V3 Single Tenant E2E Tests + ODS 7 + Mssql

# working-directory — points to the V3 project:
- working-directory: ./Application/EdFi.Ods.AdminApi/
+ working-directory: ./Application/EdFi.Ods.AdminApi.V3/

# Additional copy step — add after the V1 copy:
+ - name: Copy admin api V3 folder to docker context
+   run: cp -r ../EdFi.Ods.AdminApi.V3 ../../Docker/Application

# Compose path (../../ from V3 project still reaches Docker/):
- -f '../../Docker/V2/Compose/mssql/SingleTenant/compose-build-dev.yml'
+ -f '../../Docker/V3/Compose/mssql/SingleTenant/compose-build-dev.yml'

# Env file (relative to new working-directory):
- --env-file './E2E Tests/V2/gh-action-setup/.automation_mssql.env'
+ --env-file './E2E Tests/gh-action-setup/.automation_mssql.env'

# Inspect script:
- './E2E Tests/V2/gh-action-setup/admin_inspect.sh' adminapi
+ './E2E Tests/gh-action-setup/admin_inspect.sh' adminapi

# get_token.sh:
- bash './E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/get_token.sh' singletenant mssql
+ bash './E2E Tests/Bruno Admin API E2E 3.0/get_token.sh' singletenant mssql

# Bruno run directory and collection:
- cd './E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/'
- npx @usebruno/cli@latest run --env local --sandbox=developer --insecure -r 'v2/' ...
+ cd './E2E Tests/Bruno Admin API E2E 3.0/'
+ npx @usebruno/cli@latest run --env local --sandbox=developer --insecure -r 'v3/' ...

# Artifact upload paths (now relative to EdFi.Ods.AdminApi.V3):
- path: Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/results.html
+ path: Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/results.html
```

### ✅ Checkpoint — After Phase 5

Before closing this plan, verify:
- [ ] All 4 new `api-v3-e2e-*.yml` workflow files exist under `.github/workflows/`
- [ ] Solution still builds: `./build.ps1 -Command build`
- [ ] Unit tests still pass: `./build.ps1 -Command UnitTest`
- [ ] Push to a branch and trigger each new workflow via `workflow_dispatch` in the GitHub Actions UI
- [ ] Confirm each workflow reaches the "Run Tests" step without errors (even if the `v3/` Bruno collection is empty)
- [ ] Confirm Docker logs upload correctly on failure (test by intentionally failing one run)
- [ ] Confirm the V2 workflows (`api-v2-e2e-*.yml`) are unaffected and still pass

---

## File Change Summary

### New files

| File | Action |
|---|---|
| `Docker/Settings/shared/DB-Admin/mssql/` (×4 files) | Create — copy from V2 (no content changes) |
| `Docker/Settings/shared/DB-Admin/pgsql/` (×1 file) | Create — copy from V2 (no content changes) |
| `Docker/Settings/shared/DB-Ods/mssql/` (×3 files) | Create — copy from V2 (no content changes) |
| `Docker/Settings/shared/gateway/` (×4 files) | Create — copy from V2 (no content changes) |
| `Docker/Settings/V3/DB-Admin/mssql/Dockerfile` | Create — copy V2; update version ARG placeholder |
| `Docker/Settings/V3/DB-Admin/pgsql/Dockerfile` | Create — copy V2; update version ARG placeholder |
| `Docker/V3/db.mssql.admin.Dockerfile` | Create — copy V2 (post-Phase-2a); change Artifacts path to V3 |
| `Docker/V3/db.pgsql.admin.Dockerfile` | Create — copy V2 (post-Phase-2a); change Artifacts path to V3 |
| `Docker/V3/Compose/mssql/` (env ×2 + compose ×10) | Create — copy V2; change `AdminApiMode` default + db Dockerfile path |
| `Docker/V3/Compose/pgsql/` (env ×2 + compose ×10) | Create — copy V2; same changes |
| `Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/.automation_mssql.env` | Create — copy V2; `ADMINAPI_MODE=V3` |
| `Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/.automation_pgsql.env` | Create — copy V2; `ADMINAPI_MODE=V3` |
| `Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/admin_inspect.sh` | Create — direct copy |
| `Application/EdFi.Ods.AdminApi.V3/E2E Tests/gh-action-setup/ods_inspect.sh` | Create — direct copy |
| `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/` | Create — stub Bruno collection |
| `.github/workflows/api-v3-e2e-*.yml` (×4) | Create — copy V2 workflows; update all V3 paths |

### Modified files

| File | Change |
|---|---|
| `.github/workflows/api-e2e-*.yml` (×4) | Rename → `api-v2-e2e-*.yml`; update `name:` field |
| `Docker/V2/db.mssql.admin.Dockerfile` | Update `COPY Settings/V2/...` → `Settings/shared/...` |
| `Docker/V2/db.pgsql.admin.Dockerfile` | Update `COPY Settings/V2/...` → `Settings/shared/...` |
| All V2 compose files (×20) | Update `Settings/V2/gateway/` → `Settings/shared/gateway/` |

**Total new files:** ~44  
**Total modified files:** 26 (4 workflow renames + 2 V2 DB Dockerfiles + 20 V2 compose files)

---

## Verification Checklist

- [ ] `./build.ps1 -Command build` passes (smoke check — no code changes)
- [ ] All 4 renamed V2 workflows still appear correctly in the GitHub Actions tab
- [ ] `docker compose -f Docker/V3/Compose/pgsql/SingleTenant/compose-build-dev.yml ... config` validates YAML
- [ ] `docker exec adminapi env | grep ADMINAPI_MODE` returns `V3` in a local V3 container run
- [ ] 4 new V3 workflow files are triggered on `push` to `main` or via `workflow_dispatch`
- [ ] V3 Bruno stub collection runs without errors (zero test failures expected until `.bru` files are added)

---

## Decisions and Scope Boundaries

| Item | Decision |
|---|---|
| Gateway (nginx) | **Shared** — moved to `Settings/shared/gateway/`; both V2 and V3 reference it |
| DB-Ods scripts | **Shared** — moved to `Settings/shared/DB-Ods/`; both V2 and V3 reference it |
| DB-Admin shell scripts | **Shared** — moved to `Settings/shared/DB-Admin/`; both V2 and V3 reference them |
| DB-Admin Dockerfiles | **Version-specific** — remain in `Settings/V2/` and `Settings/V3/` (hardcoded NuGet version defaults) |
| Bruno `.bru` test requests | **Out of scope** — placeholder `v3/` directory only; separate follow-up task |
| IDP (Keycloak) compose variants | **Included** for full parity; no new IDP workflow files (original 4 V2 workflows did not include IDP) |
| `ADMINAPI_MODE` | `V3` as default in compose files and hardcoded in `.automation_*.env` files |
| V3 NuGet package versions in DB Dockerfiles | **TBD** — use placeholder ARG until V3 packages are published |
