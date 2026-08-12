# Design: Remove edOrgs list-by-instance/data-store endpoints

**Ticket:** [ADMINAPI-1488](https://edfi.atlassian.net/browse/ADMINAPI-1488)
**Date:** 2026-08-12

## Context

Admin API exposes two endpoints that return the education organizations for a
single ODS instance / data store, nested under that instance/data store:

- V2: `GET /odsInstances/{instanceId}/edOrgs` —
  `Application/EdFi.Ods.AdminApi/Features/OdsInstances/ReadEducationOrganizations.cs`
- V3: `GET /dataStores/{dataStoreId}/edOrgs` —
  `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/ReadEducationOrganizations.cs`

These endpoints are no longer needed and are being removed from both API
versions.

### Correction to the ticket's stated assumption

The ticket assumes `IGetEducationOrganizationsQuery` is shared with the
tenant-scoped `ReadTenants` endpoint and with `RefreshEducationOrganizations`,
and instructs that it must be preserved. Investigation of the current
codebase shows this is no longer accurate:

- `ReadTenants` uses a different, singular-named query,
  `IGetEducationOrganizationQuery`, via `TenantService.GetTenantEdOrgsByInstancesAsync`.
- `RefreshEducationOrganizations` doesn't call either query — it enqueues a
  Quartz job that runs raw SQL via `IEducationOrganizationService`.

`IGetEducationOrganizationsQuery` (plural), in both V2 and V3, is used
**exclusively** by the two endpoints this ticket removes. Once those
endpoints are gone, the query implementations and their DB integration tests
become dead code. Decision: remove them as part of this change rather than
leave orphaned code behind, and flag the discrepancy back on the ticket for
the author's awareness.

## Scope

### Delete outright

Feature endpoints, their now-exclusive query, and all their tests:

1. `Application/EdFi.Ods.AdminApi/Features/OdsInstances/ReadEducationOrganizations.cs`
2. `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/ReadEducationOrganizations.cs`
3. `Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/ReadEducationOrganizationsTests.cs`
4. `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/ReadEducationOrganizationsTests.cs`
5. `Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs` (V2 query — orphaned by this removal)
6. `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs` (V3 query — orphaned by this removal)
7. `Application/EdFi.Ods.AdminApi/Features/OdsInstances/OdsInstanceWithEducationOrganizationsModel.cs` (V2 response wrapper model — used only by the query/endpoint above)
8. `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/DataStoreWithEducationOrganizationsModel.cs` (V3 response wrapper model — used only by the query/endpoint above)
9. `Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetEducationOrganizationsQueryTests.cs`
10. `Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetEducationOrganizationsQueryTests.cs`
11. `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetEducationOrganizationsQueryTests.cs` (V2-only in-memory-EF unit test of the query itself; no V3 equivalent exists)
12. `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/GET - OdsInstances - EdOrgs By InstanceId.bru`
13. `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/GET - DataStores - EdOrgs By InstanceId.bru`

**Explicitly NOT orphaned — verified shared, do not touch:** `EducationOrganizationMapper.cs` and `EducationOrganizationModels.cs` (`EducationOrganizationModel` class), both V2 and V3 — also consumed by `TenantService`/`TenantDetailModel` for the tenant-scoped edOrgs feature.

No central feature-registration list needs editing: `IFeature`
implementations are discovered via reflection
(`FeaturesHelper`/`executingAssembly.GetTypes()...`), not an explicit
registry, so deleting the `.cs` files is sufficient to drop the routes.
Confirmed by a repo-wide search for `GetEducationOrganizationsQuery`: no
explicit DI registration (e.g. `AddTransient<IGetEducationOrganizationsQuery, ...>`)
exists outside the files already listed for deletion.

### Update (documentation)

Remove the by-instance/by-data-store route mentions and dangling references
to the deleted class names (`ReadEducationOrganizations`,
`IGetEducationOrganizationsQuery`, `GetEducationOrganizationsQuery`); leave
unrelated content (including the stale list-all route mentions noted below)
untouched:

12. `docs/design/Education-organization-Endpoints.md`
13. `docs/http/education-organizations.http`
14. `docs/PRD-ODS-Admin-API-2.4.md` (FR-EDORG-2)
15. `docs/TEST_COVERAGE_IMPROVEMENT_PLAN.md` (drop the two now-deleted-file line items, lines 527 and 538 as of this writing)

### Explicitly out of scope

- `ReadTenants` / `RefreshEducationOrganizations` (both API versions) and
  their tests/Bruno specs — verified independent of the removed endpoints
  and the removed query; no chained `.bru` requests depend on the removed
  endpoints' responses.
- The "list-all" route `GET /odsInstances/edOrgs` (no `instanceId`)
  mentioned in `Education-organization-Endpoints.md`,
  `education-organizations.http`, and the PRD, but not actually implemented
  anywhere in the codebase — a pre-existing documentation bug unrelated to
  this ticket. Not fixed here; flagged as a follow-up note on the ticket.
- `docs/design/edorg-sync-v1-v2-analysis.md` — a historical record of the
  v1→v2 route mapping at a point in time, not a description of current
  behavior. Left as-is.

## Acceptance Criteria

- `GET /odsInstances/{instanceId}/edOrgs` (V2) and
  `GET /dataStores/{dataStoreId}/edOrgs` (V3) no longer exist and return 404
  (route not found).
- `ReadTenants`, `RefreshEducationOrganizations`, and their respective
  queries/services continue to work unaffected (verified by their existing
  test suites remaining green, unmodified).
- A repo-wide search for `edOrgs` combined with `odsInstances/{instanceId}`
  and separately with `dataStores/{dataStoreId}` returns no remaining
  references, aside from the deliberately-untouched items listed above.
- Full unit, integration/DB, and E2E (Bruno) suites pass with no dangling
  references to the removed endpoints or query.

## Verification Approach

1. Delete the files listed above.
2. Update the four documentation files, removing only the by-id route
   content.
3. Run `./build.ps1 -Command UnitTest` (V2 and V3 unit test projects).
4. Run the DBTests projects to confirm `GetEducationOrganizationsQueryTests`
   is gone and no other DB test references the removed query.
5. Run the affected Bruno E2E collections (V2 `OdsInstances`, V3
   `DataStores`) to confirm the removed specs are gone and the
   Refresh/Tenants specs still pass unchanged.
6. Manually/E2E-confirm both removed routes now return 404.
7. Repo-wide grep for the route/class name patterns above to confirm no
   dangling references remain.
