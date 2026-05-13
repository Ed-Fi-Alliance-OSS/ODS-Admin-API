# Spike Design: Claim Set Export Format — Admin API v3

**Date:** 2026-05-08  
**Scope:** Admin API v3 only  
**Type:** Spike — findings and recommendation (no code changes)

---

## Problem Statement

Admin API v3 `GET /v3/claimSets/{id}/export` returns the full resource-claim tree (actions, default authorization strategies, overrides, children). CMS returns only a lightweight descriptor (`id`, `name`, `_isSystemReserved`, `_applications`). This gap was identified in the Gap Analysis (Row 11 — "Claim set export").

Additionally, the `Name` field on claim set create/import/edit endpoints accepts strings composed entirely of blank characters (spaces, tabs), which can produce unusable claim set names.

---

## Context

### Current Export Behavior (Admin API v3)

**Endpoint:** `GET /v3/claimSets/{id}/export`  
**Returns:** `ClaimSetDetailsModel` — a full resource-claim tree

```json
{
  "id": 1,
  "name": "EdFiSandbox",
  "_isSystemReserved": true,
  "_applications": [],
  "resourceClaims": [
    {
      "id": 1,
      "name": "types",
      "actions": [{ "name": "Read", "enabled": true }],
      "_defaultAuthorizationStrategiesForCRUD": [...],
      "authorizationStrategyOverridesForCRUD": [],
      "children": []
    }
  ]
}
```

### CMS Export Behavior

```json
{
  "id": 5,
  "name": "EdFiSandbox",
  "_isSystemReserved": true,
  "_applications": {}
}
```

### Export→Import Round-Trip (Critical Dependency)

The existing export endpoint serves a critical workflow:
1. User exports a claim set → receives the full-tree JSON as a `.json` file (in Admin App).
2. User edits the file manually (changing name, resource claims, strategies, etc.).
3. User imports the file → Admin API's `POST /v3/claimSets/import` parses the JSON and creates a new claim set with the full resource configuration.

The import endpoint (`ImportClaimSet.cs`) requires the `resourceClaims` array with full CRUD action and authorization strategy data. Removing this from the export output breaks the round-trip.

---

## Approaches Evaluated

### Option A — Replace full-tree with CMS descriptor (breaking change)

`GET /v3/claimSets/{id}/export` returns only the CMS-compatible descriptor.

**Pros:**
- Full parity with CMS default export.
- Simpler response shape.

**Cons:**
- **Breaks the export→import round-trip.** The import endpoint still needs resource claims; the exported file can no longer be used as an import payload without manual reconstruction.
- Admin App's export-as-file feature becomes non-functional for re-import use cases.
- Users must understand the full resource-claim structure to use the import endpoint — documentation alone is a poor substitute.

**Assessment:** Not recommended. The workflow breakage outweighs the benefit of CMS parity.

---

### Option B — CMS descriptor by default; full tree via `?format=full`

- `GET /v3/claimSets/{id}/export` → returns CMS-compatible descriptor (new default behavior).
- `GET /v3/claimSets/{id}/export?format=full` → returns the existing full resource-claim tree.

**Pros:**
- Default aligns with CMS — resolves the Gap Analysis finding.
- Full tree remains available; the export→import round-trip is preserved.
- Clean, versioned API design. The query parameter is a standard pattern for format negotiation.

**Cons:**
- **Forward-looking breaking change for Admin App:** Admin App currently uses the v2 Admin API endpoints (`admin-api.v2.service.ts`). Once Admin App migrates to v3, it will call `/export` without a query param and will receive the descriptor by default rather than the full tree. Admin App would need to append `?format=full` to preserve its export→import workflow. This change is minimal and well-contained (one service call).
- Slight increase in API surface (one query parameter).

**Admin App impact (when migrating to v3):**
In `packages/api/src/teams/edfi-tenants/starting-blocks/v2/admin-api.v2.service.ts` (or equivalent v3 service):
```typescript
// Current (pointing at v2 Admin API):
.get(`claimSets/${claimSetId}/export`)

// Required when switching to v3:
.get(`claimSets/${claimSetId}/export?format=full`)
```

---

### Option C — Keep full-tree as-is; document the gap with migration utility reference

No endpoint change. Document the CMS gap and reference the migration utility for interoperability.

**Pros:**
- Zero breaking changes.
- Zero implementation risk.

**Cons:**
- The CMS gap persists. The Gap Analysis finding remains unresolved.
- Interoperability between CMS and Admin API exports is left to documentation and tooling.

**Assessment:** Acceptable as a fallback, but fails to close the documented gap.

---

## Recommendation

**Adopt Option B.**

The default export response should align with CMS (descriptor-only), with the full resource-claim tree accessible via `?format=full`. This resolves the Gap Analysis finding, preserves the export→import round-trip, and results in a minimal, well-scoped change to Admin App.

### Proposed Response Contracts

**Default (`GET /v3/claimSets/{id}/export`):**
```json
{
  "id": 5,
  "name": "EdFiSandbox",
  "_isSystemReserved": true,
  "_applications": []
}
```

**Full format (`GET /v3/claimSets/{id}/export?format=full`):**
```json
{
  "id": 5,
  "name": "EdFiSandbox",
  "_isSystemReserved": true,
  "_applications": [],
  "resourceClaims": [...]
}
```

### Admin API v3 Implementation Notes

- In `ExportClaimSet.cs` (`EdFi.Ods.AdminApi.V3`): add an optional `format` query parameter (e.g., `string? format = null`).
- If `format == "full"`, return `ClaimSetDetailsModel` (existing behavior).
- Otherwise, return `ClaimSetModel` (descriptor only — already exists as the base class).
- The existing `ClaimSetModel` already maps to the descriptor shape; no new model is needed.

### Admin App Impact Summary

> **Note:** Admin App currently uses Admin API v2 endpoints. The impact below is forward-looking — it applies when Admin App migrates to Admin API v3.

| File | Change Required |
|------|----------------|
| `packages/api/.../starting-blocks/v2/admin-api.v2.service.ts` (or v3 equivalent) | Update `exportClaimset` to call `claimSets/{id}/export?format=full` |
| `packages/fe/.../useClaimsetActions.tsx` | No change needed (calls service method, not endpoint directly) |
| `packages/models/src/dtos/edfi-admin-api.v2.dto.ts` | Verify DTO still maps correctly to full-tree response (likely no change) |

---

## Finding 2 — Name Field Blank Character Validation

### Problem

The `Name` field on the following endpoints accepts strings composed entirely of blank characters (spaces, tabs):
- `POST /v3/claimSets` (`AddClaimSet.cs`)
- `POST /v3/claimSets/import` (`ImportClaimSet.cs`)
- `PUT /v3/claimSets/{id}` (`EditClaimSet.cs`)

FluentValidation's `NotEmpty()` treats `"   "` (spaces only) as a valid non-empty string. A claim set with a blank name creates an unusable record.

### Current Validators (example from `AddClaimSet.cs`)

```csharp
RuleFor(m => m.Name).NotEmpty()
    .Must(BeAUniqueName)
    .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage);
```

`NotEmpty()` rejects `null` and `""` but passes `"   "`.

### Recommendation

Add a `Must(name => name != null && name.Trim().Length > 0)` rule (or use `.NotEmpty().NotWhitespace()` if using FluentValidation 11+) to all three validators. Return a clear validation message such as `"Name must not consist of blank characters only."`.

This is a low-risk, surgical validation fix that should be implemented alongside or independently of the export format change.

---

## Migration Utility Reference

The Gap Analysis references a migration utility (VM to provide). Once the reference is available, it should be linked here as the recommended tool for migrating CMS-format exports into Admin API-compatible import payloads.

---

## Out of Scope

- Changes to Admin API v1 or v2.
- Changes to the import endpoint response or request body shape.
- Updating E2E tests or Bruno collections (these follow implementation).

---

## Next Steps

1. Stakeholder review of this spike document.
2. Create a separate ticket for the Name field whitespace validation fix across all three endpoints.
3. Obtain and link the migration utility reference from VM.
