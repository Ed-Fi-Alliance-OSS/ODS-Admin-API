# .NET 10 Upgrade Design (ODS Admin API)

## Problem Statement

Upgrade the repository from .NET 8 to .NET 10 with a hard cutover, while preserving existing API and runtime behavior. The migration must cover application projects, package compatibility baselines, build scripts, Docker images, CI/tooling references, installer SDK pinning, and developer documentation.

## Goals

1. Move all `net8.0` application targets to `net10.0`.
2. Keep existing endpoint contracts and runtime behavior unchanged.
3. Update only NuGet packages required for .NET 10 compatibility.
4. Align local/CI/container toolchains with .NET 10.
5. Remove stale .NET 8 references from operational docs and scripts.

## Non-Goals

1. Feature development or endpoint redesign.
2. Refactoring unrelated architecture.
3. Broad package modernization beyond compatibility needs.

## Constraints

1. Hard cutover to .NET 10 only (no temporary dual-targeting).
2. Compatibility-first dependency strategy.
3. No intentional behavior changes in API, auth flow, jobs, or EF data access semantics.

## Recommended Approach

Use a **staged cutover**:

1. Retarget frameworks and update build artifact path assumptions.
2. Align toolchain and runtime substrates (SDK pins, CI SDK setup, Docker images).
3. Apply compatibility-required package updates only.
4. Update docs and workflow references.
5. Validate with existing build/test/integration/container flows.

This sequencing minimizes blast radius while preserving a single hard cutover outcome.

## System Design

### Architecture Boundaries

This is a platform migration with strict boundaries:

- **In scope**: target frameworks, SDK/tooling pinning, Docker runtime/build images, compatibility package versions, script/docs references.
- **Out of scope**: endpoint contract changes, middleware behavior changes, authentication semantics, EF model/query behavior changes, DB migration logic redesign.

### Execution Flow

1. **Framework retargeting**
   - Update `Application\**\*.csproj` from `net8.0` to `net10.0`.
   - Update script path literals that reference framework-specific output folders (e.g., `build.ps1` paths currently pointing to `bin\Release\net8.0`).
2. **Toolchain alignment**
   - Update `Installer.AdminApi\global.json` to .NET 10 SDK baseline.
   - Update CI workflow files where `.NET 8` SDK setup is explicit.
3. **Container runtime alignment**
   - Update Docker build/runtime images from .NET 8 to .NET 10 counterparts with pinned digests.
   - Preserve current container entrypoints and environment behavior.
4. **Dependency compatibility pass**
   - Keep package versions unchanged unless restore/build/test indicates net10 incompatibility.
   - Upgrade only blocking packages and keep changes narrowly scoped.
5. **Documentation alignment**
   - Update docs referencing .NET 8 requirements or net8.0 output paths.

### Error Handling Strategy

1. Preserve existing HTTP exception mapping behavior.
2. Treat compile/test/runtime failures as migration defects; resolve at root cause.
3. Do not suppress warnings broadly to force a green build.

### Testing and Validation Strategy

1. Build using existing build script (`./build.ps1 -Command Build`).
2. Run unit tests (`./build.ps1 -Command UnitTest`).
3. Run integration tests (`./build.ps1 -Command IntegrationTest`).
4. Validate container builds for MSSQL and PostgreSQL Docker variants.
5. Confirm OpenAPI generation pathing after framework folder changes.

## Primary Change Surfaces

1. `Application\**\*.csproj`
2. `build.ps1`
3. `Docker\api.*.Dockerfile`, `Docker\dev.*.Dockerfile`
4. `.github\workflows\*.yml` (where SDK/runtime versions are explicit)
5. `Installer.AdminApi\global.json`
6. `docs\developer.md` and other .NET 8/net8.0 references

## Risks and Mitigations

1. **Package incompatibility under .NET 10**
   - Mitigation: compatibility-only upgrade policy; update only failing dependencies.
2. **Container image drift**
   - Mitigation: use pinned digest images and validate container builds.
3. **Hidden script assumptions on net8.0 output paths**
   - Mitigation: grep-based scan for `net8.0` literals and update all operational path references.
4. **Behavior regressions from transitive dependency shifts**
   - Mitigation: run unit/integration tests and avoid opportunistic major upgrades.

## Success Criteria

1. All application projects compile targeting `net10.0`.
2. Build, unit tests, and integration tests pass via existing repository commands.
3. Docker images build successfully with .NET 10 base images.
4. No intentional API behavior changes introduced.
5. No stale .NET 8 references remain in required operational docs/scripts.

