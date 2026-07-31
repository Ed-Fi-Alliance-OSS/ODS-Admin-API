# Rename `apiclients` Endpoints to `apiClients` (V2 and V3)

**Jira:** [ADMINAPI-1476](https://edfi.atlassian.net/browse/ADMINAPI-1476)

## Problem

The `ApiClients` feature's HTTP routes are registered using the all-lowercase literal `apiclients`, which does not match the project's camelCase endpoint naming standard. This needs to be corrected on both the V2 (`Application/EdFi.Ods.AdminApi`) and V3 (`Application/EdFi.Ods.AdminApi.V3`) APIs.

## Approach

A straight literal-string rename of the route path segment from `apiclients` to `apiClients` everywhere it is used to define or reconstruct a route. No routing abstraction, configuration flag, versioned-alias layer, or redirect middleware is introduced — the route literal is the source of truth and this is a direct rename of that literal.

This is a **clean breaking change**: the old lowercase path stops resolving (`404 Not Found`) once this ships. No backward-compatible alias/redirect is added. This was chosen over introducing a temporary alias because:
- The Jira acceptance criteria describe the endpoints as simply "changed," not deprecated-and-aliased.
- Downstream consumer work is already tracked in separate linked tickets (AC-569, AC-578) for the Admin App to adopt the new casing — the ecosystem is expected to move, not be bridged.

## Scope

### Production code — route literals to rename

Both V2 (`Application/EdFi.Ods.AdminApi/Features/ApiClients/`) and V3 (`Application/EdFi.Ods.AdminApi.V3/Features/ApiClients/`) have the identical set of five files, each requiring the same literal change from `/apiclients` to `/apiClients`:

| File | Literal(s) to change |
| --- | --- |
| `ReadApiClient.cs` | `MapGet(..., "/apiclients", ...)`, `MapGet(..., "/apiclients/{id}", ...)` |
| `AddApiClient.cs` | `MapPost(..., "/apiclients", ...)`, plus the path used to build the `Location` header (`Results.Created` in V2; `ResourceUrlHelper.BuildAbsoluteResourceUrl` in V3) |
| `EditApiClient.cs` | `MapPut(..., "/apiclients/{id}", ...)` |
| `DeleteApiClient.cs` | `MapDelete(..., "/apiclients/{id}", ...)` |
| `ResetApiClientCredentials.cs` | `MapPut(..., "/apiclients/{id}/reset-credential", ...)` |

### Tests and docs updated alongside the code change

- **Bruno E2E tests** (~40 `.bru` files under both V2's `E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/ApiClient/` and V3's `E2E Tests/Bruno Admin API E2E 3.0/v3/ApiClient/`): update the request URL inside each file to use `apiClients`. Folder names (`ApiClient/`) and file names (e.g. `POST - ApiClients.bru`) are left unchanged — they're labels, not the path under test.
- **`Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Helpers/ResourceUrlHelperTests.cs`**: the sample literal `"apiclients/101"` becomes `"apiClients/101"` for consistency (it's an arbitrary example path used to test URL-building logic, not a real route, but leaving it lowercase would read as a stale/valid alternative).
- **`docs/http/apiClients.http`**: six manual test request URLs still reference `/v3/apiclients`; updated to `/v3/apiClients`.

### Explicitly out of scope

- **No backward-compatible alias/redirect** for the old lowercase path.
- **Generated OpenAPI docs** (`docs/api-specifications/openapi-yaml/admin-api-2.3.0.yaml` and the paired markdown summary) — these are produced by `build.ps1 -Command GenerateOpenAPI` / `GenerateDocumentation`, which builds and runs the app and captures its live Swagger output through `widdershins`. They are not hand-edited, and regenerating them is a separate release/docs step.
- **The `ApiClients` database table** (`[Table("ApiClients")]` in `Application/EdFi.Ods.AdminApi.V1/Admin.DataAccess/Models/ApiClient.cs`) — this is DB schema, unrelated to the HTTP route casing.
- **Downstream Admin App consumer updates** — tracked separately in linked tickets AC-569 ("Admin App can manage ApiClients - V3") and AC-578 ("apiclients endpoints have changed to apiClients - V2 only").

## Testing

- Unit tests: `./build.ps1 -Command UnitTest`
- E2E tests (both versions, since the route change is identical in each):
  - `./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TenantMode multitenant -TearDown`
  - `./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TenantMode multitenant -TearDown`

## Risks

- **Breaking change for any existing API consumer** hitting the lowercase path — accepted per the Jira AC and the existence of separate downstream tracking tickets for consumers to adapt.
