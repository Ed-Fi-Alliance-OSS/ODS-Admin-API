# Add DataStoreManageId to v3 Tenants/DataStores/EdOrgs response

Date: 2026-07-28

## Summary

v2's `/tenants/{tenantName}/odsInstances/edOrgs` endpoint returns `OdsInstanceManageId` on each
ods-instance entry — the id of the linked `OdsInstanceManage` record (provisioning status,
database template/name). v3's equivalent endpoint, `/tenants/{tenantName}/dataStores/edOrgs`,
never got the matching field on `TenantDataStoreModel`. This is a pre-existing parity gap, not a
new feature: the underlying query (`IGetDataStoreManagesQuery`) and DI wiring are already fully
present in v3 — only the model field and two assignments are missing.

This change adds `DataStoreManageId` (v3's naming counterpart to v2's `OdsInstanceManageId`) to
`TenantDataStoreModel`, populates it in `TenantService` and `TenantMapper`, and updates unit tests
and Bruno E2E schemas to match. While in the Bruno E2E schema file, it also closes an unrelated
pre-existing gap where the v3 schema was missing `status`/`databaseTemplate`/`databaseName`
properties that `TenantDataStoreModel` already returns and v2's schema already validates.

## Background / current state

- v2: `TenantOdsInstanceModel.OdsInstanceManageId` (`Application/EdFi.Ods.AdminApi/Features/Tenants/TenantDetailModel.cs:34`)
  is populated in two places:
  - `TenantService.GetTenantEdOrgsByInstancesAsync` (`Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs:156`)
    sets it for instances linked to an `OdsInstanceManage` record.
  - `TenantMapper.ToUnlinkedOdsInstanceManageModel` (`Application/EdFi.Ods.AdminApi/Features/Tenants/TenantMapper.cs:33`)
    sets it for orphaned `OdsInstanceManage` records with no matching ods instance.
- v3: `TenantDataStoreModel` (`Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantDetailModel.cs`)
  has no equivalent field. The parallel code in `TenantService`
  (`Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Tenants/TenantService.cs:152-164`) and
  `TenantMapper.ToUnlinkedDataStoreManageModel`
  (`Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantMapper.cs:28`) sets `Status`,
  `DatabaseTemplate`, `DatabaseName` but never an id.
- `ReadTenants.cs` (both versions) and the query/DI layer already match structurally — no changes
  needed there.
- v3's single-instance endpoint (`/dataStores/{id}/edOrgs`,
  `DataStoreWithEducationOrganizationsModel`) already has `DataStoreManageId` correctly — this gap
  is specific to the tenant-level `edOrgs` endpoint.
- Neither v2 nor v3 has any DBTests coverage for the tenants/edOrgs endpoint today, so DBTests are
  not part of this change.
- JSON serialization uses the default camelCase policy (no explicit `JsonPropertyName` needed) —
  `DataStoreManageId` will serialize as `dataStoreManageId`, matching the `odsInstanceManageId` /
  `dataStoreId` casing convention already used on this model.

## Naming

Per the existing v2→v3 renaming convention on this model (`OdsInstance` → `DataStore`,
`OdsInstanceId` → `DataStoreId`), the new field is named `DataStoreManageId`.

## Code changes

1. **`Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantDetailModel.cs`**
   Add `public int? DataStoreManageId { get; set; }` to `TenantDataStoreModel`, immediately after
   `DataStoreId`, mirroring v2's field order.

2. **`Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantMapper.cs`**
   In `ToUnlinkedDataStoreManageModel`, add `DataStoreManageId = source.Id` (mirrors v2's
   `ToUnlinkedOdsInstanceManageModel`).

3. **`Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Tenants/TenantService.cs`**
   In `GetTenantEdOrgsByInstancesAsync`, in the linked-data-store loop, add
   `dataStore.DataStoreManageId = dataStoreManage.Id;` alongside the existing `Status` /
   `DatabaseTemplate` / `DatabaseName` assignments.

No changes to `ReadTenants.cs`, queries, or DI registration.

## Test changes

### `Application/EdFi.Ods.AdminApi.V3.UnitTests`

- **`Infrastructure/Services/Tenants/TenantServiceTests.cs`** — add `DataStoreManageId` assertions
  (mirroring the equivalent v2 assertions already present in
  `EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs`) to:
  - `GetTenantEdOrgsByInstancesAsync_SetsStatusCreated_WhenDataStoreHasNoLinkedDataStoreManage` —
    assert `DataStoreManageId.ShouldBeNull()`.
  - `GetTenantEdOrgsByInstancesAsync_EnrichesDataStore_WithLinkedDataStoreManageFields` — assert
    `DataStoreManageId.ShouldBe(10)`.
  - `GetTenantEdOrgsByInstancesAsync_AddsUnlinkedDataStoreManages_WithNullIds` — extend the
    `ShouldContain` predicates to include `DataStoreManageId == 20` / `== 21`.
  - `GetTenantEdOrgsByInstancesAsync_MixedScenario_LinkedAndUnlinked` — assert
    `DataStoreManageId.ShouldBe(30)` on the linked entry and `.ShouldBe(31)` on the unlinked entry.
  - `GetTenantEdOrgsByInstancesAsync_AddsDataStoreManage_WhenLinkedToMissingDataStore_ForAllStatuses`
    — assert `DataStoreManageId.ShouldBe(42)`.
  - `GetTenantEdOrgsByInstancesAsync_AppendsLatestDataStoreManagePerMissingDataStoreId` — assert
    `DataStoreManageId.ShouldBe(51)`.

- **`Features/Tenants/TenantDetailModelTests.cs`** — in `Properties_ShouldBeSettable`, set
  `DataStoreManageId = 10` on the constructed `TenantDataStoreModel` and assert it round-trips,
  mirroring v2's equivalent test.

### DBTests

No changes — no existing DBTests coverage for this feature in either version.

### Bruno E2E (`Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Tenants/`)

In both `GET - Tenants EdOrgs by Tenant Name - Multitenant.bru` and
`GET - Tenants EdOrgs by Tenant Name - Singletenant.bru`, update `GetTenantDataStoresEdOrgsSchema`
to add, matching v2's `GetTenantOdsInstancesEdOrgsSchema`:
- `dataStoreManageId`: `["integer", "null"]`
- `status`: `["string", "null"]`
- `databaseTemplate`: `["string", "null"]`
- `databaseName`: `["string", "null"]`

## Out of scope

- v3's single-instance `/dataStores/{id}/edOrgs` endpoint — already has `DataStoreManageId`
  correctly.
- DBTests for the tenants/edOrgs endpoint (neither version has any today).
- Any other pre-existing v2/v3 parity gaps not touched by this endpoint's response model.
