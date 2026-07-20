# Design: Add `dbInstanceId` to ed-org instance responses

## Scope
Add a new response field named `dbInstanceId` to:

1. `GET /v2/tenants/{tenantName}/odsInstances/edOrgs`
2. `GET /v2/odsInstances/edOrgs`
3. `GET /v2/odsInstances/{instanceId}/edOrgs`

The field must expose the backing `DbInstance.Id` when available.

## Contract changes

### `TenantOdsInstanceModel`
Add:

```csharp
public int? DbInstanceId { get; set; }
```

Serialized JSON property name will be `dbInstanceId` under current JSON naming behavior.

### `OdsInstanceWithEducationOrganizationsModel`
Add:

```csharp
public int? DbInstanceId { get; set; }
```

Serialized JSON property name will be `dbInstanceId` under current JSON naming behavior.

## Data flow / mapping updates

### Tenant endpoint path
In `TenantService.GetTenantEdOrgsByInstancesAsync`:

- When a linked db instance is found for an ODS instance, set:
  - `DbInstanceId = dbInstance.Id`
  - existing enrichment fields (`Status`, `DatabaseTemplate`, `DatabaseName`) remain unchanged.
- When there is no linked db instance:
  - keep `Status = Created`
  - keep metadata fields null, including `DbInstanceId = null`.
- For unlinked db instances appended with negative ODS ids:
  - include `DbInstanceId = source.Id` in `TenantMapper.ToUnlinkedDbInstanceModel`.
  - preserve existing negative `OdsInstanceId` behavior.

### ODS instances endpoints path
In `ReadEducationOrganizations.MergeDbInstanceData`:

- For linked matches, set `instance.DbInstanceId = dbInstance.Id` together with the existing metadata enrichment.
- For unmatched entries, `DbInstanceId` remains null.
- For appended unlinked db instances, set `DbInstanceId = dbInstance.Id` on created response rows.

## Error handling and behavior
No new exceptions or validation rules are introduced.
No route, authorization, tenancy-header, or pagination behavior changes.
Only response payload shape is extended.

## Testing plan

### Unit tests (`Application/EdFi.Ods.AdminApi.UnitTests`)
Update assertions in focused tests:

- `Infrastructure/Services/Tenants/TenantServiceTests.cs`
  - linked enrichment asserts `DbInstanceId`.
  - no-linked asserts `DbInstanceId` is null.
  - mixed/unlinked scenarios assert appended entries carry `DbInstanceId`.
- `Features/OdsInstances/ReadEducationOrganizationsTests.cs`
  - linked merge asserts `DbInstanceId`.
  - unmatched scenario asserts null.
  - appended unlinked entries assert populated `DbInstanceId`.
- `Features/Tenants/TenantDetailModelTests.cs` only if property-level initialization/settable assertions need extension.

### Integration / DB tests (`Application/EdFi.Ods.AdminApi.DBTests`)
Update only tests that validate response/query field completeness for tenant/ed-org instance shaping so they include the new `dbInstanceId` expectation where applicable.

### E2E tests (`Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2`)
Schema updates in Bruno scripts:

- `OdsInstances/GET - OdsInstances - EdOrgs.bru`
- `Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru`
- `Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru`

Add optional property:

```json
"dbInstanceId": { "type": ["integer", "null"] }
```

## Out of scope
- Any change to negative synthetic `id` semantics for unlinked rows.
- Any db schema changes.
- Any behavioral change outside response payload enrichment.
