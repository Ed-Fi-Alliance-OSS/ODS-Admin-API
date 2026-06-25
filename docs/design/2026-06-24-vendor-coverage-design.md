# Vendor Unit Test Coverage Improvement

**Date:** 2026-06-24  
**Scope:** V2 (`EdFi.Ods.AdminApi`) and V3 (`EdFi.Ods.AdminApi.V3`) — all Vendor-related source files

---

## Goal

Improve unit test coverage across all Vendor-related files in both the V2 and V3 specifications. Demonstrate coverage improvement using Coverlet via `./build.ps1 -Command UnitTest -RunCoverageAnalysis`.

---

## Files in Scope

### Features/Vendors
- `AddVendor.cs` / `AddVendor.Validator`
- `DeleteVendor.cs`
- `EditVendor.cs` / `EditVendor.Validator`
- `ReadVendor.cs`
- `VendorMapper.cs`
- `VendorModel.cs`

### Infrastructure/Database/Commands
- `AddVendorCommand.cs`
- `DeleteVendorCommand.cs`
- `EditVendorCommand.cs`

### Infrastructure/Database/Queries
- `GetVendorByIdQuery.cs`
- `GetVendorsQuery.cs`
- `GetApplicationsByVendorIdQuery.cs`
- `VendorExtensions.cs`

---

## Approach

**Approach A — Parallel V2/V3 by file.** Write new test files and fill gaps in existing ones, file-by-file. V2 (`EdFi.Ods.AdminApi.UnitTests`) and V3 (`EdFi.Ods.AdminApi.V3.UnitTests`) are worked as parallel streams since they are structurally identical.

---

## Section 1: New Test Files

### `GetApplicationsByVendorIdQueryTests` (V2 + V3)

| # | Test Name | Setup | Assertion |
|---|-----------|-------|-----------|
| 1 | `Execute_WithUnknownVendorId_ThrowsNotFoundException` | Empty in-memory DB | `NotFoundException<int>` is thrown |
| 2 | `Execute_WithExistingVendorAndNoApplications_ReturnsEmptyList` | Vendor seeded, no applications | Returns empty list (no exception) |
| 3 | `Execute_WithExistingVendorAndApplications_ReturnsApplications` | Vendor + one Application seeded | Returns list containing that application |

### `VendorMapperTests` (V2 + V3)

| # | Test Name | Setup | Assertion |
|---|-----------|-------|-----------|
| 1 | `ToModel_MapsAllFieldsCorrectly` | Vendor with name, one namespace prefix, one user | All 5 `VendorModel` fields map correctly |
| 2 | `ToModel_WithNoNamespacePrefixes_ReturnsEmptyString` | Vendor with empty `VendorNamespacePrefixes` | `NamespacePrefixes` is `string.Empty` |
| 3 | `ToModel_WithNoUsers_MapsContactFieldsAsNull` | Vendor with no users | `ContactName` and `ContactEmailAddress` are null |
| 4 | `ToModelList_MapsMultipleVendors` | Two vendors | Returns list of two correctly mapped `VendorModel` instances |

---

## Section 2: Gap-Fill for Existing Test Files

### `EditVendorCommandTests` — add 2 tests (V2 + V3)

| # | Test Name | Setup | Assertion |
|---|-----------|-------|-----------|
| 1 | `Execute_WithExistingVendor_UpdatesVendorSuccessfully` | Vendor seeded with namespace prefix + user | Name, prefix, contact name, and email are all updated in DB |
| 2 | `Execute_WithReservedVendorName_ThrowsArgumentException` | Vendor seeded with `VendorExtensions.ReservedNames[0]` as name | `ArgumentException` is thrown |

### `DeleteVendorCommandTests` — add 1 test (V2 + V3)

| # | Test Name | Setup | Assertion |
|---|-----------|-------|-----------|
| 1 | `Execute_WithReservedVendorName_ThrowsArgumentException` | Vendor seeded with `VendorExtensions.ReservedNames[0]` as name | `ArgumentException` is thrown |

### `GetVendorsQueryTests` — add 4 tests (V2 + V3)

| # | Test Name | Setup | Assertion |
|---|-----------|-------|-----------|
| 1 | `Execute_WithIdFilter_ReturnsMatchingVendor` | Two vendors seeded | Only the vendor matching the given `id` is returned |
| 2 | `Execute_WithNamespacePrefixFilter_ReturnsMatchingVendor` | Two vendors with different namespace prefixes | Only the vendor matching the given prefix is returned |
| 3 | `Execute_WithContactNameFilter_ReturnsMatchingVendor` | Two vendors with different users | Only the vendor whose user's `FullName` matches is returned |
| 4 | `Execute_WithContactEmailFilter_ReturnsMatchingVendor` | Two vendors with different users | Only the vendor whose user's `Email` matches is returned |

---

## Section 3: Validation Step

Run before writing any tests to establish a baseline:

```powershell
./build.ps1 -Command UnitTest -RunCoverageAnalysis
```

Run again after all tests are written to demonstrate coverage improvement. The Coverlet HTML report will show per-file line/branch coverage for all Vendor-related files listed above.

---

## Test Conventions

- Framework: NUnit + Shouldly
- Mocks: FakeItEasy (for `IDeleteApplicationCommand` and similar interfaces)
- DB: `SqlServerUsersContext` with `UseInMemoryDatabase` and a unique `Guid`-based name per test
- File-scoped namespaces, single-line `using` directives
- No comments unless a subtle invariant needs explanation
