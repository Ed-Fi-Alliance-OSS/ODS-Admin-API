# AI Agent Skills for Ed-Fi ODS Admin API

This document contains reusable patterns, skills, and knowledge for AI agents (especially Claude Sonnet 4.5) working on the Ed-Fi ODS Admin API codebase.

---

## 🏗️ Architecture Overview

### Project Structure

```
Application/
├── EdFi.Ods.AdminApi/              # Main API project
│   ├── Features/                    # Feature-based organization (endpoints)
│   │   ├── EducationOrganizations/  # Ed Org endpoints & models
│   │   ├── Tenants/                 # Tenant management
│   │   ├── Vendors/                 # Vendor management
│   │   └── ...
│   ├── Infrastructure/              # Cross-cutting concerns
│   │   ├── AutoMapper/              # DTO mappings
│   │   ├── Database/
│   │   │   ├── Queries/             # Read operations
│   │   │   └── Commands/            # Write operations
│   │   └── Services/                # Business logic services
│   └── E2E Tests/                   # Postman collections
├── EdFi.Ods.AdminApi.Common/        # Shared code
├── EdFi.Ods.AdminApi.UnitTests/     # Unit tests
├── EdFi.Ods.AdminApi.DBTests/       # Integration tests
└── EdFi.Ods.AdminApi.V1/            # V1 legacy support
```

### Key Architectural Patterns

* **Feature-based organization**: Endpoints grouped by domain feature

* **CQRS-lite**: Separate Query and Command classes
* **AutoMapper**: DTO/Entity mappings
* **Minimal APIs**: ASP.NET Core minimal API endpoints
* **Multi-tenancy support**: Optional tenant isolation

---

## 🔧 Common Development Skills

### Skill 1: Adding a New API Endpoint

**Pattern**: Feature → Endpoint → Query/Command → Tests

1. **Create Feature Class**

   ```csharp
   // Application/EdFi.Ods.AdminApi/Features/[FeatureName]/[Action][FeatureName].cs
   public class Read[FeatureName] : IFeature
   {
       public void MapEndpoints(IEndpointRouteBuilder endpoints)
       {
           AdminApiEndpointBuilder
               .MapGet(endpoints, "/[resource]", Get[Resource]Async)
               .WithSummaryAndDescription("Summary", "Description")
               .WithRouteOptions(b => b.WithResponse<ResponseModel>(200))
               .BuildForVersions(AdminApiVersions.V2);
       }

       public static async Task<IResult> Get[Resource]Async(
           [FromServices] IQuery query,
           [AsParameters] CommonQueryParams queryParams)
       {
           var result = await query.ExecuteAsync(queryParams);
           return Results.Ok(result);
       }
   }
   ```

2. **Create Query Class**

   ```csharp
   // Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/Get[Resource]Query.cs
   public interface IGet[Resource]Query
   {
       Task<List<Model>> ExecuteAsync(CommonQueryParams queryParams);
   }

   public class Get[Resource]Query(
       AdminApiDbContext context,
       IMapper mapper) : IGet[Resource]Query
   {
       public async Task<List<Model>> ExecuteAsync(CommonQueryParams queryParams)
       {
           var entities = await context.[DbSet]
               .OrderBy(x => x.Id)
               .ToListAsync();

           return mapper.Map<List<Model>>(entities);
       }
   }
   ```

3. **Register in DI** (Program.cs or ServiceCollectionExtensions)

   ```csharp
   services.AddScoped<IGet[Resource]Query, Get[Resource]Query>();
   ```

4. **Create Tests**
   * Unit test in `EdFi.Ods.AdminApi.UnitTests/Features/[FeatureName]/`
   * E2E test in Bruno: `E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/[FeatureName]/`
   * Note: Postman tests are legacy - use Bruno for new tests

---

### Skill 2: Working with AutoMapper

**Location**: `Application/EdFi.Ods.AdminApi/Infrastructure/AutoMapper/AdminApiMappingProfile.cs`

**Common Patterns**:

```csharp
// Simple mapping
CreateMap<Source, Destination>();

// With property mapping
CreateMap<Source, Destination>()
    .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.FullName))
    .ForMember(dst => dst.Collection, opt => opt.Ignore());

// With custom resolver
CreateMap<Source, Destination>()
    .ForMember(dst => dst.Field, opt => 
    {
        opt.ConvertUsing<CustomConverter, int>("PropertyName");
    });
```

**Best Practices**:

* Keep mappings in `AdminApiMappingProfile.cs`
* Use `.ForMember()` for explicit property mappings
* Use `.Ignore()` for properties populated elsewhere
* Test mappings with unit tests

---

### Skill 3: Database Queries with EF Core

**Patterns from the codebase**:

```csharp
// Basic query
var results = await _context.EntitySet
    .Where(e => e.Property == value)
    .OrderBy(e => e.Id)
    .ToListAsync();

// With pagination (using helper)
var results = await _context.EntitySet
    .OrderBy(e => e.Id)
    .Paginate(offset, limit, _options)
    .ToListAsync();

// Dynamic ordering (using helper)
Expression<Func<Entity, object>> columnToOrderBy = 
    _orderByColumns.GetColumnToOrderBy(queryParams.OrderBy);

var results = await _context.EntitySet
    .OrderByColumn(columnToOrderBy, queryParams.IsDescending)
    .ToListAsync();

// Grouping
var grouped = await _context.EntitySet
    .ToListAsync()
    .GroupBy(e => e.GroupKey)
    .Select(group => new ResultModel
    {
        Key = group.Key,
        Items = group.ToList()
    })
    .ToList();
```

**Important Extensions**:

* `.Paginate()` - in `Infrastructure/Helpers/PaginationHelper.cs`
* `.OrderByColumn()` - in `Infrastructure/Extensions/LinqExtensions.cs`
* `.GetColumnToOrderBy()` - in `Infrastructure/Helpers/DictionaryExtensions.cs`

---

### Skill 4: Model Design Patterns

**Request Models**:

```csharp
[SwaggerSchema]
public class Create[Resource]Request
{
    [Required]
    [SwaggerSchema(Description = "Field description", Nullable = false)]
    public string RequiredField { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Optional field")]
    public string? OptionalField { get; set; }
}
```

**Response Models**:

```csharp
[SwaggerSchema(Title = "ResourceName")]
public class [Resource]Model
{
    [SwaggerSchema(Description = "Identifier", Nullable = false)]
    public int Id { get; set; }

    [SwaggerSchema(Description = "Name", Nullable = false)]
    public string Name { get; set; } = string.Empty;
}
```

**Nested Response Models**:

```csharp
public class ParentModel
{
    public int Id { get; set; }
    public List<ChildModel> Children { get; set; } = new();
}
```

---

### Skill 5: Writing Tests

#### Unit Tests (NUnit + Shouldly + FakeItEasy)

```csharp
[TestFixture]
public class FeatureTests
{
    private IService _service;
    private IDependency _dependency;

    [SetUp]
    public void Setup()
    {
        _dependency = A.Fake<IDependency>();
        _service = new Service(_dependency);
    }

    [Test]
    public async Task Method_Should_ReturnExpectedResult_When_ConditionMet()
    {
        // Arrange
        var input = new Input { Value = "test" };
        A.CallTo(() => _dependency.GetData()).Returns("data");

        // Act
        var result = await _service.Method(input);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("expected");
        A.CallTo(() => _dependency.GetData()).MustHaveHappenedOnceExactly();
    }
}
```

**Shouldly Assertions**:

* `.ShouldBe(expected)`
* `.ShouldNotBeNull()`
* `.ShouldBeNull()`
* `.ShouldContain(item)`
* `.ShouldAllBe(predicate)`

#### E2E Tests (Bruno - Preferred)

**Note**: The project is migrating from Postman to Bruno for API testing. Bruno is now the preferred E2E testing tool.

**Location**: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/`

**Bruno File Structure**:
```
v2/
├── folder.bru
├── ApiClient/
│   ├── folder.bru
│   ├── GET - ApiClients.bru
│   └── POST - ApiClients.bru
├── Tenants/
│   ├── folder.bru
│   └── GET - Tenants.bru
└── [Other Features]/
```

**Bruno Test File Format (.bru)**:
```
get {
  url: {{API_URL}}/v2/resource
  body: none
  auth: inherit
}

headers {
  Content-Type: application/json
  tenant: {{TENANT_NAME}}
}

tests {
  test("Status code is 200", () => {
    expect(res.status).to.equal(200);
  });
  
  test("Response has expected structure", () => {
    const data = res.body;
    expect(data).to.be.an('array');
  });
  
  test("Response contains required fields", () => {
    const data = res.body;
    if (data.length > 0) {
      expect(data[0]).to.have.property('id');
      expect(data[0]).to.have.property('name');
    }
  });
}
```

**Common Bruno Test Patterns**:

```javascript
// Status code validation
test("Status code is 200", () => {
  expect(res.status).to.equal(200);
});

// Response type validation
test("Response is array", () => {
  expect(res.body).to.be.an('array');
});

// Property existence
test("Has required properties", () => {
  expect(res.body[0]).to.have.property('id');
});

// Property type validation
test("ID is a number", () => {
  expect(res.body[0].id).to.be.a('number');
});

// Nested array validation
test("Has nested collection", () => {
  expect(res.body[0].items).to.be.an('array');
});

// Property absence (for refactoring)
test("Does not have deprecated field", () => {
  expect(res.body[0]).to.not.have.property('oldField');
});
```

**Variables in Bruno**:
- Environment variables: `{{API_URL}}`, `{{TENANT_NAME}}`
- Configured in `environments/local.bru` or other environment files
- Use `{{variable_name}}` syntax in requests

#### Migrating from Postman to Bruno

**Step 1: Create Bruno file structure**
```
[FeatureName]/
├── folder.bru (folder-level configuration)
├── GET - [ResourceName].bru
├── POST - [ResourceName].bru
└── PUT - [ResourceName].bru
```

**Step 2: Convert request format**

Postman:
```json
{
  "method": "GET",
  "url": "{{API_URL}}/v2/resource",
  "header": [{"key": "tenant", "value": "{{TENANT}}"}]
}
```

Bruno:
```
get {
  url: {{API_URL}}/v2/resource
  body: none
  auth: inherit
}

headers {
  tenant: {{TENANT_NAME}}
}
```

**Step 3: Convert tests**

Postman:
```javascript
pm.test("Status code is 200", function() {
    pm.response.to.have.status(200);
});
pm.test("Response is array", function() {
    pm.expect(pm.response.json()).to.be.an('array');
});
```

Bruno:
```javascript
tests {
  test("Status code is 200", () => {
    expect(res.status).to.equal(200);
  });
  
  test("Response is array", () => {
    expect(res.body).to.be.an('array');
  });
}
```

**Key Differences**:
- Postman: `pm.response.json()` → Bruno: `res.body`
- Postman: `pm.response.code` → Bruno: `res.status`
- Postman: `pm.test()` → Bruno: `test()` inside `tests {}` block
- Bruno uses arrow functions by default

#### Legacy E2E Tests (Postman - Being Phased Out)

**Note**: Postman collections in `E2E Tests/V2/Admin API E2E 2.0 - *.postman_collection.json` are legacy and should be migrated to Bruno.

Legacy Structure:
```json
{
  "name": "Endpoint Name",
  "request": {
    "method": "GET",
    "url": "{{API_URL}}/v2/resource"
  },
  "event": [{
    "listen": "test",
    "script": {
      "exec": [
        "pm.test('Status code is 200', function() {",
        "  pm.response.to.have.status(200);",
        "});"
      ]
    }
  }]
}
```

---

### Skill 6: Error Handling Patterns

**Not Found**:

```csharp
if (entity is null)
    throw new NotFoundException<EntityType>("EntityName", id);
```

**Validation**:

```csharp
if (string.IsNullOrEmpty(required))
    throw new ValidationException([
        new ValidationFailure("FieldName", "Error message")
    ]);
```

**Result Patterns**:

```csharp
// Success
return Results.Ok(data);

// Created
return Results.Created($"/resource/{id}", data);

// Not Found
return Results.NotFound();

// Bad Request
return Results.BadRequest(error);
```

---

### Skill 7: Multi-Tenancy Support

**Check tenant header**:

```csharp
if (options.Value.MultiTenancy)
{
    var tenantHeader = request.Headers["tenant"].FirstOrDefault();
    if (tenantHeader is null)
        throw new ValidationException([
            new ValidationFailure("Tenant", ErrorMessagesConstants.Tenant_MissingHeader)
        ]);
}
```

**Get tenant-specific context**:

```csharp
var context = await _tenantSpecificDbContextProvider
    .GetAdminApiDbContextAsync(tenantName);
```

---

### Skill 8: Common Helper Methods

**Connection Strings**:

```csharp
var (host, database) = ConnectionStringHelper.GetHostAndDatabase(
    databaseEngine,
    connectionString
);
```

**Pagination**:

```csharp
var paginated = await query
    .Paginate(offset, limit, _options)
    .ToListAsync();
```

**Ordering**:

```csharp
var ordered = query.OrderByColumn(orderExpression, isDescending);
```

---

## 🎯 Best Practices Checklist

### Code Style

* [ ] Use file-scoped namespace declarations

* [ ] Add newline before opening braces
* [ ] Use `is null` / `is not null` for null checks
* [ ] Use `nameof()` for member name strings
* [ ] Initialize non-nullable properties with `= string.Empty` or `= new()`
* [ ] Use pattern matching where possible
* [ ] Use switch expressions over switch statements

### Architecture

* [ ] Follow feature-based organization

* [ ] Separate queries from commands (CQRS-lite)
* [ ] Use AutoMapper for DTO mappings
* [ ] Register services in DI container
* [ ] Keep business logic in services, not controllers

### Testing

* [ ] Write unit tests for all public methods

* [ ] Use Shouldly for assertions
* [ ] Use FakeItEasy for mocking
* [ ] Update E2E Bruno tests (preferred)
* [ ] Migrate Postman tests to Bruno when modifying features
* [ ] Test multi-tenancy scenarios when applicable
* [ ] Test edge cases (empty lists, null values, etc.)

### Database

* [ ] Use async methods (`ToListAsync`, `FirstOrDefaultAsync`, etc.)

* [ ] Use parameterized queries (EF Core handles this)
* [ ] Include error handling for database operations
* [ ] Test pagination and sorting

### API Design

* [ ] Use appropriate HTTP methods (GET, POST, PUT, DELETE)

* [ ] Return appropriate status codes
* [ ] Use Swagger annotations for documentation
* [ ] Follow REST naming conventions
* [ ] Version endpoints appropriately (V1, V2)

---

## 📚 Reference Documentation

### Internal Documentation

* `/docs/design/` - Design documents for features

* `.github/copilot-instructions.md` - Code style guide
* `.editorconfig` - Editor formatting rules
* `/docs/developer.md` - Developer setup guide

### External Resources

* [Ed-Fi Documentation](https://techdocs.ed-fi.org/)

* [ASP.NET Core Minimal APIs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
* [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
* [AutoMapper](https://docs.automapper.org/)

---

## 🔍 Troubleshooting Guide

### Issue: Tests Failing After Changes

1. Run `./build.ps1 UnitTest` to see specific failures
2. Check if AutoMapper configurations are updated
3. Verify all using statements are added
4. Check if model properties match expectations

### Issue: Endpoint Not Registering

1. Verify `IFeature` implementation
2. Check DI registration in Program.cs
3. Ensure correct API version in `.BuildForVersions()`
4. Verify route path doesn't conflict

### Issue: Database Query Not Working

1. Check EF Core logs
2. Verify entity relationships
3. Test query separately in unit test
4. Check if indexes exist for filtered columns

### Issue: AutoMapper Throwing Exceptions

1. Verify mapping configuration exists
2. Check property names match
3. Use `.Ignore()` for unmapped properties
4. Test mapping in unit test

---

## 💡 Pro Tips

1. **Use existing patterns**: Search codebase for similar implementations before creating new patterns
2. **Test early, test often**: Run unit tests after each logical change
3. **Follow the feature folder structure**: Keep related code together
4. **Use meaningful variable names**: Code should be self-documenting
5. **Keep methods small**: Single responsibility principle
6. **Update E2E tests**: Use Bruno for new tests, migrate Postman tests when touching features
7. **Bruno over Postman**: The project is migrating to Bruno - create new tests in Bruno format
8. **Document breaking changes**: Update design docs and migration guides
9. **Consider multi-tenancy**: Test both single and multi-tenant scenarios

---

**Last Updated**: February 10, 2026  
**Maintainer**: Ed-Fi Alliance  
**Repository**: [ODS-Admin-API](https://github.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x)
