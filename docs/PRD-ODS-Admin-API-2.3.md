# Product Requirements Document: ODS Admin API 2.3.1

> Status: completed \
> Owner: Stephen Fuqua \
> Product: Ed-Fi ODS Admin API \
> Repository: `Ed-Fi-Alliance-OSS/ODS-Admin-API` \
> Version: 2.3.1

Primary audiences: Ed-Fi platform hosts, system administrators, DevOps engineers, and Admin App integrators

## 1. Product overview

The ODS Admin API exists because platform hosts need a programmatic, automatable way to administer Ed-Fi ODS/API configuration without directly manipulating `EdFi_Admin` and `EdFi_Security` database tables. The product serves as a headless administrative control plane by exposing REST endpoints for managing vendors, applications, API clients, profiles, claim sets, ODS instances, ODS instance contexts, and authorization metadata used by the Ed-Fi ODS/API.

Current functionality:

- The API can run in `AppSettings:AdminApiMode` `v1` or `v2`; the configured mode controls which feature set is registered.
- v2 is the current API mode and exposes `/v2/*` endpoints for the current administrative model.
- v1 support remains in source for compatibility but differs from v2 in model shape and some credential reset behavior.
- The application exposes an anonymous `GET /` information endpoint that returns version and build metadata for the active mode.

## 1.1 Strategic alignment

ODS Admin API serves the Ed-Fi community by making ODS/API administration repeatable, auditable, and scriptable. This matters because education agencies and platform hosts operate ODS/API installations across environments, database engines, vendors, and tenants. A documented API is safer than ad hoc SQL changes and better aligned with automation, CI/CD, and least-privilege operational practices.

Current-state alignment:

- The API supports ODS/API 7.x and greater as the primary target for the 2.x line.
- Legacy support is preserved through the v1 code path and compatibility documentation for ODS/API 6.x.
- Docker documentation supports local development and testing for PostgreSQL and SQL Server-oriented deployments, including single-tenant and multi-tenant compose variants.

## 1.2 Target users and personas

Admin API is a backend service. Most of the personas below reach it indirectly, through Admin App or through their own automation, rather than through a UI of its own. Persona definitions for administrator, operator, and vendor-side roles are adopted from the Admin App PRD (`Ed-Fi-AdminApp/docs/PRD-AdminApp-v4.0.md`) so the two products describe the same people consistently. A few personas below are specific to direct use of Admin API and have no Admin App equivalent.

#### Platform host system administrator

Consider a system administrator at a state education agency (SEA) or managed service provider (MSP) whose primary mission is to collect LEA data for mandatory state reporting, or to operate Ed-Fi deployments on behalf of multiple education organizations. This administrator is in a hybrid IT role, serving both as a programmer and an IT administrator, responsible for deployment and maintenance of the Ed-Fi Technology Suite. MSP administrators (a category that also covers Ed-Fi Alliance staff managing certification demonstration environments) have a wider scope of responsibility, spanning multiple SEAs, LEAs, and Data Hubs.

**Primary motivations**

- Configure vendors, applications, client credentials, claim sets, profiles, and ODS instances needed to operate a deployment.
- Get in and out of administrative tasks quickly, whether working directly against Admin API or through Admin App.

**Technical depth**

- Broad, but not deep, responsibilities spanning programming, data engineering, deployment, and technical support.
- Skills rooted in support and deployment of .NET applications, using SQL Server or PostgreSQL, hosted on Windows/IIS, Linux, or Docker.

**Key challenges**

- Lack of time for professional development; prefers automatable, scriptable administration over manual database edits.

#### Operator

The Operator may be an IT-oriented or business-oriented employee at the hosting organization (MSP, SEA, Ed-Fi Alliance) or at the organization for whom the deployment is being managed (Data Hub, LEA). They are delegated users responsible for onboarding vendors and applications and managing credentials — typically through Admin App, but sometimes directly against Admin API for automation.

**Primary motivations**

- Create and manage vendors, applications, API clients, profile associations, ODS instance associations, and education organization access grants for integrations that submit data on behalf of an LEA.
- Get in and out of the task quickly, back to other pressing concerns.

**Technical depth**

- Skilled at using web-based applications or calling documented REST APIs, but not at deploying or managing infrastructure.

**Key challenges**

- Depends on others for management of the underlying infrastructure and database engines.

#### Vendor Application Administrator

Typically, an LEA has an application operator who manages the LEA's deployment of the third-party application that integrates with the Ed-Fi API, with direct responsibility for entering client credentials into that application. This persona does not call Admin API directly; they receive the `client_id`/`client_secret` that Admin API generates, delivered through Admin App or another distribution mechanism.

**Primary motivations**

- Receive client credentials for connecting to a given Ed-Fi API deployment.
- Keep those credentials safe, so malicious actors cannot perform illicit actions with them.

**Technical depth**

- Skilled at using web-based applications, but not at deploying or managing infrastructure.

#### DevOps engineer

Deploys Admin API into Docker, IIS, or hosted environments; configures database engines, connection strings, signing keys, path bases, logging, rate limiting, Swagger, and tenant settings. Unlike the personas above, this persona is responsible for the Admin API service itself, not only the data it manages.

#### Admin App integrator

Uses Admin API as a backend service for UI-driven administration, especially around tenants, applications, credentials, and health-oriented operational views. This is the Admin App development team itself, consuming Admin API's REST surface.

#### Security administrator

Reviews and modifies claim sets, resource claim actions, and authorization strategy overrides directly through Admin API. This persona exists because, as of Admin App v4.0, Admin App only supports viewing, exporting, and importing claim sets — an in-app claim set editor is on Admin App's roadmap — so detailed claim-set editing currently requires direct use of Admin API.

#### Developer or implementer

Uses Swagger, HTTP examples, E2E tests, and DB tests to validate API behavior during extension or upgrade work.

## 1.3 Jobs to be done

JTBD 1 and JTBD 5 describe jobs shared with Admin App; the language is aligned with the Admin App PRD so both documents describe the same underlying job consistently. The remaining entries are specific to direct or automated use of Admin API.

### JTBD 1: Issue Credentials

**Personas**: all

**When** supporting a new application integration to an existing Ed-Fi API deployment, \
**I want** to perform basic CRUD operations for Vendors, Applications, API Clients, and Credentials, \
**so that** I can distribute OAuth credentials ("key and secret") to the vendor.

**How Admin API Helps**: Exposes REST endpoints for the full CRUD lifecycle of vendors, applications, and API clients, generating a `client_id`/`client_secret` pair on creation (FR-VENDOR-1, FR-APP-1, FR-APP-5, FR-CLIENT-1).

**Variations**: Resetting (rotating) credentials for an existing application or API client preserves the key and generates a new secret, so access can continue without re-onboarding (FR-APP-10, FR-CLIENT-6).

### JTBD 2: Register Administrative Automation Clients

**Personas**: Platform host system administrator, DevOps engineer, Admin App integrator

**When** standing up an Ed-Fi ODS/API deployment, \
**I want** to register an administrative API client and obtain a token, \
**so that** automation — including Admin App — can call protected Admin API endpoints.

**How Admin API Helps**: Supports OAuth 2.0 client-credentials token issuance (`POST /connect/token`) and self-contained client registration (`POST /connect/register`) when enabled (FR-AUTH-1, FR-AUTH-2).

### JTBD 3: Configure ODS Instances

**Personas**: Platform host system administrator, DevOps engineer, Admin App integrator (on behalf of system administrators)

**When** launching a school year database ("instance") for a tenant deployment, \
**I want** to create and manage ODS instance, context, and derivative records, \
**so that** applications can be associated with the correct database instance.

**How Admin API Helps**: Provides CRUD endpoints for ODS instances, ODS instance contexts, and ODS instance derivatives (FR-ODS-1 through FR-ODS-10).

> [!NOTE]
> Unlike Admin App's broader "configure new environments/instances" job, Admin API does not create environments or tenants — tenant configuration is static (see OUT-6). This job is limited to ODS instance records within an already-configured tenant.

### JTBD 4: Manage Authorization

**Personas**: Security administrator, Platform host system administrator

**When** I manage authorization for a deployment, \
**I want** to view and edit resource claims, actions, authorization strategies, claim sets, and claim-set resource claim actions, \
**so that** access rules are visible and editable.

**How Admin API Helps**: Exposes read/write endpoints for claim sets and read-only endpoints for resource claims, actions, and authorization strategies (FR-CLAIM-1 through FR-CLAIM-8). Admin App currently only supports viewing, exporting, and importing claim sets, so detailed editing depends on Admin API directly until Admin App's in-app claim set editor ships.

### JTBD 5: Transfer Claim Sets Between Environments

**Personas**: Platform host system administrator, DevOps engineer

**When** I have a claim set that is configured correctly in one environment, \
**I want** to copy, export, and import that claim set to another environment or tenant, \
**so that** I can avoid manual claim set configuration and reduce the risk of errors when setting up a new environment.

**How Admin API Helps**: Provides copy, export, and import endpoints for claim sets that preserve enough structure to move authorization configuration between environments (FR-CLAIM-1, FR-CLAIM-8).

**Examples**:

1. Configure a claim set in a staging environment, then export and import it to production.
2. Export a claim set used in one tenant and copy it to another tenant.

### JTBD 6: Isolate Multi-Tenant Administration

**Personas**: Platform host system administrator, DevOps engineer

**When** I operate a multi-tenant deployment, \
**I want** tenant-aware requests to resolve the correct `EdFi_Admin` and `EdFi_Security` connection strings, \
**so that** each tenant's administration remains isolated.

**How Admin API Helps**: Resolves tenant context from the `tenant` request header and routes to tenant-specific connection strings (FR-TENANT-1 through FR-TENANT-10).

### JTBD 7: Troubleshoot Operational Issues

**Personas**: DevOps engineer, Developer or implementer

**When** I troubleshoot the service, \
**I want** health checks, request logging, trace IDs, and structured error responses, \
**so that** operational failures can be diagnosed quickly.

**How Admin API Helps**: Exposes a `/health` endpoint, correlates logs with trace identifiers, and returns structured error responses (FR-ERROR-1 through FR-ERROR-8, NFR-REL-1 through NFR-REL-3).

### JTBD 8: Upgrade Between Admin API Versions

**Personas**: DevOps engineer, Developer or implementer

**When** I migrate between Admin API versions, \
**I want** predictable binary replacement and deployment-specific instructions, \
**so that** upgrades do not require undocumented manual steps.

**How Admin API Helps**: Documents migration guidance for Docker and IIS installation patterns and accepts a release-supplied API version in build automation (NFR-COMPAT-3, NFR-COMPAT-4).

## 2. Enterprise Architecture

Admin API sits between administrators, automation clients, optional UI clients, and the ODS/API administrative data stores. It writes to `EdFi_Admin` and `EdFi_Security` through EF Core and Ed-Fi data access contexts. The ODS/API reads those same stores to enforce API access.

External systems and dependencies:

- Ed-Fi ODS/API: The downstream API whose vendors, applications, credentials, claim sets, profiles, and authorization rules are administered.
- `EdFi_Admin` database: Stores administrative entities such as vendors, applications, API clients, ODS instances, profile data, and OpenIddict token server tables.
- `EdFi_Security` database: Stores claim sets, resource claims, resource claim actions, and authorization strategies used by ODS/API authorization.

Deployment boundaries:

- The API is an ASP.NET Core application configured through appsettings, environment variables, and development user secrets.
- The service can run with SQL Server or PostgreSQL selected through `AppSettings:DatabaseEngine`.
- Multi-tenancy is controlled by `AppSettings:MultiTenancy` and static `Tenants` configuration.
- Path-base hosting is supported through `AppSettings:PathBase`.
- Swagger UI is disabled by default and enabled through `SwaggerSettings:EnableSwagger`.

```plaintext
+----------------------+        Bearer JWT                +-------------+
|  Client application  | -------------------------------> |  Ed-Fi ODS  |
|  (e.g. Admin App)    |   GET | POST | PUT | DELETE      |  Admin API  |
+----------------------+                                  +-------------+
                                                               | r/w
                                              -------------------------
                                              |                       |
                                              v                       v
                                        +--------------+     +-----------------+
                                        |  EdFi_Admin  |     |  EdFi_Security  |
                                        |  (database)  |     |  (database)     |
                                        +--------------+     +-----------------+
```

## 3. Current functionality and functional requirements

The requirements below describe current product behavior. They are written as requirements because they capture behavior that downstream clients rely on, but they should be reviewed before being treated as future roadmap commitments.

### 3.1 API mode, versioning, and endpoint registration

- **FR-VERSION-1** The application SHALL support an `AppSettings:AdminApiMode` configuration value that selects v1 or v2 behavior at startup.
- **FR-VERSION-2** The application SHALL reject requests whose version prefix conflicts with the configured API mode.
- **FR-VERSION-3** The application SHALL expose v2 functionality under `/v2/*` routes when running in v2 mode.
- **FR-VERSION-4** The application SHALL expose an anonymous `GET /` endpoint that returns version and build metadata for the active API mode.
- **FR-VERSION-5** The application SHALL register feature endpoints through feature classes rather than requiring controller classes for every resource.
- **FR-VERSION-6** The application SHOULD generate Swagger/OpenAPI descriptions for all configured API versions when Swagger is enabled.

### 3.2 Authentication, registration, and tokens

- **FR-AUTH-1** The application SHALL support OAuth 2.0 client credentials token issuance through `POST /connect/token`.
- **FR-AUTH-2** The application SHALL support self-contained client registration through `POST /connect/register` when `Authentication:AllowRegistration` is enabled.
- **FR-AUTH-3** The registration endpoint SHALL reject duplicate client IDs.
- **FR-AUTH-4** The registration endpoint SHALL require client ID, client secret, and display name.
- **FR-AUTH-5** The registration endpoint SHALL require client secrets to contain lowercase, uppercase, numeric, and special characters and be 32 to 128 characters long.
- **FR-AUTH-6** Access tokens SHALL use the configured issuer URL and the ``EdFi_Admin`_api/full_access` scope.
- **FR-AUTH-7** Protected endpoints SHALL require authenticated bearer tokens with the implemented full-access scope unless explicitly marked anonymous.
- **FR-AUTH-8** Production deployments SHALL provide a signing key when not running in development mode.
- **FR-AUTH-9** The application SHOULD continue supporting self-contained authentication for backwards compatibility.
- **FR-AUTH-10** The product MAY use an external OIDC provider for Admin App scenarios, but source review indicates the current implemented authorization model still has one scope.

### 3.3 Vendor management

- **FR-VENDOR-1** The API SHALL allow authorized clients to list, retrieve, create, update, and delete vendors.
- **FR-VENDOR-2** Vendor records SHALL include company name, namespace prefixes, contact name, and contact email address.
- **FR-VENDOR-3** Vendor namespace prefixes SHALL support multiple comma-separated values.
- **FR-VENDOR-4** Vendor namespace prefix handling SHALL trim whitespace and ignore empty prefixes.
- **FR-VENDOR-5** Deleting a vendor SHALL remove associated user/contact records and applications through the existing command behavior.
- **FR-VENDOR-6** Vendor endpoints SHALL support pagination, filtering, ordering, and direction parameters where documented in OpenAPI.

### 3.4 Application management

- **FR-APP-1** The API SHALL allow authorized clients to list, retrieve, create, update, delete, and reset credentials for applications.
- **FR-APP-2** Creating an application SHALL require application name, vendor ID, claim set name, education organization IDs, and ODS instance IDs.
- **FR-APP-3** Creating an application SHALL validate that the vendor ID exists.
- **FR-APP-4** Creating or updating an application SHALL validate profile IDs and ODS instance IDs when provided.
- **FR-APP-5** Creating an application SHALL create a related API client and return generated key and secret material in the creation result.
- **FR-APP-6** Application names SHALL be constrained by the maximum length enforced by validation constants.
- **FR-APP-7** When duplicate prevention is enabled, the API SHALL reject duplicate applications based on the configured composite comparison of name, vendor, claim set, profiles, and ODS instances.
- **FR-APP-8** Updating an application SHALL support changes to name, claim set, vendor, profile IDs, education organization IDs, ODS instance IDs, and enabled state.
- **FR-APP-9** Deleting an application SHALL remove related API clients, ODS instance associations, education organization associations, and client access tokens while preserving profile records.
- **FR-APP-10** Application credential reset behavior SHALL preserve the key and generate a new secret according to the implemented command behavior.

### 3.5 API client management

- **FR-CLIENT-1** The API SHALL allow authorized clients to list, retrieve, create, update, delete, and reset credentials for API clients.
- **FR-CLIENT-2** Creating an API client SHALL require name, approval state, application ID, and at least one ODS instance ID.
- **FR-CLIENT-3** Creating an API client SHALL validate that the application ID exists.
- **FR-CLIENT-4** Creating or updating an API client SHALL validate ODS instance IDs when provided.
- **FR-CLIENT-5** API client names SHALL be limited to the maximum length enforced by validation constants.
- **FR-CLIENT-6** API client reset SHALL keep the existing key and generate a new secret.
- **FR-CLIENT-7** API client reset SHALL return a not-found error when the API client does not exist.
- **FR-CLIENT-8** Deleting an API client SHALL delete related API client ODS instance associations.
- **FR-CLIENT-9** API client models SHALL default to approved, active credentials unless request or persistence state says otherwise.

### 3.6 ODS instance, context, and derivative management

- **FR-ODS-1** The API SHALL allow authorized clients to list, retrieve, create, update, and delete ODS instance records.
- **FR-ODS-2** ODS instances SHALL include name, optional instance type, and connection string.
- **FR-ODS-3** ODS instance create and update behavior SHALL reject invalid connection strings when validation identifies them.
- **FR-ODS-4** The API SHALL allow authorized clients to manage ODS instance contexts.
- **FR-ODS-5** ODS instance contexts SHALL require ODS instance ID, context key, and context value.
- **FR-ODS-6** ODS instance context uniqueness SHALL be enforced by ODS instance ID and context key.
- **FR-ODS-7** The API SHALL allow authorized clients to manage ODS instance derivatives.
- **FR-ODS-8** ODS instance derivatives SHALL require ODS instance ID and derivative type.
- **FR-ODS-9** ODS instance derivative type SHALL be restricted to the allowed derivative types documented in validation constants.
- **FR-ODS-10** ODS instance derivative uniqueness SHALL be enforced by ODS instance ID and derivative type.

### 3.7 Profiles

- **FR-PROFILE-1** The API SHALL allow authorized clients to list, retrieve, create, update, and delete profiles.
- **FR-PROFILE-2** Profile records SHALL include a name and profile definition.
- **FR-PROFILE-3** Profile creation and update SHALL prevent duplicate names where enforced by current validators and commands.
- **FR-PROFILE-4** Profile definitions SHALL support the XML content-filter format used by ODS/API profiles.
- **FR-PROFILE-5** Deleting an application SHALL NOT delete referenced profile records.

### 3.8 Claim sets and authorization rules

- **FR-CLAIM-1** The API SHALL allow authorized clients to list, retrieve, create, update, delete, copy, export, and import claim sets.
- **FR-CLAIM-2** Claim set creation SHALL store claim set name and default flags for application-only and Ed-Fi preset usage.
- **FR-CLAIM-3** Claim set names SHALL be unique where enforced by current validation and command behavior.
- **FR-CLAIM-4** The API SHALL allow authorized clients to add, update, and delete resource claim action associations on claim sets.
- **FR-CLAIM-5** The API SHALL allow authorized clients to override and reset authorization strategies for resource claim actions.
- **FR-CLAIM-6** The API SHALL expose read-only endpoints for resource claims, resource claim actions, and authorization strategies.
- **FR-CLAIM-7** A resource claim action association SHALL include at least one action when required by validation.
- **FR-CLAIM-8** Claim set import and export SHALL preserve enough claim set structure to move authorization configuration between environments.

### 3.9 Tenants and multi-tenancy

- **FR-TENANT-1** The API SHALL support single-tenant mode by default.
- **FR-TENANT-2** The API SHALL support multi-tenant mode when `AppSettings:MultiTenancy` is enabled.
- **FR-TENANT-3** In multi-tenant v2 mode, the API SHALL resolve tenant context from the `tenant` request header.
- **FR-TENANT-4** Tenant IDs SHALL contain only alphanumeric characters and hyphens and SHALL be limited to the implemented maximum length.
- **FR-TENANT-5** The API SHALL reject write requests that require a tenant when the tenant header is missing in multi-tenant mode.
- **FR-TENANT-6** Swagger requests MAY use `SwaggerSettings:DefaultTenant` when multi-tenancy is enabled and a tenant header is absent.
- **FR-TENANT-7** The tenant service SHALL return configured tenants when multi-tenancy is enabled.
- **FR-TENANT-8** The tenant service SHALL return a default tenant when multi-tenancy is disabled.
- **FR-TENANT-9** Tenant configuration SHALL include separate `EdFi_Admin` and `EdFi_Security` connection strings per tenant.
- **FR-TENANT-10** Tenant connection strings SHALL NOT be exposed as plain API response details in tenant listing behavior.

### 3.10 Sorting, filtering, and pagination

- **FR-QUERY-1** Collection endpoints SHALL support `offset` and `limit` query parameters where documented.
- **FR-QUERY-2** Default offset and limit SHALL be configurable through `AppSettings:DefaultPageSizeOffset` and `AppSettings:DefaultPageSizeLimit`.
- **FR-QUERY-3** Collection endpoints SHALL support `orderBy` and `direction` parameters where documented.
- **FR-QUERY-4** Direction values SHOULD support ascending and descending semantics as documented in the OpenAPI markdown.
- **FR-QUERY-5** Resource-specific filter parameters SHOULD match documented model fields such as ID, name, company, namespace prefix, claim set name, ODS instance name, and other entity-specific fields.

### 3.11 Error handling and responses

- **FR-ERROR-1** Validation failures SHALL return HTTP 400 with structured validation details.
- **FR-ERROR-2** Missing resources SHALL return HTTP 404 where the feature uses not-found exception behavior.
- **FR-ERROR-3** Unauthorized requests SHALL return HTTP 401.
- **FR-ERROR-4** Authenticated but unauthorized requests SHALL return HTTP 403.
- **FR-ERROR-5** Business conflicts SHOULD return HTTP 409 where endpoint metadata and command behavior support conflict responses.
- **FR-ERROR-6** Unhandled server failures SHALL return HTTP 500 with a structured error response.
- **FR-ERROR-7** Invalid token scopes SHALL return OAuth-compatible invalid-scope error details.
- **FR-ERROR-8** Error responses and logs SHALL include enough trace information to correlate a request with server logs.

## 4. Non-functional requirements

### 4.1 Compatibility and upgradeability

- **NFR-COMPAT-1** The 2.x line SHALL support ODS/API 7.0 and greater as the primary compatibility target.
- **NFR-COMPAT-2** The repository SHALL retain v1 compatibility code for older ODS/API administration scenarios where still supported.
- **NFR-COMPAT-3** Migration guidance SHOULD support Docker and IIS installation patterns.
- **NFR-COMPAT-4** Build automation SHALL accept an API version supplied by release automation.
- **NFR-COMPAT-5** The API SHOULD preserve self-contained authentication for existing deployments while allowing external OIDC design evolution.

### 4.2 Security and privacy

- **NFR-SEC-1** Protected endpoints SHALL require JWT bearer authentication.
- **NFR-SEC-2** Authorization SHALL require the implemented Admin API full-access scope unless endpoint behavior explicitly allows anonymous access.
- **NFR-SEC-3** Production deployments SHALL require configured signing material rather than relying on development-only ephemeral keys.
- **NFR-SEC-4** Client secrets SHALL meet configured complexity and length requirements at registration.
- **NFR-SEC-5** Credential reset SHALL generate new secret material instead of reusing previous secrets.
- **NFR-SEC-6** Tenant connection strings and encryption keys SHALL be treated as sensitive configuration.
- **NFR-SEC-7** Swagger SHOULD be disabled in production unless intentionally enabled and protected by deployment controls.
- **NFR-SEC-8** Registration SHOULD be disabled after bootstrap in deployments that do not require open client self-registration.
- **NFR-SEC-9** Rate limiting SHOULD protect high-risk anonymous endpoints such as registration.

### 4.3 Reliability and operations

- **NFR-REL-1** The application SHALL expose a `/health` endpoint.
- **NFR-REL-2** The health endpoint SHALL return HTTP 200 when dependencies are healthy and HTTP 503 when any dependency is unhealthy.
- **NFR-REL-3** Health output SHALL include grouped dependency status details.
- **NFR-REL-4** Database provider selection SHALL fail fast when an unsupported database engine is configured.
- **NFR-REL-5** Invalid API mode configuration SHALL fail explicitly rather than silently choosing behavior.
- **NFR-REL-6** Multi-tenant database resolution SHALL fail explicitly when tenant configuration is missing or incomplete.

### 4.4 Observability

- **NFR-OBS-1** The application SHALL use log4net for application logging.
- **NFR-OBS-2** Request logging SHALL run before routing, authentication, and endpoint execution.
- **NFR-OBS-3** Logs SHALL include request path and trace identifier.
- **NFR-OBS-4** Error logs SHOULD include structured error context.
- **NFR-OBS-5** The application SHOULD support external log collection through deployment-specific log4net configuration.
- **NFR-OBS-6** The product does not currently show explicit APM or distributed tracing integration in source; this SHOULD remain an open operational consideration.

### 4.5 Performance and scalability

- **NFR-PERF-1** Collection endpoints SHALL support pagination to avoid unbounded result sets.
- **NFR-PERF-2** Query behavior SHOULD support sorting and filtering for common administrative lookup scenarios.
- **NFR-PERF-3** PostgreSQL v2 database registration SHALL use split-query behavior where configured to reduce EF query explosion risk.
- **NFR-PERF-4** Tenant configuration SHOULD be cached where supported by tenant service behavior.
- **NFR-PERF-5** The product SHOULD document expected limits for tenants, applications, clients, and claim-set complexity in a future release.

### 4.6 Accessibility and usability

- **NFR-UX-1** The REST API SHALL provide Swagger/OpenAPI metadata when Swagger is enabled.
- **NFR-UX-2** Swagger SHALL include OAuth client-credentials configuration for interactive testing.
- **NFR-UX-3** Error responses SHALL be actionable for invalid requests, missing entities, malformed JSON, and authentication failures.
- **NFR-UX-4** HTTP example files SHOULD remain aligned with current endpoints and authentication behavior.
- **NFR-UX-5** Admin App-specific documentation SHOULD be reconciled with current source before it is treated as implementation truth.

### 4.7 SDLC and testability

- **NFR-SDLC-1** The repository SHALL include unit and database tests for command behavior, validators, queries, and feature handlers.
- **NFR-SDLC-2** NUnit SHALL be the primary test framework.
- **NFR-SDLC-3** Shouldly and FakeItEasy SHOULD be used consistently in new tests.
- **NFR-SDLC-4** E2E tests SHOULD remain available for v1 and v2 workflows.
- **NFR-SDLC-5** Build and test automation SHOULD continue to support `./build.ps1 UnitTest`.

## 5. System architecture

| Component | Current responsibility | Primary sources |
| --- | --- | --- |
| ASP.NET Core host | Starts the web application, logging, middleware pipeline, health checks, feature endpoints, controllers, and Swagger | `Program.cs`, `WebApplicationBuilderExtensions.cs`, `WebApplicationExtensions.cs` |
| Feature endpoints | Implement resource-specific REST behavior using feature classes and `AdminApiEndpointBuilder` | `Application/EdFi.Ods.AdminApi/Features/*`, `Application/EdFi.Ods.AdminApi.V1/Features/*` |
| Authentication and authorization | Provides OpenIddict client-credentials token server, JWT bearer validation, and full-access scope policy | `SecurityExtensions.cs`, `SecurityConstants.cs`, `AuthorizationPolicies.cs`, `ConnectController.cs` |
| Admin API database context | Stores OpenIddict application, authorization, scope, and token entities in the admin database | `AdminApiDbContext.cs`, SQL artifact files |
| `EdFi_Admin` data access | Reads and writes vendors, applications, API clients, profiles, ODS instances, and related administrative data | Admin data access contexts, command and query classes |
| `EdFi_Security` data access | Reads and writes claim sets, resource claims, actions, authorization strategies, and claim-set associations | Security data access contexts, claim-set editor services |
| Multi-tenancy infrastructure | Resolves tenant from request header and routes database access to tenant-specific connection strings | `TenantIdentificationMiddleware.cs`, `TenantConfigurationProvider.cs`, `TenantService.cs` |
| Validation layer | Enforces request shape, required fields, referential integrity, uniqueness checks, and length constraints | FluentValidation validators in feature classes, command tests |
| Request logging and error handling | Logs requests and converts exceptions into structured HTTP responses | `RequestLoggingMiddleware.cs` |
| Swagger/OpenAPI | Produces versioned API documentation with OAuth metadata and custom schema filters | Swagger configuration and documentation filters |
| Docker deployment assets | Provide local/test deployment topologies for PostgreSQL, SQL Server, single-tenant, multi-tenant, and optional IdP scenarios | `docs/docker.md`, `Docker/**` |

Data ownership:

- Admin API owns the administrative mutation workflow exposed through its REST endpoints.
- `EdFi_Admin` and `EdFi_Security` remain the persistence stores shared with ODS/API.
- ODS/API consumes the resulting administrative and security configuration.
- Tenant configuration is currently static configuration, not a first-class mutable database-backed product entity in the observed source.

Integration points:

- OAuth token endpoint for automation clients.
- Optional external OIDC provider described in design documentation.
- Database connections for `EdFi_Admin` and `EdFi_Security`.
- Swagger/OpenAPI for client discovery and manual testing.
- Docker and IIS deployment paths.

## 6. Out of scope and known limitations

Out of scope for the current observed product:

- OUT-1: Admin API does not provision or mutate ODS/API business data; it manages administrative and security configuration.
- OUT-2: Admin API does not currently expose implemented fine-grained scopes beyond ``EdFi_Admin`_api/full_access`, despite design notes describing tenant and worker scopes as needed.
- OUT-3: Admin Console endpoints are out of scope for this release. The release contained leftover `adminconsole` endpoint artifacts that should not have been present and should not be treated as product requirements.
- OUT-4: Instance worker endpoints were removed as part of ADMINAPI-1294.
- OUT-5: Health-check worker endpoints and specialized worker scopes were removed as part of ADMINAPI-1295.
- OUT-6: Dynamic tenant provisioning through mutable API endpoints is not evident in the observed source; tenants are configured statically.
- OUT-7: Tenant authorization is not shown as being cryptographically bound to JWT claims; tenant selection is header-driven in current source.
- OUT-8: Token lifetime is hardcoded to 30 minutes in source and is not shown as a configurable product setting.
- OUT-9: Key rotation and external key-management integration are not documented as current product behavior.
- OUT-10: Performance limits and operational sizing guidance are not present in the repository documentation reviewed.
- OUT-11: Admin API returns generated client credentials directly in the API response. Secure one-time distribution mechanisms (e.g., Admin App's optional Yopass-based link) are an Admin App concern, not implemented in Admin API.

Known documentation conflicts:

- CONFLICT-1: `docs/design/auth/README.md` describes new `tenant_access` and `worker` scopes for 2.3+, while source defines only ``EdFi_Admin`_api/full_access`.
- CONFLICT-2: Admin Console endpoint artifacts appear in generated or design documentation, but they are out of scope for this release and should be treated as leftover artifacts rather than functional requirements.
- CONFLICT-3: Some generated API documentation refers to scope `api` in warning blocks, while implemented security constants define ``EdFi_Admin`_api/full_access`.
- CONFLICT-4: The OpenAPI documentation is labeled 2.3.0-pre or 2.3.0, while the user identified the branch as 2.3.1.

Risks:

- RISK-1: Clients may rely on stale generated documentation for endpoints or scopes that are not implemented.
- RISK-2: Header-only tenant selection can lead to operational risk if deployment controls do not prevent cross-tenant misuse.
- RISK-3: Static tenant configuration increases operational friction for tenant onboarding and rotation of tenant database credentials.
- RISK-4: Self-registration is useful for bootstrap but should be tightly controlled after initial setup.
- RISK-5: Credential and connection-string encryption behavior depends on configuration discipline and key management that is not fully specified in the repository.

## 7. Open questions and decision log

Open questions:

- OQ-1: Should the 2.3.1 product officially expose only ``EdFi_Admin`_api/full_access`, or should `tenant_access` and `worker` scopes be implemented or removed from design documentation?
- OQ-2: Which generated or design artifacts should be cleaned up so leftover Admin Console endpoint references do not reappear in release documentation?
- OQ-3: Should tenant identity be validated against token claims rather than trusting a request header alone?
- OQ-4: Should token lifetime be configurable?
- OQ-5: Should tenant configuration become mutable through API endpoints and persistent storage?
- OQ-6: Should Swagger/OpenAPI artifacts be regenerated and renamed for 2.3.1 to remove stale 2.3.0-pre references?
- OQ-7: What production guidance should be added for signing key rotation, encryption key rotation, and secret handling?
- OQ-8: What role model should Admin App users use if external OIDC support is productized?
- OQ-9: Should rate limiting be tenant-aware or client-aware rather than only IP and endpoint based?
- OQ-10: Which v1 compatibility scenarios remain supported, and what is the deprecation policy?
- OQ-11: Design documentation describes Keycloak as the reference implementation for Admin App-oriented authentication, though current source still defines one implemented scope.

Decision log:

- DEC-1: Treat source and tests as authoritative for current 2.3.1 behavior when generated docs conflict with implementation.
- DEC-2: Document Admin App and external OIDC content as design direction unless matching source behavior is observed.
- DEC-3: Treat worker endpoint removal as current product scope because removal docs and source search align.
- DEC-4: Preserve v1 compatibility as current source behavior but position v2 as the current ODS/API 7.x administrative surface.
- DEC-5: Keep implementation details in architecture context unless they directly affect API clients, deployment operators, or observable behavior.

## 8. Glossary

- Admin API: REST API for administering ODS/API configuration, client credentials, and authorization metadata.
- Admin App: UI-oriented administrative application described in design documentation as using Admin API as a backend service.
- API client: Credentialed client record used to access ODS/API through key and secret material.
- Application: Administrative representation of an ODS/API client application, associated with vendor, claim set, education organizations, ODS instances, profiles, and API client credentials.
- Claim set: Named collection of authorization rules used by ODS/API security.
- Ed-Org: Education Organization. A school, district, or other educational entity whose data an application or API client is authorized to access. Admin API references Ed-Orgs by ID (education organization IDs); it does not create or manage Ed-Org records itself.
- `EdFi_Admin`: Database containing administrative configuration such as vendors, applications, API clients, profiles, and ODS instance metadata.
- `EdFi_Security`: Database containing ODS/API security metadata such as claim sets, resource claims, actions, and authorization strategies.
- ODS/API: Ed-Fi Operational Data Store and API.
- ODS instance: Administrative representation of an ODS/API database instance, including name, type, and connection string.
- ODS instance context: Key/value metadata associated with an ODS instance.
- ODS instance derivative: Related ODS instance connection such as a read replica or snapshot.
- OpenIddict: .NET library used by Admin API for self-contained OAuth/OpenID Connect server functionality.
- Profile: ODS/API profile definition that constrains API content through XML profile configuration.
- Resource claim: ODS/API authorization resource protected by claim-set rules.
- Tenant: Named deployment partition resolved by request header and mapped to tenant-specific `EdFi_Admin` and `EdFi_Security` connection strings.

## Final self-review

- Mode is existing application PRD by reverse engineering.
- Source inventory is visible and separates used, missing, and conflicting sources.
- Current functionality is separated from open product questions and documentation conflicts.
- Functional requirements use stable IDs and SHALL/SHOULD/MAY language.
- Non-functional requirements cover compatibility, security, reliability, observability, performance, usability, and SDLC concerns.
- Architecture context includes a component table, deployment boundaries, data ownership, and integration points.
- Out-of-scope items and known limitations explicitly call out removed worker endpoints and stale admin-console documentation.
- Open questions are explicit rather than hidden as assumptions.
