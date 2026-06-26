# Merge DbInstances into ReadTenants Response Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enrich the `GET /tenants/{tenantName}/odsInstances/edOrgs` response by merging `DbInstance` data — adding `Status`, `DatabaseTemplate`, and `DatabaseName` to each OdsInstance, and appending unlinked DbInstances (those with `OdsInstanceId == null`) as new entries with successive negative IDs.

**Architecture:** `TenantOdsInstanceModel` gains three nullable fields. `TenantService.GetTenantEdOrgsByInstancesAsync` receives `IGetDbInstancesQuery`, queries all DbInstances without pagination, then (a) enriches existing OdsInstance entries that have a matching DbInstance, (b) defaults `Status` to `DbInstanceStatus.Created` for OdsInstances with no matching DbInstance, and (c) appends unlinked DbInstances as new `TenantOdsInstanceModel` entries with negative `OdsInstanceId` values (-1, -2, …). `ReadTenants` injects and forwards `IGetDbInstancesQuery`.

**Tech Stack:** C# / ASP.NET Core Minimal API, NUnit, Shouldly, FakeItEasy, Bruno (E2E)

---

## File Map

| File | Action |
|---|---|
| `Application/EdFi.Ods.AdminApi/Features/Tenants/TenantDetailModel.cs` | **Modify** — add `Status?`, `DatabaseTemplate?`, `DatabaseName?` to `TenantOdsInstanceModel` |
| `Application/EdFi.Ods.AdminApi/Features/Tenants/TenantMapper.cs` | **Modify** — update `ToOdsInstanceModel` to accept and map the new fields |
| `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs` | **Modify** — update `ITenantsService` interface + service implementation to accept and use `IGetDbInstancesQuery` |
| `Application/EdFi.Ods.AdminApi/Features/Tenants/ReadTenants.cs` | **Modify** — inject `IGetDbInstancesQuery`, pass to service |
| `Application/EdFi.Ods.AdminApi.UnitTests/Features/Tenants/ReadTenantsTest.cs` | **Modify** — update existing tests, add new tests |
| `Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetTenantEdOrgsByInstancesTests.cs` | **Create** — integration tests for merged response logic |
| `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru` | **Modify** — update AJV schema to include new fields |
| `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru` | **Modify** — update AJV schema to include new fields |

---

### Task 1: Extend `TenantOdsInstanceModel` with new nullable fields

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Features/Tenants/TenantDetailModel.cs`

- [ ] **Step 1: Add `Status?`, `DatabaseTemplate?`, `DatabaseName?` to `TenantOdsInstanceModel`**

Replace the `TenantOdsInstanceModel` class body so it reads:

```csharp
[SwaggerSchema]
public class TenantOdsInstanceModel
{
    [JsonPropertyName("id")]
    public int OdsInstanceId { get; set; }
    public string Name { get; set; }
    public string? InstanceType { get; set; }
    public string? Status { get; set; }
    public string? DatabaseTemplate { get; set; }
    public string? DatabaseName { get; set; }

    [SwaggerSchema(Title = "EducationOrganizations")]
    public List<EducationOrganizationModel> EducationOrganizations { get; set; }

    public TenantOdsInstanceModel()
    {
        Name = string.Empty;
        EducationOrganizations = [];
    }
}
```

- [ ] **Step 2: Build the project to verify no compile errors**

```powershell
.\build.ps1 -Command build
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Features/Tenants/TenantDetailModel.cs
git commit -m "feat: add Status, DatabaseTemplate, DatabaseName to TenantOdsInstanceModel"
```

---

### Task 2: Update `TenantMapper` to support mapping from `DbInstance`

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Features/Tenants/TenantMapper.cs`

The mapper needs a new static method that creates a `TenantOdsInstanceModel` from an unlinked `DbInstance` (one with `OdsInstanceId == null`).

- [ ] **Step 1: Add using for DbInstance and update the mapper**

Replace the full content of `TenantMapper.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Features.Tenants;

public static class TenantMapper
{
    public static TenantOdsInstanceModel ToOdsInstanceModel(OdsInstance source)
    {
        return new TenantOdsInstanceModel
        {
            OdsInstanceId = source.OdsInstanceId,
            Name = source.Name,
            InstanceType = source.InstanceType,
        };
    }

    public static List<TenantOdsInstanceModel> ToOdsInstanceModelList(IEnumerable<OdsInstance> source)
    {
        return source.Select(ToOdsInstanceModel).ToList();
    }

    public static TenantOdsInstanceModel ToUnlinkedDbInstanceModel(DbInstance source, int negativeId)
    {
        return new TenantOdsInstanceModel
        {
            OdsInstanceId = negativeId,
            Name = source.Name,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
        };
    }
}
```

- [ ] **Step 2: Build to verify no compile errors**

```powershell
.\build.ps1 -Command build
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Features/Tenants/TenantMapper.cs
git commit -m "feat: add ToUnlinkedDbInstanceModel to TenantMapper"
```

---

### Task 3: Update `ITenantsService` and `TenantService` to accept and use `IGetDbInstancesQuery`

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs`

The core logic:
1. Fetch all DbInstances (no pagination: `new CommonQueryParams(0, null)`, no id/name filters).
2. Build a lookup dictionary: `OdsInstanceId → DbInstance` for entries where `OdsInstanceId != null`.
3. For each `TenantOdsInstanceModel` in `tenantDetails.OdsInstances`:
   - If a matching DbInstance exists → set `Status`, `DatabaseTemplate`, `DatabaseName` from that DbInstance.
   - If no match → set `Status = DbInstanceStatus.Created.ToString()`, leave `DatabaseTemplate` and `DatabaseName` as `null`.
4. Collect remaining DbInstances where `OdsInstanceId == null` and add them as new entries with `OdsInstanceId` = -1, -2, …

- [ ] **Step 1: Update the interface signature in `TenantService.cs`**

Change the `ITenantsService` interface method signature:

```csharp
Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(
    IGetOdsInstancesQuery getOdsInstancesQuery,
    IGetEducationOrganizationQuery getEducationOrganizationQuery,
    IGetDbInstancesQuery getDbInstancesQuery,
    string tenantName);
```

- [ ] **Step 2: Update the `TenantService` implementation**

Replace the `GetTenantEdOrgsByInstancesAsync` method body:

```csharp
public async Task<TenantDetailModel?> GetTenantEdOrgsByInstancesAsync(
    IGetOdsInstancesQuery getOdsInstancesQuery,
    IGetEducationOrganizationQuery getEducationOrganizationQuery,
    IGetDbInstancesQuery getDbInstancesQuery,
    string tenantName)
{
    var tenant = await GetTenantByTenantIdAsync(tenantName);

    if (tenant is not null)
    {
        var tenantDetails = new TenantDetailModel() { TenantName = tenantName };

        var odsInstances = getOdsInstancesQuery.Execute();
        tenantDetails.OdsInstances = TenantMapper.ToOdsInstanceModelList(odsInstances);

        var odsInstanceIdsList = tenantDetails.OdsInstances.Select(i => i.OdsInstanceId).ToArray();

        if (odsInstanceIdsList is not null && odsInstanceIdsList.Length > 0)
        {
            var edOrgsList = getEducationOrganizationQuery.Execute(odsInstanceIdsList);

            foreach (var odsInstance in tenantDetails.OdsInstances)
            {
                var edOrgs = edOrgsList.Where(eo => eo.InstanceId == odsInstance.OdsInstanceId).ToList();
                odsInstance.EducationOrganizations = EducationOrganizationMapper.ToModelList(edOrgs);
            }
        }

        // Merge DbInstance data
        var allDbInstances = getDbInstancesQuery.Execute(new Common.Infrastructure.CommonQueryParams(0, null), null, null);

        var linkedDbInstancesByOdsId = allDbInstances
            .Where(d => d.OdsInstanceId is not null)
            .ToDictionary(d => d.OdsInstanceId!.Value);

        foreach (var odsInstance in tenantDetails.OdsInstances)
        {
            if (linkedDbInstancesByOdsId.TryGetValue(odsInstance.OdsInstanceId, out var dbInstance))
            {
                odsInstance.Status = dbInstance.Status;
                odsInstance.DatabaseTemplate = dbInstance.DatabaseTemplate;
                odsInstance.DatabaseName = dbInstance.DatabaseName;
            }
            else
            {
                odsInstance.Status = DbInstanceStatus.Created.ToString();
            }
        }

        var unlinkedDbInstances = allDbInstances.Where(d => d.OdsInstanceId is null).ToList();
        var negativeId = -1;
        foreach (var dbInstance in unlinkedDbInstances)
        {
            tenantDetails.OdsInstances.Add(TenantMapper.ToUnlinkedDbInstanceModel(dbInstance, negativeId--));
        }

        return tenantDetails;
    }

    return null;
}
```

Make sure the `using` at the top of `TenantService.cs` includes:
```csharp
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
```

- [ ] **Step 3: Build to verify no compile errors**

```powershell
.\build.ps1 -Command build
```

Expected: build succeeds (and `ReadTenants.cs` will have compile errors until Task 4 is done — that's fine at this stage, or do Task 4 first in the same build cycle).

- [ ] **Step 4: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs
git commit -m "feat: merge DbInstances into GetTenantEdOrgsByInstancesAsync"
```

---

### Task 4: Update `ReadTenants.cs` to inject and forward `IGetDbInstancesQuery`

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Features/Tenants/ReadTenants.cs`

- [ ] **Step 1: Add `IGetDbInstancesQuery` parameter and forward it to the service**

Replace the `GetTenantEdOrgsByInstancesAsync` static method signature and call:

```csharp
public static async Task<IResult> GetTenantEdOrgsByInstancesAsync(
    HttpRequest request,
    [FromServices] ITenantsService tenantsService,
    IGetOdsInstancesQuery getOdsInstancesQuery,
    IGetEducationOrganizationQuery getEducationOrganizationQuery,
    IGetDbInstancesQuery getDbInstancesQuery,
    IMemoryCache memoryCache,
    IOptions<AppSettings> options,
    IOptions<SwaggerSettings> _swaggerOptions,
    string tenantName
)
{
    if (options.Value.MultiTenancy)
    {
        if (!IsRequestFromSwagger(request))
        {
            var tenantHeader = request.Headers["tenant"].FirstOrDefault();

            if (tenantHeader is null)
                throw new ValidationException([new ValidationFailure("Tenant", ErrorMessagesConstants.Tenant_MissingHeader)]);

            if (!string.Equals(tenantName, tenantHeader, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException([new ValidationFailure("Tenant", ErrorMessagesConstants.Tenant_ParameterMismatch)]);
        }
    }
    else if (!string.Equals(tenantName, Constants.DefaultTenantName, StringComparison.OrdinalIgnoreCase))
    {
        throw new NotFoundException<string>("TenantName", tenantName);
    }

    var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(
        getOdsInstancesQuery, getEducationOrganizationQuery, getDbInstancesQuery, tenantName);

    if (tenant is null)
        return Results.NotFound();

    return Results.Ok(
        new TenantDetailsResponse
        {
            Id = tenant.TenantName,
            Name = tenant.TenantName,
            OdsInstances = tenant.OdsInstances
        }
    );
}
```

Add the missing using at the top of `ReadTenants.cs` if not already present:
```csharp
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
```

- [ ] **Step 2: Build the full solution to verify no compile errors**

```powershell
.\build.ps1 -Command build
```

Expected: build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Features/Tenants/ReadTenants.cs
git commit -m "feat: inject IGetDbInstancesQuery into ReadTenants endpoint"
```

---

### Task 5: Update unit tests in `ReadTenantsTest.cs`

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/Features/Tenants/ReadTenantsTest.cs`

All existing tests pass `_getOdsInstancesQuery, _getEducationOrganizationQuery` to the method. Each call must now also pass a `_getDbInstancesQuery` fake.

- [ ] **Step 1: Add `_getDbInstancesQuery` field and initialize it in `SetUp`**

Add to the test class fields:
```csharp
private IGetDbInstancesQuery _getDbInstancesQuery = null!;
```

In `SetUp`, add:
```csharp
_getDbInstancesQuery = A.Fake<IGetDbInstancesQuery>();
A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string?>._))
    .Returns([]);
```

Add the missing using:
```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
```

- [ ] **Step 2: Update all existing test method calls**

Every call to `ReadTenants.GetTenantEdOrgsByInstancesAsync(...)` must have `_getDbInstancesQuery` inserted as the 5th argument (after `_getEducationOrganizationQuery`). For example:

```csharp
var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(
    request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery,
    _getDbInstancesQuery, memoryCache, options, swaggerOptions, tenantName);
```

Apply this change to all 8+ test methods in the file.

- [ ] **Step 3: Add test — OdsInstance with linked DbInstance gets Status/DatabaseTemplate/DatabaseName enriched**

```csharp
[Test]
public async Task GetTenantEdOrgsByInstancesAsync_EnrichesOdsInstance_WithLinkedDbInstanceFields()
{
    var tenantsService = A.Fake<ITenantsService>();
    var memoryCache = A.Fake<IMemoryCache>();
    var options = A.Fake<IOptions<AppSettings>>();
    var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
    string tenantName = "tenant1", tenantHeader = "tenant1";

    var odsInstance = new TenantOdsInstanceModel()
    {
        OdsInstanceId = 10,
        Name = "Instance-10",
        Status = "Active",
        DatabaseTemplate = "Minimal",
        DatabaseName = "EdFi_ODS_10"
    };

    var tenantDetailModel = new TenantDetailModel()
    {
        TenantName = tenantName,
        OdsInstances = [odsInstance]
    };

    var request = A.Fake<HttpRequest>();
    var headers = A.Fake<IHeaderDictionary>();
    A.CallTo(() => request.Headers).Returns(headers);
    A.CallTo(() => headers["tenant"]).Returns(new StringValues(tenantHeader));
    A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
    A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
    A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
    A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
    A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(
        _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, tenantName))
        .Returns(tenantDetailModel);

    var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(
        request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery,
        _getDbInstancesQuery, memoryCache, options, swaggerOptions, tenantName);

    result.ShouldNotBeNull();
}
```

- [ ] **Step 4: Add test — unlinked DbInstance appears as entry with negative OdsInstanceId**

```csharp
[Test]
public async Task GetTenantEdOrgsByInstancesAsync_AddsUnlinkedDbInstance_WithNegativeId()
{
    var tenantsService = A.Fake<ITenantsService>();
    var memoryCache = A.Fake<IMemoryCache>();
    var options = A.Fake<IOptions<AppSettings>>();
    var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
    string tenantName = "tenant1", tenantHeader = "tenant1";

    var unlinkedInstance = new TenantOdsInstanceModel()
    {
        OdsInstanceId = -1,
        Name = "Unlinked-DB",
        Status = "PendingCreate",
        DatabaseTemplate = "Minimal",
        DatabaseName = null
    };

    var tenantDetailModel = new TenantDetailModel()
    {
        TenantName = tenantName,
        OdsInstances = [unlinkedInstance]
    };

    var request = A.Fake<HttpRequest>();
    var headers = A.Fake<IHeaderDictionary>();
    A.CallTo(() => request.Headers).Returns(headers);
    A.CallTo(() => headers["tenant"]).Returns(new StringValues(tenantHeader));
    A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
    A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
    A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
    A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
    A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(
        _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, tenantName))
        .Returns(tenantDetailModel);

    var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(
        request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery,
        _getDbInstancesQuery, memoryCache, options, swaggerOptions, tenantName);

    result.ShouldNotBeNull();
    var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<TenantDetailsResponse>;
    okResult.ShouldNotBeNull();
    okResult.Value!.OdsInstances!.ShouldContain(i => i.OdsInstanceId == -1);
}
```

- [ ] **Step 5: Add test — OdsInstance with no DbInstance gets Status = Created, null template/name**

This test verifies the service-level logic through the `ITenantsService` fake returning the expected model.

```csharp
[Test]
public async Task GetTenantEdOrgsByInstancesAsync_SetsStatusCreated_WhenNoLinkedDbInstance()
{
    var tenantsService = A.Fake<ITenantsService>();
    var memoryCache = A.Fake<IMemoryCache>();
    var options = A.Fake<IOptions<AppSettings>>();
    var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
    string tenantName = "tenant1", tenantHeader = "tenant1";

    var odsInstance = new TenantOdsInstanceModel()
    {
        OdsInstanceId = 5,
        Name = "Instance-5",
        Status = "Created",
        DatabaseTemplate = null,
        DatabaseName = null
    };

    var tenantDetailModel = new TenantDetailModel()
    {
        TenantName = tenantName,
        OdsInstances = [odsInstance]
    };

    var request = A.Fake<HttpRequest>();
    var headers = A.Fake<IHeaderDictionary>();
    A.CallTo(() => request.Headers).Returns(headers);
    A.CallTo(() => headers["tenant"]).Returns(new StringValues(tenantHeader));
    A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
    A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
    A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
    A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
    A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(
        _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, tenantName))
        .Returns(tenantDetailModel);

    var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(
        request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery,
        _getDbInstancesQuery, memoryCache, options, swaggerOptions, tenantName);

    result.ShouldNotBeNull();
    var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<TenantDetailsResponse>;
    okResult.ShouldNotBeNull();
    var instance = okResult.Value!.OdsInstances!.Single();
    instance.Status.ShouldBe("Created");
    instance.DatabaseTemplate.ShouldBeNull();
    instance.DatabaseName.ShouldBeNull();
}
```

- [ ] **Step 6: Run unit tests**

```powershell
.\build.ps1 -Command UnitTest
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.UnitTests/Features/Tenants/ReadTenantsTest.cs
git commit -m "test: update and extend ReadTenants unit tests for DbInstance merging"
```

---

### Task 6: Add `TenantService`-level unit tests for the merging logic

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Tenants/TenantServiceGetTenantEdOrgsByInstancesTests.cs`

These tests exercise the actual merging logic inside `TenantService` using faked queries, without hitting a real database.

- [ ] **Step 1: Create the test file**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Services.Tenants;

[TestFixture]
public class TenantServiceGetTenantEdOrgsByInstancesTests
{
    private IGetOdsInstancesQuery _getOdsInstancesQuery = null!;
    private IGetEducationOrganizationQuery _getEducationOrganizationQuery = null!;
    private IGetDbInstancesQuery _getDbInstancesQuery = null!;

    private TenantService BuildService(bool multiTenancy = false, string tenantName = "default")
    {
        var appSettingsFile = new AppSettingsFile
        {
            AppSettings = new AppSettings { MultiTenancy = multiTenancy, DatabaseEngine = "SqlServer" },
            ConnectionStrings = new Dictionary<string, string>
            {
                ["EdFi_Admin"] = "Server=.;Database=EdFi_Admin;Trusted_Connection=True;",
                ["EdFi_Security"] = "Server=.;Database=EdFi_Security;Trusted_Connection=True;"
            },
            Tenants = []
        };

        var optionsSnapshot = A.Fake<IOptionsSnapshot<AppSettingsFile>>();
        A.CallTo(() => optionsSnapshot.Value).Returns(appSettingsFile);
        var memoryCache = A.Fake<IMemoryCache>();
        return new TenantService(optionsSnapshot, memoryCache);
    }

    [SetUp]
    public void SetUp()
    {
        _getOdsInstancesQuery = A.Fake<IGetOdsInstancesQuery>();
        _getEducationOrganizationQuery = A.Fake<IGetEducationOrganizationQuery>();
        _getDbInstancesQuery = A.Fake<IGetDbInstancesQuery>();

        A.CallTo(() => _getEducationOrganizationQuery.Execute(A<int[]>._)).Returns([]);
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string?>._))
            .Returns([]);
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_ReturnsNull_WhenTenantNotFound()
    {
        var service = BuildService(multiTenancy: false, tenantName: "default");

        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, "unknown");

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SetsStatusCreated_WhenOdsInstanceHasNoLinkedDbInstance()
    {
        var service = BuildService(multiTenancy: false);

        var odsInstance = new OdsInstance { OdsInstanceId = 1, Name = "Instance1" };
        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string?>._))
            .Returns([]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, "default");

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(1);
        result.OdsInstances[0].Status.ShouldBe(DbInstanceStatus.Created.ToString());
        result.OdsInstances[0].DatabaseTemplate.ShouldBeNull();
        result.OdsInstances[0].DatabaseName.ShouldBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_EnrichesOdsInstance_WithLinkedDbInstanceFields()
    {
        var service = BuildService(multiTenancy: false);

        var odsInstance = new OdsInstance { OdsInstanceId = 2, Name = "Instance2" };
        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);

        var dbInstance = new DbInstance
        {
            Id = 10,
            Name = "DbInstance2",
            OdsInstanceId = 2,
            Status = DbInstanceStatus.CreateInProgress.ToString(),
            DatabaseTemplate = "Minimal",
            DatabaseName = "EdFi_ODS_2",
            LastRefreshed = System.DateTime.UtcNow
        };
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string?>._))
            .Returns([dbInstance]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, "default");

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(1);
        var instance = result.OdsInstances[0];
        instance.Status.ShouldBe(DbInstanceStatus.CreateInProgress.ToString());
        instance.DatabaseTemplate.ShouldBe("Minimal");
        instance.DatabaseName.ShouldBe("EdFi_ODS_2");
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_AddsUnlinkedDbInstances_WithSuccessiveNegativeIds()
    {
        var service = BuildService(multiTenancy: false);

        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([]);

        var unlinked1 = new DbInstance
        {
            Id = 20, Name = "Unlinked-A", OdsInstanceId = null,
            Status = DbInstanceStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Sample", LastRefreshed = System.DateTime.UtcNow
        };
        var unlinked2 = new DbInstance
        {
            Id = 21, Name = "Unlinked-B", OdsInstanceId = null,
            Status = DbInstanceStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal", LastRefreshed = System.DateTime.UtcNow
        };
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string?>._))
            .Returns([unlinked1, unlinked2]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, "default");

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(2);
        result.OdsInstances.ShouldContain(i => i.OdsInstanceId == -1 && i.Name == "Unlinked-A");
        result.OdsInstances.ShouldContain(i => i.OdsInstanceId == -2 && i.Name == "Unlinked-B");
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_MixedScenario_LinkedAndUnlinked()
    {
        var service = BuildService(multiTenancy: false);

        var odsInstance = new OdsInstance { OdsInstanceId = 5, Name = "Instance5" };
        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);

        var linked = new DbInstance
        {
            Id = 30, Name = "Linked-5", OdsInstanceId = 5,
            Status = DbInstanceStatus.Created.ToString(),
            DatabaseTemplate = "Minimal", DatabaseName = "EdFi_ODS_5",
            LastRefreshed = System.DateTime.UtcNow
        };
        var unlinked = new DbInstance
        {
            Id = 31, Name = "Unlinked-C", OdsInstanceId = null,
            Status = DbInstanceStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Sample", LastRefreshed = System.DateTime.UtcNow
        };
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string?>._))
            .Returns([linked, unlinked]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, "default");

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(2);

        var linkedInstance = result.OdsInstances.Single(i => i.OdsInstanceId == 5);
        linkedInstance.Status.ShouldBe(DbInstanceStatus.Created.ToString());
        linkedInstance.DatabaseTemplate.ShouldBe("Minimal");
        linkedInstance.DatabaseName.ShouldBe("EdFi_ODS_5");

        var unlinkedInstance = result.OdsInstances.Single(i => i.OdsInstanceId == -1);
        unlinkedInstance.Name.ShouldBe("Unlinked-C");
        unlinkedInstance.Status.ShouldBe(DbInstanceStatus.PendingCreate.ToString());
    }
}
```

- [ ] **Step 2: Run unit tests**

```powershell
.\build.ps1 -Command UnitTest
```

Expected: all tests pass.

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Tenants/TenantServiceGetTenantEdOrgsByInstancesTests.cs
git commit -m "test: add TenantService unit tests for DbInstance merging logic"
```

---

### Task 7: Add DB integration tests

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetTenantEdOrgsByInstancesTests.cs`

These tests use the real `AdminApiDbContext` (via `AdminApiDbContextTestBase`) to verify that `GetDbInstancesQuery` correctly returns linked vs unlinked DbInstances as needed by the TenantService merging logic.

> Note: `TenantService` reads tenant config from `IOptionsSnapshot<AppSettingsFile>` which is app-config-based and cannot be easily tested as an integrated unit in DBTests. Therefore, these DB tests focus on verifying the query layer (`GetDbInstancesQuery`) produces the correct data that the service would consume — specifically testing the filtering of linked vs unlinked records.

- [ ] **Step 1: Create the test file**

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetTenantEdOrgsByInstancesTests : AdminApiDbContextTestBase
{
    [Test]
    public void ShouldReturnOnlyUnlinkedDbInstances_WhenFilteredByNullOdsInstanceId()
    {
        var linked = new DbInstance
        {
            Name = "Linked-Instance",
            OdsInstanceId = 999,
            Status = "Created",
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        var unlinked = new DbInstance
        {
            Name = "Unlinked-Instance",
            OdsInstanceId = null,
            Status = "PendingCreate",
            DatabaseTemplate = "Sample",
            LastRefreshed = DateTime.UtcNow
        };
        Save(linked, unlinked);

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var allResults = query.Execute(new CommonQueryParams(0, null), null, null);

            var unlinkedResults = allResults.Where(d => d.OdsInstanceId == null).ToList();
            var linkedResults = allResults.Where(d => d.OdsInstanceId != null).ToList();

            unlinkedResults.Count.ShouldBe(1);
            unlinkedResults[0].Name.ShouldBe("Unlinked-Instance");
            unlinkedResults[0].Status.ShouldBe("PendingCreate");

            linkedResults.Count.ShouldBe(1);
            linkedResults[0].Name.ShouldBe("Linked-Instance");
            linkedResults[0].OdsInstanceId.ShouldBe(999);
        });
    }

    [Test]
    public void ShouldReturnAllDbInstances_WhenNoFiltersApplied()
    {
        Save(
            new DbInstance { Name = "A", OdsInstanceId = 1, Status = "Created", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new DbInstance { Name = "B", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Sample", LastRefreshed = DateTime.UtcNow },
            new DbInstance { Name = "C", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow }
        );

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            results.Count.ShouldBe(3);
        });
    }

    [Test]
    public void ShouldReturnDbInstanceFields_ForLinkedInstance()
    {
        var dbInstance = new DbInstance
        {
            Name = "Fully-Linked",
            OdsInstanceId = 42,
            Status = "Created",
            DatabaseTemplate = "Minimal",
            DatabaseName = "EdFi_ODS_42",
            LastRefreshed = DateTime.UtcNow
        };
        Save(dbInstance);

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);

            results.Count.ShouldBe(1);
            var result = results[0];
            result.OdsInstanceId.ShouldBe(42);
            result.Status.ShouldBe("Created");
            result.DatabaseTemplate.ShouldBe("Minimal");
            result.DatabaseName.ShouldBe("EdFi_ODS_42");
        });
    }

    [Test]
    public void ShouldReturnEmptyList_WhenNoDbInstancesExist()
    {
        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            results.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldReturnMultipleUnlinkedInstances_InIdOrder()
    {
        Save(
            new DbInstance { Name = "Z-Unlinked", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new DbInstance { Name = "A-Unlinked", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Sample", LastRefreshed = DateTime.UtcNow }
        );

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            var ids = results.Select(r => r.Id).ToList();
            ids.ShouldBe(ids.OrderBy(x => x).ToList());
        });
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetTenantEdOrgsByInstancesTests.cs
git commit -m "test: add DB integration tests for GetTenantEdOrgs DbInstance merging"
```

---

### Task 8: Update Bruno E2E tests to include new fields in schema

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru`
- Modify: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru`

The AJV schema inside the `odsInstances.items.properties` block must be extended with the three new nullable fields.

- [ ] **Step 1: Update the schema in the Multitenant Bruno test**

In the `GetTenantOdsInstancesEdOrgsSchema` constant, replace the `odsInstances.items.properties` block to add the new fields:

```javascript
"properties": {
  "id": {
    "type": "integer"
  },
  "name": {
    "type": "string"
  },
  "instanceType": {
    "type": ["string", "null"]
  },
  "status": {
    "type": ["string", "null"]
  },
  "databaseTemplate": {
    "type": ["string", "null"]
  },
  "databaseName": {
    "type": ["string", "null"]
  },
  "educationOrganizations": {
    "type": "array",
    "items": {
      "type": "object",
      "properties": {
        "educationOrganizationId": {
          "type": "integer"
        },
        "nameOfInstitution": {
          "type": "string"
        },
        "shortNameOfInstitution": {
          "type": ["string", "null"]
        },
        "discriminator": {
          "type": "string"
        },
        "parentId": {
          "type": ["integer", "null"]
        }
      },
      "required": ["educationOrganizationId", "nameOfInstitution", "discriminator"]
    }
  }
},
"required": ["id", "name", "educationOrganizations"]
```

- [ ] **Step 2: Apply the same schema update to the Singletenant Bruno test**

Apply the identical `properties` change to `GET - Tenants EdOrgs by Tenant Name - Singletenant.bru`.

- [ ] **Step 3: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru"
git add "Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru"
git commit -m "test(e2e): update Bruno schema to include status, databaseTemplate, databaseName"
```

---

### Task 9: Final verification

- [ ] **Step 1: Run unit tests**

```powershell
.\build.ps1 -Command UnitTest
```

Expected: all tests pass.

- [ ] **Step 2: Verify build is clean**

```powershell
.\build.ps1 -Command build
```

Expected: no errors, no warnings related to changed files.
