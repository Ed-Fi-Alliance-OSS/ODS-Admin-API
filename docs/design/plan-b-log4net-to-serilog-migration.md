# Admin API Log4Net to Serilog Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Log4Net with Serilog in Admin API runtime logging while preserving existing behavior and log intent.

**Architecture:** Move logger provider registration from `AddLog4Net` to Serilog bootstrap + host integration, then migrate code-level `ILog` usage to DI-based `ILogger<T>` (or Serilog-backed `ILogger`) in focused increments. Keep business logic unchanged; only logging plumbing and calls change.

**Tech Stack:** .NET 10, Microsoft.Extensions.Logging, Serilog, Serilog.Extensions.Hosting, Serilog.Settings.Configuration, Serilog.Sinks.Console/File, NUnit/Shouldly.

---

## File Structure

- Modify: `Application\Directory.Packages.props`  
  add Serilog package versions; remove Log4Net provider versions.
- Modify: `Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V3\EdFi.Ods.AdminApi.V3.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.Common\EdFi.Ods.AdminApi.Common.csproj`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\WebApplicationBuilderExtensions.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Program.cs`
- Modify: `Application\EdFi.Ods.AdminApi\appsettings.json`
- Modify: `Application\EdFi.Ods.AdminApi.V3\appsettings.json`
- Delete: `Application\EdFi.Ods.AdminApi\log4net\log4net.config`
- Delete: `Application\EdFi.Ods.AdminApi.V3\log4net\log4net.config`
- Modify: `Application\EdFi.Ods.AdminApi\Features\RequestLoggingMiddleware.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Tenants\TenantService.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Services\Tenants\TenantService.cs`
- Modify: `Application\EdFi.Ods.AdminApi.Common\Infrastructure\Helpers\ConnectionStringHelper.cs`
- Modify: `Application\EdFi.Ods.AdminApi.Common\Infrastructure\Providers\Aes256SymmetricStringEncryptionProvider.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V1\Admin.DataAccess\Repositories\ClientAppRepo.cs`
- Test: `Application\EdFi.Ods.AdminApi.UnitTests\Infrastructure\WebApplicationBuilderExtensionsTests.cs`

---

### Task 1: Add Serilog packages and remove Log4Net package references

**Files:**
- Modify: `Application\Directory.Packages.props`
- Modify: `Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.V3\EdFi.Ods.AdminApi.V3.csproj`
- Modify: `Application\EdFi.Ods.AdminApi.Common\EdFi.Ods.AdminApi.Common.csproj`

- [ ] **Step 1: Write failing compile expectation (package references not resolved yet)**

```xml
<PackageVersion Include="Serilog.AspNetCore" Version="8.0.1" />
<PackageVersion Include="Serilog.Settings.Configuration" Version="8.0.0" />
<PackageVersion Include="Serilog.Sinks.Console" Version="5.0.1" />
```

- [ ] **Step 2: Run restore/build to observe missing references before edits**

Run:  
`dotnet build Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj -v minimal`  
Expected: FAIL after code changes begin, until package updates are complete.

- [ ] **Step 3: Apply package changes**

```xml
<!-- remove -->
<PackageVersion Include="Microsoft.Extensions.Logging.Log4Net.AspNetCore" Version="10.0.0" />
<PackageVersion Include="log4net" Version="3.3.0" />

<!-- add -->
<PackageVersion Include="Serilog.AspNetCore" Version="8.0.1" />
<PackageVersion Include="Serilog.Settings.Configuration" Version="8.0.0" />
<PackageVersion Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageVersion Include="Serilog.Sinks.File" Version="5.0.0" />
```

- [ ] **Step 4: Re-run targeted build**

Run:  
`dotnet build Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj -v minimal`  
Expected: PASS or only code-level logging errors remain for next tasks.

- [ ] **Step 5: Commit**

```bash
git add Application/Directory.Packages.props Application/EdFi.Ods.AdminApi/EdFi.Ods.AdminApi.csproj Application/EdFi.Ods.AdminApi.V3/EdFi.Ods.AdminApi.V3.csproj Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj
git commit -m "chore(logging): replace log4net package references with serilog"
```

### Task 2: Replace host logging bootstrap with Serilog

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\WebApplicationBuilderExtensions.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Program.cs`
- Modify: `Application\EdFi.Ods.AdminApi\appsettings.json`
- Modify: `Application\EdFi.Ods.AdminApi.V3\appsettings.json`
- Delete: `Application\EdFi.Ods.AdminApi\log4net\log4net.config`
- Delete: `Application\EdFi.Ods.AdminApi.V3\log4net\log4net.config`

- [ ] **Step 1: Write failing test for logging service registration**

```csharp
[Test]
public void AddLoggingServices_ShouldUseConfiguredProvidersWithoutLog4Net()
{
    var builder = WebApplication.CreateBuilder();
    builder.AddLoggingServices();
    // assert no throw + options from Serilog section are consumable
}
```

- [ ] **Step 2: Run failing test**

Run:  
`dotnet test Application\EdFi.Ods.AdminApi.UnitTests\EdFi.Ods.AdminApi.UnitTests.csproj --filter "FullyQualifiedName~WebApplicationBuilderExtensionsTests" -v minimal`  
Expected: FAIL while method still tied to Log4Net.

- [ ] **Step 3: Implement Serilog bootstrap + configuration**

```csharp
webApplicationBuilder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());
```

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
  "MinimumLevel": "Information",
  "WriteTo": [
    { "Name": "Console" },
    { "Name": "File", "Args": { "path": "logs/admin-api-.log", "rollingInterval": "Day" } }
  ]
}
```

- [ ] **Step 4: Re-run test and startup build**

Run:  
`dotnet test Application\EdFi.Ods.AdminApi.UnitTests\EdFi.Ods.AdminApi.UnitTests.csproj --filter "FullyQualifiedName~WebApplicationBuilderExtensionsTests" -v minimal`  
`dotnet build Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj -v minimal`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs Application/EdFi.Ods.AdminApi/Program.cs Application/EdFi.Ods.AdminApi/appsettings.json Application/EdFi.Ods.AdminApi.V3/appsettings.json Application/EdFi.Ods.AdminApi/log4net/log4net.config Application/EdFi.Ods.AdminApi.V3/log4net/log4net.config
git commit -m "feat(logging): bootstrap serilog and remove log4net config files"
```

### Task 3: Migrate middleware/service logging calls from `ILog` to `ILogger<T>`

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi\Features\RequestLoggingMiddleware.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Infrastructure\Services\Tenants\TenantService.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\Services\Tenants\TenantService.cs`
- Modify: `Application\EdFi.Ods.AdminApi.Common\Infrastructure\Helpers\ConnectionStringHelper.cs`
- Modify: `Application\EdFi.Ods.AdminApi.Common\Infrastructure\Providers\Aes256SymmetricStringEncryptionProvider.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V1\Admin.DataAccess\Repositories\ClientAppRepo.cs`

- [ ] **Step 1: Write failing compile-time update for constructor DI**

```csharp
public class RequestLoggingMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
}
```

- [ ] **Step 2: Build to surface all remaining `ILog` compile errors**

Run:  
`dotnet build Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj -v minimal`  
Expected: FAIL listing remaining log4net symbols.

- [ ] **Step 3: Replace log statements with structured templates**

```csharp
_logger.LogInformation("Request received: {Path} {TraceId}", context.Request.Path.Value, context.TraceIdentifier);
_logger.LogError(ex, "Unhandled error while processing request {TraceId}", context.TraceIdentifier);
```

- [ ] **Step 4: Rebuild and run targeted unit tests**

Run:  
`dotnet build Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj -v minimal`  
`dotnet test Application\EdFi.Ods.AdminApi.UnitTests\EdFi.Ods.AdminApi.UnitTests.csproj -v minimal`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Features/RequestLoggingMiddleware.cs Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Tenants/TenantService.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/ConnectionStringHelper.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Providers/Aes256SymmetricStringEncryptionProvider.cs Application/EdFi.Ods.AdminApi.V1/Admin.DataAccess/Repositories/ClientAppRepo.cs
git commit -m "refactor(logging): migrate runtime classes to ILogger with structured logs"
```

### Task 4: Verify no source-level Log4Net usage remains

**Files:**
- Modify as needed: any `.csproj` or `.cs` still referencing log4net APIs.

- [ ] **Step 1: Run grep check for remaining API usage**

Run:  
`rg "using log4net|ILog\\b|LogManager\\.GetLogger|AddLog4Net|Log4NetCore" Application --glob "*.cs" --glob "*.csproj" --glob "*.json"`  
Expected: zero source matches (excluding lock files until lock refresh).

- [ ] **Step 2: If matches remain, remove them and rebuild**

```csharp
// Before
using log4net;
private static readonly ILog _log = LogManager.GetLogger(typeof(TenantService));

// After
private readonly ILogger<TenantService> _log;
```

- [ ] **Step 3: Re-run grep and build**

Run:  
`rg "using log4net|ILog\\b|LogManager\\.GetLogger|AddLog4Net|Log4NetCore" Application --glob "*.cs" --glob "*.csproj" --glob "*.json"`  
`dotnet build Application\EdFi.Ods.AdminApi\EdFi.Ods.AdminApi.csproj -v minimal`  
Expected: grep clean in source + build PASS.

- [ ] **Step 4: Commit**

```bash
git add Application
git commit -m "chore(logging): remove remaining source log4net references"
```

### Task 5: End-to-end verification and regression safety

**Files:**
- No new files expected unless fixes are needed.

- [ ] **Step 1: Run unit suites**

Run:  
`.\build.ps1 -Command UnitTest`  
Expected: PASS.

- [ ] **Step 2: Run v3 Bruno smoke with new logger**

Run:  
`.\eng\run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode singletenant -BrunoFilter "v3/Validate Exception Content Type" -TearDown`  
Expected: PASS.

- [ ] **Step 3: Manual startup log smoke**

Run:  
`.\build.ps1 -Command Run -LaunchProfile "EdFi.Ods.AdminApi (Dev)"`  
Expected: startup logs written through Serilog sinks (console + configured file), no log4net configuration warnings.

- [ ] **Step 4: Commit verification artifacts only if repository requires docs updates**

```bash
git add docs/design/*.md
git commit -m "docs(logging): capture serilog migration notes and verification"
```

