# Implementation Plan: V3 API Specification Restructure

**Status**: Focused on application restructure only  
**Date**: April 17, 2026  
**Scope**: `/Application/` folder  
**Out of Scope**: E2E test content changes (test structure only), Docker integration, GitHub Actions workflow changes, Docker base images, non-workflow GH Actions

---

## Executive Summary

This plan restructures the Admin API project structure to support a new V3 specification while maintaining V1 and V2 support. The strategy follows the existing V1/V2 pattern:

* **Keep** the existing V2 implementation in the current unsuffixed projects
* **Create** independent V3 projects by copying the current V2 projects and applying V3-specific adjustments
* **Update** the Common library to register V3 versioning
* **Integrate** V3 as an additional class library while keeping the current web app as the V2 host

**Expected outcome**: A clean, version-isolated architecture with independent feature sets for V1, V2, and V3, all registered in a shared versioning infrastructure.

---

## Architecture Overview

```
Current State (V1 & V2):
┌─────────────────────────────────────────┐
│ EdFi.Ods.AdminApi (web app - V2)       │
├─────────────────────────────────────────┤
│ EdFi.Ods.AdminApi.V1 (class lib - V1)  │
├─────────────────────────────────────────┤
│ EdFi.Ods.AdminApi.Common (shared)       │
└─────────────────────────────────────────┘

Target State (V1, V2, & V3 - with improved architecture):
┌──────────────────────────────────────────┐
│ EdFi.Ods.AdminApi (web app - V2)        │
├──────────────────────────────────────────┤
│ ┌─ EdFi.Ods.AdminApi.V1 (class lib)     │
│ └─ EdFi.Ods.AdminApi.V3 (class lib)     │
│ ┌─ EdFi.Ods.AdminApi.Common (shared)    │
│ └─ EdFi.Ods.AdminApi.InstanceManagement │
└──────────────────────────────────────────┘

Note: The existing host application remains the V2 web API and directly references both V1 and V3 as
independent sibling dependencies, providing cleaner architecture.
```

---

## Projects to Create (as copies)

| Source | Target | Type |
|---|---|---|
| EdFi.Ods.AdminApi | EdFi.Ods.AdminApi.V3 | Class Library |
| EdFi.Ods.AdminApi.UnitTests | EdFi.Ods.AdminApi.UnitTests.V3 | Unit Tests |
| EdFi.Ods.AdminApi.DBTests | EdFi.Ods.AdminApi.DBTests.V3 | DB Tests |

---

## Dependency Map

```
EdFi.Ods.AdminApi (main web app for V2)
│
├─→ EdFi.Ods.AdminApi.V1 (embedded, as class library)
│
├─→ EdFi.Ods.AdminApi.V3 (embedded, as class library)
│
├─→ EdFi.Ods.AdminApi.Common
│   └─→ (no internal project dependencies)
│
└─→ EdFi.Ods.AdminApi.InstanceManagement

Test Projects:
├─→ EdFi.Ods.AdminApi.UnitTests → EdFi.Ods.AdminApi
├─→ EdFi.Ods.AdminApi.UnitTests.V3 → EdFi.Ods.AdminApi.V3
├─→ EdFi.Ods.AdminApi.DBTests → EdFi.Ods.AdminApi
├─→ EdFi.Ods.AdminApi.DBTests.V3 → EdFi.Ods.AdminApi.V3
└─→ EdFi.Ods.AdminApi.V1.DBTests → EdFi.Ods.AdminApi.V1
```

**Key architectural change**: The current V2 web host remains in `EdFi.Ods.AdminApi` and directly references both V1 and V3 as independent class libraries (siblings). This provides:

* Better separation of concerns
* Cleaner feature routing from a single host
* Independent version management
* Easier maintenance of each version

---

## Implementation Plan: 4 Phases

### Phase 1: Create V3 Projects as Independent Copies

**Goal**: Create three new V3 projects by copying the current V2 projects and then applying the V3-specific namespace and project changes  
**Build checkpoint**: After each project copy

#### Step 1.1: Copy EdFi.Ods.AdminApi → EdFi.Ods.AdminApi.V3

**File changes**:

* Copy entire folder: `Application/EdFi.Ods.AdminApi/` → `Application/EdFi.Ods.AdminApi.V3/`
* Rename project file: `EdFi.Ods.AdminApi.csproj` → `EdFi.Ods.AdminApi.V3.csproj`
* Project properties: `<AssemblyName>` and `<RootNamespace>` → `EdFi.Ods.AdminApi.V3`
* All `.cs` files: Namespace `EdFi.Ods.AdminApi.*` → `EdFi.Ods.AdminApi.V3.*`
* **Project adjustments**:
  * Update the project output so `EdFi.Ods.AdminApi.V3` builds as a class library instead of a web host
  * Keep reference to `EdFi.Ods.AdminApi.Common.csproj` ✓
  * Keep reference to `EdFi.Ods.AdminApi.InstanceManagement.csproj` ✓
  * Keep or add any references needed for shared contracts used by V3 features

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi.V3
dotnet build EdFi.Ods.AdminApi.V3.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 1.2: Copy EdFi.Ods.AdminApi.UnitTests → EdFi.Ods.AdminApi.UnitTests.V3

**File changes**:

* Copy entire folder: `Application/EdFi.Ods.AdminApi.UnitTests/` → `Application/EdFi.Ods.AdminApi.UnitTests.V3/`
* Rename project file: `EdFi.Ods.AdminApi.UnitTests.csproj` → `EdFi.Ods.AdminApi.UnitTests.V3.csproj`
* Project properties: `<AssemblyName>` and `<RootNamespace>` → `EdFi.Ods.AdminApi.UnitTests.V3`
* Project reference: `EdFi.Ods.AdminApi` → `EdFi.Ods.AdminApi.V3`
* All `.cs` files: Namespace `EdFi.Ods.AdminApi.UnitTests.*` → `EdFi.Ods.AdminApi.UnitTests.V3.*`

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi.UnitTests.V3
dotnet build EdFi.Ods.AdminApi.UnitTests.V3.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 1.3: Copy EdFi.Ods.AdminApi.DBTests → EdFi.Ods.AdminApi.DBTests.V3

**File changes**:

* Copy entire folder: `Application/EdFi.Ods.AdminApi.DBTests/` → `Application/EdFi.Ods.AdminApi.DBTests.V3/`
* Rename project file: `EdFi.Ods.AdminApi.DBTests.csproj` → `EdFi.Ods.AdminApi.DBTests.V3.csproj`
* Project properties: `<AssemblyName>` and `<RootNamespace>` → `EdFi.Ods.AdminApi.DBTests.V3`
* Project reference: `EdFi.Ods.AdminApi` → `EdFi.Ods.AdminApi.V3`
* All `.cs` files: Namespace `EdFi.Ods.AdminApi.DBTests.*` → `EdFi.Ods.AdminApi.DBTests.V3.*`

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi.DBTests.V3
dotnet build EdFi.Ods.AdminApi.DBTests.V3.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 1.4: Add New Projects to Solution File

**Changes**:

* Add `EdFi.Ods.AdminApi.V3` project entry to solution (root level)
* Add `EdFi.Ods.AdminApi.UnitTests.V3` project entry (UnitTests solution folder)
* Add `EdFi.Ods.AdminApi.DBTests.V3` project entry (IntegrationTests solution folder)

**Build check**:

```powershell
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
# Expected: ✓ Full solution builds with the current projects and the new V3 projects
```

---

#### Step 1.5: Verify Phase 1 Complete

**Verification**:

* Solution loads with the current main projects plus the new V3 projects visible
* No namespace conflicts between the existing V2 implementation and V3 projects
* All projects compile independently and together
* Test projects can discover tests

**Build check**:

```powershell
./build.ps1 -Command build
# Expected: ✓ Full solution compiles
```

---

### Phase 2: Update Shared Common Library to Support V3 Versioning

**Goal**: Register V3 in the shared versioning infrastructure  
**Build checkpoint**: After Common library updates

#### Step 2.1: Update AdminApiMode Enum

**File**: `Application/EdFi.Ods.AdminApi.Common/Constants/Constants.cs`

**Change**:

```csharp
public enum AdminApiMode
{
    V1,
    V2,
    V3,      // ← Add this
    Unversioned
}
```

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi.Common
dotnet build EdFi.Ods.AdminApi.Common.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 2.2: Update AdminApiVersions.cs

**File**: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/AdminApiVersions.cs`

**Changes**:

1. Add V3 version definition after V2:

   ```csharp
   public static readonly AdminApiVersion V3 = new(3.0, "v3");
   ```

2. Update `Initialize()` method to register V3:

   ```csharp
   builder
       .HasApiVersion(V1.Version)
       .HasApiVersion(V2.Version)
       .HasApiVersion(V3.Version)  // ← Add this
       .ReportApiVersions();
   ```

3. Verify `GetAllVersions()` method reflects all three versions (likely uses reflection, no change needed)

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi.Common
dotnet build EdFi.Ods.AdminApi.Common.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 2.3: Verify AdminApiEndpointBuilder.cs

**File**: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/AdminApiEndpointBuilder.cs`

**Verification**:

* The method `BuildForVersions(params AdminApiVersion[] versions)` should work with V3 without changes
* This method already uses variadic parameters, so it will automatically support the new V3 version

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi.Common
dotnet build EdFi.Ods.AdminApi.Common.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 2.4: Verify Phase 2 Complete

**Verification**:

* Common library compiles successfully
* All projects referencing Common compile without errors
* V3 version is registered in versioning infrastructure

**Build check**:

```powershell
./build.ps1 -Command build
# Expected: ✓ Full solution compiles
```

---

### Phase 3: Update EdFi.Ods.AdminApi to Host V1 and V3 Features

**Goal**: Keep the current web app as the V2 host and update its feature routing to host V1 and V3  
**Build checkpoint**: After each configuration change

#### Step 3.1: Update WebApplicationExtensions.cs in EdFi.Ods.AdminApi

**File**: `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationExtensions.cs`

**Change**:
Update the `MapFeatureEndpoints()` method's switch statement to handle V3 mode while keeping the current web app as the V2 host:

```csharp
public static void MapFeatureEndpoints(this WebApplication application)
{
    var adminApiMode = application.Configuration.GetValue<AdminApiMode>(
        "AppSettings:adminApiMode", 
        AdminApiMode.V2);  // ← The current web app remains the default V2 host mode

    switch (adminApiMode)
    {
        case AdminApiMode.V1:
            foreach (var routeBuilder in AdminApiV1Features.AdminApiV1FeatureHelper.GetFeatures())
            {
                routeBuilder.MapEndpoints(application);
            }
            new Features.Information.ReadInformation().MapEndpoints(application);
            break;

        case AdminApiMode.V2:
            foreach (var routeBuilder in AdminApiV2Features.AdminApiFeatureHelper.GetFeatures())
            {
                routeBuilder.MapEndpoints(application);
            }
            break;

        case AdminApiMode.V3:  // ← Add this case
            foreach (var routeBuilder in AdminApiV3Features.AdminApiFeatureHelper.GetFeatures())
            {
                routeBuilder.MapEndpoints(application);
            }
            break;

        default:
            throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}");
    }
}
```

**Note**: The `AdminApiFeatureHelper` in V3 is inherited from the copied current implementation. Verify it's correctly namespaced and accessible from `EdFi.Ods.AdminApi`.

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi
dotnet build EdFi.Ods.AdminApi.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 3.2: Verify Program.cs in EdFi.Ods.AdminApi

**File**: `Application/EdFi.Ods.AdminApi/Program.cs`

**Verification**:
Keep the default `AdminApiMode` as V2 so the host continues to boot in V2 mode unless explicitly configured otherwise:

```csharp
var adminApiMode = builder.Configuration.GetValue<AdminApiMode>(
    "AppSettings:AdminApiMode", 
    AdminApiMode.V2);
```

If the copied V3 project still contains host startup code, either remove it or convert the project so it is not used as the runnable entry point.

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi
dotnet build EdFi.Ods.AdminApi.csproj
# Expected: ✓ Builds successfully with V3 references
```

---

#### Step 3.3: Verify appsettings.json in EdFi.Ods.AdminApi

**File**: `Application/EdFi.Ods.AdminApi/appsettings.json`

**Verification**:
Ensure the host configuration remains set to V2 by default:

```json
{
    "AppSettings": {
        "adminApiMode": "v2",
        ...
    }
}
```

If environment-specific appsettings files exist, keep them aligned unless there is an explicit reason to boot the host in another mode.

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi
dotnet build EdFi.Ods.AdminApi.csproj
# Expected: ✓ Builds successfully
```

---

#### Step 3.4: Verify Project References in EdFi.Ods.AdminApi and EdFi.Ods.AdminApi.V3

**Files**: `Application/EdFi.Ods.AdminApi/EdFi.Ods.AdminApi.csproj`, `Application/EdFi.Ods.AdminApi.V3/EdFi.Ods.AdminApi.V3.csproj`

**Verification**:
Ensure the host project contains these references:

* `<ProjectReference>` to `EdFi.Ods.AdminApi.V1.csproj` ✓ (direct reference - the host can load V1 features)
* `<ProjectReference>` to `EdFi.Ods.AdminApi.V3.csproj` ✓ (direct reference - the host can load V3 features)
* `<ProjectReference>` to `EdFi.Ods.AdminApi.Common.csproj` ✓ (shared infrastructure)
* `<ProjectReference>` to `EdFi.Ods.AdminApi.InstanceManagement.csproj` ✓ (instance management)

Ensure `EdFi.Ods.AdminApi.V3.csproj` is configured as a class library and references only the projects it actually needs for compilation.

**Build check**:

```powershell
cd Application/EdFi.Ods.AdminApi
dotnet build EdFi.Ods.AdminApi.csproj
# Expected: ✓ Builds successfully with V1 and V3 dependencies
```

---

#### Step 3.5: Verify Phase 3 Complete

**Verification**:

* `EdFi.Ods.AdminApi` remains the runnable web app
* Default mode remains V2
* Feature routing handles all three versions (V1, V2, V3)
* V3 compiles successfully as a class library consumed by the host

**Build check**:

```powershell
./build.ps1 -Command build
# Expected: ✓ Full solution compiles
```

---

### Phase 4: Verification and Solution-Wide Integration

**Goal**: Perform final unit test validation and verify entire solution builds correctly

#### Step 4.1: Update Solution File for Final Consistency

**File**: `Application/Ed-Fi-ODS-AdminApi.sln`

**Verification**:

* All project GUIDs are unique ✓
* All project paths are correct ✓
* All projects are assigned to correct solution folders ✓
  * Existing and V3 tests in UnitTests folder
  * Existing and V3 DB tests in IntegrationTests folder
  * Main projects at root level

**Build check**:

```powershell
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
# Expected: ✓ Full solution loads and builds
```

---

#### Step 4.2: Full Solution Build

**Command**:

```powershell
./build.ps1 -Command build
```

**Expected results**:

* ✓ All main projects compile successfully
* ✓ No compilation errors or warnings related to project copies and new references
* ✓ Solution loads without issues in Visual Studio

---

#### Step 4.3: Run Unit Tests

**Command**:

```powershell
./build.ps1 -Command UnitTest
```

**Expected results**:

* ✓ All unit tests pass (no new test failures from the restructure)
* ✓ EdFi.Ods.AdminApi.Common.UnitTests pass
* ✓ EdFi.Ods.AdminApi.UnitTests pass
* ✓ EdFi.Ods.AdminApi.UnitTests.V3 pass (mirrors V2 tests)
* ✓ InstanceManagement tests pass

---

#### Step 4.4: Document Completion

**Deliverables**:

* Solution structure verified ✓
* All projects compile ✓
* Unit tests pass ✓
* Runtime mode switching verified from the current host ✓

---

## File Modification Summary

### Files to Create/Copy

| Source Path | Destination Path | Type |
|---|---|---|
| `Application/EdFi.Ods.AdminApi/` | `Application/EdFi.Ods.AdminApi.V3/` | Directory (recursive copy) |
| `Application/EdFi.Ods.AdminApi.UnitTests/` | `Application/EdFi.Ods.AdminApi.UnitTests.V3/` | Directory (recursive copy) |
| `Application/EdFi.Ods.AdminApi.DBTests/` | `Application/EdFi.Ods.AdminApi.DBTests.V3/` | Directory (recursive copy) |

### Files to Update

| File | Changes |
|---|---|
| `Application/EdFi.Ods.AdminApi.Common/Constants/Constants.cs` | Add `V3` to `AdminApiMode` enum |
| `Application/EdFi.Ods.AdminApi.Common/Infrastructure/AdminApiVersions.cs` | Add V3 version definition and register in `Initialize()` |
| `Application/Ed-Fi-ODS-AdminApi.sln` | Add V3 projects and update references as needed |
| All `.cs` files in copied V3 projects | Update namespace declarations |
| All `.csproj` files in copied V3 projects | Update `<AssemblyName>`, `<RootNamespace>`, and output type |

### Files Not Modified

* E2E tests in `Application/EdFi.Ods.AdminApi.V3/E2E Tests/` (out of scope)
* Docker files (out of scope)
* `.github/workflows/` files (out of scope)
* Files outside `Application/` folder (out of scope)
* `EdFi.Ods.AdminApi.Common` logic (only versioning additions)
* `EdFi.Ods.AdminApi.InstanceManagement` (no changes needed)

---

## Risk Assessment

### Low Risk

* **Namespace updates**: Mechanical search-and-replace across project files
* **Project file updates**: Standard `.csproj` property changes
* **Solution file updates**: Adding new project entries
* **Common library versioning**: Adding new enum value and version registration (additive, no breaking changes)

### Medium Risk

* **Feature routing in the host**: New switch case must correctly reference V3 feature helpers (verify namespacing)
* **Interdependencies**: Ensure the host and V3 projects reference the correct shared dependencies

### Mitigation

* Build after each major step
* Run unit tests at end of Phase 4
* Verify no circular dependencies introduced
* Manual review of namespace updates in critical files
* Integration tests can be run separately if needed for full validation

---

## Success Criteria

✓ **Phase 1**: All V3 projects (copies of the current V2 projects) compile independently and together  
✓ **Phase 2**: Common library registers V3 versioning; all projects still compile  
✓ **Phase 3**: The current web host configures feature routing for V1, V2, and V3  
✓ **Phase 4**: Full solution builds and unit tests pass, no regressions  

---

## Timeline Estimate

| Phase | Estimated Time | Notes |
|---|---|---|
| Phase 1 (Copy to V3) | 30-45 minutes | Copy directories, update namespaces in copies |
| Phase 2 (Versioning) | 15-30 minutes | Update Common library, add V3 enum/version |
| Phase 3 (Host Configuration) | 15-30 minutes | Update Program.cs, WebApplicationExtensions, config |
| Phase 4 (Verification) | 15-20 minutes | Build solution and run unit tests only |
| **Total** | **1.5-2.5 hours** | Excludes integration tests (can run separately if needed) |

---

## Post-Implementation Considerations

### 1. V2 as the Long-Term Host

The current `EdFi.Ods.AdminApi` project remains the V2 web host and references V1 and V3 as sibling class libraries, providing:

* Clean separation between versions
* Simpler feature routing from one host
* Each version can be evolved independently

### 2. Test File Scope

V3 test projects are exact copies of V2 tests. Consider:

* Are V3-specific test cases needed immediately, or is this acceptable for initial setup?
* Plan: Add V3-specific tests as new specification features differ from V2

### 3. API Response Schemas

V3 may have different API response structures than V2. Consider:

* If schemas differ significantly, V3 feature classes will need differentiation
* Plan: Review V3 specification to identify schema differences; update response DTOs accordingly

### 4. Configuration Variants

If `appsettings.Development.json` and `appsettings.Production.json` exist, ensure the current host remains aligned with the intended default mode and can route to V1 or V3 when configured.

### 5. Future Versions

This architecture supports future versions (V4, V5, etc.) by following the same pattern:

* Add to `AdminApiMode` enum
* Register in `AdminApiVersions.cs`
* Keep a single host web app unless there is a strong reason to introduce a new host
* Create new version projects as class libraries when possible
* Add independent references to all needed versions from the host application
* Update the host app's Program.cs default and feature routing as needed

---

## Approval Checklist

* [ ] Phase 1-4 (Application) completed and verified
* [ ] Document scope confirmed as `Application`-only
* [ ] Ready to proceed with application restructure implementation

---

**Next Steps**:

1. Review and approve the Application-only implementation scope
2. Proceed with Phases 1-4 implementation
3. Run solution build and unit tests after the restructure is complete
