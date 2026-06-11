# Design: Add TreatWarningsAsErrors to All Projects

**Date:** 2026-06-11  
**Scope:** All .csproj files in the solution

## Problem

Four main projects already set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, but eight projects do not — primarily test projects and `InstanceManagement`. This inconsistency means warnings in those projects can accumulate unnoticed.

## Projects Affected (missing the property)

| Project | Type |
|---|---|
| `EdFi.Ods.AdminApi.DBTests` | Integration tests |
| `EdFi.Ods.AdminApi.Common.UnitTests` | Unit tests |
| `EdFi.Ods.AdminApi.V3.UnitTests` | Unit tests |
| `EdFi.Ods.AdminApi.V3.DBTests` | Integration tests |
| `EdFi.Ods.AdminApi.V1.DBTests` | Integration tests |
| `EdFi.Ods.AdminApi.UnitTests` | Unit tests |
| `EdFi.Ods.AdminApi.InstanceManagement` | Library |
| `EdFi.Ods.AdminApi.InstanceManagement.UnitTests` | Unit tests |

## Approach

**Option A selected:** Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` directly to the `<PropertyGroup>` of each missing `.csproj`, consistent with the existing per-project pattern in the main projects.

## Implementation Steps

1. Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to each of the 8 `.csproj` files above.
2. Build the solution to surface any warnings now treated as errors.
3. Fix all resulting errors (expected: nullable warnings, unused variables, obsolete APIs).
4. Verify with `./build.ps1 -Command UnitTest`.

## Success Criteria

- All 12 projects have `TreatWarningsAsErrors` set.
- Solution builds with zero errors and zero warnings.
- All unit tests pass.
