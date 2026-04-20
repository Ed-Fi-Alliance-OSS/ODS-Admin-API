# Quickstart: .NET 10 Upgrade — ODS Admin API

**Branch**: `001-dotnet10-upgrade` | **Date**: 2025-07-14

This guide walks a developer through verifying the .NET 10 upgrade locally after the changes in
`tasks.md` have been applied.

---

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| .NET 10.0 SDK | 10.0.202 or later | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Docker Desktop | 24.x or later | Required for Dockerfile and Compose validation |
| PostgreSQL | 15+ | For DB/integration tests |
| PowerShell | 7.x | Required for `build.ps1` |

> ⚠️ Remove or deactivate any `.NET 8.0 SDK`-only installations before building, to prevent
> the wrong SDK from being resolved. Use `global.json` if you need to pin the SDK version
> per repository.

---

## Step 1: Verify SDK

```powershell
dotnet --version
# Expected: 10.0.202 or later
```

---

## Step 2: Restore and Build

```powershell
cd <repo-root>
./build.ps1 -Command build
```

Expected: Zero compilation errors. Zero framework-targeting warnings (`net8.0` references must be
absent from all output).

### What changed
- All 10 `.csproj` files: `<TargetFramework>net10.0</TargetFramework>`
- `Directory.Packages.props`: 8 package version pins updated (EF Core, JwtBearer, Npgsql,
  EFCore.NamingConventions); Quartz 3.x → 4.x; Swashbuckle 7.x → 10.2.0
- `build.ps1`: `net8.0` path strings replaced with `net10.0` (lines 219, 550)
- `EdFi.Ods.AdminApi.csproj`: `<DisableImplicitOpenApiGeneration>true</DisableImplicitOpenApiGeneration>` added

---

## Step 3: Run Unit Tests

```powershell
./build.ps1 -Command UnitTest
```

Expected: All previously-passing tests pass. No new failures. Test output paths will be under
`bin/Release/net10.0/`.

---

## Step 4: Verify No EF Core Model Drift

Before running DB tests, verify EF Core 10.0.6 has not silently changed the schema model:

```powershell
# In the Application directory
cd Application

# Check UsersContext
dotnet ef migrations add _VerifyNoChanges `
  --project EdFi.Ods.AdminApi `
  --startup-project EdFi.Ods.AdminApi `
  --context UsersContext

# The generated migration file should have empty Up() and Down() methods.
# If it is non-empty, DO NOT COMMIT. Investigate the diff and resolve.

# Clean up the test migration
dotnet ef migrations remove `
  --project EdFi.Ods.AdminApi `
  --startup-project EdFi.Ods.AdminApi `
  --context UsersContext
```

Repeat for `SecurityContext`.

---

## Step 5: Run Integration / DB Tests

```powershell
# Requires a running PostgreSQL instance with test databases provisioned
./build.ps1 -Command IntegrationTest
```

Expected: 100% pass rate matching the pre-upgrade baseline.

---

## Step 6: Build Docker Images

```powershell
# Build each of the 4 application Dockerfiles
docker build -f Docker/dev.pgsql.Dockerfile -t adminapi-dev-pgsql:net10 .
docker build -f Docker/dev.mssql.Dockerfile -t adminapi-dev-mssql:net10 .
docker build -f Docker/api.pgsql.Dockerfile -t adminapi-api-pgsql:net10 .
docker build -f Docker/api.mssql.Dockerfile -t adminapi-api-mssql:net10 .
```

Expected: All four images build without errors. Base image pull logs should reference `.NET 10`
tags.

---

## Step 7: Validate Running Container

```powershell
# Use the dev compose file (pgsql single-tenant as example)
# First generate SSL cert if not done:
bash Docker/Settings/ssl/generate-certificate.sh

# Then launch:
docker compose -f Docker/Compose/pgsql/SingleTenant/compose-build-dev.yml up

# In another terminal, hit the health endpoint:
curl -k https://localhost:<PORT>/health
```

Expected: HTTP 200 Healthy response. Container logs should show `dotnet` process starting on .NET 10.

---

## Step 8: Update Documentation

After the build passes, update the following files:

### `docs/developer.md`
- Line 21: Change `.NET 8.0 SDK` link to `.NET 10.0 SDK`
  ```
  * [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
  ```
- Line 38: Change "8.0 SDK" to "10.0 SDK"
  ```
  .NET 10.0 SDK or newer is installed.
  ```

### `AGENTS.md`
- Review for any SDK or framework version references; update to `.NET 10` as found.

### `.specify/memory/constitution.md`
- Post-merge: Update §Technology & Runtime Constraints to reference `net10.0` instead of `net8.0`.
  This is the constitution amendment follow-up documented in the plan.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| `NU1202: Package Quartz 3.x is not compatible with net10.0` | Old Quartz version in `Directory.Packages.props` | Upgrade Quartz to 4.x |
| Duplicate OpenAPI route at `/openapi/...` | Built-in OpenAPI generator active alongside Swashbuckle | Add `<DisableImplicitOpenApiGeneration>true</DisableImplicitOpenApiGeneration>` to main `.csproj` |
| EF Core migration generates schema changes | Complex property nullable model difference in EF 10 | Investigate entity model; add `= null!` annotations defensively; do NOT commit the migration |
| Docker build fails on `FROM mcr.microsoft.com/dotnet/sdk:10.0...` | Tag not pulled yet | Run `docker pull mcr.microsoft.com/dotnet/sdk:10.0.202-alpine3.23` first |
| Unit tests fail with `CS8618` | Nullable annotation enforcement stricter in C# 13 | Add null-forgiving operator or explicit initializer on the flagged property |
| Quartz job scheduling errors at startup | Breaking API change in Quartz 3 → 4 | Review Quartz job registration code; Quartz 4 uses `AddJob<T>()` instead of `AddJob(JobBuilder...)` for some patterns |
