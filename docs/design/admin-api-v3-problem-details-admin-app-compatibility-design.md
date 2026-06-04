# Admin API v3 Problem Details and Admin App Compatibility Design

## Summary

Update Admin API v3 to return RFC 9457 Problem Details responses for errors, while leaving v1 and v2 unchanged. Update the Admin App to accept both the current compact error envelope and Problem Details so existing UI behavior continues to work during the transition.

## Scope

### In scope

- Admin API v3 error payload shape
- Admin API v3 error logging follow-up documentation
- Admin App error normalization for compact and Problem Details envelopes
- Documentation and test updates that reflect the new behavior

### Out of scope

- Changing v1 or v2 error payloads
- Redesigning the Admin App notification UX
- Introducing a second error contract for v3 beyond Problem Details

## Decisions

1. Use **RFC 9457** as the canonical standard for Problem Details.
2. Apply the new error payload shape only to **Admin API v3**.
3. Keep v1 and v2 response shapes unchanged.
4. Keep **log4net** in this PR and track **Serilog** migration as follow-up work.
5. Make the Admin App tolerant of both:
   - the existing compact error envelope
   - RFC 9457 Problem Details

## Current State

Admin API v3 already returns `application/problem+json` in some places, but other paths still emit compact JSON envelopes such as `{ message: ... }`. The Admin App currently throws `error.response.data` directly from Axios and only fully understands its own `StatusResponse`-style objects, so Problem Details need a compatibility layer.

## Proposal

### Admin API repo

#### 1. Standardize v3 error responses on RFC 9457

The v3 request pipeline should emit Problem Details for all handled errors. Validation failures should use Problem Details with extension members for field errors. Unexpected failures should also use Problem Details with a stable title, status, and correlation-friendly metadata.

Expected v3 response characteristics:

- `Content-Type: application/problem+json`
- `type`, `title`, and `status` always present
- `detail` used for human-readable error details
- extension members used for:
  - `errors`
  - `validationErrors`
  - `correlationId`

#### 2. Leave v1 and v2 untouched

The existing compact envelopes and behavior for v1 and v2 stay as-is. The change is intentionally scoped to v3 because the repository supports multiple specifications and the compatibility risk should stay isolated.

#### 3. Track logging migration as follow-up work

The current change set keeps the existing runtime logger in place. A separate follow-up can migrate the v3 Admin API host to Serilog so logging is structured and aligned with the rest of the platform without coupling that migration to this error-contract change.

#### 4. Update API tests and docs

Add or update tests that verify:

- v3 returns Problem Details for validation and unexpected failures
- v1 and v2 remain unchanged
- error content type remains `application/problem+json` for v3
- logging behavior remains unchanged by this PR

### Admin App repo

#### 1. Normalize both error envelopes in one place

Add a single error normalization layer in the Admin App that can identify:

- compact envelopes with `message` / `statusCode`
- RFC 9457 Problem Details with `title`, `status`, and optional `detail`

That layer should convert both shapes into the app's existing internal error flow instead of making pages or feature code branch on raw HTTP response shapes.

#### 2. Preserve current UI behavior

The app should continue to:

- send validation errors to form state when possible
- show banner notifications for general errors
- redirect on `401` as it does today

Problem Details should not require page-by-page changes if the normalization layer is implemented centrally.

#### 3. Update backend and frontend app tests

Add tests for:

- Problem Details parsing
- compact envelope fallback
- validation error mapping
- banner routing for non-validation failures

## Recommended Implementation Shape

### API

1. Introduce a v3-only Problem Details mapping helper.
2. Ensure all v3 middleware and exception paths use the same error serializer.
3. Keep existing logging provider wiring unchanged in this change set.
4. Keep v1/v2 wiring untouched.

### App

1. Create a central error-shape adapter in the Axios response path or BFF error filter.
2. Normalize Problem Details and compact envelopes into the current internal error model.
3. Leave feature pages and banner components unchanged unless a specific parsing gap is uncovered.

## Risks

- A partial v3 migration could leave one or two endpoints still emitting compact envelopes.
- The Admin App may have hidden assumptions about `message`-only responses in a few feature flows.
- Future logging migration to Serilog should avoid changing log content in a way that breaks existing operational searches.

## Acceptance Criteria

- Admin API v3 returns RFC 9457 Problem Details for error responses.
- Admin API v1 and v2 keep their current error responses.
- Admin App handles both the compact envelope and Problem Details without regressions.
- Serilog migration for the v3 path is captured as follow-up work and is not required for this PR.
