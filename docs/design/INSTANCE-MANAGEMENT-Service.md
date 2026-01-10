
# Instance Management Service Layer Design

## Supported Operations

The following abstract operations are supported for Ed-Fi ODS instance management. Each database provider must implement these operations according to its environment and technology:

* **AddInstance** - Create a new Ed-Fi instance in the target environment (on-premises or cloud). This includes creating the database, initializing the Ed-Fi data model, and optionally populating with descriptors or sample data. The Admin API generates the database name and associates it with the OdsInstance.
* **CopyInstance** - Clone an existing Ed-Fi instance, allowing a custom name for the new instance. Copies all data from the source and ensures the new database is online, or reports errors if unsuccessful.
* **RenameInstance** - Change the name of an existing Ed-Fi instance and ensure the database remains online. Reports errors if the operation fails.
* **DeleteInstance** - Remove an Ed-Fi instance and ensure the database is deleted and offline. Consider feature-flagging this operation to prevent accidental data loss in production environments.
* **InstanceInfo** - Return metadata such as database size and server details for operational insight.
* **InstanceStatus** - Report the current status of the instance (online, offline, recovering, etc).

## Example SQL Commands

The following SQL commands illustrate how these operations are performed in PostgreSQL and Microsoft SQL environments:

| Operation        | PostgreSQL                                                   | Microsoft SQL                                                |
| ---------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| _AddInstance_    | `CREATE DATABASE new_database;`                              | `CREATE DATABASE new_database;`                              |
| _CopyInstance_   | `CREATE DATABASE new_database_name TEMPLATE existing_database_name;` | `BACKUP DATABASE existing_database_name TO DISK = 'C:\path\to\temp_backup.bak';`<br />`RESTORE DATABASE new_database_name FROM DISK = 'C:\path\to\temp_backup.bak' WITH MOVE 'existing_database_data' TO 'C:\path\to\new_database_data.mdf', MOVE 'existing_database_log' TO 'C:\path\to\new_database_log.ldf';` |
| _RenameInstance_ | `ALTER DATABASE old_database_name RENAME TO new_database_name;` | `ALTER DATABASE old_database_name MODIFY NAME = new_database_name;` |
| _DeleteInstance_ | `DROP DATABASE database_name;`                               | `DROP DATABASE database_name;`                               |
| _InstanceInfo_   | `SELECT pg_size_pretty(pg_database_size('database_name')) AS size;` | `USE database_name; EXEC sp_spaceused;`                      |
| _InstanceStatus_ | `SELECT datname AS database_name,        numbackends AS active_connections FROM pg_stat_database;` | `SELECT name AS database_name, state_desc AS status FROM sys.databases;` |

## Interface and Implementation Patterns

The [EdFi.Ods.Sandbox](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-ODS/tree/main/Application/EdFi.Ods.Sandbox) library provides a reference implementation for instance management in a sandbox environment. Recommended interface and class structure for the Instance Management Microservice:

* **IInstanceProvisioner.cs** - Interface defining all required operations (_AddInstance_, _CopyInstance_, _DeleteInstance_, _RenameInstance_, _InstanceStatus_). Each provider must implement these methods for its database or storage technology.
* **InstanceProvisionerBase.cs** - Base class for common elements such as connection management and shared variables.
* **InstanceStatus.cs** - Object class representing instance status values (e.g., "ERROR").

### Recommended Implementations

For Admin Console 1.0, provide implementations for Docker and Windows environments:

* **PostgresInstanceProvisioner.cs** - Uses PostgreSQL DDL functions for instance management.
* **SqlServerSandboxProvisioner.cs** - Uses MS-SQL DDL and DBCC commands for instance management.
