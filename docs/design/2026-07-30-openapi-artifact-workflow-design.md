# OpenAPI Spec Generation Workflow (v2 + v3, Artifact Publishing)

## Problem

The existing `.github/workflows/openapi-md.yml` action is outdated:

- It only generates the `v2` Swagger document (`build.ps1`'s `GenerateOpenAPI` function hardcodes `swagger tofile ... v2`), even though Admin API now exposes a real `v3` surface (`AdminApiVersions` registers `v1`, `v2`, `v3`, and v3 has its own endpoints, e.g. `/dataStores/manage`).
- It generates a markdown summary via `widdershins`, which is no longer wanted.
- It commits the generated files to a branch in this repo and opens a PR here, which is more process than needed for what is essentially a generated build artifact.

## Goal

A manually-dispatchable (and weekly-scheduled) GitHub Action that generates both the `v2` and `v3` OpenAPI yaml specs for Admin API and publishes them as downloadable **workflow run artifacts** — no markdown, no git commits, no PRs, and nothing pushed to any other repository.

## `build.ps1` changes

- `GenerateOpenAPI` (`build.ps1:215`) currently exports one file for the `v2` Swagger document. It changes to loop over both `v2` and `v3` document names, producing two files:
  - `docs/api-specifications/openapi-yaml/admin-api-v2-$APIVersion.yaml`
  - `docs/api-specifications/openapi-yaml/admin-api-v3-$APIVersion.yaml`
- `GenerateDocumentation` (`build.ps1:230`, the `widdershins` call) is removed entirely.
- The `GenerateOpenAPIAndMD` command is renamed to `GenerateOpenAPI`:
  - `ValidateSet` entry at `build.ps1:79` updated.
  - `Invoke-GenerateOpenAPIAndMD` (`build.ps1:411`) renamed to `Invoke-GenerateOpenAPI`, drops its `GenerateDocumentation` step.
  - Switch statement at `build.ps1:644` updated to match.
- `docs/yaml-to-md/yaml-to-md.md` (a standalone manual how-to doc for turning yaml into markdown by hand) is unrelated to this automated workflow and is left untouched.

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

### Version resolution / validation

Replaces today's "Validate version" step. Logic:

- If `inputs.version` is blank (manual dispatch with no value) or the trigger is `schedule` (no inputs available at all): resolve `version` to `"latest"`, skip semver validation.
- Otherwise (`inputs.version` provided): must match `^\d+\.\d+\.\d+$`, same as today; throw on mismatch.

This makes the input optional rather than required, since `build.ps1`'s own `$APIVersion` parameter already defaults to `"0.1"` if omitted — the strict "required + regex" behavior was a workflow-level choice, not a script constraint.

### Steps (replacing everything from "Git create branch" onward in the current file)

1. Checkout `ODS-Admin-API` (unchanged).
2. Resolve/validate `version` as above.
3. Install Swashbuckle CLI (unchanged). **Remove** the "Install widdershins CLI" step.
4. Build and generate: `./build.ps1 -APIVersion <resolved-version> -Configuration Release -DockerEnvValues $p -Command GenerateOpenAPI` — now produces both `admin-api-v2-<version>.yaml` and `admin-api-v3-<version>.yaml`.
5. **Remove**: "Git create branch", "Git add files", "Commit file" (`ghcommit-action`), and "Create PR" steps — none of them are needed anymore.
6. **Add**: `actions/upload-artifact` step uploading both generated yaml files (e.g. artifact name `admin-api-openapi-<version>`, `retention-days` left at the org default unless a shorter/longer window is wanted later).

### Permissions

Drops from `contents: write` to `permissions: read-all` (no default) — the job no longer writes anything to the repository (no commits, no branches, no PRs).

## Explicitly out of scope

- Nothing is pushed, committed, or PR'd to `Ed-Fi-Alliance-OSS/Ed-Fi-API-Specifications` or to this repository. Downstream use of the generated specs (e.g., opening a PR in the spec repo) is a manual, human-driven step outside this workflow.
- No PAT or cross-repo credential is required.
- Markdown documentation generation (`widdershins`) is removed, not just skipped for this workflow — a developer wanting a markdown summary can still follow the manual procedure in `docs/yaml-to-md/yaml-to-md.md`.

## Testing

- `./build.ps1 -Command GenerateOpenAPI -APIVersion 2.4.0` locally produces both `admin-api-v2-2.4.0.yaml` and `admin-api-v3-2.4.0.yaml` under `docs/api-specifications/openapi-yaml/`, and no markdown file is produced.
- Manually dispatch the workflow with a version and confirm the run's Artifacts section contains both yaml files.
- Manually dispatch the workflow with no version and confirm files are named with `latest`.
- (Schedule trigger itself can't be tested on-demand; correctness of the cron expression and the `latest` fallback is verified by inspection plus the no-version manual-dispatch test above, which exercises the same code path.)
