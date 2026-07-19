# ODS Admin API Product Requirements Document for Version 2.4

> **Status**: in progress \
> **Version**: 2.4 \
> **Owner**: Stephen Fuqua \
> **Product**: Ed-Fi ODS Admin API \
> **Repository**: `Ed-Fi-Alliance-OSS/ODS-Admin-API` \
> **Jira**: `ADMINAPI`
> **Baseline PRD:** [Product Requirements Document: ODS Admin API 2.3](./PRD-ODS-Admin-API-2.3.md)

## 1. Product Overview

ODS Admin API v2.4 builds on the v2.3 release by creating or improving the
workflows administrators use to manage API client credentials, education
organizations, and database instances. In addition, it introduces a `v3` API
mode with updated Management API definitions that are more compatible with the
new Ed-Fi API v8 platform. This PRD is a release delta: it complements the v2.3
PRD rather than replacing the product overview, personas, enterprise
architecture, and baseline requirements already captured there.

This PRD focuses on end-user functionality only. It intentionally excludes
technical debt, bug fixes, security hardening tasks, infrastructure work,
dependency upgrades, automated tests, and documentation-only tickets unless they
directly define user-visible behavior. These tasks are important and are part of
the next release, but they will not be described in this document.

### 1.2 Release Objectives

#### Must Have Objectives

1. Manage multiple credential sets for an application without treating the
   application itself as the credential. (Previously, Application was treated as
   1-1 with Credential.)

   ```mermaid
   erDiagram
     direction LR
     Vendor ||--o{ Application : has
     Application ||--|{ Credentials : has
   ```

2. Create and manage database instances from a database template.
3. Provide endpoints for managing education organizations.
4. Define a new `v3` mode that is compatible with the technical design of the
   Ed-Fi API v8 platform (as represented in the Ed-Fi Configuration Management
   Service application, which also implements the Management API v3
   specification).

#### Should Have Objectives

1. Provide an API health check endpoint that provides record counts from the ODS
   databases.

### 1.3 Carried-Forward Product Context

The target markets, personas, and jobs to be done from the v2.3 PRD remain valid
for v2.4.

## 2. Jobs to Be Done

> [!TIP]
> The JTBD numbering continues from the PRD for v2.3.

### JTBD 1: Issue Credentials (Modified)

**Personas:** SEA System Administrator, Managed Service Provider System
Administrator, Operator, Ed-Fi Alliance Certification Manager

**Original User Story**: When supporting a new application integration to an
existing Ed-Fi API deployment, I want to perform basic CRUD operations for
Vendors, Applications, and Credentials, so that I can distribute OAuth
credentials ("key and secret") to the vendor.

_Augment the story above with this new one:_

**When** an application has _one or more API credential sets_, \
**I want** to view, create, edit, reset, deactivate, and delete those credential sets directly, \
**so that** I can (a) manage access without confusing credentials with the parent
application and (b) manually manage overlapping key/secret pairs for **key rotation** security.

**How ODS Admin API Helps:** The v2.3 PRD described a one-to-one relationship between an
application and credentials. Version 2.4 introduces explicit credential-level
management actions.

### JTBD 9: Education Organization Synchronization

**Personas:** SEA System Administrator, Managed Service Provider System
Administrator, Operator

**User Story**: **When** an ODS/API instance's education organization
structure changes (new schools, LEAs, and so on), \
**I want** Admin API to keep an up-to-date, queryable copy of each instance's
education organizations and let me trigger an immediate refresh when needed, \
**so that** I can view and audit education organization hierarchies without
querying the ODS databases directly.

**How ODS Admin API Helps:** Admin API periodically synchronizes education
organization data from each configured ODS instance into its own store, on an
administrator-configurable interval, and exposes read endpoints — grouped by
ODS instance and, for multi-tenant deployments, by tenant — plus an on-demand
refresh trigger for administrators who need current data immediately. Listings
also surface each instance's provisioning status so administrators can see
which database instances are not yet linked to an education organization
structure.

### JTBD 10: Database Record Health Check

TBD — no PR in this delta implements the record-count health check described
in Should Have Objective #1. Related but distinct discoverability
improvements (API specification version and tenancy status on the
Information endpoint) are captured under JTBD 12.

### JTBD 11: Database Instance Provisioning (New)

**Personas:** SEA System Administrator, Managed Service Provider System
Administrator, Operator

**User Story**: **When** onboarding a new district/LEA or standing up a new
environment, \
**I want** to create a new ODS database instance from an approved template
through the API, monitor its provisioning status, and later request its
removal, \
**so that** I don't have to run manual database-provisioning scripts or track
progress out of band.

**How ODS Admin API Helps:** Version 2.4 introduces a `/v2/dbinstances` /
`/v3/dbDataStores` (v3) resource that lets an administrator request creation of a
new database instance from a named template. Admin API validates the
instance name and template, then asynchronously provisions (or de-provisions)
the instance via a background job. Progress is visible through granular
lifecycle status values and a general-purpose job-status endpoint.

### JTBD 12: Tenant and Version Discovery (New)

**Personas:** SEA System Administrator, Managed Service Provider System
Administrator, Operator, Ed-Fi Alliance Certification Manager

**User Story**: **When** integrating with or operating an Admin API
deployment, \
**I want** to discover whether the deployment is running in multi-tenant
mode, which tenants it serves, and which Management API specification
version is active, \
**so that** I can configure my tooling correctly without guessing or relying
on out-of-band documentation.

**How ODS Admin API Helps:** The unauthenticated Information endpoint
(`GET /`) now reports multi-tenant mode, the list of configured tenant names,
and the active specification version (`v1`, `v2`, or `v3`). To reduce the
security exposure of this discoverability, tenant list/detail endpoints that
previously returned database connection strings have been removed.

## 3. Functional Requirements

> [!NOTE]
> Unless otherwise specified, functional requirements apply equally to the v2
> and v3 modes.

### Credentials (JTBD 1)

- **FR-CRED-1**: The v3 ApiClients endpoint SHALL expose the client
  credential identifier as `clientId` (replacing the legacy `key` field) and
  SHALL NOT return the deprecated `useSandbox` or `sandboxType` fields.

### Education Organizations (JTBD 9)

- **FR-EDORG-1**: The API SHALL periodically refresh cached education
  organization data for each configured ODS instance at an
  administrator-configurable interval (`EdOrgsRefreshIntervalInMins`).
- **FR-EDORG-2**: The API SHALL allow an administrator to retrieve education
  organizations grouped by their owning ODS instance, for all instances or a
  specific instance, via `GET /odsInstances/edOrgs` and
  `GET /odsInstances/{instanceId}/edOrgs` (v2) or the equivalent `dataStores`
  routes (v3).
- **FR-EDORG-3**: The API SHALL allow an administrator to trigger an
  on-demand refresh of education organization data for all ODS instances or a
  specific instance via `POST /odsInstances/edOrgs/refresh` and
  `POST /odsInstances/{instanceId}/edOrgs/refresh` (v2), or the equivalent v3
  routes.
- **FR-EDORG-4**: The API SHALL allow an administrator to retrieve education
  organizations grouped by ODS instance for a named tenant, and SHALL reject
  requests where the tenant name in the URL does not match the `Tenant`
  header in multi-tenant mode.
- **FR-EDORG-5**: Education organization listing responses SHALL include each
  associated database instance's provisioning status, database template, and
  database name, and SHALL include database instances that are not yet
  linked to an ODS instance when listing all instances.

### Database Instances (JTBD 11)

- **FR-DBINST-1**: The API SHALL allow an administrator to create a database
  instance from a named, approved database template via `POST /v2/dbinstances` /
  `POST /v3/dbDataStores`, validating the instance name and template before
  accepting the request.
- **FR-DBINST-2**: The API SHALL allow an administrator to list and retrieve
  database instances, including filtering the list by name or ID.
- **FR-DBINST-3**: The API SHALL provision and de-provision database
  instances asynchronously via a background job after a create or delete
  request is accepted, including tenant-aware scheduling in multi-tenant
  deployments.
- **FR-DBINST-4**: The API SHALL reject database instance names that are
  empty, contain characters other than letters, numbers, spaces, or
  underscores, duplicate an existing non-deleted database instance name (in
  either the new database instance registry or the ODS instance registry),
  or would produce a derived database name exceeding the portability length
  limit.
- **FR-DBINST-5**: The API SHALL allow an administrator to request deletion
  of a database instance. The instance SHALL be marked `PendingDelete` (soft
  delete) rather than removed immediately, and the request SHALL be rejected
  with a descriptive error if the instance is in a status that blocks
  deletion.
- **FR-DBINST-6**: The API SHALL expose granular status values for a
  database instance's creation and deletion lifecycle (for example
  `PendingCreate`, `CreateInProgress`, `CreateFailed`, `CreateError`,
  `PendingDelete`, `DeleteInProgress`, `Deleted`, `DeleteFailed`,
  `DeleteError`) so administrators can track provisioning progress and
  failures.

### Asynchronous Job Tracking

- **FR-JOB-1**: The API SHALL allow an administrator to query the status of an
  asynchronous background job (such as database instance provisioning or
  education organization refresh) by job ID via `GET /v2/jobs/{jobId}` or `GET
  /v3/jobs/{jobId}`, including when the job was created and, if applicable, when
  it finished.

### Tenants and Deployment Discovery (JTBD 12)

- **FR-TENANT-1**: The API SHALL NOT expose tenant database connection string
  details through any tenant list or detail endpoint.
- **FR-TENANT-2**: The unauthenticated Information endpoint (`GET /`) SHALL
  report multi-tenant mode status and the list of configured tenant names,
  across all API specification versions.
- **FR-TENANT-3**: The Information endpoint SHALL report the active API
  specification version (`specificationVersion`).

### v3 API Conventions (Objective 4)

- **FR-V3-0**: The v3 API mode SHALL duplicate the `v2` API mode except as
  described in the remaining functional requirements.
- **FR-V3-1**: The v3 API SHALL return an absolute URL (including scheme and
  host) in the `Location` header for resource-creation responses.
- **FR-V3-2**: The v3 API SHALL return `204 No Content` with an empty body
  for successful DELETE and PUT operations.
- **FR-V3-3**: The v3 API SHALL reject PUT requests where the resource ID is
  missing, zero, or does not match the ID supplied in the URL.
- **FR-V3-4**: The v3 API SHALL return error responses in RFC 7807 Problem
  Details format, including a `type` URN identifying the specific error category
  and a `detail` property describing the error. The `type` URN SHALL include
  `management-api` in the path.
- **FR-V3-5**: The v3 API SHALL name the ODS instance resource family
  `dataStores` and the database instance resource `dbDataStores`, including
  corresponding field names (for example `dataStoreId`, `dataStoreType`).

### Claim Sets / Security

- **FR-CLAIM-9**: The API SHALL reject requests to create, copy, edit, or
  import a claim set whose name contains whitespace.
- **FR-CLAIM-10**: The v3 API SHALL return claim set resource claims as a
  flat list with `claimName` and `parentClaimName` identifying hierarchy, and
  the claim set export endpoint SHALL return a payload identical in shape to
  the read endpoint.

## 4. Non-Functional Requirements

This release PRD only adds non-functional requirements that directly affect
end-user behavior for the v2.4 functionality.

- ***NFR-SDLC-6**: The application SHALL use the currently-supported framework
  SDK (.NET 10).

## 5. System Architecture Implications

- Admin API now runs a background job scheduler (Quartz) to perform
  asynchronous work — education organization refresh and database instance
  create/delete provisioning — outside the request/response cycle. Job
  progress and outcome are tracked in Admin API's own data store and exposed
  via the job-status endpoint (FR-JOB-1).
- Admin API maintains a local, periodically-synchronized copy of each ODS
  instance's education organization structure rather than querying the ODS
  databases live on every request.

## 6. Out of Scope

- New functionality for Ed-Fi ODS/API v6 / `v1` mode.

## 7. Glossary Additions

- **DataStore**: The v3 API name for what v1/v2 call an "ODS instance" — a
  single ODS/API database instance managed by Admin API.
- **DbInstance / DbDataStore**: An Admin API-managed record representing a
  database instance provisioned from a template, prior to (or in addition
  to) its registration as an ODS instance. Named `DbInstance` in v2 and
  `DbDataStore` in v3.
- **Problem Details**: The RFC 7807 standard error response format
  (`type`, `title`, `detail`, and related properties) used by the v3 API.
