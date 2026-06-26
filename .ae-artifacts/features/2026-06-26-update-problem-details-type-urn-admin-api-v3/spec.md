---
title: Update Problem Details Type URN in Admin API v3
feature: 2026-06-26-update-problem-details-type-urn-admin-api-v3
date: 2026-06-26
version: 1
status: approved
---

# Update Problem Details Type URN in Admin API v3

## Problem Statement

Admin API v3 error responses currently return `"type": "about:blank"` in all problem
details payloads. RFC 9457 requires `type` to be a URI that uniquely identifies the
error type so that API consumers can programmatically distinguish and handle different
error conditions. The Ed-Fi Error Response Knowledge Base defines the expected URN
pattern for this field.

## Primary User

API consumers and integrators (developers) calling Admin API v3 endpoints who need to
identify the specific error category from a machine-readable `type` field rather than
parsing `title` or `detail` free text.

## Job-to-be-Done

When an error occurs, a calling system must be able to read the `type` field alone to
route to the appropriate error-handling path — without parsing `title` or `detail` strings.

## Success Criteria

1. A validation error (400) response includes `"type": "urn:ed-fi:admin-api:bad-request:validation"`.
2. A not-found error (404) response includes `"type": "urn:ed-fi:admin-api:not-found"`.
3. A malformed JSON request (400) response includes `"type": "urn:ed-fi:admin-api:bad-request:data"`.
4. A mode-mismatch error (400) response includes `"type": "urn:ed-fi:admin-api:bad-request:version-mismatch"`.
5. An unhandled server error (500) response includes `"type": "urn:ed-fi:admin-api:internal-server-error"`.
6. An `IAdminApiException` with a 4xx status includes `"type": "urn:ed-fi:admin-api:bad-request"`;
   with a 5xx status includes `"type": "urn:ed-fi:admin-api:internal-server-error"`.
7. No error response returns `"type": "about:blank"`.
8. All existing error-path Bruno tests assert the correct `type` URN for their scenario.
9. Unit tests cover each URN assignment in `V3ProblemDetailsFactory` and `V3RequestErrorMiddleware`.

## Explicit Scope Boundaries

**In scope:**
- Update `V3ProblemDetailsFactory.Create` to accept a caller-supplied `type` URN parameter.
- Update all call sites in `V3RequestErrorMiddleware` (exception switch) and
  `AdminApiModeValidationMiddleware` to pass the appropriate URN.
- Add unit test assertions for the `type` field to `V3ProblemDetailsFactoryTests`
  and `V3RequestErrorMiddlewareTests`.
- Update all existing error-path Bruno tests to assert the expected `type` URN value.

**Out of scope for v1:**
- v1 and v2 error response pipelines (kept untouched).
- Changes to any other RFC 9457 fields (`title`, `detail`, `status`, `correlationId`).
- Admin App error-envelope normalization.
- Making URNs dereferenceable (they are opaque identifiers).

## Confirmed Assumptions

- Assumption: `type` URN is passed explicitly by the caller. Confirmed: yes — each call site
  in `V3RequestErrorMiddleware` and `AdminApiModeValidationMiddleware` supplies its own URN string.
- Assumption: URN namespace is `urn:ed-fi:admin-api:`. Confirmed: yes — Admin API uses its own
  namespace, distinct from the ODS/API `urn:ed-fi:api:` namespace.
- Assumption: All existing error-path Bruno tests are updated with `type` assertions.
  Confirmed: yes — not only new tests, all existing error-path tests are updated.
- Assumption: Files named "Invalid JSON" in the Bruno suite test malformed syntax.
  Confirmed: no — those files send structurally valid JSON with wrong fields, so they go
  through `ValidationException` and should assert `bad-request:validation`. A new Bruno test
  is added that sends syntactically broken JSON to cover the `bad-request:data` path.
- Assumption: "System Reserved" Bruno tests produce a generic `bad-request` response.
  Confirmed: no — those tests assert `response.errors[field]`, confirming they throw
  `ValidationException` and should assert `bad-request:validation`.
- Assumption: `IAdminApiException` URN is derived from status code at runtime.
  Confirmed: yes — 4xx → `urn:ed-fi:admin-api:bad-request`, 5xx → `urn:ed-fi:admin-api:internal-server-error`.

## Open Questions

None — all design-critical assumptions resolved.

## Constraints

- New NuGet packages: not permitted — change uses only existing ASP.NET Core types.
- v1/v2 pipelines must not be touched.
- `V3ProblemDetailsFactory.Create` signature change must remain backward-compatible or
  all existing callers must be updated in the same PR.

## Dependencies

- `V3ProblemDetailsFactory.cs` — the single factory used by both error middlewares.
- `V3RequestErrorMiddleware.cs` — handles exception-to-problem-details mapping.
- `AdminApiModeValidationMiddleware.cs` — calls factory directly for mode mismatch errors.
- `V3ProblemDetailsFactoryTests.cs` — existing unit tests to be extended.
- Bruno E2E suite (`Bruno Admin API E2E 3.0/v3/`) — all error-path .bru files updated.
