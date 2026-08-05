# Design: `EnableDataStoreManagement` Feature Flag

**Ticket:** [ADMINAPI-1489](https://edfi.atlassian.net/browse/ADMINAPI-1489)

## Context

Admin API always exposes instance-management ("data store management") capability today. Some operators don't use this feature and want it fully disabled for a simpler setup. This adds a new `AppSettings` flag, `EnableDataStoreManagement` (bool, default `true`, so existing behavior is unchanged unless an operator opts out), that gates:

1. The six Manage endpoints (V2 `OdsInstances/Manage` + V3 `DataStores/Manage`).
2. The four recurring create/delete dispatcher jobs (2× V2, 2× V3).

`RefreshEducationOrganizationsJob` is explicitly out of scope — it's scheduled in the same `Program.cs` code block but is unrelated to instance create/delete and must keep running regardless of this flag.

## 1. Settings & Configuration

- `Application/EdFi.Ods.AdminApi.Common/Settings/AppSettings.cs`: add
  ```csharp
  public bool EnableDataStoreManagement { get; set; } = true;
  ```
  next to `EnableApplicationResetEndpoint`. Note the explicit `= true` default — unlike `EnableApplicationResetEndpoint`, which relies on the implicit `false` default of `bool`, this flag must default `true` per the acceptance criteria.
- `Application/EdFi.Ods.AdminApi/appsettings.json` and `Application/EdFi.Ods.AdminApi.V3/appsettings.json`: add `"EnableDataStoreManagement": true` next to `EnableApplicationResetEndpoint`.
- Docker compose: every compose file that currently sets `AppSettings__EnableApplicationResetEndpoint` (~35 files across `Docker/V1`, `Docker/V2/{mssql,pgsql}/{SingleTenant,MultiTenant}`, `Docker/V3/{mssql,pgsql}/{SingleTenant,MultiTenant}`) gets a new line:
  ```yaml
  AppSettings__EnableDataStoreManagement: ${ENABLE_DATA_STORE_MANAGEMENT:-true}
  ```
  This follows the *parameterized* convention already present in some (not all) existing compose files — not the hardcoded-`false` variant used elsewhere for `EnableApplicationResetEndpoint`. Consistency with the ticket's explicit instruction takes priority over matching every file's current (inconsistent) style.

## 2. Endpoint gating (6 handlers)

Reuse the exact convention `ResetApplicationCredentials.cs` already establishes: check the flag as the first statement of the handler, and on failure throw `FluentValidation.ValidationException` with a single `ValidationFailure`. The app's existing error-handling middleware converts this to an HTTP 400 automatically — no new response mechanism.

```csharp
if (!settings.Value.EnableDataStoreManagement)
    throw new FluentValidation.ValidationException(new[] { new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.") });
```

`nameof(OdsInstance)` is used for **all six** handlers (V2 and V3 alike) because the underlying domain entity behind both the V2 "OdsInstance" and V3 "DataStore" API surface is the same C# type, `OdsInstance` — there's no separate `DataStore` domain class to reference. This mirrors `ResetApplicationCredentials`'s pattern of naming the actual entity, not the settings key.

Handlers touched:

| File | Handler method | `IOptions<AppSettings>` today? |
|---|---|---|
| `Features/OdsInstances/Manage/AddOdsInstanceManage.cs` (V2) | `Handle` | Yes |
| `Features/OdsInstances/Manage/DeleteOdsInstanceManage.cs` (V2) | `Handle` | Yes |
| `Features/OdsInstances/Manage/ReadOdsInstanceManage.cs` (V2) | `GetOdsInstanceManages`, `GetOdsInstanceManage` | No — add as new endpoint parameter |
| `EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/AddDataStoreManage.cs` (V3) | `Handle` | Yes |
| `EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/DeleteDataStoreManage.cs` (V3) | `Handle` | Yes |
| `EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/ReadDataStoreManage.cs` (V3) | `GetDataStoreManages`, `GetDataStoreManage` | No — add as new endpoint parameter |

For the four GET handlers, adding `[FromServices] IOptions<AppSettings> options` (or the minimal-API equivalent binding, matching how the other four handlers already receive it) is the only signature change required — ASP.NET Core minimal APIs resolve it from DI automatically.

## 3. Job scheduling gate (`Program.cs`)

`Program.cs` currently has no extracted/testable scheduling logic — the `shouldScheduleDispatcher` / `shouldScheduleDeleteDispatcher` / `shouldScheduleEdOrgsRefresh` booleans are inline top-level-statement locals. Add one small `internal static` predicate, directly testable via the file's existing `[assembly: InternalsVisibleTo("EdFi.Ods.AdminApi.UnitTests")]`:

```csharp
internal static bool ShouldScheduleDataStoreManagementJobs(AppSettings settings) =>
    settings.EnableDataStoreManagement;
```

Read the setting once (via `IOptions<AppSettings>` resolved from `app.Services`, consistent with how the endpoint handlers already receive it) alongside the other `app.Configuration.GetValue<...>("AppSettings:...")` reads, then combine it with the existing interval-parse conditions at exactly the four dispatcher-scheduling call sites:

- V2: `CreatePendingOdsInstanceManagesDispatcherJob` — guard becomes `shouldScheduleDispatcher && ShouldScheduleDataStoreManagementJobs(settings)`
- V2: `DeletePendingOdsInstanceManagesDispatcherJob` — guard becomes `shouldScheduleDeleteDispatcher && ShouldScheduleDataStoreManagementJobs(settings)`
- V3: `CreatePendingDataStoreManagesDispatcherJob` — same pattern
- V3: `DeletePendingDataStoreManagesDispatcherJob` — same pattern

`shouldScheduleEdOrgsRefresh` and `RefreshEducationOrganizationsJob` scheduling are **not** touched in either branch — they don't reference the new predicate at all.

The one-off `CreateInstanceJob`/`DeleteInstanceJob`, triggered directly from the Manage endpoints, need no separate change — Section 2's endpoint gate already 400s before that scheduling path is reached. `EdFi.Ods.AdminApi.InstanceManagement` (sandbox provisioners) needs no changes either, since it's only ever invoked from the jobs gated here.

## 4. Unit tests

- **Endpoint tests** (6 total): one per handler, split across `EdFi.Ods.AdminApi.UnitTests` (V2, 3 tests) and `EdFi.Ods.AdminApi.V3.UnitTests` (V3, 3 tests), mirroring `ResetApplicationCredentialsTests.cs` exactly:
  ```csharp
  var settings = Options.Create(new AppSettings { EnableDataStoreManagement = false });
  var exception = Should.Throw<ValidationException>(async () => await Handler.Handle(...));
  exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
  ```
- **Job-scheduling test**: new `ProgramTests.cs` in `EdFi.Ods.AdminApi.UnitTests` (first test file targeting `Program.cs` in this codebase) calling `Program.ShouldScheduleDataStoreManagementJobs(AppSettings)` directly with the flag `true` and `false`, asserting the return value. Scope is deliberately narrow — just this one predicate, not the surrounding interval-parsing/scheduling code, which remains as untested as it is today.

## 5. E2E Bruno coverage

No mechanism exists today to toggle an `AppSettings` flag between Bruno runs — `API_URL` simply points at whatever instance is already running. Approach:

- No new compose *file* is needed — Section 1 already parameterizes every compose file with `${ENABLE_DATA_STORE_MANAGEMENT:-true}`. A disabled-flag run is just: `ENABLE_DATA_STORE_MANAGEMENT=false docker compose -f <existing compose file> up`.
- Document this invocation in `docs/developer.md` alongside existing E2E run instructions.
- Add 2 new `.bru` specs (one V2, one V3), placed in the existing `Manage/` folders, asserting `400` and the disabled message:
  - `POST - OdsInstances Manage - Feature Disabled.bru.disabled`
  - `POST - DataStores Manage - Feature Disabled.bru.disabled`

  Using the `.disabled` suffix convention already present in these folders (e.g. `DELETE - OdsInstance Manage - Success.bru.disabled`) so they're skipped in default runs and only enabled when running against the disabled-flag profile.
- Out of scope: running the full Bruno suite twice (once per flag state) as a CI matrix. This ticket only requires coverage that the 400 happens, not full regression testing under the disabled profile — a natural follow-up if broader disabled-flag E2E coverage is wanted later.

## 6. Documentation (`docs/developer.md`)

Add a short subsection near the existing "OdsInstanceManage Provisioning Jobs" / "Feature-specific prerequisites and configuration" content, documenting:

- `EnableDataStoreManagement` (default `true`): when `false`, disables the 6 Manage endpoints (V2 `/odsInstances/manage*`, V3 `/dataStores/manage*`) and skips scheduling the 4 create/delete dispatcher jobs; `RefreshEducationOrganizationsJob` is unaffected.
- A one-line pointer to the disabled-flag E2E run instructions from Section 5.

## Out of scope

- Any change to `EdFi.Ods.AdminApi.InstanceManagement` (sandbox provisioners) — never invoked except via the jobs gated here.
- Extracting/testing the pre-existing interval-parsing scheduling conditions (`shouldScheduleDispatcher` etc.) beyond adding the new predicate — that logic is unchanged and untested today, and reworking it is not part of this ticket.
- A full disabled-flag E2E CI matrix — only 400-path coverage is added; broader regression testing under the disabled profile is a candidate follow-up.

## Testing plan

- Unit: `./build.ps1 -Command UnitTest -Filter EnableDataStoreManagement` (or run the full suite) covering all 6 endpoint tests + the new `ProgramTests` predicate test.
- E2E: default Bruno run unaffected (flag defaults `true`); a manual/documented run with `ENABLE_DATA_STORE_MANAGEMENT=false` exercises the 2 new `.disabled`-suffixed specs.
- Manual smoke: boot with flag `false`, confirm all 6 endpoints return 400 and Quartz scheduler logs show no `CreatePendingOdsInstanceManagesDispatcherJob` / `DeletePendingOdsInstanceManagesDispatcherJob` / `CreatePendingDataStoreManagesDispatcherJob` / `DeletePendingDataStoreManagesDispatcherJob`, while `RefreshEducationOrganizationsJob` still appears.
