# EnableDataStoreManagement Feature Flag Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an `EnableDataStoreManagement` `AppSettings` flag (default `true`) that, when set `false`, disables the 6 Manage endpoints (V2 `OdsInstances/Manage` + V3 `DataStores/Manage`) with a 400 and stops the 4 create/delete dispatcher jobs from being scheduled, while leaving `RefreshEducationOrganizationsJob` untouched.

**Architecture:** Reuse the existing `EnableApplicationResetEndpoint` convention — a first-line flag check in each handler that throws `FluentValidation.ValidationException`, converted to a 400 by existing middleware. Job scheduling gets an `internal static` predicate class in `Program.cs`, testable via the file's existing `InternalsVisibleTo`.

**Tech Stack:** ASP.NET Core minimal APIs, FluentValidation, Quartz.NET, NUnit + Shouldly + FakeItEasy, Docker Compose, Bruno E2E.

**Design doc:** `docs/superpowers/specs/2026-08-05-enable-datastore-management-flag-design.md`

## Global Constraints

- `EnableDataStoreManagement` defaults to `true` — every task must preserve this so existing tests/deployments that don't set it keep working unchanged.
- `PropertyName` on the thrown `ValidationFailure` is `nameof(OdsInstance)` (from `EdFi.Admin.DataAccess.Models`) for **all six** handlers, V2 and V3 alike — there is no separate `DataStore` domain type.
- `RefreshEducationOrganizationsJob` scheduling (V2 and V3) must never be touched by this work.
- Error message text for the disabled endpoints is exactly: `"This endpoint has been disabled on application settings."` (matches `ResetApplicationCredentials` verbatim).
- Docker compose additions use the parameterized convention: `AppSettings__EnableDataStoreManagement: ${ENABLE_DATA_STORE_MANAGEMENT:-true}` — not the hardcoded-`false` variant some older files use for the reset-endpoint flag.

---

## Task 1: Add `EnableDataStoreManagement` setting

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.Common/Settings/AppSettings.cs:25`
- Modify: `Application/EdFi.Ods.AdminApi/appsettings.json:10`
- Modify: `Application/EdFi.Ods.AdminApi.V3/appsettings.json:10`

**Interfaces:**
- Produces: `AppSettings.EnableDataStoreManagement` (`bool`, default `true`) — consumed by Tasks 2, 3, and 4.

- [ ] **Step 1: Add the property to `AppSettings`**

In `Application/EdFi.Ods.AdminApi.Common/Settings/AppSettings.cs`, add the new property right after `EnableApplicationResetEndpoint` (line 25):

```csharp
    public bool EnableApplicationResetEndpoint { get; set; }
    public bool EnableDataStoreManagement { get; set; } = true;
    public int EdOrgsRefreshIntervalInMins { get; set; }
```

- [ ] **Step 2: Add the setting to both `appsettings.json` files**

In `Application/EdFi.Ods.AdminApi/appsettings.json`, right after line 10 (`"EnableApplicationResetEndpoint": false,`):

```json
        "EnableApplicationResetEndpoint": false,
        "EnableDataStoreManagement": true,
        "EdOrgsRefreshIntervalInMins": 60,
```

Make the identical change in `Application/EdFi.Ods.AdminApi.V3/appsettings.json`.

- [ ] **Step 3: Build to verify it compiles**

Run: `./build.ps1 -Command build`
Expected: build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Settings/AppSettings.cs Application/EdFi.Ods.AdminApi/appsettings.json Application/EdFi.Ods.AdminApi.V3/appsettings.json
git commit -m "[ADMINAPI-1489] Add EnableDataStoreManagement setting (default true)"
```

---

## Task 2: Gate the 3 V2 Manage endpoints (6 handler methods → 4 handler methods, 2 already gated by signature reuse)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/AddOdsInstanceManage.cs`
- Modify: `Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/DeleteOdsInstanceManage.cs`
- Modify: `Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/ReadOdsInstanceManage.cs`
- Modify (existing tests, signature fix required): `Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/ReadOdsInstanceManageTests.cs`
- Modify (new tests): `Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/AddOdsInstanceManageTests.cs`
- Modify (new tests): `Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/DeleteOdsInstanceManageTests.cs`

**Interfaces:**
- Consumes: `AppSettings.EnableDataStoreManagement` (Task 1).
- Produces: nothing new consumed by later tasks — this task is self-contained.

### Step 1: Write/update the failing tests

**1a. `ReadOdsInstanceManageTests.cs`** — every existing call to `GetOdsInstanceManages`/`GetOdsInstanceManage` needs a new trailing `IOptions<AppSettings>` argument (the handlers are gaining a required parameter), plus 2 new disabled-flag tests.

Replace the usings block (lines 6–15) with:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.OdsInstances.Manage;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
```

Add a shared enabled-options field to the test class (right after the `[TestFixture]`/class declaration, line 20):

```csharp
[TestFixture]
public class ReadOdsInstanceManageTests
{
    private static readonly IOptions<AppSettings> EnabledOptions = Options.Create(new AppSettings());

```

Update every existing call site to pass `EnabledOptions` as the last argument:

```csharp
        var result = await ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, queryParams, null, null, EnabledOptions);
```
(line 34), and identically for the calls at lines 54, 73, 85, 96, 112, 126 — same pattern, append `, EnabledOptions` to each existing argument list for both `GetOdsInstanceManages` and `GetOdsInstanceManage` calls.

Add 2 new tests at the end of the class (before the closing `}` at line 130):

```csharp
    [Test]
    public void GetOdsInstanceManages_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = Should.Throw<ValidationException>(() =>
            ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, new CommonQueryParams(0, 10), null, null, disabledOptions).GetAwaiter().GetResult());

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }

    [Test]
    public void GetOdsInstanceManage_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManageByIdQuery>();
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = Should.Throw<ValidationException>(() =>
            ReadOdsInstanceManage.GetOdsInstanceManage(fakeQuery, 1, disabledOptions).GetAwaiter().GetResult());

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
```

**1b. `AddOdsInstanceManageTests.cs`** — add one new test at the end of the class (before the closing `}` / `#nullable restore`):

```csharp
    [Test]
    public async Task Handle_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await AddOdsInstanceManage.Handle(null!, null!, null!, null!, disabledOptions, null!));

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
```

(`System.Linq`, `EdFi.Admin.DataAccess.Models`, `FluentValidation`, and `Microsoft.Extensions.Options` are already imported in this file — no using changes needed.)

**1c. `DeleteOdsInstanceManageTests.cs`** — add `using System.Linq;` and `using EdFi.Admin.DataAccess.Models;` to the usings block (lines 6–24), then add one new test at the end of the class (before the closing `}` / `#nullable restore`):

```csharp
    [Test]
    public async Task Handle_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            DeleteOdsInstanceManage.Handle(null!, null!, null!, null!, disabledOptions, 1));

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
```

### Step 2: Run tests to verify they fail

Run: `./build.ps1 -Command UnitTest -TestFilter "FullyQualifiedName~OdsInstances.Manage" -NoBuild`
Expected: **build/compile failure** — `ReadOdsInstanceManage.GetOdsInstanceManages`/`GetOdsInstanceManage` don't yet accept the new trailing argument, and `EnableDataStoreManagement` doesn't exist as a settable property path relative to the flag check that doesn't exist yet. (This compile failure is the expected RED state for a signature-driving change.)

### Step 3: Implement the gating

**`AddOdsInstanceManage.cs`** — add `using EdFi.Admin.DataAccess.Models;` and `using FluentValidation.Results;` to the usings (after line 20, `using FluentValidation;`). Insert the guard as the first statement of `Handle` (before line 54):

```csharp
    public async static Task<IResult> Handle(
        Validator validator,
        AddOdsInstanceManageCommand addOdsInstanceManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        AddOdsInstanceManageRequest request)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        await validator.GuardAsync(request);
```

**`DeleteOdsInstanceManage.cs`** — add `using EdFi.Admin.DataAccess.Models;` to the usings (`FluentValidation` and `FluentValidation.Results` are already imported). Insert the guard as the first statement of `Handle` (before line 45):

```csharp
    public static async Task<IResult> Handle(
        IGetOdsInstanceManageByIdQuery getOdsInstanceManageByIdQuery,
        IDeleteOdsInstanceManageCommand deleteOdsInstanceManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        int id
    )
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var odsInstanceManage = getOdsInstanceManageByIdQuery.Execute(id);
```

**`ReadOdsInstanceManage.cs`** — replace the usings block (lines 6–9) with:

```csharp
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
```

Replace both handler methods (lines 28–44) with:

```csharp
    public static Task<IResult> GetOdsInstanceManages(IGetOdsInstanceManagesQuery query,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var list = OdsInstanceManageMapper.ToModelList(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetOdsInstanceManage(IGetOdsInstanceManageByIdQuery query, int id,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var odsInstanceManage = query.Execute(id);
        if (odsInstanceManage == null)
        {
            throw new NotFoundException<int>("odsInstanceManage", id);
        }
        var model = OdsInstanceManageMapper.ToModel(odsInstanceManage);
        return Task.FromResult(Results.Ok(model));
    }
```

### Step 4: Run tests to verify they pass

Run: `./build.ps1 -Command UnitTest -TestFilter "FullyQualifiedName~OdsInstances.Manage" -NoBuild`
Expected: PASS — all existing tests (updated call sites) and the 4 new disabled-flag tests pass.

### Step 5: Commit

```bash
git add Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/AddOdsInstanceManage.cs Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/DeleteOdsInstanceManage.cs Application/EdFi.Ods.AdminApi/Features/OdsInstances/Manage/ReadOdsInstanceManage.cs Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/ReadOdsInstanceManageTests.cs Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/AddOdsInstanceManageTests.cs Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/Manage/DeleteOdsInstanceManageTests.cs
git commit -m "[ADMINAPI-1489] Gate V2 OdsInstances Manage endpoints behind EnableDataStoreManagement"
```

---

## Task 3: Gate the 3 V3 Manage endpoints

Mirrors Task 2 exactly, applied to the V3 `DataStores/Manage` files and tests. Same `nameof(OdsInstance)` PropertyName (the underlying entity is identical to V2's).

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/AddDataStoreManage.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/DeleteDataStoreManage.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/ReadDataStoreManage.cs`
- Modify (existing tests, signature fix required): `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/ReadDataStoreManageTests.cs`
- Modify (new tests): `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/AddDataStoreManageTests.cs`
- Modify (new tests): `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/DeleteDataStoreManageTests.cs`

**Interfaces:**
- Consumes: `AppSettings.EnableDataStoreManagement` (Task 1).
- Produces: nothing consumed by later tasks.

### Step 1: Write/update the failing tests

**1a. `ReadDataStoreManageTests.cs`** — same treatment as `ReadOdsInstanceManageTests.cs` in Task 2. Replace the usings block (lines 6–15) with:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
```

Add the shared enabled-options field:

```csharp
[TestFixture]
public class ReadDataStoreManageTests
{
    private static readonly IOptions<AppSettings> EnabledOptions = Options.Create(new AppSettings());

```

Append `, EnabledOptions` to every existing `GetDataStoreManages`/`GetDataStoreManage` call (lines 34, 54, 73, 85, 96, 112, 126 — same 7 call sites as Task 2's V2 equivalent).

Add 2 new tests at the end of the class:

```csharp
    [Test]
    public void GetDataStoreManages_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var fakeQuery = A.Fake<IGetDataStoreManagesQuery>();
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = Should.Throw<ValidationException>(() =>
            ReadDataStoreManage.GetDataStoreManages(fakeQuery, new CommonQueryParams(0, 10), null, null, disabledOptions).GetAwaiter().GetResult());

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }

    [Test]
    public void GetDataStoreManage_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var fakeQuery = A.Fake<IGetDataStoreManageByIdQuery>();
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = Should.Throw<ValidationException>(() =>
            ReadDataStoreManage.GetDataStoreManage(fakeQuery, 1, disabledOptions).GetAwaiter().GetResult());

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
```

**1b. `AddDataStoreManageTests.cs`** — add one new test at the end of the class:

```csharp
    [Test]
    public async Task Handle_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await AddDataStoreManage.Handle(null!, null!, null!, null!, disabledOptions, null!, null!));

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
```

Note the extra trailing `null!` compared to Task 2's V2 equivalent — `AddDataStoreManage.Handle` takes an additional `HttpContext httpContext` parameter that V2's `AddOdsInstanceManage.Handle` doesn't have.

**1c. `DeleteDataStoreManageTests.cs`** — add `using System.Linq;` and `using EdFi.Admin.DataAccess.Models;` to the usings block (lines 6–29; neither is currently imported in this file), then add one new test at the end of the class:

```csharp
    [Test]
    public async Task Handle_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            DeleteDataStoreManage.Handle(null!, null!, null!, null!, disabledOptions, 1));

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
```

### Step 2: Run tests to verify they fail

Run: `./build.ps1 -Command UnitTest -TestFilter "FullyQualifiedName~DataStores.Manage" -NoBuild`
Expected: build/compile failure, same reasoning as Task 2 Step 2.

### Step 3: Implement the gating

**`AddDataStoreManage.cs`** — add `using EdFi.Admin.DataAccess.Models;` and `using FluentValidation.Results;` to the usings. Insert the guard as the first statement of `Handle`:

```csharp
    public async static Task<IResult> Handle(
        Validator validator,
        AddDataStoreManageCommand addDataStoreManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        AddDataStoreManageRequest request,
        HttpContext httpContext)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        await validator.GuardAsync(request);
```

**`DeleteDataStoreManage.cs`** — add `using EdFi.Admin.DataAccess.Models;` to the usings. Insert the guard as the first statement of `Handle`:

```csharp
    public static async Task<IResult> Handle(
        IGetDataStoreManageByIdQuery getDataStoreManageByIdQuery,
        IDeleteDataStoreManageCommand deleteDataStoreManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        int id
    )
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var dataStoreManage = getDataStoreManageByIdQuery.Execute(id);
```

**`ReadDataStoreManage.cs`** — replace the usings block (lines 6–9) with:

```csharp
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
```

Replace both handler methods with:

```csharp
    public static Task<IResult> GetDataStoreManages(IGetDataStoreManagesQuery query,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var list = DataStoreManageMapper.ToModelList(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetDataStoreManage(IGetDataStoreManageByIdQuery query, int id,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var dataStoreManage = query.Execute(id);
        if (dataStoreManage == null)
        {
            throw new NotFoundException<int>("dataStoreManage", id);
        }
        var model = DataStoreManageMapper.ToModel(dataStoreManage);
        return Task.FromResult(Results.Ok(model));
    }
```

### Step 4: Run tests to verify they pass

Run: `./build.ps1 -Command UnitTest -TestFilter "FullyQualifiedName~DataStores.Manage" -NoBuild`
Expected: PASS.

### Step 5: Commit

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/AddDataStoreManage.cs Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/DeleteDataStoreManage.cs Application/EdFi.Ods.AdminApi.V3/Features/DataStores/Manage/ReadDataStoreManage.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/ReadDataStoreManageTests.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/AddDataStoreManageTests.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStores/Manage/DeleteDataStoreManageTests.cs
git commit -m "[ADMINAPI-1489] Gate V3 DataStores Manage endpoints behind EnableDataStoreManagement"
```

---

## Task 4: Gate the 4 dispatcher jobs in `Program.cs`

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Program.cs`
- Create: `Application/EdFi.Ods.AdminApi.UnitTests/ProgramTests.cs`

**Interfaces:**
- Consumes: `AppSettings.EnableDataStoreManagement` (Task 1).
- Produces: `DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(AppSettings)` (`internal static bool`) — declared as a type in `Program.cs`, in the global namespace (top-level-statement files have no enclosing namespace), reachable from `EdFi.Ods.AdminApi.UnitTests` via the file's existing `[assembly: InternalsVisibleTo("EdFi.Ods.AdminApi.UnitTests")]` (`Program.cs:26`). Not consumed by any other task.

> **Why not a local function:** local functions declared inside top-level statements compile as private, name-mangled members of the hidden `Program` class — they cannot be called from another assembly even with `InternalsVisibleTo`. A type declared after the top-level statements (legal C# — type declarations must simply come after all top-level statements in the file) is a normal member and is callable.

- [ ] **Step 1: Write the failing test**

Create `Application/EdFi.Ods.AdminApi.UnitTests/ProgramTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Settings;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests;

[TestFixture]
public class ProgramTests
{
    [Test]
    public void ShouldScheduleDataStoreManagementJobs_WhenFlagEnabled_ReturnsTrue()
    {
        var settings = new AppSettings { EnableDataStoreManagement = true };

        DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(settings).ShouldBeTrue();
    }

    [Test]
    public void ShouldScheduleDataStoreManagementJobs_WhenFlagDisabled_ReturnsFalse()
    {
        var settings = new AppSettings { EnableDataStoreManagement = false };

        DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(settings).ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `./build.ps1 -Command UnitTest -TestFilter "FullyQualifiedName~ProgramTests" -NoBuild`
Expected: build failure — `DataStoreManagementJobScheduler` doesn't exist yet.

- [ ] **Step 3: Implement the predicate and wire it into scheduling**

In `Application/EdFi.Ods.AdminApi/Program.cs`, add two usings after line 16 (`using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;`):

```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features;
```
(`EdFi.Ods.AdminApi.Features` is already imported at line 13 — just add `EdFi.Ods.AdminApi.Common.Settings` and, separately, `Microsoft.Extensions.Options`, next to the other `Microsoft.*` usings around line 22.)

Add one line right after the existing `isMultiTenancyEnabled` read (after line 122, before `if (adminApiMode == AdminApiMode.V2)` on line 124):

```csharp
var isMultiTenancyEnabled = app.Configuration.GetValue<bool>(
    "AppSettings:MultiTenancy"
);
var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

if (adminApiMode == AdminApiMode.V2)
```

Wrap the V2 create-dispatcher block (lines 179–215) so the flag check sits between the interval check and the multi-tenant/else scheduling, without disturbing the "invalid interval" error path:

```csharp
    if (shouldScheduleDispatcher)
    {
        if (DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(appSettings))
        {
            if (isMultiTenancyEnabled)
            {
                using var scope = app.Services.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantsService>();
                var tenants = await tenantService.GetTenantsAsync(fromCache: true);

                foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
                {
                    await QuartzJobScheduler.ScheduleJob<CreatePendingOdsInstanceManagesDispatcherJob>(
                        scheduler,
                        jobKey: new JobKey($"{JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName}_{tenantName}"),
                        jobData: new Dictionary<string, object>
                        {
                            [JobConstants.TenantNameKey] = tenantName
                        },
                        startImmediately: false,
                        interval: TimeSpan.FromMinutes(createOdsInstanceManagesSweepInterval)
                    );
                }
            }
            else
            {
                await QuartzJobScheduler.ScheduleJob<CreatePendingOdsInstanceManagesDispatcherJob>(
                    scheduler,
                    jobKey: new JobKey(JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName),
                    jobData: new Dictionary<string, object>(),
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(createOdsInstanceManagesSweepInterval)
                );
            }
        }
        else
        {
            _logger.Info("EnableDataStoreManagement is false; skipping CreatePendingOdsInstanceManagesDispatcherJob scheduling.");
        }
    }
    else
    {
        _logger.Error("Invalid value for CreateOdsInstanceManagesSweepIntervalInMins. Please ensure it is a valid number.");
    }
```

Apply the identical wrapping pattern (new `if (DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(appSettings))` / `else { _logger.Info(...) }` nested one level inside the existing `if (shouldScheduleDeleteDispatcher)`, existing scheduling code unchanged inside, existing `else { _logger.Error(...) }` for the outer if unchanged) to:

- V2 delete-dispatcher block (lines 217–253) — log message: `"EnableDataStoreManagement is false; skipping DeletePendingOdsInstanceManagesDispatcherJob scheduling."`
- V3 create-dispatcher block (lines 309–345) — log message: `"EnableDataStoreManagement is false; skipping CreatePendingDataStoreManagesDispatcherJob scheduling."`
- V3 delete-dispatcher block (lines 347–383) — log message: `"EnableDataStoreManagement is false; skipping DeletePendingDataStoreManagesDispatcherJob scheduling."`

Do **not** touch either `shouldScheduleEdOrgsRefresh` block (V2 lines 140–177, V3 lines 271–307) — `RefreshEducationOrganizationsJob` scheduling stays exactly as-is.

Finally, add the predicate class after the last line of the file (`await app.RunAsync();`):

```csharp
await app.RunAsync();

internal static class DataStoreManagementJobScheduler
{
    public static bool ShouldScheduleDataStoreManagementJobs(AppSettings settings) =>
        settings.EnableDataStoreManagement;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `./build.ps1 -Command UnitTest -TestFilter "FullyQualifiedName~ProgramTests" -NoBuild`
Expected: PASS.

- [ ] **Step 5: Run the full unit test suite** (this task touches shared startup code)

Run: `./build.ps1 -Command UnitTest`
Expected: PASS — no regressions in unrelated tests.

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Program.cs Application/EdFi.Ods.AdminApi.UnitTests/ProgramTests.cs
git commit -m "[ADMINAPI-1489] Skip dispatcher job scheduling when EnableDataStoreManagement is false"
```

---

## Task 5: Update Docker compose files

**Files:**
- Modify: all 36 files currently containing `AppSettings__EnableApplicationResetEndpoint` under `Docker/` (V1, V2, V3 × mssql/pgsql × SingleTenant/MultiTenant × dev/binaries/idp-dev/idp-binaries variants — full list obtained via `grep -rl AppSettings__EnableApplicationResetEndpoint Docker/`).

**Interfaces:**
- Consumes: nothing from prior tasks (purely additive compose configuration).
- Produces: `ENABLE_DATA_STORE_MANAGEMENT` env var, consumed by Task 6's E2E documentation.

- [ ] **Step 1: Run the batch-edit script**

There is no meaningful "failing test" for a compose-file text change — this is a mechanical, scripted edit across 36 files, verified by diff review immediately after (Step 2). Run this from the repo root in PowerShell:

```powershell
Get-ChildItem -Path Docker -Recurse -Filter *.yml | ForEach-Object {
    $path = $_.FullName
    $lines = Get-Content -LiteralPath $path
    $matchIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'AppSettings__EnableApplicationResetEndpoint') { $matchIndex = $i; break }
    }
    if ($matchIndex -ge 0) {
        $indent = ($lines[$matchIndex] -replace '^(\s*).*$', '$1')
        $newLine = $indent + 'AppSettings__EnableDataStoreManagement: ${ENABLE_DATA_STORE_MANAGEMENT:-true}'
        $updated = New-Object System.Collections.Generic.List[string]
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $updated.Add($lines[$i])
            if ($i -eq $matchIndex) { $updated.Add($newLine) }
        }
        Set-Content -LiteralPath $path -Value $updated
        Write-Host "Updated: $path"
    }
}
```

- [ ] **Step 2: Verify the change count and spot-check content**

Run: `git diff --stat Docker/ | Select-Object -Last 5`
Expected: 36 files changed, 1 insertion each.

Run: `git diff Docker/V3/Compose/pgsql/SingleTenant/compose-build-dev.yml`
Expected diff shows exactly one added line, immediately after the `AppSettings__EnableApplicationResetEndpoint` line, reading:
```yaml
      AppSettings__EnableDataStoreManagement: ${ENABLE_DATA_STORE_MANAGEMENT:-true}
```

Run: `git diff Docker/V1/Compose/mssql/compose-build-dev.yml`
Expected: same pattern — the new line uses the parameterized form even though the neighboring V1 `EnableApplicationResetEndpoint` line is hardcoded `false` (per the Global Constraints convention decision).

- [ ] **Step 3: Build-verify one compose file parses correctly**

Run: `docker compose -f Docker/V3/Compose/pgsql/SingleTenant/compose-build-dev.yml config --quiet`
Expected: no YAML/interpolation errors (exit code 0).

- [ ] **Step 4: Commit**

```bash
git add Docker/
git commit -m "[ADMINAPI-1489] Add ENABLE_DATA_STORE_MANAGEMENT env var to all compose files"
```

---

## Task 6: Documentation and E2E disabled-flag coverage

**Files:**
- Modify: `docs/developer.md`
- Create: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/POST - OdsInstances Manage - Feature Disabled.bru.disabled`
- Create: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/POST - DataStores Manage - Feature Disabled.bru.disabled`

**Interfaces:**
- Consumes: `ENABLE_DATA_STORE_MANAGEMENT` env var (Task 5).
- Produces: nothing consumed elsewhere — final task.

- [ ] **Step 1: Add the new Bruno specs**

These use the `.disabled` suffix convention already present in these folders (e.g. `DELETE - OdsInstance Manage - Success.bru.disabled`), so they're skipped by default recursive runs and only exercised manually against a disabled-flag instance.

Create `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/POST - OdsInstances Manage - Feature Disabled.bru.disabled`:

```
meta {
  name: OdsInstances Manage - Feature Disabled
  type: http
  seq: 2
}

post {
  url: {{API_URL}}/v2/odsinstances/manage
  body: json
  auth: inherit
}

body:json {
  {
    "name": "Test DB Instance",
    "databaseTemplate": "Minimal"
  }
}

script:post-response {
  test("POST OdsInstances Manage - Feature Disabled: Status code is Bad Request", function () {
    expect(res.getStatus()).to.equal(400);
  });

  test("POST OdsInstances Manage - Feature Disabled: Response indicates the endpoint is disabled", function () {
    const body = JSON.stringify(res.getBody());
    expect(body).to.include("disabled on application settings");
  });
}

settings {
  encodeUrl: true
}
```

Create `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/POST - DataStores Manage - Feature Disabled.bru.disabled`:

```
meta {
  name: DataStores Manage - Feature Disabled
  type: http
  seq: 2
}

post {
  url: {{API_URL}}/v3/dataStores/manage
  body: json
  auth: inherit
}

body:json {
  {
    "name": "Test DB Instance",
    "databaseTemplate": "Minimal"
  }
}

script:post-response {
  test("POST DataStores Manage - Feature Disabled: Status code is Bad Request", function () {
    expect(res.getStatus()).to.equal(400);
  });

  test("POST DataStores Manage - Feature Disabled: Response indicates the endpoint is disabled", function () {
    const body = JSON.stringify(res.getBody());
    expect(body).to.include("disabled on application settings");
  });
}

settings {
  encodeUrl: true
}
```

- [ ] **Step 2: Document the flag in `docs/developer.md`**

Add a new subsection right after "OdsInstanceManage Provisioning Jobs" (after line 278, before the `### Audit Trail Logging` heading on line 280):

```markdown

### Disabling DataStore Management

Set `AppSettings:EnableDataStoreManagement` to `false` (default `true`) to fully disable instance-management: the 6 Manage endpoints (V2 `/odsInstances/manage*`, V3 `/dataStores/manage*`) return `400` with a "disabled on application settings" message, and the 4 create/delete dispatcher jobs (2× V2, 2× V3) are skipped at startup. `RefreshEducationOrganizationsJob` is unaffected and continues to run regardless of this flag.

To exercise the disabled-flag path via Bruno E2E locally, set the compose env var before running the suite, e.g.:

```powershell
$env:ENABLE_DATA_STORE_MANAGEMENT = "false"
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -BrunoFilter "v3/DataStores/Manage"
```

Then temporarily rename the `*.bru.disabled` "Feature Disabled" specs in the `Manage/` folders to `*.bru` to include them in that run.
```

- [ ] **Step 3: Verify the docs render correctly**

Run: `git diff docs/developer.md` and confirm the new subsection reads correctly in context (correct heading level, no broken markdown list/code-fence nesting).

- [ ] **Step 4: Commit**

```bash
git add docs/developer.md "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/OdsInstances/Manage/POST - OdsInstances Manage - Feature Disabled.bru.disabled" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStores/Manage/POST - DataStores Manage - Feature Disabled.bru.disabled"
git commit -m "[ADMINAPI-1489] Document EnableDataStoreManagement and add disabled-flag E2E specs"
```

---

## Final verification (after all 6 tasks)

- [ ] Run the full unit test suite: `./build.ps1 -Command UnitTest`. Expected: all tests pass, including the new tests from Tasks 2, 3, and 4.
- [ ] Run `./build.ps1 -Command build` once more to confirm a clean full build.
- [ ] Confirm via `git log --oneline` that all 6 task commits are present on the branch in order.
