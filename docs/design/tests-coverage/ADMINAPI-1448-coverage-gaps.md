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
| 2026-06-29 | `.\build.ps1 -Command UnitTest -Configuration Debug -RunCoverageAnalysis` | Not measured before Task 2 | Not measured before Task 2 | Not measured before Task 2 | Not measured before Task 2 | V1 excluded |

## Feature inventory

| API surface | Feature | Source path | Endpoint/behavior files reviewed | Existing tests reviewed | Status |
| --- | --- | --- | --- | --- | --- |
| V2 | Actions | `Application\EdFi.Ods.AdminApi\Features\Actions` | Not reviewed before Actions batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Actions` | Not started |
| V2 | ApiClients | `Application\EdFi.Ods.AdminApi\Features\ApiClients` | Reviewed: `AddApiClient.cs`, `ApiClientMapper.cs`, `ApiClientModel.cs`, `DeleteApiClient.cs`, `EditApiClient.cs`, `ReadApiClient.cs`, `ResetApiClientCredentials.cs` | Reviewed/updated: `AddApiClientOdsInstanceIdsValidationTests.cs`, `AddApiClientValidatorTests.cs`, `ApiClientModelTests.cs`, `DeleteApiClientTests.cs`, `EditApiClientOdsInstanceIdsValidationTests.cs`, `EditApiClientValidatorTests.cs`, `ReadApiClientTests.cs`, `ResetApiClientCredentialsTests.cs` | Task 5 updated validator null-IDs coverage |
| V2 | Applications | `Application\EdFi.Ods.AdminApi\Features\Applications` | Reviewed: `AddApplication.cs`, `ApplicationMapper.cs`, `ApplicationModel.cs`, `DeleteApplication.cs`, `EditApplication.cs`, `ReadApplication.cs`, `ReadApplicationsByOdsInstance.cs`, `ReadApplicationsByVendor.cs`, `ResetApplicationCredentials.cs` | Reviewed/updated: `ApplicationMapperTests.cs`, `AddApplicationValidatorTests.cs`, `EditApplicationValidatorTests.cs`, `DeleteApplicationTests.cs`, `ResetApplicationCredentialsTests.cs` | Task 5 added validator/delete/reset coverage |
| V2 | AuthorizationStrategies | `Application\EdFi.Ods.AdminApi\Features\AuthorizationStrategies` | Not reviewed before AuthorizationStrategies batch | No existing folder found at plan time | Not started |
| V2 | ClaimSets | `Application\EdFi.Ods.AdminApi\Features\ClaimSets` | Reviewed: `AddClaimSet.cs`, `ClaimSetMapper.cs`, `ClaimSetModel.cs`, `CopyClaimSet.cs`, `DeleteClaimSet.cs`, `EditClaimSet.cs`, `ExportClaimSet.cs`, `ImportClaimSet.cs`, `ReadClaimSets.cs`, `ResourceClaimValidator.cs`, `ResourceClaims\DeleteResourceClaim.cs`, `ResourceClaims\EditAuthStrategy.cs`, `ResourceClaims\EditResourceClaimActions.cs` | Added/reviewed: `AddClaimSetValidatorTests.cs`, `ClaimSetMapperTests.cs`, `ImportClaimSetValidatorTests.cs`; no pre-existing V2 ClaimSets test folder | Task 6 added existing-behavior validator/mapper coverage; handler/DB-heavy paths documented as skipped |
| V2 | Connect | `Application\EdFi.Ods.AdminApi\Features\Connect` | Not reviewed before Connect batch | No existing folder found at plan time | Not started |
| V2 | DbInstances | `Application\EdFi.Ods.AdminApi\Features\DbInstances` | Not reviewed before DbInstances batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\DbInstances` | Not started |
| V2 | Information | `Application\EdFi.Ods.AdminApi\Features\Information` | Not reviewed before Information batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Information` | Not started |
| V2 | Jobs | `Application\EdFi.Ods.AdminApi\Features\Jobs` | Not reviewed before Jobs batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Jobs` | Not started |
| V2 | OdsInstanceContext | `Application\EdFi.Ods.AdminApi\Features\OdsInstanceContext` | Not reviewed before OdsInstanceContext batch | No existing folder found at plan time | Not started |
| V2 | OdsInstanceDerivative | `Application\EdFi.Ods.AdminApi\Features\OdsInstanceDerivative` | Not reviewed before OdsInstanceDerivative batch | No existing folder found at plan time | Not started |
| V2 | OdsInstances | `Application\EdFi.Ods.AdminApi\Features\OdsInstances` | Not reviewed before OdsInstances batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\OdsInstances` | Not started |
| V2 | Profiles | `Application\EdFi.Ods.AdminApi\Features\Profiles` | Not reviewed before Profiles batch | No existing folder found at plan time | Not started |
| V2 | ResourceClaimActionAuthStrategies | `Application\EdFi.Ods.AdminApi\Features\ResourceClaimActionAuthStrategies` | Reviewed: `ReadResourceClaimActionAuthStrategies.cs`, `ResourceClaimActionAuthStrategyModel.cs` | No tests added in Task 6 | Skipped: read endpoint delegates to concrete DB query; model-only coverage deferred |
| V2 | ResourceClaimActions | `Application\EdFi.Ods.AdminApi\Features\ResourceClaimActions` | Reviewed: `ReadResourceClaimActions.cs`, `ResourceClaimActionModel.cs` | No tests added in Task 6 | Skipped: read endpoint delegates to concrete DB query; model-only coverage deferred |
| V2 | ResourceClaims | `Application\EdFi.Ods.AdminApi\Features\ResourceClaims` | Reviewed: `ReadResourceClaims.cs`, `ResourceClaimMapper.cs` | Added: `ResourceClaimMapperTests.cs` | Task 6 added mapper coverage; read endpoint DB-query path skipped |
| V2 | Tenants | `Application\EdFi.Ods.AdminApi\Features\Tenants` | Not reviewed before Tenants batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Tenants` | Not started |
| V2 | Vendors | `Application\EdFi.Ods.AdminApi\Features\Vendors` | Not reviewed before Vendors batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Vendors` | Not started |
| V3 | ApiClients | `Application\EdFi.Ods.AdminApi.V3\Features\ApiClients` | Reviewed: `AddApiClient.cs`, `ApiClientMapper.cs`, `ApiClientModel.cs`, `DeleteApiClient.cs`, `EditApiClient.cs`, `ReadApiClient.cs`, `ResetApiClientCredentials.cs` | Reviewed/updated: `AddApiClientOdsInstanceIdsValidationTests.cs`, `AddApiClientValidatorTests.cs`, `ApiClientModelTests.cs`, `DeleteApiClientTests.cs`, `EditApiClientOdsInstanceIdsValidationTests.cs`, `EditApiClientTests.cs`, `EditApiClientValidatorTests.cs`, `ReadApiClientTests.cs`, `ResetApiClientCredentialsTests.cs` | Task 5 updated validator null-IDs coverage |
| V3 | Applications | `Application\EdFi.Ods.AdminApi.V3\Features\Applications` | Reviewed: `AddApplication.cs`, `ApplicationMapper.cs`, `ApplicationModel.cs`, `DeleteApplication.cs`, `EditApplication.cs`, `ReadApplication.cs`, `ReadApplicationsByDataStore.cs`, `ReadApplicationsByVendor.cs`, `ResetApplicationCredentials.cs` | Reviewed/updated: `AddApplicationValidatorTests.cs`, `ApplicationMapperTests.cs`, `EditApplicationTests.cs`, `EditApplicationValidatorTests.cs`, `DeleteApplicationTests.cs`, `ResetApplicationCredentialsTests.cs` | Task 5 added delete/reset coverage; existing validator tests reviewed |
| V3 | ClaimSets | `Application\EdFi.Ods.AdminApi.V3\Features\ClaimSets` | Reviewed: `AddClaimSet.cs`, `ClaimSetMapper.cs`, `ClaimSetModel.cs`, `CopyClaimSet.cs`, `DeleteClaimSet.cs`, `EditClaimSet.cs`, `ExportClaimSet.cs`, `ImportClaimSet.cs`, `ReadClaimSets.cs`, `ResourceClaimValidator.cs`, `ResourceClaims\DeleteResourceClaim.cs`, `ResourceClaims\EditAuthStrategy.cs`, `ResourceClaims\EditResourceClaimActions.cs` | Reviewed/updated: `AddClaimSetValidatorTests.cs`, `CopyClaimSetValidatorTests.cs`, `EditClaimSetTests.cs`, `EditClaimSetValidatorTests.cs`, `ImportClaimSetValidatorTests.cs`, `ResourceClaims\EditResourceClaimActionsTests.cs`, added `ClaimSetMapperTests.cs` | Task 6 added mapper coverage and reused existing validator/route-id mismatch coverage; handler/DB-heavy paths documented as skipped |

## Skipped or uncovered areas

| Feature/endpoint | API surface | Gap type | Evidence | Reason | Suspected risk | Recommended Jira summary | Recommended acceptance criteria |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `ReadApplication.GetApplication` single-record null branch | V2 Applications | Not unit-tested in Task 5 | `Application\EdFi.Ods.AdminApi\Features\Applications\ReadApplication.cs`; `Application\EdFi.Ods.AdminApi\Infrastructure\Database\Queries\GetApplicationByIdQuery.cs`; `docs\Coverage Report.zip` entry `EdFi.Ods.AdminApi_GetApplicationByIdQuery.html` shows EF-query hotspots | Handler takes concrete `GetApplicationByIdQuery`, and that query throws `NotFoundException<int>` instead of returning null; testing the handler null branch without a database would require a testability seam or behavior decision. | Low/medium: redundant null guard may be unreachable and single-record handler behavior is mostly owned by query. | Review V2 application single-record query/handler testability | Decide whether `GetApplicationByIdQuery` or handler owns not-found behavior, then add unit coverage or remove unreachable branch. |
| `ReadApplication.GetApplication` single-record null branch | V3 Applications | Not unit-tested in Task 5 | `Application\EdFi.Ods.AdminApi.V3\Features\Applications\ReadApplication.cs`; `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Database\Queries\GetApplicationByIdQuery.cs`; `docs\Coverage Report.zip` entry `EdFi.Ods.AdminApi.V3_GetApplicationByIdQuery.html` shows EF-query hotspots | Handler takes concrete `GetApplicationByIdQuery`, and that query throws `NotFoundException<int>` instead of returning null; testing the handler null branch without a database would require a testability seam or behavior decision. | Low/medium: redundant null guard may be unreachable and single-record handler behavior is mostly owned by query. | Review V3 application single-record query/handler testability | Decide whether `GetApplicationByIdQuery` or handler owns not-found behavior, then add unit coverage or remove unreachable branch. |
| ClaimSets command/query-backed handlers (`Copy`, `Delete`, `Edit`, `Export`, `Read`, resource-claim auth overrides) | V2/V3 ClaimSets | Not unit-tested in Task 6 | `Application\EdFi.Ods.AdminApi\Features\ClaimSets`; `Application\EdFi.Ods.AdminApi.V3\Features\ClaimSets`; coverage report shows high uncovered handler/query lines | Many handlers depend on concrete command/query implementations or EF-backed query behavior. Adding unit tests would require behavior-preserving seams or brittle concrete fakes; Task 6 avoided real DB tests per plan. | Medium: endpoint orchestration around not-found/system-reserved/resource override paths remains covered mostly outside unit tests. | Add ClaimSets handler testability seams | Introduce interfaces or thin service abstractions for command/query-backed ClaimSets handlers, then add unit tests for result types and command interactions without a database. |

## Final summary

| Metric | Before V1 exclusion | After V1 exclusion | Final |
| --- | ---: | ---: | ---: |
| Total line coverage | 23% | Not measured before Task 2 | Not measured before Task 10 |
| Total branch coverage | 19.9% | Not measured before Task 2 | Not measured before Task 10 |
| V2 line coverage | 22.2% | Not measured before Task 2 | Not measured before Task 10 |
| V3 line coverage | 32.8% | Not measured before Task 2 | Not measured before Task 10 |

## Work batch order

| Batch | Features | Reason | Status |
| --- | --- | --- | --- |
| 1 | Applications, ApiClients | High endpoint value and existing test patterns | Task 5 implemented targeted V2/V3 unit coverage |
| 2 | ClaimSets and ResourceClaims | High uncovered-line count and validation/mapping complexity | Task 6 added targeted V2/V3 validator and mapper unit coverage; DB-heavy handler paths documented as skipped |
| 3 | ODS/DataStore features | V2/V3 equivalent feature groups with existing partial tests | Not started |
| 4 | Vendors, Actions, AuthorizationStrategies, Information, Jobs, Tenants, Profiles, Connect | Remaining first-sweep feature coverage | Not started |
| 5 | Common and infrastructure directly exercised by V2/V3 features | Shared logic needed to support endpoint behavior | Not started |