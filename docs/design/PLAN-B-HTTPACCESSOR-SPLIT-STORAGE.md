# Plan B — `IHttpContextAccessor` for HTTP requests + rely on existing `[DisallowConcurrentExecution]` for jobs

## Context

See [DBINSTANCE-PROVISIONING-JOBS.md § Context isolation risk and remediation options](DBINSTANCE-PROVISIONING-JOBS.md#context-isolation-risk-and-remediation-options) for the full problem statement and discovery history.

## Summary

Split context storage into two paths:

1. **HTTP requests** — store tenant context in `IHttpContextAccessor.HttpContext.Items`, which is per-request and already isolated by ASP.NET Core.
2. **Quartz jobs** — keep `HashtableContextStorage` (or equivalent shared store) and rely on the `[DisallowConcurrentExecution]` attribute that is already present on both `CreateInstanceJob` and `CreatePendingDbInstancesDispatcherJob`. This prevents a second fire of the **same job key** from overlapping with the first.

This avoids replacing the storage mechanism entirely but requires conditional dispatch logic and still leaves a partial residual risk for the job path.

## Tradeoffs vs. Plan A

| | Plan A (AsyncLocal) | Plan B (split storage) |
| --- | --- | --- |
| HTTP request isolation | ✅ Full | ✅ Full |
| Job isolation | ✅ Full | ⚠️ Sequential only (jobs cannot run in parallel) |
| Code change size | Small | Medium |
| Residual risk | None | `DisallowConcurrentExecution` is per-job-type; two different provisioning jobs can still race |

## Files to change

| File | Change |
| --- | --- |
| `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Context/ContextStorage.cs` | Add `HttpContextItemsStorage` class |
| `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Context/ContextProvider.cs` | Change `ContextProvider<T>` to use `IHttpContextAccessor` when available, else fall back to `IContextStorage` |
| `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs` | Register `IHttpContextAccessor`; keep `HashtableContextStorage` for job path |
| `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreateInstanceJob.cs` | No change — `[DisallowConcurrentExecution]` already present |
| `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreatePendingDbInstancesDispatcherJob.cs` | No change — `[DisallowConcurrentExecution]` already present |

## Detailed steps

### Step 1 — Add `HttpContextItemsStorage`

```csharp
/// <summary>
/// Stores context values in the current HTTP request's Items dictionary.
/// Isolation is guaranteed by ASP.NET Core — each request has its own Items.
/// Only valid when an active HttpContext exists; returns null otherwise.
/// </summary>
public class HttpContextItemsStorage : IContextStorage
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextItemsStorage(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public void SetValue(string key, object value)
    {
        if (_httpContextAccessor.HttpContext is { } ctx)
            ctx.Items[key] = value;
    }

    public T? GetValue<T>(string key)
    {
        if (_httpContextAccessor.HttpContext is { } ctx && ctx.Items.TryGetValue(key, out var value))
            return (T?) value;
        return default;
    }
}
```

### Step 2 — Change `ContextProvider<T>` to use dual storage

```csharp
public class ContextProvider<T>(
    IHttpContextAccessor? httpContextAccessor,
    IContextStorage fallbackStorage) : IContextProvider<T>
{
    private static readonly string _contextKey = typeof(T).FullName!;

    public T? Get()
    {
        if (httpContextAccessor?.HttpContext is not null)
            return httpContextAccessor.HttpContext.Items.TryGetValue(_contextKey, out var v) ? (T?) v : default;
        return fallbackStorage.GetValue<T>(_contextKey);
    }

    public void Set(T? context)
    {
        if (httpContextAccessor?.HttpContext is not null)
            httpContextAccessor.HttpContext.Items[_contextKey] = context;
        else
            fallbackStorage.SetValue(_contextKey, context!);
    }
}
```

### Step 3 — Update DI registrations

```csharp
webApplicationBuilder.Services.AddHttpContextAccessor();
webApplicationBuilder.Services.AddSingleton<IContextStorage, HashtableContextStorage>(); // kept for job path
webApplicationBuilder.Services.AddTransient(typeof(IContextProvider<>), typeof(ContextProvider<>));
```

### Step 4 — No job attribute change needed

Both `CreateInstanceJob` and `CreatePendingDbInstancesDispatcherJob` already carry `[DisallowConcurrentExecution]`. This attribute causes Quartz to queue a second fire of the **same job key** rather than run it in parallel with the first. It is not necessary to add it.

Note: this does **not** prevent a `CreateInstanceJob` for tenant1 and a `CreateInstanceJob` for tenant2 from running in parallel, because they have different job keys. Fully eliminating the job-path race requires Plan A or Plan C.

### Step 5 — Add unit tests

Test that `Get()` delegates to `HttpContext.Items` when an HTTP context is active, and to `IContextStorage` when it is not.

## Acceptance criteria

- [ ] All unit tests pass.
- [ ] HTTP request path: context from tenant1 request is not visible to concurrent tenant2 request.
- [ ] Job path: `[DisallowConcurrentExecution]` prevents parallel execution of the same job key.
- [ ] No `NullReferenceException` when `ContextProvider<T>` is used outside an HTTP context (Quartz path).

## Residual risk

Two provisioning jobs for **different tenants** (different job keys) can still run concurrently. `[DisallowConcurrentExecution]` is scoped to a single job key, not to the whole job type. If tenant1 and tenant2 jobs fire at the same time, the shared `HashtableContextStorage` slot is still subject to a race. This is the primary reason Plan A is preferred.
