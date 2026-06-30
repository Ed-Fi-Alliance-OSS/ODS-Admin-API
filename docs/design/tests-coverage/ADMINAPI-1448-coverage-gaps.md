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
| V2 | ApiClients | `Application\EdFi.Ods.AdminApi\Features\ApiClients` | Not reviewed before ApiClients batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\ApiClients` | Not started |
| V2 | Applications | `Application\EdFi.Ods.AdminApi\Features\Applications` | Not reviewed before Applications batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Applications` | Not started |
| V2 | AuthorizationStrategies | `Application\EdFi.Ods.AdminApi\Features\AuthorizationStrategies` | Not reviewed before AuthorizationStrategies batch | No existing folder found at plan time | Not started |
| V2 | ClaimSets | `Application\EdFi.Ods.AdminApi\Features\ClaimSets` | Not reviewed before ClaimSets batch | No existing folder found at plan time | Not started |
| V2 | Connect | `Application\EdFi.Ods.AdminApi\Features\Connect` | Not reviewed before Connect batch | No existing folder found at plan time | Not started |
| V2 | DbInstances | `Application\EdFi.Ods.AdminApi\Features\DbInstances` | Not reviewed before DbInstances batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\DbInstances` | Not started |
| V2 | Information | `Application\EdFi.Ods.AdminApi\Features\Information` | Not reviewed before Information batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Information` | Not started |
| V2 | Jobs | `Application\EdFi.Ods.AdminApi\Features\Jobs` | Not reviewed before Jobs batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Jobs` | Not started |
| V2 | OdsInstanceContext | `Application\EdFi.Ods.AdminApi\Features\OdsInstanceContext` | Not reviewed before OdsInstanceContext batch | No existing folder found at plan time | Not started |
| V2 | OdsInstanceDerivative | `Application\EdFi.Ods.AdminApi\Features\OdsInstanceDerivative` | Not reviewed before OdsInstanceDerivative batch | No existing folder found at plan time | Not started |
| V2 | OdsInstances | `Application\EdFi.Ods.AdminApi\Features\OdsInstances` | Not reviewed before OdsInstances batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\OdsInstances` | Not started |
| V2 | Profiles | `Application\EdFi.Ods.AdminApi\Features\Profiles` | Not reviewed before Profiles batch | No existing folder found at plan time | Not started |
| V2 | ResourceClaimActionAuthStrategies | `Application\EdFi.Ods.AdminApi\Features\ResourceClaimActionAuthStrategies` | Not reviewed before ResourceClaimActionAuthStrategies batch | No existing folder found at plan time | Not started |
| V2 | ResourceClaimActions | `Application\EdFi.Ods.AdminApi\Features\ResourceClaimActions` | Not reviewed before ResourceClaimActions batch | No existing folder found at plan time | Not started |
| V2 | ResourceClaims | `Application\EdFi.Ods.AdminApi\Features\ResourceClaims` | Not reviewed before ResourceClaims batch | No existing folder found at plan time | Not started |
| V2 | Tenants | `Application\EdFi.Ods.AdminApi\Features\Tenants` | Not reviewed before Tenants batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Tenants` | Not started |
| V2 | Vendors | `Application\EdFi.Ods.AdminApi\Features\Vendors` | Not reviewed before Vendors batch | `Application\EdFi.Ods.AdminApi.UnitTests\Features\Vendors` | Not started |
| V3 | Feature folders | `Application\EdFi.Ods.AdminApi.V3\Features` | V3 folders are reviewed with the matching feature batch | `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features` | Not started |

## Skipped or uncovered areas

| Feature/endpoint | API surface | Gap type | Evidence | Reason | Suspected risk | Recommended Jira summary | Recommended acceptance criteria |
| --- | --- | --- | --- | --- | --- | --- | --- |

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
| 1 | Applications, ApiClients | High endpoint value and existing test patterns | Not started |
| 2 | ClaimSets and ResourceClaims | High uncovered-line count and validation/mapping complexity | Not started |
| 3 | ODS/DataStore features | V2/V3 equivalent feature groups with existing partial tests | Not started |
| 4 | Vendors, Actions, AuthorizationStrategies, Information, Jobs, Tenants, Profiles, Connect | Remaining first-sweep feature coverage | Not started |
| 5 | Common and infrastructure directly exercised by V2/V3 features | Shared logic needed to support endpoint behavior | Not started |