# Audit Trail Logging Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a database-backed audit trail to Admin API (V2 and V3) that records authentication attempts and mutating administrative actions to a new `AuditLogs` table, toggleable via a single configuration flag, without ever blocking or failing the original request.

**Architecture:** A shared (Common-project) capture pipeline — a bounded `Channel<AuditEvent>`, an `IAuditEventRecorder` used at the two capture points, and a hosted `AuditLogBackgroundService` that drains the channel and writes via EF Core with retry + log4net fallback. Each of V2 and V3 supplies its own `IAuditLogWriter` (because each has its own `AdminApiDbContext` type), plus its own DbUp SQL script and EF entity mapping for the new table.

**Tech Stack:** .NET 8/10, ASP.NET Core middleware & `JwtBearerEvents`/OpenIddict event handlers, EF Core (SQL Server + Npgsql providers), `System.Threading.Channels`, `Microsoft.Extensions.Hosting.BackgroundService`, log4net (fallback only), DbUp SQL migration scripts, NUnit + Shouldly + FakeItEasy.

## Global Constraints

- Both SQL Server and PostgreSQL must be supported (existing `AppSettings:DatabaseEngine` config value, `DatabaseEngineEnum.Parse`).
- Must work in both single-tenant and multitenant deployment modes; audit data goes into each tenant's own `EdFi_Admin` database — no cross-tenant table, no tenant-id column.
- Audit writes must never block or fail the original request (fail-open); on write failure, fall back to the existing log4net logger.
- Audit data must never be exposed through any HTTP endpoint — no controller, endpoint, or route is added for the `AuditLogs` table.
- New dependencies are allowed only if well-maintained/well-supported; this plan introduces none beyond what's already in the codebase (`System.Threading.Channels` and `Microsoft.Extensions.Hosting` are already transitively available via ASP.NET Core).
- Only one of V1/V2/V3 runs per deployed instance (`AppSettings:AdminApiMode`), so a single `AuditLogging:Enabled` flag (no per-version split) is sufficient.
- Follow `.editorconfig`: file-scoped namespaces, single-line `using` directives, newline before `{`, `nameof` for member names, non-nullable variables where possible.
- Tests use NUnit + Shouldly for assertions and FakeItEasy for mocks, mirroring existing test naming/style.

---

## Design Reference

See `docs/design/2026-07-28-audit-trail-logging/design.md` for the full design rationale (log4net-vs-EF-Core decision, schema, failure handling). This plan implements that design.

### Data model (recap)

Table `adminapi.AuditLogs`, schema `adminapi` (matches existing tables):

| Column | Type | Nullable |
|---|---|---|
| `Id` | bigint identity | No (PK) |
| `EventType` | nvarchar(30) | No |
| `Timestamp` | datetime2 (UTC) | No |
| `ClientId` | nvarchar(100) | Yes |
| `SourceIpAddress` | nvarchar(45) | Yes |
| `HttpVerb` | nvarchar(10) | Yes |
| `HttpUrl` | nvarchar(2048) | Yes |
| `StatusCode` | int | Yes |

`EventType` values (stored as strings, not a DB enum, for portability): `AuthenticationSuccess`, `AuthenticationFailure`, `Action`.

### Capture points (recap)

1. **Token-issuance events** (`/connect/token`): hooked in the existing `DefaultTokenResponseHandler.HandleAsync` (OpenIddict `ApplyTokenResponseContext`) in `SecurityExtensions.cs` — logs `AuthenticationSuccess` when `response.Error` is null, `AuthenticationFailure` otherwise. `ClientId` comes from `context.Request?.ClientId`.
2. **Rejected-credential events anywhere else in the API**: hooked in `JwtBearerEvents.OnChallenge` in `SecurityExtensions.cs` — logs `AuthenticationFailure` (covers invalid, expired, and missing credentials uniformly, since a 401 challenge fires for all three cases). `ClientId` is left null here (not reliably determinable from a rejected token).
3. **Mutating action events**: a new `AuditActionLoggingMiddleware`, registered once in `Program.cs` after `app.UseAuthorization()`, logs one `Action` event per POST/PUT/PATCH/DELETE request, with `ClientId` from `HttpContext.User`, verb, URL, and the eventual response status code.

None of these three call sites talk to the database directly — they all call `IAuditEventRecorder.RecordAsync(...)`, which enqueues onto a bounded channel. A single `AuditLogBackgroundService` drains it.

---

### Task 1: Shared audit data model and configuration

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEventType.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLog.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEvent.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLoggingSettings.cs`

**Interfaces:**
- Produces: `AuditEventType` enum (`AuthenticationSuccess`, `AuthenticationFailure`, `Action`), `AuditLog` entity class, `AuditEvent` DTO class, `AuditLoggingSettings` options class — all used by every later task.

These are plain data classes with no behavior, so there is nothing meaningful to unit test (TDD is not applicable to POCOs with no logic) — write them directly.

- [ ] **Step 1: Create the `AuditEventType` enum**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public enum AuditEventType
{
    AuthenticationSuccess,
    AuthenticationFailure,
    Action
}
```

- [ ] **Step 2: Create the `AuditLog` entity**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditLog
{
    public long Id { get; set; }
    public AuditEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ClientId { get; set; }
    public string? SourceIpAddress { get; set; }
    public string? HttpVerb { get; set; }
    public string? HttpUrl { get; set; }
    public int? StatusCode { get; set; }
}
```

- [ ] **Step 3: Create the `AuditEvent` DTO**

This is what capture points enqueue onto the channel. It carries the resolved admin connection string (captured while tenant context is still ambient) so the background writer never has to re-resolve tenant configuration.

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditEvent
{
    public required string AdminConnectionString { get; init; }
    public required AuditEventType EventType { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? ClientId { get; init; }
    public string? SourceIpAddress { get; init; }
    public string? HttpVerb { get; init; }
    public string? HttpUrl { get; init; }
    public int? StatusCode { get; init; }
}
```

- [ ] **Step 4: Create the `AuditLoggingSettings` options class**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditLoggingSettings
{
    public bool Enabled { get; set; }
}
```

- [ ] **Step 5: Build the Common project**

Run: `dotnet build Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj`
Expected: Build succeeds with no errors.

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEventType.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLog.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEvent.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLoggingSettings.cs
git commit -m "feat: add audit trail data model and configuration types"
```

---

### Task 2: Bounded channel, recorder, and writer abstraction

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLogChannel.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/IAuditEventRecorder.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEventRecorder.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/IAuditLogWriter.cs`
- Test: `Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Audit/AuditEventRecorderTests.cs`

**Interfaces:**
- Consumes: `AuditEvent`, `AuditEventType`, `AuditLoggingSettings` (Task 1); `IContextProvider<TenantConfiguration>` and `TenantConfiguration` (`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Context/ContextProvider.cs`, `Application/EdFi.Ods.AdminApi.Common/Infrastructure/MultiTenancy/TenantConfiguration.cs`); `IConfigurationExtensions.GetConnectionStringByName` (`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Extensions/IConfigurationExtensions.cs`).
- Produces: `AuditLogChannel` (singleton, exposes `ChannelWriter<AuditEvent> Writer` and `ChannelReader<AuditEvent> Reader`), `IAuditEventRecorder.RecordAsync(AuditEventType eventType, string? clientId, string? sourceIpAddress, string? httpVerb, string? httpUrl, int? statusCode)`, `IAuditLogWriter.WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)` — consumed by Task 3 (background service) and Task 4/5 (capture points) and Task 6/7 (per-version writer implementations).

- [ ] **Step 1: Create the bounded channel wrapper**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Channels;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditLogChannel
{
    private readonly Channel<AuditEvent> _channel = Channel.CreateBounded<AuditEvent>(
        new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelWriter<AuditEvent> Writer => _channel.Writer;

    public ChannelReader<AuditEvent> Reader => _channel.Reader;
}
```

- [ ] **Step 2: Create the `IAuditEventRecorder` interface**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public interface IAuditEventRecorder
{
    void Record(
        AuditEventType eventType,
        string? clientId,
        string? sourceIpAddress,
        string? httpVerb,
        string? httpUrl,
        int? statusCode);
}
```

- [ ] **Step 3: Write the failing test for `AuditEventRecorder`**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class AuditEventRecorderTests
{
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EdFi_Admin"] = "fallback-connection-string"
            })
            .Build();

    [Test]
    public void Record_WhenAuditLoggingDisabled_DoesNotEnqueueEvent()
    {
        var channel = new AuditLogChannel();
        var tenantContext = new ContextProvider<TenantConfiguration>(new AsyncLocalContextStorage());
        var recorder = new AuditEventRecorder(
            channel,
            Options.Create(new AuditLoggingSettings { Enabled = false }),
            tenantContext,
            BuildConfiguration());

        recorder.Record(AuditEventType.Action, "client-1", "127.0.0.1", "POST", "/v3/apiClients", 201);

        channel.Reader.TryRead(out _).ShouldBeFalse();
    }

    [Test]
    public void Record_WhenAuditLoggingEnabledAndNoTenantContext_EnqueuesEventWithFallbackConnectionString()
    {
        var channel = new AuditLogChannel();
        var tenantContext = new ContextProvider<TenantConfiguration>(new AsyncLocalContextStorage());
        var recorder = new AuditEventRecorder(
            channel,
            Options.Create(new AuditLoggingSettings { Enabled = true }),
            tenantContext,
            BuildConfiguration());

        recorder.Record(AuditEventType.Action, "client-1", "127.0.0.1", "POST", "/v3/apiClients", 201);

        channel.Reader.TryRead(out var auditEvent).ShouldBeTrue();
        auditEvent!.AdminConnectionString.ShouldBe("fallback-connection-string");
        auditEvent.EventType.ShouldBe(AuditEventType.Action);
        auditEvent.ClientId.ShouldBe("client-1");
        auditEvent.SourceIpAddress.ShouldBe("127.0.0.1");
        auditEvent.HttpVerb.ShouldBe("POST");
        auditEvent.HttpUrl.ShouldBe("/v3/apiClients");
        auditEvent.StatusCode.ShouldBe(201);
    }

    [Test]
    public void Record_WhenTenantContextIsSet_EnqueuesEventWithTenantConnectionString()
    {
        var channel = new AuditLogChannel();
        var tenantContext = new ContextProvider<TenantConfiguration>(new AsyncLocalContextStorage());
        tenantContext.Set(new TenantConfiguration { AdminConnectionString = "tenant-connection-string" });
        var recorder = new AuditEventRecorder(
            channel,
            Options.Create(new AuditLoggingSettings { Enabled = true }),
            tenantContext,
            BuildConfiguration());

        recorder.Record(AuditEventType.AuthenticationFailure, null, "10.0.0.5", null, null, 401);

        channel.Reader.TryRead(out var auditEvent).ShouldBeTrue();
        auditEvent!.AdminConnectionString.ShouldBe("tenant-connection-string");
        auditEvent.ClientId.ShouldBeNull();
    }
}
```

- [ ] **Step 4: Run test to verify it fails**

Run: `dotnet test Application/EdFi.Ods.AdminApi.Common.UnitTests --filter FullyQualifiedName~AuditEventRecorderTests`
Expected: FAIL — `AuditEventRecorder` and `IAuditLogWriter` do not exist yet.

- [ ] **Step 5: Create `IAuditLogWriter`**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
```

- [ ] **Step 6: Implement `AuditEventRecorder`**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditEventRecorder(
    AuditLogChannel channel,
    IOptions<AuditLoggingSettings> settings,
    IContextProvider<TenantConfiguration> tenantContextProvider,
    IConfiguration configuration) : IAuditEventRecorder
{
    public void Record(
        AuditEventType eventType,
        string? clientId,
        string? sourceIpAddress,
        string? httpVerb,
        string? httpUrl,
        int? statusCode)
    {
        if (!settings.Value.Enabled)
        {
            return;
        }

        var tenant = tenantContextProvider.Get();
        var adminConnectionString = !string.IsNullOrEmpty(tenant?.AdminConnectionString)
            ? tenant.AdminConnectionString
            : configuration.GetConnectionStringByName("EdFi_Admin");

        var auditEvent = new AuditEvent
        {
            AdminConnectionString = adminConnectionString,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            ClientId = clientId,
            SourceIpAddress = sourceIpAddress,
            HttpVerb = httpVerb,
            HttpUrl = httpUrl,
            StatusCode = statusCode
        };

        channel.Writer.TryWrite(auditEvent);
    }
}
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test Application/EdFi.Ods.AdminApi.Common.UnitTests --filter FullyQualifiedName~AuditEventRecorderTests`
Expected: PASS (3 tests).

- [ ] **Step 8: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLogChannel.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/IAuditEventRecorder.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditEventRecorder.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/IAuditLogWriter.cs Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Audit/AuditEventRecorderTests.cs
git commit -m "feat: add bounded audit event channel and recorder"
```

---

### Task 3: Background service with retry and log4net fallback

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLogBackgroundService.cs`
- Test: `Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Audit/AuditLogBackgroundServiceTests.cs`

**Interfaces:**
- Consumes: `AuditLogChannel`, `IAuditLogWriter` (Task 2).
- Produces: `AuditLogBackgroundService` (registered via `AddHostedService` in Task 8), with an internal `ProcessEventAsync(AuditEvent, IAuditLogWriter, CancellationToken)` method used directly by tests via `InternalsVisibleTo`.

This is the core of the fail-open guarantee: up to 2 retries (200ms, then 500ms backoff), and on final failure, log via log4net rather than throw.

- [ ] **Step 1: Add `InternalsVisibleTo` for the Common project's test assembly**

Check `Application/EdFi.Ods.AdminApi.Common/AssemblyInfo.cs` or the `.csproj` — if no `InternalsVisibleTo` attribute for `EdFi.Ods.AdminApi.Common.UnitTests` exists yet, add one. Create (if it doesn't already exist) `Application/EdFi.Ods.AdminApi.Common/AssemblyInfo.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EdFi.Ods.AdminApi.Common.UnitTests")]
```

If `Application/EdFi.Ods.AdminApi.Common.UnitTests` does not yet reference `EdFi.Ods.AdminApi.Common`, add a `ProjectReference` to its `.csproj` — check first, since other Common infrastructure is already tested there (e.g. `TenantResolverMiddlewareTests`), so the reference almost certainly already exists.

- [ ] **Step 2: Write the failing tests**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class AuditLogBackgroundServiceTests
{
    private static AuditEvent SampleEvent() => new()
    {
        AdminConnectionString = "conn",
        EventType = AuditEventType.Action,
        Timestamp = DateTime.UtcNow,
        HttpVerb = "DELETE",
        HttpUrl = "/v3/apiClients/1",
        StatusCode = 204
    };

    [Test]
    public async Task ProcessEventAsync_WhenWriterSucceedsFirstTry_WritesOnceAndDoesNotFallBack()
    {
        var writer = A.Fake<IAuditLogWriter>();
        var service = new AuditLogBackgroundService(new AuditLogChannel(), writer);

        var fellBackToLogging = await service.ProcessEventAsync(SampleEvent(), writer, CancellationToken.None);

        fellBackToLogging.ShouldBeFalse();
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ProcessEventAsync_WhenWriterFailsTwiceThenSucceeds_RetriesAndDoesNotFallBack()
    {
        var writer = A.Fake<IAuditLogWriter>();
        var callCount = 0;
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .Invokes(() => callCount++)
            .ReturnsLazily(() =>
            {
                if (callCount < 3)
                {
                    throw new InvalidOperationException("transient failure");
                }
                return Task.CompletedTask;
            });
        var service = new AuditLogBackgroundService(new AuditLogChannel(), writer);

        var fellBackToLogging = await service.ProcessEventAsync(SampleEvent(), writer, CancellationToken.None);

        fellBackToLogging.ShouldBeFalse();
        callCount.ShouldBe(3);
    }

    [Test]
    public async Task ProcessEventAsync_WhenWriterAlwaysFails_FallsBackAfterExhaustingRetries()
    {
        var writer = A.Fake<IAuditLogWriter>();
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("permanent failure"));
        var service = new AuditLogBackgroundService(new AuditLogChannel(), writer);

        var fellBackToLogging = await service.ProcessEventAsync(SampleEvent(), writer, CancellationToken.None);

        fellBackToLogging.ShouldBeTrue();
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .MustHaveHappened(3, Times.Exactly);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Application/EdFi.Ods.AdminApi.Common.UnitTests --filter FullyQualifiedName~AuditLogBackgroundServiceTests`
Expected: FAIL — `AuditLogBackgroundService` does not exist yet.

- [ ] **Step 4: Implement `AuditLogBackgroundService`**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using log4net;
using Microsoft.Extensions.Hosting;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditLogBackgroundService(AuditLogChannel channel, IAuditLogWriter writer) : BackgroundService
{
    private static readonly ILog _logger = LogManager.GetLogger(typeof(AuditLogBackgroundService));
    private static readonly TimeSpan[] _retryDelays = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditEvent in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessEventAsync(auditEvent, writer, stoppingToken);
        }
    }

    internal async Task<bool> ProcessEventAsync(AuditEvent auditEvent, IAuditLogWriter eventWriter, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= _retryDelays.Length; attempt++)
        {
            try
            {
                await eventWriter.WriteAsync(auditEvent, cancellationToken);
                return false;
            }
            catch (Exception ex) when (attempt < _retryDelays.Length)
            {
                _logger.Warn($"Audit log write failed (attempt {attempt + 1}), retrying.", ex);
                await Task.Delay(_retryDelays[attempt], cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"Audit log write failed after {_retryDelays.Length + 1} attempts; falling back to text log. " +
                    $"EventType={auditEvent.EventType}, ClientId={auditEvent.ClientId}, " +
                    $"SourceIpAddress={auditEvent.SourceIpAddress}, HttpVerb={auditEvent.HttpVerb}, " +
                    $"HttpUrl={auditEvent.HttpUrl}, StatusCode={auditEvent.StatusCode}, Timestamp={auditEvent.Timestamp:O}",
                    ex);
                return true;
            }
        }

        return true;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Application/EdFi.Ods.AdminApi.Common.UnitTests --filter FullyQualifiedName~AuditLogBackgroundServiceTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/AssemblyInfo.cs Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditLogBackgroundService.cs Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Audit/AuditLogBackgroundServiceTests.cs
git commit -m "feat: add audit log background writer with retry and fallback logging"
```

---

### Task 4: Action-event capture middleware

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditActionLoggingMiddleware.cs`
- Test: `Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Audit/AuditActionLoggingMiddlewareTests.cs`

**Interfaces:**
- Consumes: `IAuditEventRecorder` (Task 2).
- Produces: `AuditActionLoggingMiddleware`, registered in Task 8's `Program.cs` change.

This middleware is version-agnostic — it only needs `HttpContext` and `IAuditEventRecorder`, so it lives once in Common and is registered once for both V2 and V3.

- [ ] **Step 1: Write the failing tests**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Security.Claims;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class AuditActionLoggingMiddlewareTests
{
    private static DefaultHttpContext BuildContext(string method, string path, string? clientId, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = statusCode;
        if (clientId != null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("client_id", clientId)], "test"));
        }
        return context;
    }

    [Test]
    public async Task InvokeAsync_ForPostRequest_RecordsActionEvent()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var middleware = new AuditActionLoggingMiddleware(_ => Task.CompletedTask, recorder);
        var context = BuildContext("POST", "/v3/apiClients", "client-1", 201);

        await middleware.InvokeAsync(context);

        A.CallTo(() => recorder.Record(
            AuditEventType.Action, "client-1", A<string?>._, "POST", "/v3/apiClients", 201))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task InvokeAsync_ForGetRequest_DoesNotRecordEvent()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var middleware = new AuditActionLoggingMiddleware(_ => Task.CompletedTask, recorder);
        var context = BuildContext("GET", "/v3/apiClients", "client-1", 200);

        await middleware.InvokeAsync(context);

        A.CallTo(() => recorder.Record(
            A<AuditEventType>._, A<string?>._, A<string?>._, A<string?>._, A<string?>._, A<int?>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task InvokeAsync_WhenNoClientIdClaim_RecordsNullClientId()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var middleware = new AuditActionLoggingMiddleware(_ => Task.CompletedTask, recorder);
        var context = BuildContext("DELETE", "/v3/apiClients/1", null, 204);

        await middleware.InvokeAsync(context);

        A.CallTo(() => recorder.Record(
            AuditEventType.Action, null, A<string?>._, "DELETE", "/v3/apiClients/1", 204))
            .MustHaveHappenedOnceExactly();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Application/EdFi.Ods.AdminApi.Common.UnitTests --filter FullyQualifiedName~AuditActionLoggingMiddlewareTests`
Expected: FAIL — `AuditActionLoggingMiddleware` does not exist yet.

- [ ] **Step 3: Implement `AuditActionLoggingMiddleware`**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditActionLoggingMiddleware(RequestDelegate next, IAuditEventRecorder recorder)
{
    private static readonly HashSet<string> _mutatingVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_mutatingVerbs.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        var clientId = context.User.FindFirst("client_id")?.Value;
        var sourceIpAddress = context.Connection.RemoteIpAddress?.ToString();
        var httpVerb = context.Request.Method;
        var httpUrl = context.Request.Path.Value;

        await next(context);

        recorder.Record(AuditEventType.Action, clientId, sourceIpAddress, httpVerb, httpUrl, context.Response.StatusCode);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Application/EdFi.Ods.AdminApi.Common.UnitTests --filter FullyQualifiedName~AuditActionLoggingMiddlewareTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AuditActionLoggingMiddleware.cs Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Audit/AuditActionLoggingMiddlewareTests.cs
git commit -m "feat: add audit action-event capture middleware"
```

---

### Task 5: Authentication-event hooks in SecurityExtensions.cs

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Infrastructure/Security/SecurityExtensions.cs:81-92` (`DefaultTokenResponseHandler`) and `:173-186` (`JwtBearerEvents`)

**Interfaces:**
- Consumes: `IAuditEventRecorder` (Task 2).
- Produces: no new public interface — wires the two remaining capture points into the existing shared auth code (this file is shared across V1/V2/V3, so no separate V3 change is needed here).

This file's `DefaultTokenResponseHandler` currently has a parameterless constructor. Give it a constructor dependency on `IAuditEventRecorder`, and register it so DI can supply it (OpenIddict resolves `UseSingletonHandler<T>` instances from the app's `IServiceProvider`).

- [ ] **Step 1: Add the `IAuditEventRecorder` dependency to `DefaultTokenResponseHandler` and record token-issuance events**

In `Application/EdFi.Ods.AdminApi/Infrastructure/Security/SecurityExtensions.cs`, change:

```csharp
    public class DefaultTokenResponseHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
    {
        private const string DENIED_AUTHENTICATION_MESSAGE =
            "Access Denied. Please review your information and try again.";
        public ValueTask HandleAsync(ApplyTokenResponseContext context)
        {
            var response = context.Response;
```

to:

```csharp
    public class DefaultTokenResponseHandler(IAuditEventRecorder auditEventRecorder) : IOpenIddictServerHandler<ApplyTokenResponseContext>
    {
        private const string DENIED_AUTHENTICATION_MESSAGE =
            "Access Denied. Please review your information and try again.";
        public ValueTask HandleAsync(ApplyTokenResponseContext context)
        {
            var response = context.Response;
            var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
            auditEventRecorder.Record(
                string.IsNullOrEmpty(response.Error) ? AuditEventType.AuthenticationSuccess : AuditEventType.AuthenticationFailure,
                context.Request?.ClientId,
                httpContext?.Connection.RemoteIpAddress?.ToString(),
                httpVerb: null,
                httpUrl: null,
                statusCode: null);
```

Add `using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;` and `using OpenIddict.Server.AspNetCore;` (for the `GetHttpRequest()` extension method) to the file's `using` block.

- [ ] **Step 2: Add the `OnChallenge` hook for rejected-credential events**

In the same file, change:

```csharp
            opt.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    Console.WriteLine("Token validated successfully.");

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                }
            };
```

to:

```csharp
            opt.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    Console.WriteLine("Token validated successfully.");

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var recorder = context.HttpContext.RequestServices.GetRequiredService<IAuditEventRecorder>();
                    recorder.Record(
                        AuditEventType.AuthenticationFailure,
                        clientId: null,
                        context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        httpVerb: null,
                        httpUrl: null,
                        context.Response.StatusCode == 0 ? (int)HttpStatusCode.Unauthorized : context.Response.StatusCode);
                    return Task.CompletedTask;
                }
            };
```

Add `using System.Net;` if not already present in the file.

- [ ] **Step 3: Register `DefaultTokenResponseHandler` in DI**

At the end of `AddSecurityUsingOpenIddict` (before `services.AddTransient<ITokenService, TokenService>();`), add:

```csharp
        services.AddSingleton<DefaultTokenResponseHandler>();
```

- [ ] **Step 4: Build to verify the change compiles**

Run: `dotnet build Application/EdFi.Ods.AdminApi/EdFi.Ods.AdminApi.csproj`
Expected: Build succeeds. (No new automated test here — this wires existing OpenIddict/JwtBearer infrastructure and is exercised end-to-end by Task 9's DB test and manual verification in Task 10.)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Infrastructure/Security/SecurityExtensions.cs
git commit -m "feat: record authentication audit events at token issuance and challenge"
```

---

### Task 6: V2 audit table — DbUp scripts, EF mapping, and writer

**Files:**
- Create: `Application/EdFi.Ods.AdminApi/Artifacts/MsSql/Structure/Admin/00007-CreateAuditLogs.sql`
- Create: `Application/EdFi.Ods.AdminApi/Artifacts/PgSql/Structure/Admin/00007-CreateAuditLogs.sql`
- Modify: `Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs`
- Create: `Application/EdFi.Ods.AdminApi/Infrastructure/Audit/AdminApiAuditLogWriter.cs`
- Test: `Application/EdFi.Ods.AdminApi.DBTests/Infrastructure/Audit/AdminApiAuditLogWriterTests.cs`

**Interfaces:**
- Consumes: `AuditLog`, `AuditEvent`, `IAuditLogWriter` (Common project, Task 1/2); `DatabaseEngineEnum` (`Application/EdFi.Ods.AdminApi.Common/Infrastructure/DatabaseEngineEnum.cs`).
- Produces: `adminapi.AuditLogs` table (V2 admin DB), `AdminApiDbContext.AuditLogs` `DbSet<AuditLog>`, `AdminApiAuditLogWriter : IAuditLogWriter`.

- [ ] **Step 1: Add the SQL Server migration script**

```sql
-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'AuditLogs')
BEGIN
CREATE TABLE [adminapi].[AuditLogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [EventType] NVARCHAR(30) NOT NULL,
    [Timestamp] DATETIME2 NOT NULL,
    [ClientId] NVARCHAR(100) NULL,
    [SourceIpAddress] NVARCHAR(45) NULL,
    [HttpVerb] NVARCHAR(10) NULL,
    [HttpUrl] NVARCHAR(2048) NULL,
    [StatusCode] INT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

CREATE NONCLUSTERED INDEX [IX_AuditLogs_Timestamp]
    ON [adminapi].[AuditLogs] ([Timestamp]);

CREATE NONCLUSTERED INDEX [IX_AuditLogs_ClientId]
    ON [adminapi].[AuditLogs] ([ClientId]);
END
```

Save to: `Application/EdFi.Ods.AdminApi/Artifacts/MsSql/Structure/Admin/00007-CreateAuditLogs.sql`

- [ ] **Step 2: Add the PostgreSQL migration script**

```sql
-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE IF NOT EXISTS adminapi.AuditLogs (
    Id BIGINT NOT NULL GENERATED ALWAYS AS IDENTITY,
    EventType VARCHAR(30) NOT NULL,
    "Timestamp" TIMESTAMP NOT NULL,
    ClientId VARCHAR(100),
    SourceIpAddress VARCHAR(45),
    HttpVerb VARCHAR(10),
    HttpUrl VARCHAR(2048),
    StatusCode INT,
    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
);

CREATE INDEX IF NOT EXISTS idx_auditlogs_timestamp
    ON adminapi.AuditLogs ("Timestamp");

CREATE INDEX IF NOT EXISTS idx_auditlogs_clientid
    ON adminapi.AuditLogs (ClientId);
```

Save to: `Application/EdFi.Ods.AdminApi/Artifacts/PgSql/Structure/Admin/00007-CreateAuditLogs.sql`

(`Timestamp` is quoted in the PostgreSQL script because it is a reserved-adjacent identifier under the lower-case naming convention this codebase applies via `UseLowerCaseNamingConvention()`.)

- [ ] **Step 3: Add the `AuditLogs` `DbSet` and Fluent mapping to V2's `AdminApiDbContext`**

In `Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs`, add the using and `DbSet`:

```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
```

```csharp
    public DbSet<DbInstance> DbInstances { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }
```

And in `OnModelCreating`, add:

```csharp
        modelBuilder.Entity<DbInstance>().ToTable("DbInstances").HasKey(t => t.Id);
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs").HasKey(t => t.Id);
```

- [ ] **Step 4: Implement `AdminApiAuditLogWriter`**

This builds a short-lived `DbContext` per write using the connection string captured on the `AuditEvent` (not the DI-registered per-tenant context, since a background service call is outside any request scope).

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Audit;

public class AdminApiAuditLogWriter(IConfiguration configuration) : IAuditLogWriter
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var engine = DatabaseEngineEnum.Parse(configuration.Get("AppSettings:DatabaseEngine", "SqlServer"));
        var optionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
        if (engine == DatabaseEngineEnum.PostgreSql)
        {
            optionsBuilder.UseNpgsql(auditEvent.AdminConnectionString);
            optionsBuilder.UseLowerCaseNamingConvention();
        }
        else
        {
            optionsBuilder.UseSqlServer(auditEvent.AdminConnectionString);
        }

        await using var context = new AdminApiDbContext(optionsBuilder.Options, configuration);
        context.AuditLogs.Add(new AuditLog
        {
            EventType = auditEvent.EventType,
            Timestamp = auditEvent.Timestamp,
            ClientId = auditEvent.ClientId,
            SourceIpAddress = auditEvent.SourceIpAddress,
            HttpVerb = auditEvent.HttpVerb,
            HttpUrl = auditEvent.HttpUrl,
            StatusCode = auditEvent.StatusCode
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

Note: `configuration.Get<string>("AppSettings:DatabaseEngine", "SqlServer")` requires the `Get<T>` extension already used elsewhere in this project (`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Extensions/IConfigurationExtensions.cs`) — add `using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;` to the file.

- [ ] **Step 5: Write the DB test (uses the real DB fixture already used elsewhere in `EdFi.Ods.AdminApi.DBTests`)**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Infrastructure.Audit;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Infrastructure.Audit;

[TestFixture]
public class AdminApiAuditLogWriterTests : AdminApiDbContextTestBase
{
    [Test]
    public async Task WriteAsync_PersistsAuditLogRowWithAllFields()
    {
        var writer = new AdminApiAuditLogWriter(Testing.Configuration());
        var auditEvent = new AuditEvent
        {
            AdminConnectionString = ConnectionString,
            EventType = AuditEventType.Action,
            Timestamp = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc),
            ClientId = "test-client",
            SourceIpAddress = "192.168.1.1",
            HttpVerb = "DELETE",
            HttpUrl = "/v3/apiClients/42",
            StatusCode = 204
        };

        await writer.WriteAsync(auditEvent, CancellationToken.None);

        var savedRow = Transaction(context => context.AuditLogs.Single());
        savedRow.EventType.ShouldBe(AuditEventType.Action);
        savedRow.ClientId.ShouldBe("test-client");
        savedRow.SourceIpAddress.ShouldBe("192.168.1.1");
        savedRow.HttpVerb.ShouldBe("DELETE");
        savedRow.HttpUrl.ShouldBe("/v3/apiClients/42");
        savedRow.StatusCode.ShouldBe(204);
    }
}
```

- [ ] **Step 6: Run the DB test**

Run: `dotnet test Application/EdFi.Ods.AdminApi.DBTests --filter FullyQualifiedName~AdminApiAuditLogWriterTests`
Expected: PASS. (Requires a local test database configured per `docs/developer.md`'s DB test setup instructions — same prerequisite as every other test in this project.)

- [ ] **Step 7: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Artifacts/MsSql/Structure/Admin/00007-CreateAuditLogs.sql Application/EdFi.Ods.AdminApi/Artifacts/PgSql/Structure/Admin/00007-CreateAuditLogs.sql Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs Application/EdFi.Ods.AdminApi/Infrastructure/Audit/AdminApiAuditLogWriter.cs Application/EdFi.Ods.AdminApi.DBTests/Infrastructure/Audit/AdminApiAuditLogWriterTests.cs
git commit -m "feat: add AuditLogs table and writer for Admin API V2"
```

---

### Task 7: V3 audit table — DbUp scripts, EF mapping, and writer

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3/Artifacts/MsSql/Structure/Admin/00007-CreateAuditLogs.sql`
- Create: `Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Admin/00007-CreateAuditLogs.sql`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/AdminApiDbContext.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Audit/AdminApiAuditLogWriter.cs`
- Test: `Application/EdFi.Ods.AdminApi.V3.DBTests/Infrastructure/Audit/AdminApiAuditLogWriterTests.cs`

**Interfaces:**
- Consumes: same as Task 6, but against V3's `AdminApiDbContext` (`EdFi.Ods.AdminApi.V3.Infrastructure.AdminApiDbContext`).
- Produces: `adminapi.AuditLogs` table (V3 admin DB — same schema as V2, separate deployment target), `EdFi.Ods.AdminApi.V3.Infrastructure.AdminApiDbContext.AuditLogs`, `EdFi.Ods.AdminApi.V3.Infrastructure.Audit.AdminApiAuditLogWriter : IAuditLogWriter`.

This mirrors Task 6 exactly — the SQL is byte-for-byte identical (same convention as the existing `00001`–`00006` scripts, which are duplicated verbatim between the V2 and V3 `Artifacts` trees).

- [ ] **Step 1: Copy the SQL Server migration script**

Copy the content from Task 6 Step 1 verbatim to `Application/EdFi.Ods.AdminApi.V3/Artifacts/MsSql/Structure/Admin/00007-CreateAuditLogs.sql`.

- [ ] **Step 2: Copy the PostgreSQL migration script**

Copy the content from Task 6 Step 2 verbatim to `Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Admin/00007-CreateAuditLogs.sql`.

- [ ] **Step 3: Add the `AuditLogs` `DbSet` and Fluent mapping to V3's `AdminApiDbContext`**

In `Application/EdFi.Ods.AdminApi.V3/Infrastructure/AdminApiDbContext.cs`, add:

```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
```

```csharp
    public DbSet<DbInstance> DbInstances { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }
```

And in `OnModelCreating`:

```csharp
        modelBuilder.Entity<DbInstance>().ToTable("DbInstances").HasKey(t => t.Id);
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs").HasKey(t => t.Id);
```

- [ ] **Step 4: Implement V3's `AdminApiAuditLogWriter`**

Identical to Task 6 Step 4, but in namespace `EdFi.Ods.AdminApi.V3.Infrastructure.Audit` and constructing `EdFi.Ods.AdminApi.V3.Infrastructure.AdminApiDbContext`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Audit;

public class AdminApiAuditLogWriter(IConfiguration configuration) : IAuditLogWriter
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var engine = DatabaseEngineEnum.Parse(configuration.Get("AppSettings:DatabaseEngine", "SqlServer"));
        var optionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
        if (engine == DatabaseEngineEnum.PostgreSql)
        {
            optionsBuilder.UseNpgsql(auditEvent.AdminConnectionString);
            optionsBuilder.UseLowerCaseNamingConvention();
        }
        else
        {
            optionsBuilder.UseSqlServer(auditEvent.AdminConnectionString);
        }

        await using var context = new AdminApiDbContext(optionsBuilder.Options, configuration);
        context.AuditLogs.Add(new AuditLog
        {
            EventType = auditEvent.EventType,
            Timestamp = auditEvent.Timestamp,
            ClientId = auditEvent.ClientId,
            SourceIpAddress = auditEvent.SourceIpAddress,
            HttpVerb = auditEvent.HttpVerb,
            HttpUrl = auditEvent.HttpUrl,
            StatusCode = auditEvent.StatusCode
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Write the DB test**

Mirror Task 6 Step 5 in `Application/EdFi.Ods.AdminApi.V3.DBTests/Infrastructure/Audit/AdminApiAuditLogWriterTests.cs`, adjusting namespaces to `EdFi.Ods.AdminApi.V3.Infrastructure.Audit` / `EdFi.Ods.AdminApi.V3.DBTests`, and using whatever base test fixture the V3 DB tests project already uses for `AdminApiDbContext` (check `Application/EdFi.Ods.AdminApi.V3.DBTests` for an existing base class analogous to `AdminApiDbContextTestBase` before writing this — follow that project's established pattern rather than assuming it is identical to V2's).

- [ ] **Step 6: Run the DB test**

Run: `dotnet test Application/EdFi.Ods.AdminApi.V3.DBTests --filter FullyQualifiedName~AdminApiAuditLogWriterTests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Artifacts/MsSql/Structure/Admin/00007-CreateAuditLogs.sql Application/EdFi.Ods.AdminApi.V3/Artifacts/PgSql/Structure/Admin/00007-CreateAuditLogs.sql Application/EdFi.Ods.AdminApi.V3/Infrastructure/AdminApiDbContext.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Audit/AdminApiAuditLogWriter.cs Application/EdFi.Ods.AdminApi.V3.DBTests/Infrastructure/Audit/AdminApiAuditLogWriterTests.cs
git commit -m "feat: add AuditLogs table and writer for Admin API V3"
```

---

### Task 8: Wire it all together — DI registration, middleware pipeline, and configuration

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs`
- Modify: `Application/EdFi.Ods.AdminApi/Program.cs`
- Modify: `Application/EdFi.Ods.AdminApi/appsettings.json`
- Modify: `Application/EdFi.Ods.AdminApi.V3/appsettings.json` (design-time-only file — kept in sync with the main `appsettings.json` per existing convention, even though it is not loaded at runtime; see the design doc's "Runtime Topology Note")

**Interfaces:**
- Consumes: `AuditLoggingSettings`, `AuditLogChannel`, `IAuditEventRecorder`/`AuditEventRecorder`, `IAuditLogWriter`, `AuditLogBackgroundService`, `AuditActionLoggingMiddleware` (Tasks 1–4); `AdminApiAuditLogWriter` from both V2 (Task 6) and V3 (Task 7) namespaces.
- Produces: a fully wired, end-to-end audit pipeline reachable from a running app.

- [ ] **Step 1: Register the audit services in `WebApplicationBuilderExtensions.AddServices`**

In `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs`, add near the top of `AddServices` (after the existing `webApplicationBuilder.Services.Configure<AppSettings>(...)` line):

```csharp
        webApplicationBuilder.Services.Configure<AuditLoggingSettings>(config.GetSection("AuditLogging"));
        webApplicationBuilder.Services.AddSingleton<AuditLogChannel>();
        webApplicationBuilder.Services.AddSingleton<IAuditEventRecorder, AuditEventRecorder>();
```

Then, inside the existing `if (adminApiMode == AdminApiMode.V2) { ... } else if (adminApiMode == AdminApiMode.V3) { ... } else { ... }` block (around line 74-96, where `RegisterAdminApiServices` is called per mode), add the per-version writer registration. Since that block currently only handles V1/V2/V3 assembly loading, add a small separate switch right after it:

```csharp
        if (adminApiMode == AdminApiMode.V3)
        {
            webApplicationBuilder.Services.AddSingleton<
                IAuditLogWriter,
                EdFi.Ods.AdminApi.V3.Infrastructure.Audit.AdminApiAuditLogWriter
            >();
        }
        else
        {
            webApplicationBuilder.Services.AddSingleton<IAuditLogWriter, Audit.AdminApiAuditLogWriter>();
        }

        webApplicationBuilder.Services.AddHostedService<AuditLogBackgroundService>();
```

(V1 has no distinct `AdminApiDbContext` type of its own — `AddDatabases` registers the same `EdFi.Ods.AdminApi.Infrastructure.AdminApiDbContext` for both V1 and V2 modes — so V1 correctly falls into the `else` branch above and reuses the V2 writer. This means the shared `SecurityExtensions.cs` auth-event hooks from Task 5, which are not mode-gated, will also record audit events when running in V1 mode; this is harmless and out of scope to prevent, since V1 is legacy and not a target of this feature's acceptance criteria, but it is a side effect worth knowing about.)

Add the needed `using` directives at the top of the file:

```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Infrastructure.Audit;
```

- [ ] **Step 2: Register the action-logging middleware in `Program.cs`**

In `Application/EdFi.Ods.AdminApi/Program.cs`, after `app.UseAuthorization();` and before `app.MapFeatureEndpoints();`, add:

```csharp
app.UseMiddleware<AuditActionLoggingMiddleware>();
```

Add `using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;` to the file's `using` block.

- [ ] **Step 3: Add the `AuditLogging` section to `appsettings.json`**

In `Application/EdFi.Ods.AdminApi/appsettings.json`, add a new top-level section (alongside `SwaggerSettings`, `Authentication`, etc.):

```json
  "AuditLogging": {
    "Enabled": true
  },
```

Also add the same section to `Application/EdFi.Ods.AdminApi.V3/appsettings.json` for consistency with that project's existing (design-time-only) copy of the file.

- [ ] **Step 4: Build the whole solution**

Run: `dotnet build Application/EdFi.Ods.AdminApi.sln` (or the solution file used by this repo's `build.ps1`)
Expected: Build succeeds with no errors.

- [ ] **Step 5: Run the full unit test suite**

Run: `./build.ps1 -Command UnitTest`
Expected: All tests pass, including the new `AuditEventRecorderTests`, `AuditLogBackgroundServiceTests`, and `AuditActionLoggingMiddlewareTests`.

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs Application/EdFi.Ods.AdminApi/Program.cs Application/EdFi.Ods.AdminApi/appsettings.json Application/EdFi.Ods.AdminApi.V3/appsettings.json
git commit -m "feat: wire up audit logging services, middleware, and configuration"
```

---

### Task 9: Run DbUp migrations and manually verify end-to-end

**Files:** none (verification task — no new files)

**Interfaces:**
- Consumes: everything from Tasks 1–8.
- Produces: confirmation that the feature works against a real running instance, for both database engines the constraints require.

- [ ] **Step 1: Apply migrations locally**

Run: `./eng/run-dbup-migrations.ps1` (per `docs/developer.md`'s DB migration instructions) against a local SQL Server instance, then verify `adminapi.AuditLogs` exists:

```sql
SELECT * FROM adminapi.AuditLogs;
```

Expected: table exists, empty.

- [ ] **Step 2: Repeat against PostgreSQL**

Switch `AppSettings:DatabaseEngine` to `PostgreSql` (or run against the Docker Compose PostgreSQL profile per `docs/developer.md`), re-run the migration script, and verify the table exists there too.

- [ ] **Step 3: Start the app in V2 mode and exercise both capture points**

Run: `./build.ps1 run` (or the Visual Studio launch profile for V2).
- Call `/connect/token` with valid credentials → expect one new `AuditLogs` row with `EventType = 'AuthenticationSuccess'`.
- Call `/connect/token` with an invalid client secret → expect one new row with `EventType = 'AuthenticationFailure'`.
- Call a protected GET endpoint with no `Authorization` header → expect a 401 and one new row with `EventType = 'AuthenticationFailure'`.
- Call `POST /v2/vendors` (or any mutating V2 endpoint) with a valid token → expect one new row with `EventType = 'Action'`, correct `HttpVerb`/`HttpUrl`/`StatusCode`.
- Call `GET /v2/vendors` → expect no new row (GETs are excluded).

- [ ] **Step 4: Repeat in V3 mode**

Switch `AppSettings:AdminApiMode` to `V3`, restart, and repeat Step 3's checks against the equivalent V3 endpoints.

- [ ] **Step 5: Verify the disable flag**

Set `AuditLogging:Enabled` to `false`, restart, repeat one action and one auth call, and confirm no new rows are written to `AuditLogs`.

- [ ] **Step 6: Verify the audit table is not reachable via HTTP**

Confirm there is no endpoint, controller action, or route anywhere in the codebase that returns `AuditLog` data (grep for `AuditLog` usage outside the files created in this plan, to make sure no accidental endpoint was added).

No commit for this task — it's manual verification of already-committed work.

---

### Task 10: Documentation

**Files:**
- Create: `docs/audit-logging.md`
- Modify: `docs/developer.md` (add a link to the new doc)

**Interfaces:**
- Consumes: none — this documents the finished feature from Tasks 1–9.
- Produces: the acceptance-criteria documentation deliverable.

- [ ] **Step 1: Write `docs/audit-logging.md`**

Cover, concretely (no placeholders):
- What the `AuditLogging:Enabled` flag does and where it lives (`appsettings.json`, top-level `AuditLogging` section), noting it is a single flag — not per API version — because a deployed instance only ever runs one of V1/V2/V3 at a time.
- The exact list of captured events: `AuthenticationSuccess`/`AuthenticationFailure` at `/connect/token`; `AuthenticationFailure` for any request elsewhere rejected with a 401 (invalid/expired/missing token); `Action` for every POST/PUT/PATCH/DELETE request (GETs excluded).
- The `adminapi.AuditLogs` table schema (the column table from the design doc), including which columns are populated for which event type.
- The fail-open guarantee: audit-write failures never block or fail the original request; after 2 retries, a failure is recorded via the existing log4net text log instead.
- That the table is per-tenant (in multitenant mode, each tenant's own `EdFi_Admin` database gets its own `AuditLogs` table) and is never exposed via any HTTP endpoint — it is queryable only via direct database access.
- Out-of-scope items carried over from `task.md` (no deleted-object natural key capture yet, no retention/rotation policy, no admin UI).

- [ ] **Step 2: Link it from `docs/developer.md`**

Add a line under the appropriate existing section of `docs/developer.md` pointing to `docs/audit-logging.md`, following that file's existing link conventions.

- [ ] **Step 3: Commit**

```bash
git add docs/audit-logging.md docs/developer.md
git commit -m "docs: document audit trail logging configuration and captured events"
```

---

## Post-Implementation Checklist

- [ ] All unit tests pass: `./build.ps1 -Command UnitTest`
- [ ] All DB tests pass against both SQL Server and PostgreSQL: `./build.ps1 -Command IntegrationTest` (or the project's documented DB test command)
- [ ] Manual verification from Task 9 completed for both V2 and V3, both database engines
- [ ] `docs/audit-logging.md` reviewed for accuracy against the final implementation
