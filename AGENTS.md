# AGENTS (summary)

This file is a concise, machine-friendly summary of repository conventions and procedures. For full developer guidance and long-form procedures, see `docs/developer.md` (contents: Build Script, Running on Localhost, Application Architecture, DB migrations, test coverage).

## General

* Make only high-confidence suggestions when reviewing code changes.
* Do not change `NuGet.config` files unless explicitly requested.
* For short tasks, include the section name in the prompt so agents load only that section.
* Keep updates to `AGENTS.md` concise and focused to reduce token usage; put full details, examples and long procedures in `docs/developer.md`.

## Coding & Tests

Concise coding conventions, nullability rules, and testing basics.

* Formatting: follow `.editorconfig`, prefer file-scoped namespaces and single-line `using` directives.
* Control blocks: put a newline before `{` and keep final `return` on its own line.
* Language: prefer pattern matching, switch expressions, and use `nameof` for member names.
* Nullability: declare variables non-nullable where possible; validate at entry points; use `is null` / `is not null`.
* Testing: NUnit + Shouldly for assertions; use FakeItEasy for mocks; mirror existing test naming/style.
* Run tests locally:
  * All unit tests, with rebuild: `./build.ps1 -Command UnitTest`
  * All unit tests, without rebuild: `./build.ps1 -Command UnitTest -NoBuild`
  * Filter to a specific test, without rebuild: `./build.ps1 -Command UnitTest -Filter <?>`
  * See `docs/developer.md` for detailed integration/E2E instructions.
* Editing Bruno scripts for E2E testing: extract IDs from a `Location` header with `.split("/").pop()`, never a hardcoded index (`split("/")[2]`) — a route gaining/losing a path segment silently breaks index-based parsing, and failures then surface as confusing downstream assertion errors rather than at the source.

## Run & Architecture

Short run/build/architecture notes — see `docs/developer.md` for full procedures.

* Build helper: `./build.ps1` (common commands: `build`, `UnitTest`, `IntegrationTest`, `run`).
* Local run options: `build.ps1 run`, Docker compose, or Visual Studio launch profiles.
* DB migrations: scripts and artifacts under `Application/EdFi.Ods.AdminApi/Artifacts/` and `eng/run-dbup-migrations.ps1`. Only this (v2) copy is actually applied by the Docker migration-runner scripts — the `EdFi.Ods.AdminApi.V3/Artifacts/` copy is a docs-only duplicate kept in sync by convention; add new migrations to both, but know that only the v2 copy has any functional effect.
* Architecture: feature-based layout; `IUsersContext` handles `EdFi_Admin`, `ISecurityContext` handles `EdFi_Security` (EF Core); AutoMapper mappings in `AdminApiMappingProfile.cs`.
