# ADMINAPI-1448 V2/V3 Unit Test Coverage Gaps

## Purpose

This document tracks the unit tests coverage baselines, skipped areas, uncovered areas, and Jira-ready follow-up candidates discovered while completing ADMINAPI-1448.

## Coverage baseline

| Assembly | Line coverage | Notes |
| --- | ---: | --- |
| EdFi.Ods.AdminApi | 22.2% | V2 assembly |
| EdFi.Ods.AdminApi.V3 | 32.8% | V3 assembly |
| EdFi.Ods.AdminApi.Common | 34.3% | Shared code |
| EdFi.Ods.AdminApi.V1 | 0% | Excluded from ADMINAPI-1448 coverage measurement |

### Final summary

| Batch Run | V2 line coverage | V3 line coverage | Common line coverage | Total line coverage |
| --- | ---: | ---: | ---: | ---: |
| Baseline | 22.2% | 32.8% | 34.3% | 23% |
| 1 | 34.7% | 39.3% | 44.8% | 38.4% |
| 2 | 52.3% | 56.1% | 45% | 53.6% |
| 3 | 54.8% | 57.6% | 45% | 55.4% |
| 4 | 55.6% | 58.1% | 45% | 56.1% |
| 5 | 56.9% | 62.2% | 45% | 58.3% |
| 6 | 57.9% | 63.5% | 45% | 59.3% |

> Command used: `.\build.ps1 -Command UnitTest -Configuration Debug -RunCoverageAnalysis`

## Work batch order

| Batch | Features | Reason |
| --- | --- | --- |
| 1 | Applications, ApiClients, ClaimSets and ResourceClaims | High endpoint value and existing test patterns. High uncovered-line count and validation/mapping complexity |
| 2 | ODS/DataStore features | V2/V3 equivalent feature groups with existing partial tests |
| 3 | Vendors, Actions, AuthorizationStrategies, Information, Jobs, Tenants, Profiles, Connect | Remaining first-sweep feature coverage |
| 4 | Common and infrastructure directly exercised by V2/V3 features | Shared logic needed to support endpoint behavior. |
| 5 | V2/V3 Database.Queries and ClaimSetEditor (EF InMemory sweep) | Largest 0% coverage group; EF InMemory tests, no real DB |
| 6 | Remaining DB.Commands, ClaimSetEditor commands, Feature mappers/handlers | Second EF InMemory + FakeItEasy sweep to reach 60% |

## Skipped or uncovered areas

| Feature/endpoint | API surface | Gap type | Evidence | Reason | Suspected risk | Recommended Jira summary | Recommended acceptance criteria |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ReadApplication.GetApplication` single-record null branch | V2 Applications | Not unit-tested (testability seam missing) | [ReadApplication.cs](../../../Application/EdFi.Ods.AdminApi/Features/Applications/ReadApplication.cs); [GetApplicationByIdQuery.cs](../../../Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetApplicationByIdQuery.cs) | Handler takes concrete `GetApplicationByIdQuery`, and that query throws `NotFoundException<int>` instead of returning null; testing the handler null branch without a database would require a testability seam or behavior decision. | Low/medium: redundant null guard may be unreachable and single-record handler behavior is mostly owned by query. | Review V2 application single-record query/handler testability | Decide whether `GetApplicationByIdQuery` or handler owns not-found behavior, then add unit coverage or remove unreachable branch. |
| `ReadApplication.GetApplication` single-record null branch | V3 Applications | Not unit-tested (testability seam missing) | [ReadApplication.cs](../../../Application/EdFi.Ods.AdminApi.V3/Features/Applications/ReadApplication.cs); [GetApplicationByIdQuery.cs](../../../Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetApplicationByIdQuery.cs) | Handler takes concrete `GetApplicationByIdQuery`, and that query throws `NotFoundException<int>` instead of returning null; testing the handler null branch without a database would require a testability seam or behavior decision. | Low/medium: redundant null guard may be unreachable and single-record handler behavior is mostly owned by query. | Review V3 application single-record query/handler testability | Decide whether `GetApplicationByIdQuery` or handler owns not-found behavior, then add unit coverage or remove unreachable branch. |
| ClaimSets command/query-backed handlers (`Copy`, `Delete`, `Edit`, `Export`, `Read`, resource-claim auth overrides) | V2/V3 ClaimSets | Not unit-tested (testability seam missing) | [ClaimSets V2](../../../Application/EdFi.Ods.AdminApi/Features/ClaimSets); [ClaimSets V3](../../../Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets); coverage report shows high uncovered handler/query lines | Many handlers depend on concrete command/query implementations or EF-backed query behavior. Adding unit tests would require behavior-preserving seams or brittle concrete fakes; Avoided real DB tests per plan. | Medium: endpoint orchestration around not-found/system-reserved/resource override paths remains covered mostly outside unit tests. | Add ClaimSets handler testability seams | Introduce interfaces or thin service abstractions for command/query-backed ClaimSets handlers, then add unit tests for result types and command interactions without a database. |
| `TokenService.Handle` and `RegisterService.Handle` OpenIddict manager interactions | V2 Connect | Partially unit-tested (external dependency) | [TokenService.cs](../../../Application/EdFi.Ods.AdminApi/Features/Connect/TokenService.cs); [RegisterService.cs](../../../Application/EdFi.Ods.AdminApi/Features/Connect/RegisterService.cs). Added [ConnectControllerTests.cs](../../../Application/EdFi.Ods.AdminApi.UnitTests/Features/Connect/ConnectControllerTests.cs) | Covered controller branching and register request validation. Full service behavior depends on `IOpenIddictApplicationManager` application descriptors/secrets/scopes and would require deeper OpenIddict fakes or product-level seams; no V3 Connect source exists. | Medium: client credential and scope failures are security-sensitive. | Add Connect service unit-test seams | Introduce small abstractions or fixtures around OpenIddict application operations, then test invalid grant, missing client, invalid secret, invalid scopes, and successful claims. |
| Profile XML-schema success paths and command-backed handlers | V2/V3 Profiles | Partially unit-tested (schema fixture required) | [Profiles V2](../../../Application/EdFi.Ods.AdminApi/Features/Profiles); [Profiles V3](../../../Application/EdFi.Ods.AdminApi.V3/Features/Profiles). Added V2/V3 profile validator/mapper/read/delete tests | Covered required fields, duplicate names, mapping, read endpoint mapping, and delete command invocation. Add/Edit success paths validate XML against copied XSD and then call DB-backed commands; deeper schema-valid success and command persistence paths were skipped to avoid brittle schema fixtures or real DB tests. | Low/medium: XML profile validation and route/result orchestration remain partly covered by existing DB/integration layers. | Add profile handler/service coverage without real databases | Provide behavior-preserving command/query seams or reusable schema-valid profile fixtures, then add Add/Edit success, XML name mismatch, not-found, and V3 absolute location/no-content tests. |

## Remaining gaps to reach 70% line coverage

Total uncovered coverable lines: ~6,800. Lines needed to reach 70%: ~1,700.

| Group | Assembly | Approx. uncovered lines | Current status | Approach | Priority | Ticket |
| --- | --- | ---: | --- | --- | --- | --- |
| Feature handlers not yet tested (EditApplication, ReadOdsInstance, misc) | V3 | ~400 | Partial | FakeItEasy handler tests | High | [ADMINAPI-1450](https://edfi.atlassian.net/browse/ADMINAPI-1450) |
| `ClaimSets.ReadClaimSets` | V2/V3 | ~60 | Partial | FakeItEasy handler — GetClaimSet (single) path not yet covered | High | [ADMINAPI-1451](https://edfi.atlassian.net/browse/ADMINAPI-1451) |
| `OverrideDefaultAuthorizationStrategyCommand` | V2/V3 | ~350 | 0% covered | EF InMemory SecurityContext + deep resource/action/authstrategy seeding | Medium | [ADMINAPI-1452](https://edfi.atlassian.net/browse/ADMINAPI-1452) |
| `RequestLoggingMiddleware` | V2 | 159 | 0% covered | Fake HttpContext + Serilog sink | Medium | [ADMINAPI-1453](https://edfi.atlassian.net/browse/ADMINAPI-1453) |
| `AddOrEditResourcesOnClaimSetCommand` | V2/V3 | ~120 | 8/0 covered | SecurityContext InMemory with resource claim seeding | Medium | [ADMINAPI-1455](https://edfi.atlassian.net/browse/ADMINAPI-1455) |
| `EditResourceOnClaimSetCommand` | V2/V3 | ~114 | 4/0 covered | SecurityContext InMemory with claimset resource action seeding | Medium | [ADMINAPI-1454](https://edfi.atlassian.net/browse/ADMINAPI-1454) |
| `AuthStrategyResolver` | V2/V3 | ~74 | 0% covered | SecurityContext InMemory | Medium | [ADMINAPI-1456](https://edfi.atlassian.net/browse/ADMINAPI-1456) |
| Infrastructure helpers (extensions, formatting, encryption) | Common | ~300 | Partial | Unit tests with deterministic inputs | Medium | [ADMINAPI-1457](https://edfi.atlassian.net/browse/ADMINAPI-1457) |

### Classes confirmed not worth pursuing for coverage

| Class | Lines | Reason |
| --- | ---: | --- |
| `WebApplicationBuilderExtensions` | 581 | DI/startup registration; cannot be unit-tested without full host |
| `SecurityExtensions` (OpenIddict) | 237 | OpenIddict pipeline setup; integration-only |
| `SwaggerDefaultParameterFilter` V2+V3 | 140 each | Cyclomatic complexity 92, Crap Score 8556; extremely brittle to test |
| `Program.Main` | ~64 | Host startup; not unit-testable |
| `EducationOrganizationService`, `JobStatusService` | ~127+51 | Require Quartz scheduler or real DB; defer to integration tests |

## Discovered issues and risks

Issues observed while writing tests or reading source code during ADMINAPI-1448.

### Confirmed findings

Directly observed or reproduced while writing tests — no further investigation needed to validate the issue exists.

| Issue | Assembly | Class/File | Severity | Description | Recommended action | Ticket |
| --- | --- | --- | --- | --- | --- | --- |
| `OverrideDefaultAuthorizationStrategyCommand` has Crap Score 8556 | V2/V3 | `OverrideDefaultAuthorizationStrategyCommand` | Medium | Cyclomatic complexity 34 and 0% coverage means zero regression protection on a complex auth override path. | Add ClaimSetEditor command tests in next sweep; consider refactoring into smaller methods. | [ADMINAPI-1463](https://edfi.atlassian.net/browse/ADMINAPI-1463) |
| `GetResourcesByClaimSetIdQuery` has Crap Score 1190 on `GetDefaultAuthStrategies` | V2/V3 | `GetResourcesByClaimSetIdQuery` | Medium | `GetDefaultAuthStrategies` has cyclomatic complexity 34 and remains only partially covered. Complex parent/child inheritance logic around inherited auth strategies could silently regress. | Extend existing test coverage to cover the child resource with parent default strategy branch. | [ADMINAPI-1459](https://edfi.atlassian.net/browse/ADMINAPI-1459) |
| `ConnectController` OpenIddict security paths lack unit tests | V2 | `ConnectController`, `TokenService`, `RegisterService` | Medium | Client credential grant, invalid scope, and missing client scenarios are security-sensitive. Currently covered only partially through controller branching tests. | Introduce abstractions around `IOpenIddictApplicationManager` to enable unit-level security path tests. | [ADMINAPI-1462](https://edfi.atlassian.net/browse/ADMINAPI-1462) |
| `ValidateApplicationExistsQuery` has nested complex boolean expression | V2/V3 | `ValidateApplicationExistsQuery.Execute` | Low/Medium | The duplicate detection logic uses deeply nested `&&` / `||` conditions that are hard to reason about. Crap Score 1980. Several branch paths are untested. | Write property-based or parameterized tests covering all combinations of profiles/edOrgs/OdsInstances present vs. absent. | [ADMINAPI-1458](https://edfi.atlassian.net/browse/ADMINAPI-1458) |
| `ProfileValidator` requires XSD at runtime — V3 test project missing schema | V2/V3 | `ProfileValidator.Validate` | Low | V2 tests pass because XSD is present in V2 build output. V3 `UnitTests` project does not copy `Ed-Fi-ODS-API-Profile.xsd` to its output directory, so ProfileValidator tests are marked `[Explicit]` in V3 to avoid false failures. | Add `<Content Include="Schema/Ed-Fi-ODS-API-Profile.xsd">` to V3.UnitTests project, or decouple schema loading into an injectable provider so tests can supply a stub schema path. | [ADMINAPI-1460](https://edfi.atlassian.net/browse/ADMINAPI-1460) |
| `EditResourceOnClaimSetCommand` / `UpdateResourcesOnClaimSetCommand` concrete command chain prevents FakeItEasy faking | V2/V3 | `EditResourceOnClaimSetCommand`, `UpdateResourcesOnClaimSetCommand` | Low | These concrete classes require `ISecurityContext` in their constructors and are not behind interfaces in the handler signatures, so `A.Fake<>` fails with `NotSupportedException`. Handler tests that depend on these commands must instantiate them with InMemory SecurityContext, which requires full ClaimSet+ResourceClaim+Action seeding — significantly increasing test complexity. | Introduce `IEditResourceOnClaimSetCommand` and `IUpdateResourcesOnClaimSetCommand` interfaces to make the handler layer testable without full SecurityContext seeding. | [ADMINAPI-1461](https://edfi.atlassian.net/browse/ADMINAPI-1461) |

### Suspected — needs team review

Potential issues identified by reading source code or coverage data; not yet reproduced or confirmed.

| Issue | Assembly | Class/File | Severity | Description | Recommended action | Ticket |
| --- | --- | --- | --- | --- | --- | --- |
| Redundant null guard may be unreachable | V2/V3 | `GetApplicationByIdQuery` | Low | `GetApplicationByIdQuery.Execute` throws `NotFoundException<int>` before any null guard in the calling handler. The handler null branch is likely dead code. | Confirm and remove unreachable branch, or add test proving it can be reached. | [ADMINAPI-1464](https://edfi.atlassian.net/browse/ADMINAPI-1464) |
| `SqlServerSandboxProvisioner` partially covered | V2 | `SqlServerSandboxProvisioner` | Low | 114 covered / 71 uncovered. The uncovered paths are SQL Server provisioning failure/retry paths. | Add integration test or in-memory provisioner fake for failure paths. | [ADMINAPI-1465](https://edfi.atlassian.net/browse/ADMINAPI-1465) |
