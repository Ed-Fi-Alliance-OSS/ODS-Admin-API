# Vendor Coverage Boost Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Push vendor-related unit test coverage from ~76% to ≥80% line and branch in both `EdFi.Ods.AdminApi.UnitTests` (v2) and `EdFi.Ods.AdminApi.V3.UnitTests` (v3).

**Architecture:** Add a shared `TestEndpointRouteBuilder` stub (per test project) so the `MapEndpoints` method of each Feature class can be invoked in a unit test without a full DI container. Extend existing test files with targeted branch-gap tests. No source files are modified; no new NuGet packages are added.

**Tech Stack:** C# / .NET 10, NUnit, Shouldly, FakeItEasy, EF Core InMemory — all already referenced in both test projects.

## Global Constraints

- No new NuGet packages — FakeItEasy, NUnit, Shouldly, EF Core InMemory are already referenced.
- No source file modifications (only test files are touched).
- Every test uses a fresh `UseInMemoryDatabase(Guid.NewGuid().ToString())` to avoid cross-test interference.
- Both v2 and v3 test projects receive identical changes; only namespaces differ.
- License header required on every new file (see existing files for the exact 4-line block).
- Run tests: `.\build.ps1 -Command UnitTest` from repo root.
- Run coverage: `.\build.ps1 -Command UnitTest -RunCoverageAnalysis` — report lands in `coveragereport/`.

---

## Task 1: `TestEndpointRouteBuilder` shared helper (v2 + v3)

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Helpers/TestEndpointRouteBuilder.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Helpers/TestEndpointRouteBuilder.cs`

**Interfaces:**
- Produces: `TestEndpointRouteBuilder` — a concrete `IEndpointRouteBuilder` with a real `DataSources` list. Used by Task 2.

- [ ] **Step 1: Create the v2 helper**

Create `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Helpers/TestEndpointRouteBuilder.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Helpers;

internal class TestEndpointRouteBuilder : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider { get; } = A.Fake<IServiceProvider>();
    public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
    public IApplicationBuilder CreateApplicationBuilder() => A.Fake<IApplicationBuilder>();
}
```

- [ ] **Step 2: Create the v3 helper**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Helpers/TestEndpointRouteBuilder.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Helpers;

internal class TestEndpointRouteBuilder : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider { get; } = A.Fake<IServiceProvider>();
    public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
    public IApplicationBuilder CreateApplicationBuilder() => A.Fake<IApplicationBuilder>();
}
```

- [ ] **Step 3: Verify both files compile**

Run: `.\build.ps1 -Command build`
Expected: Build succeeds with no errors. If `IEndpointRouteBuilder` or `EndpointDataSource` are not found, add `using Microsoft.AspNetCore.Routing;` — it is in `Microsoft.AspNetCore.App` framework which is already referenced.

- [ ] **Step 4: Commit**

```
git add Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Helpers/TestEndpointRouteBuilder.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Helpers/TestEndpointRouteBuilder.cs
git commit -m "[ADMINAPI-1370] Add TestEndpointRouteBuilder helper to v2 and v3 test projects"
```

---

## Task 2: `VendorFeatureEndpointTests` — MapEndpoints smoke tests (v2 + v3)

**Depends on:** Task 1

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/VendorFeatureEndpointTests.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/VendorFeatureEndpointTests.cs`

**Interfaces:**
- Consumes: `TestEndpointRouteBuilder` from Task 1.
- Covers: `AddVendor.MapEndpoints`, `DeleteVendor.MapEndpoints`, `EditVendor.MapEndpoints`, `ReadVendor.MapEndpoints` in both v2 and v3.

- [ ] **Step 1: Create the v2 endpoint tests**

Create `Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/VendorFeatureEndpointTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Features.Vendors;
using EdFi.Ods.AdminApi.UnitTests.Infrastructure.Helpers;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors;

[TestFixture]
public class VendorFeatureEndpointTests
{
    [Test]
    public void AddVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new AddVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void DeleteVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new DeleteVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void EditVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new EditVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void ReadVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new ReadVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }
}
```

- [ ] **Step 2: Create the v3 endpoint tests**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/VendorFeatureEndpointTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.Vendors;
using EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Helpers;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Vendors;

[TestFixture]
public class VendorFeatureEndpointTests
{
    [Test]
    public void AddVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new AddVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void DeleteVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new DeleteVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void EditVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new EditVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void ReadVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new ReadVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }
}
```

- [ ] **Step 3: Run these tests**

Run: `.\build.ps1 -Command UnitTest`
Expected: All 8 new tests pass (4 per project). If `AdminApiEndpointBuilder` requires additional services from `ServiceProvider` (e.g., a `RouteGroupBuilder` lookup), you will see a `NullReferenceException` or similar — extend `TestEndpointRouteBuilder.ServiceProvider` by setting up the specific service with FakeItEasy: `A.CallTo(() => ServiceProvider.GetService(typeof(T))).Returns(A.Fake<T>())`. Start with the minimal stub as-is and only extend if the test throws.

- [ ] **Step 4: Commit**

```
git add Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/VendorFeatureEndpointTests.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/VendorFeatureEndpointTests.cs
git commit -m "[ADMINAPI-1370] Add VendorFeatureEndpointTests to cover MapEndpoints in v2 and v3"
```

---

## Task 3: `EditVendorCommandTests` — null NamespacePrefixes branch (v2 + v3)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs`

**Interfaces:**
- Covers: `EditVendorCommand.Execute` branch where `NamespacePrefixes` is null, exercising the `NamespacePrefixes?.Split(",") ?? Enumerable.Empty<string>()` null-propagation path.

- [ ] **Step 1: Add the test to the v2 file**

Open `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs`.

Add the following test after the last existing `[Test]` method and before the `EditVendorModelStub` nested class:

```csharp
[Test]
public void Execute_WithNullNamespacePrefixes_ProducesEmptyNamespaceCollection()
{
    var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
        .UseInMemoryDatabase(databaseName: $"EditVendorCommand_{Guid.NewGuid()}")
        .Options;
    using var usersContext = new SqlServerUsersContext(contextOptions);

    var vendor = new Vendor
    {
        VendorName = "Namespace Test Vendor",
        VendorNamespacePrefixes =
        [
            new VendorNamespacePrefix { NamespacePrefix = "https://old.org/ns" }
        ],
        Users =
        [
            new User { FullName = "Contact", Email = "contact@example.org" }
        ]
    };
    usersContext.Vendors.Add(vendor);
    usersContext.SaveChanges();

    var command = new EditVendorCommand(usersContext);
    var model = new EditVendorModelStub
    {
        Id = vendor.VendorId,
        Company = "Namespace Test Vendor",
        NamespacePrefixes = null,
        ContactName = "Contact",
        ContactEmailAddress = "contact@example.org"
    };

    command.Execute(model);

    var persisted = usersContext.Vendors
        .Include(v => v.VendorNamespacePrefixes)
        .Single(v => v.VendorId == vendor.VendorId);

    persisted.VendorNamespacePrefixes.ShouldBeEmpty();
}
```

Ensure `using System.Linq;` and `using Microsoft.EntityFrameworkCore;` are already present at the top of the file (they are, from existing tests).

- [ ] **Step 2: Add the same test to the v3 file**

Open `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs`.

Apply the identical test insertion. The only difference is that v3 already uses namespace `EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands` and imports `EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands` — no additional imports needed. The test body is word-for-word identical to v2.

- [ ] **Step 3: Run the tests**

Run: `.\build.ps1 -Command UnitTest`
Expected: All existing tests plus the 2 new tests pass.

- [ ] **Step 4: Commit**

```
git add Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/EditVendorCommandTests.cs
git commit -m "[ADMINAPI-1370] Add null-NamespacePrefixes branch test to EditVendorCommandTests (v2 + v3)"
```

---

## Task 4: `VendorExtensionsTests` — null vendor branch (v2 + v3)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/VendorExtensionsTests.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/VendorExtensionsTests.cs`

**Interfaces:**
- Covers: `VendorExtensions.IsSystemReservedVendor(this Vendor vendor)` branch where `vendor` is null, exercising the `vendor?.VendorName` null-propagation path (returns false because `IsSystemReservedVendorName(null)` returns false).

- [ ] **Step 1: Add test to v2 file**

Open `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/VendorExtensionsTests.cs`.

Add the following test after `IsSystemReservedVendor_ReturnsExpectedValue_ForVendorInstance`:

```csharp
[Test]
public void IsSystemReservedVendor_ReturnsFalse_ForNullVendor()
{
    Vendor? nullVendor = null;
    VendorExtensions.IsSystemReservedVendor(nullVendor!).ShouldBeFalse();
}
```

Note: `nullVendor!` suppresses the nullable warning. The method signature is `IsSystemReservedVendor(this Vendor vendor)` (non-nullable `this`), but C# extension methods accept null at runtime. The `!` operator is needed because the parameter is declared non-nullable.

- [ ] **Step 2: Add the same test to v3 file**

Open `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/VendorExtensionsTests.cs`.

Apply the identical test. The `using EdFi.Admin.DataAccess.Models;` and `using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;` are already present at the top. Test body is word-for-word identical.

- [ ] **Step 3: Run the tests**

Run: `.\build.ps1 -Command UnitTest`
Expected: All existing tests plus the 2 new tests pass.

- [ ] **Step 4: Commit**

```
git add Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/VendorExtensionsTests.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/VendorExtensionsTests.cs
git commit -m "[ADMINAPI-1370] Add null-vendor branch test to VendorExtensionsTests (v2 + v3)"
```

---

## Task 5: `AddVendorTests` — null NamespacePrefixes validator branch (v2 + v3)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/AddVendorTests.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/AddVendorTests.cs`

**Interfaces:**
- Covers: `AddVendor.Validator.HaveACorrectLength` branch where `vendorNamespacePrefixes` is null. When null, `vendorNamespacePrefixes?.Split(",")` returns null, so `namespacePrefixes == null` evaluates to true and the method returns true early (the `|| !namespacePrefixes.Exists(...)` branch is never reached).

- [ ] **Step 1: Add test to v2 `AddVendorTests.cs`**

Open `Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/AddVendorTests.cs`.

Add after the last existing `[Test]` method, before the closing `}` of the class:

```csharp
[Test]
public void Validator_WithNullNamespacePrefixes_IsValid()
{
    var validator = new AddVendor.Validator();
    var request = new AddVendor.AddVendorRequest
    {
        Company = "Acme Vendor",
        NamespacePrefixes = null,
        ContactName = "Alice",
        ContactEmailAddress = "alice@acme.org"
    };

    var result = validator.Validate(request);

    result.IsValid.ShouldBeTrue();
}
```

The `using FluentValidation;` is already present in the file. `AddVendor.Validator` is a public nested class with a public `Validate` method (inherited from `AbstractValidator<T>`).

- [ ] **Step 2: Add test to v3 `AddVendorTests.cs`**

Open `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/AddVendorTests.cs`.

Apply the identical test. The file uses `using EdFi.Ods.AdminApi.V3.Features.Vendors;` (already present), so `AddVendor.Validator` resolves to the v3 version. Test body is word-for-word identical.

- [ ] **Step 3: Run the tests**

Run: `.\build.ps1 -Command UnitTest`
Expected: All existing tests plus the 2 new tests pass.

- [ ] **Step 4: Commit**

```
git add Application/EdFi.Ods.AdminApi.UnitTests/Features/Vendors/AddVendorTests.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Vendors/AddVendorTests.cs
git commit -m "[ADMINAPI-1370] Add null-NamespacePrefixes validator branch test to AddVendorTests (v2 + v3)"
```

---

## Task 6: `GetVendorsQueryTests` — SqlServer engine branch (v2 + v3)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs`

**Interfaces:**
- Covers: The ternary branches in `GetVendorsQuery._orderByColumnVendors` lambdas where `isSQLServerEngine = true` (e.g., `x => isSQLServerEngine ? EF.Functions.Collate(x.VendorName, ...) : x.VendorName`). These lambdas are only executed when `Execute(CommonQueryParams, ...)` is called with an `OrderBy` that maps to one of those entries. `VendorCompanyColumn = "Company"` (value of `nameof(VendorModel.Company)`).
- Note on `EF.Functions.Collate` with InMemory: EF Core 6+ InMemory provider treats `Collate` as a no-op (returns the operand unchanged). The test should pass without error in .NET 10 / EF Core 9.

- [ ] **Step 1: Add test to v2 `GetVendorsQueryTests.cs`**

Open `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs`.

Add after the last existing `[Test]` method:

```csharp
[Test]
public void Execute_WithSqlServerDatabaseEngine_ReturnsVendors()
{
    var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
        .UseInMemoryDatabase(databaseName: $"GetVendorsQuery_{Guid.NewGuid()}")
        .Options;
    using var usersContext = new SqlServerUsersContext(contextOptions);

    usersContext.Vendors.Add(new Vendor
    {
        VendorName = "Acme Corp",
        Users = new List<User> { new User { FullName = "Alice", Email = "alice@test.org" } }
    });
    usersContext.SaveChanges();

    var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer", DefaultPageSizeLimit = 25 });
    var query = new GetVendorsQuery(usersContext, options);

    var result = query.Execute(new CommonQueryParams(0, 25, "Company", null), null, null, null, null, null);

    result.ShouldNotBeEmpty();
    result.Single().VendorName.ShouldBe("Acme Corp");
}
```

All required `using` directives (`System.Linq`, `System.Collections.Generic`, `EdFi.Admin.DataAccess.Models`, `EdFi.Ods.AdminApi.Common.Infrastructure`, `EdFi.Ods.AdminApi.Common.Settings`, `Microsoft.Extensions.Options`) are already present in the file.

**If this test throws** `InvalidOperationException: The method 'Collate' is for use with Entity Framework Core only and has no in-memory implementation` — fall back to a parameterless execute that still exercises the SqlServer constructor path:

```csharp
// fallback (no OrderBy — uses VendorId default column, no Collate):
var result = query.Execute();
result.ShouldNotBeEmpty();
```

With the fallback, `isSQLServerEngine = true` is exercised in the constructor, but the ternary in the lambdas is not hit. Use the fallback only if the sorting variant throws.

- [ ] **Step 2: Add the same test to v3 `GetVendorsQueryTests.cs`**

Open `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs`.

Apply the identical test. The v3 file imports `EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries` (for `GetVendorsQuery`). The `CommonQueryParams`, `AppSettings`, `Options` are all from shared packages, so they resolve identically.

- [ ] **Step 3: Run the tests**

Run: `.\build.ps1 -Command UnitTest`
Expected: All existing tests plus the 2 new tests pass. If the SqlServer/Collate test throws, apply the fallback noted in Step 1.

- [ ] **Step 4: Commit**

```
git add Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetVendorsQueryTests.cs
git commit -m "[ADMINAPI-1370] Add SqlServer engine branch test to GetVendorsQueryTests (v2 + v3)"
```

---

## Task 7: `DeleteVendorCommandTests` — ApiClient cleanup branch (v2 + v3)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs`

**Interfaces:**
- Covers: `DeleteVendorCommand.Execute` lines 44–53 — the `if (_context.ApiClients.Any())` true branch, `ApiClients.AsEnumerable().SingleOrDefault(o => o.User?.UserId == user.UserId)`, the `if (apiClient != null)` true branch, and `ApiClients.Remove(apiClient)`.
- `ApiClient` is from `EdFi.Admin.DataAccess.Models`. The constructor `new ApiClient(true)` generates a key; `new ApiClient()` skips key generation. Use `new ApiClient(true)` to mirror production usage.
- The `User` navigation property on `ApiClient` links it to a `User` record.

- [ ] **Step 1: Add test to v2 `DeleteVendorCommandTests.cs`**

Open `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs`.

Add after the last existing `[Test]` method:

```csharp
[Test]
public void Execute_WithUserHavingApiClient_RemovesApiClientBeforeRemovingUser()
{
    var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
        .UseInMemoryDatabase(databaseName: $"DeleteVendorCommand_{Guid.NewGuid()}")
        .Options;
    using var usersContext = new SqlServerUsersContext(contextOptions);

    var user = new User { FullName = "Alice", Email = "alice@acme.org" };
    var vendor = new Vendor
    {
        VendorName = "Acme Vendor",
        Users = [user]
    };
    usersContext.Vendors.Add(vendor);
    usersContext.SaveChanges();

    var apiClient = new ApiClient(true) { Name = "TestClient", User = user };
    usersContext.ApiClients.Add(apiClient);
    usersContext.SaveChanges();

    var deleteApplicationCommand = A.Fake<IDeleteApplicationCommand>();
    var command = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

    command.Execute(vendor.VendorId);

    usersContext.Vendors.Any(v => v.VendorId == vendor.VendorId).ShouldBeFalse();
    usersContext.ApiClients.Any(c => c.ApiClientId == apiClient.ApiClientId).ShouldBeFalse();
    usersContext.Users.Any().ShouldBeFalse();
}
```

All required `using` directives (`EdFi.Admin.DataAccess.Models`, `FakeItEasy`) are already present in the file.

- [ ] **Step 2: Add the same test to v3 `DeleteVendorCommandTests.cs`**

Open `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs`.

Apply the identical test. The v3 file imports `EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands` (for `DeleteVendorCommand`) and `EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries` (for `IDeleteApplicationCommand`). No additional imports are needed; the EF Core and model imports are shared.

- [ ] **Step 3: Run the tests**

Run: `.\build.ps1 -Command UnitTest`
Expected: All existing tests plus the 2 new tests pass. If `ApiClient(true)` constructor fails (EF Core InMemory doesn't support auto-generated keys from the model), fall back to `new ApiClient { Name = "TestClient", User = user }` (key generation happens in the domain constructor, not EF).

- [ ] **Step 4: Commit**

```
git add Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/DeleteVendorCommandTests.cs
git commit -m "[ADMINAPI-1370] Add ApiClient cleanup branch test to DeleteVendorCommandTests (v2 + v3)"
```

---

## Task 8: Run coverage and verify ≥80%

**Depends on:** Tasks 1–7 all complete and all tests passing.

**Files:** No code changes — this is a verification step.

- [ ] **Step 1: Run full unit test suite with coverage**

```powershell
.\build.ps1 -Command UnitTest -RunCoverageAnalysis
```

Expected exit code: 0 (all tests pass).

- [ ] **Step 2: Open coverage report**

Open `coveragereport/index.html` in a browser. Navigate to the "Coverage" tab and filter by the following class names (check both v2 and v3 assemblies):

| Class | Target line% | Target branch% |
|---|---|---|
| `AddVendor` | ≥80% | ≥80% |
| `DeleteVendor` | ≥80% | ≥80% |
| `EditVendor` | ≥80% | ≥80% |
| `ReadVendor` | ≥80% | ≥80% |
| `AddVendorCommand` | ≥80% | ≥80% |
| `DeleteVendorCommand` | ≥80% | ≥80% |
| `EditVendorCommand` | ≥80% | ≥80% |
| `GetVendorsQuery` | ≥80% | ≥80% |
| `VendorExtensions` | ≥80% | ≥80% |
| `VendorMapper` | ≥80% | ≥80% |
| `VendorModel` | ≥80% | ≥80% |
| `GetVendorByIdQuery` | ≥80% | ≥80% |

- [ ] **Step 3: If any file is below 80%, identify the gap**

The `coveragereport/index.html` highlights uncovered lines in red. Click into the file and note the exact uncovered lines. Then add a targeted test covering that line/branch — use the same in-memory EF Core pattern as all other tests in this plan. Re-run Step 1 and Step 2 after each fix.

- [ ] **Step 4: Commit (if any fixes were needed in Step 3)**

```
git add <changed test files>
git commit -m "[ADMINAPI-1370] Fix remaining coverage gaps per coverlet report"
```

---

## Self-Review Checklist

- Spec SC-1 (EditVendorCommand 4 paths): covered by existing tests + Task 3 (null NamespacePrefixes). ✓
- Spec SC-2 (DeleteVendorCommand 2 paths): covered by existing tests + Task 7 (ApiClient). ✓
- Spec SC-3 (AddVendorCommand 3 paths): covered by existing tests (already added in prior effort). ✓
- Spec SC-4 (GetVendorsQuery 4 filter paths): covered by existing tests + Task 6 (SqlServer branch). ✓
- Spec SC-5 (VendorMapperTests): already complete (prior effort). ✓
- Spec SC-6 (≥80% line and branch): verified in Task 8. ✓
- Spec SC-7 (all tests pass without source changes): verified throughout — no source files modified. ✓
- MapEndpoints coverage (design decision Option A): covered by Tasks 1–2. ✓
- VendorExtensions null-vendor branch: covered by Task 4. ✓
- AddVendor.Validator null-namespace branch: covered by Task 5. ✓
