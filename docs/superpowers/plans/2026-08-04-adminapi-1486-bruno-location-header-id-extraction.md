# ADMINAPI-1486: Standardize Bruno E2E Location-Header ID Extraction (v2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the fragile `.split("/")[2]` hardcoded-index ID extraction with `.split("/").pop()` in the 8 remaining v2 Bruno E2E files that still use it, matching the convention already documented in `CLAUDE.md:32` and already applied in the `Manage/` folder and throughout v3.

**Architecture:** Pure mechanical text substitution — one line per file, trailing `[2]` → `.pop()`. No logic, no interfaces, no new dependencies. Verification is grep-based (confirm the pattern is gone from these 8 files and no new occurrences exist) plus a manual run of the Bruno E2E suite, which the user performs after this plan completes (out of scope for this plan — requires the Docker-based E2E stack).

**Tech Stack:** Bruno `.bru` test scripts (JavaScript post-response scripts), git.

## Global Constraints

- Repo for all work: `C:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API` (a different repo from the current working directory).
- Branch: create `ADMINAPI-1486` off `origin/main`. Do not touch the current `ADMINAPI-1327` branch or its uncommitted changes.
- Only the trailing `[2]` → `.pop()` substitution is allowed per line. No other change to variable names, surrounding code, or downstream assertions.
- Exactly these 8 files are in scope — no others:
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/POST - OdsInstances.bru:33`
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/POST - OdsInstances - Invalid Existing Name.bru:67`
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Multitenant Isolation - OdsInstances/POST - OdsInstances - Tenant1.bru:39`
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstanceContexts/POST - OdsInstanceContexts.bru:81`
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstanceDerivatives/POST - OdsInstanceDerivatives.bru:81`
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru:54`
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru:53`
  - `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Vendors/POST - Vendors.bru:29`
- Do not attempt to spin up the Docker E2E stack or run `./eng/run-e2e-bruno.ps1` — that verification is the user's responsibility after this plan completes.
- Out of scope: v3 Bruno suite (already compliant — 0 occurrences confirmed), v1 Bruno suite (doesn't use this pattern).

---

### Task 1: Create the working branch

**Files:** None (git operation only).

**Interfaces:** None.

- [ ] **Step 1: Fetch latest origin/main**

```bash
cd "C:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API"
git fetch origin
```

- [ ] **Step 2: Create and check out branch `ADMINAPI-1486` from `origin/main`**

```bash
git checkout -b ADMINAPI-1486 origin/main
```

Expected: branch created and checked out cleanly. Confirm with:

```bash
git branch --show-current
```
Expected output: `ADMINAPI-1486`

- [ ] **Step 3: Confirm the working tree is clean before editing**

```bash
git status --short
```
Expected: no output (nothing pending) — the unrelated `ADMINAPI-1327` uncommitted changes (Docker compose, docs/http files) must NOT appear here, since this is a fresh checkout from `origin/main`.

---

### Task 2: Replace `.split("/")[2]` with `.split("/").pop()` in the 8 v2 Bruno files

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/POST - OdsInstances.bru:33`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/POST - OdsInstances - Invalid Existing Name.bru:67`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Multitenant Isolation - OdsInstances/POST - OdsInstances - Tenant1.bru:39`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstanceContexts/POST - OdsInstanceContexts.bru:81`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstanceDerivatives/POST - OdsInstanceDerivatives.bru:81`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru:54`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru:53`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Vendors/POST - Vendors.bru:29`

**Interfaces:** None — each edit is self-contained within its own file; no shared state between these 8 files.

- [ ] **Step 1: Record baseline — confirm exactly 8 occurrences exist before editing**

```bash
cd "C:/GAP/EdFi/ODS-Admin-API/ODS-Admin-API"
grep -rn "split(\"/\")\[2\]" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2" | wc -l
```
Expected: `8`

- [ ] **Step 2: Edit `OdsInstances/POST - OdsInstances.bru:33`**

Before:
```js
      const id = res.getHeader("location").split("/")[2];
```
After:
```js
      const id = res.getHeader("location").split("/").pop();
```

- [ ] **Step 3: Edit `OdsInstances/POST - OdsInstances - Invalid Existing Name.bru:67`**

Before:
```js
      const id = odsResponse.headers.get("location").split("/")[2];
```
After:
```js
      const id = odsResponse.headers.get("location").split("/").pop();
```

- [ ] **Step 4: Edit `Multitenant Isolation - OdsInstances/POST - OdsInstances - Tenant1.bru:39`**

Before:
```js
          const id = res.getHeader("location").split("/")[2];
```
After:
```js
          const id = res.getHeader("location").split("/").pop();
```

- [ ] **Step 5: Edit `OdsInstanceContexts/POST - OdsInstanceContexts.bru:81`**

Before:
```js
      const id = res.getHeader("location").split("/")[2];
```
After:
```js
      const id = res.getHeader("location").split("/").pop();
```

- [ ] **Step 6: Edit `OdsInstanceDerivatives/POST - OdsInstanceDerivatives.bru:81`**

Before:
```js
      const id = res.getHeader("location").split("/")[2];
```
After:
```js
      const id = res.getHeader("location").split("/").pop();
```

- [ ] **Step 7: Edit `Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru:54`**

Before:
```js
        const id = response.headers.location.split("/")[2];
```
After:
```js
        const id = response.headers.location.split("/").pop();
```

- [ ] **Step 8: Edit `Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru:53`**

Before:
```js
        const id = response.headers.location.split("/")[2];
```
After:
```js
        const id = response.headers.location.split("/").pop();
```

- [ ] **Step 9: Edit `Vendors/POST - Vendors.bru:29`**

Before:
```js
      const id = res.getHeader("location").split("/")[2];
```
After:
```js
      const id = res.getHeader("location").split("/").pop();
```

- [ ] **Step 10: Verify no occurrences of the old pattern remain in the v2 suite**

```bash
grep -rn "split(\"/\")\[2\]" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2"
```
Expected: no output (exit code 1 / no matches).

- [ ] **Step 11: Verify exactly 8 occurrences of the new pattern exist, one per target file**

```bash
grep -rln "split(\"/\")\.pop()" "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2" | wc -l
```
Expected: `8`

- [ ] **Step 12: Diff review — confirm each changed file has exactly one changed line, with no other edits**

```bash
git diff --stat
```
Expected: exactly 8 files listed, each showing `1 file changed, 1 insertion(+), 1 deletion(-)` in the stat summary (i.e., one changed line per file).

```bash
git diff
```
Expected: every `-`/`+` pair differs only in `[2]` vs `.pop()` — no other token changed.

- [ ] **Step 13: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/POST - OdsInstances.bru" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/POST - OdsInstances - Invalid Existing Name.bru" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Multitenant Isolation - OdsInstances/POST - OdsInstances - Tenant1.bru" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstanceContexts/POST - OdsInstanceContexts.bru" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstanceDerivatives/POST - OdsInstanceDerivatives.bru" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru" \
        "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Vendors/POST - Vendors.bru"

git commit -m "[ADMINAPI-1486] Standardize Bruno E2E location-header ID extraction in v2 to .split(\"/\").pop()"
```

---

## After This Plan (not part of implementation — user-performed)

Run the v2 Bruno E2E suite to confirm all 8 affected tests still pass:

```bash
./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TenantMode multitenant -TearDown
./eng/run-e2e-bruno.ps1 -ApiVersion 2 -TenantMode singletenant -TearDown
```
