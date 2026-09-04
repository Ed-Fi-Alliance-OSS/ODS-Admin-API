# Installer support for AdminApiMode / EncryptionKey / StandardVersion

## Background

The 2.3.1/2.3.2 release line added an `AdminApiMode` (v1/v2) selector,
an `EncryptionKey` parameter, and a `StandardVersion` compatibility
guard to `Install-EdFiOdsAdminApi` in `Installer.AdminApi/Install-AdminApi.psm1`.
That work never made it onto `main`'s line of history (main's installer
scripts evolved independently and never picked up these parameters).

Since that release, `main`'s application-side `AdminApiMode` concept
has grown into a three-way mode (`V1`, `V2`, `V3`) read from
`AppSettings:AdminApiMode` in `appsettings.json`, defaulting to `v3`.
All three modes ship in a single unified `EdFi.Ods.AdminApi` binary —
there is no separate v1 package (`EdFi.Ods.AdminApi.V1` and
`EdFi.Ods.AdminApi.V3` are project references pulled into the same
build). `EncryptionKey` already exists as an `AppSettings` property
used by ODS-instance encryption code (`AddOdsInstance`,
`EditOdsInstance`, `GetOdsInstanceQuery`, `GetOdsInstancesQuery`,
`CreateInstanceJob`), but nothing in the installer scripts sets it.

This means the 2.3.2 installer code cannot be copied over as-is — its
`ValidateSet('v1', 'v2')` would block installing the current default
mode (`v3`), and its `StandardVersion` `ValidateSet` includes `6.0.0`,
a Data Standard version this repo's `build.ps1` does not currently
support (only `4.0.0` and `5.2.0`, see `build.ps1:170,179`).

## Goal

Give the installer scripts (`Installer.AdminApi/install.ps1` and
`Installer.AdminApi/Install-AdminApi.psm1`) first-class support for
selecting `AdminApiMode`, supplying `EncryptionKey`, and validating
`StandardVersion` compatibility — matching how the application
actually behaves today, not how it behaved when the 2.3.x line
diverged.

## Non-goals

- No `Authority` authentication setting. That belongs to the external
  IdP/Keycloak support removed in ADMINAPI-1503; `main`'s
  `AuthenticationSettings` stays `IssuerUrl` / `SigningKey` /
  `AllowRegistration` only.
- No `6.0.0` StandardVersion option — not supported anywhere else in
  this repo currently.
- No changes to `Update-EdFiOdsAdminApi`'s parameter surface beyond
  carrying `AdminApiMode`/`EncryptionKey` forward from the existing
  installation during an upgrade (an operator does not re-supply these
  on upgrade).
- No broader Pester coverage of the rest of `Installer.AdminApi` —
  only the new validation logic introduced here.

## Design

### 1. `Install-EdFiOdsAdminApi` parameters

Add to the existing `param()` block in
`Installer.AdminApi/Install-AdminApi.psm1`:

- `AdminApiMode` — mandatory, `[ValidateSet('v1', 'v2', 'v3')]`.
- `EncryptionKey` — optional `[string]`, with a `[ValidateScript({...})]`
  that (when non-empty) decodes the value as base64 and requires
  exactly 32 bytes (256 bits), mirroring the existing
  `OdsConnectionStringEncryptionKey` contract documented in the
  Ed-Fi ODS/API installation docs. Empty is allowed at the parameter
  level; the mode/key relationship is enforced below.
- `StandardVersion` — mandatory, `[ValidateSet('4.0.0', '5.2.0')]`.

> **Implementation note (deviation, intentional):** the shipped code does
> *not* use `[ValidateSet]`/`[ValidateScript]` attributes directly on
> `Install-EdFiOdsAdminApi`'s parameters. Instead, `AdminApiMode`,
> `StandardVersion`, and `EncryptionKey` are declared as plain
> `[string]`s, and all of the validation described in this section lives
> in a standalone function, `Assert-AdminApiModeCompatibility` (plus a
> `Test-EncryptionKeyFormat` helper it calls), in a new
> dependency-free module, `Installer.AdminApi/AdminApiModeValidation.psm1`.
> `Install-EdFiOdsAdminApi` imports that module and calls
> `Assert-AdminApiModeCompatibility` explicitly at the top of its body
> (see §2).
>
> Reason: `Install-AdminApi.psm1` transitively imports
> `AppCommon/IIS/IIS-Components.psm1`, which runs `Import-Module
> WebAdministration` at import time — a Windows/IIS-only module. The
> Pester suite added for this feature (§6) runs as part of
> `./build.ps1 -Command UnitTest`, which executes in CI on
> `ubuntu-latest`. If the validation lived in attributes on
> `Install-EdFiOdsAdminApi` itself, testing it would require importing
> (or at least binding parameters on) that function — impossible on
> Linux CI, since the module can't even be imported there. Extracting
> the validation into its own IIS-independent module is what makes it
> testable in CI at all. The cost is that the `-notin @('v1','v2','v3')`
> and `-notin @('4.0.0','5.2.0')` checks inside
> `Assert-AdminApiModeCompatibility` manually re-implement what
> `[ValidateSet]` would otherwise give for free — an accepted tradeoff
> for CI coverage.

### 2. Validation gates

Immediately after `Clear-Error` in `Install-EdFiOdsAdminApi`, before
building `$Config`:

```powershell
if ($AdminApiMode -eq 'v1' -and $StandardVersion -ne '4.0.0') {
    throw "Admin API v1 mode only supports StandardVersion 4.0.0."
}

if (($AdminApiMode -eq 'v2' -or $AdminApiMode -eq 'v3') -and [string]::IsNullOrWhiteSpace($EncryptionKey)) {
    throw "EncryptionKey is required for Admin API v2 and v3 modes. This key must match the OdsConnectionStringEncryptionKey used in your Ed-Fi ODS / API installation."
}
```

`AdminApiMode` and `EncryptionKey` are added to the `$Config` hashtable
alongside the existing entries.

### 3. Writing to `appsettings.json`

In `Invoke-TransformAppSettings`, add two lines next to the existing
`DatabaseEngine`/`MultiTenancy` assignments:

```powershell
$settings.AppSettings.AdminApiMode = $Config.AdminApiMode
$settings.AppSettings.EncryptionKey = $Config.EncryptionKey
```

This matches the exact keys `AdminApiModeValidationMiddleware` and
`AppSettings.cs` already read (`AppSettings:AdminApiMode`,
`AppSettings:EncryptionKey`).

### 4. Upgrade path

In `Invoke-TransferAppsettings` (used by `Update-EdFiOdsAdminApi`), add
`AdminApiMode` and `EncryptionKey` to the list of settings copied
forward from the existing installation's `appsettings.json`, next to
`DatabaseEngine`:

```powershell
$newSettings.AppSettings.AdminApiMode = $oldSettings.AppSettings.AdminApiMode
$newSettings.AppSettings.EncryptionKey = $oldSettings.AppSettings.EncryptionKey
```

No new parameters are added to `Update-EdFiOdsAdminApi` — the operator
does not re-supply mode/key on upgrade, they carry forward.

### 5. `install.ps1` example script

Update the example configuration to demonstrate the new parameters,
keeping the existing `AuthenticationSettings` shape unchanged:

```powershell
$odsEncryptionKey = ""  # Required for AdminApiMode v2/v3. Must match the
                         # OdsConnectionStringEncryptionKey used in your
                         # Ed-Fi ODS / API installation. Base64-encoded, 32 bytes.

$p = @{
    ToolsPath = "C:/temp/tools"
    AdminApiMode = "v3"
    StandardVersion = "5.2.0"
    DbConnectionInfo = $dbConnectionInfo
    PackageVersion = "__ADMINAPI_VERSION__"
    PackageSource = $adminApiSource
    AuthenticationSettings = $authenticationSettings
    EncryptionKey = $odsEncryptionKey
}
```

Also add a short v1-mode example block, matching the existing
single/multi-tenant example style, showing `AdminApiMode = "v1"`,
`StandardVersion = "4.0.0"`, and no `EncryptionKey`.

### 6. Pester test coverage

New file `Installer.AdminApi/Install-AdminApi.Tests.ps1`, pinned via
`#Requires -Modules @{ModuleName='Pester'; ModuleVersion='5.x'}`.
Covers only the new validation logic (no real IIS/DB calls):

- `AdminApiMode` `ValidateSet` accepts `v1`/`v2`/`v3`, rejects other
  values.
- `EncryptionKey` `ValidateScript`: accepts a valid base64 32-byte key,
  rejects wrong-length or non-base64 input, allows empty.
- Throws when `AdminApiMode -eq 'v1'` and `StandardVersion -ne '4.0.0'`.
- Throws when `AdminApiMode` is `v2`/`v3` and `EncryptionKey` is empty.

Wiring: add a Pester invocation for this file into the `UnitTests`
function in `build.ps1` (called from `Invoke-UnitTestSuite`,
`build.ps1:461-463`), so `./build.ps1 -Command UnitTest` picks it up —
no new CI job needed, it flows through the existing
`.github/workflows/on-pullrequest.yml` step (`./build.ps1 -Command
UnitTest -NoBuild -Configuration Debug -RunCoverageAnalysis`). If
Pester is not present in the build environment, `build.ps1` installs
it on demand (same on-demand-tool pattern already used for
`minver-cli` in `eng/get-version.ps1`), rather than requiring manual
setup.

## Testing

- Pester unit tests (above) run via `./build.ps1 -Command UnitTest`.
- Manual verification: run `install.ps1` against a real ODS/API for
  each of `v1`/`v2`/`v3` and confirm `appsettings.json` picks up the
  correct `AdminApiMode`/`EncryptionKey`; confirm the `v1` +
  non-`4.0.0` and `v2`/`v3` + empty-key throws fire as expected;
  confirm `Update-EdFiOdsAdminApi` carries the values forward on an
  upgrade.
