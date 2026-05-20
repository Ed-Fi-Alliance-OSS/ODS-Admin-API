## AGENTS (summary)

This file is a concise, machine-friendly summary of repository conventions and procedures. For full developer guidance and long-form procedures, see `docs/developer.md`.

Full developer procedures and examples: `docs/developer.md` (Build Script, Running on Localhost, Application Architecture, DB migrations, test coverage).

### Sections (use these exact headers when referencing)

* `General`
* `Coding & Tests`
* `Run & Architecture`

---

### General

* Make only high-confidence suggestions when reviewing code changes.
* Do not change `NuGet.config` files unless explicitly requested.
* For short tasks, include the section name in the prompt so agents load only that section.
* Keep updates to `AGENTS.md` concise and focused to reduce token usage; put full details, examples and long procedures in `docs/developer.md`.

### Coding & Tests

Concise coding conventions, nullability rules, and testing basics.

* Formatting: follow `.editorconfig`, prefer file-scoped namespaces and single-line `using` directives.
* Control blocks: put a newline before `{` and keep final `return` on its own line.
* Language: prefer pattern matching, switch expressions, and use `nameof` for member names.
* Nullability: declare variables non-nullable where possible; validate at entry points; use `is null` / `is not null`.
* Testing: NUnit + Shouldly for assertions; use FakeItEasy for mocks; mirror existing test naming/style.
* Run tests locally: `./build.ps1 -Command UnitTest` (see `docs/developer.md` for integration/E2E instructions).

### Run & Architecture

Short run/build/architecture notes — see `docs/developer.md` for full procedures.

* Build helper: `./build.ps1` (common commands: `build`, `UnitTest`, `IntegrationTest`, `run`).
* Local run options: `build.ps1 run`, Docker compose, or Visual Studio launch profiles.
* DB migrations: scripts and artifacts under `Application/EdFi.Ods.AdminApi/Artifacts/` and `eng/run-dbup-migrations.ps1`.
* Architecture: feature-based layout; `IUsersContext` handles `EdFi_Admin`, `ISecurityContext` handles `EdFi_Security` (EF Core); AutoMapper mappings in `AdminApiMappingProfile.cs`.

<!-- Changelog removed to keep AGENTS.md concise; use git history for changes. -->
