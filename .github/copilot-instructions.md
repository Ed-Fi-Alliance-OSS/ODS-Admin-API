## General

* Make only high confidence suggestions when reviewing code changes.
* Never change NuGet.config files unless explicitly asked to.

## Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

### Testing

* We use NUnit tests.
* We use Shouldly for assertions.
* Use FakeItEasy for mocking in tests.
* Copy existing style in nearby files for test method names and capitalization.

## Running tests

* To build and run tests in the repo, use the command `./build.ps1 UnitTest`

## Development Notes

* **Project structure:** The `Application/EdFi.Ods.AdminApi/` project uses a feature-based layout (features, infrastructure, tests).
* **Key patterns:** Prefer feature-based endpoints, CQRS-style Query/Command separation, AutoMapper for DTO mappings, and ASP.NET Core minimal APIs for route definitions.
* **Adding an endpoint (high-level):** Create feature endpoint, add Query/Command class, register in DI, add unit tests.
* **AutoMapper:** Keep mappings in `AdminApiMappingProfile.cs`; use `CreateMap<Source, Destination>()` and `.ForMember()` or `.Ignore()` when needed.
* **EF Core queries:** Use `OrderBy`, `Paginate()`, and helper `OrderByColumn()` patterns; group results in-memory when necessary for nested response shapes.
* **Models & tests:** Use annotated request/response models with nullable annotations; write NUnit unit tests with Shouldly and FakeItEasy; E2E tests use Bruno (.bru) files under the project's E2E tests folder.
