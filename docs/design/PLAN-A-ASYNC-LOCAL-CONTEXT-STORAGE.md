# Plan A — Replace `HashtableContextStorage` with `AsyncLocal` storage

## Context

See [DBINSTANCE-PROVISIONING-JOBS.md § Context isolation risk and remediation options](DBINSTANCE-PROVISIONING-JOBS.md#context-isolation-risk-and-remediation-options) for the full problem statement and discovery history.

## Summary

Replace the singleton `HashtableContextStorage` (plain `Hashtable`, one slot shared by all threads) with a new `AsyncLocalContextStorage` implementation backed by `AsyncLocal<T>`. `AsyncLocal` isolates values per async execution chain, which means each HTTP request and each Quartz job task gets its own context slot with no interference from other concurrent operations.

This is the smallest possible fix: one new class, one registration change, no interface or call-site changes.

## Files to change

| File | Change |
| --- | --- |
| `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Context/ContextStorage.cs` | Add `AsyncLocalContextStorage` class |
| `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs` | Change `AddSingleton<IContextStorage, HashtableContextStorage>` to use `AsyncLocalContextStorage` |

## Files to create

| File | Purpose |
| --- | --- |
| `Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Context/AsyncLocalContextStorageTests.cs` | Unit tests for isolation behavior |

## Detailed steps

### Step 1 — Add `AsyncLocalContextStorage` to `ContextStorage.cs`

In `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Context/ContextStorage.cs`, add the following class alongside the existing `HashtableContextStorage`. Do not remove `HashtableContextStorage` yet (it may be referenced in tests or used elsewhere).

```csharp
/// <summary>
/// Thread-safe, async-context-isolated implementation of IContextStorage.
/// Each async execution chain (HTTP request, Quartz job task) has its own isolated
/// dictionary — reads and writes in one chain are invisible to all other chains.
/// This replaces HashtableContextStorage, which stored values in a shared Hashtable
/// and was therefore subject to race conditions under concurrent multi-tenant load.
/// </summary>
public class AsyncLocalContextStorage : IContextStorage
{
    // Static AsyncLocal so the same instance is used across DI resolution chains.
    // AsyncLocal.Value is per-async-context, not per-instance.
    private static readonly AsyncLocal<Dictionary<string, object?>> _storage = new();

    private static Dictionary<string, object?> Current =>
        _storage.Value ??= new Dictionary<string, object?>();

    public void SetValue(string key, object value) => Current[key] = value;

    public T? GetValue<T>(string key) =>
        Current.TryGetValue(key, out var value) ? (T?) value : default;
}
```

### Step 2 — Change the DI registration

In `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs`, inside `EnableMultiTenancySupport`, replace:

```csharp
webApplicationBuilder.Services.AddSingleton<IContextStorage, HashtableContextStorage>();
```

with:

```csharp
webApplicationBuilder.Services.AddSingleton<IContextStorage, AsyncLocalContextStorage>();
```

### Step 3 — Add unit tests

Create `Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Context/AsyncLocalContextStorageTests.cs` with the following tests:

```csharp
[TestFixture]
public class AsyncLocalContextStorageTests
{
    [Test]
    public void SetValue_ThenGetValue_ReturnsSameValue()
    {
        var storage = new AsyncLocalContextStorage();
        storage.SetValue("key", "value");
        storage.GetValue<string>("key").ShouldBe("value");
    }

    [Test]
    public async Task SetValue_InOneTask_DoesNotAffectAnotherTask()
    {
        var storage = new AsyncLocalContextStorage();
        string? capturedFromTask2 = "initial";

        var task1 = Task.Run(() =>
        {
            storage.SetValue("key", "tenant1");
        });

        var task2 = Task.Run(async () =>
        {
            await Task.Delay(50); // let task1 set its value first
            capturedFromTask2 = storage.GetValue<string>("key");
        });

        await Task.WhenAll(task1, task2);

        // task2 must not see task1's value — each async chain is isolated
        capturedFromTask2.ShouldBeNull();
    }

    [Test]
    public async Task SetValue_InParentContext_IsVisibleInChildTask()
    {
        // AsyncLocal propagates (read-only) to child tasks spawned after Set.
        // This is expected AsyncLocal behavior and harmless here because
        // callers always Set their own value before reading.
        var storage = new AsyncLocalContextStorage();
        storage.SetValue("key", "parent-value");

        string? capturedInChild = null;
        await Task.Run(() =>
        {
            capturedInChild = storage.GetValue<string>("key");
        });

        capturedInChild.ShouldBe("parent-value");
    }

    [Test]
    public void GetValue_WhenKeyNotSet_ReturnsDefault()
    {
        var storage = new AsyncLocalContextStorage();
        storage.GetValue<string>("missing").ShouldBeNull();
    }

    [Test]
    public void SetValue_WithNull_OverwritesPreviousValue()
    {
        var storage = new AsyncLocalContextStorage();
        storage.SetValue("key", "value");
        storage.SetValue("key", null!);
        storage.GetValue<string>("key").ShouldBeNull();
    }
}
```

## Acceptance criteria

- [ ] `AsyncLocalContextStorage` compiles with no warnings.
- [ ] All new unit tests pass.
- [ ] All existing unit tests in `EdFi.Ods.AdminApi.UnitTests` and `EdFi.Ods.AdminApi.Common.UnitTests` continue to pass (no test touches `HashtableContextStorage` directly through the DI registration).
- [ ] Manual or integration test: two simultaneous HTTP requests for different tenants return data from their respective tenants' databases.

## What does NOT need to change

- `IContextStorage` interface — unchanged.
- `IContextProvider<T>` / `ContextProvider<T>` — unchanged.
- `TenantResolverMiddleware` — unchanged.
- `CreateInstanceJob` context set/clear logic — unchanged.
- `ConfigConnectionStringsProvider` — unchanged.
- All existing unit tests — they mock `IContextProvider<TenantConfiguration>` directly and never exercise `HashtableContextStorage`.

## Risk

Low. `AsyncLocal` is the standard .NET mechanism for ambient async context, used internally by ASP.NET Core (`IHttpContextAccessor`) and Entity Framework. The only behavioral difference from `HashtableContextStorage` is that child `Task.Run` tasks inherit the parent's value as a read-only snapshot — setting a value in a child does not propagate back to the parent. This is not a concern here because every caller sets its own value before reading.
