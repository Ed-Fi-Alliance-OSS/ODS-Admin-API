# ClaimSet Name No-Whitespace Validation — Design Spec

**Date:** 2026-05-26  
**Scope:** `Application/EdFi.Ods.AdminApi.V3` only

---

## Summary

Enforce a new business rule: **ClaimSet names must not contain white spaces.**  
`"claimsetname"` is valid; `"claimset name"` is not.

---

## Affected Endpoints and Files

### Validation (source code)

| File | Field validated | Change |
|---|---|---|
| `Features/FeatureConstants.cs` | — | Add `ClaimSetNameNoWhitespaceMessage` constant |
| `Features/ClaimSets/AddClaimSet.cs` | `Name` | Add no-whitespace rule to `Validator` |
| `Features/ClaimSets/EditClaimSet.cs` | `Name` | Add no-whitespace rule to `Validator` |
| `Features/ClaimSets/ImportClaimSet.cs` | `Name` | Add no-whitespace rule to `Validator` |
| `Features/ClaimSets/CopyClaimSet.cs` | `Name` | Add no-whitespace rule to `Validator` |
| `Features/Applications/AddApplication.cs` | `ClaimSetName` | Add no-whitespace rule to `Validator` |
| `Features/Applications/EditApplication.cs` | `ClaimSetName` | Add no-whitespace rule to `Validator` |

---

## Implementation Details

### New constant in `FeatureConstants.cs`

```csharp
public const string ClaimSetNameNoWhitespaceMessage = "Claim set name must not contain white spaces.";
```

### Validation rule shape (FluentValidation)

For ClaimSet endpoints (field `Name`):
```csharp
RuleFor(m => m.Name)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

For Application endpoints (field `ClaimSetName`):
```csharp
RuleFor(m => m.ClaimSetName)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

The null guard (`name == null`) avoids double-firing with the existing `NotEmpty()` rule on the same field.

---

## Unit Tests (`EdFi.Ods.AdminApi.V3.UnitTests`)

New or updated test class per endpoint, following the pattern in `AddVendorValidatorTests.cs`.

### Files

| Test file | Status |
|---|---|
| `Features/ClaimSets/AddClaimSetValidatorTests.cs` | New |
| `Features/ClaimSets/EditClaimSetValidatorTests.cs` | New (extend `EditClaimSetTests.cs` or create) |
| `Features/ClaimSets/ImportClaimSetValidatorTests.cs` | New |
| `Features/ClaimSets/CopyClaimSetValidatorTests.cs` | New |
| `Features/Applications/AddApplicationValidatorTests.cs` | New |
| `Features/Applications/EditApplicationValidatorTests.cs` | New (extend `EditApplicationTests.cs` or create) |

### Test cases per class

- `Should_Have_Error_When_Name_Contains_Whitespace` — input with a space → error on `Name`/`ClaimSetName`
- `Should_Not_Have_Error_When_Name_Has_No_Whitespace` — clean name → no whitespace error

**Mocking strategy:** Validators that depend on `IGetAllClaimSetsQuery` or `IGetClaimSetByIdQuery` use **FakeItEasy** (matching existing patterns). `AddApplication.Validator` and `EditApplication.Validator` have no DB dependencies and need no mocks.

---

## Integration Tests (`EdFi.Ods.AdminApi.V3.DBTests`)

New test classes that use real DB-backed query instances (extend `SecurityDataTestBase`).

### Files

| Test file | Base class |
|---|---|
| `ClaimSetEditorTests/AddClaimSetValidatorTests.cs` | `SecurityDataTestBase` |
| `ClaimSetEditorTests/EditClaimSetValidatorTests.cs` | `SecurityDataTestBase` |
| `ClaimSetEditorTests/CopyClaimSetValidatorTests.cs` | `SecurityDataTestBase` |
| `ClaimSetEditorTests/ImportClaimSetValidatorTests.cs` | `SecurityDataTestBase` |

### Test cases per class

- `ShouldFailValidation_WhenNameContainsWhitespace` — name with space → validator returns invalid with expected message
- `ShouldPassValidation_WhenNameHasNoWhitespace` — clean name → validator returns valid

Application validators (`AddApplication`, `EditApplication`) have no DB-query dependencies in their validators, so unit tests are sufficient — no separate DB integration tests needed.

---

## E2E Tests (Bruno, `E2E Tests/Bruno Admin API E2E 3.0/v3`)

### New Bruno `.bru` files

**ClaimSets folder:**
- `POST - ClaimSets - Invalid Whitespace Name.bru`
- `PUT - ClaimSets - Invalid Whitespace Name.bru`
- `POST - ClaimSets-Copy - Invalid Whitespace Name.bru`
- `POST - ClaimSets - Import - Invalid Whitespace Name.bru`

**Application folder:**
- `POST - Applications - Invalid Whitespace ClaimSetName.bru`
- `PUT - Applications - Invalid Whitespace ClaimSetName.bru`

### Pattern for each file

- Send a request body with a name containing a space (e.g., `"claimset name"`)
- Assert HTTP 400
- Assert `response.errors["Name"]` (or `"ClaimSetName"`) contains the no-whitespace message
- Follow same sequence numbering and format as sibling "Invalid" test files

---

## Design Decisions

- **Null guard in rule:** The `name == null` guard prevents double error messages when the field is empty (the `NotEmpty()` rule handles that case).
- **No trim/auto-fix:** We reject names with spaces rather than auto-trimming, because silent data mutation is surprising.
- **Applications included:** Even though `ClaimSetName` on applications references an existing claimset, the rule is applied consistently to prevent referencing names that can never be created.
- **`Contains(' ')` vs regex:** Simple `Contains(' ')` is sufficient and readable. The requirement says "white spaces" but in context refers to space characters; if tabs/newlines are also excluded in the future, the rule can be updated to `name.Any(char.IsWhiteSpace)`.

---

## Out of Scope

- V1/V2 endpoints (only V3 is targeted per requirements)
- Migrating or rejecting existing DB records that already contain spaces
- Auto-trimming input
