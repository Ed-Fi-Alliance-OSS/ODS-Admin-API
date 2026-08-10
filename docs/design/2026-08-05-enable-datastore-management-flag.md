# EnableDataStoreManagement Feature Flag

**Ticket:** [ADMINAPI-1489](https://edfi.atlassian.net/browse/ADMINAPI-1489)
**PR:** [#420](https://github.com/Ed-Fi-Alliance-OSS/ODS-Admin-API/pull/420)

> This is a condensed summary of the original design and implementation plan. The full documents (`docs/superpowers/specs/2026-08-05-enable-datastore-management-flag-design.md` and `docs/superpowers/plans/2026-08-05-enable-datastore-management-flag.md`) were removed from the repo for size; this file is the durable reference going forward.

## What it does

Adds an `AppSettings:EnableDataStoreManagement` flag (bool, default `true`) that, when set to `false`, fully disables instance-management ("data store management") capability:

1. **Gates 8 API routes** — POST, DELETE, GET-collection, and GET-by-id, duplicated across V2 (`/odsInstances/manage*`, `EdFi.Ods.AdminApi`) and V3 (`/dataStores/manage*`, `EdFi.Ods.AdminApi.V3`). Each returns `400` instead of executing.
2. **Skips scheduling 4 recurring Quartz dispatcher jobs** at startup (create/delete × V2/V3), in both single-tenant and multi-tenant configurations. `RefreshEducationOrganizationsJob` is unrelated and always keeps running.
3. **Is wired end-to-end**: `AppSettings.cs`, both `appsettings.json` files, all 36 Docker compose `*.yml` files, and the Docker `*.example`/`.env` files, via a new `ENABLE_DATA_STORE_MANAGEMENT` env var (default `true`).
4. **Is documented** in `docs/developer.md`, with 2 disabled-flag Bruno E2E specs (`.bru.disabled`, not run by default).

## Design decisions

- **Reused the existing convention** from `EnableApplicationResetEndpoint`/`ResetApplicationCredentials.cs`: the flag check is the first statement of each handler, throwing `FluentValidation.ValidationException` with a single `ValidationFailure`. Existing error-handling middleware converts that to `400` automatically — no new response mechanism was introduced.
- **`PropertyName` on every `ValidationFailure` is `nameof(OdsInstance)`** (from `EdFi.Admin.DataAccess.Models`), used identically across all 8 handlers, V2 and V3 alike. There is no separate `DataStore` domain type — "DataStore" is V3 API-surface terminology over the same underlying `OdsInstance` entity.
- **Error message is exactly** `"This endpoint has been disabled on application settings."` everywhere, matching `ResetApplicationCredentials` verbatim.
- **Job-scheduling gate**: `Program.cs` (a top-level-statements file) had no extracted/testable scheduling logic. A local function declared inside top-level statements compiles as a private, name-mangled member and cannot be unit-tested via `InternalsVisibleTo` — so the gate is a small `internal static` predicate, `DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(AppSettings)`, called from all 4 dispatcher-scheduling call sites in `Program.cs`. It lives in `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/DataStoreManagementJobScheduler.cs`, not inline in `Program.cs` as originally planned — a global-namespace type there tripped SonarAnalyzer rule S3903 under `TreatWarningsAsErrors`, so it was moved into the existing `EdFi.Ods.AdminApi.Infrastructure.Services.Jobs` namespace (already imported by `Program.cs`), preserving the same `InternalsVisibleTo`-based testability with no suppression needed.
- **The flag check is nested inside the existing interval-validity check** (`if (shouldScheduleDispatcher) { if (flag) {...} else { log Info } } else { log Error }`), not combined with `&&`, so a disabled flag logs a clear "skipping" Info message instead of a misleading "Invalid value for..." Error.
- **Docker compose convention**: every file gets `AppSettings__EnableDataStoreManagement: ${ENABLE_DATA_STORE_MANAGEMENT:-true}` — the parameterized form, even in the older V1 files where the neighboring `EnableApplicationResetEndpoint` line is hardcoded `false`.
- **Out of scope**: `EdFi.Ods.AdminApi.InstanceManagement` (sandbox provisioners, only ever invoked from the gated jobs), a full disabled-flag Bruno CI matrix, and refactoring the pre-existing interval-parsing scheduling conditions beyond adding the new predicate.

## Implementation notes / known follow-ups

- Unit tests: one disabled-flag test per handler (8 total, split across `EdFi.Ods.AdminApi.UnitTests` and `EdFi.Ods.AdminApi.V3.UnitTests`), plus 2 tests for the job-scheduling predicate.
- The guard block is duplicated verbatim across all 8 handlers rather than factored into a shared helper — deliberate, mirrors the pre-existing `ResetApplicationCredentials` precedent; flagged in review as an acceptable, low-value extraction target if it ever grows further.
- The 2 new Bruno `.bru.disabled` specs use `seq: 2`, which collides with each folder's existing `...Invalid.bru` sibling also at `seq: 2` — harmless while suffixed `.disabled`, but would need renumbering if ever promoted to a real, always-run spec.
- No mechanism exists today to toggle an `AppSettings` flag between Bruno E2E runs; the disabled-flag specs are exercised manually (`$env:ENABLE_DATA_STORE_MANAGEMENT = "false"` before `./eng/run-bruno-e2e.ps1`), and `docs/developer.md` notes that the rest of the `Manage/` folder's specs are expected to fail during that run.

## Testing

- `./build.ps1 -Command UnitTest`: all 4 unit test projects green.
- Manual smoke: boot with the flag `false`, confirm all 8 endpoints return `400` and the 4 dispatcher jobs are absent from Quartz scheduling while `RefreshEducationOrganizationsJob` still appears.
