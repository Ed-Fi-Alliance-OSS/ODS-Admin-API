
# Ed-Fi ODS Instance Management Design

This document describes the design and workflow for managing Ed-Fi ODS database instances using the Admin API. All orchestration, job scheduling, and status updates are handled by the Admin API, leveraging Quartz.NET for asynchronous operations.

## System Architecture

```mermaid
C4Container
    title "Instance Management"

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

Users will need the ability to perform the following operations for ODS database
instances:

1. Create a new instance and insert records into the `dbo.OdsInstances` and related tables.
2. Rename an existing instance in `dbo.OdsInstances`.
3. Delete an existing instance and delete records from `dbo.OdsInstances`.

The first three operations will also require updating the `Status` field in the
`adminapi.Instances` table. From this perspective, the delete operation will
be a _soft delete_ for audit purposes. That is, the
`adminapi.Instances.Status` field will be set to "DELETED" instead of
physically deleting the row.

> [!NOTE]
> Valid Instances.Status values - "Pending","Completed","InProgress", "Pending_Delete", "Deleted", "Delete_Failed", "Error"

```sql
 CREATE TABLE [adminapi].[Instances] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OdsInstanceId] INT NOT NULL,
        [InstanceName] NVARCHAR(100) NOT NULL, 
        [Status] NVARCHAR(75) NOT NULL,
        [OdsDatabaseName] NVARCHAR(255) NULL,
        [LastRefreshed] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedDate] DATETIME2 NULL,
        CONSTRAINT [PK_Instances] PRIMARY KEY ([Id])

```

## Instance Management Workflow

All instance management operations are now orchestrated by the Admin API. The Admin API schedules and executes instance creation, update/ rename, and deletion jobs (using a job scheduler such as Quartz.NET), updates the status in the database, and manages all related metadata.

> [!TIP]
> The processes below refer to a new `adminapi.Instances` table managed by Admin API 2.
> Admin API 2 on startup queries the `dbo.OdsInstances` table used by the ODS/API
> and inserts missing records into the new table. This solves a potential
> synchronization problem between these two tables.

### 1. Create New Instance

```mermaid
sequenceDiagram
    participant ClientApp
    participant AdminAPI
    participant ODSDatabase
    participant EdFiAdminDB

    ClientApp->>AdminAPI: Submit request to create new instance (includes Name, Instance Type, Connection String)
    AdminAPI->>AdminAPI: Validate input data
    AdminAPI->>AdminAPI: Record job metadata and add instance entry to "Instances" table with status "Pending"
    AdminAPI->>AdminAPI: Schedule Quartz job - "CreateOdsInstanceJob{model.id}"
    AdminAPI->>ODSDatabase: Provision new ODS database (clone from template)
    ODSDatabase-->>AdminAPI: Confirm database creation
    AdminAPI->>EdFiAdminDB: Begin transaction to update "Instances" status to "Completed" and insert record into OdsInstance table
    AdminAPI->>EdFiAdminDB: Commit transaction
```

### 2. Delete Instance

```mermaid
sequenceDiagram
    actor ClientApp
    participant AdminAPI
    participant EdFi_Admin
    participant DbServer

    ClientApp ->> AdminAPI: DELETE /OdsInstances/{id}
    AdminAPI ->> EdFi_Admin: UPDATE adminapi.Instances SET status = "PENDING_DELETE"
    AdminAPI -->> ClientApp: 204 Ok

    AdminAPI ->> AdminAPI: Schedule DeleteInstance job (Quartz.NET)
    AdminAPI ->> DbServer: Drop database

    alt Drop successful
        AdminAPI ->> EdFi_Admin: BEGIN TRANSACTION
        AdminAPI --> EdFi_Admin: UPDATE Status = DELETED FROM adminapi.Instances Status
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstanceDerivative
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstanceContext
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.OdsInstances
        AdminAPI ->> EdFi_Admin: DELETE FROM dbo.ApiClients and dbo.ApiClientOdsInstances
        AdminAPI ->> EdFi_Admin: COMMIT TRANSACTION
    else Drop failed
        AdminAPI --> EdFi_Admin: UPDATE Status = DELETE_FAILED FROM adminapi.Instances Status
    end
```

### 3. Rename Instance

```mermaid
sequenceDiagram
    actor ClientApp
    participant AdminAPI
    participant EdFi_Admin
    participant DbServer

    ClientApp ->> AdminAPI: PATCH /OdsInstances/{id}
    AdminAPI ->> EdFi_Admin: UPDATE adminapi.Instance SET status = "PENDING_RENAME"
    AdminAPI -->> ClientApp: 204 Ok

    AdminAPI ->> AdminAPI: Schedule RenameInstance job (Quartz.NET)
    AdminAPI ->> DbServer: Rename database

    alt Rename successful
        AdminAPI ->> EdFi_Admin: BEGIN TRANSACTION
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstances
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceContext
        AdminAPI ->> EdFi_Admin: INSERT INTO dbo.OdsInstanceDerivative

        AdminAPI ->> EdFi_Admin: UPDATE adminapi.Instances (status, credentials)
        AdminAPI ->> EdFi_Admin: COMMIT
    else Rename failed
        AdminAPI --> EdFi_Admin: UPDATE Status = RENAME_FAILED FROM adminapi.Instance Status
    end
    AdminAPI -->> ClientApp: 200 OK
```

## Cloud Support (Planned)

> [!NOTE]
> Placeholder. Assuming that the create database process will differ across the
> managed database solutions.
