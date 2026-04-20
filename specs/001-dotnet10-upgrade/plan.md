# Implementation Plan: Upgrade ODS Admin API from .NET 8 to .NET 10

**Branch**: `001-dotnet10-upgrade` | **Date**: 2025-07-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-dotnet10-upgrade/spec.md`

## Summary

Upgrade the entire Ed-Fi ODS Admin API solution from .NET 8 to .NET 10 by updating all 10
`.csproj` target framework monikers, the central `Directory.Packages.props` NuGet version pins,
all 4 application Dockerfiles, and 2 hardcoded path strings in `build.ps1`. Research identified
two additional package upgrades not in the original spec scope: **Quartz 3.15.1 → 4.x** (no
`.NET 10` TFM in 3.x) and **Swashbuckle 7.1.0 → 10.2.0** (conflict with ASP.NET Core 10's
built-in OpenAPI support). All other packages are compatible at current or target versions.
Docker image tags specified in the spec are confirmed published and current.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10.0 (upgrading from C# 12 / .NET 8.0)
**Primary Dependencies**: ASP.NET Core 10 minimal API, EF Core 10.0.6, Npgsql 10.0.2, OpenIddict
4.10.1, Swashbuckle 10.2.0 (upgrade required), Quartz 4.x (upgrade required), FluentValidation 11.x
**Storage**: PostgreSQL (primary) + MSSQL (supported) via EF Core contexts (`IUsersContext`,
`ISecurityContext`)
**Testing**: NUnit 4.1.0 + Shouldly 4.2.1 + FakeItEasy 9.0.1; DB/integration tests via `*.DBTests`
projects
**Target Platform**: Linux/Alpine containers (Docker) + Windows developer machines
**Project Type**: Web service (ASP.NET Core minimal API)
**Performance Goals**: Identical to .NET 8 baseline; containerised health checks must pass within
same time budget (SC-004)
**Constraints**: Zero breaking changes to public API surface, request/response contracts, or
database schema; no new migration assets
**Scale/Scope**: 10 `.csproj` files, 4 root-level application Dockerfiles, 1
`Directory.Packages.props`, 1 `build.ps1`, 2 documentation files (`docs/developer.md`,
`AGENTS.md`)

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Pre-Design Gate (Phase 0 entry)

| Gate | Status | Notes |
|------|--------|-------|
| **Architecture gate** | ✅ PASS | Horizontal infrastructure change only; no alterations to `IFeature` boundaries, `IUsersContext`/`ISecurityContext` contexts, or domain logic. |
| **Style & contract gate** | ✅ PASS | No API surface or nullability contract changes expected. EF Core 10 nullable annotation analysis may surface minor fixes (non-blocking). |
| **Test gate** | ✅ PASS | All existing unit tests (`UnitTest` command) and DB/integration tests must pass post-upgrade. No new test coverage required for mechanical version bumps. |
| **Migration gate** | ✅ PASS | No new migration assets. EF Core 8 → 10.0.6 must be validated to produce identical query/migration behaviour. |
| **Documentation gate** | ⚠️ REQUIRED | `docs/developer.md` lines 21 and 38 reference `.NET 8.0 SDK`. Must update to `.NET 10.0 SDK`. `AGENTS.md` has no version references currently; review still required per CA-005. |

### Constitution Exception — .NET Runtime Constraint

The constitution (§ Technology & Runtime Constraints) states: *"Primary runtime MUST remain .NET 8
unless a governance amendment approves a platform migration."*

**Resolution**: This specification is the governance amendment. The upgrade scope is explicitly
approved via this spec. The exception is documented here per governance policy (§ Governance:
"Exceptions MUST be documented with rationale and an approved follow-up action").

- **Rationale**: .NET 8 reaches end of standard support in November 2026. .NET 10 is the next LTS
  release. Upgrading ensures continued security patches and framework support.
- **Follow-up action**: Update the constitution after this feature merges to replace "net8.0" with
  "net10.0" in the Technology & Runtime Constraints section.

### Post-Design Gate (Phase 1 exit)

| Gate | Status | Notes |
|------|--------|-------|
| **Architecture gate** | ✅ PASS | data-model.md confirms no new entities, context crossings, or structural changes. |
| **Style & contract gate** | ✅ PASS | contracts/ directory intentionally empty — no public API surface changes. Quickstart documents nullable annotation mitigations if needed. |
| **Test gate** | ✅ PASS | Quartz 4.x is API-compatible with 3.x for standard use; existing job tests require re-run, not rewrite. |
| **Migration gate** | ✅ PASS | EF Core null-tracking changes documented in data-model.md; no new migration files required. |
| **Documentation gate** | ✅ REQUIRED | quickstart.md lists doc update steps. tasks.md must include documentation update tasks. |

---

## Project Structure

### Documentation (this feature)

```text
specs/001-dotnet10-upgrade/
├── plan.md              # This file
├── research.md          # Phase 0 output — package compatibility & Docker tag verification
├── data-model.md        # Phase 1 output — EF Core model compatibility analysis
├── quickstart.md        # Phase 1 output — developer setup guide for .NET 10
└── tasks.md             # Phase 2 output — /speckit.tasks command (NOT created by /speckit.plan)
```

> No `contracts/` directory is generated: this upgrade introduces zero changes to the public API
> surface, request/response schemas, or authentication protocols.

### Source Code (affected files)

```text
Application/
├── Directory.Packages.props                  # Central NuGet version pins — 8 package changes
├── EdFi.Ods.AdminApi/
│   └── EdFi.Ods.AdminApi.csproj             # net8.0 → net10.0
├── EdFi.Ods.AdminApi.Common/
│   └── EdFi.Ods.AdminApi.Common.csproj      # net8.0 → net10.0
├── EdFi.Ods.AdminApi.Common.UnitTests/
│   └── *.csproj                             # net8.0 → net10.0
├── EdFi.Ods.AdminApi.DBTests/
│   └── EdFi.Ods.AdminApi.DBTests.csproj     # net8.0 → net10.0
├── EdFi.Ods.AdminApi.InstanceManagement/
│   └── *.csproj                             # net8.0 → net10.0
├── EdFi.Ods.AdminApi.InstanceManagement.UnitTests/
│   └── *.csproj                             # net8.0 → net10.0
├── EdFi.Ods.AdminApi.UnitTests/
│   └── *.csproj                             # net8.0 → net10.0
├── EdFi.Ods.AdminApi.V1/
│   └── EdFi.Ods.AdminApi.V1.csproj          # net8.0 → net10.0
├── EdFi.Ods.AdminApi.V1.DBTests/
│   ├── EdFi.Ods.AdminApi.DBTests.csproj     # net8.0 → net10.0 (duplicate name, V1 folder)
│   └── EdFi.Ods.AdminApi.V1.DBTests.csproj  # net8.0 → net10.0
Docker/
├── api.pgsql.Dockerfile                     # aspnet runtime image: 8.0.21 → 10.0.6-alpine3.23
├── api.mssql.Dockerfile                     # aspnet runtime image: 8.0.21 → 10.0.6-alpine3.23
├── dev.pgsql.Dockerfile                     # sdk: 8.0.415 → 10.0.202-alpine3.23 + runtime
└── dev.mssql.Dockerfile                     # sdk: 8.0.415 → 10.0.202-alpine3.23 + runtime
build.ps1                                    # 2 occurrences: net8.0 → net10.0 (lines 219, 550)
docs/developer.md                            # .NET 8.0 SDK refs (lines 21, 38) → .NET 10.0
AGENTS.md                                    # Review for SDK/framework version references
```

**Structure Decision**: Single-solution, existing structure. No new projects, directories, or source
files are introduced. All changes are targeted edits to existing files.

---

## Complexity Tracking

> Constitution exception documented above (runtime constraint). No other gate violations.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| Runtime upgrade (.NET 8 → .NET 10) violates "Primary runtime MUST remain .NET 8" | .NET 8 standard support ends Nov 2026; .NET 10 is next LTS | Staying on .NET 8 leaves the project on an unsupported runtime within the planning horizon |

---

## Phase 0: Research Findings

> See [research.md](./research.md) for full details. All NEEDS CLARIFICATION items resolved.

### Resolved Unknowns

| Question | Decision | Rationale |
|----------|----------|-----------|
| Are Docker image tags in spec published? | ✅ Use as specified | `sdk:10.0.202-alpine3.23` and `aspnet:10.0.6-alpine3.23` confirmed published and current on MCR |
| Is Quartz 3.15.1 compatible with .NET 10? | ❌ MUST upgrade to Quartz 4.x | v3.x does not include `net10.0` TFM; v4.0.0+ adds it. Gap not covered in original spec. |
| Is Swashbuckle 7.1.0 compatible with .NET 10? | ❌ MUST upgrade to 10.2.0+ | ASP.NET Core 10 includes built-in OpenAPI; Swashbuckle 7.x conflicts. v10.2.0 targets `net10.0`. |
| Is OpenIddict 4.10.1 compatible with .NET 10? | ✅ Compatible (verified TFM targets) | Source analysis confirms `net8.0;net9.0;net10.0` targets in v4.10.1 |
| Is EF Core 8 → 10 migration-safe? | ⚠️ Validate; no new migrations expected | Nullable annotation warnings possible; complex property nullability changed in EF 9. Run migrations and diff. |
| Are health check packages compatible? | ✅ No upgrade needed | `AspNetCore.HealthChecks.NpgSql` and `SqlServer` v9.0.0 are runtime-compatible |
| Are FluentValidation packages compatible? | ✅ No upgrade needed | v11.x targets net8.0 and is runtime-compatible with net10.0 apps |

### Spec Version Gaps (additions beyond FR-001 through FR-014)

| Gap | Required Action | Justification |
|-----|----------------|---------------|
| Quartz not mentioned in spec | Upgrade Quartz and Quartz.Extensions.Hosting to 4.x | v3.x has no `net10.0` TFM; build will fail |
| Swashbuckle not mentioned in spec | Upgrade Swashbuckle.AspNetCore*, .Annotations, .Filters to 10.2.0 | Built-in OpenAPI conflict; current v7.x lacks `net10.0` TFM |
| `DisableImplicitOpenApiGeneration` | Add to `EdFi.Ods.AdminApi.csproj` if keeping Swashbuckle | Prevents duplicate schema generation from built-in OpenAPI |

---

## Phase 1: Design Artifacts

### Data Model

> See [data-model.md](./data-model.md) for full details.

This upgrade introduces **no new entities, migrations, or schema changes**. The data-model document
covers:
- EF Core 10.0.6 compatibility notes for existing `IUsersContext` and `ISecurityContext` models
- Nullable annotation delta analysis
- Npgsql 10.0.x type-mapping changes
- Validation of migration behaviour equivalence

### Contracts

**No contract artifacts generated.** The public API surface (endpoints, request/response schemas,
authentication protocols) is unchanged by this upgrade. The existing integration tests serve as the
contract regression suite.

### Quickstart

> See [quickstart.md](./quickstart.md) for full developer setup guide.

Key steps: install .NET 10.0 SDK, run `build.ps1`, validate Docker images, run unit + integration
tests.
