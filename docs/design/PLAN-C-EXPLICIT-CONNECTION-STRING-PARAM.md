# Plan C — Remove ambient context from provisioning; pass connection strings explicitly

## Context

See [DBINSTANCE-PROVISIONING-JOBS.md § Context isolation risk and remediation options](DBINSTANCE-PROVISIONING-JOBS.md#context-isolation-risk-and-remediation-options) for the full problem statement and discovery history.

## Summary

Eliminate the use of `IContextProvider<TenantConfiguration>` from the provisioning path entirely. Instead of routing connection strings through ambient context, pass the `masterConnectionString` directly as a parameter through `ISandboxProvisioner`. The job already has the tenant's `MasterConnectionString` in hand from `tenantConfiguration`; it does not need ambient context to forward it.

This is the most architecturally explicit approach — no shared state at all in the provisioning path — but it requires changing an interface and all its callers.

## Tradeoffs vs. Plan A

| | Plan A (AsyncLocal) | Plan C (explicit param) |
| --- | --- | --- |
| Shared state in provisioning path | None (isolated per chain) | None (no ambient context) |
| Interface change required | No | Yes (`ISandboxProvisioner`) |
| Call-site changes | None | All `AddSandboxAsync` / `DeleteSandboxesAsync` / `CopySandboxAsync` callers |
| HTTP race fixed | ✅ Yes | ⚠️ Only for provisioning path |
| Removes ambient context dependency | No (context still exists for other uses) | Yes (for provisioning) |
| Test changes required | None | Yes (interface signature changes) |

## Files to change

| File | Change |
| --- | --- |
| `Application/EdFi.Ods.AdminApi.InstanceManagement/Provisioners/ISandboxProvisioner.cs` | Add `masterConnectionString` parameter to `AddSandboxAsync`, `DeleteSandboxesAsync`, `CopySandboxAsync`, `RenameSandboxAsync`, `GetSandboxStatusAsync` |
| `Application/EdFi.Ods.AdminApi.InstanceManagement/Provisioners/SandboxProvisionerBase.cs` | Accept `masterConnectionString` parameter; remove `ConnectionString` property and `_connectionStringsProvider` field |
| `Application/EdFi.Ods.AdminApi.InstanceManagement/Provisioners/PostgresSandboxProvisioner.cs` | Thread `masterConnectionString` through all abstract method implementations |
| `Application/EdFi.Ods.AdminApi.InstanceManagement/Provisioners/SqlServerSandboxProvisioner.cs` | Same as Postgres |
| `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/CreateInstanceJob.cs` | Pass `tenantConfiguration.MasterConnectionString` (multi-tenant) or `_config.GetConnectionString("EdFi_Master")` (single-tenant) to `AddSandboxAsync` |
| `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/ConfigConnectionStringsProvider.cs` | Remove `IContextProvider<TenantConfiguration>` dependency; revert to reading only the top-level `ConnectionStrings` section |
| `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs` | Revert `ConfigConnectionStringsProvider` registration to `AddSingleton` |

## Files to update (tests)

| File | Change |
| --- | --- |
| `Application/EdFi.Ods.AdminApi.InstanceManagement.UnitTests/Provisioners/SandboxProvisionerBaseTests.cs` | Add `masterConnectionString` argument to all provisioner method calls |
| `Application/EdFi.Ods.AdminApi.InstanceManagement.UnitTests/Provisioners/PostgresSandboxProvisionerTests.cs` | Same |
| `Application/EdFi.Ods.AdminApi.InstanceManagement.UnitTests/Provisioners/SqlServerSandboxProvisionerTests.cs` | Same |
| `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Jobs/CreateInstanceJobTests.cs` | Pass connection string in mock provisioner call assertions |
| `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Helpers/ConfigConnectionStringsProviderTests.cs` | Remove tenant-override tests (no longer applicable) |

## Detailed steps

### Step 1 — Update `ISandboxProvisioner`

```csharp
public interface ISandboxProvisioner
{
    Task AddSandboxAsync(string database, SandboxType sandboxType, string masterConnectionString);
    Task DeleteSandboxesAsync(string[] deletedClientKeys, string masterConnectionString);
    Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName, string masterConnectionString);
    Task RenameSandboxAsync(string oldName, string newName, string masterConnectionString);
    Task<SandboxStatus> GetSandboxStatusAsync(string clientKey, string masterConnectionString);
    // Synchronous wrappers follow the same pattern
}
```

### Step 2 — Update `SandboxProvisionerBase`

Remove `_connectionStringsProvider` field and `ConnectionString` property. Accept `masterConnectionString` as a method parameter and forward it to abstract methods:

```csharp
public async Task AddSandboxAsync(string database, SandboxType sandboxType, string masterConnectionString)
{
    await DeleteSandboxesAsync(new[] { database }, masterConnectionString);
    switch (sandboxType)
    {
        case SandboxType.Minimal:
            await CopySandboxAsync(_databaseNameBuilder.MinimalDatabase, database, masterConnectionString);
            break;
        case SandboxType.Sample:
            await CopySandboxAsync(_databaseNameBuilder.SampleDatabase, database, masterConnectionString);
            break;
    }
}

protected abstract Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName, string masterConnectionString);
protected abstract Task DeleteSandboxesAsync(string[] deletedClientKeys, string masterConnectionString);
```

### Step 3 — Update `CreateInstanceJob`

```csharp
var masterConnectionString = multiTenancyEnabled
    ? tenantConfiguration!.MasterConnectionString
        ?? throw new InvalidOperationException($"EdFi_Master is not configured for tenant '{tenantName}'.")
    : _configuration.GetConnectionString("EdFi_Master")
        ?? throw new InvalidOperationException("EdFi_Master connection string is not configured.");

await _sandboxProvisioner.AddSandboxAsync(
    dbInstance.DatabaseName,
    GetSandboxType(dbInstance.DatabaseTemplate),
    masterConnectionString);
```

### Step 4 — Simplify `ConfigConnectionStringsProvider`

Remove `IContextProvider<TenantConfiguration>` constructor parameter and `_options` parameter. Revert to reading only the top-level `ConnectionStrings` section:

```csharp
public class ConfigConnectionStringsProvider : IConfigConnectionStringsProvider
{
    private readonly IConfiguration _config;

    public ConfigConnectionStringsProvider(IConfiguration config) => _config = config;

    public IDictionary<string, string> ConnectionStringProviderByName =>
        _config.GetSection("ConnectionStrings")
            .GetChildren()
            .ToDictionary(k => k.Key, v => v.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    public string GetConnectionString(string name) => ConnectionStringProviderByName[name];
    public int Count => ConnectionStringProviderByName.Count;
}
```

Change DI registration back to `AddSingleton`.

### Step 5 — Update all tests

Update provisioner and job tests to pass `masterConnectionString` where required. Remove tenant-override tests from `ConfigConnectionStringsProviderTests`.

## Acceptance criteria

- [ ] `ISandboxProvisioner` updated with new signatures.
- [ ] `SandboxProvisionerBase` does not read from any ambient context.
- [ ] `ConfigConnectionStringsProvider` has no dependency on `IContextProvider<TenantConfiguration>`.
- [ ] `CreateInstanceJob` resolves and passes `masterConnectionString` explicitly.
- [ ] All unit tests pass.
- [ ] No `IContextProvider<TenantConfiguration>` calls exist in the provisioning code path.

## Residual risk

This plan eliminates shared state only for the **provisioning** path. The `HashtableContextStorage` race for other consumers of `IContextProvider<TenantConfiguration>` (such as EF Core context resolution in HTTP requests) is not addressed. Plan A should still be considered for complete coverage.
