---

description: "Task list for Upgrade ODS Admin API from .NET 8 to .NET 10"
---

# Tasks: Upgrade ODS Admin API from .NET 8 to .NET 10

**Input**: Design documents from `/specs/001-dotnet10-upgrade/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: No new test files are required for this mechanical version-bump upgrade. All existing
automated tests (unit + integration/DB) must continue to pass. Verification tasks are included
to confirm build health and test pass-rates at each phase.

**Organization**: Tasks are grouped by user story to enable independent implementation and
validation of each story. The single shared file (`Directory.Packages.props`) is addressed in
the Foundational phase as a blocking prerequisite.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in all task descriptions

---

## Phase 1: Setup

**Purpose**: Confirm the developer environment is ready to execute the upgrade.

- [X] T001 Verify .NET 10.0 SDK (10.0.202 or later) is installed and active: run `dotnet --version` and confirm the output matches 10.0.x before proceeding with any file changes

**Checkpoint**: .NET 10 SDK confirmed — proceed to Foundational phase

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Update the central NuGet version manifest. This single file is a blocking prerequisite
for ALL project files — no `.csproj` can target `net10.0` successfully until compatible package
versions are pinned here.

**⚠️ CRITICAL**: All user story implementation tasks depend on this phase completing successfully

- [X] T002 Update all EF Core package version pins (Microsoft.EntityFrameworkCore.Design, .Relational, .SqlServer, .InMemory, .Tools, and the base package) from 8.0.x to **10.0.6** in `Application/Directory.Packages.props` per FR-002
- [X] T003 Update `Microsoft.AspNetCore.Authentication.JwtBearer` from 8.0.25 to **10.0.6** in `Application/Directory.Packages.props` per FR-003
- [X] T004 Update `Npgsql.EntityFrameworkCore.PostgreSQL` from 8.0.4 to **10.0.1** and `EFCore.NamingConventions` from 8.0.3 to **10.0.1** in `Application/Directory.Packages.props` per FR-004 and FR-005
- [X] T005 Update `Npgsql` from 8.0.6 to **10.0.2** in `Application/Directory.Packages.props` per FR-006
- [X] T006 Upgrade `Quartz` and `Quartz.Extensions.Hosting` from 3.15.1 to the latest stable **4.x** version in `Application/Directory.Packages.props` (research gap: Quartz 3.x has no net10.0 TFM; upgrade required or build will fail with NU1202)
- [X] T007 Upgrade `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Annotations`, and `Swashbuckle.AspNetCore.Filters` to **10.2.0** in `Application/Directory.Packages.props` (research gap: Swashbuckle 7.x lacks net10.0 TFM and conflicts with ASP.NET Core 10 built-in OpenAPI)

**Checkpoint**: `Application/Directory.Packages.props` fully updated — user story tasks can now begin

---

## Phase 3: User Story 1 — Developer Builds Successfully on .NET 10 (Priority: P1) 🎯 MVP

**Goal**: Every project in the solution targets `net10.0`, the main project suppresses the
built-in OpenAPI generator to avoid conflicts with Swashbuckle, the build script references
the correct output paths, the solution compiles cleanly, and all unit tests pass.

**Independent Test**: Run `./build.ps1 -Command build` on a machine with only the .NET 10 SDK
installed — zero errors. Then run `./build.ps1 -Command UnitTest` — zero new failures.

### Implementation for User Story 1

- [X] T008 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi/EdFi.Ods.AdminApi.csproj`; also add `<DisableImplicitOpenApiGeneration>true</DisableImplicitOpenApiGeneration>` to suppress the ASP.NET Core 10 built-in OpenAPI generator (prevents duplicate schema routes alongside Swashbuckle 10.2.0)
- [X] T009 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj`
- [X] T010 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.Common.UnitTests/EdFi.Ods.AdminApi.Common.UnitTests.csproj`
- [X] T011 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.DBTests/EdFi.Ods.AdminApi.DBTests.csproj`
- [X] T012 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.InstanceManagement/EdFi.Ods.AdminApi.InstanceManagement.csproj`
- [X] T013 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.InstanceManagement.UnitTests/EdFi.Ods.AdminApi.InstanceManagement.UnitTests.csproj`
- [X] T014 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.UnitTests/EdFi.Ods.AdminApi.UnitTests.csproj`
- [X] T015 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.V1/EdFi.Ods.AdminApi.V1.csproj`
- [X] T016 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.V1.DBTests/EdFi.Ods.AdminApi.DBTests.csproj` (note: duplicate project name; this is the V1 folder copy)
- [X] T017 [P] [US1] Update `<TargetFramework>` from `net8.0` to `net10.0` in `Application/EdFi.Ods.AdminApi.V1.DBTests/EdFi.Ods.AdminApi.V1.DBTests.csproj`
- [X] T018 [US1] Replace both occurrences of the `net8.0` path string with `net10.0` in `build.ps1` — line 219 (`$dllPath = "./bin/Release/net8.0/EdFi.Ods.AdminApi.dll"`) and line 550 (`$source = "$solutionRoot/EdFi.Ods.AdminApi/bin/Release/net8.0/."`)
- [X] T019 [US1] Run `./build.ps1 -Command build` and resolve ALL compilation errors, API obsoletions (SYSLIB diagnostics), and nullable annotation warnings (CS8618) introduced by the .NET 8 → .NET 10 upgrade; zero errors and zero framework-targeting warnings must remain when complete (per FR-012, FR-014, SC-001)
- [X] T020 [US1] Run `./build.ps1 -Command UnitTest` and confirm all previously-passing tests continue to pass with zero new failures; test output paths must appear under `net10.0` (per FR-013, SC-002)

**Checkpoint**: Solution builds and all unit tests pass on .NET 10 — User Story 1 is complete and independently validated

---

## Phase 4: User Story 2 — Docker Images Built from .NET 10 Base Images (Priority: P2)

**Goal**: All four Dockerfiles reference the official .NET 10 SDK and ASP.NET Core runtime base
images. Each image builds successfully, the running container starts, and the health endpoint
returns a healthy response.

**Independent Test**: Run `docker build -f Docker/<name>.Dockerfile -t <tag> .` for each of the
four Dockerfiles — all four builds complete without errors. Start a container from a built image
and `curl` the health endpoint — HTTP 200 Healthy.

### Implementation for User Story 2

- [X] T021 [P] [US2] Update `Docker/dev.pgsql.Dockerfile`: replace the SDK base image with `mcr.microsoft.com/dotnet/sdk:10.0.202-alpine3.23` and the ASP.NET runtime base image with `mcr.microsoft.com/dotnet/aspnet:10.0.6-alpine3.23` per FR-009 and FR-010
- [X] T022 [P] [US2] Update `Docker/dev.mssql.Dockerfile`: replace the SDK base image with `mcr.microsoft.com/dotnet/sdk:10.0.202-alpine3.23` and the ASP.NET runtime base image with `mcr.microsoft.com/dotnet/aspnet:10.0.6-alpine3.23` per FR-009 and FR-010
- [X] T023 [P] [US2] Update `Docker/api.pgsql.Dockerfile`: replace the ASP.NET runtime base image with `mcr.microsoft.com/dotnet/aspnet:10.0.6-alpine3.23` per FR-010
- [X] T024 [P] [US2] Update `Docker/api.mssql.Dockerfile`: replace the ASP.NET runtime base image with `mcr.microsoft.com/dotnet/aspnet:10.0.6-alpine3.23` per FR-010
- [X] T025 [US2] Build all four Docker images (`docker build -f Docker/dev.pgsql.Dockerfile`, `dev.mssql.Dockerfile`, `api.pgsql.Dockerfile`, `api.mssql.Dockerfile`) and confirm all four build without base-image-related errors; base image pull logs must reference `.NET 10` tags (per SC-003)

**Checkpoint**: All four Docker images build successfully from .NET 10 base images — User Story 2 is complete and independently validated

---

## Phase 5: User Story 3 — Compatible NuGet Dependencies Confirmed (Priority: P3)

**Goal**: The package manifest documents retained packages, all upgraded package versions are
verifiable in `Directory.Packages.props`, EF Core model drift is validated as zero, and the
full DB/integration test suite passes confirming no runtime regressions.

**Independent Test**: Inspect `Application/Directory.Packages.props` — all upgraded packages are
at their target versions, retained packages have explanatory comments. Run EF Core migration
verification — generated migrations are empty. Run `./build.ps1 -Command IntegrationTest` —
100% pass rate.

### Implementation for User Story 3

- [X] T026 [US3] Add inline XML comments in `Application/Directory.Packages.props` for the two retained-version packages explaining why they are not upgraded: `Asp.Versioning.Http` at 8.1.1 (no .NET 10 release published; runtime-compatible) and `Microsoft.Extensions.Logging.Log4Net.AspNetCore` at 8.0.0 (no .NET 10 release published; runtime-compatible) — per FR-007, FR-008, and US3 acceptance scenario 4
- [ ] T027 [US3] Validate EF Core model drift for `UsersContext`: from the `Application/` directory run `dotnet ef migrations add _VerifyNoChanges --project EdFi.Ods.AdminApi --startup-project EdFi.Ods.AdminApi --context UsersContext`; confirm the generated migration file has empty `Up()` and `Down()` methods; then remove it with `dotnet ef migrations remove --project EdFi.Ods.AdminApi --startup-project EdFi.Ods.AdminApi --context UsersContext` (per data-model.md migration discipline checklist)
- [ ] T028 [US3] Validate EF Core model drift for `SecurityContext`: from the `Application/` directory run `dotnet ef migrations add _VerifyNoChanges --project EdFi.Ods.AdminApi --startup-project EdFi.Ods.AdminApi --context SecurityContext`; confirm the generated migration file has empty `Up()` and `Down()` methods; then remove it (per data-model.md migration discipline checklist)
- [ ] T029 [US3] Run `./build.ps1 -Command IntegrationTest` against a live PostgreSQL instance and confirm 100% pass rate for all DB tests (covers Npgsql 10.x type-mapping, EFCore.NamingConventions snake_case column resolution, and Quartz 4.x job scheduling) per SC-005

**Checkpoint**: All packages verified, zero EF Core model drift, integration tests passing — User Story 3 is complete

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation updates and final end-to-end validation across all stories.

- [X] T030 [P] Update `docs/developer.md` line 21 (change `.NET 8.0 SDK` download link to `.NET 10.0 SDK` pointing to `https://dotnet.microsoft.com/download/dotnet/10.0`) and line 38 (change ".NET 8.0 SDK or newer" to ".NET 10.0 SDK or newer") per CA-005 and research.md §5 documentation delta
- [X] T031 [P] Review `AGENTS.md` for any remaining `.NET 8`, `net8.0`, SDK version, or Docker image tag references; update all found references to `.NET 10` equivalents per CA-005
- [X] T032 Update `.specify/memory/constitution.md` §Technology & Runtime Constraints to replace `net8.0` with `net10.0` as the approved primary runtime; add a note referencing this feature branch as the governance amendment per plan.md Constitution Exception
- [ ] T033 Run the quickstart.md validation end-to-end (Steps 1–8: SDK verify → build → unit tests → EF model drift check → integration tests → Docker image builds → container health check → documentation review) to confirm the complete upgrade workflow is reproducible and correct

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **BLOCKS all user story phases**; `Directory.Packages.props` must be complete before any `.csproj` targets `net10.0`
- **User Story 1 (Phase 3)**: Depends on Phase 2 — all .csproj TFM changes, DisableImplicitOpenApiGeneration, and build.ps1 updates
- **User Story 2 (Phase 4)**: Depends on Phase 2 (packages in props needed for Docker build layers) — can run in **parallel with Phase 3** once Phase 2 is complete
- **User Story 3 (Phase 5)**: Depends on Phase 3 (build must succeed before EF migration validation or integration tests can run)
- **Polish (Phase 6)**: Depends on all user story phases being complete

### User Story Dependencies

- **US1 (P1)**: Unblocked after Phase 2 — no dependency on US2 or US3
- **US2 (P2)**: Unblocked after Phase 2 — no dependency on US1 or US3 (Dockerfile changes are file-independent)
- **US3 (P3)**: Depends on US1 (build must succeed for EF migration commands and integration tests to run)

### Within Each User Story

- Phase 3 tasks T008–T017 (all ten `.csproj` files) can be executed in **parallel** (different files)
- Phase 4 tasks T021–T024 (all four Dockerfiles) can be executed in **parallel** (different files)
- T019 (build verification) must follow T008–T018 sequentially
- T020 (unit test run) must follow T019 (build must pass first)
- T025 (Docker build verification) must follow T021–T024
- T027 and T028 (EF Core drift checks) can run in **parallel** (different contexts, different projects)

---

## Parallel Example: User Story 1 — All `.csproj` Updates

```text
# All ten .csproj files can be updated simultaneously:
Task T008: Application/EdFi.Ods.AdminApi/EdFi.Ods.AdminApi.csproj
Task T009: Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj
Task T010: Application/EdFi.Ods.AdminApi.Common.UnitTests/EdFi.Ods.AdminApi.Common.UnitTests.csproj
Task T011: Application/EdFi.Ods.AdminApi.DBTests/EdFi.Ods.AdminApi.DBTests.csproj
Task T012: Application/EdFi.Ods.AdminApi.InstanceManagement/EdFi.Ods.AdminApi.InstanceManagement.csproj
Task T013: Application/EdFi.Ods.AdminApi.InstanceManagement.UnitTests/EdFi.Ods.AdminApi.InstanceManagement.UnitTests.csproj
Task T014: Application/EdFi.Ods.AdminApi.UnitTests/EdFi.Ods.AdminApi.UnitTests.csproj
Task T015: Application/EdFi.Ods.AdminApi.V1/EdFi.Ods.AdminApi.V1.csproj
Task T016: Application/EdFi.Ods.AdminApi.V1.DBTests/EdFi.Ods.AdminApi.DBTests.csproj
Task T017: Application/EdFi.Ods.AdminApi.V1.DBTests/EdFi.Ods.AdminApi.V1.DBTests.csproj
# Then sequentially: T018 (build.ps1) → T019 (build) → T020 (unit tests)
```

## Parallel Example: User Story 2 — All Dockerfile Updates

```text
# All four Dockerfiles can be updated simultaneously:
Task T021: Docker/dev.pgsql.Dockerfile
Task T022: Docker/dev.mssql.Dockerfile
Task T023: Docker/api.pgsql.Dockerfile
Task T024: Docker/api.mssql.Dockerfile
# Then sequentially: T025 (docker build all four)
```

## Parallel Example: User Story 3 — EF Core Model Drift Checks

```text
# Both context checks can run simultaneously (different contexts):
Task T027: UsersContext migration verification
Task T028: SecurityContext migration verification
# Then sequentially: T029 (integration test suite)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T007) — **CRITICAL**
3. Complete Phase 3: User Story 1 (T008–T020)
4. **STOP and VALIDATE**: `./build.ps1 -Command build` passes; `./build.ps1 -Command UnitTest` passes
5. Merge or demo — the application builds and runs on .NET 10

### Incremental Delivery

1. Setup + Foundational (T001–T007) → package manifest ready
2. User Story 1 (T008–T020) → build + unit tests pass on .NET 10 (**MVP**)
3. User Story 2 (T021–T025) → Docker images running on .NET 10
4. User Story 3 (T026–T029) → packages audited, DB tests confirmed
5. Polish (T030–T033) → docs updated, quickstart validated

### Parallel Team Strategy

With two or more developers, once Phase 2 is complete:

- **Developer A**: Phase 3 (US1) — `.csproj` files + build.ps1 + build verification
- **Developer B**: Phase 4 (US2) — Dockerfiles + Docker build verification

User Story 3 and Polish can proceed once US1 completes.

---

## Notes

- `[P]` tasks operate on different files — no write conflicts; safe to run concurrently
- `[Story]` labels map each task directly to its user story acceptance scenarios in spec.md
- No new test files are created by this upgrade; existing test suites serve as the regression suite
- If T019 (build) fails with SYSLIB or CS8618 errors, resolve them within T019 before proceeding; do not skip
- If T027 or T028 (EF Core drift) produces a non-empty migration, **do not commit** — investigate the delta in data-model.md and resolve before the feature is considered complete
- Commit after each phase or logical group of tasks to maintain a clean, bisectable history
- The Quartz 3.x → 4.x upgrade (T006) may require source code changes in `EdFi.Ods.AdminApi` if Quartz 4.x has any API differences from 3.x in the job/scheduler registration patterns used by this project; verify during T019
