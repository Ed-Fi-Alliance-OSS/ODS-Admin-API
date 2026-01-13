# Ed-Fi ODS Instance Management Design

This document describes the design and workflow for managing Ed-Fi ODS database
instances using the Admin API. All orchestration, job scheduling, and status
updates are handled by the Admin API, leveraging Quartz.NET for asynchronous
operations.

## API Endpoints Overview

Database instance management operations are performed on the `adminapi.DbInstances`
endpoint for direct database management (create, rename, delete).

## System Architecture

```mermaid
C4Container
    title "Database Instance Management"

    System(ClientApp, "ClientApp", "A web application for managing ODS/API Deployments")
    UpdateElementStyle(ClientApp, $bgColor="silver")

    System_Boundary(backend, "Backend Systems") {
        Boundary(b0, "Admin API") {
            Container(AdminAPI, "Ed-Fi Admin API 2")
        }

        Boundary(b1, "ODS/API") {
            System(OdsApi, "Ed-Fi ODS/API", "A REST API system for\neducational data interoperability")
            UpdateElementStyle(OdsApi, $bgColor="silver")

            SystemDb(ods3, "EdFi_ODS_<instanceN>")
        }

        Boundary(b2, "Shared Databases") {
            ContainerDb(Admin, "EdFi_Admin,\nEdFi_Security")
        }
    }
   
    Rel(ClientApp, AdminAPI, "Issues HTTP requests")
    Rel(AdminAPI, ods3, "Creates/updates/deletes ODS instances")
    Rel(OdsApi, ods3, "Reads and writes")
    Rel(AdminAPI, Admin, "Reads and writes")
    Rel(OdsApi, Admin, "Reads")
    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="2")
```

## Functional Requirements and Status Values

### adminapi.DbInstances Endpoint Operations

The `adminapi.DbInstances` endpoint supports the following operations:

1. **Create Database Instance**: Provision a new ODS database instance. This operation creates a new record in `adminapi.DbInstances` and provisions the database using the specified template (minimal or sample).
2. **Delete Database Instance**: Remove an existing database instance. This is a soft delete; the `Status` field in `adminapi.DbInstances` is set to "DELETED" for audit purposes, and the database is dropped.
3. **Read all the database Instances**: Read instance details (OdsInstanceId, OdsInstanceName, Status, DatabaseTemplate).
4. **Read database Instance by Id**: Read Instance details by OdsInstance id.

All operations update the `Status` field in `adminapi.DbInstances` to reflect the current state (e.g., "Pending", "Completed", "InProgress", "Pending_Delete", "Deleted", "Delete_Failed", "Error").

```sql
CREATE TABLE [adminapi].[DbInstances] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OdsInstanceId] INT NOT NULL,
    [OdsInstanceName] NVARCHAR(100) NOT NULL, 
    [Status] NVARCHAR(75) NOT NULL,
    [DatabaseTemplate] NVARCHAR(100) NOT NULL,
    [DatabaseName] NVARCHAR(255) NULL,
    [LastRefreshed] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    CONSTRAINT [PK_DbInstances] PRIMARY KEY ([Id])
)
```

## Instance Management Workflow

All instance management operations are now orchestrated by the Admin API. The Admin API schedules and executes database instance creation, and deletion jobs via the `adminapi.DbInstances` endpoint (using a job scheduler such as Quartz.NET), updates the status in the `adminapi.DbInstances` table, and manages all related metadata. Successfully created database instance details will be creating a new record on dbo.OdsInstances table with connection string.

> [!TIP]
> The processes below refer to the single `adminapi.DbInstances` table managed by Admin API 2.
> Admin API 2 on startup queries the `dbo.OdsInstances` table used by the ODS/API
> and inserts missing records into the new table. This solves a potential
> synchronization problem between these two tables.

#### Create Database Instance

```mermaid
sequenceDiagram
    participant ClientApp
    participant AdminAPI
    participant ODSDatabase
    participant EdFiAdminDB

    ClientApp->>AdminAPI: POST /DbInstances (includes Name, DatabaseTemplate)
    AdminAPI->>AdminAPI: Validate input data
    AdminAPI->>AdminAPI: Record job metadata and add instance entry to "DbInstances" table with status "Pending"
    AdminAPI->>AdminAPI: Schedule Quartz job - "CreateDbInstanceJob{model.id}"
    AdminAPI->>ODSDatabase: Provision new ODS database (clone from template)
    ODSDatabase-->>AdminAPI: Confirm database creation
    AdminAPI->>EdFiAdminDB: Begin transaction to update "DbInstances" status to "Completed" and insert record into OdsInstance table
    AdminAPI->>EdFiAdminDB: Commit transaction
```

#### Delete Database Instance

```mermaid
sequenceDiagram
    actor ClientApp
    participant AdminAPI
    participant EdFi_Admin
    participant DbServer

    ClientApp ->> AdminAPI: DELETE /DbInstances/{id}
    AdminAPI ->> EdFi_Admin: UPDATE adminapi.DbInstances SET status = "PENDING_DELETE"
    AdminAPI -->> ClientApp: 204 Ok

    AdminAPI ->> AdminAPI: Schedule DeleteDbInstance job (Quartz.NET)
    AdminAPI ->> DbServer: Drop database

    alt Drop successful
        AdminAPI ->> EdFi_Admin: BEGIN TRANSACTION
        AdminAPI --> EdFi_Admin: UPDATE Status = DELETED FROM adminapi.DbInstances Status
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstanceDerivative
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstanceContext
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstances
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.ApiClients and dbo.ApiClientOdsInstances
        AdminAPI ->> EdFi_Admin: COMMIT TRANSACTION
    else Drop failed
        AdminAPI --> EdFi_Admin: UPDATE Status = DELETE_FAILED FROM adminapi.DbInstances Status
    end
```

## Cloud Support (Planned)

> [!NOTE]
> Placeholder. The `adminapi.DbInstances` endpoint will support cloud database providers in future releases. The database creation process may differ across managed database solutions.
