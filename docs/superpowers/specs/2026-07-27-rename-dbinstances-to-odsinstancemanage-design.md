# Rename DbInstances (v2) / DbDataStores (v3) to OdsInstances/Manage and DataStores/Manage

Date: 2026-07-27

## Summary

`v2`'s `DbInstances` feature and `v3`'s `DbDataStores` feature are two separate presentation
layers over the exact same physical table (`adminapi.DbInstances`) and the exact same EF Core
entity class (`DbInstance`, defined once in the shared `EdFi.Ods.AdminApi.Common` project and
consumed by both version-specific `AdminApiDbContext` classes).

This change:

- Renames the shared entity/table to `OdsInstanceManage` / `adminapi.OdsInstanceManages`.
- Moves and renames the v2 feature folder `Features\DbInstances` into a new
  `Features\OdsInstances\Manage` subfolder, changing routes from `/v2/dbInstances` to
  `/v2/odsInstances/manage`.
- Moves and renames the v3 feature folder `Features\DbDataStores` into a new
  `Features\DataStores\Manage` subfolder, changing routes from `/v3/dbDataStores` to
  `/v3/dataStores/manage`.
- Renames all identifiers containing `DbInstance`/`DbDataStore` across production code, tests,
  Bruno E2E collections, and `.http` scratch files to match.
- Is a **breaking change**: old routes and old `AppSettings` config keys are removed outright,
  with no deprecated aliases or backward-compatibility shims.

## Background / current state

- v2's `OdsInstances` feature already depends on `DbInstances` data: it merges `DbInstance`
  status/database-name into its "OdsInstance + EdOrgs" response
  (`OdsInstanceWithEducationOrganizationsModel.DbInstanceId`,
  `ReadEducationOrganizations.MergeDbInstanceData`).
- v3's `DataStores` feature does the analogous merge
  (`ReadEducationOrganizations.MergeDbDataStoreData`), but its
  `DataStoreWithEducationOrganizationsModel` is missing a field equivalent to v2's
  `DbInstanceId` — a pre-existing parity gap this change closes.
- The migration artifact tree is duplicated: `Application\EdFi.Ods.AdminApi\Artifacts\` (MsSql +
  PgSql) is the one actually consumed — the `.csproj` includes
  `<Content Include="Artifacts\**" CopyToPublishDirectory="Always" .../>`, and
  `eng\run-dbup-migrations.ps1` → `Install-AdminApiTables` runs against that published output.
  `Application\EdFi.Ods.AdminApi.V3\Artifacts\` is a documentation-only duplicate (the V3 project
  has `IsPublishable=false` and no matching `Content` include) that has historically been kept in
  sync by hand at each prior migration step. This change keeps that convention: the new migration
  is added to both locations, but only the v2 copy has any functional effect.
- There is no FK constraint between this table and `OdsInstances` — `OdsInstanceId` is a plain
  nullable, unenforced column populated once async provisioning completes.

## Naming scheme

| Concept | Old (v2) | Old (v3) | New (shared, Common project) |
|---|---|---|---|
| Entity class | `DbInstance` | `DbInstance` | `OdsInstanceManage` |
| Table | `adminapi.DbInstances` | `adminapi.DbInstances` | `adminapi.OdsInstanceManages` |
| DbSet property | `DbInstances` | `DbInstances` | `OdsInstanceManages` |
| Status enum | `DbInstanceStatus` | `DbInstanceStatus` | `OdsInstanceManageStatus` |

Per-version feature-layer naming stays distinct, matching the existing convention where v3
already renames DTO-level fields (`OdsInstanceId`/`OdsInstanceName` → `DataStoreId`/
`DataStoreName`) while sharing the underlying entity:

| Concept | v2 (new) | v3 (new) |
|---|---|---|
| Feature folder | `Features\OdsInstances\Manage\` | `Features\DataStores\Manage\` |
| Route prefix | `/v2/odsInstances/manage` | `/v3/dataStores/manage` |
| FK-style `Id` field (in the OdsInstance/DataStore + EdOrgs response models) | `OdsInstanceManageId` | `DataStoreManageId` |
| Query interfaces | `IGetOdsInstanceManagesQuery`, `IGetOdsInstanceManageByIdQuery` | `IGetDataStoreManagesQuery`, `IGetDataStoreManageByIdQuery` |
| Command classes | `AddOdsInstanceManageCommand`, `DeleteOdsInstanceManageCommand` | `AddDataStoreManageCommand`, `DeleteDataStoreManageCommand` |
| Dispatcher jobs | `CreatePendingOdsInstanceManagesDispatcherJob`, `DeletePendingOdsInstanceManagesDispatcherJob` | `CreatePendingDataStoreManagesDispatcherJob`, `DeletePendingDataStoreManagesDispatcherJob` |

Shared `JobConstants`/`AppSettings` keys (Common project) follow the entity name:

- `JobConstants.DbInstanceIdKey` → `OdsInstanceManageIdKey`
- `JobConstants.CreatePendingDbInstancesDispatcherJobName` /
  `DeletePendingDbInstancesDispatcherJobName` →
  `CreatePendingOdsInstanceManagesDispatcherJobName` / `DeletePendingOdsInstanceManagesDispatcherJobName`
- `AppSettings.CreateDbInstancesSweepIntervalInMins`, `CreateDbInstancesMaxRetryAttempts`,
  `DeleteDbInstancesSweepIntervalInMins`, `DeleteDbInstancesMaxRetryAttempts` →
  `CreateOdsInstanceManagesSweepIntervalInMins`, `CreateOdsInstanceManagesMaxRetryAttempts`,
  `DeleteOdsInstanceManagesSweepIntervalInMins`, `DeleteOdsInstanceManagesMaxRetryAttempts`
  (breaking config-key rename — call out in release notes so deployers update
  `appsettings.json`/environment overrides).

### Explicitly out of scope (left unchanged)

- `CreateInstanceJob` / `DeleteInstanceJob` (both v2's and v3's own copies) — generic names that
  don't contain "DbInstance"/"DbDataStore".
- The table's own `OdsInstanceId`/`OdsInstanceName` columns (link a row to an actual ODS
  instance) — unrelated to the FK-style `Id` field being renamed.
- No backward-compatible routes, no deprecated aliases, no config-key fallback shims.
- `NuGet.config` files (per repository convention — not touched unless explicitly requested).

## Migration

New artifact `00007-RenameDbInstancesToOdsInstanceManages.sql`, added in all four locations
(MsSql/PgSql × v2/v3 Artifacts trees), using an **in-place rename** to preserve existing data and
identity/serial sequences:

- MSSQL: `sp_rename 'adminapi.DbInstances', 'OdsInstanceManages'`, plus `sp_rename` for the two
  indexes (`IX_DbInstances_Name` → `IX_OdsInstanceManages_Name`,
  `IX_DbInstances_OdsInstanceId` → `IX_OdsInstanceManages_OdsInstanceId`).
- PostgreSQL: `ALTER TABLE adminapi."DbInstances" RENAME TO "OdsInstanceManages"`, plus
  `ALTER INDEX ... RENAME TO ...` for the same two indexes.
- Idempotent, following the existing style of prior scripts in this tree (guard with existence
  checks so re-running the migration is a no-op).

EF Core mapping updates in both `AdminApiDbContext` classes (v2 and v3):
`modelBuilder.Entity<OdsInstanceManage>().ToTable("OdsInstanceManages")...`, `DbSet<OdsInstanceManage> OdsInstanceManages`.

## v2 changes

**Folder move**: `Features\DbInstances\` → `Features\OdsInstances\Manage\` (new subfolder inside
the existing `OdsInstances` feature). Old `Features\DbInstances\` folder removed entirely.

**File renames** (namespace `EdFi.Ods.AdminApi.Features.OdsInstances.Manage`):

| Old | New |
|---|---|
| `AddDbInstance.cs` | `AddOdsInstanceManage.cs` |
| `ReadDbInstance.cs` | `ReadOdsInstanceManage.cs` |
| `DeleteDbInstance.cs` | `DeleteOdsInstanceManage.cs` |
| `DbInstanceModel.cs` | `OdsInstanceManageModel.cs` |
| `DbInstanceMapper.cs` | `OdsInstanceManageMapper.cs` |
| `DbInstanceDatabaseNameFormatter.cs` | `OdsInstanceManageDatabaseNameFormatter.cs` |

**Routes**: `POST/GET/DELETE /dbInstances...` → `POST/GET/DELETE /odsInstances/manage...`
(served as `/v2/odsInstances/manage`, still `BuildForVersions(AdminApiVersions.V2)`).

**Infrastructure layer** (stays in its existing `Infrastructure\Database\Queries` /
`Commands` / `Services\Jobs` locations — renamed in place, not moved, consistent with the
existing Features/Infrastructure architectural split):

- `IGetDbInstancesQuery`/`GetDbInstancesQuery` → `IGetOdsInstanceManagesQuery`/`GetOdsInstanceManagesQuery`
- `IGetDbInstanceByIdQuery`/`GetDbInstanceByIdQuery` → `IGetOdsInstanceManageByIdQuery`/`GetOdsInstanceManageByIdQuery`
- `AddDbInstanceCommand`/`IAddDbInstanceModel` → `AddOdsInstanceManageCommand`/`IAddOdsInstanceManageModel`
- `IDeleteDbInstanceCommand`/`DeleteDbInstanceCommand` → `IDeleteOdsInstanceManageCommand`/`DeleteOdsInstanceManageCommand`
- `CreatePendingDbInstancesDispatcherJob`/`DeletePendingDbInstancesDispatcherJob` →
  `CreatePendingOdsInstanceManagesDispatcherJob`/`DeletePendingOdsInstanceManagesDispatcherJob`

**Existing v2 `OdsInstances` files touched (not moved)**:

- `OdsInstanceWithEducationOrganizationsModel.cs`: `DbInstanceId` property → `OdsInstanceManageId`.
- `ReadEducationOrganizations.cs`: injects `IGetOdsInstanceManagesQuery` instead of
  `IGetDbInstancesQuery`; `MergeDbInstanceData` → `MergeOdsInstanceManageData`; uses
  `OdsInstanceManageStatus`.

## v3 changes

**Folder move**: `Features\DbDataStores\` → `Features\DataStores\Manage\`. Old
`Features\DbDataStores\` folder removed entirely.

**File renames** (namespace `EdFi.Ods.AdminApi.V3.Features.DataStores.Manage`):

| Old | New |
|---|---|
| `AddDbDataStore.cs` | `AddDataStoreManage.cs` |
| `ReadDbDataStore.cs` | `ReadDataStoreManage.cs` |
| `DeleteDbDataStore.cs` | `DeleteDataStoreManage.cs` |
| `DbDataStoreModel.cs` | `DataStoreManageModel.cs` |
| `DbDataStoreMapper.cs` | `DataStoreManageMapper.cs` |
| `DbDataStoreDatabaseNameFormatter.cs` | `DataStoreManageDatabaseNameFormatter.cs` |

`DbDataStoreModel`'s existing `DataStoreId`/`DataStoreName` properties carry over unchanged into
`DataStoreManageModel` — only the `DbDataStore`-named parts change.

**Routes**: `POST/GET/DELETE /dbDataStores...` → `POST/GET/DELETE /dataStores/manage...`
(served as `/v3/dataStores/manage`, still `BuildForVersions(AdminApiVersions.V3)`).

**Infrastructure layer** (renamed in place, stays under v3's own
`Infrastructure\Database\Queries`/`Commands`/`Services\Jobs`):

- `IGetDbDataStoresQuery`/`GetDbDataStoresQuery` → `IGetDataStoreManagesQuery`/`GetDataStoreManagesQuery`
- `IGetDbDataStoreByIdQuery`/`GetDbDataStoreByIdQuery` → `IGetDataStoreManageByIdQuery`/`GetDataStoreManageByIdQuery`
- `AddDbDataStoreCommand`/`IAddDbDataStoreModel` → `AddDataStoreManageCommand`/`IAddDataStoreManageModel`
- `IDeleteDbDataStoreCommand`/`DeleteDbDataStoreCommand` → `IDeleteDataStoreManageCommand`/`DeleteDataStoreManageCommand`
- `CreatePendingDbInstancesDispatcherJob`/`DeletePendingDbInstancesDispatcherJob` (v3's own
  copies) → `CreatePendingDataStoreManagesDispatcherJob`/`DeletePendingDataStoreManagesDispatcherJob`

**Existing v3 `DataStores` files touched (not moved)**:

- `ReadEducationOrganizations.cs`: injects `IGetDataStoreManagesQuery` instead of
  `IGetDbDataStoresQuery`; `MergeDbDataStoreData` → `MergeDataStoreManageData`; uses
  `OdsInstanceManageStatus` (shared enum).
- `DataStoreWithEducationOrganizationsModel.cs`: add a new `DataStoreManageId` field, closing
  the parity gap with v2's `OdsInstanceManageId`.

## Tests

**Unit tests** — renamed/moved in lockstep with production files, same mapping as above:

- v2: `Features\DbInstances\AddDbInstanceTests.cs` → `Features\OdsInstances\Manage\AddOdsInstanceManageTests.cs`
  (same pattern for `ReadDbInstanceTests.cs`/`DeleteDbInstanceTests.cs`), plus
  `Infrastructure\Database\Commands\AddDbInstanceCommandTests.cs` →
  `AddOdsInstanceManageCommandTests.cs` (and `Delete...`), `Infrastructure\Database\Queries\GetDbInstanceByIdQueryTests.cs`/
  `GetDbInstancesQueryTests.cs` → `GetOdsInstanceManageByIdQueryTests.cs`/`GetOdsInstanceManagesQueryTests.cs`,
  `Infrastructure\Services\Jobs\CreatePendingDbInstancesDispatcherJobTests.cs`/
  `DeletePendingDbInstancesDispatcherJobTests.cs` → `CreatePendingOdsInstanceManagesDispatcherJobTests.cs`/
  `DeletePendingOdsInstanceManagesDispatcherJobTests.cs`.
- v3: same pattern with `DataStoreManage` naming, e.g. `Features\DbDataStores\AddDbDataStoreTests.cs` →
  `Features\DataStores\Manage\AddDataStoreManageTests.cs`.
- **New v3 unit tests added** (closing a pre-existing coverage gap — v2 has query-level unit
  tests that v3 lacked): `GetDataStoreManagesQueryTests.cs` and
  `GetDataStoreManageByIdQueryTests.cs` in `EdFi.Ods.AdminApi.V3.UnitTests`, mirroring v2's
  existing `GetOdsInstanceManagesQueryTests.cs`/`GetOdsInstanceManageByIdQueryTests.cs`.

**DBTests** (this repo's integration-test project) — same rename pattern:
`Database\CommandTests\AddDbInstanceCommandTests.cs` → `AddOdsInstanceManageCommandTests.cs`,
`Database\QueryTests\GetDbInstancesQueryTests.cs` → `GetOdsInstanceManagesQueryTests.cs`, and v3
equivalents (`DataStoreManage`-named). `GetTenantEdOrgsByInstancesTests.cs`/
`GetTenantEdOrgsByDataStoresTests.cs` keep their file names (they don't contain the renamed term)
but have internal references updated.

**Bruno E2E collections**:

- v2: `E2E Tests\V2\...\v2\DbInstances\` → moved under `v2\OdsInstances\Manage\`; each `.bru`
  file renamed (e.g. `POST - DbInstances.bru` → `POST - OdsInstances Manage.bru`) with request
  URLs updated to `/v2/odsInstances/manage...`.
- v3: `E2E Tests\Bruno Admin API E2E 3.0\v3\DbDataStores\` → moved under
  `v3\DataStores\Manage\`; `.bru` files renamed similarly, URLs updated to
  `/v3/dataStores/manage...`.

**`docs/http/dbinstances.http`**: renamed to `docs/http/odsinstances-manage.http`. All
`/v2/dbinstances` → `/v2/odsinstances/manage` and `/v3/dbDataStores` → `/v3/dataStores/manage`.
This file currently has uncommitted local edits (manual testing notes) — the rename is applied
on top of those edits rather than discarding them.

## Risk / compatibility notes

- **Breaking change for API consumers**: old routes (`/v2/dbInstances/*`, `/v3/dbDataStores/*`)
  are removed with no aliasing. Any external client or automation hitting those paths must be
  updated to the new `/v2/odsInstances/manage/*` / `/v3/dataStores/manage/*` paths.
- **Breaking change for deployers**: `AppSettings` config keys change names (see Naming scheme
  section) — existing `appsettings.json`/environment-variable overrides referencing the old key
  names will silently stop applying (fall back to code defaults) unless updated. Call this out
  in release notes.
- **Data-preserving migration**: the table rename is in-place (`sp_rename`/`ALTER TABLE...RENAME`),
  so existing `DbInstances` rows in deployed databases carry over as `OdsInstanceManages` rows
  with no data loss and no identity/serial reseed.
