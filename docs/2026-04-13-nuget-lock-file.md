# NuGet Lock File Adoption

## Problem

.NET projects using NuGet Central Package Management
(`Directory.Packages.props`) pin _direct_ dependency versions, but do not pin
_transitive_ dependency versions — the packages that your packages depend on. As
a result:

* Two restores at different times may resolve different transitive versions even
  if no `.csproj` or `Directory.Packages.props` changed.
* CI may silently pick up a newly published transitive package between the time
  a developer restores locally and the time CI runs.
* Supply-chain attacks targeting transitive dependencies (typosquatting,
  dependency confusion, compromised patch releases) are harder to detect because
  there is no committed record of what versions were previously resolved.

The goal is to lock the full dependency graph — direct and transitive — at exact
versions, enforce that lock in CI, and keep it automatically up to date when
Dependabot bumps a direct dependency.

## Solution

### 1. Enable NuGet lock files

NuGet can generate a `packages.lock.json` file alongside each project's
`.csproj`. This file records the resolved version of every package in the full
dependency graph. When committed to source control, it becomes an auditable,
diffable record of exactly what is being restored.

Enable this globally via `Directory.Build.props` so every project in the
solution generates a lock file without per-project changes:

```xml
<PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

After adding this property, run `dotnet restore` once to generate the initial
lock files, then commit them to source control.

### 2. Enforce locked mode in CI

Add a `dotnet restore --locked-mode` step before each build job in CI. This
fails fast if the committed lock file does not match the current `.csproj` and
`Directory.Packages.props` state — for example, if someone adds a package
reference without updating the lock file.

Also add `**/packages.lock.json` to the CI path filter so that a PR that only
updates lock files (e.g., a Dependabot follow-up commit) still triggers CI.

### 3. Add a Dependabot cooldown period

Dependabot can be configured with a `cooldown` block that delays version-update
PRs for newly published packages by a configurable number of days. This reduces
supply-chain risk from packages that were published very recently — giving
security vendors time to flag poisoned or compromised releases before Dependabot
proposes them.

Typical values: 5 days for patch updates, 7 for minor, 14 for major.

### 4. Auto-regenerate lock files on Dependabot PRs

Dependabot updates `Directory.Packages.props` but does not update
`packages.lock.json`. Without intervention, every Dependabot PR will fail the
`--locked-mode` CI check.

Fix this with a dedicated GitHub Actions workflow that:

1. Fires on pull requests from `dependabot[bot]` that touch
   `Directory.Packages.props`.
2. Checks out the Dependabot branch using a PAT (required because GitHub
   silently downgrades `GITHUB_TOKEN` to read-only on Dependabot PR workflows,
   regardless of declared `permissions: contents: write`).
3. Runs `dotnet restore --force-evaluate` to regenerate the lock files from
   scratch.
4. Commits and pushes the updated lock files back to the Dependabot branch.

The `--force-evaluate` flag bypasses any cached resolution and re-derives the
full graph from `Directory.Packages.props` — important in a Central Package
Management project where that file is the authoritative version source.

### Expected flow for a Dependabot PR

1. Dependabot opens PR (modifies `Directory.Packages.props`).
2. The lock-file workflow fires, regenerates lock files, and pushes a follow-up
   commit.
3. That commit triggers CI again (because lock files are in the `paths` filter).
4. The `--locked-mode` check passes on the second CI run.

The first CI run on a Dependabot PR will fail the locked-mode check — this is
expected and resolves automatically. Do not re-request review until the second
run completes.

## Key implementation notes

* **PAT requirement:** The Dependabot lock-file workflow must use a PAT for
  checkout, not `GITHUB_TOKEN`. This is a GitHub platform constraint, not a
  configuration issue.
* **`find` over glob:** Shell `**` glob expansion is not enabled by default on
  Ubuntu Actions runners. Use `find . -name "packages.lock.json"` when staging
  or listing lock files in shell scripts.
* **Idempotent commit step:** Use `git diff --staged --quiet || git commit` so
  the step exits cleanly when no lock files changed (no-op), while still
  propagating a real commit failure as an error.
* **One lock file per project:** NuGet writes `packages.lock.json` next to each
  `.csproj`. Orphaned `.csproj` files not included in the solution will not get
  a lock file — this is expected.
