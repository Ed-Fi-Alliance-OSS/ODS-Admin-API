# Audit Trail Logging

Admin API (V2 and V3) can record a database-backed audit trail of
authentication and administrative-action events, so a system administrator
can query them directly via SQL instead of relying on unstructured text
logs.

## Enabling / disabling

Audit logging is controlled by a single flag in `appsettings.json`:

```json
"AuditLogging": {
  "Enabled": true
}
```

This lives at the top level of `appsettings.json` (see
`Application/EdFi.Ods.AdminApi/appsettings.json`), not nested under a
per-version section, and it is **not** duplicated per API version. That's
because Admin API is a single running process with one loaded
`appsettings.json`; `AppSettings:AdminApiMode` selects V1/V2/V3 behavior for
that one instance, and a deployed instance only ever runs one version at a
time. One flag is therefore sufficient to cover whichever version (V2 or V3)
is active.

When `Enabled` is `false`:

* The action-logging middleware (`AuditActionLoggingMiddleware`) is still
  registered in the pipeline, but the recorder it calls into
  (`AuditEventRecorder.Record`) returns immediately without doing any work.
* The authentication event hooks in `SecurityExtensions.cs` call the same
  recorder, so they also become no-ops.
* No rows are written and no background writer activity occurs.

## Captured events

Three event types are recorded, corresponding to the `AuditEventType` enum
(`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEventType.cs`):

| Event type | When it's recorded |
|---|---|
| `AuthenticationSuccess` | A client successfully obtains a token at `/connect/token` (OpenIddict's `ApplyTokenResponseContext`, handled by `SecurityExtensions.DefaultTokenResponseHandler` — recorded when the token response has no `Error`). |
| `AuthenticationFailure` | (a) A token request at `/connect/token` fails (invalid client, invalid grant, invalid scope, etc. — same handler as above, recorded when the token response has an `Error`); or (b) any other request anywhere in the API is rejected with a 401 because the bearer token is missing, malformed, or expired (`JwtBearerEvents.OnChallenge` in `SecurityExtensions.cs`). |
| `Action` | Every request whose HTTP method is `POST`, `PUT`, `PATCH`, or `DELETE`, regardless of outcome. `GET` requests are never logged as action events. Captured by `AuditActionLoggingMiddleware` (`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditActionLoggingMiddleware.cs`), registered in the request pipeline in `Program.cs` (`app.UseMiddleware<AuditActionLoggingMiddleware>();`). |

Note: `JwtBearerEvents.OnAuthenticationFailed` and `OnTokenValidated` also
exist in `SecurityExtensions.cs` for logging/diagnostics, but only
`OnChallenge` (the 401 response path) and the `/connect/token`
`ApplyTokenResponseContext` handler actually write audit rows.

## The `adminapi.AuditLogs` table

The table is created by the DbUp migration script
`Application/EdFi.Ods.AdminApi/Artifacts/MsSql/Structure/Admin/00007-CreateAuditLogs.sql`
(and the equivalent PostgreSQL script under `Artifacts/PgSql/Structure/Admin/`),
and mapped in each API version's `AdminApiDbContext` via the `AuditLog`
entity in `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLog.cs`.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `BIGINT IDENTITY(1,1)` | No | Primary key. |
| `EventType` | `NVARCHAR(30)` | No | One of `AuthenticationSuccess`, `AuthenticationFailure`, `Action`. |
| `Timestamp` | `DATETIME2` | No | UTC time the event was recorded (`DateTime.UtcNow` at the moment `AuditEventRecorder.Record` is called). |
| `ClientId` | `NVARCHAR(100)` | Yes | The OAuth client id. Populated for `/connect/token` events (from the token request) and for `Action` events (from the authenticated principal's `client_id` claim). Always `null` for the `OnChallenge` 401 path, since the caller could not be authenticated. |
| `SourceIpAddress` | `NVARCHAR(45)` | Yes | Populated for all three event types from `HttpContext.Connection.RemoteIpAddress`. |
| `HttpVerb` | `NVARCHAR(10)` | Yes | Populated only for `Action` events (e.g. `POST`, `PUT`, `PATCH`, `DELETE`). Always `null` for both authentication event paths (`/connect/token` and the `OnChallenge` 401 path). |
| `HttpUrl` | `NVARCHAR(2048)` | Yes | Populated only for `Action` events (the request path). Always `null` for authentication events. |
| `StatusCode` | `INT` | Yes | Populated for `Action` events (the actual response status code, or `500` if the request pipeline threw an exception before a status code was set) and for the `OnChallenge` 401 path (always `401`). Always `null` for the `/connect/token` `ApplyTokenResponseContext` handler, since that handler runs before the final HTTP status is finalized. |

Indexes exist on `Timestamp` and `ClientId` to support the most common
lookups (recent events, and events for a given client).

## Write pipeline and fail-open guarantee

Audit logging is designed so that it can never cause the original request to
fail or slow down materially:

1. **Recording** (`AuditEventRecorder.Record`, in
   `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEventRecorder.cs`)
   is synchronous and cheap: it resolves the correct `EdFi_Admin` connection
   string (tenant-specific in multitenant mode, otherwise the default), builds
   an `AuditEvent`, and enqueues it onto a bounded
   `System.Threading.Channels.Channel<AuditEvent>` with `TryWrite`. Any
   exception while resolving the connection string or constructing the event
   is caught and silently swallowed — the record call never throws back into
   request-processing code.
2. **Writing** happens off the request path entirely, in the singleton
   `AuditLogBackgroundService` hosted service
   (`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLogBackgroundService.cs`),
   which drains the channel sequentially and writes each event via EF Core
   (`IAuditLogWriter`, implemented per version as `AdminApiAuditLogWriter` in
   `Application/EdFi.Ods.AdminApi/Infrastructure/Audit/` and
   `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Audit/`).
3. **Retry**: if a write fails, the background service retries up to twice,
   waiting 200ms then 500ms between attempts.
4. **Fallback**: if all attempts (the initial write plus both retries) fail,
   the event is logged via the existing log4net text logger instead
   (`ILog.Error`, including all the event's fields) rather than being
   silently discarded.
5. Because the write happens in a background service, database outages or
   slow writes never add latency to, or affect the success/failure outcome
   of, the original HTTP request.

## Multitenancy

In multitenant mode, `AuditLogs` is not a shared, cross-tenant table — each
tenant's own `EdFi_Admin` database gets its own `AuditLogs` table, created by
the same per-tenant DbUp migration process used for the rest of the schema.
`AuditEventRecorder` resolves the correct tenant connection string from the
current tenant context before enqueueing an event, so events are always
written to the requesting tenant's own database.

## No HTTP exposure

There is no controller, minimal API route, or other HTTP endpoint that
exposes `AuditLogs` data. The only way to read audit records is direct
database access (e.g. `SELECT * FROM adminapi.AuditLogs`) against the
relevant `EdFi_Admin` database.

## Out of scope (current version)

The following are intentionally not part of this feature and may be
addressed in a future iteration:

* Recording a deleted object's natural key (e.g. an `ApiClient`'s
  `client_id`) beyond what the `Action` event's `HttpUrl` already captures.
* Any retention, rotation, purging, or archival policy for audit records —
  rows accumulate indefinitely unless removed by an operator.
* Any admin UI, reporting dashboard, export tooling, or query API for
  browsing audit data.
* Logging of read-only (`GET`) requests as action events.
