---
feature: 2026-06-23-improve-vendor-code-coverage
spec-version: 1
date: 2026-06-23
generated-by: generate-uat
test-count: 18
environment: local
---

# UAT Checklist — Improve Vendor Code Coverage for Admin API v2 and v3

> **How to use this checklist**
> Complete the Environment Setup section first. Then run each section in order:
> Smoke → Happy Path → Edge Cases → Regression. Mark each item Pass, Fail, or Skip.
> A Fail or unexpected result means the implementation diverges from the spec —
> open an issue or revisit the implement phase before declaring the feature done.

---

## Environment Setup

Complete every item in this section **before** running any test below.

| # | Setup step | Done? |
|---|-----------|-------|
| E1 | Open a terminal (PowerShell or Bash) in the repository root: `C:\dev\ed-fi\ODS-Admin-API` (or wherever the repo is cloned) | ☐ |
| E2 | Verify .NET SDK is available: run `dotnet --version`. Should print a version string without error. | ☐ |
| E3 | Confirm you are on branch `ADMINAPI-1370` (or the branch containing the new tests): run `git branch --show-current`. | ☐ |

> No authentication, database connections, or seed data are required. All unit tests use isolated in-memory EF Core databases created per-test.
> If any setup step cannot be completed, stop. The test results below will be invalid.

---

## §1 — Smoke Tests

> **Goal:** Confirm both test projects compile and tests are discovered without fundamental errors.
> A smoke failure blocks all sections below.

---

### S1.1 — Both unit test projects build and tests are discovered

**Traces to:** `spec.md` — Success Criteria 7: "All new and existing tests pass without modification to source files"

**Preconditions:**
- Terminal open in repository root (E1–E3 complete)

**Steps:**
1. Run: `./build.ps1 -Command UnitTest`
2. Wait for the full test run to complete (may take several minutes).

**Expected result:** The build output shows no compilation errors and the NUnit test runner discovers tests in both `EdFi.Ods.AdminApi.UnitTests` (v2) and `EdFi.Ods.AdminApi.V3.UnitTests` (v3). The final summary line shows 0 failed tests. Any non-zero failure count is a smoke failure — stop here.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### S1.2 — Vendor test files exist on disk in both projects

**Traces to:** `spec.md` — Success Criteria 5: "A new `VendorMapperTests.cs` exists in both test projects"

**Preconditions:**
- Repository is on the correct branch (E3 complete)

**Steps:**
1. Confirm the following files exist (use Explorer, `ls`, or `Get-ChildItem`):
   - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs`
   - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/AddVendorCommandTests.cs`
   - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs`
   - `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs`
   - `Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/VendorMapperTests.cs`
   - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs`
   - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/AddVendorCommandTests.cs`
   - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs`
   - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs`
   - `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/VendorMapperTests.cs`

**Expected result:** All 10 files listed above exist on disk. `VendorMapperTests.cs` in particular must be present in both v2 and v3 test projects (it was newly created by this feature).

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

## §2 — Happy Path

> **Goal:** Verify every success criterion from the spec for the case where inputs are valid and preconditions are met.

---

### HP2.1 — EditVendorCommand: happy path update persists all fields (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 1: "EditVendorCommand: happy path … covered by a dedicated test in both v2 and v3"

**Preconditions:**
- S1.1 passed (test run completed without errors)

**Steps:**
1. In the test output from `./build.ps1 -Command UnitTest`, locate (search or scroll) the test result for:
   - `EdFi.Ods.AdminApi.UnitTests` → `EditVendorCommandTests.Execute_WithValidVendor_UpdatesVendorAndPersists`
   - `EdFi.Ods.AdminApi.V3.UnitTests` → `EditVendorCommandTests.Execute_WithValidVendor_UpdatesVendorAndPersists`

**Expected result:** Both test results show **Passed**. If either shows Failed or is absent, this criterion is not met.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.2 — EditVendorCommand: vendor-without-users branch creates new user (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 1: "vendor-without-users branch … covered by a dedicated test in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `EditVendorCommandTests.Execute_WithVendorHavingNoUsers_CreatesNewUser` (v2 and v3)

**Expected result:** Both show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.3 — EditVendorCommand: namespace prefix replacement replaces old prefix (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 1: "namespace replacement … covered by a dedicated test in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `EditVendorCommandTests.Execute_WithDifferentNamespacePrefix_ReplacesOldPrefix` (v2 and v3)

**Expected result:** Both show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.4 — DeleteVendorCommand: cascade-delete with applications (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 2: "cascade-delete-with-applications paths are covered in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `DeleteVendorCommandTests.Execute_WithVendorHavingApplications_InvokesDeleteApplicationCommandForEachApplication` (v2 and v3)

**Expected result:** Both show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.5 — AddVendorCommand: null namespace prefix results in empty prefix collection (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 3: "null namespace … case covered in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `AddVendorCommandTests.Execute_WithNullNamespacePrefixes_PersistsVendorWithEmptyPrefixCollection` (v2 and v3)

**Expected result:** Both show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.6 — AddVendorCommand: multiple comma-separated namespaces persisted separately (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 3: "multiple namespaces … case covered in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `AddVendorCommandTests.Execute_WithMultipleCommaSeparatedNamespaces_PersistsEachPrefixSeparately` (v2 and v3)

**Expected result:** Both show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.7 — AddVendorCommand: padded fields are trimmed on persist (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 3: "name-trimming cases are covered in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `AddVendorCommandTests.Execute_WithPaddedFields_TrimsCompanyContactNameAndEmail` (v2 and v3)

**Expected result:** Both show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.8 — GetVendorsQuery: all four filter paths pass (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 4: "id, namespace prefix, contact name, and email filter paths each covered by at least one test in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate all four filter tests in both `EdFi.Ods.AdminApi.UnitTests` and `EdFi.Ods.AdminApi.V3.UnitTests`:
   - `GetVendorsQueryTests.Execute_WithIdFilter_ReturnsMatchingVendor`
   - `GetVendorsQueryTests.Execute_WithNamespacePrefixFilter_ReturnsMatchingVendor`
   - `GetVendorsQueryTests.Execute_WithContactNameFilter_ReturnsMatchingVendor`
   - `GetVendorsQueryTests.Execute_WithContactEmailFilter_ReturnsMatchingVendor`

**Expected result:** All 8 test results (4 tests × 2 projects) show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.9 — VendorMapper: ToModel and ToModelList tests pass (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 5: "VendorMapperTests.cs exists in both test projects covering `ToModel` and `ToModelList`"

**Preconditions:**
- S1.2 passed (VendorMapperTests.cs files exist in both projects)
- S1.1 passed

**Steps:**
1. In the test output, locate all three mapper tests in both projects:
   - `VendorMapperTests.ToModel_MapsAllFields_Correctly`
   - `VendorMapperTests.ToModel_WithNoUsers_ReturnsNullContactFields`
   - `VendorMapperTests.ToModelList_MapsAllVendors_InOrder`

**Expected result:** All 6 test results (3 tests × 2 projects) show **Passed**.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### HP2.10 — Coverage report shows >80% line and branch coverage for targeted Vendor files

**Traces to:** `spec.md` — Success Criteria 6: "coverage report showing >80% line and branch coverage for the targeted Vendor files"

**Preconditions:**
- S1.1 passed (test run with coverlet completed)

**Steps:**
1. After `./build.ps1 -Command UnitTest` completes, locate the coverlet output directory. It is typically under the test project output folders, e.g.:
   - `Application/EdFi.Ods.AdminApi.UnitTests/TestResults/` or a `coverage/` subdirectory
   - `Application/EdFi.Ods.AdminApi.V3.UnitTests/TestResults/`
2. Open the coverage report (HTML or JSON/LCOV). If HTML: open `index.html` in a browser.
3. Navigate to or search for the following source files in the report:
   - `EditVendorCommand.cs`
   - `DeleteVendorCommand.cs`
   - `AddVendorCommand.cs`
   - `GetVendorsQuery.cs`
   - `VendorMapper.cs`
4. Note the **Line %** and **Branch %** for each file.

**Expected result:** Each targeted Vendor file shows ≥ 80% line coverage and ≥ 80% branch coverage. Files that were untested before this feature (particularly `VendorMapper.cs`) should now show substantial coverage rather than 0%.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** Record actual percentages here: ________________________________

---

## §3 — Edge Cases

> **Goal:** Verify the implementation handles boundary values and error states correctly, matching the spec's failure paths.

---

### EC3.1 — EditVendorCommand: system-reserved vendor cannot be modified (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 1: "system-reserved vendor … covered by a dedicated test in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `EditVendorCommandTests.Execute_WithSystemReservedVendor_ThrowsArgumentException` (v2 and v3)

**Expected result:** Both show **Passed**. This test verifies that attempting to edit a system-reserved vendor (e.g., `VendorExtensions.ReservedNames[0]`) throws an `ArgumentException` with a message containing "may not be modified" — not a silent success or a different exception type.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### EC3.2 — EditVendorCommand: unknown vendor ID throws NotFoundException (v2 + v3)

**Traces to:** `spec.md` — SC-1 failure path; confirmed by implementation scan (`Execute_WithUnknownVendor_ThrowsNotFoundException`)

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `EditVendorCommandTests.Execute_WithUnknownVendor_ThrowsNotFoundException` (v2 and v3)

**Expected result:** Both show **Passed**. Passing vendor ID `999` to a context with no vendors should throw `NotFoundException<int>`, not silently return or produce a null reference.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### EC3.3 — DeleteVendorCommand: system-reserved vendor cannot be deleted (v2 + v3)

**Traces to:** `spec.md` — Success Criteria 2: "system-reserved vendor … path covered in both v2 and v3"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `DeleteVendorCommandTests.Execute_WithSystemReservedVendor_ThrowsArgumentException` (v2 and v3)

**Expected result:** Both show **Passed**. Attempting to delete a reserved vendor throws `ArgumentException` and the vendor record remains in the database (not deleted).

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### EC3.4 — GetVendorsQuery: non-matching filter returns empty list (v2 + v3)

**Traces to:** `spec.md` — SC-4 boundary case; confirmed by `Execute_WithNonMatchingFilter_ReturnsEmptyList`

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `GetVendorsQueryTests.Execute_WithNonMatchingFilter_ReturnsEmptyList` (v2 and v3)

**Expected result:** Both show **Passed**. Filtering by a company name that does not exist returns an empty list, not an error or null.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### EC3.5 — VendorMapper: vendor with no users does not throw (v2 + v3)

**Traces to:** `spec.md` — SC-5; boundary case for `ToModel` when `Users` collection is empty

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, locate:
   - `VendorMapperTests.ToModel_WithNoUsers_ReturnsNullContactFields` (v2 and v3)

**Expected result:** Both show **Passed**. Mapping a vendor with an empty `Users` list produces null `ContactName` and `ContactEmailAddress` without throwing a `NullReferenceException` or `InvalidOperationException`.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

## §4 — Regression Checks

> **Goal:** Confirm adjacent test behavior that the implementation touched still works correctly.
> Deviations logged during implementation (state.json notes) flag the specific risk areas below.

---

### RG4.1 — Existing tests in both projects still pass (no regressions from new test additions)

**Traces to:** `state.json` notes — "All tests pass (v2: 274 passed, v3: 316 passed, 0 failures)"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the `./build.ps1 -Command UnitTest` output, find the overall test summary for both test projects.
2. Note the total passed/failed counts.

**Expected result:** Total passed count in v2 (`EdFi.Ods.AdminApi.UnitTests`) is ≥ 274. Total passed count in v3 (`EdFi.Ods.AdminApi.V3.UnitTests`) is ≥ 316. Failed count in both is 0. (Counts may be higher if other tests were added; they must not be lower.)

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** Actual counts — v2: _______ passed / _______ failed. v3: _______ passed / _______ failed.

---

### RG4.2 — DeleteVendorCommand: Application model OperationalContextUri deviation does not affect existing tests

**Traces to:** `state.json` notes — deviation S3: "Application model requires OperationalContextUri for EF in-memory — set to empty string in test fixtures"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, confirm all four `DeleteVendorCommandTests` tests pass in both v2 and v3:
   - `Execute_WithUnknownVendor_ThrowsNotFoundException`
   - `Execute_WithExistingVendor_RemovesVendorAndUsers`
   - `Execute_WithSystemReservedVendor_ThrowsArgumentException`
   - `Execute_WithVendorHavingApplications_InvokesDeleteApplicationCommandForEachApplication`

**Expected result:** All 8 tests (4 × 2 projects) show **Passed**. The `OperationalContextUri = string.Empty` workaround for the in-memory EF provider should have no effect on test outcomes or assertions.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

### RG4.3 — GetVendorsQuery: namespace prefix exact-match behavior is consistent

**Traces to:** `state.json` notes — deviation S4: "Namespace prefix filter is exact-match (==) not contains — tests use exact values"

**Preconditions:**
- S1.1 passed

**Steps:**
1. In the test output, confirm `GetVendorsQueryTests.Execute_WithNamespacePrefixFilter_ReturnsMatchingVendor` passes in both v2 and v3.
2. Optionally: open `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs` and confirm the test passes the exact value `"https://acme.org/ns"` as the filter (not a substring).

**Expected result:** The test passes with an exact-match namespace prefix. If the production query were to change from `==` to `Contains`, this test would break — confirming the test correctly captures the actual behavior rather than a weaker assertion.

☐ Pass &nbsp;&nbsp; ☐ Fail &nbsp;&nbsp; ☐ Skip

**Notes:** ____________________________________________________________

---

## Sign-off

| Field              | Value                          |
|--------------------|--------------------------------|
| Tested by          | ______________________________ |
| Date tested        | ______________________________ |
| Environment        | Local dev — `./build.ps1 -Command UnitTest` |
| Spec version       | 1                              |
| Overall result     | ☐ All passed &nbsp; ☐ Failures present |
| Open issues        | ______________________________ |
| Feature declared done by | ______________________________ |

> **Reminder:** This checklist is a testing guide, not an automated gate.
> The developer decides when the feature is done based on results. Open failures
> should be tracked as issues before the feature is merged or shipped.
