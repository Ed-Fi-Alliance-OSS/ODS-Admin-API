# Instance/DataStore Management Design Docs Rewrite

**Ticket:** [ADMINAPI-1485](https://edfi.atlassian.net/browse/ADMINAPI-1485) — Update Instance/DataStore Management design docs to match current implementation

## Problem

Three design docs under `docs/design/` (`INSTANCE-MANAGEMENT.md`, `INSTANCE-MANAGEMENT-Quartz.md`, `INSTANCE-MANAGEMENT-Service.md`) describe the ODS Instance / Data Store management feature but were never kept in sync with the implementation. Beyond a large `DbInstance`/`DbDataStore` → `OdsInstanceManage`/`DataStoreManage` rename that was never reflected, an audit against current code (see Verified Facts below) found sections describing behavior that no longer exists, or never existed at all.

## Decision: Consolidate to one file

Replace the three docs with a single `docs/design/INSTANCE-MANAGEMENT.md` (the filename survives; `INSTANCE-MANAGEMENT-Quartz.md` and `INSTANCE-MANAGEMENT-Service.md` are deleted). A new engineer should be able to read one file top-to-bottom and understand the whole feature, rather than reconciling three docs with overlapping/conflicting claims.

Two other files reference the old structure and need updating:

* `docs/developer.md:278` — currently links to `design/INSTANCE-MANAGEMENT-Quartz.md`; repoint to `design/INSTANCE-MANAGEMENT.md` (with an anchor into the Background Jobs section).
* Both disabled E2E test files reference a dangling `docs/design/DBINSTANCE-PROVISIONING-JOBS.md` path in their comments (a name that predates even the old doc set):
  * `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/DELETE - OdsInstance Manage - Success.bru.disabled`
  * `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/DELETE - DataStore Manage - Success.bru.disabled`

  Repoint both to `docs/design/INSTANCE-MANAGEMENT.md`.

## Scope boundary: what "current" means

The working tree has uncommitted changes to `appsettings.json`/`appsettings.Development.json` (an `EnableDataStoreManagement` flag, `adminApiMode` settings), and an unmerged remote branch `origin/ADMINAPI-1489` adds this feature-flag layer with its own design docs. **None of that is in scope.** The rewritten doc describes only the committed, merged behavior on `main`: a process runs in exactly one of `AdminApiMode` = `v2` or `v3` (set via `AppSettings:AdminApiMode`, default `v2`), never both, and there is no feature flag gating the manage endpoints. If ADMINAPI-1489 merges later, updating this doc for it is separate follow-up work.

## Doc outline

1. **Overview** — what the feature does and why; one architecture diagram (updated C4 diagram, real names)
2. **Data model** — `OdsInstanceManage` entity/table, status enum; leads with the clarification that v3's "DataStoreManage" is an API-facing name only, not a separate table
3. **REST API** — v2 and v3 together (single narrative), differences called out inline; routes, request/response shapes, validation, response codes
4. **Background jobs (Quartz)** — status lifecycle, create/delete pipelines, dispatcher retry logic, job identity/keys, tenant-context propagation (compressed)
5. **Sandbox provisioning layer** — `ISandboxProvisioner` and implementations as they actually work; DI-based engine selection
6. **Tenant integration** — how `TenantService` merges manage-rows into tenant EdOrgs responses (new section — previously undocumented)
7. **Known Limitations** — the two real product gaps, plus current E2E-disabled-test status

## Verified facts driving corrections

Verified against `main` at `0d617f1d` (see full audit trail in conversation; summarized here as the source of truth for the rewrite).

### Routes / entity
- v2: `POST/GET/DELETE /v2/odsInstances/manage[/{id}]` — `Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/`
- v3: `POST/GET/DELETE /v3/dataStores/manage[/{id}]` — `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/`
- Single shared entity/table: `OdsInstanceManage` → `adminapi.OdsInstanceManages` (`Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs`). **No separate `DataStoreManage` table exists** — v3 is a naming re-skin over the same table.
- Columns: `Name` NOT NULL, `VARCHAR(100)`, regex `^[A-Za-z0-9 _]+$`; `OdsInstanceId`/`OdsInstanceName` nullable; `DatabaseName` column is `VARCHAR(255)` but capped at 63 chars by the **app-level validator**, not the schema.

### Response codes
- `DELETE` → **204 No Content** (both versions) — not 202.
- Blocked-status delete → **400**; not-found/already-deleted → **404**. **No 422** anywhere in this path.
- `POST` (create) → 202 Accepted (this part of the old docs was correct).

### Validation
- `DatabaseTemplate` valid values: exact-case `"Minimal"` / `"Sample"` (`SandboxType` enum).
- Duplicate-name check: rejects if name matches a non-deleted `OdsInstanceManage` row OR an existing `OdsInstance` row.
- `DatabaseName` build: `OdsInstanceManageDatabaseNameFormatter`/`DataStoreManageDatabaseNameFormatter` — normalizes spaces to `_`, strips a leading `EdFi_Ods` prefix run (case-insensitive), produces `EdFi_Ods_{name}_{template}`; rejected at POST if the result would exceed 63 chars.

### Jobs
- v2 classes: `CreatePendingOdsInstanceManagesDispatcherJob`, `DeletePendingOdsInstanceManagesDispatcherJob`, `CreateInstanceJob`, `DeleteInstanceJob` (namespace `EdFi.Ods.AdminApi...`).
- v3 classes: `CreatePendingDataStoreManagesDispatcherJob`, `DeletePendingDataStoreManagesDispatcherJob`, `CreateInstanceJob`, `DeleteInstanceJob` (namespace `EdFi.Ods.AdminApi.V3...`).
- `JobConstants` (shared project) defines job-key **strings** reused verbatim by both versions (e.g. `"CreateInstanceJob"`, `"CreatePendingOdsInstanceManagesDispatcherJob"`) — there are no separate `*DataStoreManages*` string constants. Safe only because a process runs exclusively v2 or v3 job set via `AdminApiMode` — flag as a latent fragility, not just trivia.
- Dispatchers query only `Pending*`/`*Failed` statuses — confirmed **no** recovery path for `CreateInProgress`/`DeleteInProgress` rows stuck after a crash.
- `ValidatePendingState` (both `CreateInstanceJob` classes): throws if a `PendingCreate` row already has `OdsInstanceId`/`OdsInstanceName` set, or is missing `DatabaseTemplate`.

### Provisioners
- `ISandboxProvisioner`: `AddSandbox(Async)`, `DeleteSandboxes(Async)`, `RenameSandbox(Async)`, `GetSandboxStatus(Async)`, `CopySandboxAsync`. **No `InstanceInfo`/size-query member.**
- `SandboxStatus`: only `Name`, `Code`, `Description` (+ `ErrorStatus()` factory).
- `PostgresSandboxProvisioner`: `CREATE DATABASE ... TEMPLATE ...` after terminating existing connections to the source db.
- `SqlServerSandboxProvisioner`: restores from a **static, pre-configured `.bak` file** (`AppSettings:SqlServerMinimalBakFile`/`SqlServerSampleBakFile`) via `RESTORE FILELISTONLY` + `RESTORE DATABASE ... WITH REPLACE, MOVE`. **No `BACKUP DATABASE`/`DBCC` calls.**
- Provisioner selection is DI-based: `AppSettings:DatabaseEngine` read once at startup in `WebApplicationBuilderExtensions.RegisterSandboxProvisioningServices` — not per-request.

### Tenant integration
- `TenantService.GetTenantEdOrgsByInstancesAsync` (v2 and v3, structurally identical): overlays manage-row fields (`OdsInstanceManageId`, `Status`, `DatabaseTemplate`, `DatabaseName`) onto real `OdsInstance` entries when linked by `OdsInstanceId`, picking the latest-modified manage-row per group; real instances with no linked manage-row default to `Status = "Created"`. Manage-rows with no linked instance (or pointing at a deleted one) are appended separately as "unlinked."

### Known limitations (to document, not fix)
1. No automatic recovery for rows stuck in `CreateInProgress`/`DeleteInProgress` after a crash — dispatcher never re-queries those statuses; requires manual DB fix.
2. `InstanceInfo`/size-metadata provisioner operation was never implemented.
3. `DELETE - OdsInstance Manage - Success` (v2) and `DELETE - DataStore Manage - Success` (v3) E2E tests are currently disabled (`.bru.disabled` + `skip: true`) because CI doesn't seed the `Minimal`/`Sample` template databases before the suite runs.

### Historical note (compress, don't narrate)
Tenant context is propagated to Quartz jobs (which run outside HTTP middleware) via async-local-based context storage, so per-tenant connection strings resolve correctly even though jobs and HTTP requests execute concurrently. One short paragraph in the final doc — no race-condition narrative, sequence diagrams, or rejected-alternatives discussion (that content was itself a resolved-bug postmortem, not durable design).

## Acceptance criteria (from ticket, still binding)

* [ ] Consolidated doc uses current class/route/table/config-key names — no `DbInstance`/`DbDataStore` references remain.
* [ ] DELETE response code, request-body example, and table schema match actual code behavior (per Verified Facts above).
* [ ] Doc covers both v2 and v3, correctly distinguishes the shared job-key strings from the per-version job class names.
* [ ] Provisioner section's class names, interface members, and example SQL match the real `Sandbox*` classes.
* [ ] Links from `docs/developer.md` and both `.bru.disabled` files resolve to the new consolidated doc.
* [ ] A second reviewer spot-checks a sample of claims against the code (routes, one job class, one config key) before merge — this is a review-process step for the human reviewer, not something the rewrite itself can satisfy.

## Explicitly out of scope

* The unmerged `EnableDataStoreManagement` feature flag (`ADMINAPI-1489`) — not documented.
* Building the missing crash-recovery path or the `InstanceInfo` operation — these are product gaps to describe accurately, not fix here. Flag as separate follow-up tickets if not already tracked.
