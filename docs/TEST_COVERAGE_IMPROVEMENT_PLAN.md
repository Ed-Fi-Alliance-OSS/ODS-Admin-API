# Test Coverage Improvement Plan

## EdFi.Ods.AdminApi

**Document Created:** March 16, 2026  
**Reference Pattern:** `ApiClients` endpoint (comprehensive unit test coverage)  
**Target:** Systematic improvement of unit test coverage endpoint by endpoint

---

## Overview

This plan addresses the gap between existing database integration tests and comprehensive unit test coverage. While the API has some DB integration tests, it lacks feature-level unit tests for validation, error handling, and business logic.

**Key Insight:** ApiClients demonstrates the desired coverage pattern with 8 dedicated unit test files covering:

* Model validation rules
* Add/Edit/Delete/Read handlers
* Special operations (credential reset)
* Complex validation scenarios
* Mock-based dependency testing

---

## Coverage Gap Summary

### Current State

| Category | Status | Examples |
|----------|--------|----------|
| **DB Integration Tests** | ✅ Exists | Vendors, Applications, OdsInstances, Profiles, ClaimSets |
| **Unit Tests (Handlers)** | ❌ Missing | Most endpoints lack feature-level unit tests |
| **Unit Tests (Validators)** | ❌ Missing | No AddValidator/EditValidator tests in most endpoints |
| **Exception/Error Cases** | ⚠️ Partial | Limited negative case testing |
| **Model Tests** | ⚠️ Partial | Few model-level unit tests |

### Current Endpoint Status

| Endpoint | Operation Files | Unit Tests | DB Tests | Gap |
|----------|--------|-----------|----------|-----|
| **ApiClients** ✅ | 6 | 8 | Yes | None - Reference pattern |
| **Tenants** | 1 | 3 | 0 | Needs DB tests |
| **Vendors** | 4 | 0 | 4 | Needs unit tests |
| **Applications** | 7 | 0 | 8+ | Needs unit tests |
| **OdsInstances** | 6 | 0 | 6+ | Needs unit tests |
| **OdsInstanceDerivative** | 4 | 0 | 4+ | Needs unit tests |
| **Profiles** | 4 | 0 | 4+ | Needs unit tests |
| **ClaimSets** | 7 | 0 | 10 | Needs unit tests |
| **AuthorizationStrategies** | 1 | 0 | 1 | Minimal coverage |
| **ResourceClaims** | 1 | 0 | 1+ | Minimal coverage |
| **ResourceClaimActions** | 1 | 0 | 1+ | Minimal coverage |
| **Connect** | 2 | 0 | 0 | Critical gap - No tests |

---

## Reference Pattern: ApiClients

### Test Organization

```
EdFi.Ods.AdminApi.UnitTests/Features/ApiClients/
├── ApiClientModelTests.cs              (Model defaults and setup)
├── AddApiClientValidatorTests.cs       (Add validation rules)
├── AddApiClientOdsInstanceIdsValidationTests.cs  (Complex DB validation)
├── ReadApiClientTests.cs               (Query handler)
├── DeleteApiClientTests.cs             (Delete handler)
├── EditApiClientValidatorTests.cs      (Edit validation rules)
├── EditApiClientOdsInstanceIdsValidationTests.cs (Complex DB validation)
└── ResetApiClientCredentialsTests.cs   (Special operations)
```

### Testing Stack

```
Core Frameworks:
  • NUnit - Test framework ([TestFixture], [Test], [SetUp])
  • Shouldly - Fluent assertions (.ShouldBe(), .ShouldThrow(), etc.)
  • FakeItEasy - Mocking (A.Fake<T>(), A.CallTo())
  • FluentValidation - Validator testing

Integration:
  • AutoMapper - Mapper integration
  • Microsoft.EntityFrameworkCore.InMemory - DB context mocking
```

### Test File Patterns

#### 1. Model Tests

```csharp
[TestFixture]
public class VendorModelTests
{
    [Test]
    public void New_VendorModel_Should_Have_Default_Values()
    {
        var model = new VendorModel();
        model.Name.ShouldBeNull();
        model.Description.ShouldBeNull();
    }
}
```

**Purpose:** Verify model initialization, defaults, and structure  
**Count:** 1 file per endpoint

---

#### 2. Validator Tests (Add Operations)

```csharp
[TestFixture]
public class AddVendorValidatorTests
{
    private AddVendor.Validator _validator;

    [SetUp]
    public void SetUp() => _validator = new AddVendor.Validator();

    [Test]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var request = new AddVendor.AddVendorRequest { Name = "" };
        var result = _validator.Validate(request);
        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        var request = new AddVendor.AddVendorRequest { Name = new string('x', 256) };
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void Should_Be_Valid_With_Complete_Request()
    {
        var request = new AddVendor.AddVendorRequest { Name = "Test Vendor" };
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }
}
```

**Purpose:** Test all validation rules for Add operations  
**Count:** 1 file per Add operation

**Guidelines:**

* Test one validation rule per `[Test]` method
* Always include positive case (valid data)
* Test boundary conditions (empty, max length, etc.)
* Use pattern: `result.Errors.Any(x => x.PropertyName == nameof(request.Property))`

---

#### 3. Validator Tests (Edit Operations)

```csharp
[TestFixture]
public class EditVendorValidatorTests
{
    private EditVendor.Validator _validator;

    [SetUp]
    public void SetUp() => _validator = new EditVendor.Validator();

    [Test]
    public void Should_Have_Error_When_Id_Is_Zero()
    {
        var request = new EditVendor.EditVendorRequest { Id = 0, Name = "Valid" };
        var result = _validator.Validate(request);
        result.Errors.Any(x => x.PropertyName == nameof(request.Id)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var request = new EditVendor.EditVendorRequest { Id = 1, Name = "" };
        var result = _validator.Validate(request);
        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeTrue();
    }

    [Test]
    public void Should_Be_Valid_With_Existing_Id_And_Name()
    {
        var request = new EditVendor.EditVendorRequest { Id = 1, Name = "Updated" };
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }
}
```

**Purpose:** Same as Add validators, but for Edit operations  
**Count:** 1 file per Edit operation

---

#### 4. Handler Tests (Queries)

```csharp
[TestFixture]
public class ReadVendorTests
{
    [Test]
    public async Task GetVendors_Returns_Ok_With_Mapped_List()
    {
        // Arrange
        var fakeQuery = A.Fake<IGetVendorsQuery>();
        var fakeMapper = A.Fake<IMapper>();
        var vendors = new[] { new Vendor { VendorId = 1, VendorName = "Test" } };
        var models = new[] { new VendorModel { VendorId = 1, Name = "Test" } };

        A.CallTo(() => fakeQuery.Execute()).Returns(vendors);
        A.CallTo(() => fakeMapper.Map<VendorModel[]>(vendors)).Returns(models);

        // Act
        var result = await ReadVendor.GetVendors(fakeQuery, fakeMapper);

        // Assert
        result.ShouldBeOfType<Ok<VendorModel[]>>();
        A.CallTo(() => fakeQuery.Execute()).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeMapper.Map<VendorModel[]>(vendors)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetVendorById_Returns_NotFound_When_Not_Exists()
    {
        // Arrange
        var fakeQuery = A.Fake<IGetVendorByIdQuery>();
        A.CallTo(() => fakeQuery.Execute(A<int>._)).Returns((Vendor)null);

        // Act
        var result = await ReadVendor.GetVendorById(fakeQuery, A.Fake<IMapper>(), 1);

        // Assert
        result.ShouldBeOfType<NotFound>();
    }
}
```

**Purpose:** Test query/read handlers with mocked dependencies  
**Count:** 1 file per Read operation

**Guidelines:**

* Use AAA pattern (Arrange, Act, Assert)
* Mock all dependencies (queries, mappers, contexts)
* Verify mock calls with `A.CallTo(...).MustHaveHappenedOnceExactly()`
* Test both success and error paths (null cases, empty lists, etc.)
* Test mapper integration

---

#### 5. Handler Tests (Commands)

```csharp
[TestFixture]
public class AddVendorTests
{
    [Test]
    public async Task Add_Vendor_Should_Create_And_Return_Created()
    {
        // Arrange
        var fakeCommand = A.Fake<IAddVendorCommand>();
        var fakeMapper = A.Fake<IMapper>();
        var request = new AddVendor.AddVendorRequest { Name = "New Vendor" };
        var entity = new Vendor { VendorId = 1, VendorName = "New Vendor" };
        var model = new VendorModel { VendorId = 1, Name = "New Vendor" };

        A.CallTo(() => fakeCommand.Execute(A<Vendor>._)).Returns(entity);
        A.CallTo(() => fakeMapper.Map<VendorModel>(entity)).Returns(model);

        // Act
        var result = await AddVendor.Add(request, fakeCommand, fakeMapper);

        // Assert
        result.ShouldBeOfType<Created>();
        A.CallTo(() => fakeCommand.Execute(A<Vendor>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Add_Vendor_Should_Return_BadRequest_When_Validator_Fails()
    {
        // Arrange
        var validator = new AddVendor.Validator();
        var request = new AddVendor.AddVendorRequest { Name = "" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
    }
}
```

**Purpose:** Test command/write handlers with mocked dependencies  
**Count:** 1 file per Add/Edit/Delete operation

---

#### 6. Complex Validation Tests (Database Queries)

For validators that need to query the database (e.g., checking for duplicates):

```csharp
[TestFixture]
public class AddVendorDuplicateCheckValidationTests
{
    private AddVendor.UniqueNameValidator _validator;
    private IQueryable<Vendor> _vendors;

    [SetUp]
    public void SetUp()
    {
        _vendors = new[]
        {
            new Vendor { VendorId = 1, VendorName = "Existing Vendor" }
        }.AsQueryable();

        _validator = new AddVendor.UniqueNameValidator();
    }

    [Test]
    public void Should_Have_Error_When_Name_Already_Exists()
    {
        var fakeDbSet = A.Fake<DbSet<Vendor>>(
            options => options.Implements(typeof(IQueryable<Vendor>))
        );
        A.CallTo(() => ((IQueryable<Vendor>)fakeDbSet).Provider).Returns(_vendors.Provider);
        A.CallTo(() => ((IQueryable<Vendor>)fakeDbSet).Expression).Returns(_vendors.Expression);
        A.CallTo(() => ((IQueryable<Vendor>)fakeDbSet).ElementType).Returns(_vendors.ElementType);
        A.CallTo(() => ((IQueryable<Vendor>)fakeDbSet).GetEnumerator()).Returns(_vendors.GetEnumerator());

        var request = new AddVendor.AddVendorRequest { Name = "Existing Vendor" };
        var result = _validator.Validate(request); // Uses DbSet internally

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeTrue();
    }
}
```

**Purpose:** Test validators that perform database queries  
**Count:** 1 file per validator with DB logic

---

### Coverage Example for Reference

**ApiClients (8 files, ~150-200 test methods):**

* ✅ Model initialization tests
* ✅ Add validation (empty name, max length, duplicate ID validation)
* ✅ Edit validation (ID required, name validation)
* ✅ Delete handler (verify command execution)
* ✅ Query handlers (list & by-id, mapper integration)
* ✅ Special operation (reset credentials handler)
* ✅ Complex DB validation (ODS instance ID checking via reflection)
* ✅ Exception handling (null cases, not found scenarios)

---

## Phased Improvement Plan

**Structure:** Each phase covers one endpoint folder systematically. Expected pattern based on ApiClients reference: **7-11 test files per phase** with **8-30 test methods per test file**.

**Unit test folder convention (required for all phases):**

* Endpoint tests stay under `EdFi.Ods.AdminApi.UnitTests/Features/<Endpoint>/`
* Command tests must be placed under `EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/`
* Query tests must be placed under `EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Queries/`

---

### Phase 1: Vendors

**Files to test:**

Endpoint files:

* `Features/Vendors/AddVendor.cs`
* `Features/Vendors/EditVendor.cs`
* `Features/Vendors/DeleteVendor.cs`
* `Features/Vendors/ReadVendor.cs`
* `Features/Vendors/VendorModel.cs`

Referenced infrastructure files:

* `Infrastructure/Database/Commands/AddVendorCommand.cs`
* `Infrastructure/Database/Commands/EditVendorCommand.cs`
* `Infrastructure/Database/Commands/DeleteVendorCommand.cs`
* `Infrastructure/Database/Queries/GetVendorsQuery.cs`
* `Infrastructure/Database/Queries/GetVendorByIdQuery.cs`
* `Infrastructure/Database/Queries/VendorExtensions.cs`

**Test files to create:**

* `VendorModelTests.cs` - Model initialization
* `AddVendorValidatorTests.cs` - Add validation rules
* `AddVendorTests.cs` - Add handler
* `EditVendorValidatorTests.cs` - Edit validation rules
* `EditVendorTests.cs` - Edit handler
* `DeleteVendorTests.cs` - Delete handler
* `ReadVendorTests.cs` - Query handlers (list, by-id)
* `Infrastructure/Database/Commands/AddVendorCommandTests.cs` - Direct command coverage
* `Infrastructure/Database/Commands/EditVendorCommandTests.cs` - Direct command coverage
* `Infrastructure/Database/Commands/DeleteVendorCommandTests.cs` - Direct command coverage
* `Infrastructure/Database/Queries/GetVendorsQueryTests.cs` - Direct query coverage
* `Infrastructure/Database/Queries/GetVendorByIdQueryTests.cs` - Direct query coverage
* `Infrastructure/Database/Queries/VendorExtensionsTests.cs` - Reserved vendor helper behavior

---

### Phase 2: Actions

**Files to test:**

Endpoint files:
* `Features/Actions/ActionModel.cs`
* `Features/Actions/ReadActions.cs`

Referenced infrastructure files:
* `Infrastructure/Database/Queries/GetAllActionsQuery.cs`

**Test files to create:**

* `Features/Actions/ActionModelTests.cs` - Model initialization and property mapping
* `Features/Actions/ReadActionsTests.cs` - Endpoint query handler behavior
* `Infrastructure/Database/Queries/GetAllActionsQueryTests.cs` - Direct query coverage (all actions, filter by id/name)

---

### Phase 3: Applications

**Files to test:** AddApplication.cs, EditApplication.cs, DeleteApplication.cs, ResetApplicationCredentials.cs, ReadApplication*.cs, ApplicationModel.cs, Commands/Queries

**Test files to create:**

* `ApplicationModelTests.cs`
* `AddApplicationValidatorTests.cs`
* `AddApplicationTests.cs`
* `EditApplicationValidatorTests.cs`
* `EditApplicationTests.cs`
* `DeleteApplicationTests.cs`
* `ReadApplicationTests.cs`
* `ResetApplicationCredentialsTests.cs`

---

### Phase 4: AuthorizationStrategies

**Files to test:** ReadAuthorizationStrategy.cs and related query handlers

**Current State:** Minimal coverage (1 query test exists)

**Test files to create/enhance:**

* `AuthorizationStrategyModelTests.cs` (new)
* `GetAuthorizationStrategiesQueryTests.cs` (enhance existing)

---

### Phase 5: ClaimSets

**Files to test:** AddClaimSet.cs, CopyClaimSet.cs, DeleteClaimSet.cs, EditClaimSet.cs, ExportClaimSet.cs, ImportClaimSet.cs, ReadClaimSets.cs, ClaimSetModel.cs, Commands/Queries

**Test files to create:**

* `ClaimSetModelTests.cs`
* `AddClaimSetValidatorTests.cs`
* `AddClaimSetTests.cs`
* `CopyClaimSetValidatorTests.cs`
* `CopyClaimSetTests.cs`
* `EditClaimSetValidatorTests.cs`
* `EditClaimSetTests.cs`
* `DeleteClaimSetTests.cs`
* `ExportClaimSetTests.cs`
* `ImportClaimSetTests.cs`
* `ReadClaimSetsTests.cs`

---

### Phase 6: Connect

**Files to test:** RegisterService.cs, TokenService.cs, Request/Response models

**Current State:** Critical gap - 0 tests exist

**Test files to create:**

* `RegisterServiceTests.cs`
* `TokenServiceTests.cs`
* `[Model tests if applicable]`

---

### Phase 7: OdsInstanceContext

**Files to test:** All handlers and models in OdsInstanceContext folder

**Pattern:** Apply standard test file structure for all operations in this folder

---

### Phase 8: OdsInstanceDerivative

**Files to test:** AddOdsInstanceDerivative.cs, EditOdsInstanceDerivative.cs, DeleteOdsInstanceDerivative.cs, ReadOdsInstanceDerivative.cs, OdsInstanceDerivativeModel.cs, Commands/Queries

**Test files to create:**

* `OdsInstanceDerivativeModelTests.cs`
* `AddOdsInstanceDerivativeValidatorTests.cs`
* `AddOdsInstanceDerivativeTests.cs`
* `EditOdsInstanceDerivativeValidatorTests.cs`
* `EditOdsInstanceDerivativeTests.cs`
* `DeleteOdsInstanceDerivativeTests.cs`
* `ReadOdsInstanceDerivativeTests.cs`

---

### Phase 9: OdsInstances

**Files to test:** AddOdsInstance.cs, EditOdsInstance.cs, DeleteOdsInstance.cs, ReadOdsInstance.cs, ReadEducationOrganizations.cs, RefreshEducationOrganizations.cs, OdsInstanceModel.cs, Commands/Queries

**Test files to create:**

* `OdsInstanceModelTests.cs`
* `AddOdsInstanceValidatorTests.cs`
* `AddOdsInstanceTests.cs`
* `EditOdsInstanceValidatorTests.cs`
* `EditOdsInstanceTests.cs`
* `DeleteOdsInstanceTests.cs`
* `ReadOdsInstanceTests.cs`
* `ReadEducationOrganizationsTests.cs`
* `RefreshEducationOrganizationsTests.cs`

---

### Phase 10: Profiles

**Files to test:** AddProfile.cs, EditProfile.cs, DeleteProfile.cs, ReadProfile.cs, ProfileModel.cs, Commands/Queries

**Test files to create:**

* `ProfileModelTests.cs`
* `AddProfileValidatorTests.cs`
* `AddProfileTests.cs`
* `EditProfileValidatorTests.cs`
* `EditProfileTests.cs`
* `DeleteProfileTests.cs`
* `ReadProfileTests.cs`

---

### Phase 11: ResourceClaimActionAuthStrategies

**Files to test:** All handlers and models in ResourceClaimActionAuthStrategies folder

**Pattern:** Apply standard test file structure for all operations in this folder

---

### Phase 12: ResourceClaimActions

**Files to test:** ReadResourceClaimActions.cs and related handlers

**Current State:** Minimal coverage (1 query test exists)

**Test files to create/enhance:**

* `ResourceClaimActionsModelTests.cs` (new)
* `GetResourceClaimActionsQueryTests.cs` (enhance existing)

---

### Phase 13: ResourceClaims

**Files to test:** ReadResourceClaims.cs and related handlers

**Current State:** Minimal coverage (1 query test exists)

**Test files to create/enhance:**

* `ResourceClaimsModelTests.cs` (new)
* `GetResourceClaimsQueryTests.cs` (enhance existing)

---

### Phase 14: Tenants

**Files to test:** ReadTenants.cs, TenantModel.cs, Commands/Queries

**Current State:** 3 unit tests exist, 0 DB integration tests

**Test files to create/enhance:**

* Enhance existing unit tests
* Add database integration tests to match other endpoints
* Expand coverage of all operations

---

## Detailed Implementation Checklist

### For Each Endpoint Phase

* [ ] **Analysis**
  * [ ] List all operation files (Add, Edit, Delete, Read)
  * [ ] Identify command/query dependencies
  * [ ] Review existing handler code for edge cases
  * [ ] Check for special validation logic

* [ ] **Model Tests**
  * [ ] Create `[Endpoint]ModelTests.cs`
  * [ ] Test default values
  * [ ] Test property initialization
  * [ ] Test any calculated properties

* [ ] **Validator Tests (Add)**
  * [ ] Create `Add[Endpoint]ValidatorTests.cs`
  * [ ] Test required field validation
  * [ ] Test length/range constraints
  * [ ] Test format validation (emails, URLs, etc.)
  * [ ] Test uniqueness constraints (if any)
  * [ ] Test positive case (valid request)

* [ ] **Validator Tests (Edit)**
  * [ ] Create `Edit[Endpoint]ValidatorTests.cs`
  * [ ] Test ID required validation
  * [ ] Test field-level validation (same as Add)
  * [ ] Test that edit allows valid updates
  * [ ] Test positive case

* [ ] **Handler Tests (Add/Edit/Delete)**
  * [ ] Create `Add[Endpoint]Tests.cs`, `Edit[Endpoint]Tests.cs`, `Delete[Endpoint]Tests.cs`
  * [ ] Mock all dependencies (commands, mappers, contexts)
  * [ ] Test successful execution
  * [ ] Test mapping calls
  * [ ] Test command execution
  * [ ] Test error scenarios (null, not found, exceptions)

* [ ] **Handler Tests (Read)**
  * [ ] Create `Read[Endpoint]Tests.cs`
  * [ ] Test list query (empty, with items)
  * [ ] Test by-ID query (found, not found)
  * [ ] Test mapper integration
  * [ ] Test filter/search if applicable
  * [ ] Test pagination if applicable

* [ ] **Validation Integration Tests (if DB-dependent)**
  * [ ] Create `[Endpoint]DatabaseValidationTests.cs` if validator queries DB
  * [ ] Mock DbSet with proper IQueryable implementation
  * [ ] Test duplicate detection
  * [ ] Test foreign key validation

---

## Testing Best Practices (from ApiClients Reference)

### 1. Naming Conventions

```
Model Tests:        [Endpoint]ModelTests.cs
Validators:         Add[Endpoint]ValidatorTests.cs, Edit[Endpoint]ValidatorTests.cs
Commands/Queries:   Add[Endpoint]Tests.cs, Edit[Endpoint]Tests.cs, 
                    Delete[Endpoint]Tests.cs, Read[Endpoint]Tests.cs
Special Ops:        [OperationName][Endpoint]Tests.cs
Complex Validation: [Endpoint][SpecialLogic]ValidationTests.cs

Test Methods:
  Validators:      Should_Have_Error_When_[Condition]
                   Should_Be_Valid_When_[Condition]
  Commands/Queries: [Operation]_[Condition]_[ExpectedResult]
```

### 2. Test Structure (AAA Pattern)

```csharp
[Test]
public async Task [TestName]()
{
    // Arrange - Set up test data, mocks, dependencies
    var fake... = A.Fake<I...>();
    A.CallTo(() => fake....Method()).Returns(...);

    // Act - Execute the code under test
    var result = await Handler.Method(...);

    // Assert - Verify the result
    result.ShouldBe...();
    A.CallTo(() => fake....Method()).MustHaveHappened...();
}
```

### 3. Assertion Library (Shouldly)

```csharp
// Comparisons
result.ShouldBe(expected);
result.ShouldBeNull();
result.ShouldBeEmpty();

// Collections
list.ShouldContain(item);
list.ShouldHaveCount(5);

// Exceptions
Should.Throw<InvalidOperationException>(() => method());

// Types
result.ShouldBeOfType<Ok<Model>>();
```

### 4. Mock Verification (FakeItEasy)

```csharp
// Set up
var fake = A.Fake<IService>();
A.CallTo(() => fake.Method(arg)).Returns(value);

// Verify
A.CallTo(() => fake.Method(A<string>._)).MustHaveHappenedOnceExactly();
A.CallTo(() => fake.Method(x => x == "specific")).MustHaveHappened();
A.CallTo(() => fake.Method(A<int>.Ignored)).MustNotHaveHappened();
```

### 5. Test Fixtures

```csharp
[TestFixture]
public class VendorTests
{
    private IVendorQuery _fakeQuery;
    private IMapper _fakeMapper;

    [SetUp]  // Runs before each [Test]
    public void SetUp()
    {
        _fakeQuery = A.Fake<IVendorQuery>();
        _fakeMapper = A.Fake<IMapper>();
    }

    [Test]
    public async Task Should_Call_Handler() { ... }
}
```

### 6. Edge Cases to Always Test

```csharp
// For validators:
- Empty/null values for required fields
- String length boundaries (empty, max)
- Numeric boundaries (0, negative, max int)
- Duplicate/uniqueness violations
- Invalid formats (dates, emails, URLs)

// For handlers:
- Null dependencies (should validate in SetUp)
- Database returns null (not found)
- Empty collections
- Null mapper responses
- Exception from dependencies
```

---

## Execution Guide

### Weekly Template

#### Monday-Tuesday: Analysis & Planning

1. Review feature files for the endpoint
2. Identify all handlers, validators, commands, queries
3. List edge cases and validation rules
4. Design test structure (which test files to create)

#### Wednesday: Implementation

1. Create model tests
2. Create validator tests
3. Create handler tests for basic operations

#### Thursday-Friday: Refinement & Review

1. Add edge case tests
2. Add error scenario tests
3. Run tests locally (`./build.ps1 -Command UnitTest`)
4. Code review: Check against ApiClients patterns
5. Performance check: Ensure tests run < 100ms each

---

## Success Metrics

### Coverage Targets

* [ ] Each endpoint has ≥ 1 test file per operation (Add, Edit, Delete, Read)

* [ ] Each validator has ≥ 8-10 test methods
* [ ] Each handler has ≥ 5-8 test methods (success + error cases)
* [ ] Model tests cover initialization and any special logic

### Quality Targets

* [ ] All tests follow ApiClients naming and structure patterns

* [ ] All tests use Shouldly assertions and FakeItEasy mocks
* [ ] No flaky tests (run locally 3x, all pass)
* [ ] Test execution time < 100ms per test method
* [ ] No external dependencies in unit tests (mocks only)

### Documentation

* [ ] Test method names clearly describe what is being tested

* [ ] Comments explain non-obvious test logic
* [ ] Setup/teardown is minimal and clear

---

## File Organization Summary

```
EdFi.Ods.AdminApi.UnitTests/
└── Features/
    ├── Actions/                           Phase 2 (10-12 files)
    ├── ApiClients/                        ✅ Reference implementation (8 files)
    ├── Applications/                      Phase 3 (8 files)
    ├── AuthorizationStrategies/           Phase 4 (2-3 files)
    ├── ClaimSets/                         Phase 5 (11 files)
    ├── Connect/                           Phase 6 (3 files)
    ├── OdsInstanceContext/                Phase 7 (8-10 files)
    ├── OdsInstanceDerivative/             Phase 8 (7 files)
    ├── OdsInstances/                      Phase 9 (9 files)
    ├── Profiles/                          Phase 10 (7 files)
    ├── ResourceClaimActionAuthStrategies/ Phase 11 (6-8 files)
    ├── ResourceClaimActions/              Phase 12 (2 files)
    ├── ResourceClaims/                    Phase 13 (2 files)
    ├── Tenants/                           Phase 14 (enhance existing)
    └── Vendors/                           Phase 1 (7 files)

Total new test files: ~102-115 files (across 14 phases)
```

---

## Phase Summary & Timeline

| Phase | Endpoint | Status |
|-------|----------|--------|
| 1 | Vendors | Completed |
| 2 | Actions | Completed |
| 3 | Applications | Not Started |
| 4 | AuthorizationStrategies | Not Started |
| 5 | ClaimSets | Not Started |
| 6 | Connect | Not Started |
| 7 | OdsInstanceContext | Not Started |
| 8 | OdsInstanceDerivative | Not Started |
| 9 | OdsInstances | Not Started |
| 10 | Profiles | Not Started |
| 11 | ResourceClaimActionAuthStrategies | Not Started |
| 12 | ResourceClaimActions | Not Started |
| 13 | ResourceClaims | Not Started |
| 14 | Tenants | Not Started |
| | **ALL PHASES** | **In Progress** |

---

## Performance Baseline

**ApiClients reference:**

* 8 test files
* ~180+ test methods
* Execution time: ~500-800ms for full suite
* **Per-file average: 4-6 seconds**

**Target for other endpoints:**

* Similar ratio: 1 file per operation + models + complex validation
* ~20-30 test methods per test file (typical)
* Individual test execution: 10-50ms

---

## Getting Started with Phase 1 (Vendors)

To begin implementation:

1. **Analyze the Vendors folder** - List all operation files and understand the structure
2. **Review ApiClients reference tests** - See the exact patterns to replicate
3. **Create test files** following the naming convention and structure patterns
4. **Implement tests** using NUnit + Shouldly + FakeItEasy
5. **Run locally** with `./build.ps1 -Command UnitTest`
6. **Track progress** - Mark phase as complete once all tests pass

---

## Implementation Checklist Template (Per Phase)

Before starting each phase:

* [ ] **Analysis Complete**
  * [ ] All operation files identified (Add, Edit, Delete, Read, Special ops)
  * [ ] Command/Query dependencies mapped
  * [ ] Edge cases and validation rules documented
  
* [ ] **Unit Tests Created**
  * [ ] Model test file created with 3-5 test methods
  * [ ] Validator tests created for Add/Edit operations (8-10 tests each)
  * [ ] Handler tests created for all operations (5-8 tests each)
  * [ ] Complex validation tests if DB-dependent (if applicable)

* [ ] **Quality Checks**
  * [ ] All tests run locally and pass
  * [ ] No external dependencies (all mocked)
  * [ ] Naming follows convention (Should_Have_Error_When_*, [Op]_[Condition]_[Result])
  * [ ] Test execution time < 100ms per test

* [ ] **Documentation**
  * [ ] Test method names clearly describe intent
  * [ ] Complex test logic has comments
  * [ ] SetUp/teardown is minimal

---

## Next Steps

1. **Start Phase 3:** Begin with Applications endpoint
2. **Follow folder convention:** Endpoint tests under `Features/Applications/`; command/query tests under `Infrastructure/Database/Commands` and `Infrastructure/Database/Queries`
3. **Reference the ApiClients and completed Vendors/Actions tests** for exact patterns
4. **Track progress** in the Phase Summary table
5. **Review and iterate** - each phase builds on the same patterns

---

**Document Version:** 2.0  
**Last Updated:** March 16, 2026  
**Status:** 14-Phase Plan Complete - Ready to Begin Phase 1
