# Instance Management

This document describes the work performed by the Admin API 2 application and
its associated Instance Management Worker for creating or deleting database
instances. Additional features and technical requirements are in the [Instance
Management
Worker](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-Admin-Console-Instance-Management-Worker-Process/blob/main/docs/INSTANCE-MANAGEMENT.md)
repository. The document below describes Admin API's role in support of the
worker process.

## Containers

```mermaid
C4Container
    title "Instance Management"

    System(AdminConsole, "Ed-Fi Admin Console", "A web application for managing ODS/API Deployments")
    UpdateElementStyle(AdminConsole, $bgColor="silver")

    System_Boundary(backend, "Backend Systems") {

        Boundary(b0, "Admin API") {
            Container(AdminAPI, "Ed-Fi Admin API 2")
            Container(Instance, "Admin API Instance<br />Management Worker")
        }

        Boundary(b1, "ODS/API") {
            System(OdsApi, "Ed-Fi ODS/API", "A REST API system for<br />educational data interoperability")
            UpdateElementStyle(OdsApi, $bgColor="silver")

            SystemDb(ods3, "EdFi_ODS_<instanceN>")
        }

        Boundary(b2, "Shared Databases") {
            ContainerDb(Admin, "EdFi_Admin,<br />EdFi_Security")
        }
    }
   
    Rel(AdminConsole, AdminAPI, "Issues HTTP requests")

    Rel(Instance, AdminAPI, "Reads instance requests,<br />Write instance status")
    UpdateRelStyle(Instance, AdminAPI, $offsetY="50", $offsetX="-10")

    Rel(Instance, ods3, "Creates new ODS instances")
    UpdateRelStyle(Instance, ods3, $offsetY="20", $offsetX="-50")

    Rel(OdsApi, ods3, "Reads and writes")
    UpdateRelStyle(OdsApi, ods3, $offsetX="10")
   
    Rel(AdminAPI, Admin, "Reads and writes")
    UpdateRelStyle(AdminAPI, Admin, $offsetY="50", $offsetX="10")

    Rel(OdsApi, Admin, "Reads")
    UpdateRelStyle(OdsApi, Admin, $offsetY="20", $offsetX="-10")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="2")
```

## Functional Requirements

Users will need the ability to perform the following operations for ODS database
instances:

1. Create a new instance and insert records into the `dbo.OdsInstances` and related tables.
2. Rename an existing instance and update the records (including connection string) in `dbo.OdsInstances`.
3. Delete an existing instance and delete records from `dbo.OdsInstances`.
4. Backup an existing instance
5. Restore an existing instance from backup

The first three operations will also require updating the `Status` field in the
`adminconsole.Instance` table. From this perspective, the delete operation will
be a _soft delete_ for audit purposes. That is, the
`adminconsole.Instance.Status` field will be set to "DELETED" instead of
physically deleting the row.

## Solution Design

The Instance Management Worker will be a command line interface (CLI)
application that should run on a schedule, e.g. as a Cron job in Linux or a
Windows Scheduled Task. The Worker's responsibility is to create and manage
ODS database instances ("CREATE DATABASE"). It will pull task information from
Admin API 2, and create databases directly in the RDBMS. It writes status back
to Admin API 2 so that it can update the instance management tables used by the
ODS/API (e.g. `dbo.OdsInstances`).

Ultimately, the solution needs to be robust for error handling, retries, and for
extensibility to support cloud-based platforms. These functions will be built
out incrementally as needed based on feedback from the field:

1. Assume single worker running at a time; fail gracefully; require manual
   intervention when errors occur. Support for "on prem" type of connectivity
   with PostgreSQL and MSSQL. Single Tenant.
2. Multi tenancy - creating additional Admin and Security databases.
3. More robust job handling for concurrent execution.
4. Infrastructure for Cloud managed databases (e.g. AWS Aurora, AWS RDS,
   Azure Cosmos DB, Azure SQL Server, Azure PostgreSQL).

> [!TIP]
> The processes below refer to a new `Instances` table managed by Admin API 2.
> Admin API 2 on startup queries the `dbo.OdsInstances` table used by the ODS/API
> and inserts missing records into the new table. This solves a potential
> synchronization problem between these two tables.

### v1: Single Worker

### New Instance

```mermaid
sequenceDiagram
    actor Console as Admin Console
    actor Worker as Instance Management Worker
    participant AdminAPI
    participant EdFi_Admin

    Console ->> AdminAPI: POST /adminconsole/odsInstances
    AdminAPI ->> EdFi_Admin: INSERT INTO adminconsole.Instance
    AdminAPI -->> Console: 202 Accepted

    Worker ->> AdminAPI: GET /adminconsole/instances?status=pending
    AdminAPI -->> Worker: instances

    loop For Each Pending Instance
        create participant DbServer

        Worker ->> DbServer: create / copy database from template

        Worker ->> AdminAPI: POST /adminconsole/instances/{id}/completed

        AdminAPI ->> EdFi_Admin: BEGIN TRANSACTION
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstances
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceContext
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceDerivative

        note right of EdFi_Admin: Credentials for Health Check Worker       
        rect rgb(191, 223, 255)
            AdminAPI --> EdFi_Admin: INSERT INTO dbo.ApiClients
            AdminAPI --> EdFi_Admin: INSERT INTO dbo.ApiClientOdsInstances
        end

        AdminAPI ->> EdFi_Admin: UPDATE adminconsole.Instances (status, credentials)
       
        AdminAPI ->> EdFi_Admin: COMMIT

    end

    AdminAPI -->> Worker: 200 OK
```

### Delete Instance

```mermaid
sequenceDiagram
    actor Console
    actor Worker
    participant AdminAPI
    participant EdFi_Admin
    participant DbServer

    Console ->> AdminAPI: DELETE /adminconsole/odsInstances/{id}
    AdminAPI ->> EdFi_Admin: UPDATE adminconsole.Instance SET status = "PENDING_DELETE"
    AdminAPI -->> Console: 204 Ok

    Worker ->> AdminAPI: GET /adminconsole/instances?status=PENDING_DELETE
    AdminAPI ->> EdFi_Admin: Fetch instances with status=PENDING_DELETE
    EdFi_Admin -->> AdminAPI: List of instances

    AdminAPI -->> Worker: List of instances

    loop For each instance in list
        Worker ->> DbServer: Drop database

        alt Drop successful
            Worker ->> AdminAPI: POST /adminconsole/instances/{id}/deleted

            AdminAPI ->> EdFi_Admin: BEGIN TRANSACTION
            AdminAPI --> EdFi_Admin: UPDATE Status = DELETED FROM adminconsole.Instance Status
            AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstanceDerivative
            AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstanceContext
            AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstances
            AdminAPI ->> EdFi_Admin: DELETE FROM dbo.ApiClients and dbo.ApiClientOdsInstances
            AdminAPI ->> EdFi_Admin: COMMIT TRANSACTION

            AdminAPI -->> Worker: 204 OK
        else Drop failed
            Worker ->> AdminAPI: POST /adminconsole/instances/{id}/deleteFailed
            AdminAPI --> EdFi_Admin: UPDATE Status = DELETE_FAILED FROM adminconsole.Instance Status
        end
    end
```

### Rename Instance

```mermaid
sequenceDiagram
    actor Console
    actor Worker
    participant AdminAPI
    participant EdFi_Admin
    participant DbServer

    Console ->> AdminAPI: DELETE /adminconsole/odsInstances/{id}
    AdminAPI ->> EdFi_Admin: UPDATE adminconsole.Instance SET status = "PENDING_RENAME"
    AdminAPI -->> Console: 204 Ok

    Worker ->> AdminAPI: GET /adminconsole/instances?status=PENDING_RENAME
    AdminAPI ->> EdFi_Admin: Fetch instances with status=PENDING_RENAME
    EdFi_Admin -->> AdminAPI: List of instances

    AdminAPI -->> Worker: List of instances

    loop For each instance in list
        Worker ->> DbServer: Rename database

        alt Drop successful
            Worker ->> AdminAPI: POST /adminconsole/instances/{id}/renamed

            AdminAPI ->> EdFi_Admin: BEGIN TRANSACTION
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstances
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceContext
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceDerivative

        note right of EdFi_Admin: Credentials for Health Check Worker       
        rect rgb(191, 223, 255)
            AdminAPI --> EdFi_Admin: INSERT INTO dbo.ApiClients
            AdminAPI --> EdFi_Admin: INSERT INTO dbo.ApiClientOdsInstances
        end

        AdminAPI ->> EdFi_Admin: UPDATE adminconsole.Instances (status, credentials)

            AdminAPI -->> Worker: 204 OK
        else Rename failed
            Worker ->> AdminAPI: POST /adminconsole/instances/{id}/renameFailed
            AdminAPI --> EdFi_Admin: UPDATE Status = RENAME_FAILED FROM adminconsole.Instance Status
        end
    end
```

### Backup Instance

TBD

### Restore Backup

TBD

#### Health Check Client Credentials

The section in blue is in support of the Health Check Worker, which needs client
credentials for accessing the newly created instance. Admin API 2 will need to
create and store new credentials each time an instance is created. Note that the
credentials are stored in a separate column from the rest of the document
information, and they should be encrypted. For more information, see [Health
Check Worker: Admin API's
Responsibilities](./HEALTH-CHECK-WORKER.md#admin-apis-responsibilities).

### v2: Multi Tenancy

For multi-tenancy support, the Instance Management Worker must also create new
instances of the `EdFi_Admin` and `EdFi_Security` databases. The Admin API
`Instances` table will still be in the original `EdFi_Admin` database.

> [!NOTE]
> Community feedback may suggest creating a single new multi-tenant database
> that is independent of any given `EdFi_Admin` instance / tenant. For now, the
> simplest path forward is to use the first instance.

```mermaid
sequenceDiagram
    actor Console as Admin Console
    actor Worker as Instance Management Worker
    participant AdminAPI
    participant EdFi_Admin as EdFi_Admin DB
    participant DbServer
    participant EdFi_Admin_New as EdFi_Admin_<TenantId>

    Console ->> AdminAPI: POST /adminconsole/instances
    AdminAPI ->> EdFi_Admin: INSERT INTO adminconsole.Instance
    AdminAPI -->> Console: 202 Accepted

    Worker ->> AdminAPI: GET /adminconsole/instances?status=pending
    AdminAPI -->> Worker: instances

    loop For Each Pending Instance

        Worker ->> DbServer: create / copy ODS database from template
        Worker ->> DbServer: create / copy Admin database from template
        Worker ->> DbServer: create / copy Security database from template

        Worker ->> AdminAPI: POST /adminconsole/instances/{id}/created

        AdminAPI ->> EdFi_Admin_New: BEGIN TRANSACTION
        AdminAPI ->> EdFi_Admin_New: INSERT INTO dbo.OdsInstances
        AdminAPI ->> EdFi_Admin_New: INSERT INTO dbo.OdsInstanceContext
        AdminAPI ->> EdFi_Admin_New: INSERT INTO dbo.OdsInstanceDerivative

        note right of EdFi_Admin: Credentials for Health Check Worker       
        rect rgb(191, 223, 255)
            AdminAPI --> EdFi_Admin_New: INSERT INTO dbo.ApiClients
            AdminAPI --> EdFi_Admin_New: INSERT INTO dbo.ApiClientOdsInstances
        end

        AdminAPI ->> EdFi_Admin: UPDATE adminconsole.Instance (status, credentials)
       
        AdminAPI ->> EdFi_Admin: COMMIT

    end

    AdminAPI -->> Worker: 200 OK
```

### v3: Concurrency

The following diagram only displays the single tenant situation, but is easily
adapted to multi-tenancy.

```mermaid
sequenceDiagram
    actor Console
    actor Worker
    participant AdminAPI
    participant EdFi_Admin
    participant DbServer

    Console ->> AdminAPI: POST /adminconsole/instances
    AdminAPI ->> EdFi_Admin: INSERT INTO adminconsole.Instance
    AdminAPI -->> Console: 202 Accepted

    Worker ->> AdminAPI: POST /adminconsole/instances/jobs/start
    AdminAPI ->> EdFi_Admin: Set lock and return rows from Instance
    EdFi_Admin -->> AdminAPI: locked rows

    AdminAPI -->> Worker: job information with job id

    Worker ->> Worker: createDatabase
    Worker ->> DbServer: create / copy database from template

    Worker ->> AdminAPI: POST /adminconsole/instances/jobs/{id}/complete

    AdminAPI ->> EdFi_Admin: BEGIN TRANSACTION
    AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstances
    AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceContext
    AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceDerivative
    AdminAPI ->> EdFi_Admin: INSERT key & secret INTO dbo.Application
   
    AdminAPI ->> EdFi_Admin: UPDATE adminconsole.Instance.Document including key & secret
   
    AdminAPI ->> EdFi_Admin: COMMIT

    AdminAPI -->> Worker: 200 OK
```

### v4: Cloud Support

> [!NOTE]
> Placeholder. Assuming that the create database process will differ across the
> managed database solutions.