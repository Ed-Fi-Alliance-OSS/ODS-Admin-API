# Feature Specification: Upgrade ODS Admin API from .NET 8 to .NET 10

**Feature Branch**: `001-dotnet10-upgrade`
**Created**: 2025-07-14
**Status**: Draft
**Input**: User description: "Upgrade the ODS Admin API from .NET 8 to .NET 10, updating all project files, NuGet packages, Docker base images, and build scripts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Builds the Application Successfully on .NET 10 (Priority: P1)

A developer clones the repository and builds the entire solution using the standard build script. The build completes without errors and all unit tests pass, confirming that the application compiles and runs correctly under .NET 10.

**Why this priority**: This is the foundational prerequisite for all other work. If the application cannot build on .NET 10, no further validation or deployment is possible.

**Independent Test**: Can be fully tested by running `build.ps1` on a machine with only the .NET 10 SDK installed and verifying a clean build with zero compilation errors and all tests passing.

**Acceptance Scenarios**:

1. **Given** the repository is checked out on a machine with the .NET 10 SDK installed, **When** `build.ps1` is executed, **Then** all 10 projects compile without errors or warnings related to framework targeting.
2. **Given** a successful build, **When** the full test suite is executed, **Then** all previously-passing tests continue to pass with no new failures introduced by the framework upgrade.
3. **Given** the solution is loaded in a development environment, **When** the developer inspects any project file, **Then** the target framework is `net10.0` and no project still references `net8.0`.
4. **Given** the build has completed, **When** the output paths are inspected, **Then** all published artifacts are located under `net10.0` path segments, not `net8.0`.

---

### User Story 2 - Operations Team Deploys the Docker Image Built from .NET 10 Base Images (Priority: P2)

An operations engineer builds the Docker images using the updated Dockerfiles and deploys the Admin API containers. The running containers report the .NET 10 runtime version and the API responds to health checks normally.

**Why this priority**: Docker is the primary deployment mechanism. Confirming the containerised application works on .NET 10 validates the end-to-end production deployment path.

**Independent Test**: Can be fully tested by running `docker build` against each Dockerfile and then starting the resulting container, confirming the application starts and the health endpoint returns a healthy status.

**Acceptance Scenarios**:

1. **Given** a Dockerfile references the updated .NET 10 SDK base image, **When** `docker build` is executed, **Then** the image builds successfully with no base-image-related errors.
2. **Given** a successfully built image, **When** the container is started, **Then** the Admin API process starts without crashes and the application health endpoint returns a healthy response.
3. **Given** all four Dockerfiles have been updated, **When** each is built in isolation, **Then** all four images build without errors.
4. **Given** the running container, **When** its runtime environment is inspected, **Then** the reported runtime version is .NET 10.

---

### User Story 3 - Maintainer Confirms Compatible NuGet Dependencies (Priority: P3)

A maintainer reviews the package manifest and confirms that all packages which have .NET 10-compatible versions have been updated to those versions, and that packages without .NET 10 releases are pinned at their current versions with an explanation.

**Why this priority**: Dependency hygiene is important for security and long-term supportability, but it does not block the core build or deployment stories.

**Independent Test**: Can be fully tested by inspecting `Directory.Packages.props` and verifying each upgraded package version, plus confirming that any package held back is documented with a reason.

**Acceptance Scenarios**:

1. **Given** the updated `Directory.Packages.props`, **When** the EF Core family of packages is inspected, **Then** all `Microsoft.EntityFrameworkCore.*` packages are at version 10.0.6.
2. **Given** the updated package manifest, **When** `Npgsql` and `Npgsql.EntityFrameworkCore.PostgreSQL` are inspected, **Then** they are at versions 10.0.2 and 10.0.1 respectively.
3. **Given** the updated package manifest, **When** `Microsoft.AspNetCore.Authentication.JwtBearer` is inspected, **Then** it is at version 10.0.6.
4. **Given** packages without a .NET 10 release (`Asp.Versioning.Http` at 8.1.1 and `Microsoft.Extensions.Logging.Log4Net.AspNetCore` at 8.0.0), **When** the manifest is reviewed, **Then** these packages remain at their current pinned versions and a comment or note explains why they have not been upgraded.
5. **Given** any deprecated or removed APIs encountered during the upgrade, **When** the build is executed, **Then** all usages have been updated to the equivalent .NET 10 API and no obsolescence warnings remain.

---

### Edge Cases

- What happens when a NuGet package has no .NET 10-compatible release? The package remains at its current version; the spec documents this explicitly and the build must still succeed.
- How does the system handle any breaking API changes introduced between .NET 8 and .NET 10? All breaking changes are identified during the build step and corrected before the spec is considered complete; no build warnings related to obsolete APIs are acceptable in the final state.
- What if a Docker base image tag for .NET 10 is not yet available on the target registry? The upgrade is blocked until the official Microsoft images are published; the Dockerfiles must reference immutable, published tags.
- How does the build script handle path references if the `net10.0` output directory does not exist until after the first build? The `build.ps1` updates are applied before the first build run, so the paths are correct from the first execution.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All 10 `.csproj` project files MUST declare `net10.0` as their target framework, replacing all `net8.0` references.
- **FR-002**: `Directory.Packages.props` MUST pin `Microsoft.EntityFrameworkCore.*` packages (Design, Relational, SqlServer, InMemory, Tools) to version 10.0.6.
- **FR-003**: `Directory.Packages.props` MUST pin `Microsoft.AspNetCore.Authentication.JwtBearer` to version 10.0.6.
- **FR-004**: `Directory.Packages.props` MUST pin `Npgsql.EntityFrameworkCore.PostgreSQL` to version 10.0.1.
- **FR-005**: `Directory.Packages.props` MUST pin `EFCore.NamingConventions` to version 10.0.1.
- **FR-006**: `Directory.Packages.props` MUST pin `Npgsql` to version 10.0.2.
- **FR-007**: `Directory.Packages.props` MUST retain `Asp.Versioning.Http` at version 8.1.1 (no .NET 10 release published).
- **FR-008**: `Directory.Packages.props` MUST retain `Microsoft.Extensions.Logging.Log4Net.AspNetCore` at version 8.0.0 (no .NET 10 release published).
- **FR-009**: All four Dockerfiles MUST reference the .NET 10 SDK base image `mcr.microsoft.com/dotnet/sdk:10.0.202-alpine3.23`.
- **FR-010**: All four Dockerfiles MUST reference the .NET 10 ASP.NET runtime base image `mcr.microsoft.com/dotnet/aspnet:10.0.6-alpine3.23`.
- **FR-011**: `build.ps1` MUST replace both occurrences of the `net8.0` path string with `net10.0`.
- **FR-012**: The full solution MUST compile without errors after all changes are applied.
- **FR-013**: All existing automated tests MUST pass after the upgrade with no new test failures introduced.
- **FR-014**: Any usages of APIs that were removed or made obsolete between .NET 8 and .NET 10 MUST be updated to their .NET 10 equivalents.

### Key Entities

- **Project File (.csproj)**: Declares the target framework moniker for one project. Ten files require updating from `net8.0` to `net10.0`.
- **Central Package Manifest (Directory.Packages.props)**: Single file that pins all NuGet package versions for the solution. Six packages require version upgrades; two packages remain pinned at their current versions.
- **Dockerfile**: Defines the container build process for one deployment variant. Four files require updated base image tags (SDK and runtime).
- **Build Script (build.ps1)**: PowerShell script that orchestrates the build pipeline. Two hardcoded path strings require updating from `net8.0` to `net10.0`.

## Constitution Alignment *(mandatory)*

- **CA-001 Architecture**: The upgrade is a horizontal infrastructure change that touches all projects uniformly. It does not alter the feature-oriented architecture, context boundaries (`IUsersContext` vs `ISecurityContext`), or any domain logic. No new modules or context crossings are introduced.
- **CA-002 Nullability & Contracts**: No changes to API surface, request/response contracts, or nullability annotations are required as part of the framework upgrade itself. If any breaking changes in .NET 10 require nullable annotation updates, those are made defensively and documented individually.
- **CA-003 Test Evidence**: All existing unit tests and integration tests must pass after the upgrade. No new test coverage is required for the mechanical version-bump steps. If any source code changes are needed to address .NET 10 breaking changes, unit tests covering the affected code paths must continue to pass.
- **CA-004 Migrations & Data Impact**: This upgrade does not introduce database schema changes. No new migration assets under `Application/EdFi.Ods.AdminApi/Artifacts/` or changes to `eng/run-dbup-migrations.ps1` are required. The EF Core version bump (8.x → 10.0.6) must be validated to produce identical migration behaviour against existing schemas.
- **CA-005 Documentation Impact**: `AGENTS.md` and `docs/developer.md` must be reviewed and updated wherever they reference the .NET 8 SDK version, Docker base image tags, or `net8.0` path conventions. References to minimum SDK version requirements for contributors must be updated to .NET 10.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The solution builds to completion with zero errors and zero framework-targeting warnings on a machine with only the .NET 10 SDK installed.
- **SC-002**: 100% of automated tests that passed before the upgrade continue to pass after the upgrade, with no new test failures.
- **SC-003**: All four Docker images build successfully from their updated Dockerfiles in a single CI pipeline run.
- **SC-004**: The running containerised application passes all health checks within the same time budget as the .NET 8 baseline.
- **SC-005**: The upgrade is completed with no changes to public API behaviour, request/response contracts, or database schema — verified by running the existing integration test suite against a live database.
- **SC-006**: All packages that have .NET 10-compatible releases are upgraded to those releases; no package is left on a .NET 8-era version when a supported .NET 10 version exists.

## Assumptions

- The .NET 10 SDK (version 10.0.202 or later) is available in the CI/CD build environment and on developer machines at the time this upgrade is applied.
- The Docker base image tags `mcr.microsoft.com/dotnet/sdk:10.0.202-alpine3.23` and `mcr.microsoft.com/dotnet/aspnet:10.0.6-alpine3.23` are published and available on the Microsoft Container Registry.
- `Asp.Versioning.Http` (8.1.1) and `Microsoft.Extensions.Logging.Log4Net.AspNetCore` (8.0.0) are compatible with the .NET 10 runtime despite having no .NET 10-specific release; this compatibility is confirmed before the upgrade is considered complete.
- No third-party packages outside of those listed in the context have .NET 10-incompatible versions that would block the build.
- The upgrade scope is limited to the framework, package, Docker, and build-script changes described; no feature work, API changes, or schema migrations are in scope.
- EF Core 10.0.6 generates migrations and queries that are semantically equivalent to EF Core 8.x for all schemas currently in use.
- Mobile support and multi-framework targeting (`net8.0;net10.0`) are out of scope; the target is a clean single-framework upgrade to `net10.0`.
