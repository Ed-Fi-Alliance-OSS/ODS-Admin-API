# DbInstance Provisioning Jobs Design

This document is the durable design reference for the `POST /v2/dbinstances` background provisioning pipeline introduced by `ADMINAPI-1369`.

It documents the implemented architecture, runtime flow, configuration, prerequisites, and the technical decisions behind the current `CreateInstanceJob` and `CreatePendingDbInstancesDispatcherJob` behavior.

## Scope

In scope:

* `POST /v2/dbinstances`
* `CreateInstanceJob`
* `CreatePendingDbInstancesDispatcherJob`
* `adminapi.DbInstances` lifecycle transitions
* `adminapi.JobStatuses` tracking through the shared Quartz base
* `OdsInstances` synchronization and reconciliation
* Multi-tenant job identity and tenant-aware execution

Out of scope:

* Delete-instance processing
* Quartz persistent job store migration
* Broader `OdsInstance` validator redesign outside this workflow

## Top-level hierarchy

| Layer | Primary files | Responsibility |
| --- | --- | --- |
| API entry | `Application/EdFi.Ods.AdminApi/Features/DbInstances/AddDbInstance.cs` | Validate input, persist the initial `Pending` `DbInstance`, schedule immediate background work, and return `202 Accepted`. |
| Worker job | `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreateInstanceJob.cs` | Process one `DbInstance` from `Pending` to `Completed` or `Error`. |
| Recovery orchestration | `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreatePendingDbInstancesDispatcherJob.cs`, `Application/EdFi.Ods.AdminApi/Program.cs` | Schedule recurring sweeps, discover retryable records, and enqueue worker jobs. |
| Shared Quartz infrastructure | `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/AdminApiQuartzJobBase.cs`, `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/QuartzJobScheduler.cs`, `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/JobConstants.cs` | Persist `JobStatuses`, apply common job metadata, and avoid duplicate scheduling for recurring jobs. |
| State and external dependencies | `adminapi.DbInstances`, `adminapi.JobStatuses`, `IUsersContext.OdsInstances`, `ISandboxProvisioner`, connection-string builders, encryption provider | Store business state, track executions, provision databases, and create encrypted `OdsInstance` metadata. |

```mermaid
flowchart LR
    subgraph ApiLayer[API Layer]
        Endpoint[AddDbInstance feature\nPOST /v2/dbinstances]
        Command[AddDbInstanceCommand]
    end

    subgraph Scheduling[Scheduling and orchestration]
        Scheduler[Quartz scheduler]
        Worker[CreateInstanceJob]
        Dispatcher[CreatePendingDbInstancesDispatcherJob]
        Base[AdminApiQuartzJobBase]
    end

    subgraph Persistence[State and metadata]
        DbInstances[adminapi.DbInstances]
        JobStatuses[adminapi.JobStatuses]
        OdsInstances[OdsInstances via IUsersContext]
    end

    subgraph Dependencies[Execution dependencies]
        Provisioner[ISandboxProvisioner]
        Encryption[Encryption provider and connection builders]
        Tenants[Tenant-specific context provider]
        Config[AppSettings and connection strings]
    end

    Endpoint --> Command --> DbInstances
    Endpoint --> Scheduler
    Scheduler --> Worker
    Scheduler --> Dispatcher
    Worker --> Base --> JobStatuses
    Dispatcher --> Base
    Worker --> DbInstances
    Worker --> OdsInstances
    Worker --> Provisioner
    Worker --> Encryption
    Worker --> Tenants
    Worker --> Config
    Dispatcher --> DbInstances
    Dispatcher --> JobStatuses
    Dispatcher --> Tenants
```

## Core model and invariants

* `DbInstance.DatabaseTemplate` maps to both `SandboxType` and `OdsInstance.InstanceType`.
* `DbInstance.DatabaseName` is generated once as `EdFi_Ods_<normalized DbInstance.Name>_<normalized DbInstance.DatabaseTemplate>` and then reused on retries.
* Spaces in both `DbInstance.Name` and `DbInstance.DatabaseTemplate` are normalized to `_` when building `DbInstance.DatabaseName`.
* Duplicate leading `EdFi_Ods` prefix variants are removed from the normalized `DbInstance.Name` segment, case-insensitively, before composing the final database name.
* Prefix de-duplication applies only to the leading `DbInstance.Name` segment, not to later occurrences inside the user-provided name.
* If the normalized `DbInstance.Name` segment collapses to empty because it only contained a prefix variant, the final database name becomes `EdFi_Ods_<normalized DbInstance.DatabaseTemplate>`.
* `AddDbInstance` rejects requests whose generated database name would exceed 63 characters instead of trimming it.
* `AddDbInstance` rejects requests when the trimmed `DbInstance.Name` already exists in either `adminapi.DbInstances.Name` or `admin.OdsInstances.Name`.
* The synchronized final name is always `DbInstance.Name`.
* The final name is written to both `DbInstance.OdsInstanceName` and `OdsInstance.Name`.
* `OdsInstance.ConnectionString` is derived from the configured `EdFi_Ods` connection-string shape and encrypted with `AppSettings:EncryptionKey`.
* `CreateInstanceJob` only processes `Pending` rows.
* The dispatcher only scans rows in `Pending` or `Error`.
* `AddDbInstance` validates `DbInstance.Name` so only `A-Za-z0-9 _` characters are accepted before background work is scheduled.

## Runtime flow

### Immediate API flow

`POST /v2/dbinstances` is intentionally asynchronous. The endpoint persists the request, schedules `CreateInstanceJob`, and returns immediately. The heavy work stays in the worker so the API contract remains `202 Accepted` even when provisioning takes minutes.

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API as AddDbInstance endpoint
    participant Db as adminapi.DbInstances
    participant Quartz as Quartz scheduler
    participant Worker as CreateInstanceJob
    participant Provisioner as ISandboxProvisioner
    participant Users as IUsersContext.OdsInstances
    participant Status as adminapi.JobStatuses

    Client->>API: POST /v2/dbinstances
    API->>Db: Reject if DbInstance or OdsInstance name already exists
    API->>Db: Insert DbInstance with Pending status
    API->>Quartz: Schedule CreateInstanceJob StartNow
    API-->>Client: 202 Accepted with Location header
    Quartz->>Status: Mark run InProgress via base class
    Quartz->>Worker: Execute with DbInstanceId and optional TenantName
    Worker->>Db: Load row, require Pending, set InProgress
    Worker->>Db: Generate and persist DatabaseName when missing
    Worker->>Provisioner: AddSandboxAsync(DatabaseName, SandboxType)
    Worker->>Users: Insert or update name-matched OdsInstance
    Worker->>Db: Set OdsInstanceId, OdsInstanceName, Completed
    Worker->>Status: Mark run Completed or Error
```

### Recovery and retry flow

The dispatcher owns recurring discovery and retry gating. `CreateInstanceJob` stays focused on one `Pending` row at a time and does not decide by itself when an `Error` row is eligible to replay.

```mermaid
flowchart TD
    Startup[Program.cs startup] --> Schedule[Schedule recurring dispatcher job every sweep interval]
    Schedule --> Sweep[Dispatcher query for Pending and Error DbInstances]
    Sweep --> Pending{Status is Pending?}
    Pending -->|Yes| QueuePending[Schedule CreateInstanceJob immediately]
    Pending -->|No| RetryCheck[Count Error runs in adminapi.JobStatuses by job key prefix]
    RetryCheck --> Eligible{Error count < max retry attempts?}
    Eligible -->|Yes| Promote[Reset DbInstance to Pending and update timestamps]
    Promote --> QueueRetry[Schedule CreateInstanceJob immediately]
    Eligible -->|No| Exhausted[Leave DbInstance in Error]
    QueuePending --> WorkerRun[Worker replays the whole create flow]
    QueueRetry --> WorkerRun
```

## Job identity and payloads

### Worker job identity

`CreateInstanceJob` uses per-record Quartz identities:

* single-tenant: `CreateInstanceJob-{DbInstanceId}`
* multi-tenant: `CreateInstanceJob-{TenantName}-{DbInstanceId}`

Payload:

* `DbInstanceId`
* `TenantName` when multi-tenancy is enabled

### Dispatcher job identity

The recurring dispatcher is scheduled from `Program.cs`:

* single-tenant: `CreatePendingDbInstancesDispatcherJob`
* multi-tenant: `CreatePendingDbInstancesDispatcherJob_{TenantName}`

Payload:

* `TenantName` when multi-tenancy is enabled

### Job status tracking model

All jobs inherit from `AdminApiQuartzJobBase`.

The base class:

* derives a job id from `context.JobDetail.Key.Name`
* creates a run id as `{jobId}_{context.FireInstanceId}`
* writes `InProgress`, `Completed`, or `Error` into `adminapi.JobStatuses`

Retry counting is based on persisted `JobStatuses` rows that match the `CreateInstanceJob` identity prefix for the current `DbInstance`.

## Status model

### `DbInstance.Status`

Used by the create flow:

* `Pending`: eligible for worker execution
* `InProgress`: currently being processed
* `Completed`: provisioning and synchronization succeeded
* `Error`: the last worker attempt failed

### Execution semantics

* The endpoint inserts the initial row as `Pending`.
* The worker flips `Pending -> InProgress -> Completed` on success.
* The worker flips the row to `Error` when execution fails.
* The dispatcher decides whether an `Error` row is promoted back to `Pending`.

## Prerequisites

The feature works correctly only when these prerequisites are in place:

* Admin API runs in `v2` mode so startup scheduling in `Program.cs` can register the recurring dispatcher.
* Quartz services are registered and the hosted service is enabled.
* The Admin API database migrations have been applied so `adminapi.DbInstances` and `adminapi.JobStatuses` exist.
* `AppSettings:EncryptionKey` is configured with a valid base64-encoded key because `CreateInstanceJob` encrypts the final `OdsInstance.ConnectionString`.
* `ConnectionStrings:EdFi_Ods` points at the normal ODS server shape used to build per-sandbox connection strings.
* `ConnectionStrings:EdFi_Master` points at the maintenance database used by provisioning. For PostgreSQL this should be the `postgres` database, not an ODS database.
* When multi-tenancy is enabled, the active tenant must have tenant-specific `EdFi_Admin`, `EdFi_Security`, `EdFi_Ods`, and `EdFi_Master` connection strings available before the job runs.

## Configuration reference

| Setting | Used by | Why it matters |
| --- | --- | --- |
| `AppSettings:CreateDbInstancesSweepIntervalInMins` | `Program.cs` | Controls how often the recurring dispatcher looks for `Pending` and retryable `Error` records. |
| `AppSettings:CreateDbInstancesMaxRetryAttempts` | `CreatePendingDbInstancesDispatcherJob` | Caps the number of times a failed create flow can be requeued. |
| `AppSettings:MultiTenancy` | endpoint, worker, dispatcher, startup scheduling | Turns on tenant-aware job keys, `TenantName` payload propagation, and tenant-specific context resolution. |
| `AppSettings:DatabaseEngine` | provisioner and connection-string handling | Must match the database platform used for sandbox provisioning. |
| `AppSettings:EncryptionKey` | `CreateInstanceJob` | Required to encrypt the persisted `OdsInstance.ConnectionString`. |
| `ConnectionStrings:EdFi_Ods` | `CreateInstanceJob` | Provides the connection-string shape used to build the final encrypted ODS connection string in single-tenant mode. |
| `Tenants:{tenant}:ConnectionStrings:EdFi_Ods` | `CreateInstanceJob` | Provides the tenant-specific ODS connection-string shape when multi-tenancy is enabled. |
| `ConnectionStrings:EdFi_Master` | `ISandboxProvisioner` | Provides the maintenance-database connection used during sandbox create and recreate operations. |
| `Tenants:{tenant}:ConnectionStrings:EdFi_Ods` | `CreateInstanceJob` | Provides the tenant-specific ODS connection-string shape when multi-tenancy is enabled. |
| `Tenants:{tenant}:ConnectionStrings:EdFi_Master` | `ISandboxProvisioner` via `ConfigConnectionStringsProvider` | Provides the per-tenant maintenance-database connection when multi-tenancy is enabled. Overrides the top-level `ConnectionStrings:EdFi_Master` for that tenant's provisioning job. |

## Technical decisions and rationale

### Why the endpoint does not provision inline

The endpoint stays asynchronous and returns `202 Accepted` because provisioning is background work. Executing the full create flow on the request thread would make request latency unpredictable, couple API availability to provisioning latency, and duplicate background execution logic that is already needed for retries and restart recovery.

### Why there are two jobs

The implementation deliberately uses two jobs:

* `CreateInstanceJob` owns single-record execution.
* `CreatePendingDbInstancesDispatcherJob` owns recurring discovery, retry gating, and rescheduling.

This separation keeps the worker small and deterministic. It also avoids teaching the worker how to scan the database, calculate retry eligibility, or coordinate recurring sweeps.

### Why retries replay the whole flow

Retries reuse the same `DbInstance.DatabaseName` and replay the full create path instead of special-casing only one step. That keeps the happy path and the retry path aligned, and it relies on `AddSandboxAsync` being able to recreate the sandbox for the same database name.

### Why character validation happens at the endpoint

The sandbox provisioners only accept database identifiers that contain letters, numbers, and underscores. `AddDbInstance` therefore rejects `DbInstance.Name` values outside `A-Za-z0-9 _` before the worker is scheduled. That keeps invalid characters out of the persisted create flow while still allowing spaces in the request contract and normalizing those spaces to underscores in the worker-generated database name.

### Why the feature rejects long database names

The feature uses a 63-character portable limit for generated database names and rejects requests above that limit instead of trimming them. PostgreSQL may apply identifier-length behavior differently than SQL Server, but the Admin API persists `DbInstance.DatabaseName`, uses it to build the encrypted ODS connection string, and uses the same value for provisioning and status checks. Rejecting oversized names keeps the persisted value aligned with the actual provisioned database across supported engines and avoids silent truncation collisions.

### Why retry count comes from `JobStatuses`

Retry counts are derived from persisted `adminapi.JobStatuses` rows by worker-job key prefix instead of adding dedicated retry columns to `DbInstance`. That keeps retry accounting inside the existing Quartz execution trail and avoids additional schema changes for this feature.

### Reconciliation strategy

The main retry risk is partial success across `OdsInstance` and `DbInstance` persistence:

* `OdsInstance` insert succeeds
* `DbInstance` update fails
* a later retry reaches the same final-name `OdsInstance`

The implemented behavior handles this by looking up an existing `OdsInstance` by final synchronized name and reusing that row during replay. That keeps retry behavior whole-flow and avoids duplicate final-name rows.

### Multi-tenancy strategy

Multi-tenancy is a job payload concern as well as a configuration concern:

* scheduled jobs must carry `TenantName`
* tenant identity becomes part of the Quartz job key
* worker and dispatcher resolve tenant-specific contexts before reading or writing state
* tenant-specific `EdFi_Ods` connection-string shape is used when building the encrypted `OdsInstance.ConnectionString`

#### How the job sets tenant context

HTTP requests have `TenantResolverMiddleware` running automatically for every request, which calls `IContextProvider<TenantConfiguration>.Set(tenantConfig)` so downstream services always see the right tenant.

Quartz jobs run **outside the HTTP pipeline** — no middleware runs. `CreateInstanceJob` therefore mimics the middleware by calling `Set(tenantConfiguration)` explicitly at the start of execution and `Set(null)` in the `finally` block. This is what allows `ConfigConnectionStringsProvider` and `SandboxProvisionerBase` to resolve the correct per-tenant `EdFi_Master` and `EdFi_Ods` connection strings during provisioning.

```
HTTP request path:
  TenantResolverMiddleware.Set(tenantConfig) → Controller → Provisioner reads EdFi_Master ✓

Quartz job path (no middleware):
  CreateInstanceJob.Set(tenantConfig) → Provisioner reads EdFi_Master ✓
  CreateInstanceJob.Set(null) [in finally]
```

#### How connection strings are resolved

`ConfigConnectionStringsProvider` builds the connection string map dynamically on every call (registered as `Transient`). In multi-tenant mode it reads the current ambient `TenantConfiguration` from `IContextProvider<TenantConfiguration>` and overlays per-tenant values on top of the base `ConnectionStrings` config section:

| Priority | Source | Applies when |
| --- | --- | --- |
| 1 (highest) | `Tenants:{tenant}:ConnectionStrings:*` via `TenantConfiguration` | Multi-tenancy enabled and tenant context is set |
| 2 (fallback) | Top-level `ConnectionStrings:*` in config / environment | Always present as base |

#### Known limitation: ambient context isolation

The current `IContextStorage` implementation (`HashtableContextStorage`) is a singleton backed by a plain `Hashtable` — one slot per type, shared across all threads. This means concurrent operations (two HTTP requests for different tenants, or a job and an HTTP request running simultaneously) can overwrite each other's context slot, causing a service to read the wrong tenant's connection strings.

This is a **pre-existing** architectural limitation that was present before this feature was introduced. In a purely HTTP-driven system it was latent: the middleware always set the slot at the very start of each request, so in practice the window between a wrong `Set()` and the downstream read was narrow and rarely triggered in low-traffic deployments. The provisioning job makes it materially worse for two reasons:

* **A new, non-HTTP writer exists.** Quartz threads call `Set()` and `Set(null)` from outside the HTTP pipeline. A job running during an in-flight HTTP request can overwrite the slot mid-request and then null it out in the `finally` block, leaving the HTTP request with no tenant context for the remainder of its execution.
* **The race window is much longer.** Provisioning jobs run for seconds to minutes (database creation, template copying). During that entire span the shared slot is held by the job, making collisions with concurrent HTTP requests far more likely than the sub-millisecond window between two rapid HTTP middleware calls.

See the [Context isolation risk and remediation options](#context-isolation-risk-and-remediation-options) section for the full analysis and remediation plans.

### Restart recovery strategy

Restart recovery depends on the recurring dispatcher, not on a persistent Quartz job store. If the process restarts, the next sweep reconstructs work from `DbInstances` state and the persisted `JobStatuses` history.

## Validation and verification coverage

Current implementation coverage is centered in:

* `Application/EdFi.Ods.AdminApi.UnitTests/Features/DbInstances/AddDbInstanceTests.cs`
* `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/CreateInstanceJobTests.cs`
* `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/CreatePendingDbInstancesDispatcherJobTests.cs`

The expected behaviors covered by tests and manual verification include:

* immediate endpoint scheduling
* tenant-aware job identity creation
* single-record execution from `Pending`
* transition to `Error` on failures
* recurring dispatcher pickup of `Pending` rows
* capped retries for `Error` rows
* reconciliation by reusing an existing final-name `OdsInstance`

---

## Context isolation risk and remediation options

### Discovery

This issue was discovered while investigating a CI failure where `CreateInstanceJob` provisioned a `DbInstance` to `Error` status in the multi-tenant PostgreSQL Docker pipeline. The root cause traced through three layers:

1. **Docker compose** — the multi-tenant compose file did not set `ConnectionStrings__EdFi_Master` or `ConnectionStrings__EdFi_Ods`, so the fallback values from `appsettings.json` pointed to `localhost`, which is unreachable from inside the container.
2. **ConfigConnectionStringsProvider** — it was registered as a singleton and read the config section once at startup, so even with correct env vars it could not pick up per-tenant connection strings at runtime.
3. **HashtableContextStorage** — deeper analysis of how the tenant context is stored revealed a shared-state concurrency problem that affects both HTTP and job paths in multi-tenant deployments.

Items 1 and 2 were fixed as part of the CI investigation (Docker compose updated, `ConfigConnectionStringsProvider` made transient and dynamic, `CreateInstanceJob` now sets tenant context explicitly). Item 3 is described below and is pending a team decision.

### How `HashtableContextStorage` works today

`IContextProvider<TenantConfiguration>` is the mechanism both `TenantResolverMiddleware` (HTTP) and `CreateInstanceJob` (Quartz) use to communicate the active tenant to downstream services. Its backing store is `HashtableContextStorage`:

```csharp
// Registered as Singleton — one instance for the lifetime of the application
public class HashtableContextStorage : IContextStorage
{
    public Hashtable UnderlyingHashtable { get; } = [];      // shared by ALL threads

    public void SetValue(string key, object value) => UnderlyingHashtable[key] = value;
    public T? GetValue<T>(string key) => (T?) UnderlyingHashtable[key];
}
```

The key is `typeof(TenantConfiguration).FullName` — a single string constant. Every call to `Set()` from any thread overwrites the same slot for every other thread.

### Race condition: two concurrent HTTP requests

```mermaid
sequenceDiagram
    participant T1 as Thread A (tenant1 request)
    participant T2 as Thread B (tenant2 request)
    participant Store as HashtableContextStorage (singleton)
    participant CSP as ConfigConnectionStringsProvider

    T1->>Store: Set("TenantConfiguration", tenant1Config)
    T2->>Store: Set("TenantConfiguration", tenant2Config)
    Note over Store: tenant1Config is gone
    T1->>CSP: GetConnectionString("EdFi_Master")
    CSP->>Store: Get("TenantConfiguration") → tenant2Config
    Note over T1: Thread A reads tenant2's EdFi_Master ❌
```

### Race condition: Quartz job overlapping with HTTP request

```mermaid
sequenceDiagram
    participant HTTP as HTTP Thread (tenant1)
    participant JOB as Quartz Thread (tenant2 job)
    participant Store as HashtableContextStorage (singleton)
    participant Prov as SandboxProvisionerBase

    HTTP->>Store: Middleware.Set(tenant1Config)
    JOB->>Store: Job.Set(tenant2Config)
    Note over Store: tenant1Config overwritten
    HTTP->>Store: Get("TenantConfiguration") → tenant2Config
    Note over HTTP: HTTP request for tenant1 uses tenant2's connection strings ❌
    JOB->>Prov: AddSandboxAsync → reads EdFi_Master for tenant2 ✓
    JOB->>Store: Job.Set(null) [finally]
    Note over HTTP: Slot is now null mid-request ❌
```

### Why this matters

* **Silent data corruption**: no exception is thrown. The provisioner connects to the wrong host and creates a database on the wrong tenant's server. The `OdsInstance.ConnectionString` encrypted at the end of the job points to the wrong database.
* **Non-deterministic**: the race depends on timing. It does not reproduce on every run, which makes debugging difficult.
* **Invisible in tests**: unit tests mock `IContextProvider<TenantConfiguration>` and never exercise the singleton `HashtableContextStorage`.

### Remediation options

#### Option A — Replace `HashtableContextStorage` with `AsyncLocal` storage (recommended)

`AsyncLocal<T>` is isolated per async execution context. Each HTTP request and each Quartz task has its own logical call chain, so reads and writes are invisible to other chains.

```csharp
public class AsyncLocalContextStorage : IContextStorage
{
    private static readonly AsyncLocal<Dictionary<string, object?>> _storage = new();

    private static Dictionary<string, object?> Current =>
        _storage.Value ??= new Dictionary<string, object?>();

    public void SetValue(string key, object value) => Current[key] = value;
    public T? GetValue<T>(string key) => Current.TryGetValue(key, out var v) ? (T?) v : default;
}
```

Registration change: `AddSingleton<IContextStorage, AsyncLocalContextStorage>` (can stay singleton because `AsyncLocal` manages isolation itself).

**Implementation plan**: [PLAN-A-ASYNC-LOCAL-CONTEXT-STORAGE.md](PLAN-A-ASYNC-LOCAL-CONTEXT-STORAGE.md)

#### Option B — `IHttpContextAccessor` for HTTP, shared hashtable for jobs

Use `IHttpContextAccessor.HttpContext.Items` as storage for HTTP requests (per-request isolation guaranteed by ASP.NET Core) and keep `HashtableContextStorage` only for Quartz jobs. Both `CreateInstanceJob` and `CreatePendingDbInstancesDispatcherJob` already carry `[DisallowConcurrentExecution]`, which prevents a second fire of the same job key from overlapping with the first. This partially limits the race window for the job path without any additional code change.

**Implementation plan**: [PLAN-B-HTTPACCESSOR-SPLIT-STORAGE.md](PLAN-B-HTTPACCESSOR-SPLIT-STORAGE.md)

#### Option C — Remove ambient context from provisioning; pass connection strings explicitly

Remove `IContextProvider<TenantConfiguration>` from `ConfigConnectionStringsProvider` and `SandboxProvisionerBase`. Pass the master connection string as a parameter through the `ISandboxProvisioner` interface. The job already has `tenantConfiguration.MasterConnectionString` in hand, so no ambient context is needed.

**Implementation plan**: [PLAN-C-EXPLICIT-CONNECTION-STRING-PARAM.md](PLAN-C-EXPLICIT-CONNECTION-STRING-PARAM.md)

#### Option D — Accept the risk (no change)

Document the known race condition and defer remediation. Acceptable only for low-traffic deployments where concurrent multi-tenant provisioning is practically impossible.

**Implementation plan**: [PLAN-D-ACCEPT-RISK.md](PLAN-D-ACCEPT-RISK.md)

### Comparison

| Criterion | A — AsyncLocal | B — HttpContext split | C — Explicit param | D — Accept risk |
| --- | --- | --- | --- | --- |
| Fixes HTTP request races | ✅ Yes | ✅ Yes | ⚠️ Partial (only for provisioning path) | ❌ No |
| Fixes job races | ✅ Yes | ⚠️ Partial (`[DisallowConcurrentExecution]` already present; still races across different tenant job keys) | ✅ Yes (no shared state) | ❌ No |
| Code change size | Small (1 class, 1 registration) | Medium (2 storage paths, conditional logic) | Large (interface change, 4+ call sites) | None |
| Risk of regression | Low | Medium | Medium-High | N/A |
| Test change required | No (mocks bypass storage) | No | Yes (interface signature changes) | No |
| Removes ambient context pattern | ❌ No | ❌ No | ✅ Yes (maximally explicit) | ❌ No |
| Standard .NET pattern | ✅ Yes ([`AsyncLocal<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1) is the idiomatic ambient-context pattern; used internally by ASP.NET Core's own [`IHttpContextAccessor`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-context)) | ⚠️ Mixed ([`IHttpContextAccessor`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-context) is standard for the HTTP path; the fallback hashtable for jobs is not) | ✅ Yes ([explicit dependencies via DI](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#recommendations) is the recommended guideline over ambient context) | — |

### Recommendation

**Option A** is the recommended remediation. It is the smallest change, uses the standard .NET mechanism for ambient async context (`AsyncLocal`), and fixes all identified races simultaneously — both HTTP and job paths — without changing any interfaces or DI registrations beyond swapping one class. The existing test suite does not need changes because tests mock `IContextProvider<TenantConfiguration>` directly.
