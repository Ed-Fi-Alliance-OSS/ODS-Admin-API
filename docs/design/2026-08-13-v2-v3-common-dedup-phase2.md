# V2/V3 Duplication Cleanup — Phase 2 (Common Library Extraction)

## Context

PR #328 stood up the V3 API by copying most of V2's Features/Infrastructure
code and renaming OdsInstance→DataStore. A code review of that PR (and the
follow-up bug fixes/dedup pass in this branch) confirmed the copy left real
duplication behind: identical files under different namespaces, and in a
couple of cases, identical logic hiding a shared bug (see the DBTests
`UsersTransaction` fix, already landed).

Phase 1 of this cleanup (already committed on this branch) moved the clean,
zero-blocker cases found so far into `EdFi.Ods.AdminApi.Common`:
`EntityReferenceValidator`, `ActionMapper`/`ActionModel`,
`AuthorizationStrategyMapper`/`AuthorizationStrategyModel`, and extracted
`PlatformSecurityContextTestBase` into a new shared DBTests project.

A full diff sweep (V2 vs V3 only — V1 uses its own vendored data-access
types and doesn't share cleanly, confirmed separately) turned up more
candidates across DBTests, production Features/Infrastructure, and
UnitTests. This spec covers moving those, split into two phases so a
regression is easy to bisect.

## Goal

Eliminate the remaining confirmed-safe duplication between V2 and V3 without
touching anything that carries a real per-version type dependency or
behavioral difference. Two phases:

- **Phase 1 (this pass, part A):** every zero-blocker file — plain data
  classes, mappers, extension/helper classes with no version-specific
  dependency. Purely mechanical: move file to `Common`, delete both
  per-version copies, fix up `using`s.
- **Phase 2 (this pass, part B):** de-static `PlatformUsersContextTestBase.cs`
  and `AdminApiDbContextTestBase.cs`, plus unify the production `AdminApiDbContext`
  they (and `AdminApiAuditLogWriter`) depend on. See "Phase 2" below — this
  grew from the original plan once investigation showed `AdminApiDbContext`
  wasn't actually version-specific, just blocked by `SecurityModels` (Phase 1).

**Explicitly out of scope**, with reasons (not revisited unless something
changes upstream):
- `VendorMapper`/`ResourceClaimMapper` — models are identical, but the
  extension methods they call (`VendorExtensions`, `AdminModelExtensions`,
  `IEnumerableExtensions`) diverge in real logic per version.
- Request/Validator inner classes (e.g. `AddVendorRequest`) — each
  implements a per-version `IAddVendorModel`-style interface tied to that
  version's Command class; moving them means unifying those interfaces
  first.
- `Testing.cs` (DBTests) — content is effectively identical but is imported
  via `using static` at dozens of call sites per project; centralizing a
  ~50-line config helper isn't worth that churn.
- Everything else the sweep touched (ClaimSets, most Vendor/ResourceClaim
  code, most Database/QueryTests and CommandTests files, `AdminApiAuditLogWriterTests.cs`'s
  underlying job-related tests) — real behavioral differences or tests of
  genuinely version-specific Command/Query types, not duplicated code.

## Phase 1 — mechanical moves

Same recipe as the mappers already moved this branch: create the file under
`EdFi.Ods.AdminApi.Common` in a matching subfolder, delete the v2 and v3
copies, add `using EdFi.Ods.AdminApi.Common....;` to every file that
referenced the type via same-namespace implicit access, build, repeat.

**Production Features → Common:**
- `Profiles/ProfileModel.cs` (includes `ProfileDetailsModel`),
  `Profiles/ProfileMapper.cs`, `Profiles/ProfileValidator.cs` — move as a
  set, Mapper depends on Model.
- `ResourceClaimActionAuthStrategies/ResourceClaimActionAuthStrategyModel.cs`
  (includes `ActionWithAuthorizationStrategy`, `AuthorizationStrategyModelForAction`)
- `ResourceClaimActions/ResourceClaimActionModel.cs` (includes
  `ActionForResourceClaimModel`)
- `Tenants/TenantModel.cs` (includes `TenantModelConnectionStrings`)

**Production Infrastructure → Common:**
- `Infrastructure/Helpers/HealthCheckServiceExtensions.cs`
- `Infrastructure/Helpers/FileSystemAppSettingsFileProvider.cs`
- `Infrastructure/Helpers/ConstantsHelper.cs`
- `Infrastructure/Documentation/ProfileRequestExampleFilter.cs`
- `Infrastructure/Documentation/SwaggerOptionalSchemaFilter.cs`
- `Infrastructure/Documentation/SwaggerExcludeSchemaFilter.cs`
- `Infrastructure/Security/SecurityModels.cs`

**Test code:**
- `DBTests/AssertionExtensions.cs` → `EdFi.Ods.AdminApi.DBTests.Common`
  (the shared project created in Phase 1 of the earlier cleanup; disk
  folder name stays `.DBTests.Common`, C# namespace is
  `EdFi.Ods.AdminApi.DBTestsShared` — collision-avoidance already
  established, keep using it).
- `UnitTests/Api/OdsApiValidatorTests.cs` (both v2 and v3 copies) →
  `EdFi.Ods.AdminApi.Common.UnitTests`. This one isn't just duplicated —
  its subject, `OdsApiValidator`, already lives in `Common`. The test
  should never have been duplicated per-version; this is a relocation, not
  a new-shared-code decision.

Verify after Phase 1: full solution `dotnet build`, then
`./build.ps1 -Command UnitTest -NoBuild`, then the DB and E2E test runs
described in **Verification** below.

## Phase 2 — unify AdminApiDbContext, then de-static the two test bases

Investigation while planning Phase 1 found `AdminApiDbContext.cs` (the
production EF Core context — `Infrastructure/AdminApiDbContext.cs` in both
v2 and v3) is itself a namespace-only duplicate: every using except one
already points at `EdFi.Ods.AdminApi.Common...`; the sole holdout is
`Infrastructure.Security` (`SecurityModels.cs`, moved to Common in Phase 1).
Once that dependency is gone, `AdminApiDbContext` can move too — which
unblocks `AdminApiAuditLogWriter.cs` (constructs `AdminApiDbContext`
directly, so it inherited the same block) and `AdminApiDbContextTestBase.cs`
(same static/`using static Testing` shape as `PlatformUsersContextTestBase`,
blocked the same way).

`AdminApiDbContext` is not test-only — it's the live production context,
referenced by ~30 files per version (DI registration in
`WebApplicationBuilderExtensions.cs` and `TenantSpecificDbContextProvider.cs`,
plus roughly a dozen Command/Query/Job classes each in v2 and v3). This is
the widest-blast-radius step in this cleanup; every consumer needs its
`using EdFi.Ods.AdminApi(.V3).Infrastructure;` (or wherever it currently
resolves `AdminApiDbContext` from) updated to
`using EdFi.Ods.AdminApi.Common.Infrastructure;` — mechanical, one `using`
line each, caught immediately by `dotnet build` if missed, but wide.

Order within Phase 2:
1. Move `AdminApiDbContext.cs` to `EdFi.Ods.AdminApi.Common/Infrastructure`,
   delete the v2/v3 copies, fix up every consumer's `using` (production code
   first, then the two test bases below still compile against the old
   per-project type until step 3/4).
2. Move `AdminApiAuditLogWriter.cs` to Common the same way (now unblocked).
3. De-static `PlatformUsersContextTestBase.cs`: add
   `protected abstract string AdminConnectionString { get; }`, replace the
   `using static Testing;`-derived `ConnectionString` property with that
   hook, drop `static` from every member (`Save`, `Transaction`,
   `GetDbContextOptions`, `ConnectionString`). Move to
   `EdFi.Ods.AdminApi.DBTests.Common`, delete the v2/v3 copies. 43 v2 + 43
   v3 = 86 direct-subclass test-fixture files each need one added line:
   ```csharp
   protected override string AdminConnectionString => Testing.AdminConnectionString;
   ```
4. De-static `AdminApiDbContextTestBase.cs` the same way — it needs two
   hooks, not one, since it also resolves `Testing.Configuration()`:
   ```csharp
   protected abstract string AdminConnectionString { get; }
   protected abstract IConfiguration Configuration { get; }
   ```
   Move to `EdFi.Ods.AdminApi.DBTests.Common`, delete the v2/v3 copies. 7 v2
   + 7 v3 = 14 direct-subclass files each need two added lines.

No other change in any of the 100 (86 + 14) test-fixture files — inherited
member call syntax (`Save(...)`, `Transaction(...)`) is identical whether
the base's members are static or instance.

Verify after Phase 2: same as Phase 1 — full build, unit tests, then DB/E2E
runs.

## Verification — DB tests and Bruno E2E

Both phases only touch test infrastructure and Common-library code paths
that are already exercised by unit tests, but the DBTests and Bruno E2E
suites are the ones that actually prove the `UsersTransaction`/base-class
plumbing still works end-to-end against a real database — unit tests alone
don't catch it. Both phases must additionally pass:

- **`eng/run-db-tests.ps1`** — run against both DBTests projects to confirm
  `PlatformSecurityContextTestBase`/`PlatformUsersContextTestBase` (and,
  after Phase 2, the newly-shared version) still checkpoint/transact
  correctly against a real SQL Server:
  ```powershell
  ./eng/run-db-tests.ps1 -Project All -DbEngine mssql -TearDown
  ```
  Per the script's own note, `-DbEngine pgsql`/`both` only stands up and
  migrates PostgreSQL for manual verification — the NUnit suite itself is
  hard-coded to SQL Server via `AdminApiDbContextTestBase`, unaffected by
  this spec. Run once after Phase 1, once after Phase 2.

- **`eng/run-bruno-e2e.ps1`** — confirms the moved Features/Infrastructure
  code (mappers, models, Swagger filters, audit log writer, health checks)
  still serializes and behaves correctly through the live API, not just
  under unit-test mocks. Phase 1 touches both v2 and v3, across both DB
  engines the script supports, and multitenant is v2/v3-only (V1 doesn't
  support it, unaffected either way) — full matrix, both API versions ×
  both DB engines × both tenant modes, 8 runs:
  ```powershell
  ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine pgsql -TenantMode singletenant -TearDown
  ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine pgsql -TenantMode multitenant  -TearDown
  ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine mssql -TenantMode singletenant -TearDown
  ./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine mssql -TenantMode multitenant  -TearDown
  ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine pgsql -TenantMode singletenant -TearDown
  ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine pgsql -TenantMode multitenant  -TearDown
  ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine mssql -TenantMode singletenant -TearDown
  ./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine mssql -TenantMode multitenant  -TearDown
  ```
  Phase 2 moves `AdminApiDbContext` and `AdminApiAuditLogWriter` — real
  production code, not just test plumbing — so re-run the full 8-run Bruno
  matrix after Phase 2 too, not just after Phase 1.

If either script fails after a phase, treat it as that phase's regression
signal before moving to the next phase.

## Risks

- **`AdminApiDbContext` is production code, not test infra.** Unlike every
  other move in this spec, this one changes a type used by the running
  application (DI registration, Command/Query/Job classes), not just test
  fixtures. The change itself is still mechanical (move file, fix usings,
  no logic change), but it's the one step in this plan where the Bruno E2E
  matrix — not just unit tests — is the real safety net, since DI wiring
  and EF `OnModelCreating` behavior only prove out against a live app +
  database.
- **Wide-but-shallow blast radius in Phase 2.** ~30 production consumer
  files per version for `AdminApiDbContext`, plus 100 test-fixture files
  (86 + 14) for the two de-static'd base classes — all single mechanical
  line additions with no logic change. The risk is a missed file (compile
  error, not a silent bug), which `dotnet build` catches immediately.
- **`packages.lock.json` ripple.** Phase 1 doesn't add new package
  references (all moved code already depends only on packages `Common`
  already references), so no repeat of the transitive-conflict ripple seen
  in the earlier `EdFi.Suite3.Security.DataAccess`/`EFCore.NamingConventions`
  addition. Confirm this holds when Phase 1 actually lands — if a moved
  file needs a package `Common` doesn't have yet, re-check for the same
  `NU1608` conflict pattern before adding it.
