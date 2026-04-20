<!--
Sync Impact Report
- Version change: 1.0.0 -> 1.1.0
- Modified principles:
  - III. Mandatory Test Evidence: expanded to cover E2E (Postman) tests and code coverage analysis
  - V. Operational Verifiability and Developer Ergonomics: expanded to include Docker as official
    run target and ODS/API startup-mode alignment requirement
- Added sections:
  - Docker & Deployment Environments (new top-level section)
  - Authentication & Identity (new top-level section)
- Removed sections:
  - None
- Templates requiring updates:
  - ✅ .specify/templates/plan-template.md — constitution check already covers Docker / auth
  - ✅ .specify/templates/spec-template.md — CA-005 doc reference covers new sections
  - ✅ .specify/templates/tasks-template.md — Polish phase already references build.ps1 commands
  - ✅ .specify/templates/commands/*.md — no command files present in .specify/templates/commands/
- Follow-up TODOs:
  - None
-->

# Ed-Fi ODS Admin API Constitution

## Core Principles

### I. Feature-Oriented .NET Architecture
All changes MUST follow the repository's feature-oriented structure and established
language conventions. C# code MUST follow `.editorconfig`, prefer file-scoped
namespaces, single-line `using` directives, newline-before-brace control blocks,
and `nameof` for member-name references.

Each feature MUST implement the `IFeature` interface, defining its own HTTP endpoint
route and encapsulating controller-equivalent logic in a `Handle` function. Features
MUST NOT inherit from the .NET `Controller` class.

Rationale: Consistent structure and style reduce maintenance cost and review
friction across a multi-project .NET solution.

### II. Explicit Nullability and Contract Safety
Public and internal contracts MUST declare non-nullability by default and MUST
validate external inputs at entry points. Null checks MUST use clear language
constructs (`is null`, `is not null`) and avoid ambiguous null handling.
Request validation MUST be implemented with FluentValidation.

Rationale: Contract clarity prevents latent null-reference defects in API and
data-access paths.

### III. Mandatory Test Evidence
Behavior-changing work MUST include automated test evidence before merge.
Unit-level behavior MUST be covered by NUnit tests with Shouldly assertions,
and fake collaborators SHOULD use FakeItEasy when mocking is required.
Database-facing behavior MUST include or update DB/integration tests (projects
named `*.DBTests`) where the change affects query, persistence, migrations, or
context interactions.

All three test suites (unit, integration, E2E via Postman) MUST pass before
merging into the `main` branch. When code coverage analysis is required, use
`build.ps1 -Command UnitTest -RunCoverageAnalysis` or
`build.ps1 -Command IntegrationTest -RunCoverageAnalysis`, which requires
the `dotnet-reportgenerator-globaltool` to be installed globally.

Rationale: This repository supports operational administration workflows, so
change safety depends on executable verification rather than manual confidence.

### IV. Database Boundary and Migration Discipline
Data access responsibilities MUST preserve the existing context boundaries:
`IUsersContext` for `EdFi_Admin` and `ISecurityContext` for `EdFi_Security`.
Schema and migration changes MUST use repository migration assets and scripts
under `Application/EdFi.Ods.AdminApi/Artifacts/` and `eng/run-dbup-migrations.ps1`.

The Admin API's internal management tables reside in `EdFi_Admin` and MUST be
provisioned via the migration scripts before the API can start. Both PostgreSQL
and MSSQL engines are supported; the `engine` parameter of the migration script
selects the target.

Rationale: Boundary integrity protects data correctness and avoids cross-context
coupling that complicates deployment and troubleshooting.

### V. Operational Verifiability and Developer Ergonomics
Every deliverable MUST be runnable and verifiable with documented repository
workflows (`build.ps1` commands, Docker Compose, or Visual Studio launch profiles).
The three supported local run methods are:

- `./build.ps1 run` — command-line launch.
- `Docker/Compose/pgsql/SingleTenant/compose-build-dev.yml` (or the MSSQL/multi-tenant
  equivalents) — container-based launch; handles ODS/API co-startup automatically.
- Visual Studio launch profiles (see `Application/EdFi.Ods.AdminApi/Properties/launchSettings.json`).

When Admin API runs alongside ODS/API, both MUST use the same startup mode
(`sharedinstance`, `yearspecific`, or `districtspecific`) configured via
`AppSettings.ApiStartupType` in `appsettings.json`.

Changes to behavior, architecture, or workflow MUST update relevant docs
(`AGENTS.md` for concise rules, `docs/developer.md` for full procedures) in the
same change.

Rationale: Fast, repeatable local verification and accurate docs reduce release
risk and onboarding time.

## Technology & Runtime Constraints

- Primary runtime MUST remain .NET 10 (net10.0), as approved by governance
  amendment via feature 001-dotnet10-upgrade.
- Feature implementation MUST align with current architecture patterns:
  feature endpoints via `IFeature`, DI-driven handlers via ASP.NET Core DI,
  EF Core contexts (`IUsersContext`, `ISecurityContext`), and FluentValidation.
- `NuGet.config` and `Application/NuGet.Config` MUST NOT be modified unless the
  change request explicitly requires it.
- Security-sensitive behavior (authentication/OIDC, credential handling,
  migration scripts) MUST include explicit verification steps in planning/tasks.
- Both PostgreSQL and MSSQL are supported database engines; changes affecting
  database behavior MUST be validated against both unless the scope explicitly
  limits the target engine.

## Docker & Deployment Environments

The repository ships two families of Docker Compose configurations under
`Docker/Compose/`:

| Variant | Compose file(s) | Use case |
|---|---|---|
| SingleTenant dev | `pgsql/SingleTenant/compose-build-dev.yml` | Local dev/test with latest code |
| SingleTenant binaries | `pgsql/SingleTenant/compose-build-binaries.yml` | Test NuGet-packaged release |
| MultiTenant dev | `pgsql/MultiTenant/compose-build-dev-multi-tenant.yml` | Multi-tenant local dev |
| MultiTenant binaries | `pgsql/MultiTenant/compose-build-binaries-multi-tenant.yml` | Multi-tenant release test |
| IDP (Keycloak) variants | `*-idp-dev*` / `*-idp-binaries*` | Any above + Keycloak |
| MSSQL | `mssql/` equivalents | SQL Server targets |

Docker images MUST be regenerated via
`./build.ps1 -Command buildAndDeployToAdminApiDockerContainer` when testing
local code changes inside containers.

Infrastructure topology for the full dev stack: external traffic → nginx (TLS
termination) → adminapi and api services → PGBouncer → `EdFi_Admin` /
`EdFi_Security` / `EdFi_ODS` databases.

A self-signed TLS certificate MUST be generated before first use:
`bash Docker/Settings/ssl/generate-certificate.sh`. The `.env` file (copied
from `.env.example` and never committed) MUST contain a strong, randomly
generated encryption key (`openssl rand -base64 32`).

Multi-tenant configuration is managed through `appsettings.dockertemplate.json`.

Rationale: Containerized workflows are a primary distribution and testing
mechanism; consistent Docker discipline prevents environment drift between
contributors and CI/CD pipelines.

## Authentication & Identity

Admin API supports two authentication modes:

1. **Self-contained (default)**: built-in token issuance via `/connect/register`
   and `/connect/token` endpoints. The signing key and issuer URL are configured
   under `Authentication` in `appsettings.json`. New client registration MUST be
   disabled in production by setting `AllowRegistration: false`.

2. **External IDP (Keycloak/OIDC)**: activated by setting
   `Authentication.UseSelfcontainedAuthorization: false` and providing OIDC
   parameters (`Authority`, `ValidateIssuer`, `RequireHttpsMetadata`,
   `EnableServerCertificateCustomValidationCallback`). In this mode the
   `/connect/register` and `/connect/token` endpoints are unavailable (return
   404) and MUST NOT be referenced in documentation or tooling.

Changes to either authentication path MUST include explicit verification steps
and MUST NOT leave self-registration enabled in test or production configurations
after initial setup.

Rationale: Dual auth support accommodates both standalone deployments and
enterprise IdP integrations; explicit governance prevents credential leakage.

## Delivery Workflow & Quality Gates

- Plans and tasks MUST include a constitution compliance check before
  implementation and before final merge.
- Pull requests MUST provide: scope summary, affected contexts/features, test
  evidence, and documentation deltas when applicable.
- At minimum, run `./build.ps1 -Command UnitTest` for behavior changes.
  Run `./build.ps1 -Command IntegrationTest` when database behavior is impacted.
  Run E2E (Postman) tests using the `Prod` launch profile before merging
  changes that affect API surface or authentication flows.
- Reviewers MUST block merges that violate architecture boundaries, skip
  required tests, or omit migration/doc updates that are required by scope.

## Governance

This constitution supersedes ad hoc workflow guidance for this repository.
Amendments require a documented rationale, impacted-section summary, and
template/runtime guidance synchronization in the same change.

Versioning policy for this constitution uses semantic versioning:
- MAJOR: incompatible governance changes or principle removals/redefinitions.
- MINOR: new principle/section or materially expanded obligations.
- PATCH: clarifications, wording improvements, or typo-level refinements.

Compliance review expectations:
- Every plan MUST pass constitution gates before design and implementation.
- Every pull request review MUST verify principle compliance explicitly.
- Exceptions MUST be documented with rationale and an approved follow-up action.

**Version**: 1.1.0 | **Ratified**: 2026-04-20 | **Last Amended**: 2026-04-20
