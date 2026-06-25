---
title: Improve Vendor Code Coverage for Admin API v2 and v3
feature: 2026-06-23-improve-vendor-code-coverage
date: 2026-06-23
version: 1
status: approved
---

# Improve Vendor Code Coverage for Admin API v2 and v3

## Problem Statement

Unit test coverage for Vendor-related code in Admin API v2 and v3 is incomplete. Several critical branches — including the `EditVendorCommand` happy path, system-reserved vendor guards, namespace prefix replacement logic, and filter paths in `GetVendorsQuery` — are untested. `VendorMapper` has no dedicated tests at all.

## Primary User

Developer on the Admin API team who needs confidence that Vendor CRUD operations behave correctly across edge cases and can verify this with a measurable coverage report.

## Job-to-be-Done

Close the known unit test gaps in Vendor feature files and their infrastructure, produce a coverage report showing >80% line/branch coverage, and do so for both v2 and v3 specs simultaneously.

## Success Criteria

1. `EditVendorCommand`: happy path, system-reserved vendor, vendor-without-users branch, and namespace replacement are each covered by a dedicated test in both v2 and v3.
2. `DeleteVendorCommand`: system-reserved vendor and cascade-delete-with-applications paths are covered in both v2 and v3.
3. `AddVendorCommand`: null namespace, multiple namespaces, and name-trimming cases are covered in both v2 and v3.
4. `GetVendorsQuery`: id, namespace prefix, contact name, and email filter paths are each covered by at least one test in both v2 and v3.
5. A new `VendorMapperTests.cs` exists in both `EdFi.Ods.AdminApi.UnitTests` and `EdFi.Ods.AdminApi.V3.UnitTests` covering `ToModel` and `ToModelList`.
6. Running `./build.ps1 -Command UnitTest` with coverlet enabled produces a coverage report showing >80% line and branch coverage for the targeted Vendor files.
7. All new and existing tests pass without modification to source files (unless a confirmed bug fix is needed).

## Explicit Scope Boundaries

**In scope:**

- Add or expand tests in `EdFi.Ods.AdminApi.UnitTests` (v2) and `EdFi.Ods.AdminApi.V3.UnitTests` (v3) for: `AddVendorCommand`, `DeleteVendorCommand`, `EditVendorCommand`, `GetVendorByIdQuery`, `GetVendorsQuery`, `VendorExtensions`, `AddVendor`, `DeleteVendor`, `EditVendor`, `ReadVendor`, `VendorModel`, `VendorMapper`
- Add a new `VendorMapperTests.cs` to both test projects
- Run `./build.ps1 -Command UnitTest` and verify coverage via coverlet report

**Out of scope for v1:**

- `ReadApplicationsByVendor.cs`
- Integration tests (DBTests projects)
- E2E / Bruno tests
- Modifying source code unless a confirmed bug requires it
- V1 (`EdFi.Ods.AdminApi.V1`) test improvements

## Confirmed Assumptions

- Assumption: new tests follow NUnit + Shouldly + FakeItEasy + in-memory `SqlServerUsersContext`. Confirmed.
- Assumption: both v2 and v3 test projects receive identical gap-filling in parallel. Confirmed.
- Assumption: `VendorMapper` tests should be added. Confirmed.

## Open Questions

- Q: `DeleteVendorCommand` loads all `ApiClients` into memory via `.AsEnumerable()` before filtering by user. This is a potential performance issue. Should a bug note be filed, or is this out of scope? Uncertainty: low. Impact: if in scope, requires a source change + test; if out of scope, we document it and test the current behavior only.

## Constraints

- New NuGet packages: not permitted — use existing test dependencies only.
- Coverage tool: coverlet, invoked via `./build.ps1 -Command UnitTest`.
- No changes to source files unless a bug is confirmed and in-scope.

## Dependencies

- `EdFi.Ods.AdminApi.UnitTests` project (v2 unit test project)
- `EdFi.Ods.AdminApi.V3.UnitTests` project (v3 unit test project)
- In-memory EF Core (`Microsoft.EntityFrameworkCore.InMemory`) already referenced in both test projects
- `FakeItEasy` already referenced in both test projects
