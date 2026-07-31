# OpenAPI v2/v3 Artifact-Publishing Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix `build.ps1` to generate both the Admin API `v2` and `v3` OpenAPI yaml specs (switching `adminApiMode` between passes, dropping markdown generation), and replace the `openapi-md.yml` GitHub Action's branch/commit/PR flow with a manually-dispatchable + weekly-scheduled workflow that checks out a specific ref/tag/branch, generates both specs, and publishes them as a run artifact.

**Architecture:** No new components — this is a two-file change. `build.ps1`'s `GenerateOpenAPI` now generates one doc version per call (`-DocVersion v2` or `v3`); `UpdateAppSettingsForAdminApi` gains an `-AdminApiMode` parameter; `Invoke-GenerateOpenAPI` (renamed from `Invoke-GenerateOpenAPIAndMD`) drives a two-pass loop that flips `adminApiMode` and recompiles between passes, since the app only registers one version's endpoints per running instance. `.github/workflows/openapi-md.yml` gets a `schedule` trigger, an optional `version` input that resolves to either a `vX.Y.Z` tag, a raw branch/ref, or `main`, and its post-build steps are replaced with `actions/upload-artifact` (no git branch, no commit, no PR).

**Tech Stack:** PowerShell (`build.ps1`), GitHub Actions YAML, Swashbuckle.AspNetCore.Cli.

## Global Constraints

- No markdown/widdershins generation anywhere in this workflow (per `docs/design/2026-07-30-openapi-artifact-workflow-design.md`).
- Nothing is committed, branched, or PR'd — in this repo or any other — as a result of this workflow.
- No PAT or cross-repo credential is introduced.
- Scheduled run: Sunday 06:00 UTC (`cron: '0 6 * * 0'`).
- `adminApiMode` gates which endpoints the app registers at startup (`WebApplicationBuilderExtensions.cs:74/81`) — the v2 and v3 docs MUST be generated from two separate app instances/passes with that setting flipped between them, not one static build.
- Swashbuckle CLI tool pinned version: `10.2.3` (confirmed net10.0 support; `6.6.2` predates .NET 10 entirely).
- Manual dispatch `version` input is optional and resolves to a git ref + a filename-safe version label: blank/schedule → ref `main`, label `latest`; semver (`X.Y.Z`) → ref `vX.Y.Z` (tag), label `X.Y.Z`; anything else → used as the ref as-is, label with `/` replaced by `-`.
- Output filenames: `docs/api-specifications/openapi-yaml/admin-api-v2-<version-label>.yaml` and `admin-api-v3-<version-label>.yaml`.
- This ref-based checkout only works for refs created after this change merges (old tags/branches lack the `GenerateOpenAPI` command and the `v3` mode) — not a bug to fix here, just a documented limitation.

---

### Task 1: Fix `build.ps1` — two-pass v2/v3 generation with `adminApiMode` switching, remove markdown, rename command

**Files:**
- Modify: `build.ps1:79` (ValidateSet)
- Modify: `build.ps1:215-236` (`GenerateOpenAPI`, `GenerateDocumentation`)
- Modify: `build.ps1:411-418` (`Invoke-GenerateOpenAPIAndMD`)
- Modify: `build.ps1:541-554` (`UpdateAppSettingsForAdminApi`)
- Modify: `build.ps1:644` (switch statement)

**Interfaces:**
- Produces: a PowerShell command `GenerateOpenAPI` (replacing `GenerateOpenAPIAndMD`), invoked as `./build.ps1 -Command GenerateOpenAPI -APIVersion <string> -DockerEnvValues <hashtable>`. Writes `docs/api-specifications/openapi-yaml/admin-api-v2-<APIVersion>.yaml` and `admin-api-v3-<APIVersion>.yaml`, each generated from the app running with the matching `AdminApiMode`. Produces no markdown file. This is the interface Task 2's workflow step calls.
- Produces (internal, used only within `build.ps1`): `UpdateAppSettingsForAdminApi -AdminApiMode <'v2'|'v3'>` (sets `appsettings.json`'s `AppSettings.AdminApiMode`); `GenerateOpenAPI -DocVersion <'v2'|'v3'>` (generates exactly one doc, does not loop).

- [ ] **Step 1: Reproduce current (wrong) behavior locally**

Run from the repo root:

```powershell
dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2 --create-manifest-if-needed
./build.ps1 -Command GenerateOpenAPIAndMD -APIVersion 9.9.9 -Configuration Release -DockerEnvValues @{ Authority = "http://api"; IssuerUrl = "https://localhost"; DatabaseEngine = "PostgreSql"; PathBase = "adminapi"; SigningKey = "test"; AdminDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Admin;Application Name=EdFi.Ods.AdminApi;"; SecurityDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Security;Application Name=EdFi.Ods.AdminApi;" }
Get-ChildItem docs/api-specifications/openapi-yaml/*9.9.9* , docs/api-specifications/markdown/*9.9.9*
```

Expected (documenting today's bug): only `admin-api-9.9.9.yaml` exists (no `v2`/`v3` split), and `admin-api-9.9.9-summary.md` also exists. Delete both generated files afterward and restore `appsettings.json` to leave the working tree clean:

```powershell
Remove-Item docs/api-specifications/openapi-yaml/*9.9.9*, docs/api-specifications/markdown/*9.9.9* -ErrorAction SilentlyContinue
git restore Application/EdFi.Ods.AdminApi/appsettings.json
```

- [ ] **Step 2: Update the `ValidateSet` and rename the command**

In `build.ps1:79`, replace:

```powershell
    [ValidateSet("Clean", "Build", "GenerateOpenAPIAndMD", "BuildAndPublish", "UnitTest", "IntegrationTest", "PackageApi"
        , "Push", "BuildAndTest", "BuildAndDeployToAdminApiDockerContainer"
        , "BuildAndRunAdminApiDevDocker", "RunAdminApiDevDockerContainer", "RunAdminApiDevDockerCompose", "Run", "CopyToDockerContext", "RemoveDockerContextFiles")]
```

with:

```powershell
    [ValidateSet("Clean", "Build", "GenerateOpenAPI", "BuildAndPublish", "UnitTest", "IntegrationTest", "PackageApi"
        , "Push", "BuildAndTest", "BuildAndDeployToAdminApiDockerContainer"
        , "BuildAndRunAdminApiDevDocker", "RunAdminApiDevDockerContainer", "RunAdminApiDevDockerCompose", "Run", "CopyToDockerContext", "RemoveDockerContextFiles")]
```

- [ ] **Step 3: Add `-AdminApiMode` to `UpdateAppSettingsForAdminApi`**

Replace `build.ps1:541-554`:

```powershell
function UpdateAppSettingsForAdminApi {
    param(
        [string]
        $AdminApiMode
    )

    $filePath = "$solutionRoot/EdFi.Ods.AdminApi/appsettings.json"
    $json = (Get-Content -Path $filePath) | ConvertFrom-Json
    $json.AppSettings.DatabaseEngine = $DockerEnvValues["DatabaseEngine"]
    $json.AppSettings.PathBase = $DockerEnvValues["PathBase"]

    if ($AdminApiMode) {
        $json.AppSettings.AdminApiMode = $AdminApiMode
    }

    $json.Authentication.IssuerUrl = $DockerEnvValues["IssuerUrl"]
    $json.Authentication.SigningKey = $DockerEnvValues["SigningKey"]

    $json.ConnectionStrings.EdFi_Admin = $DockerEnvValues["AdminDB"]
    $json.ConnectionStrings.EdFi_Security = $DockerEnvValues["SecurityDB"]
    $json.Log4NetCore.Log4NetConfigFileName = "log4net/log4net.config"
    $json | ConvertTo-Json -Depth 10 | Set-Content $filePath
}
```

(Only change from today: the new `$AdminApiMode` parameter and the `if ($AdminApiMode)` block. No other caller exists, so existing behavior is unaffected when the parameter is omitted.)

- [ ] **Step 4: Rewrite `GenerateOpenAPI` to generate a single doc version per call, delete `GenerateDocumentation`**

Replace `build.ps1:215-236`:

```powershell
function GenerateOpenAPI {
    param(
        [string]
        $DocVersion
    )

    Invoke-Execute {
        Push-Location $solutionRoot/EdFi.Ods.AdminApi/
        $dllPath = "./bin/Release/net10.0/EdFi.Ods.AdminApi.dll"
        $outputOpenAPI = "../../docs/api-specifications/openapi-yaml/admin-api-$DocVersion-$APIVersion.yaml"

        try {
            dotnet tool run swagger tofile --output $outputOpenAPI --yaml $dllPath $DocVersion
        }
        finally {
            Pop-Location
        }
    }
}
```

(This removes the `GenerateDocumentation` function entirely — no widdershins call remains anywhere in `build.ps1`.)

- [ ] **Step 5: Rewrite `Invoke-GenerateOpenAPIAndMD` as `Invoke-GenerateOpenAPI`, looping v2/v3 with `adminApiMode` switching**

Replace `build.ps1:411-418`:

```powershell
function Invoke-GenerateOpenAPI {
    Invoke-Step { DotNetClean }
    Invoke-Step { Restore }

    foreach ($docVersion in @("v2", "v3")) {
        Invoke-Step { UpdateAppSettingsForAdminApi -AdminApiMode $docVersion }
        Invoke-Step { Compile }
        Invoke-Step { GenerateOpenAPI -DocVersion $docVersion }
    }
}
```

- [ ] **Step 6: Update the switch statement**

In `build.ps1:644`, replace:

```powershell
        GenerateOpenAPIAndMD { Invoke-GenerateOpenAPIAndMD }
```

with:

```powershell
        GenerateOpenAPI { Invoke-GenerateOpenAPI }
```

- [ ] **Step 7: Verify the fixed behavior locally, including that the two files actually differ by version**

```powershell
./build.ps1 -Command GenerateOpenAPI -APIVersion 9.9.9 -Configuration Release -DockerEnvValues @{ Authority = "http://api"; IssuerUrl = "https://localhost"; DatabaseEngine = "PostgreSql"; PathBase = "adminapi"; SigningKey = "test"; AdminDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Admin;Application Name=EdFi.Ods.AdminApi;"; SecurityDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Security;Application Name=EdFi.Ods.AdminApi;" }
Get-ChildItem docs/api-specifications/openapi-yaml/*9.9.9*
Get-ChildItem docs/api-specifications/markdown/*9.9.9* -ErrorAction SilentlyContinue

Select-String -Path docs/api-specifications/openapi-yaml/admin-api-v2-9.9.9.yaml -Pattern "odsInstances/manage"
Select-String -Path docs/api-specifications/openapi-yaml/admin-api-v2-9.9.9.yaml -Pattern "dataStores/manage"
Select-String -Path docs/api-specifications/openapi-yaml/admin-api-v3-9.9.9.yaml -Pattern "dataStores/manage"
Select-String -Path docs/api-specifications/openapi-yaml/admin-api-v3-9.9.9.yaml -Pattern "odsInstances/manage"
```

Expected: `admin-api-v2-9.9.9.yaml` and `admin-api-v3-9.9.9.yaml` both exist under `docs/api-specifications/openapi-yaml/`; the markdown `Get-ChildItem` returns nothing. The `v2` file matches `odsInstances/manage` and does not match `dataStores/manage`; the `v3` file matches `dataStores/manage` and does not match `odsInstances/manage` — proving `adminApiMode` actually switched between passes rather than both files getting identical (or empty) v2-only content (`AddOdsInstanceManage.cs:40` maps `/odsInstances/manage` under v2; `AddDataStoreManage.cs` maps `/dataStores/manage` under v3).

Then clean up the generated test files and restore `appsettings.json` so nothing test-only gets committed:

```powershell
Remove-Item docs/api-specifications/openapi-yaml/*9.9.9* -ErrorAction SilentlyContinue
git restore Application/EdFi.Ods.AdminApi/appsettings.json
```

- [ ] **Step 8: Commit**

```bash
git add build.ps1
git commit -m "Generate v2 and v3 OpenAPI specs via adminApiMode switching, remove widdershins markdown generation"
```

---

### Task 2: Replace the branch/PR workflow with ref-based checkout, CLI version bump, and artifact publishing + weekly schedule

**Files:**
- Modify: `.github/workflows/openapi-md.yml` (full rewrite)

**Interfaces:**
- Consumes: `./build.ps1 -Command GenerateOpenAPI -APIVersion <string> -DockerEnvValues <hashtable>` from Task 1, which writes `docs/api-specifications/openapi-yaml/admin-api-v2-<version-label>.yaml` and `admin-api-v3-<version-label>.yaml`.

- [ ] **Step 1: Replace the workflow file**

Overwrite `.github/workflows/openapi-md.yml` with:

```yaml
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

name: Generate OpenAPI definitions

on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Version (e.g. "2.4.0", checks out tag v2.4.0), a branch name (checked out as-is), or blank for latest (checks out main).'
        required: false
        type: string
  schedule:
    - cron: '0 6 * * 0' # Sunday 06:00 UTC

permissions: read-all

jobs:
  generate-openapi:
    name: Generate OpenAPI v2/v3 specs
    runs-on: ubuntu-latest
    defaults:
      run:
        shell: pwsh
    steps:
      - name: Resolve ref and version
        id: resolve-version
        env:
          INPUT_VERSION: ${{ inputs.version }}
        run: |
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

          "ref=$ref" >> $env:GITHUB_OUTPUT
          "version=$versionLabel" >> $env:GITHUB_OUTPUT

      - name: Checkout the Repo
        uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3
        with:
          ref: ${{ steps.resolve-version.outputs.ref }}

      - name: Install Swashbuckle CLI
        run: dotnet tool install Swashbuckle.AspNetCore.Cli --version 10.2.3 --create-manifest-if-needed

      - name: Build and generate YAML files
        run: |
          $p = @{
              Authority        = "http://api"
              IssuerUrl        = "https://localhost"
              DatabaseEngine   = "PostgreSql"
              PathBase         = "adminapi"
              SigningKey       = "test"
              AdminDB          = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Admin;Application Name=EdFi.Ods.AdminApi;"
              SecurityDB       = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Security;Application Name=EdFi.Ods.AdminApi;"
          }
          ./build.ps1 -APIVersion "${{ steps.resolve-version.outputs.version }}" -Configuration Release -DockerEnvValues $p -Command GenerateOpenAPI

      - name: Upload OpenAPI artifacts
        uses: actions/upload-artifact@4cec3d8aa04e39d1a68397de0c4cd6fb9dce8ec1 # v4.6.2
        with:
          name: admin-api-openapi-${{ steps.resolve-version.outputs.version }}
          path: |
            docs/api-specifications/openapi-yaml/admin-api-v2-${{ steps.resolve-version.outputs.version }}.yaml
            docs/api-specifications/openapi-yaml/admin-api-v3-${{ steps.resolve-version.outputs.version }}.yaml
          if-no-files-found: error
```

- [ ] **Step 2: Verify the YAML parses**

```powershell
Get-Content .github/workflows/openapi-md.yml -Raw | ConvertFrom-Yaml
```

Expected: no error. (If `ConvertFrom-Yaml` isn't available, `Install-Module powershell-yaml -Scope CurrentUser -Force` first, or simply confirm no indentation/tab errors by eye plus a successful `actionlint` run if that tool is installed locally.)

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/openapi-md.yml
git commit -m "Publish OpenAPI v2/v3 specs as workflow artifacts from a resolved ref, add weekly schedule"
```

- [ ] **Step 4: Verify via an actual manual dispatch (post-merge/post-push)**

This step can only be exercised once the branch/commit exists on GitHub (`workflow_dispatch` requires the workflow file to be present on a branch GitHub knows about):

1. Push the branch: `git push -u origin <branch-name>`
2. Dispatch with an explicit existing released version (semver → tag): `gh workflow run "Generate OpenAPI definitions" --ref <branch-name> -f version=2.3.2`
3. Wait for the run to complete: `gh run watch $(gh run list --workflow="Generate OpenAPI definitions" --branch <branch-name> --limit 1 --json databaseId --jq '.[0].databaseId')`
4. Confirm in the run logs that the checkout step resolved to tag `v2.3.2` (not the dispatch branch) and that the run's Artifacts section contains `admin-api-v2-2.3.2.yaml` and `admin-api-v3-2.3.2.yaml`. Note: `v2.3.2` predates this change, so per the documented forward-only limitation this dispatch is expected to **fail** at the `GenerateOpenAPI` command (unrecognized command in that tag's `build.ps1`) — this failure itself is the proof that ref-based checkout is working correctly; do not attempt to make this specific historical tag succeed.
5. Dispatch with no version: `gh workflow run "Generate OpenAPI definitions" --ref <branch-name>` and confirm the checkout step resolves to `main` and the artifact is named `admin-api-openapi-latest` containing `admin-api-v2-latest.yaml` / `admin-api-v3-latest.yaml`.
6. Dispatch with a branch name (e.g. the working branch itself): `gh workflow run "Generate OpenAPI definitions" --ref <branch-name> -f version=<branch-name>` and confirm the checkout step resolves to that branch rather than `main` or a tag.

---

## Self-Review Notes

- **Spec coverage:** two-pass v2/v3 generation with `adminApiMode` switching (Task 1 Steps 3–5/7), widdershins removal (Task 1 Step 4), command rename (Task 1 Steps 2/6), Swashbuckle CLI bump to `10.2.3` (Task 2 Step 1), ref resolution (semver→tag / raw ref / blank→main) (Task 2 Step 1), weekly Sunday 06:00 UTC schedule (Task 2 Step 1 cron), artifact publishing instead of branch/commit/PR (Task 2 Step 1), `permissions: read-all` (Task 2 Step 1), forward-only historical-tag limitation documented and exercised (Task 2 Step 4.4) — all covered.
- **Placeholder scan:** no TBD/TODO; all code blocks are complete, runnable content.
- **Type/name consistency:** `GenerateOpenAPI` (function, now parameterized by `-DocVersion`) / `Invoke-GenerateOpenAPI` (invoker, loops `-DocVersion`/`-AdminApiMode` together) / `GenerateOpenAPI` (CLI `-Command` value) / `UpdateAppSettingsForAdminApi -AdminApiMode` carried consistently across Task 1 Steps 3–6 and referenced correctly in Task 2's `-Command GenerateOpenAPI`. Output filename pattern `admin-api-v{2,3}-<version-label>.yaml` is consistent between Task 1 Step 4 and Task 2's artifact `path:`/verification steps. `resolve-version` step's `ref`/`version` outputs are named consistently between Task 2 Steps 1 and 4.
