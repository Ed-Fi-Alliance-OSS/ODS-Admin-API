# .NET 10 Upgrade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade ODS Admin API from .NET 8 to .NET 10 with a hard cutover, preserving behavior while updating project targets, tooling, Docker runtime images, compatibility-required packages, and operational docs.

**Architecture:** This is a platform-only migration. We will keep feature behavior and API contracts unchanged and execute in staged layers: framework retargeting, script/toolchain alignment, Docker/runtime alignment, compatibility-only package updates, then verification and docs cleanup. Each task is independently testable and committed before moving on.

**Tech Stack:** .NET 10 SDK/runtime, C#/.csproj, PowerShell build automation, GitHub Actions YAML, Docker (aspnet/sdk alpine images), NUnit/Shouldly/FakeItEasy test suites.

---

## File Structure and Responsibilities

- `Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj` — main API target framework.
- `Application\EdFi.Ods.AdminApi.Common\EdFi.Ods.AdminApi.Common.csproj` — shared API library target framework.
- `Application\EdFi.Ods.AdminApi.InstanceManagement\EdFi.Ods.AdminApi.InstanceManagement.csproj` — instance management target framework.
- `Application\EdFi.Ods.AdminApi.V1\EdFi.Ods.AdminApi.V1.csproj` — V1 API target framework.
- `Application\EdFi.Ods.AdminApi.UnitTests\EdFi.Ods.AdminApi.UnitTests.csproj` — unit test target framework.
- `Application\EdFi.Ods.AdminApi.Common.UnitTests\EdFi.Ods.AdminApi.Common.UnitTests.csproj` — common unit tests target framework.
- `Application\EdFi.Ods.AdminApi.InstanceManagement.UnitTests\EdFi.Ods.AdminApi.InstanceManagement.UnitTests.csproj` — instance management unit tests target framework.
- `Application\EdFi.Ods.AdminApi.DBTests\EdFi.Ods.AdminApi.DBTests.csproj` — DB tests target framework.
- `Application\EdFi.Ods.AdminApi.V1.DBTests\EdFi.Ods.AdminApi.V1.DBTests.csproj` and `Application\EdFi.Ods.AdminApi.V1.DBTests\EdFi.Ods.AdminApi.DBTests.csproj` — V1 DB tests target frameworks.
- `build.ps1` — build/test orchestration and framework-specific output path assumptions.
- `Application\Directory.Packages.props` — centrally managed package versions for compatibility updates.
- `Installer.AdminApi\global.json` — installer SDK baseline.
- `Docker\api.mssql.Dockerfile`, `Docker\api.pgsql.Dockerfile`, `Docker\dev.mssql.Dockerfile`, `Docker\dev.pgsql.Dockerfile` — .NET SDK/runtime image baselines.
- `docs\developer.md`, `docs\yaml-to-md\yaml-to-md.md` — developer instructions that currently reference .NET 8/net8.0 paths.

### Task 1: Retarget all Application projects to net10.0

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.Common\EdFi.Ods.AdminApi.Common.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.InstanceManagement\EdFi.Ods.AdminApi.InstanceManagement.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V1\EdFi.Ods.AdminApi.V1.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.UnitTests\EdFi.Ods.AdminApi.UnitTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.Common.UnitTests\EdFi.Ods.AdminApi.Common.UnitTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.InstanceManagement.UnitTests\EdFi.Ods.AdminApi.InstanceManagement.UnitTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.DBTests\EdFi.Ods.AdminApi.DBTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V1.DBTests\EdFi.Ods.AdminApi.V1.DBTests.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V1.DBTests\EdFi.Ods.AdminApi.DBTests.csproj`

- [ ] **Step 1: Capture failing baseline check for net8.0 references**

```powershell
rg "<TargetFramework>net8\.0</TargetFramework>" Application -g "**/*.csproj" -n
```

Run: `rg "<TargetFramework>net8\.0</TargetFramework>" Application -g "**/*.csproj" -n`
Expected: 10 matches across Application projects.

- [ ] **Step 2: Replace framework targets in each project file**

```xml
<TargetFramework>net10.0</TargetFramework>
```

Run: edit the 10 listed `.csproj` files and replace only `net8.0` target values with `net10.0`.
Expected: project structure and package references remain unchanged.

- [ ] **Step 3: Verify project target conversion**

```powershell
rg "<TargetFramework>net8\.0</TargetFramework>" Application -g "**/*.csproj" -n
rg "<TargetFramework>net10\.0</TargetFramework>" Application -g "**/*.csproj" -n
```

Run: both commands above.
Expected: first command returns no matches, second returns 10 matches.

- [ ] **Step 4: Commit**

```bash
git add Application/**/*.csproj
git commit -m "chore: retarget application projects to net10.0"
```

### Task 2: Update build script framework output paths

**Files:**
- Modify: `build.ps1` (framework output path literals currently using `net8.0`)

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

- [ ] **Step 3: Verify script has no net8.0 literals**

```powershell
rg "net8\.0" build.ps1 -n
```

Run: `rg "net8\.0" build.ps1 -n`
Expected: no matches.

- [ ] **Step 4: Commit**

```bash
git add build.ps1
git commit -m "chore: update build script paths for net10.0 artifacts"
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

- [ ] **Step 4: Commit**

```bash
git add Installer.AdminApi/global.json .github/workflows/*.yml
git commit -m "chore: align installer and workflow sdk baselines to dotnet 10"
```

### Task 4: Update Docker images to .NET 10

**Files:**
- Modify: `Docker\api.mssql.Dockerfile`
- Modify: `Docker\api.pgsql.Dockerfile`
- Modify: `Docker\dev.mssql.Dockerfile`
- Modify: `Docker\dev.pgsql.Dockerfile`

- [ ] **Step 1: Resolve current .NET 10 image digests for required tags**

```powershell
docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.21
docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.22
docker buildx imagetools inspect mcr.microsoft.com/dotnet/sdk:10.0-alpine3.21
```

Run: the commands above.
Expected: each command returns a manifest digest (`sha256:...`) to pin in Dockerfiles.

- [ ] **Step 2: Replace .NET 8 base/build images with .NET 10 equivalents**

```powershell
$Aspnet321Digest = (docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.21 | Select-String "Digest:").ToString().Split("Digest:")[1].Trim()
$Aspnet322Digest = (docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.22 | Select-String "Digest:").ToString().Split("Digest:")[1].Trim()
$Sdk321Digest = (docker buildx imagetools inspect mcr.microsoft.com/dotnet/sdk:10.0-alpine3.21 | Select-String "Digest:").ToString().Split("Digest:")[1].Trim()

# Apply these exact digest values in Dockerfile FROM lines while preserving current stage names.
```

Run: apply equivalent replacements in all four Dockerfiles, preserving existing stage names and file-specific alpine variants.
Expected: Dockerfiles reference only .NET 10 images.

- [ ] **Step 3: Verify Dockerfiles no longer reference dotnet 8 images**

```powershell
rg "mcr\.microsoft\.com/dotnet/(aspnet|sdk):8\." Docker -g "*Dockerfile*" -n
```

Run: command above.
Expected: no matches.

- [ ] **Step 4: Commit**

```bash
git add Docker/api.*.Dockerfile Docker/dev.*.Dockerfile
git commit -m "chore: move docker base images to dotnet 10"
```

### Task 5: Compatibility-only package update pass

**Files:**
- Modify if required by build/test failures: `Application\Directory.Packages.props`

- [ ] **Step 1: Run build after retargeting to expose compatibility blockers**

```powershell
./build.ps1 -Command Build
```

Run: `./build.ps1 -Command Build`
Expected: either success or specific package/API incompatibility errors.

- [ ] **Step 2: Update only failing package versions in central package management**

```xml
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
```

Run: apply only necessary version changes reported by restore/build/test output; do not upgrade unrelated packages.
Expected: restore/build incompatibility errors are resolved with minimal package churn.

- [ ] **Step 3: Re-run build to confirm compatibility state**

```powershell
./build.ps1 -Command Build
```

Run: `./build.ps1 -Command Build`
Expected: build succeeds.

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
Expected: all existing tests pass.

- [ ] **Step 2: Validate Docker build variants**

```powershell
docker build -f Docker/api.mssql.Dockerfile .
docker build -f Docker/api.pgsql.Dockerfile .
docker build -f Docker/dev.mssql.Dockerfile .
docker build -f Docker/dev.pgsql.Dockerfile .
```

Run: all four docker build commands above.
Expected: all images build successfully using .NET 10 base layers.

- [ ] **Step 3: Enforce no lingering net8.0 operational references**

```powershell
rg "net8\.0|\.NET 8|dotnet/(aspnet|sdk):8\." . -g "!docs/superpowers/**" -n
```

Run: command above.
Expected: no matches outside intentionally historical design notes.

- [ ] **Step 4: Create final migration commit**

```bash
git add Application/**/*.csproj build.ps1 Application/Directory.Packages.props Installer.AdminApi/global.json Docker/*.Dockerfile docs/**/*.md .github/workflows/*.yml
git commit -m "feat: migrate ods admin api from dotnet 8 to dotnet 10"
```
