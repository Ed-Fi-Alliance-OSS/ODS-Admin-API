
# Integration Design: EdFi.Ods.AdminApi.V1 with EdFi.Ods.AdminApi (V2)

## Overview

This document outlines the design for integrating EdFi.Ods.AdminApi.V1 endpoints
into the existing EdFi.Ods.AdminApi (V2) solution. The integration will maintain
backward compatibility while leveraging the enhanced architecture of V2.

### Goals

* Maintain backward compatibility for existing V1 API clients
* Leverage V2's enhanced architecture and infrastructure
* Minimize code duplication between versions
* Provide clear migration path for V1 to V2
* Centralize common functionality

### Integration Strategy

* **Phase 1**: Clean up and modernize V1 codebase
* **Phase 2**: Merge projects and consolidate infrastructure
* **Phase 3**: Implement unified endpoint mapping
* **Phase 4**: Testing and Validation
* **Phase 5**: V1/V2 Multi-Tenancy Integration Strategy
* **Phase 6**: Docker setup

---

## Phase 1: EdFi.Ods.AdminApi.V1 Cleanup and Modernization

### 1.1 Remove Legacy Security Components

**Objective**: Simplify V1 codebase by removing Ed-Fi ODS 5.3 compatibility and standardizing on V6.

**Tasks**:

* Remove `EdFi.SecurityCompatiblity53.DataAccess` dependency
* Remove `OdsSecurityVersionResolver` and related version detection logic
* Remove conditional service implementations (V53Service vs V6Service)
* Update all code flows to use only V6 services
* Rename project assemblies from `EdFi.Ods.AdminApi` to `EdFi.Ods.AdminApi.V1`

**Example Transformation**:

```csharp
// BEFORE: Version-dependent service resolution
private readonly IOdsSecurityModelVersionResolver _resolver;
private readonly EditResourceOnClaimSetCommandV53Service _v53Service;
private readonly EditResourceOnClaimSetCommandV6Service _v6Service;

public EditResourceOnClaimSetCommand(IOdsSecurityModelVersionResolver resolver,
    EditResourceOnClaimSetCommandV53Service v53Service,
    EditResourceOnClaimSetCommandV6Service v6Service)
{
    _resolver = resolver;
    _v53Service = v53Service;
    _v6Service = v6Service;
}

public void Execute(IEditResourceOnClaimSetModel model)
{
    var securityModel = _resolver.DetermineSecurityModel();
    switch (securityModel)
    {
        case EdFiOdsSecurityModelCompatibility.ThreeThroughFive or EdFiOdsSecurityModelCompatibility.FiveThreeCqe:
            _v53Service.Execute(model);
            break;
        case EdFiOdsSecurityModelCompatibility.Six:
            _v6Service.Execute(model);
            break;
        default:
            throw new EdFiOdsSecurityModelCompatibilityException(securityModel);
    }
}

// AFTER: Simplified V6-only implementation
public class EditResourceOnClaimSetCommand(EditResourceOnClaimSetCommandV6Service v6Service)
{
    private readonly EditResourceOnClaimSetCommandV6Service _v6Service = v6Service;
    
    public void Execute(IEditResourceOnClaimSetModel model)
    {
        _v6Service.Execute(model);
    }
}
```

### 1.2 Project Structure Standardization

**Objective**: Align V1 project structure with V2 conventions and dependency management.

**Tasks**:

* Convert V1 project to use `Directory.Packages.props` for version management
* Remove explicit version numbers from V1 project file package references
* Ensure V1 project builds successfully with V6-only dependencies
* Validate all unit tests pass after cleanup
  
---

## Phase 2: Project Merge and Infrastructure Consolidation

### 2.1 Eliminate Duplicate Infrastructure Classes

**Objective**: Consolidate common infrastructure components to reduce maintenance overhead.

**Classes to Consolidate** (merge from V1 to V2, then remove from V1):

* `AdminApiDbContext.cs` - Database context configuration
* `AdminApiEndpointBuilder.cs` - Endpoint registration patterns
* `AdminApiVersions.cs` - API versioning constants
* `CloudOdsAdminApp.cs` - Cloud deployment configurations  
* `CommonQueryParams.cs` - Shared query parameter models
* `DatabaseEngineEnum.cs` - Database engine enumeration
* `EndpointRouteBuilderExtensions.cs` - Route building extensions
* `Enumerations.cs` - Common enumerations
* `IMarkerForEdFiOdsAdminAppManagement.cs` - Assembly markers
* `InstanceContext.cs` - Instance context management
* `OdsSecurityVersionResolver.cs` - Security version resolution (remove from V1)
* `OperationalContext.cs` - Operational context management
* `ValidatorExtensions.cs` - Validation helper extensions
* `WebApplicationBuilderExtensions.cs` - Application builder extensions
* `WebApplicationExtensions.cs` - Application configuration extensions
* **Security folder and all classes** - Use V2 security implementation
* **Connect\Register and Connect\Token endpoints** - Use V2 implementation
* **Artifacts folder** - Remove from V1, use V2 artifacts
* **Information feature** - Remove from V1, use V2 implementation

### 2.2 DataAccess Layer Strategy

**Objective**: Maintain V1 compatibility by preserving Ed-Fi ODS 6.x DataAccess
implementations while supporting runtime mode switching with unified connection
strings.

**Tasks**:

* Copy Ed-Fi ODS 6.x Admin.DataAccess and Security.DataAccess code implementation files directly to AdminAPI V1 project
* Avoid handling divergence between 6.x and 7.x Security and Admin DataAccess usages while maintaining V1 API compatibility
  
* **Isolation Benefits**:
  * V1 maintains stable DataAccess layer independent of V2 upgrades
  * Eliminates version compatibility complexity in shared DataAccess components
  * Reduces risk of breaking V1 functionality when V2 adopts newer Ed-Fi ODS versions
  
### 2.3 Database Context Setup Strategy

* **Runtime Mode Configuration**: Add `adminApiMode` setting to control which version endpoints are active:

  ```json
  {
    "AppSettings": {
      "adminApiMode": "v1",  // or "v2"
      "MultiTenancy": false,
      "EnableAdminConsoleAPI": true
    },
    "ConnectionStrings": {
      // Single connection strings for both modes
      "EdFi_Admin": "Server=.;Database=EdFi_Admin;Integrated Security=true",
      "EdFi_Security": "Server=.;Database=EdFi_Security;Integrated Security=true"
    }
  }
  ```

* **Mode-Aware DbContext Registration**: Configure DbContext based on `adminApiMode`:

  ```csharp
  public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
  {
      var adminApiMode = configuration.GetValue<string>("AppSettings:adminApiMode")?.ToLower();
      var adminConnectionString = configuration.GetConnectionString("EdFi_Admin");
      var securityConnectionString = configuration.GetConnectionString("EdFi_Security");
      
      switch (adminApiMode)
      {
          case "v1":
              // Register V1 DbContext with 6.x DataAccess layer
              services.AddDbContext<AdminApiV1DbContext>(options =>
                  options.UseSqlServer(adminConnectionString));
              services.AddDbContext<SecurityV1DbContext>(options =>
                  options.UseSqlServer(securityConnectionString));
              break;
              
          case "v2":
              // Register V2 DbContext with 7.x DataAccess layer
              services.AddDbContext<AdminApiDbContext>(options =>
                  options.UseSqlServer(adminConnectionString));
              services.AddDbContext<SecurityDbContext>(options =>
                  options.UseSqlServer(securityConnectionString));
              break;
              
          default:
              throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}. Must be 'v1' or 'v2'");
      }
  }
  ```

### 2.4 Project Type Conversion

**Objective**: Convert V1 from standalone application to class library.

**Tasks**:

* Convert `EdFi.Ods.AdminApi.V1` project to class library type
* Remove `appsettings.json` files from V1 project
* Move V1-specific configuration to V2 project `appsettings.json`
* Move V1 E2E tests to V2 E2E tests folder structure

---

## Phase 3: Endpoint Mapping and API Versioning

### 3.1 Implement V1 Endpoint Mapping

**Objective**: Create unified endpoint registration.

**Implementation** (add to `WebApplicationExtensions.cs`):

```csharp
  public static void MapAdminApiEndpoints(this WebApplication app)
  {
      var adminApiMode = app.Configuration.GetValue<string>("AppSettings:adminApiMode")?.ToLower();
      
      // Always register unversioned endpoints
      app.MapConnectEndpoints();
      app.MapDiscoveryEndpoint();
      app.MapHealthCheckEndpoints();
      
      switch (adminApiMode)
      {
          case "v1":
              app.MapAdminApiV1FeatureEndpoints();
              break;
              
          case "v2":
              app.MapAdminApiV2FeatureEndpoints();
              app.MapAdminConsoleFeatureEndpoints();
              break;
              
          default:
              throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}");
      }
  }
```

### 3.2 Mode-Aware API Configuration

**Objective**: Configure information endpoint responses and implement endpoint filtering based on the `adminApiMode` setting, including validation middleware for version-specific endpoint access.

**Implementation**:

**Version-Aware Discovery Endpoint**: Update discovery/information response based on the mode:

```csharp
public class ReadInformation : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
      // Map endpoint implementation
    }
    internal static InformationResult GetInformation()
    {
        return adminApiMode switch
          {
              "v1" => new InformationResult
              {
                  Version = "1.1", 
                  Build = buildVersion                 
              },
              "v2" => new DiscoveryResponse
              {
                  Version = "2.3",
                  Build = buildVersion                
              },
              _ => throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}")
          };
    }
}
```

* **Mode Validation Middleware**: Return 400 errors for wrong endpoint usage:

  ```csharp
  public class AdminApiModeValidationMiddleware
  {
      // Constructor: Get adminApiMode from configuration (default: "v2")
      
      public async Task InvokeAsync(HttpContext context)
      {
          // Skip validation for unversioned endpoints (/connect/, /health, /.well-known/)
          if (IsUnversionedEndpoint(path)) 
              continue to next middleware;
          
          // Extract version from path (/v1/ or /v2/)
          var requestedVersion = GetVersionFromPath(path);
          
          // If requested version doesn't match configured mode
          if (requestedVersion != _adminApiMode)
          {
              // Return 400 with descriptive error message
              return BadRequest("Wrong API version for this instance mode");
          }
          
          // Continue to next middleware
          await _next(context);
      }
      
      // Helper: Check if endpoint is unversioned (auth, health, discovery)
      private static bool IsUnversionedEndpoint(string path) { ... }
      
      // Helper: Extract "v1" or "v2" from URL path
      private static string GetVersionFromPath(string path) { ... }
  }     
  
  ```

### 3.3 API Versioning Strategy

**URL Structure**:

* V1 endpoints: `/v1/applications`, `/v1/claimsets`, etc.
* V2 endpoints: `/v2/applications`, `/v2/claimsets`, etc.  
* Default (unversioned): Connect endpoints (Register and Token), discovery endpoint, and health check endpoints remain unversioned.

---

## Phase 4: Testing and Validation

**Objective**: Enhance test coverage for V1 project and ensure seamless integration with V2 test infrastructure.

**Tasks**:

* Add unit tests for uncovered areas using NUnit and Shouldly patterns consistent with V2

### 4.1 Integration Test Consolidation

**Objective**: Merge V1 integration tests with V2 test infrastructure while maintaining test isolation and version-specific database compatibility.

**Tasks**:

* **Consolidate Test Projects**: Merge V1 `*.DBTests` projects into V2 database testing infrastructure:
  * Move V1 integration tests to `EdFi.Ods.AdminApi.DBTests` project
  * Organize tests in version-specific namespaces: `EdFi.Ods.AdminApi.DBTests.V1` and `EdFi.Ods.AdminApi.DBTests.V2`
  * Maintain separate test base classes for V1 and V2 to handle different DbContexts
  * Ensure V1 and V2 tests use completely separate test databases

### 4.2 End-to-End Test Migration

**Objective**: Consolidate V1 E2E tests into V2 test structure while maintaining version-specific validation.

**Tasks**:

* **E2E Test Organization**: Move V1 E2E tests to V2 `E2E Tests` folder with version-specific subdirectories:

  ```md

  E2E Tests/
  ├── V1/
  │   ├── Applications/
  │   ├── ClaimSets/
  │   
  ├── V2/
  
  ```

  * Update V1 Postman collections to use `/v1/` URL prefix
  * Create combined collection supporting both V1 and V2 endpoints
  * Add version-specific environment variables
  * Test version routing (v1 vs v2 vs unversioned URLs)
  * Add tests that validate V1 and V2 can operate simultaneously without conflicts
  
## Phase 5: V1/V2 Multi-Tenancy Integration Strategy

**Objective**: Strategy for maintaining multi-tenancy support in V2 while ensuring V1 endpoints continue to work without multi-tenancy requirements during the integration of AdminAPI V1 and V2.

**Tasks**:

* **Define Version-Aware Multi-Tenancy Middleware**: Enhance
  `TenantResolverMiddleware` to detect `adminApiMode` and apply
  multi-tenancy rules accordingly.

```csharp

private readonly string _adminApiMode;

public TenantResolverMiddleware(RequestDelegate next, IConfiguration configuration)
{
    _adminApiMode = configuration.GetValue<string>("AppSettings:adminApiMode")?.ToLower() ?? "v2";
}
private static bool IsV1Mode(HttpContext context)
{
    return string.Equals(_adminApiMode, "v1", StringComparison.InvariantCultureIgnoreCase);
}

public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
   
        // Check if this is a V1 endpoint
        if (IsV1Mode(context))
        {
            // For V1 endpoints, skip multi-tenancy validation entirely
            await next.Invoke(context);
            return;
        }

     if (multiTenancyEnabled)
     {
     }
}

```

## Phase 6: Docker Setup

**Objective**: Update Docker files to support mode-based Admin API deployment with schema-specific database containers selected at build time.

**Tasks**:

* **Multiple Dockerfile Strategy**: Create separate v1 and v2 folders to organize version-specific Docker files:
  
```md

Docker/
├── dev.mssql.Dockerfile
├── dev.pgsql.Dockerfile
├── v1/
│   ├── db.mssql.admin.Dockerfile # 6.x database schema
│   ├── db.pgsql.admin.Dockerfile # 6.x database schema
│   └── Compose/
│       └── pgsql/
│           └── compose-build-dev.yml
└── v2/
    ├── db.mssql.admin.Dockerfile # 7.x database schema
    ├── db.pgsql.admin.Dockerfile # 7.x database schema
    └── Compose/
        └── pgsql/
            └── compose-build-dev.yml

```

* Update Build Stage for V1 Project Integration
**Files to modify**: dev.mssql.Dockerfile and dev.pgsql.Dockerfile
  
```docker

COPY --from=assets ./Application/NuGet.Config EdFi.Ods.AdminApi.V1/
COPY --from=assets ./Application/EdFi.Ods.AdminApi.V1 EdFi.Ods.AdminApi.V1/
```

* **Mode-Based Docker Compose Configuration**: Select appropriate database Dockerfile based on mode:

  **V1 Mode Compose** (`Docker/v1/Compose/pgsql/compose-build-dev.yml`):

  ```yaml
  services:
    db-admin:
      build:
        context: ../../../../
        dockerfile: Docker/v1/db.pgsql.admin.Dockerfile  # 6.x schema
      environment:
       

    adminapi:
      build:
        context: ../../../../
        dockerfile: Docker/dev.pgsql.Dockerfile  
      environment:
        AppSettings__adminApiMode: "v1"        
      depends_on:
        - db-admin
      container_name: ed-fi-adminapi-v1
  ```

  **V2 Mode Compose** (`Docker/v2/Compose/pgsql/compose-build-dev.yml`):

  ```yaml
  services:
    db-admin:
      build:
        context: ../../../../
        dockerfile: Docker/v2/db.pgsql.admin.Dockerfile  # 7.x schema
      environment:      

    adminapi:
      environment:
        AppSettings__adminApiMode: "v2"        
      depends_on:
        - db-admin
      container_name: ed-fi-adminapi-v2
  ```
