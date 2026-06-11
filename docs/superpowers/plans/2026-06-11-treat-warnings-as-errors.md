# TreatWarningsAsErrors Rollout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to all 8 projects missing it, then fix the resulting NuGet errors so the solution builds cleanly and all unit tests pass.

**Architecture:** The property is added directly to each `.csproj` file inside the existing `<PropertyGroup>` — consistent with how the 4 main projects already declare it. The errors that surface are all NuGet restore warnings (NU1510: unnecessary explicit package references; NU1904: known vulnerability in a transitive package), not compiler warnings.

**Tech Stack:** .NET 10, MSBuild, NuGet Central Package Management (`Directory.Packages.props`)

---

## Summary of Errors Found

After adding the property (already done), `dotnet build` reports:

| Error | Project(s) | Package | Fix |
|---|---|---|---|
| NU1510 | DBTests, V3.DBTests | `System.Formats.Asn1` | Remove explicit ref |
| NU1510 | DBTests, V3.UnitTests, V3.DBTests, UnitTests | `System.Text.Json` | Remove explicit ref |
| NU1904 | V1.DBTests | `System.Drawing.Common` 5.0.0 (critical CVE) | Pin safe version |

---

### Task 1: Add TreatWarningsAsErrors to all 8 projects

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.DBTests/EdFi.Ods.AdminApi.DBTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.Common.UnitTests/EdFi.Ods.AdminApi.Common.UnitTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.V3.DBTests/EdFi.Ods.AdminApi.V3.DBTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.V1.DBTests/EdFi.Ods.AdminApi.V1.DBTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/EdFi.Ods.AdminApi.UnitTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.InstanceManagement/EdFi.Ods.AdminApi.InstanceManagement.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.InstanceManagement.UnitTests/EdFi.Ods.AdminApi.InstanceManagement.UnitTests.csproj`

- [x] **Step 1: Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` inside the first `<PropertyGroup>` of each of the 8 `.csproj` files listed above.**

  *(Already done as preliminary investigation step.)*

- [ ] **Step 2: Verify property appears in all 12 projects**

  ```powershell
  Get-ChildItem -Recurse -Filter "*.csproj" | Select-String "TreatWarningsAsErrors"
  ```

  Expected: 12 matches (4 pre-existing + 8 new).

---

### Task 2: Fix NU1510 — Remove unnecessary explicit package references

NU1510 fires when a package is explicitly listed in a `.csproj` but is already available as a transitive dependency. The fix is to remove the explicit `<PackageReference>` entry.

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.DBTests/EdFi.Ods.AdminApi.DBTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.V3.DBTests/EdFi.Ods.AdminApi.V3.DBTests.csproj`
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/EdFi.Ods.AdminApi.UnitTests.csproj`

- [ ] **Step 1: Remove `System.Text.Json` from `EdFi.Ods.AdminApi.DBTests.csproj`**

  Delete this line:
  ```xml
  <PackageReference Include="System.Text.Json" />
  ```

- [ ] **Step 2: Remove `System.Formats.Asn1` from `EdFi.Ods.AdminApi.DBTests.csproj`**

  Delete this line:
  ```xml
  <PackageReference Include="System.Formats.Asn1" />
  ```

- [ ] **Step 3: Remove `System.Text.Json` from `EdFi.Ods.AdminApi.V3.UnitTests.csproj`**

  Delete:
  ```xml
  <PackageReference Include="System.Text.Json" />
  ```

- [ ] **Step 4: Remove `System.Text.Json` and `System.Formats.Asn1` from `EdFi.Ods.AdminApi.V3.DBTests.csproj`**

  Delete both:
  ```xml
  <PackageReference Include="System.Formats.Asn1" />
  <PackageReference Include="System.Text.Json" />
  ```

- [ ] **Step 5: Remove `System.Text.Json` from `EdFi.Ods.AdminApi.UnitTests.csproj`**

  Delete:
  ```xml
  <PackageReference Include="System.Text.Json" />
  ```

---

### Task 3: Fix NU1904 — Pin safe version of System.Drawing.Common

`EdFi.Ods.AdminApi.V1.DBTests` transitively pulls in `System.Drawing.Common` 5.0.0 (via `Microsoft.SqlServer.SqlManagementObjects`), which has a known critical vulnerability (GHSA-rxg9-xrhp-64gj, fixed in 6.0.0). Fix by adding `System.Drawing.Common` 8.0.0 to central package management and referencing it directly to override the transitive version.

**Files:**
- Modify: `Application/Directory.Packages.props`
- Modify: `Application/EdFi.Ods.AdminApi.V1.DBTests/EdFi.Ods.AdminApi.V1.DBTests.csproj`

- [ ] **Step 1: Add `System.Drawing.Common` version entry to `Directory.Packages.props`**

  Add inside the `<ItemGroup>` under the `<!-- System -->` comment:
  ```xml
  <PackageVersion Include="System.Drawing.Common" Version="8.0.0" />
  ```

- [ ] **Step 2: Add `System.Drawing.Common` PackageReference to `EdFi.Ods.AdminApi.V1.DBTests.csproj`**

  Add to the `<ItemGroup>` of package references:
  ```xml
  <PackageReference Include="System.Drawing.Common" />
  ```

---

### Task 4: Verify the build succeeds

- [ ] **Step 1: Run the build**

  ```powershell
  dotnet build Application/ --no-incremental
  ```

  Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 2: Commit**

  ```powershell
  git add -A
  git commit -m "chore: add TreatWarningsAsErrors to all projects and fix resulting NuGet warnings

  - Add TreatWarningsAsErrors=true to 8 projects missing the property
  - Remove unnecessary explicit System.Text.Json and System.Formats.Asn1
    PackageReferences (NU1510: available as transitive dependencies)
  - Pin System.Drawing.Common 8.0.0 to fix critical vulnerability
    GHSA-rxg9-xrhp-64gj from transitive dependency in V1.DBTests (NU1904)

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

### Task 5: Run unit tests

- [ ] **Step 1: Run unit tests**

  ```powershell
  .\build.ps1 -Command UnitTest
  ```

  Expected: All unit tests pass.
