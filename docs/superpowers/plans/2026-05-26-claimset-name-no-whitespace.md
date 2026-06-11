# ClaimSet Name No-Whitespace Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce that ClaimSet names and ClaimSetName fields on V3 API endpoints must not contain white spaces; reject with HTTP 400 if they do.

**Architecture:** Add a `ClaimSetNameNoWhitespaceMessage` constant to `FeatureConstants.cs`, then add a single FluentValidation `Must` rule to the `Validator` class inside each of the 6 affected endpoint classes. Unit tests use FakeItEasy for DB-dependent mocks; DB integration tests use real `SecurityDataTestBase`-backed query instances; E2E tests are new Bruno `.bru` files.

**Tech Stack:** C# / .NET, FluentValidation, NUnit, Shouldly, FakeItEasy, Bruno (E2E tests)

---

## File Map

### Modified files
- `Application/EdFi.Ods.AdminApi.V3/Features/FeatureConstants.cs` — add constant
- `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/AddClaimSet.cs` — add rule
- `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/EditClaimSet.cs` — add rule
- `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/ImportClaimSet.cs` — add rule
- `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/CopyClaimSet.cs` — add rule
- `Application/EdFi.Ods.AdminApi.V3/Features/Applications/AddApplication.cs` — add rule
- `Application/EdFi.Ods.AdminApi.V3/Features/Applications/EditApplication.cs` — add rule

### New test files (unit)
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/AddClaimSetValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/EditClaimSetValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/ImportClaimSetValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/CopyClaimSetValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/AddApplicationValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/EditApplicationValidatorTests.cs`

### New test files (DB integration)
- `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/AddClaimSetValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/EditClaimSetValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/CopyClaimSetValidatorTests.cs`
- `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/ImportClaimSetValidatorTests.cs`

### New E2E files (Bruno)
- `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Invalid Whitespace Name.bru`
- `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/PUT - ClaimSets - Invalid Whitespace Name.bru`
- `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets-Copy - Invalid Whitespace Name.bru`
- `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Import - Invalid Whitespace Name.bru`
- `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/POST - Applications - Invalid Whitespace ClaimSetName.bru`
- `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/PUT - Applications - Invalid Whitespace ClaimSetName.bru`

---

## Task 1: Add the no-whitespace constant to FeatureConstants.cs

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/FeatureConstants.cs`

- [ ] **Step 1: Add the constant**

Open `Application/EdFi.Ods.AdminApi.V3/Features/FeatureConstants.cs`.

After the line:
```csharp
public const string ClaimSetNameMaxLengthMessage = "The claim set name must be less than 255 characters.";
```

Add:
```csharp
public const string ClaimSetNameNoWhitespaceMessage = "Claim set name must not contain white spaces.";
```

- [ ] **Step 2: Verify it builds**

```powershell
cd Application/EdFi.Ods.AdminApi.V3
dotnet build --no-incremental -c Debug 2>&1 | Select-String -Pattern "error|warning" | Select-Object -First 20
```

Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/FeatureConstants.cs
git commit -m "feat: add ClaimSetNameNoWhitespaceMessage constant"
```

---

## Task 2: Add no-whitespace rule to AddClaimSet.Validator — TDD

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/AddClaimSet.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/AddClaimSetValidatorTests.cs`

- [ ] **Step 1: Write the failing unit test**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/AddClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets
{
    [TestFixture]
    public class AddClaimSetValidatorTests
    {
        private AddClaimSet.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            var fakeGetAllClaimSetsQuery = A.Fake<IGetAllClaimSetsQuery>();
            A.CallTo(() => fakeGetAllClaimSetsQuery.Execute())
                .Returns(new List<ClaimSet>());

            _validator = new AddClaimSet.Validator(fakeGetAllClaimSetsQuery);
        }

        [Test]
        public void Should_Have_Error_When_Name_Contains_Whitespace()
        {
            var request = new AddClaimSet.AddClaimSetRequest { Name = "claimset name" };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Has_No_Whitespace()
        {
            var request = new AddClaimSet.AddClaimSetRequest { Name = "claimsetname" };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }
    }
}
```

- [ ] **Step 2: Run test to confirm it fails**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "AddClaimSetValidatorTests" --no-build 2>&1 | Select-String -Pattern "Failed|Error|Passed"
```

Expected: test fails (rule does not exist yet).

- [ ] **Step 3: Add the rule to AddClaimSet.Validator**

Open `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/AddClaimSet.cs`.

In the `Validator` constructor, after the `MaximumLength(255)` rule block, add:

```csharp
RuleFor(m => m.Name)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

The full constructor should look like:

```csharp
public Validator(IGetAllClaimSetsQuery getAllClaimSetsQuery)
{
    _getAllClaimSetsQuery = getAllClaimSetsQuery;

    RuleFor(m => m.Name).NotEmpty()
        .Must(BeAUniqueName)
        .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage);

    RuleFor(m => m.Name)
        .MaximumLength(255)
        .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);

    RuleFor(m => m.Name)
        .Must(name => name == null || !name.Contains(' '))
        .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
}
```

- [ ] **Step 4: Run test to confirm it passes**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "AddClaimSetValidatorTests" 2>&1 | Select-String -Pattern "Failed|Passed|Error"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/AddClaimSet.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/AddClaimSetValidatorTests.cs
git commit -m "feat: add no-whitespace validation to AddClaimSet"
```

---

## Task 3: Add no-whitespace rule to EditClaimSet.Validator — TDD

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/EditClaimSet.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/EditClaimSetValidatorTests.cs`

- [ ] **Step 1: Write the failing unit test**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/EditClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets
{
    [TestFixture]
    public class EditClaimSetValidatorTests
    {
        private EditClaimSet.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            var fakeGetClaimSetByIdQuery = A.Fake<IGetClaimSetByIdQuery>();
            A.CallTo(() => fakeGetClaimSetByIdQuery.Execute(A<int>.Ignored))
                .Returns(new ClaimSet { Id = 1, Name = "ExistingClaimSet" });

            var fakeGetAllClaimSetsQuery = A.Fake<IGetAllClaimSetsQuery>();
            A.CallTo(() => fakeGetAllClaimSetsQuery.Execute())
                .Returns(new List<ClaimSet> { new ClaimSet { Id = 1, Name = "ExistingClaimSet" } });

            _validator = new EditClaimSet.Validator(fakeGetClaimSetByIdQuery, fakeGetAllClaimSetsQuery);
        }

        [Test]
        public void Should_Have_Error_When_Name_Contains_Whitespace()
        {
            var request = new EditClaimSet.EditClaimSetRequest { Id = 1, Name = "claimset name" };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Has_No_Whitespace()
        {
            var request = new EditClaimSet.EditClaimSetRequest { Id = 1, Name = "claimsetname" };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }
    }
}
```

- [ ] **Step 2: Run test to confirm it fails**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "EditClaimSetValidatorTests" --no-build 2>&1 | Select-String -Pattern "Failed|Error|Passed"
```

Expected: test fails.

- [ ] **Step 3: Add the rule to EditClaimSet.Validator**

Open `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/EditClaimSet.cs`.

In the `Validator` constructor, after the `MaximumLength(255)` rule block, add:

```csharp
RuleFor(m => m.Name)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

The full constructor should look like:

```csharp
public Validator(IGetClaimSetByIdQuery getClaimSetByIdQuery,
    IGetAllClaimSetsQuery getAllClaimSetsQuery)
{
    _getClaimSetByIdQuery = getClaimSetByIdQuery;
    _getAllClaimSetsQuery = getAllClaimSetsQuery;

    RuleFor(m => m.Id).NotEmpty();

    RuleFor(m => m.Id)
        .Must(BeAnExistingClaimSet)
        .WithMessage(FeatureConstants.ClaimSetNotFound);

    RuleFor(m => m.Name)
    .NotEmpty()
    .Must(BeAUniqueName)
    .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage)
    .When(m => BeAnExistingClaimSet(m.Id) && NameIsChanged(m));

    RuleFor(m => m.Name)
        .MaximumLength(255)
        .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);

    RuleFor(m => m.Name)
        .Must(name => name == null || !name.Contains(' '))
        .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
}
```

- [ ] **Step 4: Run test to confirm it passes**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "EditClaimSetValidatorTests" 2>&1 | Select-String -Pattern "Failed|Passed|Error"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/EditClaimSet.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/EditClaimSetValidatorTests.cs
git commit -m "feat: add no-whitespace validation to EditClaimSet"
```

---

## Task 4: Add no-whitespace rule to ImportClaimSet.Validator — TDD

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/ImportClaimSet.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/ImportClaimSetValidatorTests.cs`

- [ ] **Step 1: Write the failing unit test**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/ImportClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets
{
    [TestFixture]
    public class ImportClaimSetValidatorTests
    {
        private ImportClaimSet.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            var fakeGetAllClaimSetsQuery = A.Fake<IGetAllClaimSetsQuery>();
            A.CallTo(() => fakeGetAllClaimSetsQuery.Execute())
                .Returns(new List<ClaimSet>());

            var fakeGetResourceClaimsAsFlatListQuery = A.Fake<IGetResourceClaimsAsFlatListQuery>();
            A.CallTo(() => fakeGetResourceClaimsAsFlatListQuery.Execute())
                .Returns(new List<ResourceClaim>());

            var fakeGetAllAuthorizationStrategiesQuery = A.Fake<IGetAllAuthorizationStrategiesQuery>();
            A.CallTo(() => fakeGetAllAuthorizationStrategiesQuery.Execute())
                .Returns(new List<AuthorizationStrategy>());

            var fakeGetAllActionsQuery = A.Fake<IGetAllActionsQuery>();
            A.CallTo(() => fakeGetAllActionsQuery.Execute())
                .Returns(new List<EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.Action>());

            _validator = new ImportClaimSet.Validator(
                fakeGetAllClaimSetsQuery,
                fakeGetResourceClaimsAsFlatListQuery,
                fakeGetAllAuthorizationStrategiesQuery,
                fakeGetAllActionsQuery);
        }

        [Test]
        public void Should_Have_Error_When_Name_Contains_Whitespace()
        {
            var request = new ImportClaimSet.ImportClaimSetRequest
            {
                Name = "claimset name",
                ResourceClaims = new List<ClaimSetResourceClaimModel>()
            };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Has_No_Whitespace()
        {
            var request = new ImportClaimSet.ImportClaimSetRequest
            {
                Name = "claimsetname",
                ResourceClaims = new List<ClaimSetResourceClaimModel>()
            };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }
    }
}
```

- [ ] **Step 2: Run test to confirm it fails**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "ImportClaimSetValidatorTests" --no-build 2>&1 | Select-String -Pattern "Failed|Error|Passed"
```

Expected: test fails.

- [ ] **Step 3: Add the rule to ImportClaimSet.Validator**

Open `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/ImportClaimSet.cs`.

In the `Validator` constructor, after the `MaximumLength(255)` rule block (before the `Custom` rule), add:

```csharp
RuleFor(m => m.Name)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

The constructor block for `Name` rules should look like:

```csharp
RuleFor(m => m.Name).NotEmpty()
    .Must(BeAUniqueName)
    .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage);

RuleFor(m => m.Name)
    .MaximumLength(255)
    .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);

RuleFor(m => m.Name)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);

RuleFor(m => m).Custom((claimSet, context) =>
{
    // ... existing custom rule unchanged
});
```

- [ ] **Step 4: Run test to confirm it passes**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "ImportClaimSetValidatorTests" 2>&1 | Select-String -Pattern "Failed|Passed|Error"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/ImportClaimSet.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/ImportClaimSetValidatorTests.cs
git commit -m "feat: add no-whitespace validation to ImportClaimSet"
```

---

## Task 5: Add no-whitespace rule to CopyClaimSet.Validator — TDD

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/CopyClaimSet.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/CopyClaimSetValidatorTests.cs`

- [ ] **Step 1: Write the failing unit test**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/CopyClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets
{
    [TestFixture]
    public class CopyClaimSetValidatorTests
    {
        private CopyClaimSet.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            var fakeGetAllClaimSetsQuery = A.Fake<IGetAllClaimSetsQuery>();
            A.CallTo(() => fakeGetAllClaimSetsQuery.Execute())
                .Returns(new List<ClaimSet>());

            var fakeGetClaimSetByIdQuery = A.Fake<IGetClaimSetByIdQuery>();
            A.CallTo(() => fakeGetClaimSetByIdQuery.Execute(A<int>.Ignored))
                .Returns(new ClaimSet { Id = 1, Name = "OriginalClaimSet" });

            _validator = new CopyClaimSet.Validator(fakeGetAllClaimSetsQuery, fakeGetClaimSetByIdQuery);
        }

        [Test]
        public void Should_Have_Error_When_Name_Contains_Whitespace()
        {
            var request = new CopyClaimSet.CopyClaimSetRequest { OriginalId = 1, Name = "claimset name" };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Has_No_Whitespace()
        {
            var request = new CopyClaimSet.CopyClaimSetRequest { OriginalId = 1, Name = "claimsetname" };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }
    }
}
```

- [ ] **Step 2: Run test to confirm it fails**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "CopyClaimSetValidatorTests" --no-build 2>&1 | Select-String -Pattern "Failed|Error|Passed"
```

Expected: test fails.

- [ ] **Step 3: Add the rule to CopyClaimSet.Validator**

Open `Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/CopyClaimSet.cs`.

In the `Validator` constructor, after the `MaximumLength(255)` rule block, add:

```csharp
RuleFor(m => m.Name)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

The full constructor should look like:

```csharp
public Validator(IGetAllClaimSetsQuery getAllClaimSetsQuery,
    IGetClaimSetByIdQuery getClaimSetByIdQuery)
{
    _getAllClaimSetsQuery = getAllClaimSetsQuery;
    _getClaimSetByIdQuery = getClaimSetByIdQuery;

    RuleFor(m => m.OriginalId)
      .Must(BeAnExistingClaimSet)
      .WithMessage("No such claim set exists in the database.Please provide valid claimset id.");

    RuleFor(m => m.Name).NotEmpty()
        .Must(BeAUniqueName)
        .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage);

    RuleFor(m => m.Name)
        .MaximumLength(255)
        .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);

    RuleFor(m => m.Name)
        .Must(name => name == null || !name.Contains(' '))
        .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
}
```

- [ ] **Step 4: Run test to confirm it passes**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "CopyClaimSetValidatorTests" 2>&1 | Select-String -Pattern "Failed|Passed|Error"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/ClaimSets/CopyClaimSet.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/ClaimSets/CopyClaimSetValidatorTests.cs
git commit -m "feat: add no-whitespace validation to CopyClaimSet"
```

---

## Task 6: Add no-whitespace rule to AddApplication.Validator — TDD

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/Applications/AddApplication.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/AddApplicationValidatorTests.cs`

- [ ] **Step 1: Write the failing unit test**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/AddApplicationValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Applications
{
    [TestFixture]
    public class AddApplicationValidatorTests
    {
        private AddApplication.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new AddApplication.Validator();
        }

        [Test]
        public void Should_Have_Error_When_ClaimSetName_Contains_Whitespace()
        {
            var request = ValidRequest();
            request.ClaimSetName = "claimset name";

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ClaimSetName)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_ClaimSetName_Has_No_Whitespace()
        {
            var request = ValidRequest();
            request.ClaimSetName = "claimsetname";

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ClaimSetName)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }

        private static AddApplication.AddApplicationRequest ValidRequest()
        {
            return new AddApplication.AddApplicationRequest
            {
                ApplicationName = "Test Application",
                VendorId = 1,
                ClaimSetName = "TestClaimSet",
                EducationOrganizationIds = new long[] { 1L },
                DataStoreIds = new[] { 1 }
            };
        }
    }
}
```

- [ ] **Step 2: Run test to confirm it fails**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "AddApplicationValidatorTests" --no-build 2>&1 | Select-String -Pattern "Failed|Error|Passed"
```

Expected: test fails.

- [ ] **Step 3: Add the rule to AddApplication.Validator**

Open `Application/EdFi.Ods.AdminApi.V3/Features/Applications/AddApplication.cs`.

In the `Validator` constructor, after the existing `ClaimSetName` `NotEmpty` rule, add:

```csharp
RuleFor(m => m.ClaimSetName)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

The `ClaimSetName` rules should look like:

```csharp
RuleFor(m => m.ClaimSetName)
    .NotEmpty()
    .WithMessage(FeatureConstants.ClaimSetNameValidationMessage);

RuleFor(m => m.ClaimSetName)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

- [ ] **Step 4: Run test to confirm it passes**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "AddApplicationValidatorTests" 2>&1 | Select-String -Pattern "Failed|Passed|Error"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/Applications/AddApplication.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/AddApplicationValidatorTests.cs
git commit -m "feat: add no-whitespace validation to AddApplication"
```

---

## Task 7: Add no-whitespace rule to EditApplication.Validator — TDD

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/Applications/EditApplication.cs`
- Create: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/EditApplicationValidatorTests.cs`

- [ ] **Step 1: Write the failing unit test**

Create `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/EditApplicationValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Applications
{
    [TestFixture]
    public class EditApplicationValidatorTests
    {
        private EditApplication.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new EditApplication.Validator();
        }

        [Test]
        public void Should_Have_Error_When_ClaimSetName_Contains_Whitespace()
        {
            var request = ValidRequest();
            request.ClaimSetName = "claimset name";

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ClaimSetName)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_ClaimSetName_Has_No_Whitespace()
        {
            var request = ValidRequest();
            request.ClaimSetName = "claimsetname";

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ClaimSetName)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }

        private static EditApplication.EditApplicationRequest ValidRequest()
        {
            return new EditApplication.EditApplicationRequest
            {
                Id = 1,
                ApplicationName = "Test Application",
                VendorId = 1,
                ClaimSetName = "TestClaimSet",
                EducationOrganizationIds = new long[] { 1L },
                DataStoreIds = new[] { 1 }
            };
        }
    }
}
```

- [ ] **Step 2: Run test to confirm it fails**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "EditApplicationValidatorTests" --no-build 2>&1 | Select-String -Pattern "Failed|Error|Passed"
```

Expected: test fails.

- [ ] **Step 3: Add the rule to EditApplication.Validator**

Open `Application/EdFi.Ods.AdminApi.V3/Features/Applications/EditApplication.cs`.

In the `Validator` constructor, after the existing `ClaimSetName` `NotEmpty` rule, add:

```csharp
RuleFor(m => m.ClaimSetName)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

The `ClaimSetName` rules should look like:

```csharp
RuleFor(m => m.ClaimSetName)
    .NotEmpty()
    .WithMessage(FeatureConstants.ClaimSetNameValidationMessage);

RuleFor(m => m.ClaimSetName)
    .Must(name => name == null || !name.Contains(' '))
    .WithMessage(FeatureConstants.ClaimSetNameNoWhitespaceMessage);
```

- [ ] **Step 4: Run test to confirm it passes**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test --filter "EditApplicationValidatorTests" 2>&1 | Select-String -Pattern "Failed|Passed|Error"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/Applications/EditApplication.cs
git add Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/EditApplicationValidatorTests.cs
git commit -m "feat: add no-whitespace validation to EditApplication"
```

---

## Task 8: Run full unit test suite

- [ ] **Step 1: Run all V3 unit tests**

```powershell
cd Application/EdFi.Ods.AdminApi.V3.UnitTests
dotnet test 2>&1 | Tail -20
```

Expected: All tests pass. Zero failures.

---

## Task 9: DB integration tests — AddClaimSet.Validator

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/AddClaimSetValidatorTests.cs`

- [ ] **Step 1: Create the DB integration test**

Create `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/AddClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.ClaimSetEditorTests;

[TestFixture]
public class AddClaimSetValidatorTests : SecurityDataTestBase
{
    [Test]
    public void ShouldFailValidation_WhenNameContainsWhitespace()
    {
        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(TestContext, options);
        var validator = new AddClaimSet.Validator(getAllClaimSetsQuery);

        var request = new AddClaimSet.AddClaimSetRequest { Name = "claimset name" };

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeTrue();
    }

    [Test]
    public void ShouldPassValidation_WhenNameHasNoWhitespace()
    {
        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(TestContext, options);
        var validator = new AddClaimSet.Validator(getAllClaimSetsQuery);

        var request = new AddClaimSet.AddClaimSetRequest { Name = "claimsetname" };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/AddClaimSetValidatorTests.cs
git commit -m "test: add DB integration tests for AddClaimSet no-whitespace validation"
```

---

## Task 10: DB integration tests — EditClaimSet.Validator

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/EditClaimSetValidatorTests.cs`

- [ ] **Step 1: Create the DB integration test**

Create `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/EditClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.DBTests.ClaimSetEditorTests;

[TestFixture]
public class EditClaimSetValidatorTests : SecurityDataTestBase
{
    [Test]
    public void ShouldFailValidation_WhenNameContainsWhitespace()
    {
        var existingClaimSet = new ClaimSet { ClaimSetName = "ExistingClaimSet" };
        Save(existingClaimSet);

        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(TestContext, options);
        var getClaimSetByIdQuery = new GetClaimSetByIdQuery(TestContext);
        var validator = new EditClaimSet.Validator(getClaimSetByIdQuery, getAllClaimSetsQuery);

        var request = new EditClaimSet.EditClaimSetRequest
        {
            Id = existingClaimSet.ClaimSetId,
            Name = "claimset name"
        };

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeTrue();
    }

    [Test]
    public void ShouldPassValidation_WhenNameHasNoWhitespace()
    {
        var existingClaimSet = new ClaimSet { ClaimSetName = "ExistingClaimSet2" };
        Save(existingClaimSet);

        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(TestContext, options);
        var getClaimSetByIdQuery = new GetClaimSetByIdQuery(TestContext);
        var validator = new EditClaimSet.Validator(getClaimSetByIdQuery, getAllClaimSetsQuery);

        var request = new EditClaimSet.EditClaimSetRequest
        {
            Id = existingClaimSet.ClaimSetId,
            Name = "claimsetnameedited"
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/EditClaimSetValidatorTests.cs
git commit -m "test: add DB integration tests for EditClaimSet no-whitespace validation"
```

---

## Task 11: DB integration tests — CopyClaimSet.Validator

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/CopyClaimSetValidatorTests.cs`

- [ ] **Step 1: Create the DB integration test**

Create `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/CopyClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.DBTests.ClaimSetEditorTests;

[TestFixture]
public class CopyClaimSetValidatorTests : SecurityDataTestBase
{
    [Test]
    public void ShouldFailValidation_WhenNameContainsWhitespace()
    {
        var originalClaimSet = new ClaimSet { ClaimSetName = "OriginalClaimSet" };
        Save(originalClaimSet);

        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(TestContext, options);
        var getClaimSetByIdQuery = new GetClaimSetByIdQuery(TestContext);
        var validator = new CopyClaimSet.Validator(getAllClaimSetsQuery, getClaimSetByIdQuery);

        var request = new CopyClaimSet.CopyClaimSetRequest
        {
            OriginalId = originalClaimSet.ClaimSetId,
            Name = "copied claimset name"
        };

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeTrue();
    }

    [Test]
    public void ShouldPassValidation_WhenNameHasNoWhitespace()
    {
        var originalClaimSet = new ClaimSet { ClaimSetName = "OriginalClaimSet2" };
        Save(originalClaimSet);

        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(TestContext, options);
        var getClaimSetByIdQuery = new GetClaimSetByIdQuery(TestContext);
        var validator = new CopyClaimSet.Validator(getAllClaimSetsQuery, getClaimSetByIdQuery);

        var request = new CopyClaimSet.CopyClaimSetRequest
        {
            OriginalId = originalClaimSet.ClaimSetId,
            Name = "CopiedClaimSetName"
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/CopyClaimSetValidatorTests.cs
git commit -m "test: add DB integration tests for CopyClaimSet no-whitespace validation"
```

---

## Task 12: DB integration tests — ImportClaimSet.Validator

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/ImportClaimSetValidatorTests.cs`

- [ ] **Step 1: Create the DB integration test**

Create `Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/ImportClaimSetValidatorTests.cs`:

```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.ClaimSetEditor;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.ClaimSetEditorTests;

[TestFixture]
public class ImportClaimSetValidatorTests : SecurityDataTestBase
{
    private ImportClaimSet.Validator CreateValidator()
    {
        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        return new ImportClaimSet.Validator(
            new GetAllClaimSetsQuery(TestContext, options),
            new GetResourceClaimsAsFlatListQuery(TestContext),
            new GetAllAuthorizationStrategiesQuery(TestContext),
            new GetAllActionsQuery(TestContext, options));
    }

    [Test]
    public void ShouldFailValidation_WhenNameContainsWhitespace()
    {
        var validator = CreateValidator();

        var request = new ImportClaimSet.ImportClaimSetRequest
        {
            Name = "claimset name",
            ResourceClaims = new List<ClaimSetResourceClaimModel>()
        };

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeTrue();
    }

    [Test]
    public void ShouldPassValidation_WhenNameHasNoWhitespace()
    {
        var validator = CreateValidator();

        var request = new ImportClaimSet.ImportClaimSetRequest
        {
            Name = "claimsetname",
            ResourceClaims = new List<ClaimSetResourceClaimModel>()
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
            .ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3.DBTests/ClaimSetEditorTests/ImportClaimSetValidatorTests.cs
git commit -m "test: add DB integration tests for ImportClaimSet no-whitespace validation"
```

---

## Task 13: Bruno E2E tests — ClaimSets (POST, PUT, Copy, Import)

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Invalid Whitespace Name.bru`
- Create: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/PUT - ClaimSets - Invalid Whitespace Name.bru`
- Create: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets-Copy - Invalid Whitespace Name.bru`
- Create: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Import - Invalid Whitespace Name.bru`

- [ ] **Step 1: Create POST ClaimSets — Invalid Whitespace Name**

Create `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Invalid Whitespace Name.bru`:

```
meta {
  name: ClaimSets - Invalid Whitespace Name
  type: http
  seq: 9.5
}

post {
  url: {{API_URL}}/v3/claimSets/
  body: json
  auth: inherit
}

body:json {
  {
      "name": "claimset name"
  }
  
}

script:post-response {
  test("POST ClaimSets Invalid Whitespace Name: Status code is Bad Request", function () {
      expect(res.getStatus()).to.equal(400);
  });
  
  const response = res.getBody();
  
  test("POST ClaimSets Invalid Whitespace Name: Response matches error format", function () {
      expect(response).to.have.property("title");
      expect(response).to.have.property("errors");
  });
  
  test("POST ClaimSets Invalid Whitespace Name: Response title is helpful and accurate", function () {
      expect(response.title.toLowerCase()).to.contain("validation");
  });
  
  test("POST ClaimSets Invalid Whitespace Name: Response errors include no-whitespace message", function () {
      expect(response.errors["Name"].length).to.be.greaterThan(0);
      expect(response.errors["Name"].some(m => m.toLowerCase().includes("white space"))).to.equal(true);
  });
  
}

settings {
  encodeUrl: true
}
```

- [ ] **Step 2: Create PUT ClaimSets — Invalid Whitespace Name**

Create `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/PUT - ClaimSets - Invalid Whitespace Name.bru`:

```
meta {
  name: ClaimSets - Invalid Whitespace Name
  type: http
  seq: 47.5
}

put {
  url: {{API_URL}}/v3/claimSets/{{CreatedClaimSetId}}
  body: json
  auth: inherit
}

body:json {
  {
      "id": {{CreatedClaimSetId}},
      "name": "claimset name"
  }
  
}

script:post-response {
  test("PUT ClaimSets Invalid Whitespace Name: Status code is Bad Request", function () {
      expect(res.getStatus()).to.equal(400);
  });
  
  const response = res.getBody();
  
  test("PUT ClaimSets Invalid Whitespace Name: Response matches error format", function () {
      expect(response).to.have.property("title");
      expect(response).to.have.property("errors");
  });
  
  test("PUT ClaimSets Invalid Whitespace Name: Response title is helpful and accurate", function () {
      expect(response.title.toLowerCase()).to.contain("validation");
  });
  
  test("PUT ClaimSets Invalid Whitespace Name: Response errors include no-whitespace message", function () {
      expect(response.errors["Name"].length).to.be.greaterThan(0);
      expect(response.errors["Name"].some(m => m.toLowerCase().includes("white space"))).to.equal(true);
  });
  
}

settings {
  encodeUrl: true
}
```

- [ ] **Step 3: Create POST ClaimSets-Copy — Invalid Whitespace Name**

Create `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets-Copy - Invalid Whitespace Name.bru`:

```
meta {
  name: ClaimSets/Copy - Invalid Whitespace Name
  type: http
  seq: 41.5
}

post {
  url: {{API_URL}}/v3/claimSets/copy
  body: json
  auth: inherit
}

body:json {
  {
      "name": "copied claimset name",
      "originalid": "{{CreatedClaimSetId}}"
  }
}

script:post-response {
  test("POST ClaimSets-Copy Invalid Whitespace Name: Status code is Bad Request", function () {
      expect(res.getStatus()).to.equal(400);
  });
  
  const response = res.getBody();
  
  test("POST ClaimSets-Copy Invalid Whitespace Name: Response matches error format", function () {
      expect(response).to.have.property("title");
      expect(response).to.have.property("errors");
  });
  
  test("POST ClaimSets-Copy Invalid Whitespace Name: Response title is helpful and accurate", function () {
      expect(response.title.toLowerCase()).to.contain("validation");
  });
  
  test("POST ClaimSets-Copy Invalid Whitespace Name: Response errors include no-whitespace message", function () {
      expect(response.errors["Name"].length).to.be.greaterThan(0);
      expect(response.errors["Name"].some(m => m.toLowerCase().includes("white space"))).to.equal(true);
  });
  
}

settings {
  encodeUrl: true
}
```

- [ ] **Step 4: Create POST ClaimSets — Import — Invalid Whitespace Name**

Create `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Import - Invalid Whitespace Name.bru`:

```
meta {
  name: ClaimSets - Import - Invalid Whitespace Name
  type: http
  seq: 8.5
}

post {
  url: {{API_URL}}/v3/claimSets/import
  body: json
  auth: inherit
}

body:json {
  {
      "name": "claimset name",
      "resourceClaims": []
  }
}

script:post-response {
  test("POST ClaimSets Import Invalid Whitespace Name: Status code is Bad Request", function () {
      expect(res.getStatus()).to.equal(400);
  });
  
  const response = res.getBody();
  
  test("POST ClaimSets Import Invalid Whitespace Name: Response matches error format", function () {
      expect(response).to.have.property("title");
      expect(response).to.have.property("errors");
  });
  
  test("POST ClaimSets Import Invalid Whitespace Name: Response title is helpful and accurate", function () {
      expect(response.title.toLowerCase()).to.contain("validation");
  });
  
  test("POST ClaimSets Import Invalid Whitespace Name: Response errors include no-whitespace message", function () {
      expect(response.errors["Name"].length).to.be.greaterThan(0);
      expect(response.errors["Name"].some(m => m.toLowerCase().includes("white space"))).to.equal(true);
  });
  
}

settings {
  encodeUrl: true
}
```

- [ ] **Step 5: Commit all ClaimSet Bruno files**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Invalid Whitespace Name.bru"
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/PUT - ClaimSets - Invalid Whitespace Name.bru"
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets-Copy - Invalid Whitespace Name.bru"
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/ClaimSets/POST - ClaimSets - Import - Invalid Whitespace Name.bru"
git commit -m "test: add Bruno E2E tests for ClaimSet no-whitespace validation"
```

---

## Task 14: Bruno E2E tests — Applications (POST and PUT)

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/POST - Applications - Invalid Whitespace ClaimSetName.bru`
- Create: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/PUT - Applications - Invalid Whitespace ClaimSetName.bru`

- [ ] **Step 1: Create POST Applications — Invalid Whitespace ClaimSetName**

Create `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/POST - Applications - Invalid Whitespace ClaimSetName.bru`:

```
meta {
  name: Applications - Invalid Whitespace ClaimSetName
  type: http
  seq: 3.5
}

post {
  url: {{API_URL}}/v3/applications/
  body: json
  auth: inherit
}

body:json {
  {
    "applicationName": "Test Application",
    "vendorId": {{ApplicationVendorId}},
    "claimSetName": "claimset name",
    "profileIds": [],
    "educationOrganizationIds": [1],
    "dataStoreIds": [{{ApplicationDataStoreId}}]
  }
  
}

script:post-response {
  test("POST Applications Invalid Whitespace ClaimSetName: Status code is Bad Request", function () {
      expect(res.getStatus()).to.equal(400);
  });
  
  const response = res.getBody();
  
  test("POST Applications Invalid Whitespace ClaimSetName: Response matches error format", function () {
      expect(response).to.have.property("title");
      expect(response).to.have.property("errors");
  });
  
  test("POST Applications Invalid Whitespace ClaimSetName: Response title is helpful and accurate", function () {
      expect(response.title.toLowerCase()).to.contain("validation");
  });
  
  test("POST Applications Invalid Whitespace ClaimSetName: Response errors include no-whitespace message for ClaimSetName", function () {
      expect(response.errors["ClaimSetName"].length).to.be.greaterThan(0);
      expect(response.errors["ClaimSetName"].some(m => m.toLowerCase().includes("white space"))).to.equal(true);
  });
  
}

settings {
  encodeUrl: true
}
```

- [ ] **Step 2: Create PUT Applications — Invalid Whitespace ClaimSetName**

Create `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/PUT - Applications - Invalid Whitespace ClaimSetName.bru`:

```
meta {
  name: Applications - Invalid Whitespace ClaimSetName
  type: http
  seq: 17.5
}

put {
  url: {{API_URL}}/v3/applications/{{CreatedApplicationId}}
  body: json
  auth: inherit
}

body:json {
  {
    "id": {{CreatedApplicationId}},
    "applicationName": "Test Application",
    "vendorId": {{OtherApplicationVendorId}},
    "claimSetName": "claimset name",
    "profileIds": [],
    "educationOrganizationIds": [1],
    "dataStoreIds": [{{ApplicationDataStoreId}}]
  }
  
}

script:post-response {
  test("PUT Applications Invalid Whitespace ClaimSetName: Status code is Bad Request", function () {
      expect(res.getStatus()).to.equal(400);
  });
  
  const response = res.getBody();
  
  test("PUT Applications Invalid Whitespace ClaimSetName: Response matches error format", function () {
      expect(response).to.have.property("title");
      expect(response).to.have.property("errors");
  });
  
  test("PUT Applications Invalid Whitespace ClaimSetName: Response title is helpful and accurate", function () {
      expect(response.title.toLowerCase()).to.contain("validation");
  });
  
  test("PUT Applications Invalid Whitespace ClaimSetName: Response errors include no-whitespace message for ClaimSetName", function () {
      expect(response.errors["ClaimSetName"].length).to.be.greaterThan(0);
      expect(response.errors["ClaimSetName"].some(m => m.toLowerCase().includes("white space"))).to.equal(true);
  });
  
}

settings {
  encodeUrl: true
}
```

- [ ] **Step 3: Commit all Application Bruno files**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/POST - Applications - Invalid Whitespace ClaimSetName.bru"
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Application/PUT - Applications - Invalid Whitespace ClaimSetName.bru"
git commit -m "test: add Bruno E2E tests for Application ClaimSetName no-whitespace validation"
```

---

## Task 15: Final verification

- [ ] **Step 1: Build the full solution**

```powershell
./build.ps1 -Command build
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 2: Run all V3 unit tests**

```powershell
./build.ps1 -Command UnitTest
```

Expected: All tests pass.

- [ ] **Step 3: Final commit summary check**

```bash
git log --oneline -15
```

Expected: 14 commits visible covering the constant, 6 feature validators, 6 unit test files, 4 DB test files, 6 Bruno files.
