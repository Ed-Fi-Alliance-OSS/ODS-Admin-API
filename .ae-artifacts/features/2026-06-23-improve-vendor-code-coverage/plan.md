---
title: Improve Vendor Code Coverage for Admin API v2 and v3
feature: 2026-06-23-improve-vendor-code-coverage
date: 2026-06-23
version: 1
status: draft
source-spec: ./spec.md
mode: brownfield
---

# Improve Vendor Code Coverage for Admin API v2 and v3

## 1. Source spec

This plan was decomposed from `spec.md` version 1 (status: approved). The project is **brownfield** — new tests are added to two existing unit test projects (`EdFi.Ods.AdminApi.UnitTests` for v2 and `EdFi.Ods.AdminApi.V3.UnitTests` for v3) within an established .NET 10 / C# Admin API codebase. The confirmed tech stack is: C# .NET 10, NUnit + Shouldly + FakeItEasy + EF Core InMemory, invoked via `./build.ps1 -Command UnitTest` with coverlet for coverage reporting.

## 2. Tech Stack

- **Language:** C# (existing; all source and test code is C#)
- **Runtime:** .NET 10 (existing project target)
- **Frameworks / major libraries:** NUnit (test runner), Shouldly (assertion library), FakeItEasy (mocking), Microsoft.EntityFrameworkCore.InMemory (in-memory DB substitute) — all already referenced in both test projects; no new NuGet packages permitted per spec constraint
- **Persistence:** N/A — unit tests use in-memory EF Core; no real database
- **Deployment:** N/A — test-only addition; no deployable artifact
- **Auth:** N/A — unit tests have no auth surface
- **Testing framework:** NUnit + Shouldly (confirmed in spec Confirmed Assumptions)
- **CI/CD:** `./build.ps1 -Command UnitTest` with coverlet coverage output (specified in spec Constraints)

## 3. Non-functional considerations

### Security

- Authentication: N/A — unit tests run locally/in CI; no auth surface.
- Authorization: N/A — no per-record checks in a test-only feature.
- Input validation: N/A — no user input; all test data is hardcoded.
- PII/sensitive data: N/A — tests use fake in-memory data only.
- Rate limiting: N/A — no externally-triggerable endpoints.

### Error states

- Test failure: NUnit runner prints the failing test name and assertion diff to console; `./build.ps1 -Command UnitTest` exits non-zero, blocking CI.
- Coverage below threshold: coverlet generates an HTML/JSON report; the developer inspects it manually to identify uncovered lines. CI does not auto-gate on the coverage number (manual verification required per spec).
- Standard boundary conditions (empty state, max input length, concurrent modification, resource not found) are N/A — there is no runtime I/O surface; all test inputs are hardcoded data with no external dependencies.

### Non-functional requirements

- Coverage target: >80% line and branch coverage for the targeted Vendor files (spec SC-6).
- Performance: N/A — no latency SLA applies to a unit test suite; the full suite must complete within the existing CI time budget (no new integration test overhead introduced).
- Availability: N/A — offline developer tooling.
- Load/concurrency: N/A — single developer or CI run at a time.

## 4. Architecture

**Approach (Option A — Parallel per-command, both v2 and v3 per step):**
Each step expands or creates tests for one command/query class, touching both the v2 and v3 test projects in the same step. This avoids version drift and respects the spec's confirmation that v2/v3 gaps are identical. Steps are parallel-safe across commands because each touches a distinct set of files.

**Rationale:** The spec states "both v2 and v3 test projects receive identical gap-filling in parallel" (Confirmed Assumption) — co-locating v2+v3 work within each step is the direct implementation of that assumption.

**Component layout:**

```
EdFi.Ods.AdminApi.UnitTests (v2)
  └── Infrastructure/Database/Commands/
        ├── EditVendorCommandTests.cs      (expand)
        ├── AddVendorCommandTests.cs       (expand)
        └── DeleteVendorCommandTests.cs    (expand)
  └── Infrastructure/Database/Queries/
        └── GetVendorsQueryTests.cs        (expand)
  └── Features/Vendors/
        └── VendorMapperTests.cs           (new)

EdFi.Ods.AdminApi.V3.UnitTests (v3)
  └── Infrastructure/Database/Commands/
        ├── EditVendorCommandTests.cs      (expand)
        ├── AddVendorCommandTests.cs       (expand)
        └── DeleteVendorCommandTests.cs    (expand)
  └── Infrastructure/Database/Queries/
        └── GetVendorsQueryTests.cs        (expand)
  └── Features/Vendors/
        └── VendorMapperTests.cs           (new)
```

**Brownfield attachment points:** All new tests call into existing source classes (`EditVendorCommand`, `AddVendorCommand`, `DeleteVendorCommand`, `GetVendorsQuery`, `VendorMapper`) using the same `SqlServerUsersContext` in-memory setup pattern established in existing tests. No source files are modified unless a confirmed bug is discovered (spec constraint). No new projects, no new service layers, nothing outside the two unit test projects is touched.

## 5. Steps

### Step 2026-06-23-improve-vendor-code-coverage-S1: EditVendorCommand — expand tests (v2 + v3)

- **Depends on:** []
- **Parallel-safe:** yes
- **Outputs:**
  - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs` (modified)
  - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs` (modified)
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** Happy path — vendor is updated and persisted
    - **Given** an in-memory database contains a non-reserved vendor with one namespace prefix and one user
    - **When** `EditVendorCommand.Execute` is called with a new company name, a new namespace prefix, and updated contact details
    - **Then** the command completes without error and the vendor's name, namespace prefix, and user contact fields reflect the new values in the in-memory database
  - **Scenario:** System-reserved vendor is rejected
    - **Given** an in-memory database contains a vendor whose name matches a system-reserved entry
    - **When** `EditVendorCommand.Execute` is called with that vendor's ID
    - **Then** the command throws `ArgumentException` with a message indicating the vendor may not be modified
  - **Scenario:** Vendor without existing users gets a new user created
    - **Given** an in-memory database contains a vendor with no users
    - **When** `EditVendorCommand.Execute` is called with contact name and email
    - **Then** the vendor's Users collection contains exactly one entry with the supplied name and email
  - **Scenario:** Namespace prefixes are replaced, not appended
    - **Given** an in-memory database contains a vendor with one existing namespace prefix
    - **When** `EditVendorCommand.Execute` is called with a different namespace prefix string
    - **Then** the vendor's namespace prefix collection contains only the new prefix and the old one is absent

This step adds the missing coverage paths to the already-existing `EditVendorCommandTests.cs` in both v2 and v3 projects. The current file covers only the not-found exception path. The source code `EditVendorCommand.Execute` contains four distinct branches that are untested: the successful update path, the `IsSystemReservedVendor()` guard, the `vendor.Users?.FirstOrDefault() != null` branch (creates a new user when none exists), and the namespace-prefix replacement loop. Each test uses a fresh `UseInMemoryDatabase(Guid.NewGuid().ToString())` database to avoid cross-test interference, following the existing pattern in the file.

---

### Step 2026-06-23-improve-vendor-code-coverage-S2: AddVendorCommand — expand tests (v2 + v3)

- **Depends on:** []
- **Parallel-safe:** yes
- **Outputs:**
  - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/AddVendorCommandTests.cs` (modified)
  - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/AddVendorCommandTests.cs` (modified)
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** Null namespace prefix creates vendor with empty prefix collection
    - **Given** an in-memory database is empty
    - **When** `AddVendorCommand.Execute` is called with `NamespacePrefixes = null`
    - **Then** the persisted vendor has zero namespace prefix entries and the command completes without error
  - **Scenario:** Multiple comma-separated namespaces are split and persisted individually
    - **Given** an in-memory database is empty
    - **When** `AddVendorCommand.Execute` is called with `NamespacePrefixes = "https://a.org/ns,https://b.org/ns"`
    - **Then** the persisted vendor has exactly two namespace prefix entries with values `https://a.org/ns` and `https://b.org/ns`
  - **Scenario:** Company name and contact fields are trimmed
    - **Given** an in-memory database is empty
    - **When** `AddVendorCommand.Execute` is called with leading/trailing spaces in `Company`, `ContactName`, and `ContactEmailAddress`
    - **Then** the persisted vendor's name, user full name, and email are stored without the surrounding spaces

The existing test covers the basic single-namespace happy path only. The source `AddVendorCommand.Execute` contains a `Split(",")` + `Where(!IsNullOrWhiteSpace)` + `Trim()` pipeline for namespace prefixes and explicit `?.Trim()` calls on name/email — each of these paths needs a dedicated test case. Both v2 and v3 test files are identical in structure; only the namespace import changes (`EdFi.Ods.AdminApi.UnitTests` vs `EdFi.Ods.AdminApi.V3.UnitTests`).

---

### Step 2026-06-23-improve-vendor-code-coverage-S3: DeleteVendorCommand — expand tests (v2 + v3)

- **Depends on:** []
- **Parallel-safe:** yes
- **Outputs:**
  - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs` (modified)
  - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs` (modified)
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** System-reserved vendor cannot be deleted
    - **Given** an in-memory database contains a vendor whose name matches a system-reserved entry
    - **When** `DeleteVendorCommand.Execute` is called with that vendor's ID
    - **Then** the command throws `ArgumentException` with a message indicating the vendor may not be deleted, and the vendor remains in the database
  - **Scenario:** Cascade delete invokes application deletion for each associated application
    - **Given** an in-memory database contains a vendor with one associated application
    - **When** `DeleteVendorCommand.Execute` is called with that vendor's ID
    - **Then** `IDeleteApplicationCommand.Execute` is called once with the application's ID, and the vendor is removed from the database

The existing v2 file already covers the not-found exception and the basic user-cleanup path. This step adds the two paths required by spec SC-2: the system-reserved guard (which throws `ArgumentException`) and the application-cascade path (which calls `IDeleteApplicationCommand` for each application before removing the vendor). The `IDeleteApplicationCommand` is already faked via FakeItEasy in the existing test fixture — the new tests extend that pattern. The `AsEnumerable()` performance issue in the source is documented in `state.json` notes and is tested as current behavior only (not fixed, per spec scope).

---

### Step 2026-06-23-improve-vendor-code-coverage-S4: GetVendorsQuery — expand filter tests (v2 + v3)

- **Depends on:** []
- **Parallel-safe:** yes
- **Outputs:**
  - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs` (modified)
  - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs` (modified)
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** Filter by vendor ID returns the matching vendor
    - **Given** an in-memory database contains two non-reserved vendors with distinct IDs
    - **When** `GetVendorsQuery.Execute` is called with the ID of the first vendor
    - **Then** the result contains exactly one vendor matching that ID
  - **Scenario:** Filter by namespace prefix returns matching vendors
    - **Given** an in-memory database contains two vendors, one with namespace prefix `https://acme.org/ns` and one without
    - **When** `GetVendorsQuery.Execute` is called with `namespacePrefixes = "acme"`
    - **Then** the result contains only the vendor whose prefix contains `"acme"`
  - **Scenario:** Filter by contact name returns matching vendors
    - **Given** an in-memory database contains two vendors with different user contact names
    - **When** `GetVendorsQuery.Execute` is called with one of those contact names
    - **Then** the result contains only the vendor associated with that contact name
  - **Scenario:** Filter by contact email returns matching vendors
    - **Given** an in-memory database contains two vendors with different user email addresses
    - **When** `GetVendorsQuery.Execute` is called with one of those email addresses
    - **Then** the result contains only the vendor associated with that email address
  - **Scenario:** Filter with no matching vendors returns empty list
    - **Given** an in-memory database contains vendors that do not match the supplied filter criteria
    - **When** `GetVendorsQuery.Execute` is called with a filter value that matches none of them
    - **Then** the result is an empty list

The existing tests cover the reserved-vendor exclusion and the company-name filter. The spec SC-4 requires id, namespace prefix, contact name, and email filter paths. All four are parameters on `GetVendorsQuery.Execute(CommonQueryParams, id, company, namespacePrefixes, contactName, contactEmail)`. The tests follow the same in-memory + `Options.Create(AppSettings)` setup used in the existing file.

---

### Step 2026-06-23-improve-vendor-code-coverage-S5: VendorMapper — create test files (v2 + v3)

- **Depends on:** []
- **Parallel-safe:** yes
- **Outputs:**
  - `Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/VendorMapperTests.cs` (new)
  - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/VendorMapperTests.cs` (new)
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** ToModel maps all Vendor fields to VendorModel
    - **Given** a `Vendor` instance with a known ID, name, one namespace prefix, and a user with name and email
    - **When** `VendorMapper.ToModel` is called with that vendor
    - **Then** the returned `VendorModel` has the same ID, company name, contact name, email, and namespace prefix as the source vendor
  - **Scenario:** ToModel with no users produces null/empty contact fields without throwing
    - **Given** a `Vendor` instance with no Users collection entries
    - **When** `VendorMapper.ToModel` is called
    - **Then** the returned `VendorModel.ContactName` and `ContactEmailAddress` are null or empty, and no exception is thrown
  - **Scenario:** ToModelList maps all vendors in a collection
    - **Given** a list of two `Vendor` instances
    - **When** `VendorMapper.ToModelList` is called with that list
    - **Then** the returned list contains two `VendorModel` entries in the same order, each matching their source vendor

This step creates the missing `VendorMapperTests.cs` in both test projects (spec SC-5). `VendorMapper` is a static class in `Features/Vendors/` with two public methods: `ToModel(Vendor)` and `ToModelList(IEnumerable<Vendor>)`. The test file is placed in `Features/Vendors/` within each unit test project, mirroring the production code's folder structure. No `SqlServerUsersContext` setup is needed — `VendorMapper` operates directly on model objects with no EF Core dependency.

---

### Step 2026-06-23-improve-vendor-code-coverage-S6: Run unit tests and verify coverage

- **Depends on:** [2026-06-23-improve-vendor-code-coverage-S1, 2026-06-23-improve-vendor-code-coverage-S2, 2026-06-23-improve-vendor-code-coverage-S3, 2026-06-23-improve-vendor-code-coverage-S4, 2026-06-23-improve-vendor-code-coverage-S5]
- **Parallel-safe:** no
- **Outputs:** coverlet coverage report (HTML/JSON, generated under the test project output directories by `./build.ps1 -Command UnitTest`)
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** All tests pass
    - **Given** all test files from S1–S5 are in place
    - **When** `./build.ps1 -Command UnitTest` is run from the repository root
    - **Then** the build exits with code 0 and the test runner output shows zero failures across both `EdFi.Ods.AdminApi.UnitTests` and `EdFi.Ods.AdminApi.V3.UnitTests`
  - **Scenario:** Coverage meets the 80% threshold for targeted Vendor files
    - **Given** the `./build.ps1 -Command UnitTest` run produced a coverlet report
    - **When** the developer inspects the coverage report for `AddVendorCommand`, `EditVendorCommand`, `DeleteVendorCommand`, `GetVendorsQuery`, `VendorMapper`, `VendorExtensions`, `VendorModel`, `AddVendor`, `EditVendor`, `DeleteVendor`, `ReadVendor` in both v2 and v3
    - **Then** all targeted files show >80% line coverage and >80% branch coverage

This is the verification gate step. It runs after all test-writing steps are complete and confirms that both SC-6 (coverage target) and SC-7 (all tests pass) are satisfied. This step produces no code — it runs the existing build command and reads the coverlet output.

## 6. Traceability

- **SC-1: "EditVendorCommand: happy path, system-reserved vendor, vendor-without-users branch, and namespace replacement are each covered by a dedicated test in both v2 and v3."** — covered by: `2026-06-23-improve-vendor-code-coverage-S1`
- **SC-2: "DeleteVendorCommand: system-reserved vendor and cascade-delete-with-applications paths are covered in both v2 and v3."** — covered by: `2026-06-23-improve-vendor-code-coverage-S3`
- **SC-3: "AddVendorCommand: null namespace, multiple namespaces, and name-trimming cases are covered in both v2 and v3."** — covered by: `2026-06-23-improve-vendor-code-coverage-S2`
- **SC-4: "GetVendorsQuery: id, namespace prefix, contact name, and email filter paths are each covered by at least one test in both v2 and v3."** — covered by: `2026-06-23-improve-vendor-code-coverage-S4`
- **SC-5: "A new VendorMapperTests.cs exists in both EdFi.Ods.AdminApi.UnitTests and EdFi.Ods.AdminApi.V3.UnitTests covering ToModel and ToModelList."** — covered by: `2026-06-23-improve-vendor-code-coverage-S5`
- **SC-6: "Running ./build.ps1 -Command UnitTest with coverlet enabled produces a coverage report showing >80% line and branch coverage for the targeted Vendor files."** — covered by: `2026-06-23-improve-vendor-code-coverage-S6`
- **SC-7: "All new and existing tests pass without modification to source files (unless a confirmed bug fix is needed)."** — covered by: `2026-06-23-improve-vendor-code-coverage-S6`

## 7. Interface Contracts

No cross-layer boundaries identified. No `contracts.md` generated.
