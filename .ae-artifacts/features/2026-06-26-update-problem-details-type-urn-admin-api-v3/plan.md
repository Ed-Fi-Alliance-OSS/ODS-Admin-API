---
title: Update Problem Details Type URN in Admin API v3
feature: 2026-06-26-update-problem-details-type-urn-admin-api-v3
date: 2026-06-26
version: 1
status: draft
source-spec: ./spec.md
mode: brownfield
---

# Update Problem Details Type URN in Admin API v3

## 1. Source spec

This plan was derived from spec.md version 1 (status: approved). Mode: brownfield — existing
.NET 10 / ASP.NET Core v3 codebase. Tech stack: C# .NET 10 + ASP.NET Core minimal API +
NUnit/Shouldly/FakeItEasy (unit tests) + Bruno (E2E), no new packages.

## 2. Tech Stack

- **Language:** C# (existing)
- **Runtime:** .NET 10 (existing)
- **Frameworks / major libraries:** ASP.NET Core minimal API, Microsoft.AspNetCore.Mvc.ProblemDetails (existing)
- **Persistence:** N/A — no database changes
- **Deployment target:** Existing Docker / IIS / localhost profiles (existing)
- **Auth:** OpenID Connect (existing; untouched by this feature)
- **Testing framework:** NUnit + Shouldly + FakeItEasy for unit tests; Bruno for E2E (existing)
- **CI/CD:** GitHub Actions (existing)

## 3. Non-functional considerations

### Security

N/A — this change only modifies a string field in error response bodies. No auth surface, no user input, no PII.

### Error states

N/A — this is an error-response-format fix, not a user-facing flow that can itself fail.

### Non-functional requirements

Response latency impact: negligible (string constant substitution). Target: error response p95 ≤ 100ms, consistent with existing error pipeline performance.

## 4. Architecture

**Approach (Option B — URN constants class):**
Introduce a new static `AdminApiProblemTypes` class in `Infrastructure/ErrorHandling/` holding all
URN string constants. Add a required `type` string parameter to `V3ProblemDetailsFactory.Create`.
Each call site (`V3RequestErrorMiddleware` exception switch, `AdminApiModeValidationMiddleware`,
`GetJobStatus`) passes the appropriate `AdminApiProblemTypes.*` constant. `CreateValidation` passes
`AdminApiProblemTypes.BadRequestValidation` to `Create` internally.

**Rationale:** The constants class prevents typos across the URN values and makes the full URN
set discoverable in one place. Spec-confirmed: caller supplies the type.

**Component diagram:**

```
AdminApiProblemTypes (new)
     ↓ (referenced by)
V3ProblemDetailsFactory.Create (type param added)
     ↑ (called by)
     ├── V3RequestErrorMiddleware (exception switch — 5+ cases)
     ├── AdminApiModeValidationMiddleware (1 call)
     └── GetJobStatus (1 call)
```

**URN mapping:**

| Trigger | URN |
|---------|-----|
| `ValidationException` | `urn:ed-fi:admin-api:bad-request:validation` |
| `INotFoundException` | `urn:ed-fi:admin-api:not-found` |
| `BadHttpRequestException` | `urn:ed-fi:admin-api:bad-request:data` |
| `IAdminApiException` (4xx) | `urn:ed-fi:admin-api:bad-request` |
| `IAdminApiException` (5xx) / unhandled | `urn:ed-fi:admin-api:internal-server-error` |
| Mode mismatch (`AdminApiModeValidationMiddleware`) | `urn:ed-fi:admin-api:bad-request:version-mismatch` |
| Missing tenant (`GetJobStatus`) | `urn:ed-fi:admin-api:bad-request` |

**Brownfield attachment points:**
- `V3ProblemDetailsFactory.cs` is modified (one new required param on `Create`).
- `V3RequestErrorMiddleware.cs` is modified (passes constants to each `Create` call).
- `AdminApiModeValidationMiddleware.cs` is modified (passes version-mismatch constant).
- `GetJobStatus.cs` is modified (passes bad-request constant).
- v1/v2 pipelines and `RequestLoggingMiddleware` are untouched.

## 5. Steps

### Step 2026-06-26-update-problem-details-type-urn-admin-api-v3-S1: Add AdminApiProblemTypes constants class and update V3ProblemDetailsFactory

- **Depends on:** []
- **Parallel-safe:** no
- **Outputs:**
  - New: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/ErrorHandling/AdminApiProblemTypes.cs`
  - Modified: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/ErrorHandling/V3ProblemDetailsFactory.cs`
- **Deletes:** (none)
- **Acceptance:**
  - **Given** `V3ProblemDetailsFactory.Create` is called with `type: "urn:ed-fi:admin-api:bad-request:validation"` and status 400
  - **When** the returned `ProblemDetails` object is inspected in a unit test
  - **Then** the `type` field equals `"urn:ed-fi:admin-api:bad-request:validation"` and does not equal `"about:blank"`

Create `AdminApiProblemTypes` as a static class in `Infrastructure/ErrorHandling/` with string constants
for all URN values (`BadRequestValidation`, `NotFound`, `BadRequestData`, `BadRequest`, `InternalServerError`,
`BadRequestVersionMismatch`). Add a required `string type` parameter to `V3ProblemDetailsFactory.Create`
(after `detail`, before `correlationId`). Update `CreateValidation` to pass `AdminApiProblemTypes.BadRequestValidation`
internally. All other existing callers will fail to compile until updated in S2 and S3 — this is intentional
and ensures no call site silently continues returning `about:blank`.

### Step 2026-06-26-update-problem-details-type-urn-admin-api-v3-S2: Update V3RequestErrorMiddleware call sites

- **Depends on:** [2026-06-26-update-problem-details-type-urn-admin-api-v3-S1]
- **Parallel-safe:** yes
- **Outputs:**
  - Modified: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/ErrorHandling/V3RequestErrorMiddleware.cs`
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** Validation error
    - **Given** a request triggers a `ValidationException`
    - **When** the error middleware writes the response
    - **Then** the response body `type` is `"urn:ed-fi:admin-api:bad-request:validation"` and status is 400
  - **Scenario:** Not found
    - **Given** a request triggers an `INotFoundException`
    - **When** the error middleware writes the response
    - **Then** `type` is `"urn:ed-fi:admin-api:not-found"` and status is 404
  - **Scenario:** Malformed JSON
    - **Given** a request triggers a `BadHttpRequestException`
    - **When** the error middleware writes the response
    - **Then** `type` is `"urn:ed-fi:admin-api:bad-request:data"` and status is 400
  - **Scenario:** 4xx IAdminApiException
    - **Given** an `IAdminApiException` with a 4xx status code is thrown
    - **When** the error middleware writes the response
    - **Then** `type` is `"urn:ed-fi:admin-api:bad-request"`
  - **Scenario:** Unhandled error
    - **Given** an unhandled exception is thrown
    - **When** the error middleware writes the response
    - **Then** `type` is `"urn:ed-fi:admin-api:internal-server-error"` and status is 500

Update each branch of the exception switch in `CreateProblemDetails` to pass the appropriate
`AdminApiProblemTypes.*` constant. For `IAdminApiException`, derive the URN at runtime: if the
status code is >= 500 or null, pass `AdminApiProblemTypes.InternalServerError`; otherwise pass
`AdminApiProblemTypes.BadRequest`.

### Step 2026-06-26-update-problem-details-type-urn-admin-api-v3-S3: Update AdminApiModeValidationMiddleware and GetJobStatus call sites

- **Depends on:** [2026-06-26-update-problem-details-type-urn-admin-api-v3-S1]
- **Parallel-safe:** yes
- **Outputs:**
  - Modified: `Application/EdFi.Ods.AdminApi.V3/Features/AdminApiModeValidationMiddleware.cs`
  - Modified: `Application/EdFi.Ods.AdminApi.V3/Features/Jobs/GetJobStatus.cs`
- **Deletes:** (none)
- **Acceptance:**
  - **Scenario:** Mode mismatch
    - **Given** the API is configured in V2 mode and a `/v3/` endpoint is requested
    - **When** the mode validation middleware returns its error response
    - **Then** the response body `type` is `"urn:ed-fi:admin-api:bad-request:version-mismatch"` and status is 400
  - **Scenario:** Missing tenant
    - **Given** multi-tenancy is enabled and no tenant is resolved for a job-status request
    - **When** the job status handler returns its error response
    - **Then** the response body `type` is `"urn:ed-fi:admin-api:bad-request"` and status is 400

### Step 2026-06-26-update-problem-details-type-urn-admin-api-v3-S4: Update unit tests to assert type field values

- **Depends on:** [2026-06-26-update-problem-details-type-urn-admin-api-v3-S2, 2026-06-26-update-problem-details-type-urn-admin-api-v3-S3]
- **Parallel-safe:** yes
- **Outputs:**
  - Modified: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/ErrorHandling/V3ProblemDetailsFactoryTests.cs`
  - Modified: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/ErrorHandling/V3RequestErrorMiddlewareTests.cs`
  - Modified: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Jobs/GetJobStatusTests.cs`
  - Modified: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/AdminApiModeValidationMiddlewareTests.cs`
- **Deletes:** (none)
- **Acceptance:**
  - **Given** the unit tests for the problem details factory, request error middleware, job status handler, and mode validation middleware are run
  - **When** `./build.ps1 -Command UnitTest` completes
  - **Then** all tests pass and each error-response test asserts the expected `urn:ed-fi:admin-api:*` URN — no test asserts or accepts `"about:blank"`

Add `type` field assertions to every existing test that constructs or triggers a problem details
response. Add new test cases in `V3RequestErrorMiddlewareTests` covering `BadHttpRequestException`,
`IAdminApiException` with a 4xx status, and `IAdminApiException` with a 5xx status.
Add a `type` assertion to `GetJobStatusTests.Handle_ReturnsProblemDetails_WhenMultiTenancyEnabledAndTenantMissing`.
Add a `type` assertion to `AdminApiModeValidationMiddlewareTests.InvokeAsync_WhenVersionMismatch_ReturnsProblemDetails`
asserting `urn:ed-fi:admin-api:bad-request:version-mismatch`.

### Step 2026-06-26-update-problem-details-type-urn-admin-api-v3-S5: Update all Bruno E2E error-path tests to assert type field

- **Depends on:** [2026-06-26-update-problem-details-type-urn-admin-api-v3-S2, 2026-06-26-update-problem-details-type-urn-admin-api-v3-S3]
- **Parallel-safe:** yes
- **Outputs:**
  - Modified: existing error-path `.bru` files under `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/`
  - New: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Malformed Body.bru`
- **Deletes:** (none)
- **Acceptance:**
  - **Given** the Admin API v3 is running locally in the appropriate mode
  - **When** `./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TenantMode multitenant -TearDown` completes
  - **Then** all error-path tests pass, each including an assertion on the `type` field with a `urn:ed-fi:admin-api:*` value matching the error category for that test

Add a `type` assertion to the `script:post-response` block of each error-path `.bru` file.
Expected values by scenario category:
- 404 Not Found tests → `urn:ed-fi:admin-api:not-found`
- Validation / "Invalid" tests → `urn:ed-fi:admin-api:bad-request:validation`
- Files named "Invalid JSON" → `urn:ed-fi:admin-api:bad-request:validation` (these send valid JSON
  with wrong schema fields — they go through `ValidationException`, not `BadHttpRequestException`)
- "System Reserved" tests → `urn:ed-fi:admin-api:bad-request:validation` (these assert
  `response.errors[field]`, confirming they throw `ValidationException`)
- Mode mismatch test → `urn:ed-fi:admin-api:bad-request:version-mismatch`
- `IAdminApiException` 4xx tests (responses without an `errors` object) → `urn:ed-fi:admin-api:bad-request`
- **New** `POST - ClaimSets - Malformed Body.bru` (sends syntactically broken JSON, e.g.
  `{ "name":`) → asserts status 400 and `type` equals `urn:ed-fi:admin-api:bad-request:data`

## 6. Traceability

- **SC-1: "A validation error (400) response includes `urn:ed-fi:admin-api:bad-request:validation`"** — covered by: `S2`, `S4`, `S5`
- **SC-2: "A not-found error (404) response includes `urn:ed-fi:admin-api:not-found`"** — covered by: `S2`, `S4`, `S5`
- **SC-3: "A malformed JSON request (400) response includes `urn:ed-fi:admin-api:bad-request:data`"** — covered by: `S2`, `S4`, `S5`
- **SC-4: "A mode-mismatch error (400) response includes `urn:ed-fi:admin-api:bad-request:version-mismatch`"** — covered by: `S3`, `S4`, `S5`
- **SC-5: "An unhandled server error (500) response includes `urn:ed-fi:admin-api:internal-server-error`"** — covered by: `S2`, `S4`, `S5`
- **SC-6: "`IAdminApiException` with 4xx → `urn:ed-fi:admin-api:bad-request`; with 5xx → `urn:ed-fi:admin-api:internal-server-error`"** — covered by: `S2`, `S4`, `S5`
- **SC-7: "No error response returns `about:blank`"** — covered by: `S1`, `S2`, `S3`
- **SC-8: "All existing error-path Bruno tests assert the correct `type` URN for their scenario"** — covered by: `S5`
- **SC-9: "Unit tests cover each URN assignment in `V3ProblemDetailsFactory` and `V3RequestErrorMiddleware`"** — covered by: `S4`

## 7. Interface Contracts

No cross-layer boundaries identified. No `contracts.md` generated.
