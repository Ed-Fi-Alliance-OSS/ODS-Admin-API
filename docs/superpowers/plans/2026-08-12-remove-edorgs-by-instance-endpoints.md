# Remove edOrgs list-by-instance/data-store endpoints Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the V2 `GET /odsInstances/{instanceId}/edOrgs` and V3 `GET /dataStores/{dataStoreId}/edOrgs` endpoints, and every piece of code/tests/docs that exists solely to support them, without touching the tenant-scoped edOrgs feature or the refresh feature.

**Architecture:** No new code. Pure deletion of two `IFeature` endpoint classes (discovered via reflection, no registry to edit), their now-orphaned query/model classes, and their unit/integration/E2E tests, followed by narrow documentation edits.

**Tech Stack:** ASP.NET Core minimal APIs, EF Core, NUnit + Shouldly + FakeItEasy, Bruno E2E specs.

## Global Constraints

- Follow `.editorconfig`: file-scoped namespaces, single-line `using` directives, newline before `{`, final `return` on its own line (only relevant if any file is edited rather than deleted).
- Do not modify `NuGet.config` files.
- Do not remove or modify `EducationOrganizationMapper.cs` or `EducationOrganizationModels.cs` (either API version) — confirmed shared with `TenantService`/`TenantDetailModel` for the tenant-scoped edOrgs feature.
- Do not modify `ReadTenants.cs`, `RefreshEducationOrganizations.cs` (either version), or their tests/Bruno specs — confirmed independent of this removal.
- Run unit tests via `./build.ps1 -Command UnitTest -NoBuild` (or without `-NoBuild` for a full rebuild) after each code-deletion task.
- Reference spec: `docs/superpowers/specs/2026-08-12-remove-edorgs-by-instance-endpoints-design.md`.

---

### Task 1: Remove V2 edOrgs-by-instance endpoint, its query, and its unit tests

**Files:**
- Delete: `Application/EdFi.Ods.AdminApi/Features/OdsInstances/ReadEducationOrganizations.cs`
- Delete: `Application/EdFi.Ods.AdminApi/Features/OdsInstances/OdsInstanceWithEducationOrganizationsModel.cs`
- Delete: `Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs`
- Delete: `Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/ReadEducationOrganizationsTests.cs`
- Delete: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetEducationOrganizationsQueryTests.cs`

**Interfaces:**
- Consumes: none — these five files are confirmed to have no consumers outside each other (verified in the design spec's repo-wide search for `IGetEducationOrganizationsQuery`, `OdsInstanceWithEducationOrganizationsModel`, and `ReadEducationOrganizations`).
- Produces: nothing for later tasks. `EducationOrganizationMapper.cs` / `EducationOrganizationModels.cs` in the same `OdsInstances` folder are NOT touched here — they remain, used by `TenantService`.

- [ ] **Step 1: Confirm no other consumers before deleting**

Run:
```
git grep -nF "IGetEducationOrganizationsQuery" -- 'Application/EdFi.Ods.AdminApi/*' 'Application/EdFi.Ods.AdminApi.UnitTests/*'
git grep -nF "OdsInstanceWithEducationOrganizationsModel" -- 'Application/EdFi.Ods.AdminApi/*' 'Application/EdFi.Ods.AdminApi.UnitTests/*'
```
Expected: every match is inside one of the five files listed above. If any other file matches, stop and investigate before deleting.

- [ ] **Step 2: Delete the five files**

```bash
git rm "Application/EdFi.Ods.AdminApi/Features/OdsInstances/ReadEducationOrganizations.cs"
git rm "Application/EdFi.Ods.AdminApi/Features/OdsInstances/OdsInstanceWithEducationOrganizationsModel.cs"
git rm "Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs"
git rm "Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/ReadEducationOrganizationsTests.cs"
git rm "Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetEducationOrganizationsQueryTests.cs"
```

- [ ] **Step 3: Build to confirm no compile errors**

Run: `./build.ps1 -Command Build`
Expected: build succeeds with no errors referencing `ReadEducationOrganizations`, `GetEducationOrganizationsQuery`, or `OdsInstanceWithEducationOrganizationsModel`.

- [ ] **Step 4: Run V2 unit tests to confirm the suite is still green**

Run: `./build.ps1 -Command UnitTest -NoBuild -TestFilter "FullyQualifiedName~EdFi.Ods.AdminApi.UnitTests"`
Expected: PASS, with no tests for the deleted classes appearing in the run.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Remove V2 edOrgs-by-instance endpoint, query, and unit tests"
```

---

### Task 2: Remove V2 DB integration tests for the now-deleted query

**Files:**
- Delete: `Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetEducationOrganizationsQueryTests.cs`

**Interfaces:**
- Consumes: none — this file only exercised `GetEducationOrganizationsQuery`, deleted in Task 1.
- Produces: nothing for later tasks.

- [ ] **Step 1: Delete the file**

```bash
git rm "Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetEducationOrganizationsQueryTests.cs"
```

- [ ] **Step 2: Build the DBTests project to confirm no compile errors**

Run: `./build.ps1 -Command Build`
Expected: build succeeds.

- [ ] **Step 3: Run integration tests to confirm the suite is still green**

Run: `./build.ps1 -Command IntegrationTest -TestFilter "FullyQualifiedName~EdFi.Ods.AdminApi.DBTests"`
Expected: PASS. (Requires a local test database per `docs/developer.md` §Integration tests — if no local DB is configured, skip local execution and rely on CI, noting that in the PR.)

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Remove V2 GetEducationOrganizationsQuery DB integration tests"
```

---

### Task 3: Remove V2 Bruno E2E spec for the by-instance endpoint

**Files:**
- Delete: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/GET - OdsInstances - EdOrgs By InstanceId.bru`

**Interfaces:**
- Consumes: none — verified no other `.bru` file in the V2 `OdsInstances` or `Tenants` folders chains off this request's `Location` header or response body.
- Produces: nothing for later tasks.

- [ ] **Step 1: Delete the file**

```bash
git rm "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/GET - OdsInstances - EdOrgs By InstanceId.bru"
```

- [ ] **Step 2: Run the V2 OdsInstances Bruno collection to confirm the remaining specs still pass**

Run: `./eng/run-bruno-e2e.ps1 -ApiVersion 2 -BrunoFilter "v2/OdsInstances"`
Expected: PASS for all remaining specs (Refresh, Manage, CRUD); the deleted "EdOrgs By InstanceId" spec no longer appears in the run. (Requires the local Docker Compose stack per `docs/developer.md` §E2E tests.)

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Remove V2 Bruno E2E spec for edOrgs-by-instance endpoint"
```

---

### Task 4: Remove V3 edOrgs-by-data-store endpoint, its query, and its unit tests

**Files:**
- Delete: `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/ReadEducationOrganizations.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/DataStoreWithEducationOrganizationsModel.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/ReadEducationOrganizationsTests.cs`

**Interfaces:**
- Consumes: none — mirrors Task 1's verification for the V3 namespace. Note: unlike V2, there is no V3-only unit test file targeting the query directly (`GetEducationOrganizationsQueryTests.cs` exists only under `V3.DBTests`, handled in Task 5).
- Produces: nothing for later tasks. `EducationOrganizationMapper.cs` / `EducationOrganizationModels.cs` under `V3/Features/DataStores` are NOT touched — shared with V3 `TenantService`.

- [ ] **Step 1: Confirm no other consumers before deleting**

Run:
```
git grep -nF "IGetEducationOrganizationsQuery" -- 'Application/EdFi.Ods.AdminApi.V3/*' 'Application/EdFi.Ods.AdminApi.V3.UnitTests/*'
git grep -nF "DataStoreWithEducationOrganizationsModel" -- 'Application/EdFi.Ods.AdminApi.V3/*' 'Application/EdFi.Ods.AdminApi.V3.UnitTests/*'
```
Expected: every match is inside one of the four files listed above.

- [ ] **Step 2: Delete the four files**

```bash
git rm "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/ReadEducationOrganizations.cs"
git rm "Application/EdFi.Ods.AdminApi.V3/Features/DataStores/DataStoreWithEducationOrganizationsModel.cs"
git rm "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs"
git rm "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/ReadEducationOrganizationsTests.cs"
```

- [ ] **Step 3: Build to confirm no compile errors**

Run: `./build.ps1 -Command Build`
Expected: build succeeds.

- [ ] **Step 4: Run V3 unit tests to confirm the suite is still green**

Run: `./build.ps1 -Command UnitTest -NoBuild -TestFilter "FullyQualifiedName~EdFi.Ods.AdminApi.V3.UnitTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Remove V3 edOrgs-by-data-store endpoint, query, and unit tests"
```

---

### Task 5: Remove V3 DB integration tests for the now-deleted query

**Files:**
- Delete: `Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetEducationOrganizationsQueryTests.cs`

**Interfaces:**
- Consumes: none — only exercised `GetEducationOrganizationsQuery`, deleted in Task 4.
- Produces: nothing for later tasks.

- [ ] **Step 1: Delete the file**

```bash
git rm "Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetEducationOrganizationsQueryTests.cs"
```

- [ ] **Step 2: Build to confirm no compile errors**

Run: `./build.ps1 -Command Build`
Expected: build succeeds.

- [ ] **Step 3: Run integration tests to confirm the suite is still green**

Run: `./build.ps1 -Command IntegrationTest -TestFilter "FullyQualifiedName~EdFi.Ods.AdminApi.V3.DBTests"`
Expected: PASS. (Same local-DB caveat as Task 2 Step 3.)

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Remove V3 GetEducationOrganizationsQuery DB integration tests"
```

---

### Task 6: Remove V3 Bruno E2E spec for the by-data-store endpoint

**Files:**
- Delete: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/GET - DataStores - EdOrgs By InstanceId.bru`

**Interfaces:**
- Consumes: none — verified no other `.bru` file in the V3 `DataStores` or `Tenants` folders chains off this request's `Location` header or response body.
- Produces: nothing for later tasks.

- [ ] **Step 1: Delete the file**

```bash
git rm "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/GET - DataStores - EdOrgs By InstanceId.bru"
```

- [ ] **Step 2: Run the V3 DataStores Bruno collection to confirm the remaining specs still pass**

Run: `./eng/run-bruno-e2e.ps1 -ApiVersion 3 -BrunoFilter "v3/DataStores"`
Expected: PASS for all remaining specs; the deleted "EdOrgs By InstanceId" spec no longer appears in the run.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Remove V3 Bruno E2E spec for edOrgs-by-data-store endpoint"
```

---

### Task 7: Update documentation to drop dangling references

**Files:**
- Modify: `docs/design/Education-organization-Endpoints.md`
- Modify: `docs/http/education-organizations.http`
- Modify: `docs/PRD-ODS-Admin-API-2.4.md`
- Modify: `docs/TEST_COVERAGE_IMPROVEMENT_PLAN.md`

**Interfaces:** None — documentation only, no code interfaces involved.

- [ ] **Step 1: Edit `docs/design/Education-organization-Endpoints.md` — remove the by-instance REST endpoint bullet**

Replace:
```markdown
  * `GET /{version}/odsInstances/edOrgs` - Returns all education
    organizations from all instances
  * `GET /{version}/odsInstances/{instanceId}/edOrgs` - Returns education
    organizations for a specific instance
  * `POST /{version}/odsInstances/edOrgs/refresh` - Refreshes the education
    organizations for all instances
```
with:
```markdown
  * `GET /{version}/odsInstances/edOrgs` - Returns all education
    organizations from all instances
  * `POST /{version}/odsInstances/edOrgs/refresh` - Refreshes the education
    organizations for all instances
```

- [ ] **Step 2: Same file — remove the "Get Education Organizations for Specific Instance" example**

Replace:
````markdown
```http
GET /v2/odsInstances/edOrgs
Authorization: Bearer <token>
```

### Get Education Organizations for Specific Instance

```http
GET /v2/odsInstances/123/edOrgs
Authorization: Bearer <token>
```

## Configuration
````
with:
````markdown
```http
GET /v2/odsInstances/edOrgs
Authorization: Bearer <token>
```

## Configuration
````

- [ ] **Step 3: Same file — remove the now-deleted query classes from the Architecture section**

Replace:
```markdown
The service files can be maintained in a common project and shared between the
V1 and V2 projects to avoid code duplication.

* `IGetEducationOrganizationsQuery` - Main query handling interface

* `GetEducationOrganizationsQuery` - Implementation of database context query logic
  for reading the EducationOrganizations

The `RefreshEducationOrganizationCommand` service layer implements a
```
with:
```markdown
The service files can be maintained in a common project and shared between the
V1 and V2 projects to avoid code duplication.

The `RefreshEducationOrganizationCommand` service layer implements a
```

- [ ] **Step 4: Same file — update the Controllers section to drop `ReadEducationOrganizations`**

Replace:
```markdown
### Controllers

* `ReadEducationOrganizations` and `RefreshEducationOrganizations` features -
  REST API endpoints for read and refresh operations
* Includes proper authorization, error handling, and logging
```
with:
```markdown
### Controllers

* `RefreshEducationOrganizations` feature - REST API endpoint for refresh
  operations
* Includes proper authorization, error handling, and logging
```

- [ ] **Step 5: Edit `docs/http/education-organizations.http` — remove the by-instance sample request**

Replace:
```
### Get EducationOrganizations
GET {{adminapi_url}}/v2/odsInstances/edOrgs
Content-Type: application/json
Authorization: bearer {{token}}
Tenant: tenant1

### Get EducationOrganizations by OdsInstanceId
GET {{adminapi_url}}/v2/odsInstances/2/edOrgs
Content-Type: application/json
Authorization: bearer {{token}}
Tenant: tenant1
```
with:
```
### Get EducationOrganizations
GET {{adminapi_url}}/v2/odsInstances/edOrgs
Content-Type: application/json
Authorization: bearer {{token}}
Tenant: tenant1
```

- [ ] **Step 6: Edit `docs/PRD-ODS-Admin-API-2.4.md` — narrow FR-EDORG-2 to the all-instances route only**

Replace:
```markdown
- **FR-EDORG-2**: The API SHALL allow an administrator to retrieve education
  organizations grouped by their owning ODS instance, for all instances or a
  specific instance, via `GET /odsInstances/edOrgs` and
  `GET /odsInstances/{instanceId}/edOrgs` (v2) or the equivalent `dataStores`
  routes (v3).
```
with:
```markdown
- **FR-EDORG-2**: The API SHALL allow an administrator to retrieve education
  organizations grouped by their owning ODS instance, for all instances, via
  `GET /odsInstances/edOrgs` (v2) or the equivalent `dataStores` route (v3).
```

- [ ] **Step 7: Edit `docs/TEST_COVERAGE_IMPROVEMENT_PLAN.md` — drop the two now-deleted-file line items**

Replace:
```markdown
**Files to test:** AddOdsInstance.cs, EditOdsInstance.cs, DeleteOdsInstance.cs, ReadOdsInstance.cs, ReadEducationOrganizations.cs, RefreshEducationOrganizations.cs, OdsInstanceModel.cs, Commands/Queries
```
with:
```markdown
**Files to test:** AddOdsInstance.cs, EditOdsInstance.cs, DeleteOdsInstance.cs, ReadOdsInstance.cs, RefreshEducationOrganizations.cs, OdsInstanceModel.cs, Commands/Queries
```

- [ ] **Step 8: Same file — drop the `ReadEducationOrganizationsTests.cs` bullet**

Replace:
```markdown
* `EditOdsInstanceTests.cs`
* `DeleteOdsInstanceTests.cs`
* `ReadOdsInstanceTests.cs`
* `ReadEducationOrganizationsTests.cs`
* `RefreshEducationOrganizationsTests.cs`
```
with:
```markdown
* `EditOdsInstanceTests.cs`
* `DeleteOdsInstanceTests.cs`
* `ReadOdsInstanceTests.cs`
* `RefreshEducationOrganizationsTests.cs`
```

- [ ] **Step 9: Verify no unintended edits landed in the untouched list-all/historical content**

Run:
```
git diff -- docs/design/Education-organization-Endpoints.md docs/http/education-organizations.http docs/PRD-ODS-Admin-API-2.4.md docs/TEST_COVERAGE_IMPROVEMENT_PLAN.md
```
Expected: the diff shows only the removals above; the `GET /odsInstances/edOrgs` (list-all) lines and `docs/design/edorg-sync-v1-v2-analysis.md` are untouched.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Update docs to drop references to removed edOrgs-by-instance endpoints"
```

---

### Task 8: Final full-suite verification and dangling-reference sweep

**Files:** None created or modified — verification only.

**Interfaces:** None.

- [ ] **Step 1: Run the full unit test suite**

Run: `./build.ps1 -Command UnitTest`
Expected: PASS, full rebuild included.

- [ ] **Step 2: Run the full integration test suite (if a local test DB is configured; otherwise defer to CI)**

Run: `./build.ps1 -Command IntegrationTest`
Expected: PASS.

- [ ] **Step 3: Run the full V2 and V3 Bruno E2E suites (if the local Docker Compose stack is running; otherwise defer to CI)**

Run:
```
./eng/run-bruno-e2e.ps1 -ApiVersion 2
./eng/run-bruno-e2e.ps1 -ApiVersion 3
```
Expected: PASS.

- [ ] **Step 4: Repo-wide grep sweep for dangling references**

Run:
```
git grep -nF "odsInstances/{instanceId}/edOrgs"
git grep -nF "dataStores/{dataStoreId}/edOrgs"
git grep -nF "ReadEducationOrganizations"
git grep -nF "OdsInstanceWithEducationOrganizationsModel"
git grep -nF "DataStoreWithEducationOrganizationsModel"
```
Expected: no results for the first two; the class-name searches return zero results outside of `docs/design/edorg-sync-v1-v2-analysis.md` and `docs/design/2026-05-17-rename-odsinstance-to-datastore-v3-only.md` (both deliberately-untouched historical docs per the spec).

- [ ] **Step 5: Manually confirm both routes 404**

Run the API locally (`build.ps1 run` or the Docker compose stack per `docs/developer.md`), then:
```
curl -i https://localhost/adminapi/v2/odsInstances/1/edOrgs
curl -i https://localhost/adminapi/v3/dataStores/1/edOrgs
```
Expected: both return `404 Not Found`.

- [ ] **Step 6: Add a note to ADMINAPI-1488 flagging the corrected assumption**

Post a comment on the ticket (with the user's confirmation before posting) noting that `IGetEducationOrganizationsQuery` was found to be exclusive to the removed endpoints rather than shared with `ReadTenants`/`RefreshEducationOrganizations`, and was therefore removed as dead code rather than preserved — see `docs/superpowers/specs/2026-08-12-remove-edorgs-by-instance-endpoints-design.md` for details.

- [ ] **Step 7: Final commit (only if any of the above steps produced file changes, e.g. a re-run fix)**

```bash
git add -A
git commit -m "[ADMINAPI-1488] Final verification for edOrgs-by-instance endpoint removal"
```
