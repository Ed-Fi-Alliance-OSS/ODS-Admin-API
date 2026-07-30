# OpenAPI v2/v3 Artifact-Publishing Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix `build.ps1` to generate both the Admin API `v2` and `v3` OpenAPI yaml specs (dropping markdown generation), and replace the `openapi-md.yml` GitHub Action's branch/commit/PR flow with a manually-dispatchable + weekly-scheduled workflow that publishes the two yaml files as a run artifact.

**Architecture:** No new components — this is a two-file change. `build.ps1` gains a second swagger-doc-name in its existing `GenerateOpenAPI` loop and loses its `GenerateDocumentation` (widdershins) function; the `GenerateOpenAPIAndMD` command is renamed `GenerateOpenAPI`. `.github/workflows/openapi-md.yml` gets a `schedule` trigger, an optional `version` input with an `"latest"` fallback, and its post-build steps are replaced with `actions/upload-artifact` (no git branch, no commit, no PR).

**Tech Stack:** PowerShell (`build.ps1`), GitHub Actions YAML, Swashbuckle.AspNetCore.Cli.

## Global Constraints

- No markdown/widdershins generation anywhere in this workflow (per `docs/design/2026-07-30-openapi-artifact-workflow-design.md`).
- Nothing is committed, branched, or PR'd — in this repo or any other — as a result of this workflow.
- No PAT or cross-repo credential is introduced.
- Scheduled run: Sunday 06:00 UTC (`cron: '0 6 * * 0'`).
- Manual dispatch `version` input is optional; blank input or a scheduled run resolves to the literal string `latest`; an explicit value must match `^\d+\.\d+\.\d+$`.
- Output filenames: `docs/api-specifications/openapi-yaml/admin-api-v2-<version>.yaml` and `admin-api-v3-<version>.yaml`.

---

### Task 1: Fix `build.ps1` — generate v2 + v3, remove markdown generation, rename command

**Files:**
- Modify: `build.ps1:79` (ValidateSet)
- Modify: `build.ps1:215-236` (`GenerateOpenAPI`, `GenerateDocumentation`)
- Modify: `build.ps1:411-418` (`Invoke-GenerateOpenAPIAndMD`)
- Modify: `build.ps1:644` (switch statement)

**Interfaces:**
- Produces: a PowerShell command `GenerateOpenAPI` (replacing `GenerateOpenAPIAndMD`), invoked as `./build.ps1 -Command GenerateOpenAPI -APIVersion <string>`. Writes `docs/api-specifications/openapi-yaml/admin-api-v2-<APIVersion>.yaml` and `admin-api-v3-<APIVersion>.yaml`. Produces no markdown file. This is the interface Task 2's workflow step calls.

- [ ] **Step 1: Reproduce current (wrong) behavior locally**

Run from the repo root:

```powershell
dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2 --create-manifest-if-needed
./build.ps1 -Command GenerateOpenAPIAndMD -APIVersion 9.9.9 -Configuration Release -DockerEnvValues @{ Authority = "http://api"; IssuerUrl = "https://localhost"; DatabaseEngine = "PostgreSql"; PathBase = "adminapi"; SigningKey = "test"; AdminDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Admin;Application Name=EdFi.Ods.AdminApi;"; SecurityDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Security;Application Name=EdFi.Ods.AdminApi;" }
Get-ChildItem docs/api-specifications/openapi-yaml/*9.9.9* , docs/api-specifications/markdown/*9.9.9*
```

Expected (documenting today's bug): only `admin-api-9.9.9.yaml` exists (no `v2`/`v3` split), and `admin-api-9.9.9-summary.md` also exists. Delete both generated files afterward and run `git restore Application/EdFi.Ods.AdminApi/appsettings.json` to leave the working tree clean:

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

- [ ] **Step 3: Rewrite `GenerateOpenAPI` to loop over v2/v3 and delete `GenerateDocumentation`**

Replace `build.ps1:215-236`:

```powershell
function GenerateOpenAPI {
    Invoke-Execute {
        Push-Location $solutionRoot/EdFi.Ods.AdminApi/
        $dllPath = "./bin/Release/net10.0/EdFi.Ods.AdminApi.dll"

        try {
            foreach ($docVersion in @("v2", "v3")) {
                $outputOpenAPI = "../../docs/api-specifications/openapi-yaml/admin-api-$docVersion-$APIVersion.yaml"
                dotnet tool run swagger tofile --output $outputOpenAPI --yaml $dllPath $docVersion
            }
        }
        finally {
            Pop-Location
        }
    }
}
```

(This removes the `GenerateDocumentation` function entirely — no widdershins call remains anywhere in `build.ps1`.)

- [ ] **Step 4: Rename `Invoke-GenerateOpenAPIAndMD` and drop its markdown step**

Replace `build.ps1:411-418`:

```powershell
function Invoke-GenerateOpenAPI {
    Invoke-Step { UpdateAppSettingsForAdminApi }
    Invoke-Step { DotNetClean }
    Invoke-Step { Restore }
    Invoke-Step { Compile }
    Invoke-Step { GenerateOpenAPI }
}
```

- [ ] **Step 5: Update the switch statement**

In `build.ps1:644`, replace:

```powershell
        GenerateOpenAPIAndMD { Invoke-GenerateOpenAPIAndMD }
```

with:

```powershell
        GenerateOpenAPI { Invoke-GenerateOpenAPI }
```

- [ ] **Step 6: Verify the fixed behavior locally**

```powershell
./build.ps1 -Command GenerateOpenAPI -APIVersion 9.9.9 -Configuration Release -DockerEnvValues @{ Authority = "http://api"; IssuerUrl = "https://localhost"; DatabaseEngine = "PostgreSql"; PathBase = "adminapi"; SigningKey = "test"; AdminDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Admin;Application Name=EdFi.Ods.AdminApi;"; SecurityDB = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Security;Application Name=EdFi.Ods.AdminApi;" }
Get-ChildItem docs/api-specifications/openapi-yaml/*9.9.9*
Get-ChildItem docs/api-specifications/markdown/*9.9.9* -ErrorAction SilentlyContinue
```

Expected: `admin-api-v2-9.9.9.yaml` and `admin-api-v3-9.9.9.yaml` both exist under `docs/api-specifications/openapi-yaml/`; the markdown `Get-ChildItem` returns nothing (no file matches, command produces no output / a non-terminating "not found" that's silenced).

Then clean up the generated test files and restore `appsettings.json` so nothing test-only gets committed:

```powershell
Remove-Item docs/api-specifications/openapi-yaml/*9.9.9* -ErrorAction SilentlyContinue
git restore Application/EdFi.Ods.AdminApi/appsettings.json
```

- [ ] **Step 7: Commit**

```bash
git add build.ps1
git commit -m "Generate v2 and v3 OpenAPI specs, remove widdershins markdown generation"
```

---

### Task 2: Replace the branch/PR workflow with artifact publishing + weekly schedule

**Files:**
- Modify: `.github/workflows/openapi-md.yml` (full rewrite)

**Interfaces:**
- Consumes: `./build.ps1 -Command GenerateOpenAPI -APIVersion <string> ...` from Task 1, which writes `docs/api-specifications/openapi-yaml/admin-api-v2-<version>.yaml` and `admin-api-v3-<version>.yaml`.

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
        description: 'Version name for output filenames, e.g. "2.4.0". Leave blank to use "latest".'
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
      - name: Checkout the Repo
        uses: actions/checkout@df4cb1c069e1874edd31b4311f1884172cec0e10 # v6.0.3

      - name: Resolve version
        id: resolve-version
        env:
          INPUT_VERSION: ${{ inputs.version }}
        run: |
          $version = $env:INPUT_VERSION

          if ([string]::IsNullOrWhiteSpace($version))
          {
              $version = "latest"
          }
          elseif ($version -ne "latest" -and $version -notmatch '^\d+\.\d+\.\d+$')
          {
              throw "Invalid version format: $version"
          }

          "version=$version" >> $env:GITHUB_OUTPUT

      - name: Install Swashbuckle CLI
        run: dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2 --create-manifest-if-needed

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
git commit -m "Publish OpenAPI v2/v3 specs as workflow artifacts, add weekly schedule"
```

- [ ] **Step 4: Verify via an actual manual dispatch (post-merge/post-push)**

This step can only be exercised once the branch/commit exists on GitHub (`workflow_dispatch` requires the workflow file to be present on a branch GitHub knows about):

1. Push the branch: `git push -u origin <branch-name>`
2. `gh workflow run "Generate OpenAPI definitions" --ref <branch-name> -f version=2.4.0`
3. Wait for the run to complete: `gh run watch $(gh run list --workflow="Generate OpenAPI definitions" --branch <branch-name> --limit 1 --json databaseId --jq '.[0].databaseId')`
4. Confirm the artifact exists and is named correctly: `gh run view --log <run-id> | grep -i "admin-api-openapi-2.4.0"` or check the run's Artifacts section in the GitHub UI — it should contain `admin-api-v2-2.4.0.yaml` and `admin-api-v3-2.4.0.yaml`.
5. Repeat with `-f version=` (empty) and confirm the artifact is named `admin-api-openapi-latest` and contains `admin-api-v2-latest.yaml` / `admin-api-v3-latest.yaml`.

---

## Self-Review Notes

- **Spec coverage:** v2+v3 generation (Task 1 Steps 3/6), widdershins removal (Task 1 Step 3), command rename (Task 1 Steps 2/4/5), optional `version` input + `latest` fallback (Task 2 Step 1), weekly Sunday 06:00 UTC schedule (Task 2 Step 1 cron), artifact publishing instead of branch/commit/PR (Task 2 Step 1), `permissions: read-all` / no `contents: write` (Task 2 Step 1) — all covered.
- **Placeholder scan:** no TBD/TODO; all code blocks are complete, runnable content.
- **Type/name consistency:** `GenerateOpenAPI` (function) / `Invoke-GenerateOpenAPI` (invoker) / `GenerateOpenAPI` (CLI `-Command` value) are the three related-but-distinct names carried consistently across Task 1 Steps 2–5 and referenced correctly in Task 2's `-Command GenerateOpenAPI`. Output filename pattern `admin-api-v{2,3}-<version>.yaml` is consistent between Task 1 Step 3 and Task 2's artifact `path:` and verification steps.
