
# Instance Management Service Layer Design

## Interface and Implementation Patterns

The [EdFi.Ods.Sandbox](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-ODS/tree/main/Application/EdFi.Ods.Sandbox) library provides a reference implementation for instance management in a sandbox environment.

* **IInstanceProvisioner.cs** - Interface defining all required operations (_AddInstance_, _CopyInstance_, _DeleteInstance_, _InstanceInfo_, _InstanceStatus_). Each provider must implement these methods for its database or storage technology.
* **InstanceProvisionerBase.cs** - Base class for common elements such as connection management and shared variables.
* **InstanceStatus.cs** - Object class representing instance status values (e.g., "ERROR").

### Recommended Implementations

Provide implementations for Docker and Windows environments:

* **PostgresInstanceProvisioner.cs** - Uses PostgreSQL DDL functions for instance management.
* **SqlServerSandboxProvisioner.cs** - Uses MS-SQL DDL and DBCC commands for instance management.

## Implementation Details

The Instance Management Service Layer orchestrates database instance operations (create, copy, delete, status, info) across supported database platforms. The implementation uses a provider pattern for extensibility and separation of concerns.

### Key Components

* **IInstanceProvisioner**  
 Defines the contract for all instance management operations:
  * `AddInstance`
  * `CopyInstance`
  * `DeleteInstance`
  * `InstanceInfo`
  * `InstanceStatus`
 Each database provider (e.g., PostgreSQL, SQL Server) implements this interface.

* **InstanceProvisionerBase**  
 Provides shared logic for connection management, error handling, and logging. Concrete provisioners inherit from this base class.

* **InstanceStatus**  
 Represents the status and metadata of a managed instance (e.g., status, size, last modified).

### Provider Implementations

* **PostgresInstanceProvisioner**  
 Implements instance management using PostgreSQL DDL commands and system views. Handles database creation, cloning (via template), deletion, and status queries.

* **SqlServerSandboxProvisioner**  
 Implements instance management for Microsoft SQL Server using DDL, BACKUP/RESTORE, and DBCC commands. Handles file movement and status queries.

### Service Layer Workflow

1. **Request Handling**  
  The service receives a request (e.g., create, delete, status, info) and selects the appropriate provisioner based on configuration or instance type.

2. **Operation Execution**  
  The selected provisioner executes the requested operation using the appropriate SQL commands and updates status objects.

3. **Error Handling**  
  All operations are wrapped with error handling and logging. Failures are reported via the `InstanceStatus` object.

4. **Extensibility**  
  New database providers can be added by implementing the `IInstanceProvisioner` interface and registering the implementation.

### Example Usage

```csharp
IInstanceProvisioner provisioner = GetProvisionerFor(instanceType);
var status = await provisioner.AddInstance(instanceConfig);
if (status.IsError)
{
  // Handle error, log details
}
```

## Example SQL Commands

The following SQL commands illustrate how these operations are performed in PostgreSQL and Microsoft SQL environments:

| Operation        | PostgreSQL                                                   | Microsoft SQL                                                |
| ---------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Create Database    | `CREATE DATABASE new_database;`                              | `CREATE DATABASE new_database;`                              |
| Copy Database      | `CREATE DATABASE new_database_name TEMPLATE existing_database_name;` | `BACKUP DATABASE existing_database_name TO DISK = 'C:\path\to\temp_backup.bak';`<br />`RESTORE DATABASE new_database_name FROM DISK = 'C:\path\to\temp_backup.bak' WITH MOVE 'existing_database_data' TO 'C:\path\to\new_database_data.mdf', MOVE 'existing_database_log' TO 'C:\path\to\new_database_log.ldf';` |
| Delete Database    | `DROP DATABASE database_name;`                               | `DROP DATABASE database_name;`                               |
| Instance Info      | `SELECT pg_size_pretty(pg_database_size('database_name')) AS size;` | `USE database_name; EXEC sp_spaceused;`                      |
| Instance Status    | `SELECT datname AS database_name,        numbackends AS active_connections FROM pg_stat_database;` | `SELECT name AS database_name, state_desc AS status FROM sys.databases;` |
