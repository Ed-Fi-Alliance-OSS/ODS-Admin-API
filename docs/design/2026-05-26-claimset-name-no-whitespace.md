# Design: ClaimSet Name No-Whitespace Validation (V3)

## Problem Statement

ClaimSet names containing whitespace (e.g. `"Ed-Fi Sandbox"`) caused ambiguity in downstream consumers and are better expressed as single tokens. The V3 API must reject any ClaimSet name or Application `claimSetName` that contains whitespace characters (spaces, tabs, newlines, etc.) with a clear validation error.

## Scope

**V3 only** — `Application/EdFi.Ods.AdminApi.V3` and its test projects. V1/V2 endpoints are unchanged.

## Implementation

### New constant (`FeatureConstants.cs`)

```csharp
public const string ClaimSetNameNoWhitespaceMessage = "Claim set name must not contain white spaces.";
```

### Validation rule (FluentValidation, added to each affected `Validator` class)

```csharp
RuleFor(m => m.Name)   // or m.ClaimSetName for Application validators
    .Must(name => name == null || !name.Any(char.IsWhiteSpace))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

`char.IsWhiteSpace` rejects all Unicode whitespace (space, tab, newline, non-breaking space, etc.). The `null` guard prevents the rule from double-firing alongside the existing `NotEmpty()` rule.

### Affected validators

| File | Field |
|---|---|
| `Features/ClaimSets/AddClaimSet.cs` | `Name` |
| `Features/ClaimSets/EditClaimSet.cs` | `Name` |
| `Features/ClaimSets/ImportClaimSet.cs` | `Name` |
| `Features/ClaimSets/CopyClaimSet.cs` | `Name` |
| `Features/Applications/AddApplication.cs` | `ClaimSetName` |
| `Features/Applications/EditApplication.cs` | `ClaimSetName` |

## Tests

### Unit tests (`V3.UnitTests`)

One test class per validator, using NUnit + Shouldly + FakeItEasy. Each class covers:
- Valid name (no whitespace) — no error
- Name with space, tab, and newline — error with `ClaimSetNameNoWhitespaceMessage`

### DB integration tests (`V3.DBTests`)

Tests in `ClaimSetEditorTests/` covering the four ClaimSet validators end-to-end against a real SQL Server database.

### Bruno E2E tests

One `.bru` file per endpoint verifying HTTP 400 is returned with the whitespace error message:
- `v3/ClaimSets/POST - ClaimSets - Invalid Name With Whitespace.bru`
- `v3/ClaimSets/PUT - ClaimSets - Invalid Name With Whitespace.bru`
- `v3/ClaimSets/POST - ClaimSets-Copy - Invalid Name With Whitespace.bru`
- `v3/ClaimSets/POST - ClaimSets - Import - Invalid Name With Whitespace.bru`
- `v3/Application/POST - Applications - Invalid ClaimSetName With Whitespace.bru`
- `v3/Application/PUT - Applications - Invalid ClaimSetName With Whitespace.bru`

Existing E2E tests that used spaced names (`"Ed-Fi Sandbox"`, `"Test ClaimSet"`, etc.) were updated to use hyphenated no-space equivalents or to create a claimset dynamically in the pre-request script.
