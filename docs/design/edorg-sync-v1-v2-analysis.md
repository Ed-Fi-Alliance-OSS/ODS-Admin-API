# EdOrg Sync Disabled for V1 API Mode: Analysis and Implementation Plan

## Executive Summary

This document analyzes the implications of disabling EdOrg Sync for the V1 version of the Admin API and provides implementation guidance for both the Admin API and Admin App v4 to support this architectural decision.

**Key Finding**: EdOrg Sync is architecturally bound to V2 features (multi-tenancy, tenant resolution) and cannot be simply "enabled" for V1 without significant refactoring. The current implementation is correct for the intended V1/V2 separation.

## Table of Contents

1. [Context and Background](#context-and-background)
2. [Current Implementation Analysis](#current-implementation-analysis)
3. [Architectural Differences: V1 vs V2](#architectural-differences-v1-vs-v2)
4. [Impact Analysis](#impact-analysis)
5. [Admin App v4 Considerations](#admin-app-v4-considerations)
6. [Recommendations and Implementation Plan](#recommendations-and-implementation-plan)
7. [Testing Strategy](#testing-strategy)

---

## Context and Background

### Overview

Admin API 2.3 implements both V1 and V2 specifications:
* **V1 Spec**: Maintains backward compatibility with Admin API 1.x, supporting ODS/API versions 3.4 through 6.1
* **V2 Spec**: Introduces new features including multi-tenancy, education organization management, and enhanced instance management

### Problem Statement

As of the current implementation, Education Organization (EdOrg) Sync functionality is disabled for V1 API mode. This decision affects:

1. How Admin App v4 operates when running against V1 endpoints
2. What features are available to V1 API consumers
3. Future feature development and API evolution

### Related Documentation

* [Education Organization Endpoints Design](./Education-organization-Endpoints.md)
* [V1 Integration Design](./INTEGRATE-ADMINAPI.V1.md)
* [Admin Console Support](./adminconsole/readme.md)
* [Legacy Support Documentation](../legacy-support.md)

---

## Current Implementation Analysis

### EdOrg Sync Gate in Program.cs

**Location**: [Program.cs](../../Application/EdFi.Ods.AdminApi/Program.cs#L98)

```csharp
if (adminApiMode == AdminApiMode.V2)
{
    if (double.TryParse(edOrgsRefreshIntervalInMins, out var refreshInterval))
    {
        if (isMultiTenancyEnabled)
        {
            // Schedule job per tenant
            foreach (var tenantName in tenantNames)
            {
                await QuartzJobScheduler.ScheduleJob<RefreshEducationOrganizationsJob>(
                    scheduler,
                    jobKey: jobKey,
                    jobData: jobData,
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(refreshInterval)
                );
            }
        }
        else
        {
            // Schedule single job (V2 without multi-tenancy)
            await QuartzJobScheduler.ScheduleJob<RefreshEducationOrganizationsJob>(
                scheduler,
                jobKey: new JobKey(JobConstants.RefreshEducationOrganizationsJobName),
                jobData: new Dictionary<string, object>(),
                startImmediately: false,
                interval: TimeSpan.FromMinutes(refreshInterval)
            );
        }
    }
}
```

**Analysis**: The EdOrg Sync scheduling is explicitly gated behind the V2 mode check. This is **by design**, not an oversight.

### EdOrg Sync Architecture

The Education Organization synchronization system consists of:

1. **Background Job**: `RefreshEducationOrganizationsJob`
   * Quartz.NET scheduled job
   * Runs on configured interval (default: 60 minutes)
   * Supports both single-tenant and multi-tenant modes
   * **Requires tenant context resolution** (V2 feature)

2. **Service Layer**: `EducationOrganizationService`
   * Connects to ODS instances dynamically
   * Queries education organizations from ODS databases
   * Persists data to `adminapi.EducationOrganizations` table
   * **Relies on tenant-specific DB context provider** (V2 feature)

3. **Database Schema**: `adminapi.EducationOrganizations` table
   * Stores consolidated EdOrg data across instances
   * Includes instance ID, tenant information
   * Refreshed periodically by background job

4. **REST Endpoints** (V2 only):
   * `GET /v2/educationOrganizations`
   * `GET /v2/educationOrganizations/{instanceId}`
   * `POST /v2/educationOrganizations/refresh`
   * `POST /v2/educationOrganizations/refresh/{instanceId}`

### Middleware Architecture

The request pipeline includes mode validation:

```csharp
// Program.cs
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<AdminApiModeValidationMiddleware>();

if (adminApiMode == AdminApiMode.V2)
    app.UseMiddleware<TenantResolverMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
```

**Key Point**: `TenantResolverMiddleware` is only enabled for V2, which is required for EdOrg Sync operations in multi-tenant scenarios.

---

## Architectural Differences: V1 vs V2

### Comparison Table

| Feature | V1 API Mode | V2 API Mode |
|---------|-------------|-------------|
| **Multi-Tenancy** | ❌ Not supported | ✅ Full support |
| **EdOrg Sync** | ❌ Disabled | ✅ Enabled |
| **EdOrg Endpoints** | ❌ Not available | ✅ Available |
| **Tenant Resolution Middleware** | ❌ Not enabled | ✅ Enabled |
| **Tenant-specific DbContext** | ❌ Not available | ✅ Available |
| **Vendors** | ✅ Available | ✅ Available |
| **Applications** | ✅ Available | ✅ Available |
| **ClaimSets** | ✅ Available | ✅ Available |
| **ODS Instances** | ✅ Available | ✅ Available |
| **ODS Instance Context** | ❌ Not available | ✅ Available |
| **ODS Instance Derivative** | ❌ Not available | ✅ Available |
| **Profiles** | ❌ Not available | ✅ Available |
| **Resource Claims** | ❌ Not available | ✅ Available |
| **Resource Claim Actions** | ❌ Not available | ✅ Available |
| **Tenants Management** | ❌ Not available | ✅ Available |

### V1 Available Endpoints

Based on code analysis, V1 provides the following features:

**Vendors**
* `GET /v1/vendors`
* `GET /v1/vendors/{id}`
* `POST /v1/vendors`
* `PUT /v1/vendors/{id}`
* `DELETE /v1/vendors/{id}`

**Applications**
* `GET /v1/applications`
* `GET /v1/applications/{id}`
* `GET /v1/vendors/{vendorId}/applications`
* `POST /v1/applications`
* `PUT /v1/applications/{id}`
* `DELETE /v1/applications/{id}`
* `POST /v1/applications/{id}/reset-credential`

**ClaimSets**
* `GET /v1/claimSets`
* `GET /v1/claimSets/{id}`
* `POST /v1/claimSets`
* `PUT /v1/claimSets/{id}`
* `DELETE /v1/claimSets/{id}`

**ODS Instances**
* `GET /v1/odsInstances`
* `GET /v1/odsInstances/{id}`
* `POST /v1/odsInstances`
* `PUT /v1/odsInstances/{id}`
* `DELETE /v1/odsInstances/{id}`

### V2 Additional Features

V2 includes all V1 features PLUS:

* Education Organizations (read, refresh)
* Tenants Management
* ODS Instance Context
* ODS Instance Derivative
* Profiles
* Resource Claims
* Resource Claim Actions

### Database Context Dependencies

**V1 Mode**:
* Uses `IUsersContext` (EdFi_Admin database)
* Uses `ISecurityContext` (EdFi_Security database)
* Single-tenant architecture

**V2 Mode**:
* Uses `IUsersContext` and `ISecurityContext`
* **Additionally uses `ITenantSpecificDbContextProvider`** for tenant isolation
* **Requires `IContextProvider<TenantConfiguration>`** for tenant resolution
* Multi-tenant architecture with isolated databases per tenant

---

## Impact Analysis

### 1. Technical Implications

#### Positive Aspects

✅ **Clean Separation of Concerns**
* V1 remains stable and unchanged
* V2 can evolve independently
* Reduces risk of breaking changes for V1 consumers

✅ **Reduced Complexity for V1**
* No unnecessary background jobs
* Simpler middleware pipeline
* Lower resource consumption

✅ **Explicit Feature Boundaries**
* Clear documentation of what's available in each version
* Easier to reason about system behavior

#### Challenges

⚠️ **No EdOrg Data in V1 Mode**
* Applications running in V1 mode cannot access education organization information
* Admin App v4 must handle the absence of EdOrg endpoints gracefully

⚠️ **Migration Path Complexity**
* Organizations upgrading from V1 to V2 must understand feature differences
* May require Admin App UI changes to support both modes

⚠️ **Data Consistency**
* V1 and V2 share same database tables (EdFi_Admin, EdFi_Security)
* But V2 adds `adminapi.EducationOrganizations` table that V1 doesn't populate

### 2. Admin App v4 Impact

The Admin App v4 is a **frontend-only application** that uses Admin API as its backend-for-frontend (BFF).

#### When Running Against V1 API

**Missing Features**:

1. **Education Organizations Browse/Search**
   * Cannot display EdOrg hierarchy
   * Cannot filter by education organization
   * Cannot show EdOrg metadata (type, name, parent)

2. **Tenant Management**
   * No multi-tenancy UI
   * No tenant selection/switching

3. **Advanced Instance Management**
   * No ODS Instance Context management
   * No ODS Instance Derivative management

4. **Advanced Security Features**
   * No Profiles management
   * No Resource Claims management
   * No granular Resource Claim Actions

#### When Running Against V2 API

**Full Feature Set**:
* All V1 features (vendors, applications, claimsets, instances)
* Plus all additional V2 features listed above

### 3. Consumer Impact

#### V1 API Consumers

- Must rely on direct ODS queries for education organization data
* Cannot leverage Admin API for EdOrg information
* May need to implement their own EdOrg caching/indexing

#### V2 API Consumers

- Full EdOrg data available via REST API
* Automatic background refresh
* Consolidated view across all ODS instances

---

## Admin App v4 Considerations

### Architectural Context

From [Admin Console Design](./adminconsole/readme.md):

> The Ed-Fi Admin App application is a front-end only. The Ed-Fi Admin API 2 application will act as the backend-for-frontend (BFF), serving all of the interaction needs for Admin App.

### Current State (as of analysis date)

According to the acceptance criteria, Admin App v4 has **not yet implemented** EdOrg synchronization. However, a future implementation is referenced:

**Planned Implementation**: [Admin App Commit 2505f9d](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-AdminApp/commit/2505f9d40d311df42f4a9a7979241e11ab6acc72)

### Recommended Admin App v4 Implementation Strategy

#### 1. API Mode Detection

Admin App should detect which API mode it's running against:

```typescript
// Pseudo-code example
interface ApiInformation {
  version: string;
  build: string;
  tenancy: 'single-tenant' | 'multi-tenant';
  apiMode: 'v1' | 'v2';
}

async function detectApiMode(): Promise<ApiInformation> {
  const response = await fetch('/information');
  return await response.json();
}
```

**Admin API Endpoint**: `/information` (unversioned)
* Returns API mode, version, build info
* Available in both V1 and V2 modes

#### 2. Feature Flags Based on API Mode

```typescript
interface FeatureFlags {
  educationOrganizations: boolean;
  multiTenancy: boolean;
  advancedInstanceManagement: boolean;
  profiles: boolean;
  resourceClaims: boolean;
}

function getFeatureFlagsForMode(mode: 'v1' | 'v2'): FeatureFlags {
  return {
    educationOrganizations: mode === 'v2',
    multiTenancy: mode === 'v2',
    advancedInstanceManagement: mode === 'v2',
    profiles: mode === 'v2',
    resourceClaims: mode === 'v2',
  };
}
```

#### 3. Conditional UI Rendering

**Navigation Menu**:

```typescript
function renderNavigation(features: FeatureFlags) {
  return (
    <nav>
      <NavItem to="/vendors">Vendors</NavItem>
      <NavItem to="/applications">Applications</NavItem>
      <NavItem to="/claimsets">Claim Sets</NavItem>
      <NavItem to="/instances">ODS Instances</NavItem>
      
      {features.educationOrganizations && (
        <NavItem to="/education-organizations">
          Education Organizations
        </NavItem>
      )}
      
      {features.multiTenancy && (
        <NavItem to="/tenants">Tenants</NavItem>
      )}
      
      {features.profiles && (
        <NavItem to="/profiles">Profiles</NavItem>
      )}
    </nav>
  );
}
```

**Application Forms**:

```typescript
function ApplicationForm({ features }: { features: FeatureFlags }) {
  return (
    <form>
      <Input name="vendorId" label="Vendor" required />
      <Input name="claimSetName" label="Claim Set" required />
      
      {features.educationOrganizations && (
        <EducationOrganizationSelector
          label="Education Organizations"
          multiSelect
        />
      )}
      
      {features.profiles && (
        <ProfileSelector
          label="Profiles"
          multiSelect
        />
      )}
      
      {/* Other form fields */}
    </form>
  );
}
```

#### 4. Graceful Degradation

When EdOrg endpoints are not available:

```typescript
async function fetchEducationOrganizations() {
  try {
    const response = await fetch('/v2/educationOrganizations');
    if (response.status === 400) {
      // API mode mismatch - running against V1
      return { available: false, data: [] };
    }
    const data = await response.json();
    return { available: true, data };
  } catch (error) {
    console.error('EdOrg fetch failed:', error);
    return { available: false, data: [] };
  }
}
```

#### 5. Error Messages and User Guidance

```typescript
function EducationOrganizationsPage({ features }: { features: FeatureFlags }) {
  if (!features.educationOrganizations) {
    return (
      <InfoBanner type="info">
        <h3>Education Organizations Not Available</h3>
        <p>
          This Admin API instance is running in V1 mode, which does not
          support Education Organization management.
        </p>
        <p>
          To access this feature, please upgrade to V2 API mode or contact
          your system administrator.
        </p>
        <Link to="/documentation">Learn more about API versions</Link>
      </InfoBanner>
    );
  }
  
  return <EducationOrganizationsList />;
}
```

---

## Recommendations and Implementation Plan

### For Admin API

#### ✅ No Changes Required to Current Implementation

The current implementation is **correct** for the following reasons:

1. **Architectural Consistency**: EdOrg Sync depends on V2-specific features (tenant resolution, tenant-specific DB contexts)
2. **Resource Efficiency**: V1 consumers don't need the overhead of EdOrg sync jobs
3. **Clear API Contract**: V1 and V2 have explicit, documented differences

#### Recommended Documentation Updates

1. **Update README.md**
   * Add clear section on V1 vs V2 feature differences
   * Link to comprehensive comparison table

2. **Update OpenAPI/Swagger Documentation**
   * Ensure V1 and V2 endpoints are clearly separated
   * Add schema descriptions noting version availability

3. **Update TechDocs**
   * Migration guide: V1 → V2
   * Feature availability matrix
   * Code examples for both versions

4. **Configuration Documentation**
   * Document `AppSettings:AdminApiMode` setting
   * Explain when to use V1 vs V2
   * Document multi-tenancy requirements for V2

#### Sample Configuration Documentation

Create `docs/configuration.md`:

```markdown
## AdminApiMode Configuration

### Setting

```json
{
  "AppSettings": {
    "adminApiMode": "v1"  // or "v2"
  }
}
```

### V1 Mode

**When to Use**:
* Supporting legacy ODS/API versions (3.4 - 6.1)
* Single-tenant deployments
* Backward compatibility required

**Features Available**: Vendors, Applications, Claim Sets, ODS Instances

**Features NOT Available**: Education Organizations, Multi-Tenancy, Profiles, Resource Claims

### V2 Mode

**When to Use**:
* ODS/API version 7.0+
* Multi-tenant deployments
* Need Education Organization management

**Features Available**: All V1 features PLUS Education Organizations, Multi-Tenancy, etc.

**Requirements**:
* `MultiTenancy` setting (true/false)
* `EdOrgsRefreshIntervalInMins` for background sync

```

### For Admin App v4

#### Implementation Checklist

##### Phase 1: API Mode Detection (High Priority)
- [ ] Implement `/information` endpoint call on app initialization
- [ ] Parse and store API mode configuration
- [ ] Create feature flag service based on detected mode
- [ ] Add mode indicator to UI (e.g., header badge)

##### Phase 2: Conditional Feature Rendering (High Priority)
- [ ] Update navigation to conditionally show V2-only features
- [ ] Add feature guards to all V2-specific pages
- [ ] Implement graceful degradation for missing features
- [ ] Show appropriate user messages when features unavailable

##### Phase 3: Form Updates (Medium Priority)
- [ ] Update Application forms to conditionally show EdOrg selector
- [ ] Update Application forms to conditionally show Profile selector
- [ ] Ensure form validation works in both modes
- [ ] Test form submission with and without optional V2 fields

##### Phase 4: EdOrg Synchronization (When Admin App Implements EdOrg Management)
- [ ] Implement EdOrg list/search UI (V2 only)
- [ ] Implement EdOrg hierarchy visualization (V2 only)
- [ ] Add EdOrg filtering to relevant pages (V2 only)
- [ ] Implement manual refresh trigger UI

##### Phase 5: Testing
- [ ] Test all features in V1 mode
- [ ] Test all features in V2 mode (single-tenant)
- [ ] Test all features in V2 mode (multi-tenant)
- [ ] Test mode switching (if supported)
- [ ] Verify error handling for API version mismatches

##### Phase 6: Documentation
- [ ] User guide: Understanding API versions
- [ ] Installation guide: Choosing V1 vs V2
- [ ] Migration guide: Moving from V1 to V2
- [ ] Troubleshooting: Common API mode issues

#### Sample Admin App Code Structure

```

src/
├── services/
│   ├── api/
│   │   ├── adminApi.ts
│   │   ├── apiModeDetection.ts
│   │   └── featureFlags.ts
│   └── ...
├── components/
│   ├── common/
│   │   ├── FeatureGuard.tsx
│   │   ├── ApiModeIndicator.tsx
│   │   └── FeatureUnavailableMessage.tsx
│   ├── navigation/
│   │   └── MainNavigation.tsx  // Conditional rendering
│   └── ...
├── pages/
│   ├── vendors/
│   ├── applications/
│   ├── claimsets/
│   ├── instances/
│   ├── educationOrganizations/  // V2 only
│   │   ├── index.tsx
│   │   └── EducationOrganizationsList.tsx
│   ├── tenants/  // V2 only
│   └── ...
└── ...

```

**Key Components**:

1. **apiModeDetection.ts**: Detects and stores API mode
2. **featureFlags.ts**: Determines available features based on mode
3. **FeatureGuard.tsx**: HOC that conditionally renders based on feature availability
4. **ApiModeIndicator.tsx**: Shows current API mode in header
5. **FeatureUnavailableMessage.tsx**: Displays when feature not available in current mode

---

## Testing Strategy

### Admin API Testing

#### Unit Tests
- ✅ Existing tests already validate V2-only endpoint registration
- ✅ Existing tests validate middleware pipeline for V1 vs V2
- 📝 **Add**: Test that EdOrg sync job is NOT scheduled in V1 mode
- 📝 **Add**: Test that `/v1/educationOrganizations` returns 400

#### Integration Tests
- ✅ Existing tests validate EdOrg sync in V2 mode
- 📝 **Add**: Test Admin API startup in V1 mode (no EdOrg job scheduled)
- 📝 **Add**: Test Admin API startup in V2 mode (EdOrg job scheduled)
- 📝 **Add**: Test API mode validation middleware with V1/V2 endpoints

#### E2E Tests
- 📝 **Add**: Complete V1 workflow (vendors → applications → reset credentials)
- 📝 **Add**: Complete V2 workflow including EdOrg operations
- 📝 **Add**: Test switching API mode (if supported)

### Admin App Testing

#### Unit Tests
- Test feature flag service with V1 mode
- Test feature flag service with V2 mode
- Test conditional component rendering

#### Integration Tests
- Test API mode detection
- Test navigation renders correctly for V1 mode
- Test navigation renders correctly for V2 mode
- Test forms work with and without EdOrg fields

#### E2E Tests (Cypress/Playwright)
```typescript
describe('Admin App with V1 API', () => {
  it('should hide EdOrg navigation item', () => {
    cy.visit('/');
    cy.get('nav').should('not.contain', 'Education Organizations');
  });
  
  it('should show feature unavailable message on EdOrg page', () => {
    cy.visit('/education-organizations');
    cy.contains('Education Organizations Not Available').should('be.visible');
  });
  
  it('should complete vendor and application creation without EdOrg', () => {
    // Test workflow without EdOrg selector
  });
});

describe('Admin App with V2 API', () => {
  it('should show EdOrg navigation item', () => {
    cy.visit('/');
    cy.get('nav').should('contain', 'Education Organizations');
  });
  
  it('should display education organizations list', () => {
    cy.visit('/education-organizations');
    cy.get('[data-testid="edorg-list"]').should('be.visible');
  });
  
  it('should allow EdOrg selection in application form', () => {
    cy.visit('/applications/new');
    cy.get('[data-testid="edorg-selector"]').should('be.visible');
  });
});
```

### Database State Tests

- Verify `adminapi.EducationOrganizations` table is empty when running in V1 mode
* Verify table populates when switching to V2 mode and running refresh job

---

## Risk Assessment

### Low Risk ✅

- **V1 API Consumers**: No breaking changes; everything continues to work
* **Database Schema**: No schema changes required
* **Backward Compatibility**: V1 behavior unchanged

### Medium Risk ⚠️

- **Admin App UX**: Users may be confused by missing features if not properly communicated
  * **Mitigation**: Clear documentation, in-app messaging, mode indicator
* **Documentation Lag**: Users may not understand V1 vs V2 differences
  * **Mitigation**: Comprehensive documentation updates, migration guides

### High Risk ❌

None identified.

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| Current | Keep EdOrg Sync disabled for V1 | Architectural dependency on V2 features (multi-tenancy, tenant resolution) makes it impractical to enable for V1 |
| Current | No code changes required to Admin API | Current implementation is correct and well-architected |
| Future | Admin App must implement mode detection | Admin App needs to gracefully handle feature availability differences |
| Future | Document V1 vs V2 differences clearly | Users need to understand what features are available in each mode |

---

## Appendix

### A. Configuration Examples

#### V1 Mode Configuration

```json
{
  "AppSettings": {
    "adminApiMode": "v1",
    "DatabaseEngine": "SqlServer",
    "MultiTenancy": false
  },
  "ConnectionStrings": {
    "EdFi_Admin": "Server=.;Database=EdFi_Admin;Integrated Security=True",
    "EdFi_Security": "Server=.;Database=EdFi_Security;Integrated Security=True"
  }
}
```

#### V2 Mode (Single-Tenant) Configuration

```json
{
  "AppSettings": {
    "adminApiMode": "v2",
    "DatabaseEngine": "SqlServer",
    "MultiTenancy": false,
    "EdOrgsRefreshIntervalInMins": 60
  },
  "ConnectionStrings": {
    "EdFi_Admin": "Server=.;Database=EdFi_Admin;Integrated Security=True",
    "EdFi_Security": "Server=.;Database=EdFi_Security;Integrated Security=True"
  }
}
```

#### V2 Mode (Multi-Tenant) Configuration

```json
{
  "AppSettings": {
    "adminApiMode": "v2",
    "DatabaseEngine": "SqlServer",
    "MultiTenancy": true,
    "EdOrgsRefreshIntervalInMins": 60
  },
  "Tenants": {
    "tenant1": {
      "ConnectionStrings": {
        "EdFi_Security": "Server=.;Database=EdFi_Security_Tenant1;Integrated Security=True",
        "EdFi_Admin": "Server=.;Database=EdFi_Admin_Tenant1;Integrated Security=True"
      }
    },
    "tenant2": {
      "ConnectionStrings": {
        "EdFi_Security": "Server=.;Database=EdFi_Security_Tenant2;Integrated Security=True",
        "EdFi_Admin": "Server=.;Database=EdFi_Admin_Tenant2;Integrated Security=True"
      }
    }
  }
}
```

### B. API Endpoint Availability Matrix

| Endpoint | V1 | V2 |
|----------|----|----|
| `GET /vendors` | ✅ | ✅ |
| `POST /vendors` | ✅ | ✅ |
| `PUT /vendors/{id}` | ✅ | ✅ |
| `DELETE /vendors/{id}` | ✅ | ✅ |
| `GET /applications` | ✅ | ✅ |
| `POST /applications` | ✅ | ✅ |
| `PUT /applications/{id}` | ✅ | ✅ |
| `DELETE /applications/{id}` | ✅ | ✅ |
| `POST /applications/{id}/reset-credential` | ✅ | ✅ |
| `GET /claimSets` | ✅ | ✅ |
| `POST /claimSets` | ✅ | ✅ |
| `PUT /claimSets/{id}` | ✅ | ✅ |
| `DELETE /claimSets/{id}` | ✅ | ✅ |
| `GET /odsInstances` | ✅ | ✅ |
| `POST /odsInstances` | ✅ | ✅ |
| `PUT /odsInstances/{id}` | ✅ | ✅ |
| `DELETE /odsInstances/{id}` | ✅ | ✅ |
| `GET /educationOrganizations` | ❌ | ✅ |
| `GET /educationOrganizations/{instanceId}` | ❌ | ✅ |
| `POST /educationOrganizations/refresh` | ❌ | ✅ |
| `POST /educationOrganizations/refresh/{instanceId}` | ❌ | ✅ |
| `GET /tenants` | ❌ | ✅ |
| `GET /odsInstanceContexts` | ❌ | ✅ |
| `POST /odsInstanceContexts` | ❌ | ✅ |
| `PUT /odsInstanceContexts/{id}` | ❌ | ✅ |
| `DELETE /odsInstanceContexts/{id}` | ❌ | ✅ |
| `GET /odsInstanceDerivatives` | ❌ | ✅ |
| `POST /odsInstanceDerivatives` | ❌ | ✅ |
| `PUT /odsInstanceDerivatives/{id}` | ❌ | ✅ |
| `DELETE /odsInstanceDerivatives/{id}` | ❌ | ✅ |
| `GET /profiles` | ❌ | ✅ |
| `PUT /profiles/{id}` | ❌ | ✅ |
| `GET /resourceClaims` | ❌ | ✅ |
| `GET /resourceClaimActions` | ❌ | ✅ |

### C. References

1. [Admin API Repository](https://github.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x)
2. [Admin App Repository](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-AdminApp)
3. [Admin App EdOrg Sync Commit](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-AdminApp/commit/2505f9d40d311df42f4a9a7979241e11ab6acc72)
4. [Ed-Fi ODS/API Documentation](https://techdocs.ed-fi.org)
5. [Admin API TechDocs](https://techdocs.ed-fi.org/display/ADMINAPI)

### D. Glossary

* **Admin API**: REST API for managing Ed-Fi ODS/API platform instances
* **Admin App**: Web-based UI for managing Ed-Fi deployments (front-end only)
* **EdOrg**: Education Organization (schools, districts, LEAs, etc.)
* **EdOrg Sync**: Background job that periodically refreshes education organization data from ODS instances
* **V1 API Mode**: Backward-compatible mode supporting ODS/API 3.4-6.1, limited features
* **V2 API Mode**: Modern mode supporting ODS/API 7.0+, full feature set including multi-tenancy
* **Multi-Tenancy**: Architecture supporting multiple isolated tenants sharing the same Admin API instance
* **BFF**: Backend-for-Frontend, a pattern where an API serves a specific UI application

---

## Change History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2024-02-26 | Analysis Agent | Initial analysis document created |

---

**Document Status**: ✅ Complete - Ready for Review
