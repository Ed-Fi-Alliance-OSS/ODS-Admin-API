# Rename DbInstances/DbDataStores to OdsInstanceManage/DataStoreManage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the shared `DbInstance` entity/table to `OdsInstanceManage`, move v2's `Features\DbInstances` into `Features\OdsInstances\Manage` (`/v2/odsInstances/manage`), move v3's `Features\DbDataStores` into `Features\DataStores\Manage` (`/v3/dataStores/manage`), and update every dependent identifier, config key, test, and E2E artifact.

**Architecture:** No new architecture — this is a mechanical rename+move refactor across two parallel, already-symmetric API version trees (v2 and v3) that share one entity/table in the `EdFi.Ods.AdminApi.Common` project. Each task renames one cohesive slice (shared layer, then v2, then v3, mirroring the same steps) and ends with a build+test checkpoint.

**Tech Stack:** .NET / ASP.NET Core minimal APIs, EF Core, Quartz.NET, FluentValidation, NUnit + Shouldly, Bruno E2E collections, raw SQL migrations (MSSQL + PostgreSQL).

## Global Constraints

- Breaking change: old routes (`/v2/dbInstances/*`, `/v3/dbDataStores/*`) and old `AppSettings` config keys are removed outright — no back-compat aliases, no deprecation shims.
- Table rename is in-place (`sp_rename` / `ALTER TABLE ... RENAME TO`) to preserve existing data and identity/serial sequences — never drop-and-recreate.
- New migration artifact uses the next sequential number `00007-...sql`, added in all four locations: `Application\EdFi.Ods.AdminApi\Artifacts\{MsSql,PgSql}\Structure\Admin\` and `Application\EdFi.Ods.AdminApi.V3\Artifacts\{MsSql,PgSql}\Structure\Admin\` (only the v2 copy is actually consumed by `Install-AdminApiTables`/Docker init scripts; the v3 copy is a documentation-only duplicate kept in sync by convention).
- `CreateInstanceJob`/`DeleteInstanceJob` (both v2's and v3's own copies) are NOT renamed — generic names that don't contain "DbInstance"/"DbDataStore".
- v2's FK-style field is named `OdsInstanceManageId`; v3's is named `DataStoreManageId` — they are DTO-level fields on different response models, not the same property.
- New v3 unit tests are added for `GetDataStoreManagesQuery`/`GetDataStoreManageByIdQuery` to close a pre-existing coverage gap (v2 has these, v3 didn't).
- A new `DataStoreManageId` field is added to v3's `DataStoreWithEducationOrganizationsModel` for parity with v2's `OdsInstanceManageId`.
- `appsettings.json`/`appsettings.Development.json` config keys are renamed; `appsettings.Development.json` currently has uncommitted local edits (personal dev environment values) — preserve those values, only rename the keys.
- `docs/http/dbinstances.http` (renamed to `odsinstances-manage.http`) currently has uncommitted manual-testing edits — rebase the rename on top of those, don't discard them.
- Spec: `docs/superpowers/specs/2026-07-27-rename-dbinstances-to-odsinstancemanage-design.md`.
- v3's original `AddDbDataStore.cs`/`DeleteDbDataStore.cs` used parameter names that matched their type names in capitalization (e.g. `AddDbDataStoreCommand AddDbDataStoreCommand`, `IGetDbDataStoreByIdQuery GetDbDataStoreByIdQuery`) — an inconsistency with v2's lowerCamelCase convention. Tasks 11's rewritten code normalizes these to standard lowerCamelCase (`addDataStoreManageCommand`, `getDataStoreManageByIdQuery`) as an incidental, same-line cleanup, not a separate refactor.

---

## Master Rename Table

Reference this table from every task below instead of repeating it. Every occurrence of the "Old" token (as a whole identifier/word, not substring-inside-unrelated-word) becomes "New" in the files that task touches.

### Shared (`EdFi.Ods.AdminApi.Common` project — affects both v2 and v3)

| Old | New |
|---|---|
| `DbInstance` (entity class) | `OdsInstanceManage` |
| `DbInstances` (DbSet property / table name) | `OdsInstanceManages` |
| `DbInstanceStatus` (enum) | `OdsInstanceManageStatus` |
| `JobConstants.DbInstanceIdKey` | `JobConstants.OdsInstanceManageIdKey` |
| `JobConstants.CreatePendingDbInstancesDispatcherJobName` | `JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName` |
| `JobConstants.DeletePendingDbInstancesDispatcherJobName` | `JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName` |
| `AppSettings.CreateDbInstancesSweepIntervalInMins` | `AppSettings.CreateOdsInstanceManagesSweepIntervalInMins` |
| `AppSettings.CreateDbInstancesMaxRetryAttempts` | `AppSettings.CreateOdsInstanceManagesMaxRetryAttempts` |
| `AppSettings.DeleteDbInstancesSweepIntervalInMins` | `AppSettings.DeleteOdsInstanceManagesSweepIntervalInMins` |
| `AppSettings.DeleteDbInstancesMaxRetryAttempts` | `AppSettings.DeleteOdsInstanceManagesMaxRetryAttempts` |
| Table `adminapi.DbInstances` | `adminapi.OdsInstanceManages` |
| Index `IX_DbInstances_Name` | `IX_OdsInstanceManages_Name` |
| Index `IX_DbInstances_OdsInstanceId` | `IX_OdsInstanceManages_OdsInstanceId` |
| Constraint `PK_DbInstances` | `PK_OdsInstanceManages` |

### v2-specific (`EdFi.Ods.AdminApi` project)

| Old | New |
|---|---|
| Namespace `EdFi.Ods.AdminApi.Features.DbInstances` | `EdFi.Ods.AdminApi.Features.OdsInstances.Manage` |
| `AddDbInstance` (class/file) | `AddOdsInstanceManage` |
| `ReadDbInstance` (class/file) | `ReadOdsInstanceManage` |
| `DeleteDbInstance` (class/file) | `DeleteOdsInstanceManage` |
| `DbInstanceModel` (class/file) | `OdsInstanceManageModel` |
| `DbInstanceMapper` (class/file) | `OdsInstanceManageMapper` |
| `DbInstanceDatabaseNameFormatter` (class/file) | `OdsInstanceManageDatabaseNameFormatter` |
| `AddDbInstanceRequest` | `AddOdsInstanceManageRequest` |
| `IAddDbInstanceModel` | `IAddOdsInstanceManageModel` |
| `AddDbInstanceCommand` | `AddOdsInstanceManageCommand` |
| `IDeleteDbInstanceCommand` / `DeleteDbInstanceCommand` | `IDeleteOdsInstanceManageCommand` / `DeleteOdsInstanceManageCommand` |
| `IGetDbInstancesQuery` / `GetDbInstancesQuery` | `IGetOdsInstanceManagesQuery` / `GetOdsInstanceManagesQuery` |
| `IGetDbInstanceByIdQuery` / `GetDbInstanceByIdQuery` | `IGetOdsInstanceManageByIdQuery` / `GetOdsInstanceManageByIdQuery` |
| `CreatePendingDbInstancesDispatcherJob` (v2 copy) | `CreatePendingOdsInstanceManagesDispatcherJob` |
| `DeletePendingDbInstancesDispatcherJob` (v2 copy) | `DeletePendingOdsInstanceManagesDispatcherJob` |
| `MaxDbInstanceNameLength` | `MaxOdsInstanceManageNameLength` |
| `_validDbInstanceNamePattern` | `_validOdsInstanceManageNamePattern` |
| Route `/dbInstances` | `/odsInstances/manage` |
| `OdsInstanceWithEducationOrganizationsModel.DbInstanceId` | `OdsInstanceManageId` |
| `MergeDbInstanceData` | `MergeOdsInstanceManageData` |
| `TenantOdsInstanceModel.DbInstanceId` (`Features\Tenants\TenantDetailModel.cs`) | `OdsInstanceManageId` |
| `TenantMapper.ToUnlinkedDbInstanceModel` | `TenantMapper.ToUnlinkedOdsInstanceManageModel` |

### v3-specific (`EdFi.Ods.AdminApi.V3` project)

| Old | New |
|---|---|
| Namespace `EdFi.Ods.AdminApi.V3.Features.DbDataStores` | `EdFi.Ods.AdminApi.V3.Features.DataStores.Manage` |
| `AddDbDataStore` (class/file) | `AddDataStoreManage` |
| `ReadDbDataStore` (class/file) | `ReadDataStoreManage` |
| `DeleteDbDataStore` (class/file) | `DeleteDataStoreManage` |
| `DbDataStoreModel` (class/file) | `DataStoreManageModel` |
| `DbDataStoreMapper` (class/file) | `DataStoreManageMapper` |
| `DbDataStoreDatabaseNameFormatter` (class/file) | `DataStoreManageDatabaseNameFormatter` |
| `AddDbDataStoreRequest` | `AddDataStoreManageRequest` |
| `IAddDbDataStoreModel` | `IAddDataStoreManageModel` |
| `AddDbDataStoreCommand` | `AddDataStoreManageCommand` |
| `IDeleteDbDataStoreCommand` / `DeleteDbDataStoreCommand` | `IDeleteDataStoreManageCommand` / `DeleteDataStoreManageCommand` |
| `IGetDbDataStoresQuery` / `GetDbDataStoresQuery` | `IGetDataStoreManagesQuery` / `GetDataStoreManagesQuery` |
| `IGetDbDataStoreByIdQuery` / `GetDbDataStoreByIdQuery` | `IGetDataStoreManageByIdQuery` / `GetDataStoreManageByIdQuery` |
| `CreatePendingDbInstancesDispatcherJob` (v3 copy) | `CreatePendingDataStoreManagesDispatcherJob` |
| `DeletePendingDbInstancesDispatcherJob` (v3 copy) | `DeletePendingDataStoreManagesDispatcherJob` |
| `MaxDbDataStoreNameLength` | `MaxDataStoreManageNameLength` |
| `_validDbDataStoreNamePattern` | `_validDataStoreManageNamePattern` |
| Route `/dbDataStores` | `/dataStores/manage` |
| `MergeDbDataStoreData` | `MergeDataStoreManageData` |
| *(new field, no old counterpart)* | `DataStoreWithEducationOrganizationsModel.DataStoreManageId` |
| `TenantMapper.ToUnlinkedDbDataStoreModel` | `TenantMapper.ToUnlinkedDataStoreManageModel` |

Note: `DataStoreModel.DataStoreId`/`DataStoreModel.DataStoreType` (the existing `DataStore`'s own DTO fields, unrelated file) and `DbDataStoreModel.DataStoreId`/`DataStoreName` (already-renamed-at-DTO-level fields carried over unchanged into `DataStoreManageModel`) are **not** touched — only tokens literally containing `DbInstance`/`DbDataStore` change.

---

### Task 1: Migration — rename `adminapi.DbInstances` to `adminapi.OdsInstanceManages`

**Files:**
- Create: `Application\EdFi.Ods.AdminApi\Artifacts\MsSql\Structure\Admin\00007-RenameDbInstancesToOdsInstanceManages.sql`
- Create: `Application\EdFi.Ods.AdminApi\Artifacts\PgSql\Structure\Admin\00007-RenameDbInstancesToOdsInstanceManages.sql`
- Create: `Application\EdFi.Ods.AdminApi.V3\Artifacts\MsSql\Structure\Admin\00007-RenameDbInstancesToOdsInstanceManages.sql`
- Create: `Application\EdFi.Ods.AdminApi.V3\Artifacts\PgSql\Structure\Admin\00007-RenameDbInstancesToOdsInstanceManages.sql`

**Interfaces:**
- Produces: table `adminapi.OdsInstanceManages` with the same columns as the old `adminapi.DbInstances` (`Id, Name, OdsInstanceId, OdsInstanceName, Status, DatabaseTemplate, DatabaseName, LastRefreshed, LastModifiedDate`), indexes `IX_OdsInstanceManages_Name` / `IX_OdsInstanceManages_OdsInstanceId`. Task 2's EF Core mapping depends on this table name existing.

- [ ] **Step 1: Write the MSSQL migration script**

```sql
-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'DbInstances')
   AND NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'OdsInstanceManages')
BEGIN
    EXEC sp_rename 'adminapi.DbInstances', 'OdsInstanceManages';
    EXEC sp_rename 'adminapi.PK_DbInstances', 'PK_OdsInstanceManages';
    EXEC sp_rename 'adminapi.OdsInstanceManages.IX_DbInstances_Name', 'IX_OdsInstanceManages_Name', 'INDEX';
    EXEC sp_rename 'adminapi.OdsInstanceManages.IX_DbInstances_OdsInstanceId', 'IX_OdsInstanceManages_OdsInstanceId', 'INDEX';
END
```

Save this file to all two MSSQL locations (`Application\EdFi.Ods.AdminApi\Artifacts\MsSql\Structure\Admin\` and `Application\EdFi.Ods.AdminApi.V3\Artifacts\MsSql\Structure\Admin\`) — byte-identical, matching the existing convention where `00005-CreateDbInstances.sql` is duplicated across both trees.

- [ ] **Step 2: Write the PostgreSQL migration script**

```sql
-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'adminapi' AND table_name = 'dbinstances')
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'adminapi' AND table_name = 'odsinstancemanages')
    THEN
        ALTER TABLE adminapi.DbInstances RENAME TO OdsInstanceManages;
        ALTER TABLE adminapi.OdsInstanceManages RENAME CONSTRAINT pk_dbinstances TO pk_odsinstancemanages;
        ALTER INDEX adminapi.idx_dbinstances_name RENAME TO idx_odsinstancemanages_name;
        ALTER INDEX adminapi.idx_dbinstances_odsinstanceid RENAME TO idx_odsinstancemanages_odsinstanceid;
    END IF;
END $$;
```

Save this file to both PgSql locations (`Application\EdFi.Ods.AdminApi\Artifacts\PgSql\Structure\Admin\` and `Application\EdFi.Ods.AdminApi.V3\Artifacts\PgSql\Structure\Admin\`) — byte-identical.

- [ ] **Step 3: Verify against a local database**

Run: `./eng/run-dbup-migrations.ps1` (or the equivalent Docker init flow already used locally) against a database that has the old `00001`-`00006` scripts applied, then confirm:
- MSSQL: `SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID('adminapi');` shows `OdsInstanceManages`, not `DbInstances`.
- PostgreSQL: `\dt adminapi.*` shows `odsinstancemanages`, not `dbinstances`.
- Existing rows (if any were present before the rename) are unchanged in count and content.
- Re-running the script a second time is a no-op (idempotency guard prevents re-running `sp_rename`/`ALTER TABLE RENAME` once already renamed).

- [ ] **Step 4: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/Artifacts/MsSql/Structure/Admin/00007-RenameDbInstancesToOdsInstanceManages.sql" \
        "Application/EdFi.Ods.AdminApi/Artifacts/PgSql/Structure/Admin/00007-RenameDbInstancesToOdsInstanceManages.sql" \
        "Application/EdFi.Ods.AdminApi.V3/Artifacts/MsSql/Structure/Admin/00007-RenameDbInstancesToOdsInstanceManages.sql" \
        "Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Admin/00007-RenameDbInstancesToOdsInstanceManages.sql"
git commit -m "Add migration renaming adminapi.DbInstances to adminapi.OdsInstanceManages"
```

---

### Task 2: Rename shared entity, enum, JobConstants, AppSettings (Common project + both DbContexts)

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.Common\Infrastructure\Models\DbInstance.cs` → rename file to `OdsInstanceManage.cs`
- Modify: `Application\EdFi.Ods.AdminApi.Common\Constants\Constants.cs`
- Modify: `Application\EdFi.Ods.AdminApi.Common\Infrastructure\Jobs\JobConstants.cs`
- Modify: `Application\EdFi.Ods.AdminApi.Common\Settings\AppSettings.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\AdminApiDbContext.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\AdminApiDbContext.cs`
- Modify: `Application\EdFi.Ods.AdminApi\appsettings.json`
- Modify: `Application\EdFi.Ods.AdminApi\appsettings.Development.json`
- Modify: `Application\EdFi.Ods.AdminApi.V3\appsettings.json`

**Interfaces:**
- Produces: `OdsInstanceManage` entity class (namespace `EdFi.Ods.AdminApi.Common.Infrastructure.Models`), `OdsInstanceManageStatus` enum (namespace `EdFi.Ods.AdminApi.Common.Constants`), `JobConstants.OdsInstanceManageIdKey`/`CreatePendingOdsInstanceManagesDispatcherJobName`/`DeletePendingOdsInstanceManagesDispatcherJobName`, `AppSettings.CreateOdsInstanceManagesSweepIntervalInMins`/`CreateOdsInstanceManagesMaxRetryAttempts`/`DeleteOdsInstanceManagesSweepIntervalInMins`/`DeleteOdsInstanceManagesMaxRetryAttempts`. Every later task in this plan consumes these exact names.

- [ ] **Step 1: Rename the entity file and class**

```bash
git mv "Application/EdFi.Ods.AdminApi.Common/Infrastructure/Models/DbInstance.cs" "Application/EdFi.Ods.AdminApi.Common/Infrastructure/Models/OdsInstanceManage.cs"
```

Edit the file content to:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Models;

public class OdsInstanceManage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? OdsInstanceId { get; set; }

    [StringLength(100)]
    public string? OdsInstanceName { get; set; }

    [Required]
    [StringLength(75)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DatabaseTemplate { get; set; } = string.Empty;

    [StringLength(255)]
    public string? DatabaseName { get; set; }

    [Required]
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;

    public DateTime? LastModifiedDate { get; set; }
}
```

- [ ] **Step 2: Rename the `DbInstanceStatus` enum in `Constants.cs`**

In `Application\EdFi.Ods.AdminApi.Common\Constants\Constants.cs`, replace:

```csharp
public enum DbInstanceStatus
```

with:

```csharp
public enum OdsInstanceManageStatus
```

(the enum's member list — `PendingCreate, Created, CreateInProgress, CreateFailed, CreateError, PendingDelete, DeleteInProgress, Deleted, DeleteFailed, DeleteError` — is unchanged.)

- [ ] **Step 3: Rename `JobConstants` members**

In `Application\EdFi.Ods.AdminApi.Common\Infrastructure\Jobs\JobConstants.cs`, replace:

```csharp
    public const string DbInstanceIdKey = "DbInstanceId";
    public const string OdsInstanceIdKey = "OdsInstanceId";
    public const string CreateInstanceJobName = "CreateInstanceJob";
    public const string CreatePendingDbInstancesDispatcherJobName = "CreatePendingDbInstancesDispatcherJob";
    public const string DeleteInstanceJobName = "DeleteInstanceJob";
    public const string DeletePendingDbInstancesDispatcherJobName = "DeletePendingDbInstancesDispatcherJob";
```

with:

```csharp
    public const string OdsInstanceManageIdKey = "OdsInstanceManageId";
    public const string OdsInstanceIdKey = "OdsInstanceId";
    public const string CreateInstanceJobName = "CreateInstanceJob";
    public const string CreatePendingOdsInstanceManagesDispatcherJobName = "CreatePendingOdsInstanceManagesDispatcherJob";
    public const string DeleteInstanceJobName = "DeleteInstanceJob";
    public const string DeletePendingOdsInstanceManagesDispatcherJobName = "DeletePendingOdsInstanceManagesDispatcherJob";
```

(`OdsInstanceIdKey`, `CreateInstanceJobName`, `DeleteInstanceJobName`, `RefreshEducationOrganizationsJobName`, `RunIdKey`, `JobTypeKey`, `TenantNameKey` are unrelated and unchanged.)

- [ ] **Step 4: Rename `AppSettings` properties**

In `Application\EdFi.Ods.AdminApi.Common\Settings\AppSettings.cs`, replace:

```csharp
    public int CreateDbInstancesSweepIntervalInMins { get; set; } = 5;
    public int CreateDbInstancesMaxRetryAttempts { get; set; } = 3;
    public int DeleteDbInstancesSweepIntervalInMins { get; set; } = 5;
    public int DeleteDbInstancesMaxRetryAttempts { get; set; } = 3;
```

with:

```csharp
    public int CreateOdsInstanceManagesSweepIntervalInMins { get; set; } = 5;
    public int CreateOdsInstanceManagesMaxRetryAttempts { get; set; } = 3;
    public int DeleteOdsInstanceManagesSweepIntervalInMins { get; set; } = 5;
    public int DeleteOdsInstanceManagesMaxRetryAttempts { get; set; } = 3;
```

- [ ] **Step 5: Update both `AdminApiDbContext` classes**

In `Application\EdFi.Ods.AdminApi\Infrastructure\AdminApiDbContext.cs` (and identically in `Application\EdFi.Ods.AdminApi.V3\Infrastructure\AdminApiDbContext.cs`), replace:

```csharp
    public DbSet<DbInstance> DbInstances { get; set; }
```

with:

```csharp
    public DbSet<OdsInstanceManage> OdsInstanceManages { get; set; }
```

and replace:

```csharp
        modelBuilder.Entity<DbInstance>().ToTable("DbInstances").HasKey(t => t.Id);
```

with:

```csharp
        modelBuilder.Entity<OdsInstanceManage>().ToTable("OdsInstanceManages").HasKey(t => t.Id);
```

- [ ] **Step 6: Update `appsettings.json` (v2) config keys**

In `Application\EdFi.Ods.AdminApi\appsettings.json`, replace the four keys (preserving their existing values `120`/`3`/`120`/`3`):

```json
        "CreateDbInstancesSweepIntervalInMins": 120,
        "CreateDbInstancesMaxRetryAttempts": 3,
        "DeleteDbInstancesSweepIntervalInMins": 120,
        "DeleteDbInstancesMaxRetryAttempts": 3,
```

with:

```json
        "CreateOdsInstanceManagesSweepIntervalInMins": 120,
        "CreateOdsInstanceManagesMaxRetryAttempts": 3,
        "DeleteOdsInstanceManagesSweepIntervalInMins": 120,
        "DeleteOdsInstanceManagesMaxRetryAttempts": 3,
```

- [ ] **Step 7: Update `appsettings.Development.json` (v2) config keys**

This file currently has uncommitted local edits. Read the file first to get its current exact values, then replace only the four key names (`CreateDbInstancesSweepIntervalInMins`, `CreateDbInstancesMaxRetryAttempts`, `DeleteDbInstancesSweepIntervalInMins`, `DeleteDbInstancesMaxRetryAttempts`) with their `OdsInstanceManages`-named equivalents, preserving whatever values are currently present (as of this plan's writing: `5`, `3`, `5`, `3`) and every other uncommitted edit in the file untouched.

- [ ] **Step 8: Update `appsettings.json` (v3) config keys**

Same four-key rename as Step 6, applied to `Application\EdFi.Ods.AdminApi.V3\appsettings.json` (existing values `120`/`3`/`120`/`3`).

- [ ] **Step 9: Build to confirm no compile errors yet from callers**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln` (or the solution file this repo uses — check for a `.sln` at the repo root or under `Application\`).
Expected: FAILS — callers in v2/v3 Features/Infrastructure/Jobs still reference the old `DbInstance`/`DbInstanceStatus`/old `JobConstants`/old `AppSettings` names. This is expected at this checkpoint; Tasks 3–15 fix each caller. Confirm the failures are all in files this plan's later tasks will touch (grep the build output for `DbInstance`/`DbDataStore` to sanity-check no unexpected file is affected).

- [ ] **Step 10: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.Common/Infrastructure/Models/OdsInstanceManage.cs" \
        "Application/EdFi.Ods.AdminApi.Common/Constants/Constants.cs" \
        "Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/JobConstants.cs" \
        "Application/EdFi.Ods.AdminApi.Common/Settings/AppSettings.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/AdminApiDbContext.cs" \
        "Application/EdFi.Ods.AdminApi/appsettings.json" \
        "Application/EdFi.Ods.AdminApi/appsettings.Development.json" \
        "Application/EdFi.Ods.AdminApi.V3/appsettings.json"
git commit -m "Rename shared DbInstance entity/enum/config keys to OdsInstanceManage"
```

Note: this commit intentionally leaves the solution non-building until Task 3 onward completes — that's expected for a rename this wide; each subsequent task is reviewed independently but the "build passes" checkpoint only becomes true again at the end of Task 9 (v2 side fully done) and again at the end of Task 15 (v3 side fully done). If your workflow requires green-build-per-commit, squash Tasks 2–9 (or 2–15) before merging instead of committing at each intermediate step — call this out to whoever reviews the branch.

---

### Task 3: v2 Infrastructure layer rename (Queries, Commands, Jobs)

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Database\Queries\GetDbInstancesQuery.cs` → rename to `GetOdsInstanceManagesQuery.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Database\Queries\GetDbInstanceByIdQuery.cs` → rename to `GetOdsInstanceManageByIdQuery.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Database\Commands\AddDbInstanceCommand.cs` → rename to `AddOdsInstanceManageCommand.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Database\Commands\DeleteDbInstanceCommand.cs` → rename to `DeleteOdsInstanceManageCommand.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Jobs\CreatePendingDbInstancesDispatcherJob.cs` → rename to `CreatePendingOdsInstanceManagesDispatcherJob.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Jobs\DeletePendingDbInstancesDispatcherJob.cs` → rename to `DeletePendingOdsInstanceManagesDispatcherJob.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Jobs\CreateInstanceJob.cs` (renamed in place, filename unchanged)
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Jobs\DeleteInstanceJob.cs` (renamed in place, filename unchanged)

**Interfaces:**
- Consumes: `OdsInstanceManage` entity, `OdsInstanceManageStatus` enum, `JobConstants.OdsInstanceManageIdKey`, `JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName`/`DeletePendingOdsInstanceManagesDispatcherJobName`, `AppSettings.CreateOdsInstanceManagesMaxRetryAttempts`/`DeleteOdsInstanceManagesMaxRetryAttempts` (all from Task 2), `AdminApiDbContext.OdsInstanceManages` (from Task 2).
- Produces: `IGetOdsInstanceManagesQuery`/`GetOdsInstanceManagesQuery`, `IGetOdsInstanceManageByIdQuery`/`GetOdsInstanceManageByIdQuery`, `AddOdsInstanceManageCommand`/`IAddOdsInstanceManageModel`, `IDeleteOdsInstanceManageCommand`/`DeleteOdsInstanceManageCommand`, `CreatePendingOdsInstanceManagesDispatcherJob`, `DeletePendingOdsInstanceManagesDispatcherJob` — consumed by Task 4 (feature files) and Task 6 (wiring).

- [ ] **Step 1: Rename and update the Queries**

```bash
git mv "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetDbInstancesQuery.cs" "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetOdsInstanceManagesQuery.cs"
git mv "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetDbInstanceByIdQuery.cs" "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetOdsInstanceManageByIdQuery.cs"
```

`GetOdsInstanceManagesQuery.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetOdsInstanceManagesQuery
{
    List<OdsInstanceManage> Execute(CommonQueryParams commonQueryParams, int? id, string? name);
}

public class GetOdsInstanceManagesQuery : IGetOdsInstanceManagesQuery
{
    private readonly AdminApiDbContext _context;
    private readonly IOptions<AppSettings> _options;

    public GetOdsInstanceManagesQuery(AdminApiDbContext context, IOptions<AppSettings> options)
    {
        _context = context;
        _options = options;
    }

    public List<OdsInstanceManage> Execute(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        return _context.OdsInstanceManages
            .Where(d => id == null || d.Id == id)
            .Where(d => name == null || d.Name == name)
            .OrderBy(d => d.Id)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
```

`GetOdsInstanceManageByIdQuery.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetOdsInstanceManageByIdQuery
{
    OdsInstanceManage? Execute(int id);
}

public class GetOdsInstanceManageByIdQuery : IGetOdsInstanceManageByIdQuery
{
    private readonly AdminApiDbContext _context;

    public GetOdsInstanceManageByIdQuery(AdminApiDbContext context)
    {
        _context = context;
    }

    public OdsInstanceManage? Execute(int id)
    {
        return _context.OdsInstanceManages.SingleOrDefault(d => d.Id == id);
    }
}
```

- [ ] **Step 2: Rename and update the Commands**

```bash
git mv "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Commands/AddDbInstanceCommand.cs" "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Commands/AddOdsInstanceManageCommand.cs"
git mv "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Commands/DeleteDbInstanceCommand.cs" "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Commands/DeleteOdsInstanceManageCommand.cs"
```

`AddOdsInstanceManageCommand.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public class AddOdsInstanceManageCommand
{
    private readonly AdminApiDbContext _context;

    public AddOdsInstanceManageCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public OdsInstanceManage Execute(IAddOdsInstanceManageModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ArgumentException("Name is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(model.DatabaseTemplate))
            throw new ArgumentException("DatabaseTemplate is required.", nameof(model));

        var now = DateTime.UtcNow;

        var odsInstanceManage = new OdsInstanceManage
        {
            Name = model.Name.Trim(),
            DatabaseTemplate = model.DatabaseTemplate.Trim(),
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            OdsInstanceId = null,
            OdsInstanceName = null,
            DatabaseName = null,
            LastRefreshed = now,
            LastModifiedDate = now
        };

        _context.OdsInstanceManages.Add(odsInstanceManage);
        _context.SaveChanges();
        return odsInstanceManage;
    }
}

public interface IAddOdsInstanceManageModel
{
    string? Name { get; }
    string? DatabaseTemplate { get; }
}
```

`DeleteOdsInstanceManageCommand.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IDeleteOdsInstanceManageCommand
{
    void Execute(int id);
}

public class DeleteOdsInstanceManageCommand : IDeleteOdsInstanceManageCommand
{
    private readonly AdminApiDbContext _context;

    public DeleteOdsInstanceManageCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var odsInstanceManage =
            _context.OdsInstanceManages.Find(id)
            ?? throw new NotFoundException<int>("odsInstanceManage", id);

        if (odsInstanceManage.Status != OdsInstanceManageStatus.Created.ToString())
            throw new NotFoundException<int>("odsInstanceManage", id);

        odsInstanceManage.Status = OdsInstanceManageStatus.PendingDelete.ToString();
        odsInstanceManage.LastModifiedDate = DateTime.UtcNow;

        _context.SaveChanges();
    }
}
```

- [ ] **Step 3: Rename and update the dispatcher Jobs**

```bash
git mv "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreatePendingDbInstancesDispatcherJob.cs" "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreatePendingOdsInstanceManagesDispatcherJob.cs"
git mv "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/DeletePendingDbInstancesDispatcherJob.cs" "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/DeletePendingOdsInstanceManagesDispatcherJob.cs"
```

`CreatePendingOdsInstanceManagesDispatcherJob.cs` (apply the Master Rename Table to the existing content — class name, constructor param, local variable names `eligibleDbInstances`→`eligibleOdsInstanceManages`, `dbInstance`→`odsInstanceManage` loop variable, `JobConstants.DbInstanceIdKey`→`JobConstants.OdsInstanceManageIdKey`, `DbInstanceStatus`→`OdsInstanceManageStatus`, `_options.Value.CreateDbInstancesMaxRetryAttempts`→`_options.Value.CreateOdsInstanceManagesMaxRetryAttempts`, `adminApiDbContext.DbInstances`→`adminApiDbContext.OdsInstanceManages`):

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class CreatePendingOdsInstanceManagesDispatcherJob(
    ILogger<CreatePendingOdsInstanceManagesDispatcherJob> logger,
    IJobStatusService jobStatusService,
    AdminApiDbContext dbContext,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    IOptions<AppSettings> options)
    : AdminApiQuartzJobBase(logger, jobStatusService)
{
    private const int DefaultMaxRetryAttempts = 3;

    private readonly AdminApiDbContext _dbContext = dbContext;
    private readonly ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = tenantSpecificDbContextProvider;
    private readonly IOptions<AppSettings> _options = options;

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var tenantName = GetTenantName(context, multiTenancyEnabled);
        AdminApiDbContext? tenantAdminApiDbContext = null;
        var adminApiDbContext = _dbContext;

        try
        {
            if (multiTenancyEnabled)
            {
                tenantAdminApiDbContext = _tenantSpecificDbContextProvider.GetAdminApiDbContext(tenantName!);
                adminApiDbContext = tenantAdminApiDbContext;
            }

            var eligibleOdsInstanceManages = await adminApiDbContext.OdsInstanceManages
                .Where(instance => instance.Status == OdsInstanceManageStatus.PendingCreate.ToString() || instance.Status == OdsInstanceManageStatus.CreateFailed.ToString())
                .OrderBy(instance => instance.Id)
                .ToListAsync();

            foreach (var odsInstanceManage in eligibleOdsInstanceManages)
            {
                if (string.Equals(odsInstanceManage.Status, OdsInstanceManageStatus.PendingCreate.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    await ScheduleCreateJobAsync(context, odsInstanceManage.Id, tenantName);
                    continue;
                }

                if (!await IsRetryEligibleAsync(adminApiDbContext, odsInstanceManage, tenantName))
                {
                    odsInstanceManage.Status = OdsInstanceManageStatus.CreateError.ToString();
                    odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                    odsInstanceManage.LastRefreshed = DateTime.UtcNow;
                    await adminApiDbContext.SaveChangesAsync();
                    continue;
                }

                odsInstanceManage.Status = OdsInstanceManageStatus.PendingCreate.ToString();
                odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                odsInstanceManage.LastRefreshed = DateTime.UtcNow;
                await adminApiDbContext.SaveChangesAsync();

                await ScheduleCreateJobAsync(context, odsInstanceManage.Id, tenantName);
            }
        }
        finally
        {
            if (tenantAdminApiDbContext is not null)
            {
                await tenantAdminApiDbContext.DisposeAsync();
            }
        }
    }

    private async Task<bool> IsRetryEligibleAsync(AdminApiDbContext adminApiDbContext, OdsInstanceManage odsInstanceManage, string? tenantName)
    {
        var maxRetryAttempts = _options.Value.CreateOdsInstanceManagesMaxRetryAttempts > 0
            ? _options.Value.CreateOdsInstanceManagesMaxRetryAttempts
            : DefaultMaxRetryAttempts;

        var jobIdPrefix = $"{CreateInstanceJob.BuildJobIdentity(odsInstanceManage.Id, tenantName)}_";
        var errorCount = await adminApiDbContext.JobStatuses
            .CountAsync(status => status.JobId.StartsWith(jobIdPrefix) && status.Status == QuartzJobStatus.Error.ToString());

        return errorCount < maxRetryAttempts;
    }

    private static async Task ScheduleCreateJobAsync(IJobExecutionContext context, int odsInstanceManageId, string? tenantName)
    {
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.OdsInstanceManageIdKey] = odsInstanceManageId
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobData[JobConstants.TenantNameKey] = tenantName;
        }

        await QuartzJobScheduler.ScheduleJob<CreateInstanceJob>(
            context.Scheduler,
            CreateInstanceJob.CreateJobKey(odsInstanceManageId, tenantName),
            jobData,
            startImmediately: true);
    }

    private static string? GetTenantName(IJobExecutionContext context, bool multiTenancyEnabled)
    {
        if (!multiTenancyEnabled)
        {
            return null;
        }

        var tenantName = context.MergedJobDataMap.ContainsKey(JobConstants.TenantNameKey)
            ? context.MergedJobDataMap.GetString(JobConstants.TenantNameKey)
            : null;

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new InvalidOperationException(
                $"{JobConstants.TenantNameKey} must be provided when multi-tenancy is enabled.");
        }

        return tenantName;
    }
}
```

`DeletePendingOdsInstanceManagesDispatcherJob.cs` — apply the identical transformation (class name, `eligibleOdsInstanceManages`, `odsInstanceManage` loop var, `DeleteOdsInstanceManagesMaxRetryAttempts`, `PendingDelete`/`DeleteFailed`/`DeleteError` status branches instead of Create's) mirroring `CreatePendingOdsInstanceManagesDispatcherJob.cs` above but keeping the Delete-specific status logic and calling `DeleteInstanceJob` (unchanged name) instead of `CreateInstanceJob`.

- [ ] **Step 4: Update `CreateInstanceJob.cs` and `DeleteInstanceJob.cs` in place (filenames unchanged, class names unchanged — only internal references)**

In `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Jobs\CreateInstanceJob.cs`:
- Replace `using EdFi.Ods.AdminApi.Features.DbInstances;` with `using EdFi.Ods.AdminApi.Features.OdsInstances.Manage;` (namespace of `OdsInstanceManageDatabaseNameFormatter`, produced by Task 4 — this file will not compile until Task 4 completes; that's expected, note it in the PR).
- Replace every `DbInstance? dbInstance` / `DbInstance dbInstance` type reference with `OdsInstanceManage? odsInstanceManage` / `OdsInstanceManage odsInstanceManage` (rename the local variable throughout the method body too, e.g. `dbInstance.Status`→`odsInstanceManage.Status`).
- Replace `adminApiDbContext.DbInstances` with `adminApiDbContext.OdsInstanceManages`.
- Replace `DbInstanceStatus` with `OdsInstanceManageStatus` (all switch/enum references).
- Replace `JobConstants.DbInstanceIdKey` with `JobConstants.OdsInstanceManageIdKey`.
- Replace `DbInstanceDatabaseNameFormatter` with `OdsInstanceManageDatabaseNameFormatter`.
- Replace `int dbInstanceId` parameter names with `int odsInstanceManageId` in `CreateJobKey`/`BuildJobIdentity`.
- Update the comment `// The CreatePendingDbInstancesDispatcherJob may have already scheduled...` (if present) and any other prose comment mentioning "DbInstance" to say "OdsInstanceManage" instead, and `CreatePendingDbInstancesDispatcherJob` to `CreatePendingOdsInstanceManagesDispatcherJob`.

In `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Jobs\DeleteInstanceJob.cs`: apply the identical set of replacements (this file doesn't reference `DbInstanceDatabaseNameFormatter`, so skip that one).

- [ ] **Step 5: Build to confirm the Infrastructure layer compiles in isolation**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`
Expected: still FAILS — `Features\DbInstances\*` (Task 4), `Features\OdsInstances\*` (Task 5), `Program.cs`/`WebApplicationBuilderExtensions.cs` (Task 6) haven't been updated yet. Confirm the remaining errors are now confined to those files only (no errors left in `Infrastructure\Database\*` or `Infrastructure\Services\Jobs\*`).

- [ ] **Step 6: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetOdsInstanceManagesQuery.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetOdsInstanceManageByIdQuery.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Commands/AddOdsInstanceManageCommand.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Commands/DeleteOdsInstanceManageCommand.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreatePendingOdsInstanceManagesDispatcherJob.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/DeletePendingOdsInstanceManagesDispatcherJob.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreateInstanceJob.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/DeleteInstanceJob.cs"
git commit -m "Rename v2 Infrastructure Queries/Commands/Jobs to OdsInstanceManage naming"
```

---

### Task 4: v2 Feature folder move — `Features\DbInstances` → `Features\OdsInstances\Manage`

**Files:**
- Create: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\Manage\AddOdsInstanceManage.cs`
- Create: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\Manage\ReadOdsInstanceManage.cs`
- Create: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\Manage\DeleteOdsInstanceManage.cs`
- Create: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\Manage\OdsInstanceManageModel.cs`
- Create: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\Manage\OdsInstanceManageMapper.cs`
- Create: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\Manage\OdsInstanceManageDatabaseNameFormatter.cs`
- Delete: `Application\EdFi.Ods.AdminApi\Features\DbInstances\` (entire folder, all 6 old files)

**Interfaces:**
- Consumes: `AddOdsInstanceManageCommand`/`IAddOdsInstanceManageModel`, `IGetOdsInstanceManagesQuery`/`IGetOdsInstanceManageByIdQuery`, `IDeleteOdsInstanceManageCommand` (Task 3), `OdsInstanceManage`/`OdsInstanceManageStatus` (Task 2), `JobConstants.OdsInstanceManageIdKey` (Task 2), `CreateInstanceJob`/`DeleteInstanceJob` (Task 3, unchanged names).
- Produces: routes `POST/GET/DELETE /odsInstances/manage` under `EdFi.Ods.AdminApi.Features.OdsInstances.Manage`, consumed by Task 9 (Bruno E2E) and Task 17 (`.http` file).

- [ ] **Step 1: Move the folder with git so history follows, then delete leftovers**

```bash
git mv "Application/EdFi.Ods.AdminApi/Features/DbInstances/AddDbInstance.cs" "Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/AddOdsInstanceManage.cs"
git mv "Application/EdFi.Ods.AdminApi/Features/DbInstances/ReadDbInstance.cs" "Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/ReadOdsInstanceManage.cs"
git mv "Application/EdFi.Ods.AdminApi/Features/DbInstances/DeleteDbInstance.cs" "Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/DeleteOdsInstanceManage.cs"
git mv "Application/EdFi.Ods.AdminApi/Features/DbInstances/DbInstanceModel.cs" "Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/OdsInstanceManageModel.cs"
git mv "Application/EdFi.Ods.AdminApi/Features/DbInstances/DbInstanceMapper.cs" "Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/OdsInstanceManageMapper.cs"
git mv "Application/EdFi.Ods.AdminApi/Features/DbInstances/DbInstanceDatabaseNameFormatter.cs" "Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/OdsInstanceManageDatabaseNameFormatter.cs"
```

Confirm `Application\EdFi.Ods.AdminApi\Features\DbInstances\` is now empty and remove the empty folder if git/your OS leaves it behind.

- [ ] **Step 2: Replace `AddOdsInstanceManage.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public class AddOdsInstanceManage : IFeature
{
    private const int MaxSynchronizedNameLength = 100;
    private const int MaxOdsInstanceManageNameLength = MaxSynchronizedNameLength;
    private static readonly Regex _validOdsInstanceManageNamePattern = new(
        "^[A-Za-z0-9 _]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/odsInstances/manage", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public async static Task<IResult> Handle(
        Validator validator,
        AddOdsInstanceManageCommand addOdsInstanceManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        AddOdsInstanceManageRequest request)
    {
        await validator.GuardAsync(request);

        var added = addOdsInstanceManageCommand.Execute(request);

        var tenantIdentifier = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;

        var jobBuilder = JobBuilder.Create<CreateInstanceJob>()
            .WithIdentity(CreateInstanceJob.CreateJobKey(added.Id, tenantIdentifier))
            .UsingJobData(JobConstants.OdsInstanceManageIdKey, added.Id);

        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            jobBuilder = jobBuilder.UsingJobData(JobConstants.TenantNameKey, tenantIdentifier);
        }

        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler();

        try
        {
            await scheduler.ScheduleJob(jobBuilder.Build(), trigger);
        }
        catch (ObjectAlreadyExistsException)
        {
            // The CreatePendingOdsInstanceManagesDispatcherJob may have already scheduled this job
            // (e.g. it fired between the DB insert and this ScheduleJob call). Treat duplicate
            // scheduling as success — the job is already queued and will process the OdsInstanceManage.
        }

        return Results.Accepted($"/odsinstances/manage/{added.Id}", null);
    }

    [SwaggerSchema(Title = "AddOdsInstanceManageRequest")]
    public class AddOdsInstanceManageRequest : IAddOdsInstanceManageModel
    {
        [SwaggerSchema(Description = "Name of the database instance", Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = "Database template to use for the instance", Nullable = false)]
        public string? DatabaseTemplate { get; set; }
    }

    public class Validator : AbstractValidator<AddOdsInstanceManageRequest>
    {
        private static readonly string[] _validDatabaseTemplates = Enum.GetNames<SandboxType>();
        private readonly AdminApiDbContext _adminApiDbContext;
        private readonly IUsersContext _usersContext;

        public Validator(AdminApiDbContext adminApiDbContext, IUsersContext usersContext)
        {
            _adminApiDbContext = adminApiDbContext;
            _usersContext = usersContext;

            RuleFor(m => m.Name)
                .NotEmpty()
                .MaximumLength(MaxOdsInstanceManageNameLength)
                .WithMessage($"'{{PropertyName}}' must be {MaxOdsInstanceManageNameLength} characters or fewer so the synchronized ODS instance name fits within {MaxSynchronizedNameLength} characters.")
                .Matches(_validOdsInstanceManageNamePattern)
                .WithMessage("'{PropertyName}' may only contain letters, numbers, spaces, and underscores.");

            RuleFor(m => m.DatabaseTemplate).NotEmpty().MaximumLength(100)
                .Must(t => t != null && _validDatabaseTemplates.Contains(t))
                .WithMessage($"'{{PropertyValue}}' is not a valid database template. Allowed values are: {string.Join(", ", _validDatabaseTemplates)}.");

            RuleFor(m => m).CustomAsync(async (request, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name)
                    || string.IsNullOrWhiteSpace(request.DatabaseTemplate)
                    || request.Name.Length > MaxOdsInstanceManageNameLength
                    || !_validOdsInstanceManageNamePattern.IsMatch(request.Name)
                    || !_validDatabaseTemplates.Contains(request.DatabaseTemplate))
                {
                    return;
                }

                var normalizedName = request.Name.Trim();

                if (await _adminApiDbContext.OdsInstanceManages.AnyAsync(instance => instance.Name == normalizedName && instance.Status != OdsInstanceManageStatus.Deleted.ToString(), cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddOdsInstanceManageRequest.Name),
                        $"An OdsInstanceManage named '{normalizedName}' already exists.");
                    return;
                }

                if (await _usersContext.OdsInstances.AnyAsync(instance => instance.Name == normalizedName, cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddOdsInstanceManageRequest.Name),
                        $"An OdsInstance named '{normalizedName}' already exists.");
                    return;
                }

                var databaseName = OdsInstanceManageDatabaseNameFormatter.Build(request.Name, request.DatabaseTemplate);

                if (databaseName.Length > OdsInstanceManageDatabaseNameFormatter.MaxPortableDatabaseNameLength)
                {
                    context.AddFailure(
                        nameof(AddOdsInstanceManageRequest.Name),
                        $"The generated database name '{databaseName}' exceeds the portable limit of {OdsInstanceManageDatabaseNameFormatter.MaxPortableDatabaseNameLength} characters. Shorten Name or DatabaseTemplate.");
                }
            });
        }
    }
}
```

- [ ] **Step 3: Replace `ReadOdsInstanceManage.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public class ReadOdsInstanceManage : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances/manage", GetOdsInstanceManages)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceManageModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances/manage/{id}", GetOdsInstanceManage)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceManageModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static Task<IResult> GetOdsInstanceManages(IGetOdsInstanceManagesQuery query,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name)
    {
        var list = OdsInstanceManageMapper.ToModelList(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetOdsInstanceManage(IGetOdsInstanceManageByIdQuery query, int id)
    {
        var odsInstanceManage = query.Execute(id);
        if (odsInstanceManage == null)
        {
            throw new NotFoundException<int>("odsInstanceManage", id);
        }
        var model = OdsInstanceManageMapper.ToModel(odsInstanceManage);
        return Task.FromResult(Results.Ok(model));
    }
}
```

- [ ] **Step 4: Replace `DeleteOdsInstanceManage.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public class DeleteOdsInstanceManage : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapDelete(endpoints, "/odsInstances/manage/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(
        IGetOdsInstanceManageByIdQuery getOdsInstanceManageByIdQuery,
        IDeleteOdsInstanceManageCommand deleteOdsInstanceManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        int id
    )
    {
        var odsInstanceManage = getOdsInstanceManageByIdQuery.Execute(id);
        if (odsInstanceManage is null)
            throw new NotFoundException<int>("odsInstanceManage", id);

        if (odsInstanceManage.Status == OdsInstanceManageStatus.Deleted.ToString())
            throw new NotFoundException<int>("odsInstanceManage", id);

        var blockingMessage = GetBlockingStatusMessage(odsInstanceManage.Status);
        if (blockingMessage is not null)
            throw new ValidationException([new ValidationFailure(nameof(id), blockingMessage)]);

        deleteOdsInstanceManageCommand.Execute(id);

        var tenantName = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.OdsInstanceManageIdKey] = id
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobData[JobConstants.TenantNameKey] = tenantName;
        }

        var scheduler = await schedulerFactory.GetScheduler();

        try
        {
            await QuartzJobScheduler.ScheduleJob<DeleteInstanceJob>(
                scheduler,
                DeleteInstanceJob.CreateJobKey(id, tenantName),
                jobData,
                startImmediately: true);
        }
        catch (ObjectAlreadyExistsException)
        {
            // The DeletePendingOdsInstanceManagesDispatcherJob may have already scheduled this job.
            // Treat duplicate scheduling as success — the job is already queued.
        }

        return Results.NoContent();
    }

    private static string? GetBlockingStatusMessage(string status)
    {
        if (Enum.TryParse<OdsInstanceManageStatus>(status, ignoreCase: true, out var parsed))
        {
            return parsed switch
            {
                OdsInstanceManageStatus.PendingCreate    => "OdsInstanceManage is being provisioned. Wait for creation to complete.",
                OdsInstanceManageStatus.CreateInProgress => "OdsInstanceManage is currently being provisioned. Wait for creation to complete.",
                OdsInstanceManageStatus.CreateFailed     => "OdsInstanceManage creation failed. It will be retried automatically by the background job.",
                OdsInstanceManageStatus.CreateError      => "OdsInstanceManage creation failed permanently. Manual database intervention required before deleting.",
                OdsInstanceManageStatus.PendingDelete    => "OdsInstanceManage is already queued for deletion.",
                OdsInstanceManageStatus.DeleteInProgress => "OdsInstanceManage is currently being deleted.",
                OdsInstanceManageStatus.DeleteFailed     => "OdsInstanceManage deletion failed. It will be retried automatically by the background job.",
                OdsInstanceManageStatus.DeleteError      => "OdsInstanceManage deletion failed permanently. Manual database intervention required.",
                _ => null,
            };
        }

        return null;
    }
}
```

- [ ] **Step 5: Replace `OdsInstanceManageModel.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

[SwaggerSchema(Title = "OdsInstanceManage")]
public class OdsInstanceManageModel
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public int? OdsInstanceId { get; set; }
    public string? OdsInstanceName { get; set; }
    public string? Status { get; set; }
    public string? DatabaseTemplate { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? LastRefreshed { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}
```

- [ ] **Step 6: Replace `OdsInstanceManageMapper.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public static class OdsInstanceManageMapper
{
    public static OdsInstanceManageModel ToModel(OdsInstanceManage source)
    {
        return new OdsInstanceManageModel
        {
            Id = source.Id,
            Name = source.Name,
            OdsInstanceId = source.OdsInstanceId,
            OdsInstanceName = source.OdsInstanceName,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
            LastRefreshed = source.LastRefreshed,
            LastModifiedDate = source.LastModifiedDate,
        };
    }

    public static List<OdsInstanceManageModel> ToModelList(IEnumerable<OdsInstanceManage> source)
    {
        return source.Select(ToModel).ToList();
    }
}
```

- [ ] **Step 7: Replace `OdsInstanceManageDatabaseNameFormatter.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

internal static class OdsInstanceManageDatabaseNameFormatter
{
    private const string CanonicalPrefix = "EdFi_Ods";

    // Use PostgreSQL's identifier limit as the portable ceiling so the persisted
    // DatabaseName always matches the real provisioned database across engines.
    internal const int MaxPortableDatabaseNameLength = 63;

    private static readonly Regex _leadingCanonicalPrefixPattern = new(
        @"^(?:(?:edfi_+ods)(?:_+|$))+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static string Build(string instanceName, string databaseTemplate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseTemplate);

        var normalizedName = NormalizeSegment(instanceName);
        var normalizedDatabaseTemplate = NormalizeSegment(databaseTemplate);
        var normalizedNameWithoutPrefix = _leadingCanonicalPrefixPattern.Replace(normalizedName, string.Empty).Trim('_');

        return string.IsNullOrWhiteSpace(normalizedNameWithoutPrefix)
            ? $"{CanonicalPrefix}_{normalizedDatabaseTemplate}"
            : $"{CanonicalPrefix}_{normalizedNameWithoutPrefix}_{normalizedDatabaseTemplate}";
    }

    private static string NormalizeSegment(string value)
        => value.Replace(' ', '_').Trim('_');
}
```

- [ ] **Step 8: Build**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`
Expected: remaining errors confined to `Features\OdsInstances\OdsInstanceWithEducationOrganizationsModel.cs`/`ReadEducationOrganizations.cs` (Task 5) and `Program.cs`/`WebApplicationBuilderExtensions.cs` (Task 6).

- [ ] **Step 9: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage" \
        "Application/EdFi.Ods.AdminApi/Features/DbInstances"
git commit -m "Move v2 DbInstances feature into OdsInstances/Manage, rename routes to /odsInstances/manage"
```

---

### Task 5: v2 existing `OdsInstances` and `Tenants` files — update references to the renamed query/enum

**Amendment (discovered during Task 3 implementation):** the original plan missed a whole consumer area — `Features\Tenants\*` and `Infrastructure\Services\Tenants\TenantService.cs` also inject `IGetDbInstancesQuery`/`DbInstance`/`DbInstanceStatus` to build the `/tenants/{tenantName}/odsInstances/edOrgs` response. This section folds that fix into Task 5 rather than adding a new task number.

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\OdsInstanceWithEducationOrganizationsModel.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Features\OdsInstances\ReadEducationOrganizations.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Features\Tenants\TenantDetailModel.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Features\Tenants\TenantMapper.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Features\Tenants\ReadTenants.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Tenants\TenantService.cs`

**Interfaces:**
- Consumes: `IGetOdsInstanceManagesQuery` (Task 3), `OdsInstanceManageStatus` (Task 2).
- Produces: `OdsInstanceWithEducationOrganizationsModel.OdsInstanceManageId` (renamed from `DbInstanceId`), `TenantOdsInstanceModel.OdsInstanceManageId` (renamed from `DbInstanceId`), `TenantMapper.ToUnlinkedOdsInstanceManageModel` (renamed from `ToUnlinkedDbInstanceModel`), `ITenantsService.GetTenantEdOrgsByInstancesAsync(..., IGetOdsInstanceManagesQuery, ...)` — all public API response shape / internal wiring only, consumed by nothing else in this plan.

- [ ] **Step 1: Rename the `DbInstanceId` property**

In `OdsInstanceWithEducationOrganizationsModel.cs`, replace:

```csharp
    [SwaggerSchema(Description = "DbInstance identifier for this ODS instance")]
    public int? DbInstanceId { get; set; }
```

with:

```csharp
    [SwaggerSchema(Description = "OdsInstanceManage identifier for this ODS instance")]
    public int? OdsInstanceManageId { get; set; }
```

- [ ] **Step 2: Update `ReadEducationOrganizations.cs`**

Replace the full file content with:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

public class ReadEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/odsInstances/{instanceId}/edOrgs", GetEducationOrganizationsByInstance)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific ODS instance",
                "Returns all education organizations for the specified ODS instance in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> GetEducationOrganizationsByInstance(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetOdsInstanceQuery getOdsInstanceQuery,
        [FromServices] IGetOdsInstanceManagesQuery getOdsInstanceManagesQuery,
        [AsParameters] CommonQueryParams commonQueryParams,
        int instanceId)
    {
        getOdsInstanceQuery.Execute(instanceId);

        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            instanceId: instanceId);

        MergeOdsInstanceManageData(educationOrganizations, getOdsInstanceManagesQuery);
        return Results.Ok(educationOrganizations);
    }

    private static void MergeOdsInstanceManageData(
        List<OdsInstanceWithEducationOrganizationsModel> instances,
        IGetOdsInstanceManagesQuery getOdsInstanceManagesQuery)
    {
        var allOdsInstanceManages = getOdsInstanceManagesQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

        var linkedById = allOdsInstanceManages
            .Where(d => d.OdsInstanceId is not null)
            .GroupBy(d => d.OdsInstanceId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

        foreach (var instance in instances)
        {
            if (instance.Id is int instanceId && linkedById.TryGetValue(instanceId, out var odsInstanceManage))
            {
                instance.OdsInstanceManageId = odsInstanceManage.Id;
                instance.Status = odsInstanceManage.Status;
                instance.DatabaseTemplate = odsInstanceManage.DatabaseTemplate;
                instance.DatabaseName = odsInstanceManage.DatabaseName;
            }
            else
            {
                instance.Status = OdsInstanceManageStatus.Created.ToString();
            }
        }
    }
}
```

- [ ] **Step 3: Rename `TenantOdsInstanceModel.DbInstanceId` in `TenantDetailModel.cs`**

Replace:

```csharp
    [JsonPropertyName("id")]
    public int? OdsInstanceId { get; set; }
    public int? DbInstanceId { get; set; }
```

with:

```csharp
    [JsonPropertyName("id")]
    public int? OdsInstanceId { get; set; }
    public int? OdsInstanceManageId { get; set; }
```

- [ ] **Step 4: Update `TenantMapper.cs`**

Replace the full file content with:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Features.Tenants;

public static class TenantMapper
{
    public static TenantOdsInstanceModel ToOdsInstanceModel(OdsInstance source)
    {
        return new TenantOdsInstanceModel
        {
            OdsInstanceId = source.OdsInstanceId,
            Name = source.Name,
            InstanceType = source.InstanceType,
        };
    }

    public static List<TenantOdsInstanceModel> ToOdsInstanceModelList(IEnumerable<OdsInstance> source)
    {
        return source.Select(ToOdsInstanceModel).ToList();
    }

    public static TenantOdsInstanceModel ToUnlinkedOdsInstanceManageModel(OdsInstanceManage source)
    {
        return new TenantOdsInstanceModel
        {
            OdsInstanceId = null,
            OdsInstanceManageId = source.Id,
            Name = source.Name,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
        };
    }
}
```

- [ ] **Step 5: Update `ReadTenants.cs`**

Replace:

```csharp
        IGetDbInstancesQuery getDbInstancesQuery,
```

with:

```csharp
        IGetOdsInstanceManagesQuery getOdsInstanceManagesQuery,
```

and replace:

```csharp
        var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(
            getOdsInstancesQuery, getEducationOrganizationQuery, getDbInstancesQuery, tenantName);
```

with:

```csharp
        var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(
            getOdsInstancesQuery, getEducationOrganizationQuery, getOdsInstanceManagesQuery, tenantName);
```

Add `using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;` if not already present (it already is — `IGetOdsInstanceManagesQuery` lives in the same namespace as `IGetOdsInstancesQuery`/`IGetEducationOrganizationQuery`, both already imported in this file).

- [ ] **Step 6: Update `TenantService.cs`**

Replace the interface line:

```csharp
    Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(IGetOdsInstancesQuery getOdsInstancesQuery, IGetEducationOrganizationQuery getEducationOrganizationQuery, IGetDbInstancesQuery getDbInstancesQuery, string tenantName);
```

with:

```csharp
    Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(IGetOdsInstancesQuery getOdsInstancesQuery, IGetEducationOrganizationQuery getEducationOrganizationQuery, IGetOdsInstanceManagesQuery getOdsInstanceManagesQuery, string tenantName);
```

Replace the method signature:

```csharp
    public async Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(
        IGetOdsInstancesQuery getOdsInstancesQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IGetDbInstancesQuery getDbInstancesQuery,
        string tenantName)
```

with:

```csharp
    public async Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(
        IGetOdsInstancesQuery getOdsInstancesQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IGetOdsInstanceManagesQuery getOdsInstanceManagesQuery,
        string tenantName)
```

Replace the method body's use of the renamed query and entity:

```csharp
            var allDbInstances = getDbInstancesQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

            var linkedDbInstancesByOdsId = allDbInstances
                .Where(d => d.OdsInstanceId is not null)
                .GroupBy(d => d.OdsInstanceId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

            foreach (var odsInstance in tenantDetails.OdsInstances)
            {
                if (odsInstance.OdsInstanceId is int odsInstanceId && linkedDbInstancesByOdsId.TryGetValue(odsInstanceId, out var dbInstance))
                {
                    odsInstance.DbInstanceId = dbInstance.Id;
                    odsInstance.Status = dbInstance.Status;
                    odsInstance.DatabaseTemplate = dbInstance.DatabaseTemplate;
                    odsInstance.DatabaseName = dbInstance.DatabaseName;
                }
                else
                {
                    odsInstance.Status = DbInstanceStatus.Created.ToString();
                }
            }

            var existingOdsInstanceIds = tenantDetails.OdsInstances
                .Where(i => i.OdsInstanceId is int)
                .Select(i => i.OdsInstanceId!.Value)
                .ToHashSet();

            var unlinkedDbInstances = allDbInstances
                .Where(d => d.OdsInstanceId is null)
                .Concat(allDbInstances
                    .Where(d => d.OdsInstanceId is not null && !existingOdsInstanceIds.Contains(d.OdsInstanceId.Value))
                    .GroupBy(d => d.OdsInstanceId!.Value)
                    .Select(g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First()))
                .ToList();
            foreach (var dbInstance in unlinkedDbInstances)
            {
                tenantDetails.OdsInstances.Add(TenantMapper.ToUnlinkedDbInstanceModel(dbInstance));
            }
```

with:

```csharp
            var allOdsInstanceManages = getOdsInstanceManagesQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

            var linkedOdsInstanceManagesByOdsId = allOdsInstanceManages
                .Where(d => d.OdsInstanceId is not null)
                .GroupBy(d => d.OdsInstanceId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

            foreach (var odsInstance in tenantDetails.OdsInstances)
            {
                if (odsInstance.OdsInstanceId is int odsInstanceId && linkedOdsInstanceManagesByOdsId.TryGetValue(odsInstanceId, out var odsInstanceManage))
                {
                    odsInstance.OdsInstanceManageId = odsInstanceManage.Id;
                    odsInstance.Status = odsInstanceManage.Status;
                    odsInstance.DatabaseTemplate = odsInstanceManage.DatabaseTemplate;
                    odsInstance.DatabaseName = odsInstanceManage.DatabaseName;
                }
                else
                {
                    odsInstance.Status = OdsInstanceManageStatus.Created.ToString();
                }
            }

            var existingOdsInstanceIds = tenantDetails.OdsInstances
                .Where(i => i.OdsInstanceId is int)
                .Select(i => i.OdsInstanceId!.Value)
                .ToHashSet();

            var unlinkedOdsInstanceManages = allOdsInstanceManages
                .Where(d => d.OdsInstanceId is null)
                .Concat(allOdsInstanceManages
                    .Where(d => d.OdsInstanceId is not null && !existingOdsInstanceIds.Contains(d.OdsInstanceId.Value))
                    .GroupBy(d => d.OdsInstanceId!.Value)
                    .Select(g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First()))
                .ToList();
            foreach (var odsInstanceManage in unlinkedOdsInstanceManages)
            {
                tenantDetails.OdsInstances.Add(TenantMapper.ToUnlinkedOdsInstanceManageModel(odsInstanceManage));
            }
```

- [ ] **Step 7: Build**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`
Expected: remaining errors confined to `Program.cs`/`WebApplicationBuilderExtensions.cs` (Task 6) and all of v3 (Tasks 10-13, not yet done).

- [ ] **Step 8: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/Features/OdsInstances/OdsInstanceWithEducationOrganizationsModel.cs" \
        "Application/EdFi.Ods.AdminApi/Features/OdsInstances/ReadEducationOrganizations.cs" \
        "Application/EdFi.Ods.AdminApi/Features/Tenants/TenantDetailModel.cs" \
        "Application/EdFi.Ods.AdminApi/Features/Tenants/TenantMapper.cs" \
        "Application/EdFi.Ods.AdminApi/Features/Tenants/ReadTenants.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs"
git commit -m "Update v2 OdsInstances and Tenants EdOrgs merges to use renamed OdsInstanceManage query"
```

---

### Task 6: v2 wiring — `Program.cs` and `WebApplicationBuilderExtensions.cs`

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\Program.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\WebApplicationBuilderExtensions.cs`

**Interfaces:**
- Consumes: `JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName`/`DeletePendingOdsInstanceManagesDispatcherJobName` (Task 2), `AppSettings.CreateOdsInstanceManagesSweepIntervalInMins`/`DeleteOdsInstanceManagesSweepIntervalInMins` (Task 2), `CreatePendingOdsInstanceManagesDispatcherJob`/`DeletePendingOdsInstanceManagesDispatcherJob` (Task 3).
- Produces: fully wired v2 Quartz scheduling — this is the last v2-side file needed for the solution to build; Task 7's build checkpoint depends on this completing cleanly.

- [ ] **Step 1: Update config key lookups in `Program.cs`**

Replace:

```csharp
var createDbInstancesSweepIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:CreateDbInstancesSweepIntervalInMins"
);
var deleteDbInstancesSweepIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:DeleteDbInstancesSweepIntervalInMins"
);
```

with:

```csharp
var createOdsInstanceManagesSweepIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:CreateOdsInstanceManagesSweepIntervalInMins"
);
var deleteOdsInstanceManagesSweepIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:DeleteOdsInstanceManagesSweepIntervalInMins"
);
```

- [ ] **Step 2: Rename every local variable derived from those two lines, in both the `AdminApiMode.V2` and `AdminApiMode.V3` branches**

Throughout both branches, rename:
- `createDbInstancesSweepIntervalInMins` → `createOdsInstanceManagesSweepIntervalInMins`
- `deleteDbInstancesSweepIntervalInMins` → `deleteOdsInstanceManagesSweepIntervalInMins`
- `createDbInstancesSweepInterval` → `createOdsInstanceManagesSweepInterval`
- `deleteDbInstancesSweepInterval` → `deleteOdsInstanceManagesSweepInterval`

(these are `double.TryParse(..., out var createDbInstancesSweepInterval)`-style declarations and their later `TimeSpan.FromMinutes(...)` usages — rename the declaration and every usage site.)

- [ ] **Step 3: Update `JobKey`/job-type references in the `AdminApiMode.V2` branch**

Replace every occurrence in the V2 branch of:

```csharp
await QuartzJobScheduler.ScheduleJob<CreatePendingDbInstancesDispatcherJob>(
    scheduler,
    jobKey: new JobKey($"{JobConstants.CreatePendingDbInstancesDispatcherJobName}_{tenantName}"),
```

and the non-multi-tenant equivalent

```csharp
await QuartzJobScheduler.ScheduleJob<CreatePendingDbInstancesDispatcherJob>(
    scheduler,
    jobKey: new JobKey(JobConstants.CreatePendingDbInstancesDispatcherJobName),
```

with the `CreatePendingOdsInstanceManagesDispatcherJob` class and `JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName` constant (both multi-tenant and non-multi-tenant branches). Do the same for the `Delete...` pair (`DeletePendingDbInstancesDispatcherJob` → `DeletePendingOdsInstanceManagesDispatcherJob`, `JobConstants.DeletePendingDbInstancesDispatcherJobName` → `JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName`).

Also update the two `_logger.Error(...)` messages:

```csharp
_logger.Error("Invalid value for CreateDbInstancesSweepIntervalInMins. Please ensure it is a valid number.");
...
_logger.Error("Invalid value for DeleteDbInstancesSweepIntervalInMins. Please ensure it is a valid number.");
```

to:

```csharp
_logger.Error("Invalid value for CreateOdsInstanceManagesSweepIntervalInMins. Please ensure it is a valid number.");
...
_logger.Error("Invalid value for DeleteOdsInstanceManagesSweepIntervalInMins. Please ensure it is a valid number.");
```

(these log messages appear once in the V2 branch and once in the V3 branch — update both; Task 13 covers the V3-branch job-type swap to `V3Jobs.CreatePendingDataStoreManagesDispatcherJob`/`DeletePendingDataStoreManagesDispatcherJob`, but the log message text and interval variable renames happen here since they're shared local variables computed once before the `if/else if` branches.)

- [ ] **Step 4: Update `WebApplicationBuilderExtensions.cs` DI registrations for the v2 (`else`) branch**

Find the `else` branch of `RegisterQuartzServices` (the non-V3 branch, currently registering `CreateInstanceJob`, `CreatePendingDbInstancesDispatcherJob`, `RefreshEducationOrganizationsJob`, etc. — grep for `webApplicationBuilder.Services.AddTransient<CreatePendingDbInstancesDispatcherJob>();`) and replace:

```csharp
            webApplicationBuilder.Services.AddTransient<CreatePendingDbInstancesDispatcherJob>();
```

with:

```csharp
            webApplicationBuilder.Services.AddTransient<CreatePendingOdsInstanceManagesDispatcherJob>();
```

Also find and rename the corresponding `DeletePendingDbInstancesDispatcherJob` registration (grep the same file for it — it sits near the `Create...` registration in the same `else` block) to `DeletePendingOdsInstanceManagesDispatcherJob`.

- [ ] **Step 5: Build the full solution**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`
Expected: v2 side (everything except v3's `EdFi.Ods.AdminApi.V3` project) now compiles cleanly. Remaining errors should be entirely inside `Application\EdFi.Ods.AdminApi.V3\` (fixed by Tasks 10–13) and its `.UnitTests`/`.DBTests` counterparts (Tasks 14–15) and `Application\EdFi.Ods.AdminApi.UnitTests`/`.DBTests` (Tasks 7–8, not yet done).

- [ ] **Step 6: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/Program.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs"
git commit -m "Update v2 Quartz wiring to use renamed OdsInstanceManage jobs and config keys"
```

---

### Task 7: v2 Unit tests rename (`EdFi.Ods.AdminApi.UnitTests`)

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Features\DbInstances\AddDbInstanceTests.cs` → move+rename to `Application\EdFi.Ods.AdminApi.UnitTests\Features\OdsInstances\Manage\AddOdsInstanceManageTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Features\DbInstances\ReadDbInstanceTests.cs` → move+rename to `Application\EdFi.Ods.AdminApi.UnitTests\Features\OdsInstances\Manage\ReadOdsInstanceManageTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Features\DbInstances\DeleteDbInstanceTests.cs` → move+rename to `Application\EdFi.Ods.AdminApi.UnitTests\Features\OdsInstances\Manage\DeleteOdsInstanceManageTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Database\Commands\AddDbInstanceCommandTests.cs` → rename to `AddOdsInstanceManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Database\Commands\DeleteDbInstanceCommandTests.cs` → rename to `DeleteOdsInstanceManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Database\Queries\GetDbInstancesQueryTests.cs` → rename to `GetOdsInstanceManagesQueryTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Database\Queries\GetDbInstanceByIdQueryTests.cs` → rename to `GetOdsInstanceManageByIdQueryTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Services\Jobs\CreatePendingDbInstancesDispatcherJobTests.cs` → rename to `CreatePendingOdsInstanceManagesDispatcherJobTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Services\Jobs\DeletePendingDbInstancesDispatcherJobTests.cs` → rename to `DeletePendingOdsInstanceManagesDispatcherJobTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Services\Jobs\CreateInstanceJobTests.cs` (renamed in place, filename unchanged)
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Services\Jobs\DeleteInstanceJobTests.cs` (renamed in place, filename unchanged)
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Features\Tenants\ReadTenantsTest.cs` (filename unchanged — update references to Task 5's renamed Tenants types)
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Features\Tenants\TenantDetailModelTests.cs` (filename unchanged — update references)
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Services\Tenants\TenantServiceTests.cs` (filename unchanged — update references)

**Amendment (same gap as Task 5):** the three Tenants test files above were missed in the original plan — they test `Features\Tenants\*`/`TenantService.cs`, which Task 5 (amended) now renames. Their production counterparts changed in Task 5, so these tests need matching updates or they won't compile.

**Interfaces:**
- Consumes: every production type from Tasks 2–6 (`OdsInstanceManage`, `OdsInstanceManageStatus`, `IGetOdsInstanceManagesQuery`, `AddOdsInstanceManageCommand`, `AddOdsInstanceManage`/`ReadOdsInstanceManage`/`DeleteOdsInstanceManage` features, `CreatePendingOdsInstanceManagesDispatcherJob`, etc.), plus Task 5's renamed `TenantOdsInstanceModel.OdsInstanceManageId`, `TenantMapper.ToUnlinkedOdsInstanceManageModel`, `ITenantsService.GetTenantEdOrgsByInstancesAsync(..., IGetOdsInstanceManagesQuery, ...)`.

- [ ] **Step 1: Move and rename the query test files (full content known — apply directly)**

```bash
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetDbInstancesQueryTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetOdsInstanceManagesQueryTests.cs"
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetDbInstanceByIdQueryTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetOdsInstanceManageByIdQueryTests.cs"
```

`GetOdsInstanceManagesQueryTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstanceManagesQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstanceManagesQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_WithoutFilters_ReturnsAllOdsInstanceManages()
    {
        using var context = CreateContext();
        context.OdsInstanceManages.AddRange(
            new OdsInstanceManage { Name = "Sandbox A", Status = "Healthy", DatabaseTemplate = "Minimal" },
            new OdsInstanceManage { Name = "Sandbox B", Status = "Healthy", DatabaseTemplate = "Minimal" });
        context.SaveChanges();

        var query = new GetOdsInstanceManagesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, null);

        result.Count.ShouldBe(2);
        result.Select(x => x.Name).ShouldBe(["Sandbox A", "Sandbox B"], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsMatchingOdsInstanceManage()
    {
        using var context = CreateContext();
        context.OdsInstanceManages.AddRange(
            new OdsInstanceManage { Name = "Sandbox A", Status = "Healthy", DatabaseTemplate = "Minimal" },
            new OdsInstanceManage { Name = "Sandbox B", Status = "Healthy", DatabaseTemplate = "Minimal" });
        context.SaveChanges();

        var query = new GetOdsInstanceManagesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, "Sandbox B");

        result.Count.ShouldBe(1);
        result.Single().Name.ShouldBe("Sandbox B");
    }
}
```

`GetOdsInstanceManageByIdQueryTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstanceManageByIdQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstanceManageByIdQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_WithExistingId_ReturnsOdsInstanceManage()
    {
        using var context = CreateContext();
        var odsInstanceManage = new OdsInstanceManage
        {
            Name = "Sandbox",
            Status = "Healthy",
            DatabaseTemplate = "Minimal"
        };
        context.OdsInstanceManages.Add(odsInstanceManage);
        context.SaveChanges();

        var query = new GetOdsInstanceManageByIdQuery(context);

        var result = query.Execute(odsInstanceManage.Id);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Sandbox");
    }

    [Test]
    public void Execute_WithUnknownId_ReturnsNull()
    {
        using var context = CreateContext();
        var query = new GetOdsInstanceManageByIdQuery(context);

        var result = query.Execute(999);

        result.ShouldBeNull();
    }
}
```

- [ ] **Step 2: Move and rename the remaining v2 unit test files, applying the Master Rename Table**

```bash
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Features/DbInstances/AddDbInstanceTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/AddOdsInstanceManageTests.cs"
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Features/DbInstances/ReadDbInstanceTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/ReadOdsInstanceManageTests.cs"
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Features/DbInstances/DeleteDbInstanceTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/DeleteOdsInstanceManageTests.cs"
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/AddDbInstanceCommandTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/AddOdsInstanceManageCommandTests.cs"
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteDbInstanceCommandTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteOdsInstanceManageCommandTests.cs"
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/CreatePendingDbInstancesDispatcherJobTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/CreatePendingOdsInstanceManagesDispatcherJobTests.cs"
git mv "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/DeletePendingDbInstancesDispatcherJobTests.cs" "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/DeletePendingOdsInstanceManagesDispatcherJobTests.cs"
```

For each moved file and for the two in-place files (`CreateInstanceJobTests.cs`, `DeleteInstanceJobTests.cs`), open it and apply every substitution from the "v2-specific" and "Shared" Master Rename Table sections above (class name matching the new filename, `namespace ...UnitTests.Features.DbInstances` → `...UnitTests.Features.OdsInstances.Manage`, every `DbInstance`/`OdsInstance...`-adjacent identifier, string literal route fragments like `"/dbInstances"` → `"/odsInstances/manage"` if any test asserts on the route string, and any `[TestFixture] public class AddDbInstanceTests` → `AddOdsInstanceManageTests`).

- [ ] **Step 3: Update the three Tenants-area test files (filenames unchanged)**

Open `Application\EdFi.Ods.AdminApi.UnitTests\Features\Tenants\ReadTenantsTest.cs`, `Application\EdFi.Ods.AdminApi.UnitTests\Features\Tenants\TenantDetailModelTests.cs`, and `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\Services\Tenants\TenantServiceTests.cs`. Update every reference to match Task 5's renames: `IGetDbInstancesQuery`/`getDbInstancesQuery` → `IGetOdsInstanceManagesQuery`/`getOdsInstanceManagesQuery`, `DbInstance`/`DbInstanceStatus` → `OdsInstanceManage`/`OdsInstanceManageStatus` (mock/fixture setups), `TenantOdsInstanceModel.DbInstanceId` → `.OdsInstanceManageId`, `TenantMapper.ToUnlinkedDbInstanceModel` → `.ToUnlinkedOdsInstanceManageModel`. Do not rename the files or their test class names — only internal references.

- [ ] **Step 4: Run the v2 unit test suite**

Run: `dotnet test Application/EdFi.Ods.AdminApi.UnitTests/EdFi.Ods.AdminApi.UnitTests.csproj`
Expected: PASS, same test count as before the rename (compare `git stash`'s pre-change `dotnet test` output count if unsure, or just confirm no failures/errors and no tests silently vanished — count should match the pre-rename baseline exactly since this task renames tests, not delete/add them).

- [ ] **Step 5: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Features/DbInstances" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/AddOdsInstanceManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteOdsInstanceManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetOdsInstanceManagesQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetOdsInstanceManageByIdQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/CreatePendingOdsInstanceManagesDispatcherJobTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/DeletePendingOdsInstanceManagesDispatcherJobTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/CreateInstanceJobTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/DeleteInstanceJobTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Features/Tenants/ReadTenantsTest.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Features/Tenants/TenantDetailModelTests.cs" \
        "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs"
git commit -m "Rename v2 unit tests to OdsInstanceManage naming"
```

---

### Task 8: v2 DBTests rename (`EdFi.Ods.AdminApi.DBTests`)

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.DBTests\Database\CommandTests\AddDbInstanceCommandTests.cs` → rename to `AddOdsInstanceManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.DBTests\Database\CommandTests\DeleteDbInstanceCommandTests.cs` → rename to `DeleteOdsInstanceManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.DBTests\Database\QueryTests\GetDbInstanceByIdQueryTests.cs` → rename to `GetOdsInstanceManageByIdQueryTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.DBTests\Database\QueryTests\GetDbInstancesQueryTests.cs` → rename to `GetOdsInstanceManagesQueryTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.DBTests\...\GetTenantEdOrgsByInstancesTests.cs` (filename unchanged — update internal references only)

**Interfaces:**
- Consumes: same production types as Task 7, against a real database (this project is this repo's integration-test layer).

- [ ] **Step 1: Move and rename**

```bash
git mv "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/AddDbInstanceCommandTests.cs" "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/AddOdsInstanceManageCommandTests.cs"
git mv "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/DeleteDbInstanceCommandTests.cs" "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/DeleteOdsInstanceManageCommandTests.cs"
git mv "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetDbInstanceByIdQueryTests.cs" "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetOdsInstanceManageByIdQueryTests.cs"
git mv "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetDbInstancesQueryTests.cs" "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetOdsInstanceManagesQueryTests.cs"
```

Apply the Master Rename Table to each moved file's content (class name, `AdminApiDbContext.DbInstances`→`.OdsInstanceManages`, `DbInstance`/`DbInstanceStatus`→`OdsInstanceManage`/`OdsInstanceManageStatus`, command/query type names). In `GetTenantEdOrgsByInstancesTests.cs` (filename unchanged, locate it first with a search since its exact path wasn't confirmed during research — search `Application\EdFi.Ods.AdminApi.DBTests` for `GetTenantEdOrgsByInstancesTests`), update only the internal references to renamed types (`IGetOdsInstanceManagesQuery`, `OdsInstanceManage`, etc.), not the filename or class name.

- [ ] **Step 2: Run the v2 DB test suite**

Run: `dotnet test Application/EdFi.Ods.AdminApi.DBTests/EdFi.Ods.AdminApi.DBTests.csproj` (requires a local test database per `docs/developer.md` DB migration instructions — apply Task 1's migration first).
Expected: PASS, same test count as the pre-rename baseline.

- [ ] **Step 3: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/AddOdsInstanceManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/DeleteOdsInstanceManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetOdsInstanceManageByIdQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetOdsInstanceManagesQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/AddDbInstanceCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.DBTests/Database/CommandTests/DeleteDbInstanceCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetDbInstanceByIdQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetDbInstancesQueryTests.cs"
git commit -m "Rename v2 DBTests to OdsInstanceManage naming"
```

---

### Task 9: v2 Bruno E2E collection rename

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\E2E Tests\V2\Bruno Admin API E2E 2.0 refactor\v2\DbInstances\` → move all files to `...\v2\OdsInstances\Manage\`

**Interfaces:**
- Consumes: routes `/v2/odsInstances/manage*` (Task 4).

- [ ] **Step 1: Move the folder and rename every `.bru` file**

```bash
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/folder.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/folder.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/POST - DbInstances.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/POST - OdsInstances Manage.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/POST - DbInstances - Sample Template.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/POST - OdsInstances Manage - Sample Template.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/POST - DbInstances - Invalid.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/POST - OdsInstances Manage - Invalid.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/POST - DbInstances - Invalid Database Template.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/POST - OdsInstances Manage - Invalid Database Template.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/GET - DbInstances.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/GET - OdsInstances Manage.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/GET - DbInstances by ID.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/GET - OdsInstances Manage by ID.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/GET - DbInstances - Without Offset.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/GET - OdsInstances Manage - Without Offset.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/GET - DbInstances - Without Limit.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/GET - OdsInstances Manage - Without Limit.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/GET - DbInstances - Without Limit and Offset.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/GET - OdsInstances Manage - Without Limit and Offset.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/GET - DbInstances - Not Found.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/GET - OdsInstances Manage - Not Found.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/GET - DbInstances - Filter by Name.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/GET - OdsInstances Manage - Filter by Name.bru"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/DELETE - DbInstance - Success.bru.disabled" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/DELETE - OdsInstance Manage - Success.bru.disabled"
git mv "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances/DELETE - DbInstance - Not Found.bru" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/DELETE - OdsInstance Manage - Not Found.bru"
```

- [ ] **Step 2: Update `folder.bru`**

```
meta {
  name: Manage
  seq: 99
}

auth {
  mode: inherit
}
```

- [ ] **Step 3: Update every moved `.bru` file's content**

For each file: change `meta { name: DbInstances ... }` → `meta { name: OdsInstances Manage ... }` (or the per-file variant, e.g. `DbInstance` singular in the DELETE files → `OdsInstance Manage`), change the request URL from `{{API_URL}}/v2/dbinstances...` to `{{API_URL}}/v2/odsinstances/manage...` (preserve any `/{id}` suffix or query string), and rename any `bru.setVar("CreatedDbInstanceId", ...)` / `{{CreatedDbInstanceId}}` variable references to `CreatedOdsInstanceManageId`, and update `test("POST DbInstances: ...")`-style assertion description strings to say `OdsInstances Manage` instead of `DbInstances`.

For example, `POST - OdsInstances Manage.bru`:

```
meta {
  name: OdsInstances Manage
  type: http
  seq: 1
}

post {
  url: {{API_URL}}/v2/odsinstances/manage
  body: json
  auth: inherit
}

body:json {
  {
    "name": "Test DB Instance",
    "databaseTemplate": "Minimal"
  }
}

script:post-response {
  test("POST OdsInstances Manage: Status code is Accepted", function () {
    expect(res.getStatus()).to.equal(202);
  });

  test("POST OdsInstances Manage: Response includes location in header", function () {
    expect(res.getHeaders()).to.have.property("location");
    const id = res.getHeader("location").split("/")[2];
    if (id) {
      bru.setVar("CreatedOdsInstanceManageId", id);
    }
  });
}

settings {
  encodeUrl: true
}
```

Apply the same URL/variable/assertion-text substitution pattern to the remaining 11 files (`GET`, `DELETE`, invalid-input variants) based on each file's current content.

- [ ] **Step 4: Run the v2 Bruno E2E suite**

Run: `./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TenantMode singletenant -TearDown` (adjust `-TenantMode` to match this repo's default if different — check `docs/developer.md` for the exact invocation).
Expected: PASS, all `OdsInstances Manage` requests succeed with the same assertions as before the rename.

- [ ] **Step 5: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/DbInstances"
git commit -m "Rename v2 Bruno E2E DbInstances collection to OdsInstances/Manage"
```

---

### Task 10: v3 Infrastructure layer rename (Queries, Commands, Jobs)

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Database\Queries\GetDbDataStoresQuery.cs` → rename to `GetDataStoreManagesQuery.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Database\Queries\GetDbDataStoreByIdQuery.cs` → rename to `GetDataStoreManageByIdQuery.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Database\Commands\AddDbDataStoreCommand.cs` → rename to `AddDataStoreManageCommand.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Database\Commands\DeleteDbDataStoreCommand.cs` → rename to `DeleteDataStoreManageCommand.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Services\Jobs\CreatePendingDbInstancesDispatcherJob.cs` → rename to `CreatePendingDataStoreManagesDispatcherJob.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Services\Jobs\DeletePendingDbInstancesDispatcherJob.cs` → rename to `DeletePendingDataStoreManagesDispatcherJob.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Services\Jobs\CreateInstanceJob.cs` (renamed in place, filename unchanged)
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Services\Jobs\DeleteInstanceJob.cs` (renamed in place, filename unchanged)

**Interfaces:**
- Consumes: `OdsInstanceManage` entity, `OdsInstanceManageStatus` enum, `JobConstants.OdsInstanceManageIdKey` (Task 2 — shared, so v3 also uses the `OdsInstanceManage`-prefixed shared names even though its own feature-layer naming is `DataStoreManage`), `AppSettings.CreateOdsInstanceManagesMaxRetryAttempts`/`DeleteOdsInstanceManagesMaxRetryAttempts` (Task 2), `AdminApiDbContext.OdsInstanceManages` (Task 2, V3's own DbContext).
- Produces: `IGetDataStoreManagesQuery`/`GetDataStoreManagesQuery`, `IGetDataStoreManageByIdQuery`/`GetDataStoreManageByIdQuery`, `AddDataStoreManageCommand`/`IAddDataStoreManageModel`, `IDeleteDataStoreManageCommand`/`DeleteDataStoreManageCommand`, `CreatePendingDataStoreManagesDispatcherJob`, `DeletePendingDataStoreManagesDispatcherJob` — consumed by Task 11 (feature files) and Task 13 (wiring).

- [ ] **Step 1: Rename and update the Queries**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDbDataStoresQuery.cs" "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreManagesQuery.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDbDataStoreByIdQuery.cs" "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreManageByIdQuery.cs"
```

`GetDataStoreManagesQuery.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Extensions;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDataStoreManagesQuery
{
    List<OdsInstanceManage> Execute(CommonQueryParams commonQueryParams, int? id, string? name);
}

public class GetDataStoreManagesQuery : IGetDataStoreManagesQuery
{
    private readonly AdminApiDbContext _context;
    private readonly IOptions<AppSettings> _options;

    public GetDataStoreManagesQuery(AdminApiDbContext context, IOptions<AppSettings> options)
    {
        _context = context;
        _options = options;
    }

    public List<OdsInstanceManage> Execute(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        return _context.OdsInstanceManages
            .Where(d => id == null || d.Id == id)
            .Where(d => name == null || d.Name == name)
            .OrderBy(d => d.Id)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
```

`GetDataStoreManageByIdQuery.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDataStoreManageByIdQuery
{
    OdsInstanceManage? Execute(int id);
}

public class GetDataStoreManageByIdQuery : IGetDataStoreManageByIdQuery
{
    private readonly AdminApiDbContext _context;

    public GetDataStoreManageByIdQuery(AdminApiDbContext context)
    {
        _context = context;
    }

    public OdsInstanceManage? Execute(int id)
    {
        return _context.OdsInstanceManages.SingleOrDefault(d => d.Id == id);
    }
}
```

- [ ] **Step 2: Rename and update the Commands**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/AddDbDataStoreCommand.cs" "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/AddDataStoreManageCommand.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/DeleteDbDataStoreCommand.cs" "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/DeleteDataStoreManageCommand.cs"
```

`AddDataStoreManageCommand.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public class AddDataStoreManageCommand
{
    private readonly AdminApiDbContext _context;

    public AddDataStoreManageCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public OdsInstanceManage Execute(IAddDataStoreManageModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ArgumentException("Name is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(model.DatabaseTemplate))
            throw new ArgumentException("DatabaseTemplate is required.", nameof(model));

        var now = DateTime.UtcNow;

        var odsInstanceManage = new OdsInstanceManage
        {
            Name = model.Name.Trim(),
            DatabaseTemplate = model.DatabaseTemplate.Trim(),
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            OdsInstanceId = null,
            OdsInstanceName = null,
            DatabaseName = null,
            LastRefreshed = now,
            LastModifiedDate = now
        };

        _context.OdsInstanceManages.Add(odsInstanceManage);
        _context.SaveChanges();
        return odsInstanceManage;
    }
}

public interface IAddDataStoreManageModel
{
    string? Name { get; }
    string? DatabaseTemplate { get; }
}
```

`DeleteDataStoreManageCommand.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IDeleteDataStoreManageCommand
{
    void Execute(int id);
}

public class DeleteDataStoreManageCommand : IDeleteDataStoreManageCommand
{
    private readonly AdminApiDbContext _context;

    public DeleteDataStoreManageCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var odsInstanceManage =
            _context.OdsInstanceManages.Find(id)
            ?? throw new NotFoundException<int>("dataStoreManage", id);

        if (odsInstanceManage.Status == OdsInstanceManageStatus.Deleted.ToString())
            throw new NotFoundException<int>("dataStoreManage", id);

        odsInstanceManage.Status = OdsInstanceManageStatus.PendingDelete.ToString();
        odsInstanceManage.LastModifiedDate = DateTime.UtcNow;

        _context.SaveChanges();
    }
}
```

- [ ] **Step 3: Rename and update the dispatcher Jobs**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/CreatePendingDbInstancesDispatcherJob.cs" "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/CreatePendingDataStoreManagesDispatcherJob.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/DeletePendingDbInstancesDispatcherJob.cs" "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/DeletePendingDataStoreManagesDispatcherJob.cs"
```

`CreatePendingDataStoreManagesDispatcherJob.cs` (same transformation pattern as v2's `CreatePendingOdsInstanceManagesDispatcherJob.cs` from Task 3 — class renamed to `CreatePendingDataStoreManagesDispatcherJob`, everything else identical since v3's dispatcher logic was byte-identical to v2's):

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class CreatePendingDataStoreManagesDispatcherJob(
    ILogger<CreatePendingDataStoreManagesDispatcherJob> logger,
    IJobStatusService jobStatusService,
    AdminApiDbContext dbContext,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    IOptions<AppSettings> options)
    : AdminApiQuartzJobBase(logger, jobStatusService)
{
    private const int DefaultMaxRetryAttempts = 3;

    private readonly AdminApiDbContext _dbContext = dbContext;
    private readonly ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = tenantSpecificDbContextProvider;
    private readonly IOptions<AppSettings> _options = options;

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var tenantName = GetTenantName(context, multiTenancyEnabled);
        AdminApiDbContext? tenantAdminApiDbContext = null;
        var adminApiDbContext = _dbContext;

        try
        {
            if (multiTenancyEnabled)
            {
                tenantAdminApiDbContext = _tenantSpecificDbContextProvider.GetAdminApiDbContext(tenantName!);
                adminApiDbContext = tenantAdminApiDbContext;
            }

            var eligibleOdsInstanceManages = await adminApiDbContext.OdsInstanceManages
                .Where(instance => instance.Status == OdsInstanceManageStatus.PendingCreate.ToString() || instance.Status == OdsInstanceManageStatus.CreateFailed.ToString())
                .OrderBy(instance => instance.Id)
                .ToListAsync();

            foreach (var odsInstanceManage in eligibleOdsInstanceManages)
            {
                if (string.Equals(odsInstanceManage.Status, OdsInstanceManageStatus.PendingCreate.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    await ScheduleCreateJobAsync(context, odsInstanceManage.Id, tenantName);
                    continue;
                }

                if (!await IsRetryEligibleAsync(adminApiDbContext, odsInstanceManage, tenantName))
                {
                    odsInstanceManage.Status = OdsInstanceManageStatus.CreateError.ToString();
                    odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                    odsInstanceManage.LastRefreshed = DateTime.UtcNow;
                    await adminApiDbContext.SaveChangesAsync();
                    continue;
                }

                odsInstanceManage.Status = OdsInstanceManageStatus.PendingCreate.ToString();
                odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                odsInstanceManage.LastRefreshed = DateTime.UtcNow;
                await adminApiDbContext.SaveChangesAsync();

                await ScheduleCreateJobAsync(context, odsInstanceManage.Id, tenantName);
            }
        }
        finally
        {
            if (tenantAdminApiDbContext is not null)
            {
                await tenantAdminApiDbContext.DisposeAsync();
            }
        }
    }

    private async Task<bool> IsRetryEligibleAsync(AdminApiDbContext adminApiDbContext, OdsInstanceManage odsInstanceManage, string? tenantName)
    {
        var maxRetryAttempts = _options.Value.CreateOdsInstanceManagesMaxRetryAttempts > 0
            ? _options.Value.CreateOdsInstanceManagesMaxRetryAttempts
            : DefaultMaxRetryAttempts;

        var jobIdPrefix = $"{CreateInstanceJob.BuildJobIdentity(odsInstanceManage.Id, tenantName)}_";
        var errorCount = await adminApiDbContext.JobStatuses
            .CountAsync(status => status.JobId.StartsWith(jobIdPrefix) && status.Status == QuartzJobStatus.Error.ToString());

        return errorCount < maxRetryAttempts;
    }

    private static async Task ScheduleCreateJobAsync(IJobExecutionContext context, int odsInstanceManageId, string? tenantName)
    {
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.OdsInstanceManageIdKey] = odsInstanceManageId
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobData[JobConstants.TenantNameKey] = tenantName;
        }

        await QuartzJobScheduler.ScheduleJob<CreateInstanceJob>(
            context.Scheduler,
            CreateInstanceJob.CreateJobKey(odsInstanceManageId, tenantName),
            jobData,
            startImmediately: true);
    }

    private static string? GetTenantName(IJobExecutionContext context, bool multiTenancyEnabled)
    {
        if (!multiTenancyEnabled)
        {
            return null;
        }

        var tenantName = context.MergedJobDataMap.ContainsKey(JobConstants.TenantNameKey)
            ? context.MergedJobDataMap.GetString(JobConstants.TenantNameKey)
            : null;

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new InvalidOperationException(
                $"{JobConstants.TenantNameKey} must be provided when multi-tenancy is enabled.");
        }

        return tenantName;
    }
}
```

`DeletePendingDataStoreManagesDispatcherJob.cs` — apply the identical transformation (mirroring v2's `DeletePendingOdsInstanceManagesDispatcherJob.cs` from Task 3, renamed to `DeletePendingDataStoreManagesDispatcherJob`, using `DeleteOdsInstanceManagesMaxRetryAttempts` and calling `DeleteInstanceJob`).

- [ ] **Step 4: Update `CreateInstanceJob.cs` and `DeleteInstanceJob.cs` in place (v3 copies — filenames and class names unchanged, only internal references)**

Apply the same substitutions as Task 3 Step 4, but for the v3 files: replace `using EdFi.Ods.AdminApi.V3.Features.DbDataStores;` with `using EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;` (namespace of `DataStoreManageDatabaseNameFormatter`, produced by Task 11), rename `DbInstance`/`dbInstance` → `OdsInstanceManage`/`odsInstanceManage`, `adminApiDbContext.DbInstances` → `.OdsInstanceManages`, `DbInstanceStatus` → `OdsInstanceManageStatus`, `JobConstants.DbInstanceIdKey` → `JobConstants.OdsInstanceManageIdKey`, and (in `CreateInstanceJob.cs` only) `DbDataStoreDatabaseNameFormatter` → `DataStoreManageDatabaseNameFormatter`.

- [ ] **Step 5: Build to confirm the v3 Infrastructure layer compiles in isolation**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln` (or the appropriate project reference — confirm the v3 project's build command from `docs/developer.md`).
Expected: remaining errors confined to `Features\DbDataStores\*` (Task 11), `Features\DataStores\*` (Task 12), and `Program.cs`/`WebApplicationBuilderExtensions.cs` (Task 13, shared file already partially updated in Task 6).

- [ ] **Step 6: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreManagesQuery.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreManageByIdQuery.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/AddDataStoreManageCommand.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/DeleteDataStoreManageCommand.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/CreatePendingDataStoreManagesDispatcherJob.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/DeletePendingDataStoreManagesDispatcherJob.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/CreateInstanceJob.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Jobs/DeleteInstanceJob.cs"
git commit -m "Rename v3 Infrastructure Queries/Commands/Jobs to DataStoreManage naming"
```

---

### Task 11: v3 Feature folder move — `Features\DbDataStores` → `Features\DataStores\Manage`

**Files:**
- Create: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\Manage\AddDataStoreManage.cs`
- Create: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\Manage\ReadDataStoreManage.cs`
- Create: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\Manage\DeleteDataStoreManage.cs`
- Create: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\Manage\DataStoreManageModel.cs`
- Create: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\Manage\DataStoreManageMapper.cs`
- Create: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\Manage\DataStoreManageDatabaseNameFormatter.cs`
- Delete: `Application\EdFi.Ods.AdminApi.V3\Features\DbDataStores\` (entire folder, all 6 old files)

**Interfaces:**
- Consumes: `AddDataStoreManageCommand`/`IAddDataStoreManageModel`, `IGetDataStoreManagesQuery`/`IGetDataStoreManageByIdQuery`, `IDeleteDataStoreManageCommand` (Task 10), `OdsInstanceManage`/`OdsInstanceManageStatus` (Task 2), `JobConstants.OdsInstanceManageIdKey` (Task 2), `CreateInstanceJob`/`DeleteInstanceJob` (Task 10, unchanged names).
- Produces: routes `POST/GET/DELETE /dataStores/manage` under `EdFi.Ods.AdminApi.V3.Features.DataStores.Manage`, consumed by Task 16 (Bruno E2E) and Task 17 (`.http` file).

- [ ] **Step 1: Move the folder with git**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3/Features/DbDataStores/AddDbDataStore.cs" "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/AddDataStoreManage.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Features/DbDataStores/ReadDbDataStore.cs" "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/ReadDataStoreManage.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Features/DbDataStores/DeleteDbDataStore.cs" "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/DeleteDataStoreManage.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Features/DbDataStores/DbDataStoreModel.cs" "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/DataStoreManageModel.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Features/DbDataStores/DbDataStoreMapper.cs" "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/DataStoreManageMapper.cs"
git mv "Application/EdFi.Ods.AdminApi.V3/Features/DbDataStores/DbDataStoreDatabaseNameFormatter.cs" "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/DataStoreManageDatabaseNameFormatter.cs"
```

- [ ] **Step 2: Replace `AddDataStoreManage.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Swashbuckle.AspNetCore.Annotations;
using EdFi.Ods.AdminApi.V3.Infrastructure;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

public class AddDataStoreManage : IFeature
{
    private const int MaxSynchronizedNameLength = 100;
    private const int MaxDataStoreManageNameLength = MaxSynchronizedNameLength;
    private static readonly Regex _validDataStoreManageNamePattern = new(
        "^[A-Za-z0-9 _]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/dataStores/manage", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public async static Task<IResult> Handle(
        Validator validator,
        AddDataStoreManageCommand addDataStoreManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        AddDataStoreManageRequest request,
        HttpContext httpContext)
    {
        await validator.GuardAsync(request);

        var added = addDataStoreManageCommand.Execute(request);

        var tenantIdentifier = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;

        var jobBuilder = JobBuilder.Create<CreateInstanceJob>()
            .WithIdentity(CreateInstanceJob.CreateJobKey(added.Id, tenantIdentifier))
            .UsingJobData(JobConstants.OdsInstanceManageIdKey, added.Id);

        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            jobBuilder = jobBuilder.UsingJobData(JobConstants.TenantNameKey, tenantIdentifier);
        }

        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler();

        try
        {
            await scheduler.ScheduleJob(jobBuilder.Build(), trigger);
        }
        catch (ObjectAlreadyExistsException)
        {
            // The CreatePendingDataStoreManagesDispatcherJob may have already scheduled this job
            // (e.g. it fired between the DB insert and this ScheduleJob call). Treat duplicate
            // scheduling as success — the job is already queued and will process the OdsInstanceManage.
        }

        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStores/manage/{added.Id}");
        return Results.Accepted(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddDataStoreManageRequest")]
    public class AddDataStoreManageRequest : IAddDataStoreManageModel
    {
        [SwaggerSchema(Description = "Name of the DataStore database", Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = "Database template to use for the DataStore database", Nullable = false)]
        public string? DatabaseTemplate { get; set; }
    }

    public class Validator : AbstractValidator<AddDataStoreManageRequest>
    {
        private static readonly string[] _validDatabaseTemplates = Enum.GetNames<SandboxType>();
        private readonly AdminApiDbContext _adminApiDbContext;
        private readonly IUsersContext _usersContext;

        public Validator(AdminApiDbContext adminApiDbContext, IUsersContext usersContext)
        {
            _adminApiDbContext = adminApiDbContext;
            _usersContext = usersContext;

            RuleFor(m => m.Name)
                .NotEmpty()
                .MaximumLength(MaxDataStoreManageNameLength)
                .WithMessage($"'{{PropertyName}}' must be {MaxDataStoreManageNameLength} characters or fewer so the synchronized DataStore name fits within {MaxSynchronizedNameLength} characters.")
                .Matches(_validDataStoreManageNamePattern)
                .WithMessage("'{PropertyName}' may only contain letters, numbers, spaces, and underscores.");

            RuleFor(m => m.DatabaseTemplate).NotEmpty().MaximumLength(100)
                .Must(t => t != null && _validDatabaseTemplates.Contains(t))
                .WithMessage($"'{{PropertyValue}}' is not a valid database template. Allowed values are: {string.Join(", ", _validDatabaseTemplates)}.");

            RuleFor(m => m).CustomAsync(async (request, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name)
                    || string.IsNullOrWhiteSpace(request.DatabaseTemplate)
                    || request.Name.Length > MaxDataStoreManageNameLength
                    || !_validDataStoreManageNamePattern.IsMatch(request.Name)
                    || !_validDatabaseTemplates.Contains(request.DatabaseTemplate))
                {
                    return;
                }

                var normalizedName = request.Name.Trim();

                if (await _adminApiDbContext.OdsInstanceManages.AnyAsync(instance => instance.Name == normalizedName && instance.Status != OdsInstanceManageStatus.Deleted.ToString(), cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddDataStoreManageRequest.Name),
                        $"A DataStoreManage named '{normalizedName}' already exists.");
                    return;
                }

                if (await _usersContext.OdsInstances.AnyAsync(instance => instance.Name == normalizedName, cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddDataStoreManageRequest.Name),
                        $"A DataStore named '{normalizedName}' already exists.");
                    return;
                }

                var databaseName = DataStoreManageDatabaseNameFormatter.Build(request.Name, request.DatabaseTemplate);

                if (databaseName.Length > DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength)
                {
                    context.AddFailure(
                        nameof(AddDataStoreManageRequest.Name),
                        $"The generated database name '{databaseName}' exceeds the portable limit of {DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength} characters. Shorten Name or DatabaseTemplate.");
                }
            });
        }
    }
}
```

- [ ] **Step 3: Replace `ReadDataStoreManage.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

public class ReadDataStoreManage : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores/manage", GetDataStoreManages)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreManageModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores/manage/{id}", GetDataStoreManage)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreManageModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static Task<IResult> GetDataStoreManages(IGetDataStoreManagesQuery query,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name)
    {
        var list = DataStoreManageMapper.ToModelList(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetDataStoreManage(IGetDataStoreManageByIdQuery query, int id)
    {
        var dataStoreManage = query.Execute(id);
        if (dataStoreManage == null)
        {
            throw new NotFoundException<int>("dataStoreManage", id);
        }
        var model = DataStoreManageMapper.ToModel(dataStoreManage);
        return Task.FromResult(Results.Ok(model));
    }
}
```

- [ ] **Step 4: Replace `DeleteDataStoreManage.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

public class DeleteDataStoreManage : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapDelete(endpoints, "/dataStores/manage/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(
        IGetDataStoreManageByIdQuery getDataStoreManageByIdQuery,
        IDeleteDataStoreManageCommand deleteDataStoreManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        int id
    )
    {
        var dataStoreManage = getDataStoreManageByIdQuery.Execute(id);
        if (dataStoreManage is null)
            throw new NotFoundException<int>("dataStoreManage", id);

        if (dataStoreManage.Status == OdsInstanceManageStatus.Deleted.ToString())
            throw new NotFoundException<int>("dataStoreManage", id);

        var blockingMessage = GetBlockingStatusMessage(dataStoreManage.Status);
        if (blockingMessage is not null)
            throw new ValidationException([new ValidationFailure(nameof(id), blockingMessage)]);

        deleteDataStoreManageCommand.Execute(id);

        var tenantName = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.OdsInstanceManageIdKey] = id
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobData[JobConstants.TenantNameKey] = tenantName;
        }

        var scheduler = await schedulerFactory.GetScheduler();

        try
        {
            await QuartzJobScheduler.ScheduleJob<DeleteInstanceJob>(
                scheduler,
                DeleteInstanceJob.CreateJobKey(id, tenantName),
                jobData,
                startImmediately: true);
        }
        catch (ObjectAlreadyExistsException)
        {
            // The DeletePendingDataStoreManagesDispatcherJob may have already scheduled this job.
            // Treat duplicate scheduling as success — the job is already queued.
        }

        return Results.NoContent();
    }

    private static string? GetBlockingStatusMessage(string status)
    {
        if (Enum.TryParse<OdsInstanceManageStatus>(status, out var parsed))
        {
            return parsed switch
            {
                OdsInstanceManageStatus.PendingCreate    => "OdsInstanceManage is being provisioned. Wait for creation to complete.",
                OdsInstanceManageStatus.CreateInProgress => "OdsInstanceManage is currently being provisioned. Wait for creation to complete.",
                OdsInstanceManageStatus.CreateFailed     => "OdsInstanceManage creation failed. It will be retried automatically by the background job.",
                OdsInstanceManageStatus.CreateError      => "OdsInstanceManage creation failed permanently. Manual database intervention required before deleting.",
                OdsInstanceManageStatus.PendingDelete    => "OdsInstanceManage is already queued for deletion.",
                OdsInstanceManageStatus.DeleteInProgress => "OdsInstanceManage is currently being deleted.",
                OdsInstanceManageStatus.DeleteFailed     => "OdsInstanceManage deletion failed. It will be retried automatically by the background job.",
                OdsInstanceManageStatus.DeleteError      => "OdsInstanceManage deletion failed permanently. Manual database intervention required.",
                _ => null,
            };
        }

        return null;
    }
}
```

- [ ] **Step 5: Replace `DataStoreManageModel.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

[SwaggerSchema(Title = "DataStoreManage")]
public class DataStoreManageModel
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public int? DataStoreId { get; set; }
    public string? DataStoreName { get; set; }
    public string? Status { get; set; }
    public string? DatabaseTemplate { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? LastRefreshed { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}
```

- [ ] **Step 6: Replace `DataStoreManageMapper.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

public static class DataStoreManageMapper
{
    public static DataStoreManageModel ToModel(OdsInstanceManage source)
    {
        return new DataStoreManageModel
        {
            Id = source.Id,
            Name = source.Name,
            DataStoreId = source.OdsInstanceId,
            DataStoreName = source.OdsInstanceName,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
            LastRefreshed = source.LastRefreshed,
            LastModifiedDate = source.LastModifiedDate,
        };
    }

    public static List<DataStoreManageModel> ToModelList(IEnumerable<OdsInstanceManage> source)
    {
        return source.Select(ToModel).ToList();
    }
}
```

- [ ] **Step 7: Replace `DataStoreManageDatabaseNameFormatter.cs` content**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

internal static class DataStoreManageDatabaseNameFormatter
{
    private const string CanonicalPrefix = "EdFi_Ods";

    // Use PostgreSQL's identifier limit as the portable ceiling so the persisted
    // DatabaseName always matches the real provisioned database across engines.
    internal const int MaxPortableDatabaseNameLength = 63;

    private static readonly Regex _leadingCanonicalPrefixPattern = new(
        @"^(?:(?:edfi_+ods)(?:_+|$))+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static string Build(string dataStoreName, string databaseTemplate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataStoreName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseTemplate);

        var normalizedName = NormalizeSegment(dataStoreName);
        var normalizedDatabaseTemplate = NormalizeSegment(databaseTemplate);
        var normalizedNameWithoutPrefix = _leadingCanonicalPrefixPattern.Replace(normalizedName, string.Empty).Trim('_');

        return string.IsNullOrWhiteSpace(normalizedNameWithoutPrefix)
            ? $"{CanonicalPrefix}_{normalizedDatabaseTemplate}"
            : $"{CanonicalPrefix}_{normalizedNameWithoutPrefix}_{normalizedDatabaseTemplate}";
    }

    private static string NormalizeSegment(string value)
        => value.Replace(' ', '_').Trim('_');
}
```

- [ ] **Step 8: Build**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`
Expected: remaining errors confined to `Features\DataStores\DataStoreWithEducationOrganizationsModel.cs`/`ReadEducationOrganizations.cs` (Task 12) and `Program.cs`/`WebApplicationBuilderExtensions.cs` V3 branches (Task 13).

- [ ] **Step 9: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage" \
        "Application/EdFi.Ods.AdminApi.V3/Features/DbDataStores"
git commit -m "Move v3 DbDataStores feature into DataStores/Manage, rename routes to /dataStores/manage"
```

---

### Task 12: v3 existing `DataStores` and `Tenants` files — update references and close the `DataStoreManageId` parity gap

**Amendment (same gap as Task 5, mirrored on the v3 side):** `Features\Tenants\*` and `Infrastructure\Services\Tenants\TenantService.cs` inject `IGetDbDataStoresQuery`/`DbInstance`/`DbInstanceStatus` to build the `/tenants/{tenantName}/dataStores/edOrgs` response. v3's `TenantDetailModel.cs` does NOT reference these old names (its `TenantDataStoreModel` has no linking-ID field equivalent to v2's `DbInstanceId` — that's a pre-existing v2/v3 asymmetry, not something to fix here), so only `TenantMapper.cs`, `ReadTenants.cs`, and `TenantService.cs` need changes.

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\DataStoreWithEducationOrganizationsModel.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Features\DataStores\ReadEducationOrganizations.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Features\Tenants\TenantMapper.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Features\Tenants\ReadTenants.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Services\Tenants\TenantService.cs`

**Interfaces:**
- Consumes: `IGetDataStoreManagesQuery` (Task 10), `OdsInstanceManageStatus` (Task 2).
- Produces: `DataStoreWithEducationOrganizationsModel.DataStoreManageId` (new field, parity with v2's `OdsInstanceManageId` from Task 5), `TenantMapper.ToUnlinkedDataStoreManageModel` (renamed from `ToUnlinkedDbDataStoreModel`), `ITenantsService.GetTenantEdOrgsByInstancesAsync(..., IGetDataStoreManagesQuery, ...)`.

- [ ] **Step 1: Add the `DataStoreManageId` field**

In `DataStoreWithEducationOrganizationsModel.cs`, add a new property alongside `Id` (mirroring v2's `OdsInstanceWithEducationOrganizationsModel.OdsInstanceManageId` from Task 5):

```csharp
    [SwaggerSchema(Description = "Data store identifier")]
    public int? Id { get; set; }

    [SwaggerSchema(Description = "DataStoreManage identifier for this data store")]
    public int? DataStoreManageId { get; set; }
```

(insert the new property immediately after `Id`, before `Name`.)

- [ ] **Step 2: Update `ReadEducationOrganizations.cs`**

Replace the full file content with:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public class ReadEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/dataStores/{dataStoreId}/edOrgs", GetEducationOrganizationsByDataStore)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific data store",
                "Returns all education organizations for the specified data store in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<DataStoreWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> GetEducationOrganizationsByDataStore(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetDataStoreQuery getDataStoreQuery,
        [FromServices] IGetDataStoreManagesQuery getDataStoreManagesQuery,
        [AsParameters] CommonQueryParams commonQueryParams,
        int dataStoreId)
    {
        getDataStoreQuery.Execute(dataStoreId);

        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            dataStoreId: dataStoreId);

        MergeDataStoreManageData(educationOrganizations, getDataStoreManagesQuery);
        return Results.Ok(educationOrganizations);
    }

    private static void MergeDataStoreManageData(
        List<DataStoreWithEducationOrganizationsModel> instances,
        IGetDataStoreManagesQuery getDataStoreManagesQuery)
    {
        var allDataStoreManages = getDataStoreManagesQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

        var linkedById = allDataStoreManages
            .Where(d => d.OdsInstanceId is not null)
            .GroupBy(d => d.OdsInstanceId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

        foreach (var instance in instances)
        {
            if (instance.Id is int dataStoreId && linkedById.TryGetValue(dataStoreId, out var dataStoreManage))
            {
                instance.DataStoreManageId = dataStoreManage.Id;
                instance.Status = dataStoreManage.Status;
                instance.DatabaseTemplate = dataStoreManage.DatabaseTemplate;
                instance.DatabaseName = dataStoreManage.DatabaseName;
            }
            else
            {
                instance.Status = OdsInstanceManageStatus.Created.ToString();
            }
        }
    }
}
```

(this adds `instance.DataStoreManageId = dataStoreManage.Id;` in the linked branch, which is the new parity behavior — v2's equivalent method already sets `instance.OdsInstanceManageId` the same way.)

- [ ] **Step 3: Update `TenantMapper.cs`**

Replace the full file content with:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Features.Tenants;

public static class TenantMapper
{
    public static TenantDataStoreModel ToDataStoreModel(OdsInstance source)
    {
        return new TenantDataStoreModel
        {
            DataStoreId = source.OdsInstanceId,
            Name = source.Name,
            DataStoreType = source.InstanceType,
        };
    }

    public static List<TenantDataStoreModel> ToDataStoreModelList(IEnumerable<OdsInstance> source)
    {
        return source.Select(ToDataStoreModel).ToList();
    }

    public static TenantDataStoreModel ToUnlinkedDataStoreManageModel(OdsInstanceManage source)
    {
        return new TenantDataStoreModel
        {
            DataStoreId = null,
            Name = source.Name,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
        };
    }
}
```

- [ ] **Step 4: Update `ReadTenants.cs`**

Replace:

```csharp
        IGetDbDataStoresQuery getDbDataStoresQuery,
```

with:

```csharp
        IGetDataStoreManagesQuery getDataStoreManagesQuery,
```

and replace:

```csharp
        var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(
            getDataStoresQuery, getEducationOrganizationQuery, getDbDataStoresQuery, tenantName);
```

with:

```csharp
        var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(
            getDataStoresQuery, getEducationOrganizationQuery, getDataStoreManagesQuery, tenantName);
```

(`IGetDataStoreManagesQuery` lives in the same `EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries` namespace already imported in this file — no new using needed.)

- [ ] **Step 5: Update `TenantService.cs`**

Replace the interface line:

```csharp
    Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(IGetDataStoresQuery getDataStoresQuery, IGetEducationOrganizationQuery getEducationOrganizationQuery, IGetDbDataStoresQuery getDbDataStoresQuery, string tenantName);
```

with:

```csharp
    Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(IGetDataStoresQuery getDataStoresQuery, IGetEducationOrganizationQuery getEducationOrganizationQuery, IGetDataStoreManagesQuery getDataStoreManagesQuery, string tenantName);
```

Replace the method signature:

```csharp
    public async Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(
        IGetDataStoresQuery getDataStoresQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IGetDbDataStoresQuery getDbDataStoresQuery,
        string tenantName)
```

with:

```csharp
    public async Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(
        IGetDataStoresQuery getDataStoresQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IGetDataStoreManagesQuery getDataStoreManagesQuery,
        string tenantName)
```

Replace the method body's use of the renamed query/entity:

```csharp
            var allDbDataStores = getDbDataStoresQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

            var linkedDbDataStoresByDataStoreId = allDbDataStores
                .Where(d => d.OdsInstanceId is not null)
                .GroupBy(d => d.OdsInstanceId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

            foreach (var dataStore in tenantDetails.DataStores)
            {
                if (dataStore.DataStoreId is int dataStoreId && linkedDbDataStoresByDataStoreId.TryGetValue(dataStoreId, out var dbDataStore))
                {
                    dataStore.Status = dbDataStore.Status;
                    dataStore.DatabaseTemplate = dbDataStore.DatabaseTemplate;
                    dataStore.DatabaseName = dbDataStore.DatabaseName;
                }
                else
                {
                    dataStore.Status = DbInstanceStatus.Created.ToString();
                }
            }

            var existingDataStoreIds = tenantDetails.DataStores
                .Where(i => i.DataStoreId is int)
                .Select(i => i.DataStoreId!.Value)
                .ToHashSet();

            var unlinkedDbDataStores = allDbDataStores
                .Where(d => d.OdsInstanceId is null)
                .Concat(allDbDataStores
                    .Where(d => d.OdsInstanceId is not null && !existingDataStoreIds.Contains(d.OdsInstanceId.Value))
                    .GroupBy(d => d.OdsInstanceId!.Value)
                    .Select(g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First()))
                .ToList();
            foreach (var dbDataStore in unlinkedDbDataStores)
            {
                tenantDetails.DataStores.Add(TenantMapper.ToUnlinkedDbDataStoreModel(dbDataStore));
            }
```

with:

```csharp
            var allDataStoreManages = getDataStoreManagesQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

            var linkedDataStoreManagesByDataStoreId = allDataStoreManages
                .Where(d => d.OdsInstanceId is not null)
                .GroupBy(d => d.OdsInstanceId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

            foreach (var dataStore in tenantDetails.DataStores)
            {
                if (dataStore.DataStoreId is int dataStoreId && linkedDataStoreManagesByDataStoreId.TryGetValue(dataStoreId, out var dataStoreManage))
                {
                    dataStore.Status = dataStoreManage.Status;
                    dataStore.DatabaseTemplate = dataStoreManage.DatabaseTemplate;
                    dataStore.DatabaseName = dataStoreManage.DatabaseName;
                }
                else
                {
                    dataStore.Status = OdsInstanceManageStatus.Created.ToString();
                }
            }

            var existingDataStoreIds = tenantDetails.DataStores
                .Where(i => i.DataStoreId is int)
                .Select(i => i.DataStoreId!.Value)
                .ToHashSet();

            var unlinkedDataStoreManages = allDataStoreManages
                .Where(d => d.OdsInstanceId is null)
                .Concat(allDataStoreManages
                    .Where(d => d.OdsInstanceId is not null && !existingDataStoreIds.Contains(d.OdsInstanceId.Value))
                    .GroupBy(d => d.OdsInstanceId!.Value)
                    .Select(g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First()))
                .ToList();
            foreach (var dataStoreManage in unlinkedDataStoreManages)
            {
                tenantDetails.DataStores.Add(TenantMapper.ToUnlinkedDataStoreManageModel(dataStoreManage));
            }
```

- [ ] **Step 6: Build**

Run: `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`
Expected: remaining errors confined to `Program.cs`/`WebApplicationBuilderExtensions.cs` V3 branches (Task 13).

- [ ] **Step 7: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/DataStoreWithEducationOrganizationsModel.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/ReadEducationOrganizations.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantMapper.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Features/Tenants/ReadTenants.cs" \
        "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Tenants/TenantService.cs"
git commit -m "Update v3 DataStores and Tenants EdOrgs merges to use renamed DataStoreManage query, add DataStoreManageId for v2 parity"
```

---

### Task 13: v3 wiring — `Program.cs` V3 branch and `WebApplicationBuilderExtensions.cs` V3 branch

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\Program.cs` (V3 branch only — the shared interval-variable renames were already done in Task 6)
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\WebApplicationBuilderExtensions.cs` (V3 branch only)

**Interfaces:**
- Consumes: `JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName`/`DeletePendingOdsInstanceManagesDispatcherJobName` (Task 2 — same shared constants v2 uses, since `JobConstants` isn't version-specific), `CreatePendingDataStoreManagesDispatcherJob`/`DeletePendingDataStoreManagesDispatcherJob` (Task 10, the V3-namespaced classes aliased as `V3Jobs.*`).
- Produces: fully wired v3 Quartz scheduling — the last v3-side wiring file; Task 14's build checkpoint depends on this.

- [ ] **Step 1: Update job-type references in the `AdminApiMode.V3` branch of `Program.cs`**

Replace every occurrence in the V3 branch of:

```csharp
await QuartzJobScheduler.ScheduleJob<V3Jobs.CreatePendingDbInstancesDispatcherJob>(
    scheduler,
    jobKey: new JobKey($"{JobConstants.CreatePendingDbInstancesDispatcherJobName}_{tenantName}"),
```

and the non-multi-tenant equivalent

```csharp
await QuartzJobScheduler.ScheduleJob<V3Jobs.CreatePendingDbInstancesDispatcherJob>(
    scheduler,
    jobKey: new JobKey(JobConstants.CreatePendingDbInstancesDispatcherJobName),
```

with `V3Jobs.CreatePendingDataStoreManagesDispatcherJob` and `JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName` (both branches). Do the same for `V3Jobs.DeletePendingDbInstancesDispatcherJob` → `V3Jobs.DeletePendingDataStoreManagesDispatcherJob` with `JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName`.

Note: `JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName`/`DeletePendingOdsInstanceManagesDispatcherJobName` are the same shared string constants v2 uses (from Task 2) — only the generic type parameter (`V3Jobs.CreatePendingDataStoreManagesDispatcherJob` vs. v2's bare `CreatePendingOdsInstanceManagesDispatcherJob`) differs between branches, matching the existing pre-rename pattern where both branches already used the same `JobConstants.CreatePendingDbInstancesDispatcherJobName` string.

- [ ] **Step 2: Update `WebApplicationBuilderExtensions.cs` DI registrations for the V3 branch**

Replace:

```csharp
            webApplicationBuilder.Services.AddTransient<
                EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs.CreatePendingDbInstancesDispatcherJob
            >();
```

with:

```csharp
            webApplicationBuilder.Services.AddTransient<
                EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs.CreatePendingDataStoreManagesDispatcherJob
            >();
```

and replace:

```csharp
            webApplicationBuilder.Services.AddTransient<
                EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs.DeletePendingDbInstancesDispatcherJob
            >();
```

with:

```csharp
            webApplicationBuilder.Services.AddTransient<
                EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs.DeletePendingDataStoreManagesDispatcherJob
            >();
```

(the surrounding `CreateInstanceJob`/`DeleteInstanceJob`/`JobStatusService` registrations in this same V3 block are unchanged — only the two dispatcher job type references change.)

- [ ] **Step 3: Build the full solution**

Run: `dotnet build` at the repository root (build everything — both v2 and v3 solutions/projects).
Expected: PASS with zero errors. Grep the full source tree for `DbInstance` and `DbDataStore` (`grep -rn "DbInstance\|DbDataStore" Application --include=*.cs`) — the only remaining matches at this point should be inside test projects (Tasks 7, 8, 14, 15 — not yet done) and E2E/`.http` artifacts (Tasks 9, 16, 17 — not yet done). No matches should remain in any non-test `.cs` file under `Application\EdFi.Ods.AdminApi\` or `Application\EdFi.Ods.AdminApi.V3\` (excluding their `E2E Tests` folders, which Tasks 9/16 handle).

- [ ] **Step 4: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/Program.cs" \
        "Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs"
git commit -m "Update v3 Quartz wiring to use renamed DataStoreManage dispatcher jobs"
```

---

### Task 14: v3 Unit tests rename + add missing query-test coverage (`EdFi.Ods.AdminApi.V3.UnitTests`)

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DbDataStores\AddDbDataStoreTests.cs` → move+rename to `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DataStores\Manage\AddDataStoreManageTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DbDataStores\ReadDbDataStoreTests.cs` → move+rename to `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DataStores\Manage\ReadDataStoreManageTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DbDataStores\DeleteDbDataStoreTests.cs` → move+rename to `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DataStores\Manage\DeleteDataStoreManageTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Database\Commands\AddDbDataStoreCommandTests.cs` → rename to `AddDataStoreManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Database\Commands\DeleteDbDataStoreCommandTests.cs` → rename to `DeleteDataStoreManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Jobs\CreatePendingDbInstancesDispatcherJobTests.cs` → rename to `CreatePendingDataStoreManagesDispatcherJobTests.cs` (search first to confirm exact path — the initial research report listed this file but it wasn't independently verified in this plan's file reads)
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Jobs\DeletePendingDbInstancesDispatcherJobTests.cs` → rename to `DeletePendingDataStoreManagesDispatcherJobTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Jobs\CreateInstanceJobTests.cs` (filename unchanged — update `DbDataStore`/`DbInstance` references only)
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Jobs\DeleteInstanceJobTests.cs` (filename unchanged — update references only)
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DataStores\ReadEducationOrganizationsTests.cs` (filename unchanged — update `IGetDbDataStoresQuery`/`DbInstance` references)
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Tenants\TenantServiceTests.cs` (filename unchanged — update references)
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\Tenants\ReadTenantsTest.cs` (filename unchanged — update references)
- Create: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Database\Queries\GetDataStoreManagesQueryTests.cs` (new — closes the coverage gap)
- Create: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Database\Queries\GetDataStoreManageByIdQueryTests.cs` (new — closes the coverage gap)

**Interfaces:**
- Consumes: every v3 production type from Tasks 10–13.

- [ ] **Step 1: Move and rename the Command test files (full content known — apply directly)**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/AddDbDataStoreCommandTests.cs" "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/AddDataStoreManageCommandTests.cs"
git mv "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteDbDataStoreCommandTests.cs" "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteDataStoreManageCommandTests.cs"
```

`AddDataStoreManageCommandTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddDataStoreManageCommandTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"AddDataStoreManageCommand_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "SqlServer"
            })
            .Build();
        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_WithValidModel_PersistsOdsInstanceManage()
    {
        using var context = CreateContext();
        var command = new AddDataStoreManageCommand(context);
        var model = new AddDataStoreManageModelStub
        {
            Name = "Test Instance",
            DatabaseTemplate = "Minimal"
        };

        var result = command.Execute(model);

        result.Id.ShouldBeGreaterThan(0);
        context.OdsInstanceManages.Any(d => d.Id == result.Id).ShouldBeTrue();
    }

    [Test]
    public void Execute_WithValidModel_SetsExpectedFieldValues()
    {
        using var context = CreateContext();
        var command = new AddDataStoreManageCommand(context);
        var before = DateTime.UtcNow;
        var model = new AddDataStoreManageModelStub
        {
            Name = "  Test Instance  ",
            DatabaseTemplate = " Minimal "
        };

        var result = command.Execute(model);

        result.Name.ShouldBe("Test Instance");
        result.DatabaseTemplate.ShouldBe("Minimal");
        result.Status.ShouldBe(OdsInstanceManageStatus.PendingCreate.ToString());
        result.OdsInstanceId.ShouldBeNull();
        result.OdsInstanceName.ShouldBeNull();
        result.DatabaseName.ShouldBeNull();
        result.LastRefreshed.ShouldBeGreaterThanOrEqualTo(before);
        result.LastModifiedDate.ShouldNotBeNull();
        result.LastModifiedDate!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public void Execute_WithSampleTemplate_PersistsWithCorrectTemplate()
    {
        using var context = CreateContext();
        var command = new AddDataStoreManageCommand(context);
        var model = new AddDataStoreManageModelStub
        {
            Name = "Sample Instance",
            DatabaseTemplate = "Sample"
        };

        var result = command.Execute(model);

        result.DatabaseTemplate.ShouldBe("Sample");
        context.OdsInstanceManages.Any(d => d.Id == result.Id && d.DatabaseTemplate == "Sample").ShouldBeTrue();
    }

    private sealed class AddDataStoreManageModelStub : IAddDataStoreManageModel
    {
        public string? Name { get; set; }
        public string? DatabaseTemplate { get; set; }
    }
}

#nullable restore
```

`DeleteDataStoreManageCommandTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class DeleteDataStoreManageCommandTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteDataStoreManageCommand_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:DatabaseEngine"] = "SqlServer" }
            )
            .Build();
        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_SetsStatusToPendingDelete()
    {
        using var context = CreateContext();
        var instance = new OdsInstanceManage
        {
            Name = "Test Instance",
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow,
        };
        context.OdsInstanceManages.Add(instance);
        context.SaveChanges();

        var command = new DeleteDataStoreManageCommand(context);
        command.Execute(instance.Id);

        var updated = context.OdsInstanceManages.Single(d => d.Id == instance.Id);
        updated.Status.ShouldBe(OdsInstanceManageStatus.PendingDelete.ToString());
    }

    [Test]
    public void Execute_UpdatesLastModifiedDate()
    {
        using var context = CreateContext();
        var before = DateTime.UtcNow;
        var instance = new OdsInstanceManage
        {
            Name = "Test Instance",
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow,
        };
        context.OdsInstanceManages.Add(instance);
        context.SaveChanges();

        var command = new DeleteDataStoreManageCommand(context);
        command.Execute(instance.Id);

        var updated = context.OdsInstanceManages.Single(d => d.Id == instance.Id);
        updated.LastModifiedDate.ShouldNotBeNull();
        updated.LastModifiedDate!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public void Execute_WithNonExistentId_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var command = new DeleteDataStoreManageCommand(context);

        Should.Throw<NotFoundException<int>>(() => command.Execute(9999));
    }

    [Test]
    public void Execute_WhenStatusIsDeleted_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var instance = new OdsInstanceManage
        {
            Name = "Test Instance",
            Status = OdsInstanceManageStatus.Deleted.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow,
        };
        context.OdsInstanceManages.Add(instance);
        context.SaveChanges();

        var command = new DeleteDataStoreManageCommand(context);

        Should.Throw<NotFoundException<int>>(() => command.Execute(instance.Id));
    }
}

#nullable restore
```

- [ ] **Step 2: Write the new `GetDataStoreManagesQueryTests.cs` (closes the pre-existing v3 coverage gap)**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDataStoreManagesQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreManagesQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_WithoutFilters_ReturnsAllOdsInstanceManages()
    {
        using var context = CreateContext();
        context.OdsInstanceManages.AddRange(
            new OdsInstanceManage { Name = "Sandbox A", Status = "Healthy", DatabaseTemplate = "Minimal" },
            new OdsInstanceManage { Name = "Sandbox B", Status = "Healthy", DatabaseTemplate = "Minimal" });
        context.SaveChanges();

        var query = new GetDataStoreManagesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, null);

        result.Count.ShouldBe(2);
        result.Select(x => x.Name).ShouldBe(["Sandbox A", "Sandbox B"], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsMatchingOdsInstanceManage()
    {
        using var context = CreateContext();
        context.OdsInstanceManages.AddRange(
            new OdsInstanceManage { Name = "Sandbox A", Status = "Healthy", DatabaseTemplate = "Minimal" },
            new OdsInstanceManage { Name = "Sandbox B", Status = "Healthy", DatabaseTemplate = "Minimal" });
        context.SaveChanges();

        var query = new GetDataStoreManagesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, "Sandbox B");

        result.Count.ShouldBe(1);
        result.Single().Name.ShouldBe("Sandbox B");
    }
}
```

- [ ] **Step 3: Write the new `GetDataStoreManageByIdQueryTests.cs` (closes the pre-existing v3 coverage gap)**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDataStoreManageByIdQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreManageByIdQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_WithExistingId_ReturnsOdsInstanceManage()
    {
        using var context = CreateContext();
        var odsInstanceManage = new OdsInstanceManage
        {
            Name = "Sandbox",
            Status = "Healthy",
            DatabaseTemplate = "Minimal"
        };
        context.OdsInstanceManages.Add(odsInstanceManage);
        context.SaveChanges();

        var query = new GetDataStoreManageByIdQuery(context);

        var result = query.Execute(odsInstanceManage.Id);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Sandbox");
    }

    [Test]
    public void Execute_WithUnknownId_ReturnsNull()
    {
        using var context = CreateContext();
        var query = new GetDataStoreManageByIdQuery(context);

        var result = query.Execute(999);

        result.ShouldBeNull();
    }
}
```

- [ ] **Step 4: Move and rename the remaining v3 unit test files, applying the Master Rename Table**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DbDataStores/AddDbDataStoreTests.cs" "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/AddDataStoreManageTests.cs"
git mv "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DbDataStores/ReadDbDataStoreTests.cs" "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/ReadDataStoreManageTests.cs"
git mv "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DbDataStores/DeleteDbDataStoreTests.cs" "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/DeleteDataStoreManageTests.cs"
```

Search for the exact dispatcher-job-test filenames first (`Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Jobs\`) — rename `CreatePendingDbInstancesDispatcherJobTests.cs`/`DeletePendingDbInstancesDispatcherJobTests.cs` to `CreatePendingDataStoreManagesDispatcherJobTests.cs`/`DeletePendingDataStoreManagesDispatcherJobTests.cs` if present.

For each moved file, apply every substitution from the Master Rename Table (class name matching new filename, `namespace ...V3.UnitTests.Features.DbDataStores` → `...V3.UnitTests.Features.DataStores.Manage`, route string literals `"/dbDataStores"` → `"/dataStores/manage"` if asserted, mock setups referencing `IGetDbDataStoresQuery`/`AddDbDataStoreCommand`/etc. → their renamed equivalents).

- [ ] **Step 5: Update the four non-renamed files that reference `DbDataStore`/`DbInstance` internally**

Open each of the following and replace every `DbDataStore`/`DbInstance`-family token per the Master Rename Table (mock setups, fixture data, assertions) without changing the filename or test class name:
- `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Jobs\CreateInstanceJobTests.cs`
- `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Jobs\DeleteInstanceJobTests.cs`
- `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\DataStores\ReadEducationOrganizationsTests.cs`
- `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\Services\Tenants\TenantServiceTests.cs`
- `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\Tenants\ReadTenantsTest.cs`

- [ ] **Step 6: Run the v3 unit test suite**

Run: `dotnet test Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj`
Expected: PASS. Test count should be the pre-rename baseline **plus 4** (the two new `GetDataStoreManagesQueryTests` tests and two new `GetDataStoreManageByIdQueryTests` tests).

- [ ] **Step 7: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DbDataStores" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/AddDataStoreManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteDataStoreManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreManagesQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreManageByIdQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Services/Jobs" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/ReadEducationOrganizationsTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Tenants/ReadTenantsTest.cs"
git commit -m "Rename v3 unit tests to DataStoreManage naming, add missing query test coverage"
```

---

### Task 15: v3 DBTests rename (`EdFi.Ods.AdminApi.V3.DBTests`)

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.V3.DBTests\Database\CommandTests\AddDbDataStoreCommandTests.cs` → rename to `AddDataStoreManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.DBTests\Database\CommandTests\DeleteDbDataStoreCommandTests.cs` → rename to `DeleteDataStoreManageCommandTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.DBTests\Database\QueryTests\GetDbDataStoreByIdQueryTests.cs` → rename to `GetDataStoreManageByIdQueryTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.DBTests\Database\QueryTests\GetDbDataStoresQueryTests.cs` → rename to `GetDataStoreManagesQueryTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3.DBTests\Database\QueryTests\GetTenantEdOrgsByDataStoresTests.cs` (filename unchanged — update internal references only)

**Interfaces:**
- Consumes: same v3 production types as Task 14, against a real database.

- [ ] **Step 1: Move and rename**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/AddDbDataStoreCommandTests.cs" "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/AddDataStoreManageCommandTests.cs"
git mv "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/DeleteDbDataStoreCommandTests.cs" "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/DeleteDataStoreManageCommandTests.cs"
git mv "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDbDataStoreByIdQueryTests.cs" "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreManageByIdQueryTests.cs"
git mv "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDbDataStoresQueryTests.cs" "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreManagesQueryTests.cs"
```

Apply the Master Rename Table to each moved file's content (class name, `AdminApiDbContext.DbInstances`→`.OdsInstanceManages`, `DbInstance`/`DbInstanceStatus`→`OdsInstanceManage`/`OdsInstanceManageStatus`, command/query type names to their `DataStoreManage`-prefixed v3 equivalents). In `GetTenantEdOrgsByDataStoresTests.cs` (filename unchanged), update only the internal references to renamed types, not the filename or class name.

- [ ] **Step 2: Run the v3 DB test suite**

Run: `dotnet test Application/EdFi.Ods.AdminApi.V3.DBTests/EdFi.Ods.AdminApi.V3.DBTests.csproj` (requires the local test database with Task 1's migration applied).
Expected: PASS, same test count as the pre-rename baseline.

- [ ] **Step 3: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/AddDataStoreManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/DeleteDataStoreManageCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreManageByIdQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreManagesQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetTenantEdOrgsByDataStoresTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/AddDbDataStoreCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/CommandTests/DeleteDbDataStoreCommandTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDbDataStoreByIdQueryTests.cs" \
        "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDbDataStoresQueryTests.cs"
git commit -m "Rename v3 DBTests to DataStoreManage naming"
```

---

### Task 16: v3 Bruno E2E collection rename

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.V3\E2E Tests\Bruno Admin API E2E 3.0\v3\DbDataStores\` → move all files to `...\v3\DataStores\Manage\`

**Interfaces:**
- Consumes: routes `/v3/dataStores/manage*` (Task 11).

- [ ] **Step 1: Move the folder and rename every `.bru` file**

```bash
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/folder.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/folder.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/POST - DbDataStores.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/POST - DataStores Manage.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/POST - DbDataStores - Sample Template.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/POST - DataStores Manage - Sample Template.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/POST - DbDataStores - Invalid.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/POST - DataStores Manage - Invalid.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/POST - DbDataStores - Invalid Database Template.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/POST - DataStores Manage - Invalid Database Template.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/GET - DbDataStores.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/GET - DataStores Manage.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/GET - DbDataStores by ID.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/GET - DataStores Manage by ID.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/GET - DbDataStores - Without Offset.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/GET - DataStores Manage - Without Offset.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/GET - DbDataStores - Without Limit.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/GET - DataStores Manage - Without Limit.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/GET - DbDataStores - Without Limit and Offset.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/GET - DataStores Manage - Without Limit and Offset.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/GET - DbDataStores - Not Found.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/GET - DataStores Manage - Not Found.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/GET - DbDataStores - Filter by Name.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/GET - DataStores Manage - Filter by Name.bru"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/DELETE - DbDataStore - Success.bru.disabled" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/DELETE - DataStore Manage - Success.bru.disabled"
git mv "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores/DELETE - DbDataStore - Not Found.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/DELETE - DataStore Manage - Not Found.bru"
```

- [ ] **Step 2: Update `folder.bru`**

```
meta {
  name: Manage
  seq: 99
}

auth {
  mode: inherit
}
```

- [ ] **Step 3: Update every moved `.bru` file's content**

Same substitution pattern as Task 9 Step 3: `meta { name: DbDataStores ... }` → `meta { name: DataStores Manage ... }`, request URL `{{API_URL}}/v3/dbDataStores...` → `{{API_URL}}/v3/dataStores/manage...`, any `bru.setVar`/`{{...}}` variable named `CreatedDbDataStoreId`-style → `CreatedDataStoreManageId`, and `test("POST DbDataStores: ...")`-style description strings → `DataStores Manage`.

For example, `POST - DataStores Manage.bru`:

```
meta {
  name: DataStores Manage
  type: http
  seq: 1
}

post {
  url: {{API_URL}}/v3/dataStores/manage
  body: json
  auth: inherit
}

body:json {
  {
    "name": "Test DB Instance",
    "databaseTemplate": "Minimal"
  }
}

script:post-response {
  test("POST DataStores Manage: Status code is Accepted", function () {
    expect(res.getStatus()).to.equal(202);
  });

  test("POST DataStores Manage: Response includes location in header", function () {
    expect(res.getHeaders()).to.have.property("location");
    const id = res.getHeader("location").split("/").pop();
    if (id) {
      bru.setVar("CreatedDataStoreManageId", id);
    }
  });
}

settings {
  encodeUrl: true
}
```

Apply the same URL/variable/assertion-text substitution pattern to the remaining files based on each file's current content (note v3's POST uses an absolute `Location` header via `ResourceUrlHelper.BuildAbsoluteResourceUrl`, unlike v2's relative path — preserve whatever ID-extraction logic the original `.bru` file used).

- [ ] **Step 4: Run the v3 Bruno E2E suite**

Run: `./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TenantMode singletenant -TearDown` (adjust `-TenantMode` per `docs/developer.md`).
Expected: PASS, all `DataStores Manage` requests succeed with the same assertions as before the rename.

- [ ] **Step 5: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage" \
        "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DbDataStores"
git commit -m "Rename v3 Bruno E2E DbDataStores collection to DataStores/Manage"
```

---

### Task 17: Rename and update `docs/http/dbinstances.http`

**Files:**
- Modify: `docs\http\dbinstances.http` → rename to `docs\http\odsinstances-manage.http`

**Interfaces:**
- Consumes: routes `/v2/odsInstances/manage*` (Task 4) and `/v3/dataStores/manage*` (Task 11).

- [ ] **Step 1: Read the file's current (uncommitted-edits-included) content first**

Read `docs\http\dbinstances.http` fresh (don't rely on the diff seen earlier in this conversation — more local edits may have landed since) to get the exact current content, since this file has uncommitted personal edits that must be preserved.

- [ ] **Step 2: Rename the file**

```bash
git mv "docs/http/dbinstances.http" "docs/http/odsinstances-manage.http"
```

- [ ] **Step 3: Update every request URL in the file**

Replace every occurrence of:
- `{{adminapi_url}}/v2/dbinstances` → `{{adminapi_url}}/v2/odsinstances/manage`
- `{{adminapi_url}}/v3/dbDataStores` → `{{adminapi_url}}/v3/dataStores/manage`

preserving any path suffix (`/1`, `?offset=0&limit=10`, etc.), any `@name` block labels that reference `createDbInstance...` (rename to `createOdsInstanceManage...` for v2 blocks, `createDataStoreManage...` for v3 blocks), and every other line in the file exactly as currently written (commented-out `@adminapi_url` lines, tenant headers, existing `/v2/odsinstances` and `/v3/dataStores` blocks that were already correct, etc.).

- [ ] **Step 4: Manually smoke-test a couple of requests**

Using the VS Code REST Client (or whichever `.http` runner this repo's contributors use per `docs/developer.md`), run the renamed `POST`/`GET`/`DELETE` blocks against a locally running v2 and v3 instance and confirm they hit `/odsInstances/manage`/`/dataStores/manage` successfully.

- [ ] **Step 5: Commit**

```bash
git add "docs/http/odsinstances-manage.http" "docs/http/dbinstances.http"
git commit -m "Rename docs/http/dbinstances.http to odsinstances-manage.http, update routes"
```

---

### Task 18: Full solution verification

**Files:** none (verification only).

- [ ] **Step 1: Full clean build**

Run: `dotnet build` at the repository root (or `./build.ps1 build` per this repo's build script convention).
Expected: zero errors, zero warnings introduced by this change.

- [ ] **Step 2: Full unit test suite**

Run: `./build.ps1 -Command UnitTest`
Expected: PASS. Total test count = pre-rename baseline + 4 (Task 14's new v3 query tests).

- [ ] **Step 3: Full integration (DBTests) suite**

Run the DBTests projects per `docs/developer.md`'s integration-test instructions (apply Task 1's migration to the test database first if not already applied).
Expected: PASS, same test count as pre-rename baseline.

- [ ] **Step 4: Full-repo grep for any remaining stray references**

Run: `grep -rn "DbInstance\|DbDataStore" --include=*.cs --include=*.sql --include=*.json --include=*.bru --include=*.http .` from the repository root.
Expected: zero matches, except:
- Historical migration scripts `00001`–`00006` (which correctly still say `DbInstances` — they describe the table's state at that point in history and must not be edited).
- This plan document and the design spec document themselves (`docs/superpowers/plans/...`, `docs/superpowers/specs/...`), which describe the rename and legitimately mention the old names.

- [ ] **Step 5: Run both v2 and v3 Bruno E2E suites end-to-end**

Run: `./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TenantMode singletenant -TearDown` and `./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TenantMode singletenant -TearDown` (and the multitenant variants if this repo's CI runs those too — check `docs/developer.md`).
Expected: PASS.

- [ ] **Step 6: Final review pass**

Re-read the Master Rename Table at the top of this plan against the grep output from Step 4 to confirm every listed identifier was actually renamed everywhere it appeared — no partial renames left (e.g. a class renamed but a lingering XML doc comment or log message still saying the old name).

No commit for this task — it's a verification checkpoint. If Step 4 or 6 turns up a stray reference, fix it in place and amend the most relevant task's commit (or add a small follow-up commit), then re-run the affected verification step.




