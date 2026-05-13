# Design: Claim Set Export — Option A (CMS Descriptor-Only, Breaking Change)

**Date:** 2026-05-11  
**Scope:** Admin API v3 only  
**Related spike:** `docs/superpowers/specs/2026-05-08-claimset-export-spike-design.md`  
**Type:** Implementation design — concrete notes for each sub-option

---

## Summary

This document expands on **Option A** from the original spike: replacing the current full resource-claim tree export with a CMS-compatible descriptor. The export endpoint would return only `{ id, name, _isSystemReserved, _applications }`, aligning Admin API v3 with CMS.

The core Admin API change is straightforward. The primary complexity lies in deciding how to handle the **export→import round-trip**, which the current system relies on. Three sub-options are documented below.

---

## Core Change — Export Endpoint (All Sub-Options)

**Endpoint:** `GET /v3/claimSets/{id}/export`  
**Current response:** `ClaimSetDetailsModel` (full resource-claim tree)  
**New response:** `ClaimSetModel` (CMS-compatible descriptor)

### New Response Contract

```json
{
  "id": 5,
  "name": "EdFiSandbox",
  "_isSystemReserved": true,
  "_applications": []
}
```

### Implementation — `ExportClaimSet.cs`

**File:** `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/ExportClaimSet.cs`

```csharp
// Before:
internal static Task<IResult> GetClaimSet(
    IGetClaimSetByIdQuery getClaimSetByIdQuery,
    IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
    IGetApplicationsByClaimSetIdQuery getApplications, int id)
{
    var claimSet = getClaimSetByIdQuery.Execute(id);
    var allResources = getResourcesByClaimSetIdQuery.AllResources(id);
    var applications = getApplications.Execute(id);
    var claimSetData = ClaimSetMapper.ToDetailsModel(claimSet);
    if (applications != null)
        claimSetData.Applications = ClaimSetMapper.ToSimpleApplicationModelList(applications);
    if (allResources != null)
        claimSetData.ResourceClaims = ClaimSetMapper.ToClaimSetResourceClaimModelList(allResources.ToList());
    return Task.FromResult(Results.Ok(claimSetData));
}

// After:
internal static Task<IResult> GetClaimSet(
    IGetClaimSetByIdQuery getClaimSetByIdQuery,
    IGetApplicationsByClaimSetIdQuery getApplications, int id)
{
    var claimSet = getClaimSetByIdQuery.Execute(id);
    var applications = getApplications.Execute(id);
    var claimSetData = ClaimSetMapper.ToModel(claimSet);
    if (applications != null)
        claimSetData.Applications = ClaimSetMapper.ToSimpleApplicationModelList(applications);
    return Task.FromResult(Results.Ok(claimSetData));
}
```

**Notes:**
- `IGetResourcesByClaimSetIdQuery` is no longer injected into this handler — remove the dependency.
- `ClaimSetMapper.ToModel()` already exists and returns `ClaimSetModel`. No new model or mapper is needed.
- The Swagger `WithResponse<ClaimSetDetailsModel>(200)` annotation must be updated to `WithResponse<ClaimSetModel>(200)`.
- This is a **breaking change** on the `/v3/claimSets/{id}/export` response shape.

---

## The Round-Trip Problem

The original export→import workflow is:
1. `GET /v3/claimSets/{id}/export` → full-tree JSON saved as `.json` file.
2. User edits file (changes name, resource claims, strategies).
3. `POST /v3/claimSets/import` → parses JSON, creates a new claim set with full configuration.

Under Option A, the export no longer contains `resourceClaims`. The import endpoint (`ImportClaimSet.cs`) currently expects a `resourceClaims` array to configure the new claim set. Three sub-options address this.

---

## Sub-Option A1 — Accept the Breakage; Users Build Resource Claims via API

The export round-trip is removed. The import endpoint is unchanged (it still accepts a full `resourceClaims` payload for programmatic use). Users who need to replicate a claim set's resource configuration must do so manually through the resource claim management endpoints.

### Available Resource Claim Endpoints (Admin API v3)

| Endpoint | Description |
|----------|-------------|
| `POST /v3/claimSets/{claimSetId}/resourceClaimActions` | Adds a resource claim with actions to the claim set |
| `PUT /v3/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}` | Updates resource claim actions |
| `DELETE /v3/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}` | Removes a resource claim from the claim set |
| `POST /v3/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}/overrideAuthorizationStrategy` | Overrides default authorization strategies for a specific action |
| `POST /v3/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}/resetAuthorizationStrategies` | Resets authorization strategy overrides to defaults |

### Admin API Changes

No changes beyond the core export endpoint change. The import endpoint remains as-is.

### Admin App Impact

**Significant.** Admin App currently has no UI for the resource claim management endpoints listed above. Users would have no way to configure resource claims on a claim set through Admin App after importing a descriptor. To make A1 viable in Admin App, the following **new pages/flows** would be required:

| Admin App Feature Needed | Supporting Endpoint |
|--------------------------|---------------------|
| Add resource claim to claimset page/form | `POST /v3/claimSets/{id}/resourceClaimActions` |
| Edit resource claim actions form | `PUT /v3/claimSets/{id}/resourceClaimActions/{resourceClaimId}` |
| Remove resource claim action | `DELETE /v3/claimSets/{id}/resourceClaimActions/{resourceClaimId}` |
| Override authorization strategy form | `POST /v3/claimSets/{id}/resourceClaimActions/{resourceClaimId}/overrideAuthorizationStrategy` |
| Reset authorization strategies | `POST /v3/claimSets/{id}/resourceClaimActions/{resourceClaimId}/resetAuthorizationStrategies` |

**Assessment:** A1 is not low-effort. It shifts complexity from the export format to the Admin App UI, requiring substantial frontend work to cover all resource claim editing scenarios. It is only viable if new Admin App pages are built as part of the same effort.

---

## Sub-Option A2 — Add `GET /v3/claimSets/{id}/export?format=full` for Round-Trip Use

The default `/export` endpoint returns the CMS descriptor. A new, explicitly-named endpoint `/export/full` returns the full resource-claim tree. Admin App (when migrating to v3) uses `/export/full` for its export-as-file flow.

### New Endpoint Contract

```
GET /v3/claimSets/{id}/export?format=full
```

**Response:** Same as the current `/export` response (`ClaimSetDetailsModel`).

### Admin API Changes

Add a second route in `ExportClaimSet.cs`:

```csharp
public void MapEndpoints(IEndpointRouteBuilder endpoints)
{
    // CMS-compatible descriptor (new default)
    AdminApiEndpointBuilder.MapGet(endpoints, "/claimSets/{id}/export", GetClaimSetDescriptor)
        .WithSummary("Exports a specific claimset descriptor by id (CMS-compatible)")
        .WithRouteOptions(b => b.WithResponse<ClaimSetModel>(200))
        .BuildForVersions(AdminApiVersions.V3);

    // Full resource-claim tree (for import round-trip)
    AdminApiEndpointBuilder.MapGet(endpoints, "/claimSets/{id}/export/full", GetClaimSetFull)
        .WithSummary("Exports a specific claimset with full resource claim tree by id")
        .WithRouteOptions(b => b.WithResponse<ClaimSetDetailsModel>(200))
        .BuildForVersions(AdminApiVersions.V3);
}
```

`GetClaimSetDescriptor` uses the simplified handler from the core change above.  
`GetClaimSetFull` retains the current handler logic (injecting `IGetResourcesByClaimSetIdQuery`).

### Admin App Impact (when migrating to v3)

Minimal. One service call update in the v3 equivalent of `admin-api.v2.service.ts`:

```typescript
// exportClaimset method: update URL
.get(`claimSets/${claimSetId}/export?format=full`)
```

No model changes, no UI changes, no import flow changes.

**Assessment:** Clean separation of concerns. CMS gets its descriptor on the default URL; the full-tree path is explicit and self-documenting. This is the recommended sub-option if Option A is chosen.

---

## Sub-Option A3 — Update Import to Accept Descriptor-Only Payload

Make `resourceClaims` optional in `ImportClaimSetRequest`. If omitted or empty, the claimset is created with no resource claims. The export file (descriptor only) can be re-imported to create a copy of the claim set shell (name only).

### Admin API Changes — `ImportClaimSet.cs`

The `resourceClaims` field is already nullable (`List<ClaimSetResourceClaimModel>?`). The handler already guards with `if (claimSet.ResourceClaims != null && claimSet.ResourceClaims.Any())` in the validator. The main change is:

1. **Remove the validation error** that requires `resourceClaims` to be non-empty (currently implicit in `ResourceClaimValidator`).
2. **Guard the `addOrEditResourcesOnClaimSetCommand.Execute()` call** to skip it when `resourceClaims` is null or empty:

```csharp
// In Handle():
if (request.ResourceClaims != null && request.ResourceClaims.Any())
{
    var resourceClaims = ClaimSetMapper.ToResourceClaimList(request.ResourceClaims);
    var resolvedResourceClaims = strategyResolver.ResolveAuthStrategies(resourceClaims).ToList();
    addOrEditResourcesOnClaimSetCommand.Execute(addedClaimSetId, resolvedResourceClaims);
}
```

3. **Update the Swagger description** to clarify that `resourceClaims` is optional and that omitting it creates a claim set with no resource claims.

### Behavior

| Import payload | Result |
|----------------|--------|
| `{ name, resourceClaims: [...] }` | Creates claim set with full resource configuration |
| `{ name }` or `{ name, resourceClaims: [] }` | Creates an empty claim set shell (no resource claims) |

### Admin App Impact

Admin App's current import flow parses the exported `.json` file and POSTs its content to the import endpoint. If export only returns the descriptor, the imported claimset will be created with no resource claims. Users would then need to add resource claims manually (same Admin App gap as A1).

**Assessment:** A3 reduces the import endpoint friction but does not resolve the Admin App gap — users still have no UI to add resource claims post-import. It is most useful in API-only or scripting scenarios where consumers want to create a named claimset shell and populate it programmatically.

---

## Comparison Summary

| | A1 | A2 | A3 |
|---|---|---|---|
| `/export` returns descriptor | ✅ | ✅ | ✅ |
| Full tree still accessible | ❌ (API only via CRUD) | ✅ (`/export/full`) | ❌ |
| Import round-trip preserved | ❌ | ✅ (via `/export/full`) | Partial (shell only) |
| Admin App new pages needed | ✅ (significant) | ❌ | ✅ (same as A1) |
| Admin API changes | Small | Small + new route | Small + import relaxation |
| Breaking change | Yes | Yes (default `/export`) | Yes (default `/export`) |

---

## Out of Scope

- Changes to Admin API v1 or v2.
- Name field whitespace validation (tracked separately in the original spike).
- Admin App new resource claim management pages (flagged as a dependency of A1/A3 but not designed here).
- E2E test updates (follow implementation).

---

## Next Steps

1. Decide which sub-option to adopt (A1, A2, or A3).
2. If A1 or A3: scope the Admin App resource claim management pages as a prerequisite or parallel ticket.
3. If A2: create implementation tickets for Admin API v3 (`ExportClaimSet.cs` dual-route change) and Admin App service update (change `claimSets/${claimSetId}/export` → `claimSets/${claimSetId}/export/full`).
4. Update E2E Bruno collections for the new/changed export endpoint(s).
