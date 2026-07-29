# Audit Trail Logging — Design

## Overview

Admin API (V2 and V3) gains a database-backed audit trail so a system
administrator can query authentication and administrative-action events
directly via SQL, without relying on unstructured text logs. This document
describes the technical design; see `task.md` in this folder for the
underlying requirements, scope, and confirmed assumptions.

## Open Question Resolved: log4net vs. direct EF Core write

The repository's log4net setup (`Application/EdFi.Ods.AdminApi/log4net/log4net.config`,
`Application/EdFi.Ods.AdminApi.V3/log4net/log4net.config`) uses only
`ConsoleAppender` and `RollingFileAppender` — there is no ADO.NET appender
anywhere in the codebase today. Meanwhile, both API versions already have an
EF Core `AdminApiDbContext` (Fluent API, Code First against a DbUp-managed
schema) that transparently supports both SQL Server and PostgreSQL.

**Decision: direct EF Core write**, not a log4net `AdoNetAppender`. A new
`AuditLog` entity/table is added the same way any other table is added in
this codebase (DbUp SQL script + EF entity + Fluent config), reusing existing
connection strings and cross-DB support. log4net continues to do exactly
what it does today (text/console logging) and is only touched as a
**fallback** sink when an audit write fails — see Failure Handling below.

## Runtime Topology Note

`EdFi.Ods.AdminApi.V3` is a class library (`OutputType=Library`,
`IsPublishable=false`) referenced by the single `EdFi.Ods.AdminApi`
executable. There is one running process, one `Program.cs`, and one loaded
`appsettings.json` (`Application/EdFi.Ods.AdminApi/appsettings.json`).
`AppSettings:AdminApiMode` selects V1/V2/V3 behavior at startup for that one
instance — a deployment only ever runs one version at a time. Consequently
audit logging needs only a single configuration flag, not one per version.

## Data Model

A single unified table, `AuditLogs`, added to `EdFi_Admin` (each tenant's own
database in multitenant mode — no cross-tenant sharing, no tenant-id column
needed, consistent with the existing per-tenant-database architecture):

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity | Primary key |
| `EventType` | varchar/enum | `AuthenticationSuccess`, `AuthenticationFailure`, `Action` |
| `Timestamp` | datetime2 (UTC) | When the event occurred |
| `ClientId` | nvarchar, nullable | Null when genuinely undeterminable (e.g. malformed/missing credentials) |
| `SourceIpAddress` | nvarchar, nullable | Captured for both authentication and action events |
| `HttpVerb` | nvarchar, nullable | Action events only |
| `HttpUrl` | nvarchar, nullable | Action events only |
| `StatusCode` | int, nullable | Response status code, captured for both event types |

One unified table (rather than separate auth/action tables) was chosen
deliberately, trading a few always-null columns per row for simpler
cross-event-type querying (e.g. "everything around timestamp X for IP Y").

Added via a new EF entity (`AuditLog` class, `DbSet<AuditLog>`, Fluent
configuration in `OnModelCreating`) in each of V2's and V3's
`AdminApiDbContext`, plus a matching numbered DbUp SQL script under both
`Artifacts/MsSql/Structure/Admin/` and `Artifacts/PgSql/Structure/Admin/` for
each project, following the existing 5-digit-sequence naming convention.

## Capture Points

**Action events** (POST/PUT/PATCH/DELETE only — GETs excluded per scope):
new lightweight middleware, registered alongside the existing
`RequestLoggingMiddleware` (V2) / `V3RequestErrorMiddleware` (V3). It
captures client_id (from the authenticated principal), timestamp, verb, URL,
and source IP up front, then after `next()` completes, adds the response
status code and enqueues the event.

**Authentication events**, hooked in the shared
`Application/EdFi.Ods.AdminApi/Infrastructure/Security/SecurityExtensions.cs`:

- `OnTokenValidated` → `AuthenticationSuccess`
- `OnAuthenticationFailed` / `OnChallenge` → `AuthenticationFailure` (covers
  invalid/expired/missing credentials anywhere in the API, not just the
  token endpoint)
- Token-issuance-specific failures (e.g. invalid client secret) are also
  captured in the `/connect/token` path (`TokenService`/`ConnectController`).

## Write Pipeline

Both capture points push an `AuditEvent` onto a bounded
`System.Threading.Channels.Channel<AuditEvent>`. A single hosted
`AuditLogBackgroundService` drains the channel sequentially and writes via
EF Core, using `IDbContextFactory` (the service itself is a singleton, so it
cannot hold a scoped `DbContext` directly).

This keeps the write fully off the request path: enqueueing is a
synchronous, near-zero-cost `TryWrite`, so a slow or unavailable database
never adds latency to, or affects the outcome of, the original request.

## Configuration

A single flag, independent of API version (since only one version runs per
deployed instance):

```json
"AuditLogging": {
  "Enabled": true
}
```

When `false`, the audit middleware is not registered, the authentication
event hooks are no-ops, and the background service does not start — negligible
overhead when disabled.

## Failure Handling

- **Retry**: on a DB write failure, the background service retries up to 2
  times with short backoff (~200ms, ~500ms).
- **Fallback**: if all retries fail, the event is logged via the existing
  log4net text logger as a fallback (rate-limited to avoid flooding the log
  under sustained outages).
- **Fail-open**: the original request's success or failure is never affected
  by audit-write outcomes, at any stage — enqueueing already happens
  independently of the DB write.
- **Backpressure**: if the bounded channel fills under extreme sustained
  load (e.g. long DB outage), new events are dropped and the drop is
  recorded via the log4net fallback rather than growing memory unbounded or
  blocking the request.

## Cross-Cutting Notes

- **Cross-DB support**: reuses the existing `AdminApiDbContext` / EF Core
  provider setup, so SQL Server and PostgreSQL are both supported
  automatically; only the two DbUp SQL scripts are engine-specific.
- **Multitenant mode**: no special handling needed — each tenant's own
  `EdFi_Admin` database gets its own `AuditLogs` table via the same
  per-tenant DB context resolution the rest of the app already uses.
- **No HTTP exposure**: no controller, endpoint, or minimal API route is
  added for this table. The only way to read it is direct SQL access to the
  database.

## Testing

- Unit tests (NUnit + Shouldly + FakeItEasy): action-event middleware (verb
  filtering, status code capture), authentication event hooks, and the
  background service's retry/fallback logic (mock the DB write to throw,
  assert the log4net fallback is invoked only after retries are exhausted).
- Integration/DB tests (existing `*.DBTests` projects): an end-to-end
  request against a real database produces a row with the expected columns,
  verified against both SQL Server and PostgreSQL.
- A test verifying that with `AuditLogging:Enabled = false`, no rows are
  written and no measurable middleware overhead is added.

## Documentation Deliverable

A new doc under `docs/`, linked from `docs/developer.md` per the project's
`CLAUDE.md` convention, covering:

- The `AuditLogging:Enabled` flag and where it lives in `appsettings.json`.
- Exactly which events are captured (authentication success/failure; which
  HTTP verbs count as action events).
- The `AuditLogs` table schema and the meaning/nullability of each column.
- The fail-open behavior (audit failures fall back to the text log, never
  block a request).

## Out of Scope (v1)

- Recording a deleted object's natural key (e.g. an ApiClient's `client_id`)
  beyond what the action event's HTTP URL already captures.
- Retention, rotation, purging, or archival policy for audit records.
- Any admin UI, reporting dashboard, export tooling, or query API for
  browsing audit data.
- Logging of read-only (GET) requests as action events.
