# Installer pre-check: cryptic error on duplicate IIS applications

## Background

While working on ADMINAPI-1504 and applying fixes from PR #459, a
customer reported that running `install.ps1` when another Admin Api
installation already exists at the target location produces a cryptic,
unhelpful exception:

```
"GetVersionInfo" with "1" argument(s): "The given path's format is not
 supported."
at C:\repos\EdFi.Suite3.ODS.AdminApi.2.3.0\installer\Install-AdminApi.psm1:283 char:20
+        $result += Invoke-InstallationPreCheck -Config $Config
+                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo         : NotSpecified: (:) [Invoke-InstallationPreCheck], MethodInvocationException
    + FullyQualifiedErrorId : NotSupportedException,Invoke-InstallationPreCheck
```

### Root cause report

A follow-up report reproduced the issue on Admin API 2.3.2 (Windows 11,
IIS, Windows PowerShell 5.1) and identified the trigger precisely:

> The error occurs when two or more IIS web applications named
> `AdminApi` exist across different websites (for example, one under
> the Ed-Fi site from a current install and a leftover one under
> Default Web Site or a custom site from a previous install). A single
> preexisting installation does not reproduce it — the pre-check
> handles that case correctly.

Root cause chain (in the then-current v2.3.2 `Install-AdminApi.psm1`,
same shape as `main`'s code prior to this fix):

1. `Invoke-InstallationPreCheck` calls `Get-WebApplication` /
   `get-webapplication $Config.WebApplicationName` without a `-Site`
   filter, so it returns every IIS application named `AdminApi` across
   **all** sites. With duplicates, the result is an array.
2. `GetExistingAppVersion` evaluates `($existingAdminApi).PhysicalPath`
   on that array and interpolates it into a single string, producing a
   space-joined pseudo-path such as:
   `C:\inetpub\wwwroot     C:\inetpub\Ed-Fi\AdminApi\EdFi.Ods.AdminApi.dll`
3. Under Windows PowerShell 5.1 (.NET Framework),
   `[System.Diagnostics.FileVersionInfo]::GetVersionInfo()` rejects the
   second `:` in that string with
   `NotSupportedException: "The given path's format is not supported."`
   — the cryptic error the customer sees.

The externally suggested fix was to scope the lookup with
`Get-WebApplication -Site $Config.WebsiteName -Name
$Config.WebApplicationName` in both `Invoke-InstallationPreCheck` and
`Invoke-ApplicationUpgrade`, handle multiple results, and wrap
`GetVersionInfo` in a try/catch with a clear message (e.g. "Multiple
existing 'AdminApi' IIS applications were found... Please remove stale
applications or run `upgrade.ps1`").

## Investigation

Verified against the current `main` branch (`Installer.AdminApi/Install-AdminApi.psm1`):

- The unscoped `get-webapplication $Config.WebApplicationName` call is
  still present in `Invoke-InstallationPreCheck`, and the same
  unscoped pattern exists in `Invoke-ApplicationUpgrade`. The
  underlying bug is confirmed still present and unaffected by PR #459.
- `GetExistingAppVersion` still builds the physical path directly from
  `PhysicalPath` with no defensive check for an array result, and
  `GetVersionInfo` is called with no try/catch.

The suggested fix referenced pointing customers at `upgrade.ps1`. That
script **does not exist in this repository** and has not for some
time:

- `Installer.AdminApi/upgrade.ps1` was first added in the shared
  installer history (commit `a6ec1e84`, 2022-11-15,
  `[ADMINAPI-36] - PowerShell Installer for Admin Api`).
- It was deleted in the
  [`Ed-Fi-Alliance-OSS/AdminAPI-2.x`](https://github.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x)
  repository by commit `15843388` (2023-12-15), authored by
  **CSR2017**, part of PR #71,
  **[ADMINAPI-770] Implement installer update for the Multi-tenancy
  feature in AdminApi**, under the sub-commit message "Delete upgrade
  file." The PR description contains no written rationale beyond that;
  the deletion was carried out as part of reworking the installer for
  multi-tenancy support. It was never re-added afterward in that
  repository's history.
- This repository (`ODS-Admin-API`, the 1.x line) had, independently,
  kept or re-added its own variant of `upgrade.ps1` (calling
  `Update-EdFiOdsAdminApi` with a `PackageVersion = '1.1.0.0'`-style
  example, different from the AdminAPI-2.x original). That surviving
  copy was removed from `main` in commit `020db6e2` (2025-12-19,
  `[ADMINAPI-1332] Admin API 2.X into Admin API 1.X (#237)`) when
  AdminAPI-2.x's current state — which has lacked `upgrade.ps1` since
  2023 — was merged in.
- Neither `.nuspec` (`EdFi.Ods.AdminApi.nuspec` /
  `EdFi.Ods.AdminApi.V3.nuspec`) currently packages an `upgrade.ps1`
  file; only `install.ps1` and `uninstall.ps1` are shipped.
- The `Update-EdFiOdsAdminApi` function itself, and the
  `Invoke-ApplicationUpgrade` function it calls, are still present and
  exported from `Install-AdminApi.psm1`, but have no shipped
  entry-point script calling them.

### docs.ed-fi.org reference check

Before deciding whether to restore `upgrade.ps1`, the official
documentation was checked for any reference to it or to an in-place
upgrade workflow:

- [Admin API Getting Started](https://docs.ed-fi.org/reference/admin-api/getting-started/)
  makes no mention of `upgrade.ps1`, an upgrade script, or a PowerShell
  upgrade procedure.
- The Admin API 1.x and 2.x "IIS Installation (PowerShell)" pages
  (`docs.ed-fi.org/reference/admin-api/admin-api-1.x/...` and
  `.../admin-api-2.x/...`) and the "IIS Installation (Manual)" pages
  consistently state:

  > Admin API does not support in-place upgrades from prior versions.
  > Please install a fresh copy of Admin API to upgrade from prior
  > versions.

- No current Ed-Fi documentation references `upgrade.ps1` or
  `Update-EdFiOdsAdminApi` as a supported or documented procedure.

This means the externally suggested fix's premise — pointing customers
at "the included upgrade script" — does not match either the current
codebase (the script doesn't exist) or current product documentation
(in-place upgrade is explicitly not supported; a fresh install is the
documented path).

## Decision

**Do not restore `upgrade.ps1`.** Reviving it would reintroduce a
workflow that was deliberately removed from the source repository in
2023 (ADMINAPI-770) and that contradicts the documented, supported
upgrade policy (fresh install only). Three options were considered:

1. **Restore `upgrade.ps1`, adapted to the current installer surface.**
   Rejected — resurrects an unsupported, undocumented path; would need
   ongoing maintenance to track `Update-EdFiOdsAdminApi`'s signature;
   contradicts current docs.
2. **Fix the crash and correct the guidance to match documented
   policy (chosen).** Keeps scope tight, fixes the real bug (the
   unscoped `Get-WebApplication` call and the unguarded
   `GetVersionInfo` call), and stops telling customers to run a script
   that doesn't exist and isn't a supported operation.
3. **Merge upgrade behavior into `install.ps1` itself
   (auto-detect-and-upgrade).** Rejected as out of scope — this would
   change `install.ps1`'s contract and requires its own design/doc
   work; not warranted just to fix a broken pointer in an error
   message.

## Design (implemented)

Changes made to `Installer.AdminApi/Install-AdminApi.psm1`:

1. **`Invoke-InstallationPreCheck`**: scope the IIS application lookup
   to the target site —
   `Get-WebApplication -Site $existingWebSiteName -Name $Config.WebApplicationName`
   — instead of the unscoped `get-webapplication $Config.WebApplicationName`.
   This is the fix for the reported trigger (duplicate `AdminApi`
   applications across different sites).
2. **Multi-match guard**: if the scoped lookup still returns more than
   one application (e.g. duplicates under the same site), throw a
   clear, actionable error instead of letting it flow into
   `GetExistingAppVersion` and fail cryptically:
   `"Multiple existing 'AdminApi' IIS applications were found under site 'X'. Please remove the stale application(s) before continuing, then retry installation."`
3. **`GetExistingAppVersion`**: wrap the
   `[System.Diagnostics.FileVersionInfo]::GetVersionInfo()` call in a
   try/catch as defense-in-depth, so any other unexpected path shape
   surfaces a readable error naming the offending path instead of the
   raw `NotSupportedException`.
4. **Block the in-place overwrite instead of prompting to continue.**
   A GitHub Copilot review of the PR flagged that the initial version
   of this fix contradicted itself: the pre-check's "preexisting
   installation found" message stated that Admin Api does not support
   in-place upgrades, but the `'y'`-continue path directly below it
   still installed into the same `WebApplicationPath`, and
   `Install-EdFiApplicationIntoIIS` → `Copy-ArchiveOrDirectory`
   (`AppCommon/Environment/FolderAdmin.psm1`) copies the new package
   over the existing directory with `Copy-Item -Recurse -Force` and no
   cleanup — an in-place overwrite that can leave stale files from the
   old version behind.

   Tracing `IsVersionHigherThanOther` (strict `-gt` comparison)
   confirmed that this branch only executes when installing a version
   newer than the one already present — i.e., its entire purpose was
   to let the operator in-place-upgrade by answering `y`. That is
   exactly the behavior docs.ed-fi.org says is unsupported, not a
   separate, legitimate "reconfigure the same version" feature worth
   preserving (a same-version reinstall does not reach this branch at
   all — it falls into the sibling "downgrade" branch instead).

   The branch was rewritten to match the other two branches in the
   same `if`/`elseif`/`else` (dead-elseif and downgrade): it now
   `Write-Warning`s that Admin Api does not support in-place upgrades,
   directs the operator to install a fresh copy targeting a different
   `WebsiteName`/`WebApplicationName`/`WebSitePath`, or to fully
   uninstall the existing installation first, and `exit`s. The
   `Request-Information` confirmation prompt was removed, along with
   the `$appSettings = Get-Content $appsettingsFile | ...` read that
   used to run on the `'y'` path — that variable was never consumed by
   anything downstream (the "your encryption key is being copied
   forward" message next to it was aspirational, not real), so
   removing it changes no behavior.
5. **`UnEncryptedConnection` fixed to actually apply outside the
   default install mode.** A separate commit on this branch
   (`f067d687`, made before this Copilot review) had ported
   `UnEncryptedConnection` from a top-level `-UnEncryptedConnection`
   switch (applied unconditionally via `$Config.UnEncryptedConnection`)
   to a property nested under each database connection-info object
   (`$Config.DbConnectionInfo.UnEncryptedConnection`), gated on
   `$Config.DbConnectionInfo.Engine -ieq "SqlServer"`, across four call
   sites: `Invoke-TransformConnectionStrings`,
   `Invoke-TransformMultiTenantConnectionStrings`, and both branches of
   `Invoke-DbUpScripts`. Copilot flagged that `$Config.DbConnectionInfo`
   is only populated for the `SharedCredentials` parameter set (and for
   `MultiTenant` installs using shared credentials) — it is `$null` for
   the `SeparateCredentials` parameter set and for multi-tenant
   installs using per-tenant credentials, so the condition was always
   false there and `Encrypt=false` silently stopped being applied. This
   was a regression versus the pre-`f067d687` top-level switch, which
   worked across all parameter sets.

   Fixed all four call sites to read `Engine`/`UnEncryptedConnection`
   from the connection-info object actually used to build each
   connection string, instead of always from `$Config.DbConnectionInfo`:
   `$Config.AdminDbConnectionInfo` / `$Config.SecurityDbConnectionInfo`
   for the single-tenant paths (`Invoke-TransformConnectionStrings`,
   the non-multi-tenant branch of `Invoke-DbUpScripts`), and
   `$Config.Tenants[$tenantKey].AdminDbConnectionInfo` /
   `$Config.Tenants[$tenantKey].SecurityDbConnectionInfo` for the
   multi-tenant paths (`Invoke-TransformMultiTenantConnectionStrings`,
   the multi-tenant branch of `Invoke-DbUpScripts`). These objects are
   guaranteed populated by the time each site runs — either supplied
   directly by the caller, or cloned from `$Config.DbConnectionInfo`
   earlier in the same function when `$Config.usingSharedCredentials`
   is true — so the fix covers the shared-credentials case exactly as
   before and now also covers `SeparateCredentials` and per-tenant
   credentials.
6. **Messaging fix**: the pre-check's "preexisting installation found"
   message no longer suggests running "the included upgrade script."
   It now states that Admin Api does not support in-place upgrades and
   that a fresh install is required, matching docs.ed-fi.org (see
   item 4 above for the full rewrite of this branch).

Explicitly out of scope / left unchanged:

- `Invoke-ApplicationUpgrade` and `Update-EdFiOdsAdminApi` — dead code
  with no shipped entry point; not exercised by any documented
  workflow.
- Restoring `upgrade.ps1` or any `.nuspec` packaging changes for it.
- The pre-existing unreachable `elseif ($targetIsNewer)` branch a few
  lines below the changed code (duplicate condition of the preceding
  `if`) — a separate, pre-existing defect not caused by or related to
  this fix.

## Testing

- Verified `Install-AdminApi.psm1` parses without syntax errors via
  `[System.Management.Automation.Language.Parser]::ParseFile`.
- No automated Pester coverage is possible for this path:
  `Invoke-InstallationPreCheck` transitively imports
  `AppCommon/IIS/IIS-Components.psm1`, which calls `Import-Module
  WebAdministration` at import time (Windows/IIS-only) — the same
  constraint already documented in
  `docs/design/2026-09-04-installer-adminapi-mode-design.md` for why
  IIS-dependent installer code can't run under the Linux-based CI unit
  test job.
- Manual verification (pending): reproduce the original report's
  repro steps (create a second IIS application named `AdminApi` under
  a different site, e.g. via
  `New-WebApplication -Site "Default Web Site" -Name "AdminApi" -PhysicalPath "C:\inetpub\wwwroot"`)
  and confirm `install.ps1` now either proceeds correctly (single
  match after scoping) or reports the new clear "multiple
  applications found" message instead of the original cryptic
  `NotSupportedException`.
