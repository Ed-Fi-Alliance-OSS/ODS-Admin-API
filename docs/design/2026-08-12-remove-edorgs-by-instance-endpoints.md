# Remove edOrgs list-by-instance/data-store endpoints

**Ticket:** [ADMINAPI-1488](https://edfi.atlassian.net/browse/ADMINAPI-1488)
**Date:** 2026-08-12

## What changed

Removed two deprecated endpoints that returned education organizations
nested under a single ODS instance / data store:

- V2: `GET /odsInstances/{instanceId}/edOrgs`
- V3: `GET /dataStores/{dataStoreId}/edOrgs`

Both endpoints now return 404 — asserted by a dedicated Bruno E2E spec per
API version (`GET - OdsInstances - EdOrgs By InstanceId Route Removed.bru`,
`GET - DataStores - EdOrgs By InstanceId Route Removed.bru`), so an
accidental route reintroduction would be caught by CI. The tenant-scoped
edOrgs endpoint (`ReadTenants`) and the `RefreshEducationOrganizations`
refresh feature (both API versions) are unaffected.

## Why the query classes were also removed

The ticket assumed `IGetEducationOrganizationsQuery` was shared with
`ReadTenants` and `RefreshEducationOrganizations`, and asked that it be
preserved. Investigation found this was no longer accurate:

- `ReadTenants` uses a different, singular-named query,
  `IGetEducationOrganizationQuery`, via `TenantService`.
- `RefreshEducationOrganizations` doesn't call either query — it runs raw
  SQL via `IEducationOrganizationService`.

`IGetEducationOrganizationsQuery` (plural), in both V2 and V3, was
exclusive to the two removed endpoints. It was removed as dead code along
with the endpoints, rather than preserved.

## Removed

- `ReadEducationOrganizations.cs` (V2 and V3 feature endpoints)
- `OdsInstanceWithEducationOrganizationsModel.cs` / `DataStoreWithEducationOrganizationsModel.cs` (response wrapper models)
- `GetEducationOrganizationsQuery.cs` (V2 and V3, interface + implementation)
- Their unit tests, DB integration tests, and Bruno E2E specs (the specs
  that exercised the 200-response behavior were deleted; replaced with a
  404-regression spec per API version, see above)

## Explicitly preserved (verified untouched)

- `ReadTenants.cs`, `RefreshEducationOrganizations.cs` (both API versions)
- `EducationOrganizationMapper.cs`, `EducationOrganizationModels.cs` (both
  versions) — shared with `TenantService`

## Out of scope

- A pre-existing "list-all" route (`GET /odsInstances/edOrgs`, no
  `instanceId`) is described in several docs but isn't actually
  implemented in code. That's a separate, pre-existing documentation bug,
  not something this change introduced or fixed.
- `docs/PRD-ODS-Admin-API-2.4.md` was left as-is at the requester's
  direction, so it still describes the removed by-instance/data-store
  routes as if they exist.

## References

- PR: [#429](https://github.com/Ed-Fi-Alliance-OSS/ODS-Admin-API/pull/429)
- Full design rationale and task-by-task implementation plan (superseded
  by this summary) are no longer kept in the repo — see PR #429 and this
  branch's commit history for the detailed trail.
