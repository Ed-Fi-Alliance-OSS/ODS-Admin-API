# Vendor Coverage Boost — Design Spec

**Date:** 2026-06-25
**Status:** approved
**Goal:** Push unit test coverage for all Vendor-related files in Admin API v2 and v3 from ~76% to ≥80% line and branch.

---

## Context

A prior implementation effort (branch `ADMINAPI-1370`) added tests for `EditVendorCommand`, `AddVendorCommand`, `DeleteVendorCommand`, `GetVendorsQuery`, and `VendorMapper` in both `EdFi.Ods.AdminApi.UnitTests` (v2) and `EdFi.Ods.AdminApi.V3.UnitTests` (v3), reaching ~76% coverage. This spec closes the remaining gaps.

Coverage is measured with coverlet via `.\build.ps1 -Command UnitTest -RunCoverageAnalysis`, which writes a report to `coveragereport/` at the repo root.

---

## Current Coverage Gaps

| File | Line% | Branch% | Root cause |
|---|---|---|---|
| DeleteVendor (feature) | 40% | — | `MapEndpoints` 6 lines uncovered |
| ReadVendor (feature) | 58% | 100% | `MapEndpoints` 10 lines uncovered |
| EditVendor (feature) | 78% | — | `MapEndpoints` uncovered |
| AddVendor (feature) | 81% | 50% | `MapEndpoints` + null-namespace validator branch |
| AddVendorCommand | 100% | 63% | `?.Trim()` null-propagation on Company/ContactName/Email |
| GetVendorsQuery | 100% | 50% | SQL Server engine collation branch never taken |
| VendorExtensions | 100% | 75% | Null vendor path never tested |
| EditVendorCommand | 100% | 83% | `NamespacePrefixes = null` branch |
| DeleteVendorCommand | 74% | 56% | ApiClient cleanup path (lines 44–53) never seeded |

---

## Approach

**Option 1 selected:** Extend existing test files and add a shared `TestEndpointRouteBuilder` helper. No new NuGet packages. No source file modifications.

---

## File Layout

Changes are symmetric across both test projects. Only namespaces differ between v2 and v3.

### New files (2 per project, 4 total)

```
{TestProject}/Infrastructure/Helpers/TestEndpointRouteBuilder.cs
{TestProject}/Features/Vendors/VendorFeatureEndpointTests.cs
```

### Modified files (5 per project, 10 total)

```
{TestProject}/Infrastructure/Database/Commands/AddVendorCommandTests.cs
{TestProject}/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs
{TestProject}/Infrastructure/Database/Commands/EditVendorCommandTests.cs
{TestProject}/Infrastructure/Database/Queries/GetVendorsQueryTests.cs
{TestProject}/Features/Vendors/AddVendorTests.cs
```

`VendorExtensions` null-vendor branch: covered in a new `VendorExtensionsTests.cs` in `Infrastructure/Database/Queries/` (no existing file for this class).

---

## Component Details

### `TestEndpointRouteBuilder`

A minimal `IEndpointRouteBuilder` implementation that provides a real `DataSources` collection so ASP.NET Core's `MapGet`/`MapPost`/`MapPut`/`MapDelete` extension methods can execute without a full DI container:

```csharp
internal class TestEndpointRouteBuilder : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider { get; } = A.Fake<IServiceProvider>();
    public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
    public IApplicationBuilder CreateApplicationBuilder() => A.Fake<IApplicationBuilder>();
}
```

### `VendorFeatureEndpointTests`

One NUnit test per Feature class (`AddVendor`, `DeleteVendor`, `EditVendor`, `ReadVendor`). Each test:
1. Creates a `TestEndpointRouteBuilder`
2. Calls `feature.MapEndpoints(builder)`
3. Asserts no exception and `builder.DataSources` is non-empty

These are smoke tests — they verify route registration runs without error, not that routes behave correctly.

### Branch gap additions (per existing test file)

| Test file | New test(s) | Covers |
|---|---|---|
| `DeleteVendorCommandTests` | Vendor with seeded `ApiClient` → `Execute` → ApiClient removed | Lines 44–53; branch ~56%→90% |
| `AddVendorCommandTests` | `Company=null`, `ContactName=null`, `ContactEmailAddress=null` | `?.Trim()` null branches; branch 63%→90% |
| `EditVendorCommandTests` | `NamespacePrefixes=null` → vendor has empty namespace collection | Null-propagation branch; branch 83%→100% |
| `GetVendorsQueryTests` | `DatabaseEngine="SqlServer"` in AppSettings → query returns results | SQL Server collation branch; branch 50%→100% |
| `AddVendorTests` | Validator `HaveACorrectLength` with `namespacePrefixes=null` | Null-namespace validator branch; branch 50%→100% |
| `VendorExtensionsTests` | `IsSystemReservedVendor(null)` returns false | Null vendor branch; branch 75%→100% |

---

## Constraints

- No new NuGet packages — use existing FakeItEasy, NUnit, Shouldly, EF Core InMemory
- No source file modifications unless a confirmed bug is found
- `IEndpointRouteBuilder` stub must not import test-only framework packages that aren't already referenced
- Both v2 and v3 test projects receive identical changes (symmetric)

---

## Validation

Final step after all tests are written:

```powershell
.\build.ps1 -Command UnitTest -RunCoverageAnalysis
```

Inspect `coveragereport/index.html` for the following files in both v2 and v3:

- `AddVendor`, `DeleteVendor`, `EditVendor`, `ReadVendor`
- `VendorMapper`, `VendorModel`
- `AddVendorCommand`, `DeleteVendorCommand`, `EditVendorCommand`
- `GetVendorsQuery`, `GetVendorByIdQuery`, `VendorExtensions`

**Pass criteria:** All targeted files ≥80% line coverage and ≥80% branch coverage.

If any file falls short, the coverlet report identifies the exact uncovered lines — fix in place before marking complete.

---

## Out of Scope

- `MapEndpoints` behavioral correctness (routing rules, auth policies, response types)
- `ReadApplicationsByVendor`
- Integration tests (DBTests projects)
- E2E / Bruno tests
- V1 (`EdFi.Ods.AdminApi.V1`) test improvements
