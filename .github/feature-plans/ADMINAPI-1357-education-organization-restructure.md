# ADMINAPI-1357: Education Organization Response Model Restructure

## AI Agent Instructions for Claude Sonnet 4.5

This document provides clear, step-by-step instructions for implementing the restructuring of Education Organization endpoints to use nested response models.

---

## 🎯 Feature Overview

**Objective**: Restructure three V2 endpoints to return a nested response format where education organizations are grouped under their ODS instances, eliminating data duplication.

**Affected Endpoints**:

1. `GET /v2/educationorganizations`
2. `GET /v2/educationorganizations/{instanceId}`
3. `GET /v2/tenants/{tenantName}/edOrgsByInstances` → **RENAME TO** → `GET /v2/tenants/{tenantName}/OdsInstances/edOrgs`

**Design Reference**: See `docs/design/ADMINAPI-1357.md` for detailed requirements.

**E2E Testing**: This project uses **Bruno** for API testing. Bruno tests are located in `E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/`. Legacy Postman collections are being phased out - create new tests in Bruno format and migrate Postman tests when touching features.

---

## 📋 Requirements Summary

### Current Response Structure (Flat - with duplication)

```json
[
  {
    "instanceId": 1,
    "instanceName": "odsinstance1 tenant1",
    "educationOrganizationId": 255901,
    "nameOfInstitution": "Grand Bend ISD",
    "shortNameOfInstitution": "GBISD",
    "discriminator": "edfi.LocalEducationAgency",
    "parentId": 255950
  },
  {
    "instanceId": 1,
    "instanceName": "odsinstance1 tenant1",
    "educationOrganizationId": 255950,
    "nameOfInstitution": "Region 99 Education Service Center",
    "shortNameOfInstitution": null,
    "discriminator": "edfi.EducationServiceCenter",
    "parentId": null
  }
]
```

### Expected Response Structure (Nested - no duplication)

```json
[
  {
    "id": 1,
    "name": "Ods-test",
    "instanceType": "OdsInstance",
    "educationOrganizations": [
      {
        "educationOrganizationId": 255901,
        "nameOfInstitution": "Grand Bend ISD",
        "shortNameOfInstitution": "GBISD",
        "discriminator": "edfi.LocalEducationAgency",
        "parentId": 255950
      },
      {
        "educationOrganizationId": 255950,
        "nameOfInstitution": "Region 99 Education Service Center",
        "shortNameOfInstitution": null,
        "discriminator": "edfi.EducationServiceCenter",
        "parentId": null
      }
    ]
  }
]
```

### Key Changes

1. ✅ Group education organizations by ODS instance
2. ✅ Remove `instanceId` and `instanceName` from individual education organization objects
3. ✅ Create parent-level structure with instance metadata
4. ✅ Rename tenant endpoint route
5. ✅ Remove duplicate `TenantEducationOrganizationModel` class
6. ✅ Return empty arrays for instances with no education organizations

---

## 🏗️ Implementation Plan

### Phase 1: Model Structure Changes

#### Task 1.1: Create New Wrapper Model

**File**: `Application/EdFi.Ods.AdminApi/Features/EducationOrganizations/EducationOrganizationModels.cs`

**Action**: Add new class at the end of the file:

```csharp
[SwaggerSchema(Title = "OdsInstanceWithEducationOrganizations")]
public class OdsInstanceWithEducationOrganizationsModel
{
    [SwaggerSchema(Description = "ODS instance identifier", Nullable = false)]
    public int Id { get; set; }

    [SwaggerSchema(Description = "ODS instance name", Nullable = false)]
    public string Name { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Type of ODS instance")]
    public string? InstanceType { get; set; }

    [SwaggerSchema(Description = "List of education organizations in this instance")]
    public List<EducationOrganizationModel> EducationOrganizations { get; set; } = new();
}
```

**Note**: This wrapper will be reused for future migration to `/v2/odsInstances/...` endpoints.

#### Task 1.2: Update EducationOrganizationModel

**File**: `Application/EdFi.Ods.AdminApi/Features/EducationOrganizations/EducationOrganizationModels.cs`

**Action**: Remove `InstanceId` and `InstanceName` properties from `EducationOrganizationModel`:

**Before**:

```csharp
public class EducationOrganizationModel
{
    public int InstanceId { get; set; }           // ❌ REMOVE
    public string InstanceName { get; set; }       // ❌ REMOVE
    public long EducationOrganizationId { get; set; }
    public string NameOfInstitution { get; set; }
    public string? ShortNameOfInstitution { get; set; }
    public string Discriminator { get; set; }
    public long? ParentId { get; set; }
}
```

**After**:

```csharp
[SwaggerSchema(Title = "EducationOrganization")]
public class EducationOrganizationModel
{
    [SwaggerSchema(Description = "Education organization identifier", Nullable = false)]
    public long EducationOrganizationId { get; set; }

    [SwaggerSchema(Description = "Name of the education organization", Nullable = false)]
    public string NameOfInstitution { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Short name of the education organization")]
    public string? ShortNameOfInstitution { get; set; }

    [SwaggerSchema(Description = "Type of education organization (e.g., LocalEducationAgency, School)", Nullable = false)]
    public string Discriminator { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Parent education organization identifier")]
    public long? ParentId { get; set; }
}
```

---

### Phase 2: Data Layer Updates

#### Task 2.1: Update AutoMapper Mappings

**File**: `Application/EdFi.Ods.AdminApi/Infrastructure/AutoMapper/AdminApiMappingProfile.cs`

**Location**: In the constructor, after existing mappings (around line 213)

**Actions**:

1. The existing `CreateMap<EducationOrganization, EducationOrganizationModel>()` will work as-is after removing properties
2. **REMOVE** this mapping:

   ```csharp
   CreateMap<EducationOrganization, TenantEducationOrganizationModel>()
       .ForMember(dst => dst.InstanceId, opt => opt.MapFrom(src => src.InstanceId))
       .ForMember(dst => dst.InstanceName, opt => opt.MapFrom(src => src.InstanceName))
       .ForMember(dst => dst.EducationOrganizationId, opt => opt.MapFrom(src => src.EducationOrganizationId))
       .ForMember(dst => dst.NameOfInstitution, opt => opt.MapFrom(src => src.NameOfInstitution))
       .ForMember(dst => dst.ShortNameOfInstitution, opt => opt.MapFrom(src => src.ShortNameOfInstitution))
       .ForMember(dst => dst.Discriminator, opt => opt.MapFrom(src => src.Discriminator))
       .ForMember(dst => dst.ParentId, opt => opt.MapFrom(src => src.ParentId));
   ```

3. **ADD** new mapping for OdsInstance wrapper:

   ```csharp
   CreateMap<OdsInstance, OdsInstanceWithEducationOrganizationsModel>()
       .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.OdsInstanceId))
       .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
       .ForMember(dst => dst.InstanceType, opt => opt.MapFrom(src => src.InstanceType))
       .ForMember(dst => dst.EducationOrganizations, opt => opt.Ignore());
   ```

#### Task 2.2: Update GetEducationOrganizationsQuery Interface

**File**: `Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs`

**Action**: Update interface (lines 20-26):

**Before**:

```csharp
public interface IGetEducationOrganizationsQuery
{
    Task<List<EducationOrganizationModel>> ExecuteAsync();
    Task<List<EducationOrganizationModel>> ExecuteAsync(CommonQueryParams commonQueryParams, int? instanceId);
}
```

**After**:

```csharp
public interface IGetEducationOrganizationsQuery
{
    Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync();
    Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync(CommonQueryParams commonQueryParams, int? instanceId);
}
```

#### Task 2.3: Update GetEducationOrganizationsQuery Implementation

**File**: `Application/EdFi.Ods.AdminApi/Infrastructure/Database/Queries/GetEducationOrganizationsQuery.cs`

**Action 1**: Update both `ExecuteAsync` methods to return new type and add grouping logic:

**For ExecuteAsync() - no parameters**:

```csharp
public async Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync()
{
    var educationOrganizations = await _adminApiDbContext.EducationOrganizations
        .OrderBy(e => e.InstanceId)
        .ThenBy(e => e.EducationOrganizationId)
        .ToListAsync();

    return GroupEducationOrganizationsByInstance(educationOrganizations);
}
```

**For ExecuteAsync(CommonQueryParams, int?) - with parameters**:

```csharp
public async Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync(
    CommonQueryParams commonQueryParams,
    int? instanceId)
{
    Expression<Func<EducationOrganization, object>> columnToOrderBy =
        _orderByColumns.GetColumnToOrderBy(commonQueryParams.OrderBy);

    var educationOrganizations = await _adminApiDbContext.EducationOrganizations
        .Where(e => instanceId == null || e.InstanceId == instanceId)
        .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
        .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
        .ToListAsync();

    return GroupEducationOrganizationsByInstance(educationOrganizations);
}
```

**Action 2**: Add helper method at the end of the class:

```csharp
private List<OdsInstanceWithEducationOrganizationsModel> GroupEducationOrganizationsByInstance(
    List<EducationOrganization> educationOrganizations)
{
    return educationOrganizations
        .GroupBy(e => new { e.InstanceId, e.InstanceName })
        .Select(group => new OdsInstanceWithEducationOrganizationsModel
        {
            Id = group.Key.InstanceId,
            Name = group.Key.InstanceName,
            InstanceType = "OdsInstance",
            EducationOrganizations = _mapper.Map<List<EducationOrganizationModel>>(group.ToList())
        })
        .OrderBy(i => i.Id)
        .ToList();
}
```

**Important**: Ensure `using EdFi.Ods.AdminApi.Features.EducationOrganizations;` is added at the top.

---

### Phase 3: API Endpoint Updates

#### Task 3.1: Update ReadEducationOrganizations Endpoints

**File**: `Application/EdFi.Ods.AdminApi/Features/EducationOrganizations/ReadEducationOrganizations.cs`

**Action**: Update both endpoint methods:

**Update return types and route options**:

```csharp
AdminApiEndpointBuilder
    .MapGet(endpoints, "/educationOrganizations", GetEducationOrganizations)
    .WithSummaryAndDescription(
        "Retrieves all education organizations grouped by ODS instance",
        "Returns all education organizations from all ODS instances in a nested structure"
    )
    .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
    .BuildForVersions(AdminApiVersions.V2);

AdminApiEndpointBuilder
    .MapGet(endpoints, "/educationOrganizations/{instanceId}", GetEducationOrganizationsByInstance)
    .WithSummaryAndDescription(
        "Retrieves education organizations for a specific ODS instance",
        "Returns all education organizations for the specified ODS instance in a nested structure"
    )
    .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
    .BuildForVersions(AdminApiVersions.V2);
```

**Methods remain mostly the same** - query return types will automatically match.

#### Task 3.2: Rename and Update Tenant Endpoint

**File**: `Application/EdFi.Ods.AdminApi/Features/Tenants/ReadTenants.cs`

**Action 1**: Update route mapping (line 37):

**Before**:

```csharp
AdminApiEndpointBuilder
    .MapGet(endpoints, "/tenants/{tenantName}/edOrgsByInstances", GetTenantEdOrgsByInstancesAsync)
    .BuildForVersions(AdminApiVersions.V2);
```

**After**:

```csharp
AdminApiEndpointBuilder
    .MapGet(endpoints, "/tenants/{tenantName}/OdsInstances/edOrgs", GetTenantEdOrgsByInstancesAsync)
    .BuildForVersions(AdminApiVersions.V2);
```

#### Task 3.3: Remove TenantEducationOrganizationModel

**File**: `Application/EdFi.Ods.AdminApi/Features/Tenants/TenantDetailModel.cs`

**Action 1**: Update `TenantOdsInstanceModel.EducationOrganizations` property (line 37):

**Before**:

```csharp
public List<TenantEducationOrganizationModel> EducationOrganizations { get; set; }
```

**After**:

```csharp
public List<EducationOrganizationModel> EducationOrganizations { get; set; }
```

**Action 2**: Delete entire `TenantEducationOrganizationModel` class (lines 46-66):

```csharp
[SwaggerSchema]
public class TenantEducationOrganizationModel  // ❌ DELETE THIS ENTIRE CLASS
{
    public int InstanceId { get; set; }
    public string InstanceName { get; set; }
    public long EducationOrganizationId { get; set; }
    public string NameOfInstitution { get; set; }
    public string? ShortNameOfInstitution { get; set; }
    public string Discriminator { get; set; }
    public long? ParentId { get; set; }

    public TenantEducationOrganizationModel()
    {
        InstanceName = string.Empty;
        NameOfInstitution = string.Empty;
        ShortNameOfInstitution = string.Empty;
        Discriminator = string.Empty;
    }
}
```

**Action 3**: Add using statement at top (if not present):

```csharp
using EdFi.Ods.AdminApi.Features.EducationOrganizations;
```

#### Task 3.4: Update TenantService

**File**: `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs`

**Action**: Update mapper call (line 137):

**Before**:

```csharp
odsInstance.EducationOrganizations = mapper.Map<List<TenantEducationOrganizationModel>>(edOrgs);
```

**After**:

```csharp
odsInstance.EducationOrganizations = mapper.Map<List<EducationOrganizationModel>>(edOrgs);
```

---

### Phase 4: Test Updates

#### Task 4.1: Update Unit Tests - ReadTenantsTest

**File**: `Application/EdFi.Ods.AdminApi.UnitTests/Features/Tenants/ReadTenantsTest.cs`

**Actions**:

1. Replace all `TenantEducationOrganizationModel` with `EducationOrganizationModel`
2. Update endpoint path in tests from `/edOrgsByInstances` to `/OdsInstances/edOrgs`
3. Remove `InstanceId` and `InstanceName` from test data

**Example** (lines 157-162):
**Before**:

```csharp
var educationOrganization = new TenantEducationOrganizationModel()
{
    InstanceId = 1,
    InstanceName = "instance name 1",
    NameOfInstitution = "name of institution 1",
    ShortNameOfInstitution = "short name of institution 1",
    Discriminator = "discriminator 1"
};
```

**After**:

```csharp
var educationOrganization = new EducationOrganizationModel()
{
    EducationOrganizationId = 100,
    NameOfInstitution = "name of institution 1",
    ShortNameOfInstitution = "short name of institution 1",
    Discriminator = "discriminator 1"
};
```

**Update path** (line 183):

```csharp
A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
```

#### Task 4.2: Update Unit Tests - TenantDetailModelTests

**File**: `Application/EdFi.Ods.AdminApi.UnitTests/Features/Tenants/TenantDetailModelTests.cs`

**Action**: Replace `TenantEducationOrganizationModel` with `EducationOrganizationModel` (line 32):

**Before**:

```csharp
var educationOrganization = new TenantEducationOrganizationModel()
{
    InstanceId = 1,
    InstanceName = "instance name 1",
    NameOfInstitution = "name of institution 1",
    ShortNameOfInstitution = "short name of institution 1",
    Discriminator = "discriminator 1"
};
```

**After**:

```csharp
var educationOrganization = new EducationOrganizationModel()
{
    EducationOrganizationId = 1,
    NameOfInstitution = "name of institution 1",
    ShortNameOfInstitution = "short name of institution 1",
    Discriminator = "discriminator 1"
};
```

#### Task 4.3: Update Unit Tests - TenantServiceTests

**File**: `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs`

**Action**: Update test around line 202, replace `TenantEducationOrganizationModel` with `EducationOrganizationModel`:

**Before**:

```csharp
var tenantEducationOrganizationModels = new List<TenantEducationOrganizationModel>
{
    new()
    {
        InstanceId = 101,
        InstanceName = "Test Instance",
        EducationOrganizationId = 100,
        NameOfInstitution = "Test School",
        ShortNameOfInstitution = "Test",
        Discriminator = "School"
    }
};

A.CallTo(() => _mapper.Map<List<TenantEducationOrganizationModel>>(A<List<EducationOrganization>>._)).Returns(tenantEducationOrganizationModels);
```

**After**:

```csharp
var educationOrganizationModels = new List<EducationOrganizationModel>
{
    new()
    {
        EducationOrganizationId = 100,
        NameOfInstitution = "Test School",
        ShortNameOfInstitution = "Test",
        Discriminator = "School"
    }
};

A.CallTo(() => _mapper.Map<List<EducationOrganizationModel>>(A<List<EducationOrganization>>._)).Returns(educationOrganizationModels);
```

#### Task 4.4: Update E2E Tests - Tenants Bruno Collection

**Primary Directory**: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/Tenants/`

**Legacy File** (if exists): `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Admin API E2E 2.0 - Tenants.postman_collection.json`

**Note**: Bruno is the preferred E2E testing tool. Migrate from Postman if tests haven't been migrated yet.

**Actions for Bruno Tests (.bru files)**:

1. Search for `/edOrgsByInstances` in all `.bru` files and replace with `/OdsInstances/edOrgs`
2. Update response schema expectations to match nested structure
3. Update test assertions in the `tests` section of `.bru` files
4. If legacy Postman tests exist, migrate them to Bruno format

**Bruno Test Pattern Example**:

```javascript
tests {
  test("Response structure is nested", () => {
    const instances = res.body;
    expect(instances).to.be.an('array');
    expect(instances[0]).to.have.property('id');
    expect(instances[0]).to.have.property('name');
    expect(instances[0]).to.have.property('educationOrganizations');
    expect(instances[0].educationOrganizations).to.be.an('array');
    // Verify no instanceId in education orgs
    expect(instances[0].educationOrganizations[0]).to.not.have.property('instanceId');
  });
}
```

#### Task 4.5: Create/Update E2E Tests - EducationOrganizations Bruno Collection

**Target Directory**: `Application/EdFi.Ods.AdminApi/E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/v2/EducationOrganizations/`

**Note**: EducationOrganizations tests may need to be created in Bruno if not yet migrated from Postman.

**Actions**:

1. Create Bruno test files (`.bru`) if not yet migrated from Postman
2. Update/Create test assertions to expect nested structure
3. Update schema validations:
   * Expect array of instances with `id`, `name`, `instanceType`, `educationOrganizations`
   * Verify education org objects don't have `instanceId` or `instanceName`
4. Add tests for empty `educationOrganizations` arrays

**Bruno File Structure**:

```
EducationOrganizations/
├── folder.bru
├── GET - EducationOrganizations.bru
├── GET - EducationOrganizations by Instance.bru
└── [other test files].bru
```

**Example Bruno Test** (GET - EducationOrganizations.bru):

```
get {
  url: {{API_URL}}/v2/educationOrganizations
  body: none
  auth: inherit
}

tests {
  test("Status code is 200", () => {
    expect(res.status).to.equal(200);
  });
  
  test("Response has nested structure", () => {
    const instances = res.body;
    expect(instances).to.be.an('array');
    if (instances.length > 0) {
      expect(instances[0]).to.have.property('id');
      expect(instances[0]).to.have.property('name');
      expect(instances[0]).to.have.property('instanceType');
      expect(instances[0]).to.have.property('educationOrganizations');
      expect(instances[0].educationOrganizations).to.be.an('array');
    }
  });
  
  test("Education orgs don't have instance fields", () => {
    const instances = res.body;
    if (instances.length > 0 && instances[0].educationOrganizations.length > 0) {
      const edOrg = instances[0].educationOrganizations[0];
      expect(edOrg).to.not.have.property('instanceId');
      expect(edOrg).to.not.have.property('instanceName');
      expect(edOrg).to.have.property('educationOrganizationId');
      expect(edOrg).to.have.property('nameOfInstitution');
    }
  });
}
```

#### Task 4.6: Update HTTP Test File

**File**: `docs/http/tenants.http`

**Action**: Update endpoint URL (line 43):

**Before**:

```http
GET {{adminapi_url}}/v2/tenants/tenant1/edOrgsByInstances
```

**After**:

```http
GET {{adminapi_url}}/v2/tenants/tenant1/OdsInstances/edOrgs
```

---

### Phase 5: Verification and Testing

#### Task 5.1: Run Unit Tests

```powershell
./build.ps1 UnitTest
```

**Expected**: All tests should pass with new structure.

#### Task 5.2: Manual Testing Checklist

* [ ] Test `GET /v2/educationorganizations` returns nested structure

* [ ] Test `GET /v2/educationorganizations/{instanceId}` returns nested structure for specific instance
* [ ] Test `GET /v2/tenants/{tenantName}/OdsInstances/edOrgs` returns nested structure
* [ ] Verify old endpoint `/edOrgsByInstances` is no longer accessible (should 404)
* [ ] Test with instance that has no education organizations (should return empty array)
* [ ] Test pagination and sorting still work correctly
* [ ] Test in multi-tenancy mode
* [ ] Test with multiple ODS instances

#### Task 5.3: Response Validation

Verify all responses match this structure:

```json
[
  {
    "id": 1,
    "name": "ODS Instance Name",
    "instanceType": "OdsInstance",
    "educationOrganizations": [
      {
        "educationOrganizationId": 255901,
        "nameOfInstitution": "School Name",
        "shortNameOfInstitution": "Short Name",
        "discriminator": "edfi.School",
        "parentId": 255900
      }
    ]
  }
]
```

---

## 🎨 Code Style Guidelines

**Follow existing patterns from `.github/copilot-instructions.md`**:

* Use file-scoped namespaces
* Add newline before opening braces
* Use `is null` / `is not null` for null checks
* Use `nameof()` for member references
* Keep nullable reference type conventions
* Use NUnit, Shouldly, and FakeItEasy for tests

---

## 📚 Reference Files

**Key files to understand the codebase**:

* `/docs/design/ADMINAPI-1357.md` - Feature requirements
* `/docs/design/Education-organization-Endpoints.md` - Endpoint documentation
* `.github/copilot-instructions.md` - Code style guide
* `.github/agent-skills.md` - Reusable development patterns (includes Bruno testing)
* `.editorconfig` - Formatting rules

**E2E Testing Resources**:

* Bruno CLI docs: <https://docs.usebruno.com/>
* Bruno tests location: `E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/`
* Environment config: `E2E Tests/V2/Bruno Admin API E2E 2.0 refactor/environments/local.bru`
* Bruno vs Postman migration guide: See `.github/agent-skills.md` - "Migrating from Postman to Bruno" section

**Similar patterns in codebase**:

* Look at other query classes in `Infrastructure/Database/Queries/` for query patterns
* Look at other feature endpoints in `Features/` for endpoint patterns
* Look at `AdminApiMappingProfile.cs` for AutoMapper patterns

---

## ⚠️ Common Pitfalls to Avoid

1. **Don't forget to add using statements** for new model namespaces
2. **Don't break existing V1 endpoints** - only modify V2
3. **Don't forget pagination** - grouping must work with pagination
4. **Test empty states** - instances with no ed orgs should return empty arrays
5. **Update ALL test files** - unit, integration, and E2E tests
6. **Verify AutoMapper configuration** - ensure mappings are correctly removing fields

---

## ✅ Definition of Done

* [ ] All model changes implemented
* [ ] All query logic updated with grouping
* [ ] All endpoints return nested structure
* [ ] Tenant endpoint renamed
* [ ] TenantEducationOrganizationModel removed
* [ ] AutoMapper configurations updated
* [ ] All unit tests passing
* [ ] All E2E Bruno tests updated and passing
* [ ] Legacy Postman tests migrated to Bruno (if applicable)
* [ ] HTTP test files updated
* [ ] No compilation errors or warnings
* [ ] Manual testing confirms expected behavior
* [ ] Empty instances return empty arrays (not null)
* [ ] Pagination and sorting work correctly
* [ ] Multi-tenancy scenarios tested

---

## 🚀 Future Considerations

This structure prepares for future migration:

* `/v2/educationorganizations` → `/v2/odsInstances/edOrgs`
* `/v2/educationorganizations/{instanceId}` → `/v2/odsInstances/{instanceId}/edOrgs`

The `OdsInstanceWithEducationOrganizationsModel` will be directly reusable for these future endpoints.

---

## 📞 Questions or Issues?

If you encounter issues during implementation:

1. Review the reference files listed above
2. Check existing similar implementations in the codebase
3. Ensure all using statements are added
4. Verify AutoMapper configurations
5. Run unit tests frequently to catch issues early

---

**Last Updated**: February 10, 2026  
**Feature Ticket**: ADMINAPI-1357  
**Target Version**: V2 API
