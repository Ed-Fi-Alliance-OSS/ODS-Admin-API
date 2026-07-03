# ADMINAPI-1448 V2/V3 Unit Test Coverage Gaps

## Purpose

This document tracks the endpoint/feature inventory, coverage baseline, skipped areas, uncovered areas, and Jira-ready follow-up candidates discovered while completing ADMINAPI-1448.

## Evidence rules

Each inventory or gap item must cite one of:

- Source file path
- Existing or added test file path
- Coverage report entry
- Local command output

Do not add inferred endpoint behavior or guessed defects.

## Coverage baseline

### Downloaded PR artifact before V1 exclusion

Source: `artifacts\Coverage Report\index.html`

| Assembly | Line coverage | Branch coverage | Notes |
| --- | ---: | ---: | --- |
| EdFi.Ods.AdminApi | 22.2% | 20.8% | V2/current API assembly |
| EdFi.Ods.AdminApi.V3 | 32.8% | 28.4% | V3 assembly |
| EdFi.Ods.AdminApi.Common | 34.3% | 24% | Shared code |
| EdFi.Ods.AdminApi.V1 | 0% | 0.2% | Must be excluded from ADMINAPI-1448 coverage measurement |

### After V1 exclusion

| Date | Command | V2 line coverage | V3 line coverage | Common line coverage | Total line coverage | Notes |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 2026-07-01 | `.\build.ps1 -Command UnitTest -Configuration Debug -RunCoverageAnalysis` | 34.7% | 39.3% | 44.8% | 38.4% | V1 excluded; all unit tests passed and `coveragereport\index.html` generated |
| 2026-07-03 (after Batches 1+2+3a) | `.\build.ps1 -Command UnitTest -Configuration Debug -RunCoverageAnalysis` | 52.3% | 56.1% | 45% | 53.6% | ADMINAPI-1448b Batches 1-3a: remaining DB.Commands (V2+V3), ClaimSetEditor commands, handler tests; V2: 501 passed, 10 skipped; V3: 474 passed; V1 excluded |
| 2026-07-03 (after Batches 3b-3d) | `.\build.ps1 -Command UnitTest -Configuration Debug -RunCoverageAnalysis` | 54.8% | 57.6% | 45% | 55.4% | Handler tests: AddApplication/EditApplication/CopyClaimSet/DeleteClaimSet V2, AddDataStore/DeleteDataStore V3, ReadApplication V3; V2: 514 passed, 10 skipped; V3: 484 passed; V1 excluded |
| 2026-07-03 (after Batch 3e) | `.\build.ps1 -Command UnitTest -Configuration Debug -RunCoverageAnalysis` | 55.6% | 58.1% | 45% | 56.1% | Handler tests: EditResourceClaimActions validator, ProfileValidator V2, EditDataStore V3; V2: 529 passed, 10 skipped; V3: 486 passed; V1 excluded |

## Feature inventory

| API surface | Feature | Source path | Endpoint/behavior files reviewed | Existing tests reviewed | Status |
| --- | --- | --- | --- | --- | --- |
| V2 | Actions | `Application\EdFi.Ods.AdminApi\Features\Actions` | Reviewed: `ActionMapper.cs`, `ActionModel.cs`, `ReadActions.cs` | Reviewed existing: `ActionModelTests.cs`, `ReadActionsTests.cs` | Task 8 reviewed existing coverage; no new tests added |
| V2 | ApiClients | `Application\EdFi.Ods.AdminApi\Features\ApiClients` | Reviewed: `AddApiClient.cs`, `ApiClientMapper.cs`, `ApiClientModel.cs`, `DeleteApiClient.cs`, `EditApiClient.cs`, `ReadApiClient.cs`, `ResetApiClientCredentials.cs` | Reviewed/updated: `AddApiClientOdsInstanceIdsValidationTests.cs`, `AddApiClientValidatorTests.cs`, `ApiClientModelTests.cs`, `DeleteApiClientTests.cs`, `EditApiClientOdsInstanceIdsValidationTests.cs`, `EditApiClientValidatorTests.cs`, `ReadApiClientTests.cs`, `ResetApiClientCredentialsTests.cs` | Task 5 updated validator null-IDs coverage |
| V2 | Applications | `Application\EdFi.Ods.AdminApi\Features\Applications` | Reviewed: `AddApplication.cs`, `ApplicationMapper.cs`, `ApplicationModel.cs`, `DeleteApplication.cs`, `EditApplication.cs`, `ReadApplication.cs`, `ReadApplicationsByOdsInstance.cs`, `ReadApplicationsByVendor.cs`, `ResetApplicationCredentials.cs` | Reviewed/updated: `ApplicationMapperTests.cs`, `AddApplicationValidatorTests.cs`, `EditApplicationValidatorTests.cs`, `DeleteApplicationTests.cs`, `ResetApplicationCredentialsTests.cs` | Task 5 added validator/delete/reset coverage |
| V2 | AuthorizationStrategies | `Application\EdFi.Ods.AdminApi\Features\AuthorizationStrategies` | Reviewed: `AuthorizationStrategyMapper.cs`, `AuthorizationStrategyModel.cs`, `ReadAuthorizationStrategy.cs` | Added: `AuthorizationStrategyMapperTests.cs`, `ReadAuthorizationStrategyTests.cs` | Task 8 added existing-behavior mapper/read endpoint coverage |
| V2 | ClaimSets | `Application\EdFi.Ods.AdminApi\Features\ClaimSets` | Reviewed: `AddClaimSet.cs`, `ClaimSetMapper.cs`, `ClaimSetModel.cs`, `CopyClaimSet.cs`, `DeleteClaimSet.cs`, `EditClaimSet.cs`, `ExportClaimSet.cs`, `ImportClaimSet.cs`, `ReadClaimSets.cs`, `ResourceClaimValidator.cs`, `ResourceClaims\DeleteResourceClaim.cs`, `ResourceClaims\EditAuthStrategy.cs`, `ResourceClaims\EditResourceClaimActions.cs` | Added/reviewed: `AddClaimSetValidatorTests.cs`, `ClaimSetMapperTests.cs`, `ImportClaimSetValidatorTests.cs`; no pre-existing V2 ClaimSets test folder | Task 6 added existing-behavior validator/mapper coverage; handler/DB-heavy paths documented as skipped |
| V2 | Connect | `Application\EdFi.Ods.AdminApi\Features\Connect` | Reviewed: `ConnectController.cs`, `RegisterService.cs`, `TokenService.cs` | Added: `ConnectControllerTests.cs` | Task 8 added existing-behavior controller and register validator coverage; OpenIddict service paths documented as skipped |
| V2 | DbInstances | `Application\EdFi.Ods.AdminApi\Features\DbInstances` | Reviewed: `AddDbInstance.cs`, `DbInstanceDatabaseNameFormatter.cs`, `DbInstanceMapper.cs`, `DbInstanceModel.cs`, `DeleteDbInstance.cs`, `ReadDbInstance.cs` | Reviewed existing: `AddDbInstanceTests.cs`, `DeleteDbInstanceTests.cs`, `ReadDbInstanceTests.cs` | Task 7 reviewed existing coverage; no new tests added |
| V2 | Information | `Application\EdFi.Ods.AdminApi\Features\Information` | Reviewed: `InformationModel.cs`, `ReadInformation.cs` | Reviewed existing: `ReadInformationTest.cs` | Task 8 reviewed existing V1/V2/V3 mode and tenancy coverage; no new tests added |
| V2 | Jobs | `Application\EdFi.Ods.AdminApi\Features\Jobs` | Reviewed: `GetJobStatus.cs` | Reviewed existing: `GetJobStatusTests.cs` | Task 8 reviewed existing status/not-found/error coverage; no new tests added |
| V2 | OdsInstanceContext | `Application\EdFi.Ods.AdminApi\Features\OdsInstanceContext` | Reviewed: `AddOdsInstanceContext.cs`, `EditOdsInstanceContext.cs`, `DeleteOdsInstanceContext.cs`, `ReadOdsInstanceContext.cs`, `OdsInstanceContextMapper.cs`, `OdsInstanceContextModel.cs` | Added: `OdsInstanceContextTests.cs` | Task 7 added validator and mapper coverage; targeted V2 ODS tests passed |
| V2 | OdsInstanceDerivative | `Application\EdFi.Ods.AdminApi\Features\OdsInstanceDerivative` | Reviewed: `AddOdsInstanceDerivative.cs`, `EditOdsInstanceDerivative.cs`, `DeleteOdsInstanceDerivative.cs`, `ReadOdsInstanceDerivative.cs`, `OdsInstanceDerivativeMapper.cs`, `OdsInstanceDerivativeModel.cs` | Added: `OdsInstanceDerivativeTests.cs` | Task 7 added validator and mapper coverage; targeted V2 ODS tests passed |
| V2 | OdsInstances | `Application\EdFi.Ods.AdminApi\Features\OdsInstances` | Reviewed: `AddOdsInstance.cs`, `EditOdsInstance.cs`, `DeleteOdsInstance.cs`, `ReadOdsInstance.cs`, `OdsInstanceMapper.cs`, `OdsInstanceModel.cs`, `RefreshEducationOrganizations.cs`, `ReadEducationOrganizations.cs` | Reviewed/updated: `OdsInstanceMapperTests.cs`, `RefreshEducationOrganizationsTests.cs`, `RefreshAndJobStatusE2ETests.cs` | Task 7 added mapper coverage; targeted V2 ODS tests passed |
| V2 | Profiles | `Application\EdFi.Ods.AdminApi\Features\Profiles` | Reviewed: `AddProfile.cs`, `DeleteProfile.cs`, `EditProfile.cs`, `ProfileMapper.cs`, `ProfileModel.cs`, `ProfileValidator.cs`, `ReadProfile.cs` | Added: `AddProfileValidatorTests.cs`, `DeleteProfileTests.cs`, `EditProfileValidatorTests.cs`, `ProfileMapperTests.cs`, `ReadProfileTests.cs` | Task 8 added existing-behavior validator/mapper/read/delete coverage; XML-schema/DB-heavy command paths documented as skipped |
| V2 | ResourceClaimActionAuthStrategies | `Application\EdFi.Ods.AdminApi\Features\ResourceClaimActionAuthStrategies` | Reviewed: `ReadResourceClaimActionAuthStrategies.cs`, `ResourceClaimActionAuthStrategyModel.cs` | No tests added in Task 6 | Skipped: read endpoint delegates to concrete DB query; model-only coverage deferred |
| V2 | ResourceClaimActions | `Application\EdFi.Ods.AdminApi\Features\ResourceClaimActions` | Reviewed: `ReadResourceClaimActions.cs`, `ResourceClaimActionModel.cs` | No tests added in Task 6 | Skipped: read endpoint delegates to concrete DB query; model-only coverage deferred |
| V2 | ResourceClaims | `Application\EdFi.Ods.AdminApi\Features\ResourceClaims` | Reviewed: `ReadResourceClaims.cs`, `ResourceClaimMapper.cs` | Added: `ResourceClaimMapperTests.cs` | Task 6 added mapper coverage; read endpoint DB-query path skipped |
| V2 | Tenants | `Application\EdFi.Ods.AdminApi\Features\Tenants` | Reviewed: `ReadTenants.cs`, `TenantDetailModel.cs`, `TenantMapper.cs`, `TenantModel.cs` | Reviewed existing: `ReadTenantsTest.cs`, `TenantDetailModelTests.cs`, `TenantModelTests.cs` | Task 8 reviewed existing tenant header/model coverage; no new tests added |
| V2 | Vendors | `Application\EdFi.Ods.AdminApi\Features\Vendors` | Reviewed: `AddVendor.cs`, `DeleteVendor.cs`, `EditVendor.cs`, `ReadVendor.cs`, `VendorMapper.cs`, `VendorModel.cs` | Reviewed existing: `AddVendorTests.cs`, `AddVendorValidatorTests.cs`, `DeleteVendorTests.cs`, `EditVendorTests.cs`, `EditVendorValidatorTests.cs`, `ReadVendorTests.cs`, `VendorFeatureEndpointTests.cs`, `VendorMapperTests.cs`, `VendorModelTests.cs` | Task 8 reviewed existing coverage; no new tests added |
| V3 | ApiClients | `Application\EdFi.Ods.AdminApi.V3\Features\ApiClients` | Reviewed: `AddApiClient.cs`, `ApiClientMapper.cs`, `ApiClientModel.cs`, `DeleteApiClient.cs`, `EditApiClient.cs`, `ReadApiClient.cs`, `ResetApiClientCredentials.cs` | Reviewed/updated: `AddApiClientOdsInstanceIdsValidationTests.cs`, `AddApiClientValidatorTests.cs`, `ApiClientModelTests.cs`, `DeleteApiClientTests.cs`, `EditApiClientOdsInstanceIdsValidationTests.cs`, `EditApiClientTests.cs`, `EditApiClientValidatorTests.cs`, `ReadApiClientTests.cs`, `ResetApiClientCredentialsTests.cs` | Task 5 updated validator null-IDs coverage |
| V3 | Applications | `Application\EdFi.Ods.AdminApi.V3\Features\Applications` | Reviewed: `AddApplication.cs`, `ApplicationMapper.cs`, `ApplicationModel.cs`, `DeleteApplication.cs`, `EditApplication.cs`, `ReadApplication.cs`, `ReadApplicationsByDataStore.cs`, `ReadApplicationsByVendor.cs`, `ResetApplicationCredentials.cs` | Reviewed/updated: `AddApplicationValidatorTests.cs`, `ApplicationMapperTests.cs`, `EditApplicationTests.cs`, `EditApplicationValidatorTests.cs`, `DeleteApplicationTests.cs`, `ResetApplicationCredentialsTests.cs` | Task 5 added delete/reset coverage; existing validator tests reviewed |
| V3 | ClaimSets | `Application\EdFi.Ods.AdminApi.V3\Features\ClaimSets` | Reviewed: `AddClaimSet.cs`, `ClaimSetMapper.cs`, `ClaimSetModel.cs`, `CopyClaimSet.cs`, `DeleteClaimSet.cs`, `EditClaimSet.cs`, `ExportClaimSet.cs`, `ImportClaimSet.cs`, `ReadClaimSets.cs`, `ResourceClaimValidator.cs`, `ResourceClaims\DeleteResourceClaim.cs`, `ResourceClaims\EditAuthStrategy.cs`, `ResourceClaims\EditResourceClaimActions.cs` | Reviewed/updated: `AddClaimSetValidatorTests.cs`, `CopyClaimSetValidatorTests.cs`, `EditClaimSetTests.cs`, `EditClaimSetValidatorTests.cs`, `ImportClaimSetValidatorTests.cs`, `ResourceClaims\EditResourceClaimActionsTests.cs`, added `ClaimSetMapperTests.cs` | Task 6 added mapper coverage and reused existing validator/route-id mismatch coverage; handler/DB-heavy paths documented as skipped |
| V3 | DataStoreContexts | `Application\EdFi.Ods.AdminApi.V3\Features\DataStoreContexts` | Reviewed: `AddDataStoreContext.cs`, `EditDataStoreContext.cs`, `DeleteDataStoreContext.cs`, `ReadDataStoreContext.cs`, `DataStoreContextMapper.cs`, `DataStoreContextModel.cs` | Added: `DataStoreContextTests.cs` | Task 7 added validator and mapper coverage; targeted V3 DataStores tests passed |
| V3 | DataStoreDerivatives | `Application\EdFi.Ods.AdminApi.V3\Features\DataStoreDerivatives` | Reviewed: `AddDataStoreDerivative.cs`, `EditDataStoreDerivative.cs`, `DeleteDataStoreDerivative.cs`, `ReadDataStoreDerivative.cs`, `DataStoreDerivativeMapper.cs`, `DataStoreDerivativeModel.cs` | Added: `DataStoreDerivativeTests.cs` | Task 7 added validator and mapper coverage; targeted V3 DataStores tests passed |
| V3 | DataStores | `Application\EdFi.Ods.AdminApi.V3\Features\DataStores` | Reviewed: `AddDataStore.cs`, `EditDataStore.cs`, `DeleteDataStore.cs`, `ReadDataStore.cs`, `DataStoreMapper.cs`, `DataStoreModel.cs`, `RefreshEducationOrganizations.cs`, `ReadEducationOrganizations.cs` | Reviewed/updated: `AddDataStoreValidatorTests.cs`, `DataStoreMapperTests.cs`, `EditDataStoreTests.cs` | Task 7 added validator and mapper coverage; targeted V3 DataStores tests passed |
| V3 | DbDataStores | `Application\EdFi.Ods.AdminApi.V3\Features\DbDataStores` | Reviewed: `AddDbDataStore.cs`, `DbDataStoreDatabaseNameFormatter.cs`, `DbDataStoreMapper.cs`, `DbDataStoreModel.cs`, `DeleteDbDataStore.cs`, `ReadDbDataStore.cs` | Reviewed existing: `AddDbDataStoreTests.cs`, `DeleteDbDataStoreTests.cs`, `ReadDbDataStoreTests.cs` | Task 7 reviewed existing coverage; no new tests added |
| V3 | Actions | `Application\EdFi.Ods.AdminApi.V3\Features\Actions` | Reviewed: `ActionMapper.cs`, `ActionModel.cs`, `ReadActions.cs` | Reviewed existing: `ActionModelTests.cs`, `ReadActionsTests.cs` | Task 8 reviewed existing coverage; no new tests added |
| V3 | AuthorizationStrategies | `Application\EdFi.Ods.AdminApi.V3\Features\AuthorizationStrategies` | Reviewed: `AuthorizationStrategyMapper.cs`, `AuthorizationStrategyModel.cs`, `ReadAuthorizationStrategy.cs` | Added: `AuthorizationStrategyMapperTests.cs`, `ReadAuthorizationStrategyTests.cs` | Task 8 added existing-behavior mapper/read endpoint coverage |
| V3 | Connect | No V3 feature source folder found under `Application\EdFi.Ods.AdminApi.V3\Features` | Inventory command found no V3 Connect `.cs` files | Created empty folder `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\Connect`; no source to test | Task 8 documented as not applicable for V3 source |
| V3 | Information | No V3 feature source folder found under `Application\EdFi.Ods.AdminApi.V3\Features`; V2 `ReadInformation.cs` covers V3 mode | Reviewed V2 `ReadInformation.cs` V3 mode behavior | Created empty folder `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\Information`; V2 `ReadInformationTest.cs` already asserts V3 mode and V3 tenancy service | Task 8 documented as covered through V2 Information tests/no V3 source |
| V3 | Jobs | `Application\EdFi.Ods.AdminApi.V3\Features\Jobs` | Reviewed: `GetJobStatus.cs` | Reviewed existing: `GetJobStatusTests.cs` | Task 8 reviewed existing status/not-found/error coverage; no new tests added |
| V3 | Profiles | `Application\EdFi.Ods.AdminApi.V3\Features\Profiles` | Reviewed: `AddProfile.cs`, `DeleteProfile.cs`, `EditProfile.cs`, `ProfileMapper.cs`, `ProfileModel.cs`, `ProfileValidator.cs`, `ReadProfile.cs` | Reviewed/added: existing `EditProfileTests.cs`; added `AddProfileValidatorTests.cs`, `DeleteProfileTests.cs`, `EditProfileValidatorTests.cs`, `ProfileMapperTests.cs`, `ReadProfileTests.cs` | Task 8 added existing-behavior validator/mapper/read/delete coverage; XML-schema/DB-heavy command paths documented as skipped |
| V3 | Tenants | `Application\EdFi.Ods.AdminApi.V3\Features\Tenants` | Reviewed: `ReadTenants.cs`, `TenantDetailModel.cs`, `TenantMapper.cs`, `TenantModel.cs` | Reviewed existing: `ReadTenantsTest.cs`, `TenantDetailModelTests.cs`, `TenantModelTests.cs` | Task 8 reviewed existing tenant/model coverage; no new tests added |
| V3 | Vendors | `Application\EdFi.Ods.AdminApi.V3\Features\Vendors` | Reviewed: `AddVendor.cs`, `DeleteVendor.cs`, `EditVendor.cs`, `ReadVendor.cs`, `VendorMapper.cs`, `VendorModel.cs` | Reviewed existing: `AddVendorTests.cs`, `AddVendorValidatorTests.cs`, `DeleteVendorTests.cs`, `EditVendorTests.cs`, `EditVendorValidatorTests.cs`, `ReadVendorTests.cs`, `VendorFeatureEndpointTests.cs`, `VendorMapperTests.cs`, `VendorModelTests.cs` | Task 8 reviewed existing coverage; no new tests added |

## Skipped or uncovered areas

| Feature/endpoint | API surface | Gap type | Evidence | Reason | Suspected risk | Recommended Jira summary | Recommended acceptance criteria |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ReadApplication.GetApplication` single-record null branch | V2 Applications | Not unit-tested in Task 5 | `Application\EdFi.Ods.AdminApi\Features\Applications\ReadApplication.cs`; `Application\EdFi.Ods.AdminApi\Infrastructure\Database\Queries\GetApplicationByIdQuery.cs`; `docs\Coverage Report.zip` entry `EdFi.Ods.AdminApi_GetApplicationByIdQuery.html` shows EF-query hotspots | Handler takes concrete `GetApplicationByIdQuery`, and that query throws `NotFoundException<int>` instead of returning null; testing the handler null branch without a database would require a testability seam or behavior decision. | Low/medium: redundant null guard may be unreachable and single-record handler behavior is mostly owned by query. | Review V2 application single-record query/handler testability | Decide whether `GetApplicationByIdQuery` or handler owns not-found behavior, then add unit coverage or remove unreachable branch. |
| `ReadApplication.GetApplication` single-record null branch | V3 Applications | Not unit-tested in Task 5 | `Application\EdFi.Ods.AdminApi.V3\Features\Applications\ReadApplication.cs`; `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Database\Queries\GetApplicationByIdQuery.cs`; `docs\Coverage Report.zip` entry `EdFi.Ods.AdminApi.V3_GetApplicationByIdQuery.html` shows EF-query hotspots | Handler takes concrete `GetApplicationByIdQuery`, and that query throws `NotFoundException<int>` instead of returning null; testing the handler null branch without a database would require a testability seam or behavior decision. | Low/medium: redundant null guard may be unreachable and single-record handler behavior is mostly owned by query. | Review V3 application single-record query/handler testability | Decide whether `GetApplicationByIdQuery` or handler owns not-found behavior, then add unit coverage or remove unreachable branch. |
| ClaimSets command/query-backed handlers (`Copy`, `Delete`, `Edit`, `Export`, `Read`, resource-claim auth overrides) | V2/V3 ClaimSets | Not unit-tested in Task 6 | `Application\EdFi.Ods.AdminApi\Features\ClaimSets`; `Application\EdFi.Ods.AdminApi.V3\Features\ClaimSets`; coverage report shows high uncovered handler/query lines | Many handlers depend on concrete command/query implementations or EF-backed query behavior. Adding unit tests would require behavior-preserving seams or brittle concrete fakes; Task 6 avoided real DB tests per plan. | Medium: endpoint orchestration around not-found/system-reserved/resource override paths remains covered mostly outside unit tests. | Add ClaimSets handler testability seams | Introduce interfaces or thin service abstractions for command/query-backed ClaimSets handlers, then add unit tests for result types and command interactions without a database. |
| `TokenService.Handle` and `RegisterService.Handle` OpenIddict manager interactions | V2 Connect | Partially unit-tested in Task 8 | `Application\EdFi.Ods.AdminApi\Features\Connect\TokenService.cs`; `Application\EdFi.Ods.AdminApi\Features\Connect\RegisterService.cs`; added `Application\EdFi.Ods.AdminApi.UnitTests\Features\Connect\ConnectControllerTests.cs` | Task 8 covered controller branching and register request validation. Full service behavior depends on `IOpenIddictApplicationManager` application descriptors/secrets/scopes and would require deeper OpenIddict fakes or product-level seams; no V3 Connect source exists. | Medium: client credential and scope failures are security-sensitive. | Add Connect service unit-test seams | Introduce small abstractions or fixtures around OpenIddict application operations, then test invalid grant, missing client, invalid secret, invalid scopes, and successful claims. |
| Profile XML-schema success paths and command-backed handlers | V2/V3 Profiles | Partially unit-tested in Task 8 | `Application\EdFi.Ods.AdminApi\Features\Profiles`; `Application\EdFi.Ods.AdminApi.V3\Features\Profiles`; added V2/V3 profile validator/mapper/read/delete tests | Task 8 covered required fields, duplicate names, mapping, read endpoint mapping, and delete command invocation. Add/Edit success paths validate XML against copied XSD and then call DB-backed commands; deeper schema-valid success and command persistence paths were skipped to avoid brittle schema fixtures or real DB tests. | Low/medium: XML profile validation and route/result orchestration remain partly covered by existing DB/integration layers. | Add profile handler/service coverage without real databases | Provide behavior-preserving command/query seams or reusable schema-valid profile fixtures, then add Add/Edit success, XML name mismatch, not-found, and V3 absolute location/no-content tests. |

## Final summary

| Metric | Before V1 exclusion | After V1 exclusion | After ADMINAPI-1448b |
| --- | ---: | ---: | ---: |
| Total line coverage | 23% | 38.4% | 49.6% |
| Total branch coverage | 19.9% | 32.8% | 38.4% |
| V2 line coverage | 22.2% | 34.7% | 47.1% |
| V3 line coverage | 32.8% | 39.3% | 52.4% |

## Remaining gaps to reach 70% line coverage

Analysis as of 2026-07-03 after ADMINAPI-1448b. Total uncovered coverable lines: 7,683. Lines needed to reach 70%: ~2,969.

| Group | Uncovered lines | Current % | Approach | Estimated lift | Priority |
| --- | ---: | ---: | --- | ---: | --- |
| Features (handlers/mappers/models) | 2,910 | 49% | FakeItEasy mock handlers + AutoMapper profile tests | +10-12pp | High |
| Infrastructure (extensions, helpers, middleware) | 1,784 | 37% | Mixed; some testable with fakes, some not | +5-7pp | Medium |
| ClaimSetEditor commands | 1,295 | 25% | EF InMemory SqlServerSecurityContext | +8pp | High |
| Database.Commands (remaining) | 791 | 40% | EF InMemory SqlServerUsersContext | +5pp | High |
| Services | 598 | 70% | Near threshold already | +1-2pp | Low |
| Database.Queries | 199 | 89% | Near saturation | +1pp | Done |

### Classes not worth pursuing for coverage

| Class | Lines | Reason |
| --- | ---: | --- |
| `WebApplicationBuilderExtensions` | 581 | DI/startup registration; cannot be unit-tested without full host |
| `SecurityExtensions` (OpenIddict) | 237 | OpenIddict pipeline setup; integration-only |
| `SwaggerDefaultParameterFilter` V2+V3 | 140 each | Cyclomatic complexity 92, Crap Score 8556; extremely brittle to test |
| `Program.Main` | ~64 | Host startup; not unit-testable |
| `EducationOrganizationService`, `JobStatusService` | ~127+51 | Require Quartz scheduler or real DB; defer to integration tests |

## Discovered issues and risks

Issues observed while writing tests or reading source code during ADMINAPI-1448/b. Not yet confirmed as bugs — each needs team review.

| Issue | Assembly | Class/File | Severity | Description | Recommended action |
| --- | --- | --- | --- | --- | --- |
| Redundant null guard may be unreachable | V2/V3 | `GetApplicationByIdQuery` | Low | `GetApplicationByIdQuery.Execute` throws `NotFoundException<int>` before any null guard in the calling handler. The handler null branch is likely dead code. | Confirm and remove unreachable branch, or add test proving it can be reached. |
| `OverrideDefaultAuthorizationStrategyCommand` has Crap Score 8556 | V2/V3 | `OverrideDefaultAuthorizationStrategyCommand` | Medium | Cyclomatic complexity 34 and 0% coverage means zero regression protection on a complex auth override path. | Add ClaimSetEditor command tests in next sweep; consider refactoring into smaller methods. |
| `GetResourcesByClaimSetIdQuery` has Crap Score 1190 on `GetDefaultAuthStrategies` | V2/V3 | `GetResourcesByClaimSetIdQuery` | Medium | `GetDefaultAuthStrategies` has cyclomatic complexity 34 and remains only partially covered. Complex parent/child inheritance logic around inherited auth strategies could silently regress. | Extend existing test coverage to cover the child resource with parent default strategy branch. |
| `ValidateApplicationExistsQuery` has nested complex boolean expression | V2/V3 | `ValidateApplicationExistsQuery.Execute` | Low/Medium | The duplicate detection logic uses deeply nested `&&` / `\|\|` conditions that are hard to reason about. Crap Score 1980. Several branch paths are untested. | Write property-based or parameterized tests covering all combinations of profiles/edOrgs/OdsInstances present vs. absent. |
| `ConnectController` OpenIddict security paths lack unit tests | V2 | `ConnectController`, `TokenService`, `RegisterService` | Medium | Client credential grant, invalid scope, and missing client scenarios are security-sensitive. Currently covered only partially through controller branching tests. | Introduce abstractions around `IOpenIddictApplicationManager` to enable unit-level security path tests. |
| `SqlServerSandboxProvisioner` partially covered | V2 | `SqlServerSandboxProvisioner` | Low | 114 covered / 71 uncovered. The uncovered paths are SQL Server provisioning failure/retry paths. | Add integration test or in-memory provisioner fake for failure paths. |

## Work batch order

| Batch | Features | Reason | Status |
| --- | --- | --- | --- |
| 1 | Applications, ApiClients | High endpoint value and existing test patterns | Task 5 implemented targeted V2/V3 unit coverage |
| 2 | ClaimSets and ResourceClaims | High uncovered-line count and validation/mapping complexity | Task 6 added targeted V2/V3 validator and mapper unit coverage; DB-heavy handler paths documented as skipped |
| 3 | ODS/DataStore features | V2/V3 equivalent feature groups with existing partial tests | Task 7 added/reviewed targeted ODS/DataStore unit coverage; V2 ODS targeted tests passed (63 passed, 10 skipped, 0 failed, 73 total) and V3 DataStores targeted tests passed (56 passed, 0 failed, 56 total) |
| 4 | Vendors, Actions, AuthorizationStrategies, Information, Jobs, Tenants, Profiles, Connect | Remaining first-sweep feature coverage | Task 8 added/reviewed targeted V2/V3 unit coverage; targeted V2 remaining tests passed and targeted V3 remaining tests passed |
| 5 | Common and infrastructure directly exercised by V2/V3 features | Shared logic needed to support endpoint behavior | Task 9 added existing-behavior coverage for shared tenant resolution (`TenantResolverMiddleware`), V2/V3 claim-set editor enumeration/action helpers, and V3 token-endpoint error middleware preservation paths. Targeted tests passed: Common 59/59, V2 infrastructure 137/137, V3 infrastructure 147/147. |
| 6 | V2/V3 Database.Queries and ClaimSetEditor (EF InMemory sweep) | Largest 0% coverage group; EF InMemory tests, no real DB | ADMINAPI-1448b: added ~100 test files covering all V2/V3 UsersContext/SecurityContext queries, ClaimSetEditor, and AddApplicationCommand/EditApplicationCommand. Final V2: 458 passed; V3: 448 passed. Coverage increased from 38.4% → 49.6% total line. |
| 7 | Remaining DB.Commands, ClaimSetEditor commands, Feature mappers/handlers | Second EF InMemory + FakeItEasy sweep to reach 70% | In progress |
