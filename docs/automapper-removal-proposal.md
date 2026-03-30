# AutoMapper Removal Proposal (V2 + V1)

## Objective

Remove AutoMapper from the Admin API solution and replace it with explicit mapping code that is easier to debug, test, and maintain.

## Scope

In scope:

* Remove AutoMapper usage from both V2 and V1 API code paths
* Replace `IMapper` call sites with explicit mapping methods
* Replace AutoMapper converters/resolvers with focused services where DB access is required
* Remove AutoMapper package references after migration completion
* Update affected unit and DB tests

Out of scope:

* Endpoint contract changes
* Business logic changes not required for mapping replacement
* Non-mapping refactors

## Why Replace AutoMapper

Benefits expected:

* Clear, compile-time-visible mapping behavior
* Faster debugging and simpler breakpoints
* Reduced framework magic and implicit configuration drift
* Better control over DB access during mapping

Trade-offs:

* More explicit code to maintain
* Initial migration effort across two code paths (V2 and V1)

## Current-State Summary

The current solution has broad AutoMapper usage:

* DI registration in `WebApplicationBuilderExtensions`
* Mapping profiles in V2 and V1
* 50+ map invocations across feature handlers/services
* Custom converter/resolver logic
* DB tests creating `MapperConfiguration` directly

High-risk areas:

* Claim set/resource nested mapping flows
* Converter logic that currently queries data stores during map operations
* Dual maintenance for V2 and V1 parity

## Target Architecture

### 1. Static Mapper Classes for Pure Transformations

Use feature-scoped static classes for deterministic source-to-target mapping.

Example folder layout (V2):

* `Features/Vendors/Mappers/VendorMapper.cs`
* `Features/Applications/Mappers/ApplicationMapper.cs`
* `Features/ClaimSets/Mappers/ClaimSetMapper.cs`
* `Features/ApiClients/Mappers/ApiClientMapper.cs`

Mirror the same pattern in V1.

Guidelines:

* Keep methods side-effect free
* No DB queries in static mappers
* Use explicit null-safe handling only where type contracts allow nullable values
* Prefer list projection helpers for collections

Example shape:

```csharp
internal static class VendorMapper
{
    internal static VendorModel ToModel(Admin.DataAccess.Models.Vendor source)
    {
        return new VendorModel
        {
            Id = source.VendorId,
            VendorName = source.VendorName,
            NamespacePrefixes = source.NamespacePrefixes
        };
    }

    internal static List<VendorModel> ToModelList(IEnumerable<Admin.DataAccess.Models.Vendor> source)
    {
        return source.Select(ToModel).ToList();
    }
}
```

### 2. Focused Services for DB-Backed Mapping Fields

Some current conversions require DB/context lookups. Keep these out of static mappers.

Pattern:

* Introduce small query services that return derived values needed for response models
* Call these services in handlers/services before or during explicit mapping

Examples:

* Authorization strategy name-to-id resolution
* ODS instance ID lookups for applications/api clients

Example interface shape:

```csharp
public interface IAuthorizationStrategyIdResolver
{
    Task<List<int>> ResolveIdsAsync(List<string> strategyNames, CancellationToken cancellationToken);
}
```

### 3. Endpoint/Service Mapping Flow

Replace:

* `mapper.Map<TDestination>(source)`

With:

* `FeatureMapper.ToModel(source)` for pure maps
* Service-assisted composition for fields that need lookups

Keep mapping invocation close to handler/service return boundaries to preserve readability.

## Dependency Injection Changes

### Remove

* `AddAutoMapper(...)` registrations

* `IMapper` dependencies from endpoint handlers/services

### Add

* Resolver/query services needed for DB-backed mapped fields

* No DI registration needed for static mappers

## Migration Plan

### Phase 0: Baseline and Guardrails

* Capture baseline test run and API smoke checks

* Document key payload snapshots for high-risk endpoints

### Phase 1: V2 Simple Mappings

* Migrate straightforward feature maps first (Vendors, Actions, Profiles, basic Applications reads)

* Remove `IMapper` from those handler signatures

### Phase 2: V2 Complex/Nested Mappings

* Migrate ClaimSets, ResourceClaims, OdsInstanceContext, OdsInstanceDerivative

* Replace resolver/converter behavior with explicit code + focused services

### Phase 3: V1 Parity Migration

* Apply the same pattern to V1 features

* Keep V1 and V2 behavior aligned

### Phase 4: Tests and Cleanup

* Replace test helper usage of `MapperConfiguration`

* Update unit tests currently mocking `IMapper`
* Remove AutoMapper package references and dead files

## Test Strategy

Unit tests:

* Mapper class tests per feature for pure transformation logic
* Handler/service tests verifying resolver/service collaboration

DB tests:

* Focus on claim set/resource hierarchy behavior
* Verify authorization strategy and ODS instance field parity

Regression checks:

* API smoke tests for read/add/edit/reset flows in Applications, ApiClients, Vendors, ClaimSets

Build/test command:

* `./build.ps1 UnitTest`

## Risk Management

Primary risks:

* Subtle response-shape regressions in nested models
* Behavior drift between V2 and V1
* Hidden coupling in converter logic that previously ran implicitly

Mitigations:

* Feature-by-feature rollout with tests at each step
* Snapshot comparison for selected JSON payloads
* Keep each PR focused to one feature slice where possible

## Estimated Effort

Rough effort (single stream):

* Analysis and scaffolding: 1-2 days
* V2 migration: 4-7 days
* V1 migration: 2-4 days
* Test updates and package cleanup: 2-3 days

Total: approximately 2-3 weeks depending on parallelization and review cycles.

## Proposed File-Level Change Areas

Primary:

* `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs`
* `Application/EdFi.Ods.AdminApi/Infrastructure/Services/ClaimSetEditor/GetResourcesByClaimSetIdQueryService.cs`
* `Application/EdFi.Ods.AdminApi/Features/**`
* `Application/EdFi.Ods.AdminApi.V1/Features/**`
* `Application/EdFi.Ods.AdminApi/Infrastructure/AutoMapper/**` (remove after migration)
* `Application/EdFi.Ods.AdminApi.V1/Infrastructure/AutoMapper/**` (remove after migration)
* `Application/EdFi.Ods.AdminApi.DBTests/SecurityDataTestBase.cs`
* `Application/EdFi.Ods.AdminApi.V1.DBTests/SecurityDataTestBase.cs`
* `Application/Directory.Packages.props`
* `Application/EdFi.Ods.AdminApi.V1/EdFi.Ods.AdminApi.V1.csproj`

## Acceptance Criteria

The migration is complete when:

* No `AutoMapper` package references remain
* No `IMapper`, `Profile`, `CreateMap`, or `Map<` usage remains
* All tests pass with `./build.ps1 UnitTest`
* Key endpoint payloads match expected contract behavior in V2 and V1
* Converter/resolver behaviors are covered by explicit tests

## Recommendation

Proceed with a phased implementation starting with V2 simple mappings to validate patterns and test strategy, then handle complex V2 areas, then V1 parity, and finally package cleanup.
