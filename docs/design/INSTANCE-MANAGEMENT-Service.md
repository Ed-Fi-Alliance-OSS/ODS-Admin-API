
# Instance Management Service Layer Design

## Supported Operations and Endpoints

The main endpoint for database management is `/dbInstances`. Each database provider must implement the following operations according to its environment and technology:

* **Create Database** (`POST /dbInstances`): Create a new Ed-Fi database instance in the target environment (on-premises or cloud). Includes options for minimal or sample template population. The Admin API generates the database name and associates it with the OdsInstance.
* **Copy Database** (`POST /dbInstances/copy`): Clone an existing Ed-Fi database instance, allowing a custom name for the new instance. Copies all data from the source and ensures the new database is online, or reports errors if unsuccessful.
* **Delete Database** (`DELETE /dbInstances/{id}`): Remove an Ed-Fi database instance and ensure the database is deleted and offline. Consider feature-flagging this operation to prevent accidental data loss in production environments.
* **Instance Info** (`GET /dbInstances/{id}/info`): Return metadata such as database size and server details for operational insight.
* **Instance Status** (`GET /dbInstances/{id}/status`): Report the current status of the instance (online, offline, recovering, etc).

The `/odsInstances` endpoint is used for managing ODS instance records and metadata, without triggering database creation jobs. This is useful for SaaS providers who manage databases externally but need to maintain ODS instance records.

## Example SQL Commands

The following SQL commands illustrate how these operations are performed in PostgreSQL and Microsoft SQL environments:

| Operation        | PostgreSQL                                                   | Microsoft SQL                                                |
| ---------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Create Database    | `CREATE DATABASE new_database;`                              | `CREATE DATABASE new_database;`                              |
| Copy Database      | `CREATE DATABASE new_database_name TEMPLATE existing_database_name;` | `BACKUP DATABASE existing_database_name TO DISK = 'C:\path\to\temp_backup.bak';`<br />`RESTORE DATABASE new_database_name FROM DISK = 'C:\path\to\temp_backup.bak' WITH MOVE 'existing_database_data' TO 'C:\path\to\new_database_data.mdf', MOVE 'existing_database_log' TO 'C:\path\to\new_database_log.ldf';` |
| Delete Database    | `DROP DATABASE database_name;`                               | `DROP DATABASE database_name;`                               |
| Instance Info      | `SELECT pg_size_pretty(pg_database_size('database_name')) AS size;` | `USE database_name; EXEC sp_spaceused;`                      |
| Instance Status    | `SELECT datname AS database_name,        numbackends AS active_connections FROM pg_stat_database;` | `SELECT name AS database_name, state_desc AS status FROM sys.databases;` |

## Interface and Implementation Patterns

The [EdFi.Ods.Sandbox](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-ODS/tree/main/Application/EdFi.Ods.Sandbox) library provides a reference implementation for instance management in a sandbox environment. Recommended interface and class structure for the Instance Management Microservice:

* **IInstanceProvisioner.cs** - Interface defining all required operations (_AddInstance_, _CopyInstance_, _DeleteInstance_, _RenameInstance_, _InstanceStatus_). Each provider must implement these methods for its database or storage technology.
* **InstanceProvisionerBase.cs** - Base class for common elements such as connection management and shared variables.
* **InstanceStatus.cs** - Object class representing instance status values (e.g., "ERROR").

### Recommended Implementations

For Admin Console 1.0, provide implementations for Docker and Windows environments:

* **PostgresInstanceProvisioner.cs** - Uses PostgreSQL DDL functions for instance management.
* **SqlServerSandboxProvisioner.cs** - Uses MS-SQL DDL and DBCC commands for instance management.
