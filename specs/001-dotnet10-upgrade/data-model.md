# Data Model: .NET 10 Upgrade — ODS Admin API

**Branch**: `001-dotnet10-upgrade` | **Date**: 2025-07-14

---

## Overview

This upgrade introduces **no new entities, database tables, or schema changes**. The data model
document focuses on EF Core compatibility analysis for the existing models to ensure the .NET 8 →
.NET 10 upgrade (including EF Core 8.0.x → 10.0.6) does not silently alter migration behaviour or
column type mappings.

---

## EF Core Context Inventory

| Context Interface | Concrete Type | Database | Projects |
|-------------------|---------------|----------|---------|
| `IUsersContext` | `UsersContext` | `EdFi_Admin` | `EdFi.Ods.AdminApi`, `EdFi.Ods.AdminApi.Common` |
| `ISecurityContext` | `SecurityContext` | `EdFi_Security` | `EdFi.Ods.AdminApi`, `EdFi.Ods.AdminApi.Common` |

Both contexts originate from the `EdFi.Suite3.Admin.DataAccess` and
`EdFi.Suite3.Security.DataAccess` NuGet packages (versions 7.3.67 and 7.4.102 respectively). These
packages are **not being upgraded** as part of this feature — their EF Core dependency is resolved
transitively and compatibility with EF Core 10.0.6 must be validated during the build step.

The Admin API's own internal management tables are provisioned via DbUp migration scripts under
`Application/EdFi.Ods.AdminApi/Artifacts/` and are not affected by the EF Core upgrade.

---

## EF Core 8 → 10.0.6 Compatibility Notes

### Nullable Annotation Changes (EF Core 9)

EF Core 9 changed how nullable reference types interact with entity models:

- **Complex type properties**: Struct-based complex properties now enforce nullability at the model
  level. If any entity in `EdFi.Suite3.Admin.DataAccess` or `EdFi.Suite3.Security.DataAccess` uses
  struct complex types, the EF model may differ from EF 8.
- **Navigation properties**: EF Core 9+ emits CS8618 compiler warnings for uninitialized non-null
  reference navigation properties. These will not break the build unless `TreatWarningsAsErrors` is
  active for the affected packages.

**Validation step**: After applying the upgrade, run:
```powershell
dotnet ef migrations add _VerifyNoChanges `
  --project Application/EdFi.Ods.AdminApi `
  --startup-project Application/EdFi.Ods.AdminApi `
  --context UsersContext
```
The generated migration file **must be empty** (no `Up()` / `Down()` operations). If changes are
detected, review and document them before proceeding. Then delete the `_VerifyNoChanges` migration.
Repeat for `SecurityContext`.

### Npgsql 8.0.x → 10.0.2 Type Mapping Changes

Npgsql 10.x is a major version upgrade. Known considerations:
- **Enum type mapping**: Npgsql 10 has stricter PostgreSQL enum type registration. Verify any custom
  enum type mappings compile and resolve correctly.
- **Array type handling**: Minor changes to how NpgsqlDbType.Array is resolved. Standard .NET array
  and `List<T>` properties are unaffected.
- **Connection string parameters**: Npgsql 10 may deprecate some legacy connection string keys.
  Verify connection strings in `appsettings.json` and `appsettings.dockertemplate.json` compile
  without warnings.

**Validation step**: Run the full `EdFi.Ods.AdminApi.DBTests` suite against a live PostgreSQL
instance post-upgrade.

### `EFCore.NamingConventions` 8.0.3 → 10.0.1

This package provides snake_case column name conventions. The major version bump from 8 to 10
tracks EF Core versioning. No breaking API changes are expected in the `UseSnakeCaseNamingConvention()`
call pattern. Validate that all table and column names resolve identically post-upgrade by running
the DB tests.

---

## Migration Discipline Checklist

| Check | Method | Expected Result |
|-------|--------|----------------|
| No schema drift from EF Core upgrade | `dotnet ef migrations add _Verify...` | Empty migration (no Up/Down ops) |
| No column type changes | DB test suite with `Respawn` reset | All CRUD operations pass |
| No query behavioural regression | Integration tests (`DBTests` projects) | 100% pass rate |
| Both PostgreSQL and MSSQL validated | Run `build.ps1 -Command IntegrationTest` for each engine | Zero failures |

---

## No New Entities

This upgrade does not introduce any new:
- Entity classes
- DbSet properties
- Migration files (in `Application/EdFi.Ods.AdminApi/Artifacts/`)
- Database seed data
- DbUp script changes (in `eng/run-dbup-migrations.ps1`)

The migration gate is **PASS** by design: if EF Core 10 produces a non-empty model diff, that is a
bug to be resolved before the feature is considered complete, not a new migration to be committed.
