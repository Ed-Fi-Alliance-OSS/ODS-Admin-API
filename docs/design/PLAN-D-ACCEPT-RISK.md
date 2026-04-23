# Plan D — Accept the risk (no change)

## Context

See [DBINSTANCE-PROVISIONING-JOBS.md § Context isolation risk and remediation options](DBINSTANCE-PROVISIONING-JOBS.md#context-isolation-risk-and-remediation-options) for the full problem statement and discovery history.

## Summary

Do not change `HashtableContextStorage` or any related code. Document the known race condition as a limitation and add an operational constraint that concurrent multi-tenant provisioning must not be relied upon in production.

**This plan is only appropriate when all of the following are true:**

* The deployment is low-traffic.
* Concurrent provisioning jobs for two different tenants at the exact same millisecond is practically impossible given the use pattern.
* The team accepts that a future scaling requirement will require revisiting this.

## What "accepting the risk" means in practice

The race is **silent and non-deterministic**. If it occurs:

* The provisioner connects to the wrong tenant's `EdFi_Master` and creates a database on the wrong server.
* The `OdsInstance.ConnectionString` stored at the end of the job may point to the wrong database.
* No exception is thrown; the job completes with `Completed` status while the data is corrupt.
* The only indication is a subsequent connection failure when the ODS API tries to use the stored connection string.

## Implementation steps

There is no code to change. The required actions are:

### Step 1 — Add operational documentation

Add a warning to the deployment and operations guide (or `docs/developer.md`) noting that in multi-tenant mode, simultaneous provisioning jobs for different tenants must not be allowed to run concurrently. Operators should ensure:

* Quartz max concurrency is limited to 1 for provisioning job types (configure `maxConcurrency` in Quartz settings, not in the job attribute).
* The provisioning sweep interval is set conservatively so that two sweeps are unlikely to overlap.

### Step 2 — Add a code comment

Add the following comment in `HashtableContextStorage` and/or `WebApplicationBuilderExtensions.cs`:

```csharp
// WARNING: HashtableContextStorage uses a shared Hashtable (singleton) with no thread isolation.
// Concurrent multi-tenant operations (two HTTP requests or two Quartz jobs for different tenants)
// can overwrite each other's context slot, causing a service to read the wrong tenant's connection strings.
// This is a known limitation. See docs/design/DBINSTANCE-PROVISIONING-JOBS.md §
// "Context isolation risk and remediation options" for analysis and remediation plans.
// Remediation is deferred. Do not increase Quartz concurrency for provisioning jobs in multi-tenant deployments.
```

### Step 3 — Record the decision

Add an Architecture Decision Record (ADR) or a note in `DBINSTANCE-PROVISIONING-JOBS.md` recording that the team reviewed the risk and consciously deferred remediation, with the trigger condition that will prompt revisitation (e.g., load testing, concurrent tenant count exceeds N, customer incident).

## Acceptance criteria

- [ ] Warning comment added to `HashtableContextStorage`.
- [ ] Operational constraint documented.
- [ ] Team decision recorded with a named trigger condition for revisitation.

## Risk summary

| Severity | Likelihood | Impact |
| --- | --- | --- |
| High (data corruption, silent) | Low in low-traffic deployments; increases proportionally with concurrent provisioning load | Wrong `OdsInstance.ConnectionString` stored; ODS API connection failures; potential cross-tenant data access |

## Recommendation

This plan is not recommended. The risk-to-effort ratio is unfavorable: Plan A fixes the issue with a single small class addition and zero interface changes. Choosing Plan D only avoids that small effort while leaving an invisible, hard-to-diagnose data integrity problem in production.
