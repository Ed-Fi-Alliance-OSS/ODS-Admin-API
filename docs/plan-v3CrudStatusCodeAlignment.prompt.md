## Plan: V3 CRUD Status Code Alignment

Align Application/EdFi.Ods.AdminApi.V3 standard CRUD endpoints to consistent success status codes (POST=201, GET=200, PUT=204, DELETE=204) while preserving intentional non-CRUD exceptions (async operations returning 202 and action endpoints returning 200 with response payload). Execute in 4 phases with a hard checkpoint after each phase so you can run tests and commit before proceeding.

**Steps**
1. Phase 0 - Baseline and guardrails
2. Confirm endpoint inventory in Application/EdFi.Ods.AdminApi.V3/Features and classify endpoints into: standard CRUD vs intentional exceptions.
3. Lock explicit exceptions in scope notes before edits: POST /dbInstances and POST refresh operations remain 202; ClaimSet authorization-strategy action operations remain 200. *blocks all later phases*
4. Capture a one-to-one endpoint-to-Bruno test mapping for each CRUD operation to ensure endpoint and E2E assertion changes stay synchronized. *parallel with step 2*
5. Pause checkpoint: no code changes yet; review baseline list.
6. Phase 1 - POST/create returns 201
7. Audit all standard create endpoints in Features for explicit response metadata and returned result type; correct any non-201 standard CRUD creates to 201 Created with appropriate Location behavior.
8. Update corresponding Bruno success assertions under Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3 for each corrected POST endpoint.
9. Verify Phase 1 with targeted Bruno collection run for POST success paths and affected negative paths.
10. Pause checkpoint for your test run + commit before Phase 2.
11. Phase 2 - GET/read returns 200
12. Audit all standard read endpoints in Features and ensure success responses are 200 OK with unchanged resource payload semantics.
13. Update Bruno GET success assertions only where mismatches are found; keep existing 404/400 negative assertions intact.
14. Verify Phase 2 with targeted Bruno GET suites.
15. Pause checkpoint for your test run + commit before Phase 3.
16. Phase 3 - PUT/update returns 204
17. Update standard update endpoints currently returning 200 OK to 204 NoContent in endpoint response metadata and result return type, including reset-credential PUT endpoints for ApiClients and Applications.
18. Update matching Bruno PUT success assertions from 200 to 204 and adjust assertion message text if needed.
19. Confirm no behavioral regressions in validation/not-found flows (400/404 unchanged).
20. Verify Phase 3 with targeted Bruno PUT suites.
21. Pause checkpoint for your test run + commit before Phase 4.
22. Phase 4 - DELETE returns 204
23. Update standard delete endpoints currently returning 200 OK to 204 NoContent in endpoint response metadata and result return type.
24. Update matching Bruno DELETE success assertions from 200 to 204 and keep not-found/business-rule error assertions unchanged.
25. Verify Phase 4 with targeted Bruno DELETE suites and a final CRUD smoke pass.
26. Final checkpoint for your full run + commit.
27. Documentation handoff
28. Create a repository plan/summary markdown artifact (recommended path: docs/v3-status-code-alignment-plan.md) that records: endpoints changed, intentional exceptions retained, Bruno files updated, and per-phase verification evidence. *depends on completion of phases 1-4*

**Relevant files**
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/Applications/EditApplication.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/ApiClients/EditApiClient.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/EditClaimSet.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/ResourceClaims/EditResourceClaimActions.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/EditOdsInstance.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/OdsInstanceContext/EditOdsInstanceContext.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/OdsInstanceDerivative/EditOdsInstanceDerivative.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/Profiles/EditProfile.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/Vendors/EditVendor.cs - PUT currently mapped as 200; expected Phase 3 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/Applications/DeleteApplication.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/ApiClients/DeleteApiClient.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/DeleteClaimSet.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/DeleteOdsInstance.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/OdsInstanceContext/DeleteOdsInstanceContext.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/OdsInstanceDerivative/DeleteOdsInstanceDerivative.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/Profiles/DeleteProfile.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/Vendors/DeleteVendor.cs - DELETE currently mapped as 200; expected Phase 4 update to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/DbInstances/AddDbInstance.cs - intentional POST 202 exception to preserve.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/RefreshEducationOrganizations.cs - intentional POST 202 exceptions to preserve.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/ApiClients/ResetApiClientCredentials.cs - include in Phase 3 to change success response from 200 to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/Applications/ResetApplicationCredentials.cs - include in Phase 3 to change success response from 200 to 204.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/ResourceClaims/EditAuthStrategy.cs - intentional POST 200 action endpoints to preserve.
- c:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API/Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3 - Bruno test root; update per-endpoint .bru status assertions for changed PUT/DELETE (and POST/GET only if mismatches are found).

**Verification**
1. For each phase, run only affected Bruno request suites first (method-focused smoke) and confirm updated success code assertions pass while 400/404 negatives stay unchanged.
2. After each phase, stop for your local validation and commit gate before moving to the next phase.
3. At end of Phase 4, run a full CRUD Bruno smoke pass across Applications, ApiClients, ClaimSets, OdsInstances, Profiles, Vendors.
4. Optional confidence pass: run repository unit tests via ./build.ps1 -Command UnitTest after all four phases.

**Decisions**
- Included scope: standard CRUD status-code consistency for Application/EdFi.Ods.AdminApi.V3 only, plus synchronized Bruno updates.
- Excluded scope: changing intentional async/action endpoint semantics that remain 202 or ClaimSet authorization-strategy actions that remain 200 for business reasons.
- Process decision: enforce phased delivery with explicit stop points for your tests and commit after each phase.
- Documentation decision: produce a repository markdown plan/summary artifact later (recommended docs/v3-status-code-alignment-plan.md).

**Further Considerations**
1. PUT/DELETE response body handling when moving to 204: recommendation is to remove/avoid success payload assertions for those operations in Bruno because 204 should not include a response body.
2. If any endpoint currently relies on client-side parsing of delete/update success messages, capture that as a migration note in the final documentation artifact.
3. Keep route-level error response expectations (400/404) unchanged to prevent accidental API contract broadening during status-code alignment.