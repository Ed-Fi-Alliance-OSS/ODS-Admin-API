# DataStoreDerivative Connection String Encryption Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop `DataStoreDerivative.ConnectionString` from being stored and returned in plaintext by the V3 Admin API, matching the primary `DataStore`'s existing encrypt-on-write / never-return behavior.

**Architecture:** Mirror the primary `DataStore` pattern exactly: encrypt in the feature `Handle` before persisting (write path), drop `ConnectionString` from the response model/mapper entirely (read path), and lazily re-encrypt any legacy plaintext row the moment it's next read, via an extended `DataStoreEncryptionHelper` (backfill).

**Tech Stack:** .NET / ASP.NET Core minimal APIs, EF Core, FluentValidation, NUnit + Shouldly + FakeItEasy (unit tests), Bruno (E2E).

## Global Constraints

- Scope is V3 only (`Application/EdFi.Ods.AdminApi.V3`) — there is no `DataStoreDerivative` concept in V2.
- Reuse the existing `ISymmetricStringEncryptionProvider` singleton and `AppSettings.EncryptionKey` — do not introduce a new encryption abstraction.
- AC3 (legacy plaintext rows) is satisfied via lazy backfill-on-read only — no separate DB migration script.
- `DataStoreDerivativeModel` must have its `ConnectionString` property removed entirely, not mapped to `null`.
- Every read path that loads `OdsInstanceDerivative` entities must run the backfill: `GetDataStoreQuery` (detail), `GetDataStoreDerivativesQuery` (list), `GetDataStoreDerivativeByIdQuery` (by id).

Reference spec: `docs/superpowers/specs/2026-08-10-datastore-derivative-connectionstring-encryption-design.md`

---

## File Structure

**Modify:**
- `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/DataStoreEncryptionHelper.cs` — add derivative backfill method
- `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/AddDataStoreDerivative.cs` — encrypt on create
- `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/EditDataStoreDerivative.cs` — encrypt on update
- `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/DataStoreDerivativeModel.cs` — remove `ConnectionString`
- `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/DataStoreDerivativeMapper.cs` — remove mapping
- `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreQuery.cs` — backfill derivatives on detail read
- `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreDerivativesQuery.cs` — backfill on list read
- `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreDerivativeByIdQuery.cs` — backfill on by-id read
- 5 Bruno `.bru` files under `DataStoreDerivatives/` (schema cleanup)

**Test files (modify/create):**
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Helpers/DataStoreEncryptionHelperTests.cs` (extend)
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/AddDataStoreDerivativeHandlerTests.cs` (new)
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/EditDataStoreDerivativeHandlerTests.cs` (modify)
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/DataStoreDerivativeTests.cs` (modify)
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreQueryTests.cs` (extend)
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreDerivativesQueryTests.cs` (modify)
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreDerivativeByIdQueryTests.cs` (modify)
- `Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreDerivativeByIdQueryTests.cs` (modify)

---

### Task 1: Extend `DataStoreEncryptionHelper` for derivatives

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/DataStoreEncryptionHelper.cs`
- Test: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Helpers/DataStoreEncryptionHelperTests.cs`

**Interfaces:**
- Produces: `DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(List<OdsInstanceDerivative> derivatives, IUsersContext usersContext, ISymmetricStringEncryptionProvider encryptionProvider, string encryptionKey, string databaseEngine, CancellationToken cancellationToken = default) : Task` — used by Task 5, 6, 7.

- [ ] **Step 1: Write the failing tests**

Add to `DataStoreEncryptionHelperTests.cs`:

```csharp
[Test]
public async Task EncryptDerivativeConnectionStringsIfNeededAsync_WithMixedStrings_OnlyEncryptsPlaintextAndCallsSaveChangesAsync()
{
    var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
    var plaintextDerivative = new OdsInstanceDerivative { DerivativeType = "ReadReplica", ConnectionString = PlainConnectionString };
    var encryptedDerivative = new OdsInstanceDerivative { DerivativeType = "Snapshot", ConnectionString = encrypted };
    var usersContext = A.Fake<IUsersContext>();

    await DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
        new List<OdsInstanceDerivative> { plaintextDerivative, encryptedDerivative }, usersContext, _provider, TestEncryptionKey, "SqlServer");

    _provider.IsEncrypted(plaintextDerivative.ConnectionString).ShouldBeTrue();
    encryptedDerivative.ConnectionString.ShouldBe(encrypted);
    A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
}

[Test]
public async Task EncryptDerivativeConnectionStringsIfNeededAsync_WithInvalidConnectionString_SkipsEncryptionAndDoesNotCallSaveChangesAsync()
{
    // PlainConnectionString is SqlServer format; using PostgreSql engine makes it invalid
    var derivative = new OdsInstanceDerivative { DerivativeType = "ReadReplica", ConnectionString = PlainConnectionString };
    var usersContext = A.Fake<IUsersContext>();

    await DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
        new List<OdsInstanceDerivative> { derivative }, usersContext, _provider, TestEncryptionKey, "PostgreSql");

    derivative.ConnectionString.ShouldBe(PlainConnectionString);
    A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
}

[Test]
public async Task EncryptDerivativeConnectionStringsIfNeededAsync_WithEmptyList_DoesNotCallSaveChangesAsync()
{
    var usersContext = A.Fake<IUsersContext>();

    await DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
        new List<OdsInstanceDerivative>(), usersContext, _provider, TestEncryptionKey, "SqlServer");

    A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
}

[Test]
public async Task EncryptDerivativeConnectionStringsIfNeededAsync_WithAlreadyEncryptedString_DoesNotCallSaveChangesAsync()
{
    var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
    var derivative = new OdsInstanceDerivative { DerivativeType = "ReadReplica", ConnectionString = encrypted };
    var usersContext = A.Fake<IUsersContext>();

    await DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
        new List<OdsInstanceDerivative> { derivative }, usersContext, _provider, TestEncryptionKey, "SqlServer");

    derivative.ConnectionString.ShouldBe(encrypted);
    A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter EncryptDerivativeConnectionStringsIfNeededAsync`
Expected: FAIL/build error — `EncryptDerivativeConnectionStringsIfNeededAsync` does not exist on `DataStoreEncryptionHelper`.

- [ ] **Step 3: Write minimal implementation**

Add to `DataStoreEncryptionHelper.cs` (below the existing `EncryptConnectionStringsIfNeededAsync` method, inside the same `static class`):

```csharp
    public static async Task EncryptDerivativeConnectionStringsIfNeededAsync(
        List<OdsInstanceDerivative> derivatives,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        string encryptionKey,
        string databaseEngine,
        CancellationToken cancellationToken = default)
    {
        byte[] key = Convert.FromBase64String(encryptionKey);
        bool anyUpdated = false;

        foreach (var derivative in derivatives)
        {
            if (string.IsNullOrEmpty(derivative.ConnectionString))
                continue;

            if (encryptionProvider.IsEncrypted(derivative.ConnectionString))
                continue;

            if (!ConnectionStringHelper.ValidateConnectionString(databaseEngine, derivative.ConnectionString))
                continue;

            derivative.ConnectionString = encryptionProvider.Encrypt(derivative.ConnectionString, key);
            anyUpdated = true;
        }

        if (anyUpdated)
            await usersContext.SaveChangesAsync(cancellationToken);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter EncryptDerivativeConnectionStringsIfNeededAsync`
Expected: PASS (4 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/DataStoreEncryptionHelper.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Helpers/DataStoreEncryptionHelperTests.cs
git commit -m "[ADMINAPI-1482] Add derivative connection string backfill to DataStoreEncryptionHelper

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 2: Encrypt on create (`AddDataStoreDerivative`)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/AddDataStoreDerivative.cs`
- Test: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/AddDataStoreDerivativeHandlerTests.cs` (new file)

**Interfaces:**
- Consumes: `ISymmetricStringEncryptionProvider.Encrypt(string? value, byte[]? key) : string` (existing).
- Produces: `AddDataStoreDerivative.Handle(Validator, IAddDataStoreDerivativeCommand, ISymmetricStringEncryptionProvider, IOptions<AppSettings>, AddDataStoreDerivativeRequest, HttpContext) : Task<IResult>` — new signature, one extra param each for `ISymmetricStringEncryptionProvider` and `IOptions<AppSettings>` inserted right after the command param (matches `AddDataStore.Handle`'s parameter order).

- [ ] **Step 1: Write the failing test**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/AddDataStoreDerivativeHandlerTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreDerivatives;

[TestFixture]
public class AddDataStoreDerivativeHandlerTests
{
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "PostgreSql",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        });

    [Test]
    public async Task Handle_WithValidRequest_EncryptsConnectionStringBeforeExecute()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());
        var fakeAddCommand = A.Fake<IAddDataStoreDerivativeCommand>();
        var derivative = new OdsInstanceDerivative { OdsInstanceDerivativeId = 5, DerivativeType = "ReadReplica", OdsInstance = new OdsInstance { OdsInstanceId = 1 } };
        A.CallTo(() => fakeAddCommand.Execute(A<IAddDataStoreDerivativeModel>._)).Returns(derivative);
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        A.CallTo(() => fakeEncryption.Encrypt(A<string>._, A<byte[]>._)).Returns("encrypted");

        var validator = new AddDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, Options());
        var request = new AddDataStoreDerivative.AddDataStoreDerivativeRequest
        {
            DataStoreId = 1,
            DerivativeType = "ReadReplica",
            ConnectionString = "Host=localhost;Port=5432;Database=EdFi_ODS"
        };

        var fakeHttpContext = new DefaultHttpContext();
        fakeHttpContext.Request.Scheme = "https";
        fakeHttpContext.Request.Host = new HostString("localhost");

        var result = await AddDataStoreDerivative.Handle(validator, fakeAddCommand, fakeEncryption, Options(), request, fakeHttpContext);

        request.ConnectionString.ShouldBe("encrypted");
        A.CallTo(() => fakeAddCommand.Execute(A<IAddDataStoreDerivativeModel>.That.Matches(m => m.ConnectionString == "encrypted"))).MustHaveHappenedOnceExactly();
        result.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_WithNullEncryptionKey_ThrowsInvalidOperationException()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());
        var fakeAddCommand = A.Fake<IAddDataStoreDerivativeCommand>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        var optionsWithoutKey = Microsoft.Extensions.Options.Options.Create(new AppSettings { DatabaseEngine = "PostgreSql", EncryptionKey = null });

        var validator = new AddDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, optionsWithoutKey);
        var request = new AddDataStoreDerivative.AddDataStoreDerivativeRequest
        {
            DataStoreId = 1,
            DerivativeType = "ReadReplica",
            ConnectionString = "Host=localhost;Port=5432;Database=EdFi_ODS"
        };
        var fakeHttpContext = new DefaultHttpContext();

        await Should.ThrowAsync<InvalidOperationException>(
            () => AddDataStoreDerivative.Handle(validator, fakeAddCommand, fakeEncryption, optionsWithoutKey, request, fakeHttpContext));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter AddDataStoreDerivativeHandlerTests`
Expected: FAIL/build error — `AddDataStoreDerivative.Handle` does not accept an `ISymmetricStringEncryptionProvider`/`IOptions<AppSettings>` overload.

- [ ] **Step 3: Write minimal implementation**

In `AddDataStoreDerivative.cs`, add the missing using and update `Handle`:

```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
```

```csharp
    public static async Task<IResult> Handle(
        Validator validator,
        IAddDataStoreDerivativeCommand addDataStoreDerivativeCommand,
        ISymmetricStringEncryptionProvider encryptionProvider,
        IOptions<AppSettings> options,
        AddDataStoreDerivativeRequest request,
        HttpContext httpContext)
    {
        await validator.GuardAsync(request);
        string encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        request.ConnectionString = encryptionProvider.Encrypt(request.ConnectionString, Convert.FromBase64String(encryptionKey));
        var added = addDataStoreDerivativeCommand.Execute(request);
        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStoreDerivatives/{added.OdsInstanceDerivativeId}");
        return Results.Created(absoluteLocation, null);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter AddDataStoreDerivativeHandlerTests`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/AddDataStoreDerivative.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/AddDataStoreDerivativeHandlerTests.cs
git commit -m "[ADMINAPI-1482] Encrypt DataStoreDerivative connection string on create

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 3: Encrypt on update (`EditDataStoreDerivative`)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/EditDataStoreDerivative.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/EditDataStoreDerivativeHandlerTests.cs`

**Interfaces:**
- Produces: `EditDataStoreDerivative.Handle(Validator, IEditDataStoreDerivativeCommand, ISymmetricStringEncryptionProvider, IOptions<AppSettings>, EditDataStoreDerivativeRequest, int) : Task<IResult>` — new signature, same insertion point as Task 2.

- [ ] **Step 1: Write the failing test**

Replace the existing `Handle_WithValidRequest_ReturnsNoContent` test body in `EditDataStoreDerivativeHandlerTests.cs` and add a new key-guard test:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreDerivatives;

[TestFixture]
public class EditDataStoreDerivativeHandlerTests
{
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "PostgreSql",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        });

    [Test]
    public async Task Handle_WithValidRequest_EncryptsConnectionStringAndReturnsNoContent()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());
        var fakeEditCommand = A.Fake<IEditDataStoreDerivativeCommand>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        A.CallTo(() => fakeEncryption.Encrypt(A<string>._, A<byte[]>._)).Returns("encrypted");

        var validator = new EditDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, Options());
        var request = new EditDataStoreDerivative.EditDataStoreDerivativeRequest
        {
            Id = 1, DataStoreId = 1, DerivativeType = "ReadReplica", ConnectionString = "Host=localhost;Port=5432;Database=EdFi"
        };

        var result = await EditDataStoreDerivative.Handle(validator, fakeEditCommand, fakeEncryption, Options(), request, 1);

        request.ConnectionString.ShouldBe("encrypted");
        A.CallTo(() => fakeEditCommand.Execute(A<IEditDataStoreDerivativeModel>.That.Matches(m => m.ConnectionString == "encrypted"))).MustHaveHappenedOnceExactly();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }

    [Test]
    public async Task Handle_WithNullEncryptionKey_ThrowsInvalidOperationException()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());
        var fakeEditCommand = A.Fake<IEditDataStoreDerivativeCommand>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        var optionsWithoutKey = Microsoft.Extensions.Options.Options.Create(new AppSettings { DatabaseEngine = "PostgreSql", EncryptionKey = null });

        var validator = new EditDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, optionsWithoutKey);
        var request = new EditDataStoreDerivative.EditDataStoreDerivativeRequest
        {
            Id = 1, DataStoreId = 1, DerivativeType = "ReadReplica", ConnectionString = "Host=localhost;Port=5432;Database=EdFi"
        };

        await Should.ThrowAsync<InvalidOperationException>(
            () => EditDataStoreDerivative.Handle(validator, fakeEditCommand, fakeEncryption, optionsWithoutKey, request, 1));
    }

    [Test]
    public async Task Validator_WhenDerivativeTypeEmpty_FailsValidation()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());

        var validator = new EditDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, Options());
        var result = await validator.ValidateAsync(new EditDataStoreDerivative.EditDataStoreDerivativeRequest { Id = 1, DataStoreId = 1, DerivativeType = "" });
        result.IsValid.ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter EditDataStoreDerivativeHandlerTests`
Expected: FAIL/build error — `EditDataStoreDerivative.Handle` doesn't accept the new parameters yet.

- [ ] **Step 3: Write minimal implementation**

In `EditDataStoreDerivative.cs`, add the missing using and update `Handle`:

```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
```

```csharp
    public static async Task<IResult> Handle(
        Validator validator,
        IEditDataStoreDerivativeCommand editDataStoreDerivativeCommand,
        ISymmetricStringEncryptionProvider encryptionProvider,
        IOptions<AppSettings> options,
        EditDataStoreDerivativeRequest request,
        int id)
    {
        ValidatorExtensions.GuardRouteIdMatchesBodyId(id, request.Id, nameof(request.Id));
        request.Id = id;
        await validator.GuardAsync(request);
        string encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        request.ConnectionString = encryptionProvider.Encrypt(request.ConnectionString, Convert.FromBase64String(encryptionKey));
        editDataStoreDerivativeCommand.Execute(request);
        return Results.NoContent();
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter EditDataStoreDerivativeHandlerTests`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/EditDataStoreDerivative.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/EditDataStoreDerivativeHandlerTests.cs
git commit -m "[ADMINAPI-1482] Encrypt DataStoreDerivative connection string on update

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 4: Remove `ConnectionString` from the response model and mapper

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/DataStoreDerivativeModel.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/DataStoreDerivativeMapper.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/DataStoreDerivativeTests.cs`

**Interfaces:**
- Produces: `DataStoreDerivativeModel` with no `ConnectionString` property; `DataStoreDerivativeMapper.ToModel`/`ToModelList` unchanged in signature, just stop copying `ConnectionString`.

- [ ] **Step 1: Write the failing test**

In `DataStoreDerivativeTests.cs`, replace `ToModel_MapsDerivativeFieldsAndParentDataStoreId`:

```csharp
    [Test]
    public void ToModel_MapsDerivativeFieldsAndParentDataStoreId_WithoutConnectionString()
    {
        var source = new DbOdsInstanceDerivative
        {
            OdsInstanceDerivativeId = 22,
            DerivativeType = "ReadReplica",
            ConnectionString = "encrypted",
            OdsInstance = new OdsInstance { OdsInstanceId = 99 }
        };

        var model = DataStoreDerivativeMapper.ToModel(source);

        model.DataStoreDerivativeId.ShouldBe(22);
        model.DataStoreId.ShouldBe(99);
        model.DerivativeType.ShouldBe("ReadReplica");
        model.GetType().GetProperty("ConnectionString").ShouldBeNull();
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter ToModel_MapsDerivativeFieldsAndParentDataStoreId_WithoutConnectionString`
Expected: FAIL — `model.GetType().GetProperty("ConnectionString")` is not null because the property still exists.

- [ ] **Step 3: Write minimal implementation**

`DataStoreDerivativeModel.cs`:

```csharp
[SwaggerSchema(Title = "DataStoreDerivative")]
public class DataStoreDerivativeModel
{
    [JsonPropertyName("id")]
    public int DataStoreDerivativeId { get; set; }
    public int DataStoreId { get; set; }
    public string? DerivativeType { get; set; }
}
```

`DataStoreDerivativeMapper.cs`:

```csharp
    public static DataStoreDerivativeModel ToModel(DbOdsInstanceDerivative source)
    {
        return new DataStoreDerivativeModel
        {
            DataStoreDerivativeId = source.OdsInstanceDerivativeId,
            DataStoreId = source.OdsInstance?.OdsInstanceId ?? 0,
            DerivativeType = source.DerivativeType
        };
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter DataStoreDerivativeTests`
Expected: PASS (all 3 tests in the fixture)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/DataStoreDerivativeModel.cs Application/EdFi.Ods.AdminApi.V3/Features/DataStoreDerivatives/DataStoreDerivativeMapper.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/DataStoreDerivatives/DataStoreDerivativeTests.cs
git commit -m "[ADMINAPI-1482] Remove ConnectionString from DataStoreDerivative response model

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 5: Backfill on DataStore detail read (`GetDataStoreQuery`)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreQuery.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreQueryTests.cs`

**Interfaces:**
- Consumes: `DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync` (Task 1).
- No signature change to `IGetDataStoreQuery.Execute(int id)`.

- [ ] **Step 1: Write the failing test**

Add to `GetDataStoreQueryTests.cs`:

```csharp
    [Test]
    public void Execute_WithUnencryptedDerivativeConnectionString_EncryptsOnRead()
    {
        using var usersContext = CreateContext();
        var odsInstance = new OdsInstance { Name = "Test", InstanceType = "type", ConnectionString = PlainConnectionString };
        usersContext.OdsInstances.Add(odsInstance);
        usersContext.SaveChanges();
        var derivative = new OdsInstanceDerivative { OdsInstance = odsInstance, DerivativeType = "ReadReplica", ConnectionString = PlainConnectionString };
        usersContext.OdsInstanceDerivatives.Add(derivative);
        usersContext.SaveChanges();

        var query = new GetDataStoreQuery(usersContext, _provider, OptionsWithKey(TestEncryptionKey));
        var result = query.Execute(odsInstance.OdsInstanceId);

        var resultDerivative = result.OdsInstanceDerivatives.Single();
        resultDerivative.ConnectionString.ShouldNotBe(PlainConnectionString);
        _provider.IsEncrypted(resultDerivative.ConnectionString).ShouldBeTrue();
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter Execute_WithUnencryptedDerivativeConnectionString_EncryptsOnRead`
Expected: FAIL — `resultDerivative.ConnectionString` is still plaintext because `GetDataStoreQuery` doesn't backfill derivatives yet.

- [ ] **Step 3: Write minimal implementation**

In `GetDataStoreQuery.cs`, update `Execute`:

```csharp
    public OdsInstance Execute(int id)
    {
        var dataStore = _usersContext.OdsInstances
            .Include(p => p.OdsInstanceContexts)
            .Include(p => p.OdsInstanceDerivatives)
            .SingleOrDefault(o => o.OdsInstanceId == id)
            ?? throw new NotFoundException<int>("DataStore", id);

        if (!string.IsNullOrEmpty(_options.Value.EncryptionKey) && !string.IsNullOrEmpty(_options.Value.DatabaseEngine))
        {
            DataStoreEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
                new List<OdsInstance> { dataStore }, _usersContext, _encryptionProvider, _options.Value.EncryptionKey, _options.Value.DatabaseEngine).GetAwaiter().GetResult();
            DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
                dataStore.OdsInstanceDerivatives.ToList(), _usersContext, _encryptionProvider, _options.Value.EncryptionKey, _options.Value.DatabaseEngine).GetAwaiter().GetResult();
        }

        return dataStore;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter GetDataStoreQueryTests`
Expected: PASS (all tests in the fixture, including the new one)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreQuery.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreQueryTests.cs
git commit -m "[ADMINAPI-1482] Backfill-encrypt derivative connection strings on DataStore detail read

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 6: Backfill on derivative list read (`GetDataStoreDerivativesQuery`)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreDerivativesQuery.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreDerivativesQueryTests.cs`

**Interfaces:**
- Produces: `GetDataStoreDerivativesQuery(IUsersContext, IOptions<AppSettings>, ISymmetricStringEncryptionProvider)` — new constructor signature (third param added, matching `GetDataStoresQuery`'s existing `(userContext, options, encryptionProvider)` order).

- [ ] **Step 1: Write the failing test**

Replace the constructor call and add a backfill test in `GetDataStoreDerivativesQueryTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDataStoreDerivativesQueryTests
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";
    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();

    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreDerivatives_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
    private IOptions<AppSettings> OptionsWithEncryption() =>
        Options.Create(new AppSettings { DatabaseEngine = "SqlServer", DefaultPageSizeLimit = 25, EncryptionKey = TestEncryptionKey });

    [Test]
    public void Execute_ReturnsList()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        ctx.OdsInstanceDerivatives.Add(new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = "cs-replica" });
        ctx.SaveChanges();
        var result = new GetDataStoreDerivativesQuery(ctx, DefaultOptions(), _provider).Execute(new CommonQueryParams(0, 25));
        result.Count.ShouldBe(1);
    }

    [Test]
    public void Execute_WithUnencryptedConnectionString_EncryptsOnRead()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        ctx.OdsInstanceDerivatives.Add(new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = PlainConnectionString });
        ctx.SaveChanges();

        var result = new GetDataStoreDerivativesQuery(ctx, OptionsWithEncryption(), _provider).Execute(new CommonQueryParams(0, 25));

        _provider.IsEncrypted(result.Single().ConnectionString).ShouldBeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter GetDataStoreDerivativesQueryTests`
Expected: FAIL/build error — `GetDataStoreDerivativesQuery` has no 3-argument constructor yet.

- [ ] **Step 3: Write minimal implementation**

In `GetDataStoreDerivativesQuery.cs`, add the missing using and update the class:

```csharp
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
```

```csharp
public class GetDataStoreDerivativesQuery : IGetDataStoreDerivativesQuery
{
    private readonly IUsersContext _usersContext;
    private readonly IOptions<AppSettings> _options;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider;
    private readonly Dictionary<string, Expression<Func<OdsInstanceDerivative, object>>> _orderByColumnOds;
    public GetDataStoreDerivativesQuery(IUsersContext usersContext, IOptions<AppSettings> options, ISymmetricStringEncryptionProvider encryptionProvider)
    {
        _usersContext = usersContext;
        _options = options;
        _encryptionProvider = encryptionProvider;
        var DatabaseEngine = _options.Value.DatabaseEngine ??= DatabaseEngineEnum.SqlServer;
        var isSQLServerEngine = DatabaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase);
        _orderByColumnOds = new Dictionary<string, Expression<Func<OdsInstanceDerivative, object>>>
            (StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DataStoreDerivativeTypeColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.DerivativeType, DatabaseEngineEnum.SqlServerCollation) : x.DerivativeType },
            { SortingColumns.DataStoreDerivativeDataStoreIdColumn, x => x.OdsInstance.OdsInstanceId },
            { SortingColumns.DefaultIdColumn, x => x.OdsInstanceDerivativeId }
        };
    }

    public List<OdsInstanceDerivative> Execute()
    {
        var derivatives = _usersContext.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .OrderBy(p => p.DerivativeType)
            .ToList();
        EncryptIfNeeded(derivatives);
        return derivatives;
    }

    public List<OdsInstanceDerivative> Execute(CommonQueryParams commonQueryParams)
    {
        Expression<Func<OdsInstanceDerivative, object>> columnToOrderBy = _orderByColumnOds.GetColumnToOrderBy(commonQueryParams.OrderBy);

        var derivatives = _usersContext.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
        EncryptIfNeeded(derivatives);
        return derivatives;
    }

    private void EncryptIfNeeded(List<OdsInstanceDerivative> derivatives)
    {
        if (!string.IsNullOrEmpty(_options.Value.EncryptionKey) && !string.IsNullOrEmpty(_options.Value.DatabaseEngine))
            DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
                derivatives, _usersContext, _encryptionProvider, _options.Value.EncryptionKey, _options.Value.DatabaseEngine).GetAwaiter().GetResult();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter GetDataStoreDerivativesQueryTests`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreDerivativesQuery.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreDerivativesQueryTests.cs
git commit -m "[ADMINAPI-1482] Backfill-encrypt derivative connection strings on list read

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 7: Backfill on derivative by-id read (`GetDataStoreDerivativeByIdQuery`)

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreDerivativeByIdQuery.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreDerivativeByIdQueryTests.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreDerivativeByIdQueryTests.cs`

**Interfaces:**
- Produces: `GetDataStoreDerivativeByIdQuery(IUsersContext, ISymmetricStringEncryptionProvider, IOptions<AppSettings>)` — new constructor signature (matching `GetDataStoreQuery`'s existing `(context, encryptionProvider, options)` order).

- [ ] **Step 1: Write the failing tests**

Replace `GetDataStoreDerivativeByIdQueryTests.cs` (UnitTests project) in full:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDataStoreDerivativeByIdQueryTests
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";
    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();

    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreDerivativeById_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> OptionsWithKey(string? key = null) =>
        Options.Create(new AppSettings { EncryptionKey = key, DatabaseEngine = "SqlServer" });

    [Test]
    public void Execute_WhenExists_ReturnsDataStoreDerivative()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        var d = new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = "cs-replica" };
        ctx.OdsInstanceDerivatives.Add(d);
        ctx.SaveChanges();
        var result = new GetDataStoreDerivativeByIdQuery(ctx, _provider, OptionsWithKey()).Execute(d.OdsInstanceDerivativeId);
        result.ShouldNotBeNull();
        result.DerivativeType.ShouldBe("read-replica");
    }

    [Test]
    public void Execute_WithUnencryptedConnectionString_EncryptsOnRead()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        var d = new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = PlainConnectionString };
        ctx.OdsInstanceDerivatives.Add(d);
        ctx.SaveChanges();

        var result = new GetDataStoreDerivativeByIdQuery(ctx, _provider, OptionsWithKey(TestEncryptionKey)).Execute(d.OdsInstanceDerivativeId);

        _provider.IsEncrypted(result.ConnectionString).ShouldBeTrue();
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetDataStoreDerivativeByIdQuery(ctx, _provider, OptionsWithKey()).Execute(9999));
    }
}
```

Replace the constructor call in `Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreDerivativeByIdQueryTests.cs`:

```csharp
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;
```

```csharp
            var query = new GetDataStoreDerivativeByIdQuery(usersContext, new Aes256SymmetricStringEncryptionProvider(), Testing.GetAppSettings());
```

(`Testing.GetAppSettings()` leaves `EncryptionKey` unset, so backfill is skipped and the existing `result.ConnectionString.ShouldBe(odsInstanceDerivative1.ConnectionString)` assertion keeps holding — same no-op guard used by `GetDataStoresQuery`'s existing DBTests.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter GetDataStoreDerivativeByIdQueryTests`
Expected: FAIL/build error — `GetDataStoreDerivativeByIdQuery` has no 3-argument constructor yet.

- [ ] **Step 3: Write minimal implementation**

Replace `GetDataStoreDerivativeByIdQuery.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDataStoreDerivativeByIdQuery
{
    OdsInstanceDerivative Execute(int dataStoreDerivativeId);
}

public class GetDataStoreDerivativeByIdQuery : IGetDataStoreDerivativeByIdQuery
{
    private readonly IUsersContext _context;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider;
    private readonly IOptions<AppSettings> _options;

    public GetDataStoreDerivativeByIdQuery(IUsersContext context, ISymmetricStringEncryptionProvider encryptionProvider, IOptions<AppSettings> options)
    {
        _context = context;
        _encryptionProvider = encryptionProvider;
        _options = options;
    }

    public OdsInstanceDerivative Execute(int dataStoreDerivativeId)
    {
        var odsInstanceDerivative = _context.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(app => app.OdsInstanceDerivativeId == dataStoreDerivativeId);
        if (odsInstanceDerivative == null)
        {
            throw new NotFoundException<int>("DataStoreDerivative", dataStoreDerivativeId);
        }

        if (!string.IsNullOrEmpty(_options.Value.EncryptionKey) && !string.IsNullOrEmpty(_options.Value.DatabaseEngine))
            DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
                new List<OdsInstanceDerivative> { odsInstanceDerivative }, _context, _encryptionProvider, _options.Value.EncryptionKey, _options.Value.DatabaseEngine).GetAwaiter().GetResult();

        return odsInstanceDerivative;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `./build.ps1 -Command UnitTest -NoBuild -Filter GetDataStoreDerivativeByIdQueryTests`
Expected: PASS (3 tests)

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetDataStoreDerivativeByIdQuery.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetDataStoreDerivativeByIdQueryTests.cs Application/EdFi.Ods.AdminApi.V3.DBTests/Database/QueryTests/GetDataStoreDerivativeByIdQueryTests.cs
git commit -m "[ADMINAPI-1482] Backfill-encrypt derivative connection strings on by-id read

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 8: Clean up stale `connectionString` schema assertions in Bruno E2E tests

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives.bru`
- Modify: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives - Without Offset.bru`
- Modify: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives - Without Limit.bru`
- Modify: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives - Without Offset and Limit.bru`
- Modify: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives by ID.bru`

**Note:** These schemas don't list `connectionString` under `required` and have no `additionalProperties: false`, so ajv validation would still pass today even without this change — this task is a documentation-accuracy cleanup (the schema should reflect that the field is now permanently absent), not a fix for a failing E2E run. The two `DataStores/GET - DataStores by ID*.bru` files use a generic `"items": {}` for `dataStoreDerivatives` and need no change.

- [ ] **Step 1: Update the 4 list-endpoint `.bru` files**

In each of `GET - DataStoreDerivatives.bru`, `GET - DataStoreDerivatives - Without Offset.bru`, `GET - DataStoreDerivatives - Without Limit.bru`, `GET - DataStoreDerivatives - Without Offset and Limit.bru`, remove the `connectionString` property block from `GetDataStoreDerivativesSchema`:

```javascript
  const GetDataStoreDerivativesSchema = {
    "type": "array",
    "items": [
      {
        "type": "object",
        "properties": {
          "id": {
            "type": "integer"
          },
          "derivativeType": {
            "type": "string"
          },
          "dataStoreId": {
            "type": "integer"
          },
        },
        "required": [
          "id",
          "derivativeType",
          "dataStoreId"     
        ]
      }
    ]
  }
```

- [ ] **Step 2: Update `GET - DataStoreDerivatives by ID.bru`**

Remove the `connectionString` property block from `GetDataStoreDerivativesSchema`:

```javascript
  const GetDataStoreDerivativesSchema = {
    "type": "object",
        "properties": {
          "id": {
            "type": "integer"
          },
          "derivativeType": {
            "type": "string"
          },
          "dataStoreId": {
            "type": "integer"
          },
        },
        "required": [
          "id",
          "derivativeType",
          "dataStoreId"     
        ]
  }
```

- [ ] **Step 3: Run the full unit test suite as a final sanity check**

Run: `./build.ps1 -Command UnitTest -NoBuild`
Expected: PASS — Bruno changes don't affect the .NET build/unit tests; this confirms nothing else regressed before wrapping up.

- [ ] **Step 4: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives - Without Offset.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives - Without Limit.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives - Without Offset and Limit.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/DataStoreDerivatives/GET - DataStoreDerivatives by ID.bru"
git commit -m "[ADMINAPI-1482] Remove stale connectionString schema assertions from DataStoreDerivatives E2E tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```
