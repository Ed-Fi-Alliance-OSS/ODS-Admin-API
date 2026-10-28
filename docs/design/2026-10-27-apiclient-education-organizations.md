# API client education organizations (ADMINAPI-1540)

**Status:** implemented ([PR #527](https://github.com/Ed-Fi-Alliance-OSS/ODS-Admin-API/pull/527))
**Jira:** [ADMINAPI-1540](https://edfi.atlassian.net/browse/ADMINAPI-1540)
**Contract:** Management API 3.0.0, amended in [Ed-Fi-API-Specifications#74](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-API-Specifications/pull/74)
**Source design:** [Management API APIClient EducationOrganizations](https://edfi.atlassian.net/wiki/spaces/BD/pages/2911076354) (Confluence)

This document describes what Admin API changed and why. It covers the behavior and the main code paths, not every file.

## Why

Many SEAs keep one Application per vendor product (for example, a SIS used by 200 LEAs) and issue one key/secret per LEA. Each key must only work for its own LEA. `EdFi_Admin` has supported this through `ApiClientApplicationEducationOrganizations`, but Admin API couldn't:

- v3 client requests had no education organization field, so every client got the Application's full list.
- Every Application edit (v2 and v3) relinked every client to every education organization. That silently widened a client narrowed by direct SQL or other tooling.
- `POST /v3/applications` always created a full-access default client, which SEAs then had to delete.

The only workaround was one Application per vendor-and-LEA pair.

## What changed for API callers

### v3

- **Client narrowing.** `POST /v3/apiClients` and `PUT /v3/apiClients/{id}` accept two optional fields:
  - `educationOrganizationIds`: the education organizations the client is limited to. Each one must be on the client's Application.
  - `inheritsApplicationEducationOrganizations`: `true` means the client follows the Application's list, including future changes. `false` means it's narrowed to `educationOrganizationIds`.

  GET returns both fields. `educationOrganizationIds` is the client's effective list and is never `null`; `[]` means no education organization access.
- **The rules.** "The Application's list" means the target Application's list on a move.

  | Request contains | Result |
  | --- | --- |
  | A list, no flag | Narrowed to that list. |
  | `false` with a list | Narrowed to that list. |
  | `true`, no list | Inherits. |
  | `true` with a list equal to the Application's list | Inherits; the list is ignored. |
  | `true` with any other list | 400 |
  | `false` without a list | 400 |
  | `[]` | 400 |
  | An ID not on the Application | 400 |
  | Neither field, POST | Inherits. |
  | Neither field, PUT, same Application | Keeps the current flag and list. |
  | Neither field, PUT that moves the client | Inheriting: takes the new Application's list. Narrowed: keeps its list if it fits; otherwise 409 `conflict:education-organization-scope`. |

  Duplicate IDs are removed silently. Every 400 here is `bad-request:data`, with the error on the field concerned.
- **Moves.** `PUT /v3/apiClients/{id}` with a different `applicationId` moves the client. Its links are replaced in the same save, and its `User` is reset from the target Application's vendor.
- **Default-client opt-out.** `POST /v3/applications` accepts `createDefaultCredentials` (default `true`).
  - With `false`, the Application and its education organization rows are created, but no client.
  - `key` and `secret` are returned as `null`.
  - Sending `dataStoreIds` or `enabled` with `false` returns 400 `bad-request:data`.
- **Application edits apply only differences.**
  - Removed education organizations are removed from every client.
  - Added ones go only to inheriting clients.
  - Unchanged rows and narrowed clients' links to them are left alone.
  - Every inheriting client is re-linked to every one of the Application's rows, which repairs drift.
  - The edit works for Applications with zero clients.

### v2

v2 doesn't expose narrowing, but it no longer undoes it:

- `PUT /v2/applications/{id}` uses the same diff as v3.
- New v2 clients inherit.
- `PUT /v2/applications/{id}/reset-credential` returns 409, not 500, for an Application with no clients. v3 can now create those.

### Error responses (v3 only)

| Case | Before | Now |
| --- | --- | --- |
| Vendor, profile, Application or data store in a request body not found | 400 | 409 `urn:ed-fi:management-api:conflict:unresolved-reference` |
| Duplicate client name in an Application | 400 on POST; not checked on PUT | 409 `urn:ed-fi:management-api:conflict`, on POST and on PUT when the client moves or is renamed |
| Narrowed client doesn't fit the Application it's moved to | n/a | 409 `urn:ed-fi:management-api:conflict:education-organization-scope` |
| `AdminApiException` with status 409 (for example Application-level reset) | `type: …:bad-request` | `type: urn:ed-fi:management-api:conflict` |
| Education organization rule violations, and `createDefaultCredentials` combinations | n/a | 400 `urn:ed-fi:management-api:bad-request:data`, with `validationErrors` keyed by the field |

A missing resource in the route (`/apiClients/{id}`) is still 404. The 502 and partial-sync responses in the Management API spec apply only to the Configuration Service. Admin API has no identity provider to sync, because ODS/API reads client education organizations from `EdFi_Admin` when it issues a token.

## Database: migration `00010`

`00010-AddInheritsApplicationEducationOrganizationsToApiClients.sql`, for SQL Server and PostgreSQL. It is in both `Application/EdFi.Ods.AdminApi/Artifacts/` and `Application/EdFi.Ods.AdminApi.V3/Artifacts/`, and the two copies are byte-identical.

- **Adds** `dbo.ApiClients.InheritsApplicationEducationOrganizations`: `BIT`/`boolean`, `NOT NULL`, default `1`/`true`.
- **Checks first.** If the column already exists (for example, because Ed-Fi-ODS ships it later), the script creates nothing and doesn't touch values. Re-running it is harmless.
- **Backfills only when it created the column.** A client becomes `false` (narrowed) if it isn't linked to every education organization on its Application. Every other client stays `true`. That includes clients whose Application has no education organizations, and clients with no Application. No client's access changes on upgrade.
  - **No-link clients are narrowed.** Before ADMINAPI-1514 (#468), credentials created through `POST /apiClients` got no links. Marking them inheriting would have given them the Application's full list on the next Application edit, a widening nobody chose for that client. Until that edit, GET would also have returned `true` with `[]`, which contradicts the spec. They now keep no access until someone sets them to inherit with `PUT /v3/apiClients/{id}`. The migration guides show how to find them. (Changed after PR review.)
  - **Known limitation:** a client deliberately narrowed to every current education organization is marked inheriting.
- **Atomic.** On SQL Server, the column and the backfill run in one transaction (`SET XACT_ABORT ON`). The PostgreSQL `DO` block is atomic already. A failed backfill leaves no column, so a rerun starts over instead of skipping the backfill.
- **Older writers keep working.** Admin App, older Admin API versions and v2 don't know the column; their inserts get the default (inheriting), which matches what they did before. ODS/API's token lookup doesn't read the column.

### Required upgrade step

Fresh installs and new Docker volumes get `00010` automatically. **Existing databases don't:** Docker DB images run init scripts only on a new volume.

Before deploying this release, apply `00010` to **every** `EdFi_Admin` (each tenant's, in multi-tenant installs), for example with `eng/run-dbup-migrations.ps1`. Until the column exists, every API client and Application endpoint fails; the code doesn't tolerate a missing column. The migration guides (`docs/migration-guide*.md`) describe this, and how to find and repair clients with no links.

Ed-Fi-ODS should know about this column, so that a future `EdFi_Admin` script or `EdFi.Admin.DataAccess` property uses the same name, type and default.

## How it's built

### Mapping a column the package doesn't know

`EdFi.Suite3.Admin.DataAccess` has no property for the new column, so `EdFi.Ods.AdminApi.Common/Infrastructure/Database/` maps it as an EF **shadow property**:

- `AdminApiSqlServerUsersContext` and `AdminApiPostgresUsersContext` subclass the package contexts. They map the property with an explicit column name per engine (`ApiClientShadowProperties.Configure`).
- **There's no `HasDefaultValue`.** EF would treat `false` as "not set" and let the database default win, so a narrowed client could never be saved.
- **New clients default to inheriting** through a `ChangeTracker.Tracked` hook (`DefaultNewApiClientsToInheriting`). It sets `true` on every newly added `ApiClient`, whichever path creates it. A later explicit `false` wins.
- **Access goes through `ApiClientInheritanceExtensions`.** It has single-client get/set and a bulk get by IDs. It throws if the context isn't one of the subclasses. The single-client get reads the tracked entry, so callers must pass a tracked client.
- **Every non-V1 construction site uses the subclasses:** `WebApplicationBuilderExtensions` (single-tenant, both engines) and both `TenantSpecificDbContextProvider`s (v2 and V3). V1 is unchanged.

If a future `EdFi.Admin.DataAccess` release adds a real `ApiClient.InheritsApplicationEducationOrganizations` property, replace the shadow property with it and drop the extensions.

### Client scope

`ClientEducationOrganizationScopeResolver.Resolve` (V3, `Infrastructure/Database/Commands/`) is a pure function. It implements the rules table and returns `ClientEducationOrganizationScope(Inherits, EducationOrganizationIds)`, or throws `BadRequestDataException` / `ConflictException`.

- **`AddApiClientCommand`** links only the target Application's rows that match the scope, then sets the flag.
- **`EditApiClientCommand`:**
  - It runs every check that can fail before changing anything: missing target, duplicate name, and the resolver.
  - In one `SaveChanges`, it then clears the client's links, re-points the Application and `User` on a move, links the matching target rows, and sets the flag.
  - Links only ever come from the target Application's own rows. `EdFi_Admin` doesn't enforce that, so the command does.
- **GET queries** (`GetApiClientByIdQuery`, `GetApiClientsByApplicationIdQuery`) now `Include` the client's education organization rows. Before this change, they weren't loaded.

### Application edits

`ApplicationEducationOrganizationSync.Apply` (Common) is shared by the v2 and v3 `EditApplicationCommand`s:

- It removes rows that are no longer requested. Their join rows are cleared explicitly first, so it doesn't rely on FK cascade behavior.
- It adds new rows linked only to inheriting clients.
- It re-links inheriting clients to every row.

### v3 errors

These live in `EdFi.Ods.AdminApi.V3/Infrastructure/ErrorHandling/`:

- **`AdminApiProblemTypes`** gains `Conflict`, `ConflictUnresolvedReference` and `ConflictEducationOrganizationScope`.
- **`ConflictException`** (an `AdminApiException`, status 409) carries its problem type.
- **`BadRequestDataException`** carries the field name.
- **`UnresolvedReferenceGuard`** replaces `EntityReferenceValidator` in the v3 handlers. v2 keeps `EntityReferenceValidator` and its 400s.
- **`V3RequestErrorMiddleware`:**
  - maps both new exceptions;
  - maps any `AdminApiException` with status 409 to `…:conflict`;
  - writes `bad-request:data` field errors in `validationErrors` / `errors` (`V3ProblemDetailsFactory.CreateData`).
- **The duplicate-name check on POST** moved from the FluentValidation validator into the handler, so it can return 409.
- **`display409`** is enabled on `POST`/`PUT /v3/apiClients` and `PUT /v3/applications/{id}`.

## Tests and verification

- **Unit tests:** the resolver (one test per rule), the error middleware, the shadow property and hook, the commands on InMemory contexts, the `createDefaultCredentials` validator and handler, and v2 reset.
- **SQL Server DB tests:**
  - migration `00010`: column shape, backfill (full, partial and no-link clients, and an Application with no education organizations), skip when the column exists, re-run, and rollback when the backfill fails;
  - a narrowed client round-tripping `false`;
  - GET returning junction rows;
  - client moves;
  - v2 and v3 Application edits preserving narrowing and removing join rows.
- **E2E (Bruno):** `v3/ApiClient Education Organizations` covers the SEA flow end to end. Existing ApiClient and Application suites were updated for the 409 changes. `docs/http/apiclient-education-organizations.http` has the same flow, plus a move, for manual use.
- **Not yet verified:**
  - Migration `00010` and the PostgreSQL context have not been run against PostgreSQL.
  - The Bruno suites have not been run against a live environment.

  Run both before release.
