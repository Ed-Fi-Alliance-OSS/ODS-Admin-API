# AutoMapper Migration Phase 0 Baseline

Date: 2026-03-30

## Purpose

Establish a pre-migration baseline before any AutoMapper removal work starts.

Phase 0 goals:
* Capture current test baseline
* Define API smoke-check targets
* Define payload snapshot checkpoints for high-risk mapping endpoints

## 1. Test Baseline (Completed)

Command executed:

```powershell
./build.ps1 UnitTest
```

Observed results:
* `EdFi.Ods.AdminApi.Common.UnitTests`: 22 total, 22 passed, 0 failed
* `EdFi.Ods.AdminApi.UnitTests`: 59 total, 59 passed, 0 failed
* Overall: build succeeded

TRX outputs:
* `Application/EdFi.Ods.AdminApi.Common.UnitTests/EdFi.Ods.AdminApi.Common.UnitTests.csproj.trx`
* `Application/EdFi.Ods.AdminApi.UnitTests/EdFi.Ods.AdminApi.UnitTests.csproj.trx`

## 2. API Smoke-Check Targets (Prepared)

Use existing HTTP collections as execution sources:
* `docs/http/claimsets.http`
* `docs/http/apiClients.http`
* `docs/http/vendors.http`
* `docs/ADMINAPI-1221.md` (applications query behavior reference)

Recommended endpoint checks (V2 first, then V1 parity):
* `GET /v2/claimsets`
* `GET /v2/claimsets/{id}`
* `GET /v2/applications?id={id}`
* `GET /v2/applications?ids={id1},{id2}`
* `GET /v2/apiclients/{id}`
* `GET /v2/apiclients?applicationid={id}`
* `GET /v2/vendors`
* `GET /v2/vendors/{id}`

For V1 parity:
* `GET /v1/claimsets`
* `GET /v1/claimsets/{id}`
* `GET /v1/applications/{id}` or equivalent route in current deployment
* `GET /v1/vendors`

## 3. Payload Snapshot Checkpoints (High Risk)

Capture and store representative JSON responses for these model families before migration. These snapshots should be compared after each migration phase.

### Applications (V2)

Model: `ApplicationModel`
Key fields to compare:
* `id`
* `applicationName`
* `claimSetName`
* `educationOrganizationIds`
* `vendorId`
* `profileIds`
* `odsInstanceIds`
* `enabled`

Special behavior checks (from ADMINAPI-1221):
* `id` query takes precedence over `ids` if both are supplied
* `id` invalid format handling
* `ids` invalid format handling (400)
* `404` when requested IDs are missing

### ApiClients (V2)

Model: `ApiClientModel`
Key fields to compare:
* `id`
* `key`
* `name`
* `isApproved`
* `useSandbox`
* `sandboxType`
* `applicationId`
* `keyStatus`
* `educationOrganizationIds`
* `odsInstanceIds`

### Vendors (V2)

Model: `VendorModel`
Key fields to compare:
* `id`
* `company`
* `namespacePrefixes`
* `contactName`
* `contactEmailAddress`

### ClaimSets and Resource Hierarchy (V2)

Models: `ClaimSetModel`, `ClaimSetDetailsModel`, `ClaimSetResourceClaimModel`
Key fields to compare:
* `id`
* `name`
* `_isSystemReserved`
* `_applications`
* `resourceClaims`
* Nested `children` hierarchy
* `actions`
* `_defaultAuthorizationStrategiesForCRUD`
* `authorizationStrategyOverridesForCRUD`

This is the highest-risk area because mapping includes nested structures and authorization strategy projections.

### V1 Parity Models

Compare equivalent model payloads for V1:
* `ApplicationModel` (V1 shape differs from V2)
* `VendorModel` (V1 uses `vendorId`)
* `ClaimSetDetailsModel` and nested `ResourceClaimModel`

## 4. Snapshot Storage Convention

Create baseline artifacts under:
* `docs/baselines/automapper-phase0/`

Suggested files:
* `v2-applications-id.json`
* `v2-applications-ids.json`
* `v2-apiclient-by-id.json`
* `v2-vendors.json`
* `v2-claimsets.json`
* `v2-claimset-details.json`
* `v1-claimsets.json`
* `v1-claimset-details.json`
* `v1-vendors.json`

## 5. Phase 0 Exit Criteria

Phase 0 is complete when:
* Unit test baseline is recorded (done)
* Smoke-check endpoint list is defined (done)
* Payload snapshot checkpoints are documented (done)
* Baseline JSON snapshots are captured in environment where Admin API is running (pending runtime execution)

## 6. Notes

* This baseline intentionally avoids implementation changes.
* Next step is Phase 1 migration for simple V2 mappings (Vendors/Actions/Profiles/basic Applications reads), with response snapshots used for parity checks.
