# AdminConsole Endpoints Removal Analysis and Design

## Overview

This document outlines the findings from the analysis phase (Phase 1) of the ticket ADM### Proposed Endpoint Structure

### New Endpoints

| Current Endpoint | Proposed Endpoint | Purpose |
|------------------|-------------------|---------|
| `/adminconsole/tenants` | `/v2/tenants` | List all tenants |
| `/adminconsole/tenants/{tenantId}` | `/v2/tenants/{tenantId}` | Get tenant details |
| `/adminconsole/odsInstances/{id}` | `/v2/odsInstances/{id}/metadata` | Get instance details |

## Current State Analysis

### Existing Endpoints

The following `/adminconsole` endpoints have been identified in the codebase:

| Endpoint | HTTP Method | Purpose |
|----------|-------------|---------|
| `/tenants` | GET | List all tenants |
| `/tenants/{tenantId}` | GET | Get tenant details by ID |
| `/odsInstances` | GET | List all ODS instances |
| `/odsInstances/{id}` | GET | Get instance details by ID |
| `/instances` | GET | List all instances for worker use |
| `/instances/{id}` | GET | Get instance details for worker use |
| `/instances` | POST | Create a new instance |
| `/instances` | PUT | Update an existing instance |
| `/instances` | DELETE | Delete an instance |
| `/instances/{instanceId}/completed` | POST | Mark instance creation as completed |
| `/instances/{instanceId}/deletefailed` | POST | Mark instance deletion as failed |
| `/instances/{instanceId}/renameFailed` | POST | Mark instance rename as failed |
| `/instances/{instanceId}/renamed` | POST | Mark instance rename as completed |
| `/instances/{instanceId}/deleted` | POST | Mark instance deletion as completed |
| `/healthcheck` | GET | Get health check information |
| `/healthcheck` | POST | Create a health check entry |

### Database Schema

The admin console functionality currently uses the following tables in the `adminconsole` schema:

1. `adminconsole.Instances` - Stores ODS instance information
   * Maps to the `Instance` entity model
   * Contains information about ODS instances including status, credentials, and metadata
   * References `dbo.OdsInstances` via the `OdsInstanceId` foreign key

2. `adminconsole.HealthChecks` - Stores health check information
   * Maps to the `HealthCheck` entity model
   * Used by the health check worker to track instance health

3. `adminconsole.OdsInstanceContexts` - Stores context information for ODS instances
   * Maps to the `OdsInstanceContext` entity model
   * Contains context key-value pairs for ODS instances

4. `adminconsole.OdsInstanceDerivatives` - Stores derivative information for ODS instances
   * Maps to the `OdsInstanceDerivative` entity model
   * Contains derivative type information for ODS instances

### Code Dependencies

#### Core Services

1. `IAdminConsoleTenantsService` - Manages tenant operations
   * Responsible for initializing and retrieving tenant information
   * Used by the tenant endpoints

2. `IAdminConsoleInstancesService` - Manages instance operations
   * Responsible for initializing instance data
   * Maps ODS instances to admin console instances

3. `IAdminConsoleInitializationService` - Handles initialization of admin console data
   * Initializes applications needed for the admin console

#### Worker Functionality (To Be Removed)

The codebase includes worker-specific functionality that will be removed as per the ticket requirements:

1. **Instance Management Worker**
   * Handles ODS instance creation, deletion, and renaming
   * Transitions instance status (Pending → Completed, Pending_Delete → Deleted, etc.)
   * Uses endpoints like `/instances/{instanceId}/completed` to report operation results

2. **Health Check Worker**
   * Monitors the health of ODS instances
   * Records health check results in the `adminconsole.HealthChecks` table
   * Uses health check endpoints to report monitoring results

This worker functionality will be tagged in Git before removal to allow for future restoration if needed.

## Endpoints Categorization

### Core API Endpoints (to be preserved and migrated)

1. **Tenant Management**
   * `/tenants` (GET) - Retrieve all tenants
   * `/tenants/{tenantId}` (GET) - Retrieve a specific tenant

2. **ODS Instance Management**
   * `/odsInstances` (GET) - Retrieve all ODS instances
   * `/odsInstances/{id}` (GET) - Retrieve a specific ODS instance

### Worker-Specific Endpoints (to be removed)

1. **Instance Status Management**
   * `/instances` (GET) - Get instances for worker use
   * `/instances/{id}` (GET) - Get specific instance for worker use
   * `/instances/{instanceId}/completed` (POST) - Used by worker processes
   * `/instances/{instanceId}/deletefailed` (POST) - Used by worker processes
   * `/instances/{instanceId}/renameFailed` (POST) - Used by worker processes
   * `/instances/{instanceId}/renamed` (POST) - Used by worker processes
   * `/instances/{instanceId}/deleted` (POST) - Used by worker processes

2. **Health Check Management**
   * `/healthcheck` (GET) - Used by health check worker
   * `/healthcheck` (POST) - Used by health check worker

## Tables to Be Migrated

### Approach for Table Migration

After analyzing the relationship between `[EdFi_Admin].[adminconsole].[Instances]` and `[EdFi_Admin].[dbo].[OdsInstances]`, we've determined that the additional information in the `adminconsole.Instances` table serves specific purposes not covered by the core `OdsInstances` table.

Additionally, we've identified that there is already duplication between the following tables:

* `adminconsole.OdsInstanceContexts` and `dbo.OdsInstanceContext`
* `adminconsole.OdsInstanceDerivatives` and `dbo.OdsInstanceDerivative`

Since we're removing worker functionality, we should leverage the existing tables in the `dbo` schema rather than creating redundant tables in the `adminapi` schema.

### Recommended Approach: Create a Single Extension Table

Instead of migrating all three tables from the `adminconsole` schema, we will:

1. Create a new extension table in the `adminapi` schema:

```sql
[EdFi_Admin].[adminapi].[InstanceMetadata]
```

1. Continue using the existing tables in the `dbo` schema:

```sql
[EdFi_Admin].[dbo].[OdsInstanceContext]
[EdFi_Admin].[dbo].[OdsInstanceDerivative]
```

The `InstanceMetadata` table will:

* Have a foreign key to `dbo.OdsInstances` (OdsInstanceId)
* Store tenant association information (TenantId, TenantName)
* Store API access information (BaseUrl, ResourceUrl, OAuthUrl, Credentials)
* Remove worker-specific status tracking fields
* Reference the existing context and derivative information in `dbo` schema

This approach provides a clean separation of concerns while eliminating redundancy in the database model.

### Tables to Create

1. `adminapi.InstanceMetadata`
   * Extends the core `dbo.OdsInstances` table with additional API configuration
   * Will not include worker-specific status fields
   * Will maintain foreign key relationship to `dbo.OdsInstances`

### Tables to Remove

Tables exclusively used by worker processes should be removed:

1. `adminconsole.Instances` - Will be replaced by `adminapi.InstanceMetadata`
2. `adminconsole.OdsInstanceContexts` - Redundant with `dbo.OdsInstanceContext`
3. `adminconsole.OdsInstanceDerivatives` - Redundant with `dbo.OdsInstanceDerivative`
4. `adminconsole.HealthChecks` - Specific to health check worker functionality

## Proposed Endpoint Structure

### Endpoints to Preserve

| Current Endpoint | Proposed Endpoint | Purpose |
|------------------|-------------------|---------|
| `/adminconsole/tenants` | `/v2/tenants` | List all tenants |
| `/adminconsole/tenants/{tenantId}` | `/v2/tenants/{tenantId}` | Get tenant details |
| `/adminconsole/odsInstances/{id}` | `/v2/odsInstances/{id}/metadata` | Get instance details with data that previously was coming from `/adminconsole/odsInstances/{id}` endpoint|

### Endpoints to Remove

The following endpoints are exclusively used by worker processes and should be removed:

1. `/adminconsole/instances` (GET) - Used by worker to get all instances
2. `/adminconsole/instances/{id}` (GET) - Used by worker to get instance details
3. `/adminconsole/instances` (POST) - Used to create new instance
4. `/adminconsole/instances` (PUT) - Used to update instance
5. `/adminconsole/instances` (DELETE) - Used to delete instance
6. `/adminconsole/instances/{instanceId}/completed` - Used to mark instance creation as completed
7. `/adminconsole/instances/{instanceId}/deletefailed` - Used to mark instance deletion as failed
8. `/adminconsole/instances/{instanceId}/renameFailed` - Used to mark instance rename as failed
9. `/adminconsole/instances/{instanceId}/renamed` - Used to mark instance rename as completed
10. `/adminconsole/instances/{instanceId}/deleted` - Used to mark instance deletion as completed
11. `/adminconsole/healthcheck` (GET) - Used to retrieve health check information
12. `/adminconsole/healthcheck` (POST) - Used to create health check entries

## Implementation Plan

### 1. Code Preservation Strategy

To ensure we can easily restore the worker functionality in the future:

1. Create separate, focused Pull Requests for each worker component:
   * PR #1: Remove the Instance Management Worker functionality
   * PR #2: Remove the Health Check Worker functionality

2. This approach has several advantages:
   * Each PR will represent a discrete, reversible change
   * We can use `git revert` on specific PRs to restore functionality when needed
   * Changes are more manageable and easier to review
   * Documentation of the removed functionality is preserved in the PR history

3. Document the PR numbers and purpose in the project documentation for future reference

### 2. Database Migration

1. Create the new extension table in the `adminapi` schema:
   * `adminapi.InstanceMetadata` (replacing functionality from `adminconsole.Instances`)
   * Ensure it has appropriate foreign key relationships to `dbo.OdsInstances`

2. Remove redundant tables:
   * Drop `adminconsole.Instances`, `adminconsole.OdsInstanceContexts`, and `adminconsole.OdsInstanceDerivatives`
   * Remove the `adminconsole.HealthChecks` table and any other worker-specific tables

### 3. Endpoint Updates

1. Preserve the approved endpoints by updating their routes:
   * `/tenants` → `/v2/tenants`
   * `/tenants/{tenantId}` → `/v2/tenants/{tenantId}`
   * `/odsInstances` → `/v2/odsInstances/{id}/metadata`
   * `/odsInstances/{id}` → `/v2/odsInstances/{id}/metadata`

2. Remove worker-specific endpoints:
   * `/instances` (GET)
   * `/instances/{id}` (GET)
   * `/instances/{instanceId}/completed`
   * `/instances/{instanceId}/deletefailed`
   * `/instances/{instanceId}/renameFailed`
   * `/instances/{instanceId}/renamed`
   * `/instances/{instanceId}/deleted`
   * `/healthcheck`

### 4. Code Cleanup

1. Remove worker-specific services and dependencies
2. Update entity models to reference the correct tables:
   * Create new model for `adminapi.InstanceMetadata`
   * Continue using existing models for `dbo.OdsInstanceContext` and `dbo.OdsInstanceDerivative`
3. Update AutoMapper profiles and other dependent code
4. Clean up any unused code related to worker functionality
5. Update dependency injection registrations

### 5. Test Updates

1. Update existing test projects:
   * `EdFi.Ods.AdminConsole.DBTests` - Contains numerous tests for instance management commands
     * Update command tests (CompleteInstance, AddInstance, DeleteInstance, etc.)
     * Update query tests (GetInstanceById, etc.)

2. Create new tests for the migrated endpoints:
   * Test `/v2/odsInstances/{id}/metadata` endpoints
   * Test `/v2/tenants` endpoints
   * Verify database interactions with the new `adminapi.InstanceMetadata` table

3. Update integration tests:
   * Modify tests that rely on `adminconsole` schema tables
   * Update tests to use the new endpoint paths
   * Ensure proper integration with the existing `/v2/odsInstances` endpoints
