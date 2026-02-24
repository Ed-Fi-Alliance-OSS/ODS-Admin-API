## AGENTS

This file documents agent guidance and repository-specific rules for automated assistants and contributors.

### General

* Make only high confidence suggestions when reviewing code changes.
* Never change NuGet.config files unless explicitly asked to.

### Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.

### Testing

* We use NUnit tests.
* We use Shouldly for assertions.
* Use FakeItEasy for mocking in tests.
* Copy existing style in nearby files for test method names and capitalization.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and avoid redundant null checks when the type system covers it.

### Running tests

* To build and run tests in the repo, use the command `./build.ps1 UnitTest`.

### Development Notes

* **Project structure:** The `Application/EdFi.Ods.AdminApi/` project uses a feature-based layout (features, infrastructure, tests).
* **Key patterns:** Prefer feature-based endpoints, CQRS-style Query/Command separation, AutoMapper for DTO mappings, and ASP.NET Core minimal APIs for route definitions.
* **AutoMapper:** Keep mappings in `AdminApiMappingProfile.cs`.
* **EF Core queries:** Use `OrderBy`, `Paginate()`, and helper `OrderByColumn()` patterns.
* **Models & tests:** Use annotated request/response models with nullable annotations; write NUnit unit tests with Shouldly and FakeItEasy; E2E tests use Bruno (.bru) files under the project's E2E tests folder.

For expanded developer guidance, see `docs/developer.md`. That document includes detailed bullets and procedures for:

* Development pre-requisites (requires .NET 8.0 SDK; Visual Studio 2022 or Build Tools recommended).
* The `build.ps1` build script and available commands (`clean`, `build`, `UnitTest`, `IntegrationTest`, `BuildAndTest`, `package`, `run`).
* Running the application locally (via `build.ps1 run`, Docker compose, or Visual Studio) and launch profiles.
* Running unit/integration/E2E tests and generating code coverage reports (`./build.ps1 UnitTest`, `./build.ps1 -Command IntegrationTest`).
* Database migration and reset steps (see `Application/EdFi.Ods.AdminApi/Artifacts/` and `eng/run-dbup-migrations.ps1`).

### Important

Agents and contributors should consult `docs/developer.md` when a task involves local development setup, builds, tests, or database operations.

### How to use

* Short tasks: Include the section name in the prompt (for example: "Formatting" or "Testing"). Agents should load and apply only that section.
* System prompt example: "Apply repository rules from `AGENTS.md` (root). If task requests a specific section, load that section only."
* Developer prompt example: "Apply these high-priority rules: Formatting, Nullable Reference Types, Testing, and Running tests (`./build.ps1 UnitTest`)."
* User prompt example: "Implement feature X and follow the `Formatting` and `Testing` sections in `AGENTS.md`."
