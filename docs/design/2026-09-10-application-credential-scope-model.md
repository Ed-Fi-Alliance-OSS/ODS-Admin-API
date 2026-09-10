# Application, credential and scope model (Admin API v2 / v3)

**Status:** reference, not a proposal
**Written:** 2026-09-10, from the ADMINAPI-1514 investigation
**Audience:** [ADMINAPI-1515](https://edfi.atlassian.net/browse/ADMINAPI-1515) and [AC-616](https://edfi.atlassian.net/browse/AC-616) implementers
**Verified on:** V3 / multitenant / PostgreSQL, local Docker stack

Every claim here was checked against the code and, where the table says so,
observed in a live database. It exists because ADMINAPI-1514, AC-616 and
ADMINAPI-1515 are three symptoms of one structural fact, and each was
investigated separately before that became clear.

## The tables involved

| Table | Holds |
| --- | --- |
| `dbo.Applications` | the Application |
| `dbo.ApiClients` | credentials (key/secret). **Many per Application** since AC-569 |
| `dbo.ApplicationEducationOrganizations` (AEO) | an education-organization grant, with an `Application_ApplicationId` column |
| `dbo.ApiClientApplicationEducationOrganizations` | join table, PK `(ApiClientId, ApplicationEducationOrganizationId)` |
| `dbo.ApiClientOdsInstances` | a credential's data-store grant |

`ApiClient` to AEO is many-to-many, mapped explicitly in
`Admin.DataAccess/Contexts/UsersContext.cs:65`. The V1 local model names the
inverse navigation `Clients`; the packaged model used by v2/v3 names it
`ApiClients`. Same column, different C# member — read the tree you are editing.

## Where each Application-level scope actually lives

This is the table that matters. Two scopes that a user edits identically on the
Application form are stored at **different levels**:

| Scope | Stored | Survives deleting every credential? |
| --- | --- | --- |
| Education organizations | AEO rows carry `Application_ApplicationId`. Credentials additionally join to them | **Yes** — the AEO rows remain |
| Data stores | **only** `ApiClientOdsInstances`, which hangs off a credential | **No** — nothing remains |
| Enabled state | `ApiClients.IsApproved`, per credential; aggregated for display | No credentials, no state |

There is **no** `Application`-to-`OdsInstance` association table. The
`Application` entity does expose an `OdsInstance` navigation
(`Admin.DataAccess/Models/Application.cs:32`), but nothing in the codebase ever
assigns it — a repository-wide grep for `application.OdsInstance =` returns
zero hits. It is not a usable home for this association.

Confirmed by the read path, which derives an Application's data stores
*entirely through its credentials*
(`Infrastructure/Database/Queries/GetDataStoreIdsByApplicationIdQuery.cs:28`,
v2's `GetOdsInstanceIdsByApplicationIdQuery.cs:29`):

```csharp
_context.ApiClientOdsInstances
    .Where(p => p.ApiClient.Application.ApplicationId == applicationId)
    .Select(p => p.OdsInstance.OdsInstanceId)
```

### Observed consequence: `dataStoreIds` is required but unstorable

On an Application with **zero credentials**, `PUT /applications/{id}`:

* **requires** a non-empty `dataStoreIds` — `RuleFor(m => m.DataStoreIds).NotEmpty()`
  in `Features/Applications/EditApplication.cs:114`, message
  *"Please provide at least one data store id."*;
* **cannot persist** what it receives, because there is no credential to hang
  `ApiClientOdsInstances` rows from;
* returns `204`, and a subsequent `GET` reports `"dataStoreIds": []`.

Observed 2026-09-09. Education organizations submitted in the same request
*are* persisted and returned, because AEO rows belong to the Application. That
asymmetry is the whole of ADMINAPI-1515 in one request.

This became reachable only through ADMINAPI-1514: before that fix the same
`PUT` returned **500**, so the discard could not be observed.

## The EF lazy-loading asymmetry

A repository-wide hazard, and the direct cause of several defects.

`UseLazyLoadingProxies()` is configured **only** in V1
(`EdFi.Ods.AdminApi.V1/Admin.DataAccess/Contexts/UsersContextFactory.cs:50`).
The v2 and V3 contexts are built with `UseSqlServer` / `UseNpgsql` alone
(`Infrastructure/Services/Tenants/TenantSpecificDbContextProvider.cs:69`).

Consequently, in v2/v3 an un-`Include`d collection navigation is **neither null
nor an error** — it is the empty collection the entity constructor assigned
(`Application`'s constructor initialises `ApiClients`, `Profiles` and
`ApplicationEducationOrganizations`). Reading it yields nothing, silently.

Code written against V1's semantics changes meaning when copied into v2/v3
without its `Include`s. `AddApiClientCommand` did exactly that and produced
credentials with no education-organization rows and a null `User`; both were
observed in the database before ADMINAPI-1514 corrected them.

**Rule of thumb:** in v2/v3, if a command reads a navigation, the query must
`Include` it. There is no runtime signal when it does not.

## The defect class: collections assumed singular

Every defect in this area is the same mistake — code written when *one
Application meant one ApiClient*, an assumption AC-569 removed without
revisiting callers. The symptom depends only on which LINQ operator was used:

| Operator | Behaviour once the collection is not exactly one |
| --- | --- |
| `.Single()` | throws — loud, harmless, blocks the request |
| `.All(...)` | vacuously `true` on empty — silent wrong answer |
| `.First()` | picks an arbitrary element — silent wrong target |
| `.Select(...)` on an unloaded navigation | yields nothing — silent data loss |

### Fixed in ADMINAPI-1514

* `EditApplicationCommand` — `.Single()` on `ApiClients`; now iterates all.
* `EditApplicationCommand` — assigned `apiClient.Name = model.ApplicationName`,
  overwriting user-chosen credential names; now never writes `Name`.
* `ApplicationMapper` — `ApiClients.All(a => a.IsApproved)` reported
  `enabled: true` for an Application with no credentials.
* `EditApplicationCommand` — rebuilt AEO rows attached to one credential only,
  which would have dropped every other credential's education-organization
  scope once the 500 was fixed.
* `AddApiClientCommand` — read `EducationOrganizationIds()` and
  `Vendor.Users` without `Include`ing either.

### Still present, deliberately out of that ticket's scope

* **`RegenerateApplicationApiClientSecretCommand.cs:32`** (v2 and V3) —
  `application.ApiClients.First()`. On a multi-credential Application,
  `PUT /applications/{id}/reset-credential` silently regenerates **one
  arbitrary credential's** secret and returns that credential's key. It does
  not throw, because `First()` on a non-empty collection succeeds.

  **Deliberately deferred past release 2.4**, on this basis:

  * The endpoint ships **disabled**. `AppSettings.EnableApplicationResetEndpoint`
    has no initialiser, so it defaults to `false`
    (`EdFi.Ods.AdminApi.Common/Settings/AppSettings.cs:25` — contrast
    `EnableDataStoreManagement`, which is `= true`), and the shipped
    `appsettings.json` of both v2 and V3 sets it to `false` explicitly.
    `ResetApplicationCredentials.HandleResetCredentials` rejects the request
    before reaching any data when the flag is off. The path is unreachable in a
    default install.
  * ADMINAPI-1514 neither worsened it nor made it newly reachable — unlike the
    `dataStoreIds` case above, whose exposure the 500 was masking. It behaves
    identically before and after that fix.
  * A correct fix is a **contract change**, not a bug fix, so it needs product
    sign-off rather than a freeze-window patch. `ApplicationResult` carries a
    single `Key`/`Secret` and cannot represent "rotated every credential", which
    is strong evidence the endpoint's contract assumes one credential. The
    likely resolution is to reject the ambiguous case (`400`/`409`) and point
    callers at `PUT /apiClients/{id}/reset-credential`, the per-credential route
    AC-569 added.

  Counter-consideration recorded because it nearly reversed the decision:
  AC-616's guidance tells users to create the replacement credential *before*
  deleting the old one, so 2.4 actively grows the multi-credential population.
  Absent the feature flag, the trigger would be common rather than rare and this
  would have warranted fixing inside ADMINAPI-1514.

  Mitigation for 2.4, documentation only: operators who enable
  `EnableApplicationResetEndpoint` should use the per-credential route for
  multi-credential Applications. Needs its own ticket for the next release.
* **`EditApiClient` has no name-uniqueness check.** `AddApiClient` enforces
  uniqueness within an Application (`AddApiClient.cs:112-115`), but the edit
  validator covers only length and data-store ids, so a credential can be
  renamed into a collision with a sibling.
* **V1** carries the same `.Single()` at `EditApplicationCommand.cs:43` but is
  not reachable: V1 exposes no ApiClient endpoints and always creates exactly
  one credential per Application.

## Observed evidence

Application with two education organizations and two credentials, before
ADMINAPI-1514 — note that credential 2 holds **no** junction rows, because it
was added through `POST /apiClients`:

```
 apiclientid |         name          | user_userid      client | aeo_id
-------------+-----------------------+-------------    --------+--------
           1 | Nightly Sync          |           1          1 |      1
           2 | nightly-sync-rotation |        NULL          1 |      2
```

After the fix, following one `PUT /applications/{id}`:

```
 client | aeo_id
--------+--------
      1 |      5
      1 |      6
      2 |      5     <- credential 2 gained the Application's scope
      2 |      6
```

Two facts for ADMINAPI-1515 to rely on:

1. **AEO rows are deleted and rebuilt wholesale** on every Application edit
   (`EditApplicationCommand`), so their ids change even when the
   education-organization set does not. Anything keyed on
   `ApplicationEducationOrganizationId` must not assume stability.
2. **Education-organization scope self-heals on edit; `user_userid` does not.**
   `EditApplicationCommand` never writes `User`, so credentials created before
   ADMINAPI-1514 keep `user_userid = NULL` until recreated.

## Implications for ADMINAPI-1515

* The read-path fallback that story proposes must cover **both** query names —
  `GetOdsInstanceIdsByApplicationIdQuery` (v2) and
  `GetDataStoreIdsByApplicationIdQuery` (V3). They are separate files.
* The `PUT` validator already demands `dataStoreIds`, so once an
  Application-level association exists, no validator change is needed — the
  value simply becomes storable. Worth an explicit acceptance criterion:
  *given an Application with zero credentials, when `PUT /applications/{id}` is
  called with `dataStoreIds`, then that list is persisted and returned.*
* `Application.OdsInstance` is dead, not merely unused: adopting it would mean
  defining semantics from scratch, with no existing data to migrate.
* AEO rows already demonstrate the pattern this story wants for data stores —
  an Application-level grant table that credentials join to. Mirroring that
  shape keeps the two scopes symmetric, which is what makes the current bug
  class possible to reason about.

## Related

* [ADMINAPI-1514](https://edfi.atlassian.net/browse/ADMINAPI-1514) — the
  empty-and-multiple-collection defects, fixed
* [ADMINAPI-1515](https://edfi.atlassian.net/browse/ADMINAPI-1515) — the
  Application-level data-store association
* [AC-616](https://edfi.atlassian.net/browse/AC-616) — blocked deleting an
  Application's last credential in Admin App. Note the Admin **API** still
  permits it: `DELETE /apiClients/{id}` on the last credential returns `204`,
  so the zero-credential state remains reachable
* [AC-569](https://edfi.atlassian.net/browse/AC-569) — shipped multi-credential
  management, which made this class of defect reachable
