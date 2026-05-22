# .NET 10 Upgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade ODS Admin API from .NET 8 to .NET 10 with a hard cutover, preserving behavior while updating project targets, tooling, Docker runtime images, compatibility-required packages, and operational docs. Includes the newly added V3 specification projects.

**Architecture:** This is a platform-only migration. We will keep feature behavior and API contracts unchanged and execute in staged layers: framework retargeting, script/toolchain alignment, Docker/runtime verification, compatibility-only package updates, then verification and docs cleanup. Each task is independently testable and committed before moving on.

**Tech Stack:** .NET 10 SDK/runtime, C#/.csproj, PowerShell build automation, GitHub Actions YAML, Docker (aspnet/sdk alpine images), NUnit/Shouldly/FakeItEasy test suites.

**Validation commands (run after every task):**
- `./build.ps1 -Command Build` — confirms the solution builds clean.
- `./build.ps1 -Command UnitTest` — confirms no unit test regressions.
- `./eng/run-e2e-bruno.ps1 -ApiVersion 1 -TearDown` — end-to-end smoke test (V1, pgsql). `-TearDown` removes containers after the run so failures can be cleanly attributed to the upgrade.
- `./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TearDown` — end-to-end smoke test (V2, single-tenant, pgsql).
- `./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TearDown` — end-to-end smoke test (V3, single-tenant, pgsql).
- Not all validation commands need to run after every task — the relevant ones are called out in each task's verification step.

---

## File Structure and Responsibilities

- `Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj` — main API target framework.
- `Application\EdFi.Ods.AdminApi.Common\EdFi.Ods.AdminApi.Common.csproj` — shared API library target framework.
- `Application\EdFi.Ods.AdminApi.InstanceManagement\EdFi.Ods.AdminApi.InstanceManagement.csproj` — instance management target framework.
- `Application\EdFi.Ods.AdminApi.V1\EdFi.Ods.AdminApi.V1.csproj` — V1 API target framework.
- `Application\EdFi.Ods.AdminApi.V3\EdFi.Ods.AdminApi.V3.csproj` — **V3 API target framework (added with v3 specification).**
- `Application\EdFi.Ods.AdminApi.UnitTests\EdFi.Ods.AdminApi.UnitTests.csproj` — unit test target framework.
- `Application\EdFi.Ods.AdminApi.Common.UnitTests\EdFi.Ods.AdminApi.Common.UnitTests.csproj` — common unit tests target framework.
- `Application\EdFi.Ods.AdminApi.InstanceManagement.UnitTests\EdFi.Ods.AdminApi.InstanceManagement.UnitTests.csproj` — instance management unit tests target framework.
- `Application\EdFi.Ods.AdminApi.V3.UnitTests\EdFi.Ods.AdminApi.V3.UnitTests.csproj` — **V3 unit tests target framework (added with v3 specification).**
- `Application\EdFi.Ods.AdminApi.DBTests\EdFi.Ods.AdminApi.DBTests.csproj` — DB tests target framework.
- `Application\EdFi.Ods.AdminApi.V1.DBTests\EdFi.Ods.AdminApi.V1.DBTests.csproj` — V1 DB tests target framework.
- `Application\EdFi.Ods.AdminApi.V3.DBTests\EdFi.Ods.AdminApi.V3.DBTests.csproj` — **V3 DB tests target framework (added with v3 specification).**
- `build.ps1` — build/test orchestration and framework-specific output path assumptions; `IntegrationTests7x` filter must be extended to cover V3 DBTests.
- `Application\Directory.Packages.props` — centrally managed package versions for compatibility updates.
- `Installer.AdminApi\global.json` — installer SDK baseline.
- `Docker\api.mssql.Dockerfile`, `Docker\api.pgsql.Dockerfile`, `Docker\dev.mssql.Dockerfile`, `Docker\dev.pgsql.Dockerfile` — .NET SDK/runtime image baselines (**already updated to .NET 10 as of this plan revision**).
- `Docker\V3\db.mssql.admin.Dockerfile`, `Docker\V3\db.pgsql.admin.Dockerfile` — V3 database Dockerfiles (use MSSQL/PostgreSQL base images, no .NET SDK reference needed).
- `docs\developer.md`, `docs\yaml-to-md\yaml-to-md.md` — developer instructions that currently reference .NET 8/net8.0 paths.

### Task 1: Retarget all Application projects to net10.0

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.Common\EdFi.Ods.AdminApi.Common.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.InstanceManagement\EdFi.Ods.AdminApi.InstanceManagement.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V1\EdFi.Ods.AdminApi.V1.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V3\EdFi.Ods.AdminApi.V3.csproj` *(new — added with v3 specification)*
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\EdFi.Ods.AdminApi.UnitTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.Common.UnitTests\EdFi.Ods.AdminApi.Common.UnitTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.InstanceManagement.UnitTests\EdFi.Ods.AdminApi.InstanceManagement.UnitTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V3.UnitTests\EdFi.Ods.AdminApi.V3.UnitTests.csproj` *(new — added with v3 specification)*
- Modify: `Application\EdFi.Ods.AdminApi.DBTests\EdFi.Ods.AdminApi.DBTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V1.DBTests\EdFi.Ods.AdminApi.V1.DBTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V3.DBTests\EdFi.Ods.AdminApi.V3.DBTests.csproj` *(new — added with v3 specification)*

- [ ] **Step 1: Capture failing baseline check for net8.0 references**

```powershell
rg "<TargetFramework>net8\.0</TargetFramework>" Application -g "**/*.csproj" -n
```

Run: `rg "<TargetFramework>net8\.0</TargetFramework>" Application -g "**/*.csproj" -n`
Expected: 12 matches across Application projects (original 9 + 3 new V3 projects).

- [ ] **Step 2: Replace framework targets in each project file**

```xml
<TargetFramework>net10.0</TargetFramework>
```

Run: edit the 12 listed `.csproj` files and replace only `net8.0` target values with `net10.0`.
Expected: project structure and package references remain unchanged.

- [ ] **Step 3: Verify project target conversion**

```powershell
rg "<TargetFramework>net8\.0</TargetFramework>" Application -g "**/*.csproj" -n
rg "<TargetFramework>net10\.0</TargetFramework>" Application -g "**/*.csproj" -n
```

Run: both commands above.
Expected: first command returns no matches, second returns 12 matches.

- [ ] **Step 4: Build validation**

```powershell
./build.ps1 -Command Build
./build.ps1 -Command UnitTest
```

Run: both commands above.
Expected: solution builds and all unit tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/**/*.csproj
git commit -m "chore: retarget application projects to net10.0"
```

### Task 2: Update build script framework output paths and V3 integration test coverage

**Files:**
- Modify: `build.ps1` (framework output path literals currently using `net8.0`; `IntegrationTests7x` filter must include V3 DBTests)

- [ ] **Step 1: Confirm current hardcoded framework path usage**

```powershell
rg "net8\.0" build.ps1 -n
```

Run: `rg "net8\.0" build.ps1 -n`
Expected: matches for OpenAPI DLL path and publish source path.

- [ ] **Step 2: Change framework-specific paths to net10.0**

```powershell
$dllPath = "./bin/Release/net10.0/EdFi.Ods.AdminApi.dll"
$source = "$solutionRoot/EdFi.Ods.AdminApi/bin/Release/net10.0/."
```

Run: update the corresponding lines in `build.ps1`.
Expected: all build script framework output assumptions now point to `net10.0`.

- [ ] **Step 3: Add V3 DBTests to integration test runner**

The `IntegrationTests7x` function uses filter `*AdminApi.DBTests`, which does NOT match `EdFi.Ods.AdminApi.V3.DBTests` (ends in `V3.DBTests`). Add a dedicated function and call it from `Invoke-IntegrationTestSuite`:

```powershell
function IntegrationTests3x {
    Invoke-Execute { RunTests -Filter "*AdminApi.V3.DBTests" }
}
```

Add the call to `IntegrationTests3x` inside `Invoke-IntegrationTestSuite` alongside `IntegrationTests7x`.

Run: edit `build.ps1` to add the function and its call site.
Expected: running `./build.ps1 -Command IntegrationTest` will now also execute V3 DB tests.

- [ ] **Step 4: Verify script has no net8.0 literals**

```powershell
rg "net8\.0" build.ps1 -n
```

Run: `rg "net8\.0" build.ps1 -n`
Expected: no matches.

- [ ] **Step 5: Build validation**

```powershell
./build.ps1 -Command Build
./build.ps1 -Command UnitTest
```

Run: both commands above.
Expected: solution builds and all unit tests pass.

- [ ] **Step 6: Commit**

```bash
git add build.ps1
git commit -m "chore: update build script paths for net10.0 artifacts and add V3 DBTests coverage"
```

### Task 3: Align SDK/toolchain baseline

**Files:**
- Modify: `Installer.AdminApi\global.json`
- Review/Modify if needed: `.github\workflows\*.yml` (only if explicit .NET SDK pin exists)

- [ ] **Step 1: Inspect current installer SDK pin**

```powershell
Get-Content Installer.AdminApi/global.json
```

Run: `Get-Content Installer.AdminApi/global.json`
Expected: SDK currently pinned to an older major version.

- [ ] **Step 2: Update installer SDK version to a .NET 10 SDK baseline**

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMajor"
  }
}
```

Run: edit `Installer.AdminApi\global.json` with the values above.
Expected: installer toolchain resolves to .NET 10 SDK.

- [ ] **Step 3: Scan workflows for explicit .NET SDK pins**

```powershell
rg "setup-dotnet|dotnet-version|8\.0\.x|\.NET 8" .github/workflows -g "*.yml" -n
```

Run: command above.
Expected: either no matches or only non-SDK numeric literals; if SDK pins exist, update to .NET 10 equivalent in same task before commit.

- [ ] **Step 4: Build validation**

```powershell
./build.ps1 -Command Build
./build.ps1 -Command UnitTest
```

Run: both commands above.
Expected: solution builds and all unit tests pass.

- [ ] **Step 5: Commit**

```bash
git add Installer.AdminApi/global.json .github/workflows/*.yml
git commit -m "chore: align installer and workflow sdk baselines to dotnet 10"
```

### Task 4: Verify Docker images are at .NET 10 (main Dockerfiles already updated)

> **Note:** As of this plan revision, the four main Dockerfiles (`api.mssql.Dockerfile`, `api.pgsql.Dockerfile`, `dev.mssql.Dockerfile`, `dev.pgsql.Dockerfile`) are already using `.NET 10` base images. The V3 database Dockerfiles (`Docker\V3\db.mssql.admin.Dockerfile`, `Docker\V3\db.pgsql.admin.Dockerfile`) use MSSQL/PostgreSQL base images and do not reference the .NET SDK. This task verifies there are no remaining .NET 8 image references.

**Files:**
- Verify: `Docker\api.mssql.Dockerfile`
- Verify: `Docker\api.pgsql.Dockerfile`
- Verify: `Docker\dev.mssql.Dockerfile`
- Verify: `Docker\dev.pgsql.Dockerfile`
- Verify: `Docker\V3\db.mssql.admin.Dockerfile`
- Verify: `Docker\V3\db.pgsql.admin.Dockerfile`

- [ ] **Step 1: Confirm no .NET 8 image references remain in any Dockerfile**

```powershell
rg "mcr\.microsoft\.com/dotnet/(aspnet|sdk):8\." Docker -g "*Dockerfile*" -n
```

Run: command above.
Expected: no matches. If any match is found, update the corresponding `FROM` lines to `.NET 10` equivalents and resolve current image digests with:

```powershell
docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.22
docker buildx imagetools inspect mcr.microsoft.com/dotnet/sdk:10.0-alpine3.22
```

- [ ] **Step 2: Validate Docker builds for all main variants**

```powershell
docker build -f Docker/api.mssql.Dockerfile .
docker build -f Docker/api.pgsql.Dockerfile .
docker build -f Docker/dev.mssql.Dockerfile .
docker build -f Docker/dev.pgsql.Dockerfile .
```

Run: all four docker build commands above.
Expected: all images build successfully using .NET 10 base layers.

- [ ] **Step 3: Run E2E smoke tests to confirm runtime behavior**

```powershell
./eng/run-e2e-bruno.ps1 -ApiVersion 1 -TearDown
./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TearDown
./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TearDown
```

Run: all three commands above. `-TearDown` removes containers after each run so any failure can be cleanly attributed to the .NET 10 upgrade rather than leftover state.
Expected: all API versions pass E2E tests with the .NET 10 container images.

- [ ] **Step 4: Commit (only if changes were needed in Step 1)**

```bash
git add Docker/**/*Dockerfile* Docker/*Dockerfile*
git commit -m "chore: ensure all docker base images are on dotnet 10"
```

### Task 5: Compatibility-only package update pass

**Files:**
- Modify if required by build/test failures: `Application\Directory.Packages.props`

The following packages in `Directory.Packages.props` are pinned to `.NET 8`-era versions and must be updated to their `.NET 10` equivalents. Update **only** these packages unless the build/restore output identifies additional blockers.

| Package | Current version | Target version |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `8.0.26` | `10.0.x` |
| `Microsoft.EntityFrameworkCore` | `8.0.8` | `10.0.x` |
| `Microsoft.EntityFrameworkCore.Abstractions` | `8.0.8` | `10.0.x` |
| `Microsoft.EntityFrameworkCore.Design` | `8.0.8` | `10.0.x` |
| `Microsoft.EntityFrameworkCore.InMemory` | `8.0.14` | `10.0.x` |
| `Microsoft.EntityFrameworkCore.SqlServer` | `8.0.8` | `10.0.x` |
| `Microsoft.EntityFrameworkCore.Tools` | `8.0.8` | `10.0.x` |
| `Microsoft.EntityFrameworkCore.Proxies` | `8.0.8` | `10.0.x` |
| `Microsoft.EntityFrameworkCore.Relational` | `8.0.8` | `10.0.x` |
| `Microsoft.Extensions.Logging.Log4Net.AspNetCore` | `8.0.0` | `10.0.x` (if compatible) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `8.0.4` | `10.0.x` (must align with EF Core major version) |
| `EFCore.NamingConventions` | `8.0.3` | `10.0.x` (must align with EF Core major version) |

> **Note:** `Npgsql` (driver, currently `8.0.6`) uses its own versioning scheme independent of .NET — do not upgrade it solely because of the .NET upgrade unless a build failure requires it.

- [ ] **Step 1: Run build after retargeting to expose compatibility blockers**

```powershell
./build.ps1 -Command Build
```

Run: `./build.ps1 -Command Build`
Expected: either success or specific package/API incompatibility errors.

- [ ] **Step 2: Update the explicitly listed package versions in central package management**

```xml
<PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Abstractions" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Proxies" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0" />
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<PackageVersion Include="EFCore.NamingConventions" Version="10.0.0" />
```

Run: apply version changes to `Directory.Packages.props`; also update `Microsoft.Extensions.Logging.Log4Net.AspNetCore` if its `8.0.0` build causes errors.
Expected: restore/build incompatibility errors are resolved with minimal package churn. Do not upgrade unrelated packages.

- [ ] **Step 3: Re-run build and unit tests to confirm compatibility state**

```powershell
./build.ps1 -Command Build
./build.ps1 -Command UnitTest
```

Run: both commands above.
Expected: build succeeds and all unit tests pass.

- [ ] **Step 4: Commit**

```bash
git add Application/Directory.Packages.props
git commit -m "chore: apply compatibility package updates for dotnet 10"
```

### Task 6: Update operational documentation and commands

**Files:**
- Modify: `docs\developer.md`
- Modify: `docs\yaml-to-md\yaml-to-md.md`

- [ ] **Step 1: Locate stale .NET 8 and net8.0 doc references**

```powershell
rg "\.NET 8|net8\.0" docs -g "*.md" -n
```

Run: command above.
Expected: matches in developer/setup and yaml-to-md docs.

- [ ] **Step 2: Update docs to .NET 10 requirements and output paths**

```markdown
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
```

```text
Application\EdFi.Ods.AdminApi\bin\Debug\net10.0\EdFi.Ods.AdminApi.dll
```

Run: replace .NET 8 references and net8.0 artifact paths in the two files.
Expected: docs instruct .NET 10 tooling and paths.

- [ ] **Step 3: Commit**

```bash
git add docs/developer.md docs/yaml-to-md/yaml-to-md.md
git commit -m "docs: update runtime and artifact guidance to dotnet 10"
```

### Task 7: Full verification and final migration guard checks

**Files:**
- No new code files
- Verify all modified files from Tasks 1-6

- [ ] **Step 1: Run unit and integration validation suites**

```powershell
./build.ps1 -Command UnitTest
./build.ps1 -Command IntegrationTest
```

Run: both commands above.
Expected: all existing tests pass (including V3 unit tests via `*.UnitTests` filter and V3 DBTests via new `IntegrationTests3x` function added in Task 2).

- [ ] **Step 2: Validate Docker build variants**

```powershell
docker build -f Docker/api.mssql.Dockerfile .
docker build -f Docker/api.pgsql.Dockerfile .
docker build -f Docker/dev.mssql.Dockerfile .
docker build -f Docker/dev.pgsql.Dockerfile .
```

Run: all four docker build commands above.
Expected: all images build successfully using .NET 10 base layers.

- [ ] **Step 3: Run E2E tests for all API versions**

```powershell
./eng/run-e2e-bruno.ps1 -ApiVersion 1 -TearDown
./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TearDown
./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TenantMode multitenant -TearDown
./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TearDown
./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TenantMode multitenant -TearDown
```

Run: all five commands above. `-TearDown` removes containers after each run so any failure can be cleanly attributed to the .NET 10 upgrade rather than stale container state.
Expected: all E2E test suites pass across all API versions and tenant modes.

- [ ] **Step 4: Enforce no lingering net8.0 operational references**

```powershell
rg "net8\.0|\.NET 8|dotnet/(aspnet|sdk):8\." . -g "!docs/superpowers/**" -n
```

Run: command above.
Expected: no matches outside intentionally historical design notes.

- [ ] **Step 5: Create final migration commit**

```bash
git add Application/**/*.csproj build.ps1 Application/Directory.Packages.props Installer.AdminApi/global.json Docker/*.Dockerfile Docker/V3/*.Dockerfile docs/**/*.md .github/workflows/*.yml
git commit -m "feat: migrate ods admin api from dotnet 8 to dotnet 10"
```
