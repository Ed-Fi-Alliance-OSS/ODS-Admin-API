# OpenAPI Spec Generation Workflow (v2 + v3, Artifact Publishing)

## Problem

The existing `.github/workflows/openapi-md.yml` action is outdated:

- It only generates the `v2` Swagger document (`build.ps1`'s `GenerateOpenAPI` function hardcodes `swagger tofile ... v2`), even though Admin API now exposes a real `v3` surface (`AdminApiVersions` registers `v1`, `v2`, `v3`, and v3 has its own endpoints, e.g. `/dataStores/manage`).
- It generates a markdown summary via `widdershins`, which is no longer wanted.
- It commits the generated files to a branch in this repo and opens a PR here, which is more process than needed for what is essentially a generated build artifact.

## Goal

A manually-dispatchable (and weekly-scheduled) GitHub Action that generates both the `v2` and `v3` OpenAPI yaml specs for Admin API and publishes them as downloadable **workflow run artifacts** — no markdown, no git commits, no PRs, and nothing pushed to any other repository.

## `build.ps1` changes

- **`adminApiMode` must be switched per doc generated.** `AppSettings:AdminApiMode` (`appsettings.json`) isn't cosmetic — `WebApplicationBuilderExtensions.cs:74/81` uses it to decide which endpoints get registered at startup at all (v2-only endpoints when `AdminApiMode=v2`, v3-only when `AdminApiMode=v3`). Since `swagger tofile` boots the real app pipeline to introspect routes, a single static `adminApiMode` can only ever produce a correct doc for one of the two versions. So generation must run in two passes, flipping the mode between them:
  - `UpdateAppSettingsForAdminApi` (`build.ps1:541`, currently only called from `Invoke-GenerateOpenAPIAndMD`/`build.ps1:412`) gains an `-AdminApiMode` parameter that sets `json.AppSettings.AdminApiMode` when provided. No other call site exists, so this is a safe, non-breaking addition.
  - `GenerateOpenAPI` (`build.ps1:215`) takes a `-DocVersion` parameter (`v2` or `v3`) instead of looping internally, and no longer needs to loop over both — the loop moves up to `Invoke-GenerateOpenAPI`.
  - `Invoke-GenerateOpenAPI` (renamed from `Invoke-GenerateOpenAPIAndMD`, `build.ps1:411`) runs `DotNetClean`/`Restore` once, then for each of `v2`/`v3`: `UpdateAppSettingsForAdminApi -AdminApiMode <version>` → `Compile` (re-copies the edited `appsettings.json` into `bin/`, since it's `CopyToOutputDirectory: PreserveNewest` by ASP.NET Core SDK default — no full rebuild needed) → `GenerateOpenAPI -DocVersion <version>`.
  - Output files: `docs/api-specifications/openapi-yaml/admin-api-v2-$APIVersion.yaml` and `admin-api-v3-$APIVersion.yaml`.
- `GenerateDocumentation` (`build.ps1:230`, the `widdershins` call) is removed entirely.
- The `GenerateOpenAPIAndMD` command is renamed to `GenerateOpenAPI`:
  - `ValidateSet` entry at `build.ps1:79` updated.
  - Switch statement at `build.ps1:644` updated to match.
- `docs/yaml-to-md/yaml-to-md.md` (a standalone manual how-to doc for turning yaml into markdown by hand) is unrelated to this automated workflow and is left untouched.
- The workflow's `$p` `-DockerEnvValues` hashtable needs **no new key** for this — mode-switching is entirely internal to `-Command GenerateOpenAPI`.

## Workflow changes (`.github/workflows/openapi-md.yml`)

### Triggers

```yaml
on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Version name for output filenames, e.g. "2.4.0". Leave blank to use "latest" (always used for the scheduled run).'
        required: false
        type: string
  schedule:
    - cron: '0 6 * * 0'   # Sunday 06:00 UTC
```

### Ref + version resolution

Historically, `version` was also implicitly meant to let a run target a specific tagged release rather than always building whatever's on `main` — the old workflow just never actually implemented that (it always checked out the triggering branch). This redesign makes that explicit: the input now resolves to **both** a git ref to check out **and** a version label used for output filenames. This repo already tags releases as `vX.Y.Z` (confirmed via `git tag`, e.g. `v2.3.2`), so a semver input maps directly to that tag convention. A non-semver input (e.g. a branch name) is used as a raw ref, so new branches can be targeted directly too.

Replaces today's "Validate version" step, and now runs **before** checkout (pure input parsing, no repo access needed):

```powershell
$version = $env:INPUT_VERSION

if ([string]::IsNullOrWhiteSpace($version))
{
    $ref = "main"
    $versionLabel = "latest"
}
elseif ($version -match '^\d+\.\d+\.\d+$')
{
    $ref = "v$version"
    $versionLabel = $version
}
else
{
    $ref = $version
    $versionLabel = ($version -replace '[\\/]', '-')
}
```

- Blank input (manual dispatch with nothing typed, or the `schedule` trigger, which has no `inputs` at all) → checkout `main`, label files `latest`.
- Semver input (`2.4.0`) → checkout tag `v2.4.0`, label files `2.4.0`.
- Anything else (`feature/foo`, `release/2.4`) → checkout that ref as-is, label files with `/` replaced by `-` (filesystem/artifact-name safety).

**Limitation (accepted, forward-only):** this only works correctly for refs created *after* this workflow change merges, since it depends on code that doesn't exist in old tags/branches — the `GenerateOpenAPI` command name, the `adminApiMode` v2/v3 loop, and the `v3` API mode itself are all new. Checking out a pre-existing tag like `v1.4.3` would fail (no such command, no v3 concept at all). No compatibility shim is being built for historical refs; anyone needing an old spec regenerated would do so manually against that old tag's own tooling.

### Steps (replacing everything from "Git create branch" onward in the current file)

1. Resolve `ref` and `version` as above (no checkout needed yet).
2. Checkout `ODS-Admin-API` at the resolved `ref` (`actions/checkout` with `ref: ${{ steps.resolve-version.outputs.ref }}`).
3. Install Swashbuckle CLI. **Bump the pinned version from `6.6.2` to `10.2.3`** (latest release; `6.6.2` predates .NET 10 support — confirmed via the NuGet listing that `10.2.3` explicitly supports `net10.0`, which this app now targets). **Remove** the "Install widdershins CLI" step.
4. Build and generate: `./build.ps1 -APIVersion <resolved-version-label> -Configuration Release -DockerEnvValues $p -Command GenerateOpenAPI` — now produces both `admin-api-v2-<version>.yaml` and `admin-api-v3-<version>.yaml`.
5. **Remove**: "Git create branch", "Git add files", "Commit file" (`ghcommit-action`), and "Create PR" steps — none of them are needed anymore.
6. **Add**: `actions/upload-artifact` step uploading both generated yaml files (e.g. artifact name `admin-api-openapi-<version-label>`, `retention-days` left at the org default unless a shorter/longer window is wanted later).

### Permissions

Drops from `contents: write` to `permissions: read-all` (no default) — the job no longer writes anything to the repository (no commits, no branches, no PRs).

## Explicitly out of scope

- Nothing is pushed, committed, or PR'd to `Ed-Fi-Alliance-OSS/Ed-Fi-API-Specifications` or to this repository. Downstream use of the generated specs (e.g., opening a PR in the spec repo) is a manual, human-driven step outside this workflow.
- No PAT or cross-repo credential is required.
- Markdown documentation generation (`widdershins`) is removed, not just skipped for this workflow — a developer wanting a markdown summary can still follow the manual procedure in `docs/yaml-to-md/yaml-to-md.md`.

## Testing

- `./build.ps1 -Command GenerateOpenAPI -APIVersion 2.4.0` locally produces both `admin-api-v2-2.4.0.yaml` and `admin-api-v3-2.4.0.yaml` under `docs/api-specifications/openapi-yaml/`, no markdown file is produced, and both files reflect only their own version's endpoints (e.g. the v2 file has no `/dataStores/manage` v3-only path and vice versa).
- Manually dispatch the workflow with an existing tag's version (e.g. an already-released `vX.Y.Z`) and confirm the run's Artifacts section contains both yaml files, generated from that tag's code.
- Manually dispatch the workflow with a branch name and confirm it checks out that branch rather than `main`.
- Manually dispatch the workflow with no version and confirm files are named with `latest` and the ref checked out is `main`.
- (Schedule trigger itself can't be tested on-demand; correctness of the cron expression and the `latest` fallback is verified by inspection plus the no-version manual-dispatch test above, which exercises the same code path.)
