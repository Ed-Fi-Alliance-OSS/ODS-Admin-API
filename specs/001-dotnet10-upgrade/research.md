# Research: .NET 10 Upgrade — ODS Admin API

**Branch**: `001-dotnet10-upgrade` | **Date**: 2025-07-14
**Purpose**: Resolve all NEEDS CLARIFICATION items from the Technical Context before Phase 1 design.

---

## 1. Docker Base Image Tags

### Decision
Use `mcr.microsoft.com/dotnet/sdk:10.0.202-alpine3.23` and `mcr.microsoft.com/dotnet/aspnet:10.0.6-alpine3.23` exactly as specified in FR-009 and FR-010.

### Rationale
Both tags are confirmed **published and available** on the Microsoft Container Registry as of July 2025. `10.0.202` is the latest stable .NET 10 SDK patch and `10.0.6` is the latest stable ASP.NET Core runtime patch, both on Alpine 3.23 (the latest Alpine variant for .NET 10).

### Alternatives Considered
- Floating tags (e.g., `10.0`) — rejected: spec requires immutable, pinned tags (FR-009/FR-010 edge case note).
- SHA-pinned digests — not required by spec; can be added as a follow-up hardening task.

---

## 2. NuGet Package Compatibility

### Decision Matrix

| Package | Current | Target | Compatible at Target? | Action |
|---------|---------|--------|-----------------------|--------|
| `Microsoft.EntityFrameworkCore.*` (6 packages) | 8.0.x | 10.0.6 | ✅ Yes | Upgrade per spec FR-002 |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.25 | 10.0.6 | ✅ Yes | Upgrade per spec FR-003 |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.4 | 10.0.1 | ✅ Yes | Upgrade per spec FR-004 |
| `EFCore.NamingConventions` | 8.0.3 | 10.0.1 | ✅ Yes | Upgrade per spec FR-005 |
| `Npgsql` | 8.0.6 | 10.0.2 | ✅ Yes | Upgrade per spec FR-006 |
| `Asp.Versioning.Http` | 8.1.1 | Stay 8.1.1 | ✅ Yes (runtime-compat) | Retain per spec FR-007 |
| `Microsoft.Extensions.Logging.Log4Net.AspNetCore` | 8.0.0 | Stay 8.0.0 | ✅ Yes (runtime-compat) | Retain per spec FR-008 |
| **`Quartz`** | **3.15.1** | **4.x** | **❌ No — UPGRADE REQUIRED** | **Gap: not in original spec** |
| **`Quartz.Extensions.Hosting`** | **3.15.1** | **4.x** | **❌ No — UPGRADE REQUIRED** | **Gap: not in original spec** |
| **`Swashbuckle.AspNetCore`** | **7.1.0** | **10.2.0** | **❌ No — UPGRADE REQUIRED** | **Gap: not in original spec** |
| **`Swashbuckle.AspNetCore.Annotations`** | **7.1.0** | **10.2.0** | **❌ No — UPGRADE REQUIRED** | **Gap: not in original spec** |
| **`Swashbuckle.AspNetCore.Filters`** | **8.0.2** | **10.2.0** | **❌ No — UPGRADE REQUIRED** | **Gap: not in original spec** |
| `OpenIddict.AspNetCore` | 4.10.1 | Stay 4.10.1 | ✅ Yes (targets net8;net9;net10) | No change needed |
| `OpenIddict.EntityFrameworkCore` | 4.10.1 | Stay 4.10.1 | ✅ Yes (targets net8;net9;net10) | No change needed |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | Stay 9.0.0 | ✅ Yes | No change needed |
| `AspNetCore.HealthChecks.SqlServer` | 9.0.0 | Stay 9.0.0 | ✅ Yes | No change needed |
| `FluentValidation` | 11.11.0 | Stay 11.11.0 | ✅ Yes | No change needed |
| `FluentValidation.AspNetCore` | 11.3.1 | Stay 11.3.1 | ✅ Yes | No change needed |
| `Microsoft.Extensions.Caching.Memory` | 10.0.5 | Stay 10.0.5 | ✅ Yes (already v10) | No change needed |
| `System.Text.Json` | 9.0.0 | Stay 9.0.0 | ✅ Yes | Optional upgrade to 10.x; deferred |
| `NJsonSchema` | 11.0.2 | Stay 11.0.2 | ✅ Yes | No change needed |
| `Azure.Identity` | 1.12.0 | Stay 1.12.0 | ✅ Yes | No change needed |

### Rationale for Quartz Upgrade (gap not in spec)
Quartz 3.x does not include a `net10.0` TFM in its NuGet package. Building a `net10.0` project that
references Quartz 3.15.1 will fail at restore/build time with a `NU1202` no-compatible-framework
error. Quartz 4.0.0 added `net10.0` targeting. Quartz 4.x is API-compatible with 3.x for standard
job/trigger registration patterns used in this project.

**Recommended version**: Quartz 4.0.0 (or latest 4.x stable).
Verify: Check that the Quartz `IJob` implementation, `ISchedulerFactory`, and background service
wiring in `EdFi.Ods.AdminApi` compile and function correctly against Quartz 4.x API.

### Rationale for Swashbuckle Upgrade (gap not in spec)
ASP.NET Core 9 and 10 ship with built-in OpenAPI document generation
(`Microsoft.AspNetCore.OpenApi`). When a project targets `net10.0`, the SDK automatically injects
implicit OpenAPI generation unless explicitly disabled. Swashbuckle 7.x was written for .NET 8 and
does not declare a `net10.0` TFM; it can cause:
- Duplicate endpoint metadata registration
- Conflicting OpenAPI document routes
- Package restore warnings treated as errors

**Decision**: Upgrade to `Swashbuckle.AspNetCore` 10.2.0 (and matching Annotations + Filters
packages) which explicitly targets `net10.0` and is designed to coexist with the built-in OpenAPI
middleware. Additionally, add `<DisableImplicitOpenApiGeneration>true</DisableImplicitOpenApiGeneration>`
to the main project file to prevent the SDK from injecting the built-in generator alongside
Swashbuckle.

### Alternatives Considered for Swashbuckle
- Migrate to ASP.NET Core built-in OpenAPI: High risk; requires rewriting all Swagger annotations
  (`[SwaggerOperation]`, `[SwaggerResponse]`, filters). Out of scope for this upgrade.
- Keep Swashbuckle 7.x and suppress: The package lacks `net10.0` TFM, causing restore errors that
  cannot be silenced globally without masking real issues.

---

## 3. .NET 8 → .NET 10 Breaking Changes (Relevant to this Project)

### Priority 1 — Must Verify Before Merge

| # | Area | Risk | Description | Mitigation |
|---|------|------|-------------|------------|
| 1 | **EF Core nullable annotations** | ⚠️ Medium | EF Core 9 changed how struct-type complex properties are treated for nullability. Upgrading from 8.0.x to 10.0.6 may emit new nullable warnings on entity models. | Run `dotnet build` and review WARN CA-xxxx/CS8618 on entity types. Fix with `= null!` or `?` where appropriate. |
| 2 | **API obsoletions (SYSLIB)** | ⚠️ Medium | ~130+ BCL APIs marked obsolete with custom SYSLIB diagnostic IDs in .NET 9. If `TreatWarningsAsErrors` is set, these become build errors. | Run build and resolve SYSLIB0001–SYSLIB0060 warnings. `BinaryFormatter` removed entirely. |
| 3 | **Swashbuckle / built-in OpenAPI conflict** | ❌ High | ASP.NET Core 9+ implicit OpenAPI generation conflicts with Swashbuckle 7.x. | Upgrade Swashbuckle to 10.2.0 + set `DisableImplicitOpenApiGeneration=true`. |
| 4 | **Quartz 3.x TFM missing** | ❌ High | Quartz 3.x has no `net10.0` TFM; restore fails. | Upgrade to Quartz 4.x. |

### Priority 2 — Validate During Testing

| # | Area | Risk | Description | Mitigation |
|---|------|------|-------------|------------|
| 5 | **EF Core 9 migration model changes** | ⚠️ Medium | Complex property nullability changes in EF 9 may produce unexpected `Add-Migration` output. | After upgrade, run `dotnet ef migrations add TestMigration --project ... --startup-project ...` and verify the generated migration is empty (no changes). Then delete the test migration. |
| 6 | **JwtBearer stricter validation** | ℹ️ Low | .NET 9 JwtBearer has stricter token format validation. | Run full auth flow integration test with both self-contained and Keycloak modes. |
| 7 | **HttpClientFactory header redaction** | ℹ️ Info | HTTP client logs now redact auth headers by default in .NET 9. | Behaviour change is safer; no action required unless debug tooling depends on logged headers. |
| 8 | **StringValues implicit conversion** | ℹ️ Low | Overload resolution changes may affect implicit `StringValues` casts in header handling. | Run unit tests; fix explicit casts if needed. |

### Priority 3 — Informational

| # | Area | Description |
|---|------|-------------|
| 9 | `System.Linq.AsyncEnumerable` built-in | Now included in .NET 10 core libraries. No package conflict in this codebase (package not referenced). |
| 10 | Configuration null preservation | .NET 10 preserves null values in config. Low risk for this project's `appsettings.json` patterns. |
| 11 | C# 13 overload resolution | `params` span-type overload preference change. Unlikely to affect this codebase's patterns. |

---

## 4. Scope Boundary Confirmation

The following areas were evaluated and confirmed **out of scope**:

- **Database schema changes**: None. EF Core upgrade must produce an empty diff migration.
- **API contract changes**: None. All endpoints, request/response DTOs, and authentication
  protocols are unchanged.
- **New feature work**: None. This is a pure infrastructure upgrade.
- **Multi-framework targeting**: Not in scope. Clean single-framework target: `net10.0`.
- **Mobile/WASM support**: Not applicable.

---

## 5. Documentation Delta

| File | Current Reference | Required Update |
|------|------------------|-----------------|
| `docs/developer.md` line 21 | `.NET 8.0 SDK` download link | Change to `.NET 10.0 SDK` with link to `https://dotnet.microsoft.com/download/dotnet/10.0` |
| `docs/developer.md` line 38 | ".NET 8.0 SDK or newer is installed" | Change to ".NET 10.0 SDK or newer is installed" |
| `AGENTS.md` | No version references found | Review and update if any SDK/framework version guidance is added in future; no current changes needed |
| `.specify/memory/constitution.md` | "Primary runtime MUST remain .NET 8" | Post-merge: update to `net10.0`; document governance amendment |

---

## Summary: All Clarifications Resolved

All NEEDS CLARIFICATION items from Technical Context are resolved. No blockers. Proceed to Phase 1.
