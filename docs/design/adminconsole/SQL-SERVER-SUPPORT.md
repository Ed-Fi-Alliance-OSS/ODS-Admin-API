# Adding MS SQL Support to Instance Management

## Summary

The Instance Management worker allows system administrators to manage instances of their ODS. This worker is currently configured to support PostgreSQL and needs support added for SQL Server.

## Background

The Instance Management worker was designed to execute ODS administration tasks without tying up a handler's main process. This asynchronous nature requires the Instance management worker to manage other aspects of its environment, including persistence and compatibility with other services. The data layer for the Instance Management worker was implemented using PostgreSQL, with the idea of using this dialect of SQL to start, verifying its implementation, then including support for SQL Server after.

The Postgres implementation of the Instance Manager includes support for Creating, Renaming, Deleting, and Restoring ODS Instances.

There is a Postgres DB dump of sample student data that serves as a starting point for minimal and populated templates. No such sample exists for SQL Server so this will need to be solved in order to remain in parity with the Postgres provisioning features. Additionally, SQL Server utilizes a slightly different dialect from server so these actions must be converted. Lastly, SQL Server licensing requires that images / containers including the Azure SQL Server base image not be distributed. This does not prohibit providing instructions on how to build these images.

Azure SQL Server support is an additional consideration but will remain out of the scope of this implementation.

## Design Overview

Adding support for SQL Server has been divided into the following four distinct areas of focus below.

### 1. Configuring and connecting to the database

The first step involves adding a connection and corresponding configuration to the application. This step is to ensure the application is communicating with the desired SQL Server instance (Platform hosted, docker hosted). There is a `CreateConnection()` method in the [`SqlServerSandboxProvisioner.cs`](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-ODS/blob/main/Application/EdFi.Ods.Sandbox/Provisioners/SqlServerSandboxProvisioner.cs) example from ODS Sandbox, which demonstrates how to connect properly.

### 2. Translating the actions to MS SQL dialect for each of the admin functions

The current Postgres implementation of the Instance Management Worker borrows from the provisioner patterns seen in the `EdFi.Ods.Sandbox` project.

The `PostgresSandboxProvisioner.cs` class contains methods for creating connections, renaming, deleting,  managing file paths, retrieving DB Status among others. These actions can be used to inform the implementation details for the Instance Management worker and the corresponding SQL Server actions. These actions will need to be translated to MS SQL and added to a new`SqlServerInstanceProvisioner.cs` file located in the `Provisioners/` directory.

Note, for restoring the database, the SqlServerSandboxProvisioner.cs from the API Sandbox uses a `CopySandboxAsync` command which sets up internal functions to backup a target and restore to a new destination. An internal function in particular, `GetBackupDirectoryAsync` uses the windows registry to locate the backup directory of the server. This will need to change, as SQL Server can run on platforms outside of Windows. The design details will configures a backup location the host and the application can read.

Other supporting configuration files, e.g. `Provisioners/DatabaseEngineProvider.cs` will also need to be updated to reflect added support for SQL Server, resulting in a seamless experience between selecting the PostgreSQL engine and SQL Server.

### 3. Low-friction SQL Server connection setup

The next task is providing a low-friction environment for users to spin up and connect to the desired SQL Server instance. Historically, Ed-Fi has provided guidance to users on how to provision SQL Server configurations for various environments. This is done to avoid hosting images containing the distribution itself, which would create conflict with the Apache 2.0 license that accompanies the Ed-Fi Alliance source code and tools.

SQL Server Options that Ed-Fi provides guidance:

1. Installation included with official binaries
2. Experimental, bare MSSQL install scripts
3. Docker compose with local sample data (Users SQL Server Express Edition)

A quick reference for setting up SQL Server runtime can be found at the following [tech docs guide](https://docs.ed-fi.org/reference/docker/#step-2-setup-runtime-environment). Because the Instance Management worker already takes advantage of Docker compose, option three appears to provide the most benefit for little effort. This will still allow users to get up and running quickly without spending much time provisioning a host machine.

### 4. Seeding data for restoration

Lastly, the Instance Management worker should be able to restore an ODS instance. This by extension means that the worker must support exporting the data, creating the instance, and importing the data to a created instance.

The SQL Server database will need to be populated in order to provide the necessary data for export. Two viable options to consider for migrating the data include:

1. Creating the Sample data and tables directly using synthesized data.
2. Transforming Sample data from PostgreSQL backups

It appears that the MSSQL Sandbox compose files include a populated template that connects to a predefined volume. An approach can be to connect to this instance then export the to a BACPAC file, which can be used for restoration.

Once this is done successfully, the next step is to implement the restoration of this BACPAC template data. An action can be added to the interface that runs the necessary steps within the transaction that results in creating an instance and subsequently reading the BACPAC.

## Design Details

The following adds additional implementation details on the design overview provided above.

### 1. Configuring and connecting to the Database

This step ensure that a Database connection can be established using the provided connection string settings.

There is an `InstanceProvisionerBase.cs` class that is responsible for retrieving the connection strings and configuration settings when the provisioner is instantiated. This class also set up abstractions for creating a DbConnection, which can be overwritten for use in the SQL Engine specific Provisioner. The following is used to create a PostgreSQL DB connection:

```csharp
protected override DbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);
```

The `CreateConnection()` method is then used throughout the Provisioner to create a connection object for executing queries against.

```csharp
using (var conn = CreateConnection())
//...remaining implementation with conn.
```

The _databaseNameBuilder.SandboxNameForKey(clientKey) will likely need to be modified with multi-tenancy under consideration.

### 2. Translate actions to MS SQL Dialect

The next lift is to translate each of the DB Provisioning methods to the appropriate MS SQL Dialect

#### Create

The PostgreSQL provisioner implementation of `CopyDbInstanceAsync` container includes a `CREATE` statement that uses a `TEMPLATE` database. The SQL Server implementation does not support Db templates, so this will need to user a bak file to configure the populated template. This bak file will be restored when a Create DB instance command is issued.

The Admin API Sandbox has SqlServerSandboxProvisioner.cs which implements a method `CopySandboxAsync`. This method  illustrates the restoration method used with SQL Server and the backup file. This excerpt summarizes the process by calling internal functions near the top, making it easier to follow along.

```csharp
            using (var conn = CreateConnection())
            {
                try
                {
                    await conn.OpenAsync();

                    // This points to where the template should be saved
                    string backupDirectory = await GetBackupDirectoryAsync()
                        .ConfigureAwait(false);

                    _logger.Debug($"backup directory = {backupDirectory}");

                    string backup = await PathCombine(backupDirectory, originalDatabaseName + ".bak");
                    _logger.Debug($"backup file = {backup}");

                    var sqlFileInfo = await GetDatabaseFilesAsync(originalDatabaseName, newDatabaseName)
                        .ConfigureAwait(false);

                    await BackUpAndRestoreSandbox()
                        .ConfigureAwait(false);
                        //..........
                }
                // ........
            }
```

Note that the `GetBackupDirectoryAsync` will need to use a value other than the `HKEY_LOCAL_MACHINE` for retrieving the backup directory location. This could be replaced with an AppSettings configuration and will need to be configurable when using the RESTORE functionality.

#### Delete

Deleting instances from the Management Worker context requires removing the Database and tables associated with the client key. For reference, the Admin API Sandbox does the following:

```csharp
            using (var conn = CreateConnection())
            {
                foreach (string key in deletedClientKeys)
                {
                    await conn.ExecuteAsync($@"
                         IF EXISTS (SELECT name from sys.databases WHERE (name = '{_databaseNameBuilder.SandboxNameForKey(key)}'))
                        BEGIN
                            ALTER DATABASE [{_databaseNameBuilder.SandboxNameForKey(key)}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE [{_databaseNameBuilder.SandboxNameForKey(key)}];
                        END;
                        ", commandTimeout: CommandTimeout)
                        .ConfigureAwait(false);
                }
            }
```

The above is used to iterate through DB associated with the client key to perform the drop.

#### Rename

Renaming an instance is a two part process. The first is renaming the instances and tables themselves. This can be done with an ALTER command similar to this:

```csharp
await conn.ExecuteAsync($"ALTER DATABASE {oldName} MODIFY Name = {newName};")
                    .ConfigureAwait(false);
```

The next step is updating data to reference this new instance. The client credentials and secrets table will need to be updated - care must be exercised to ensure renaming a table doest not impact the effectiveness of these credentials.

Lastly, the system will need a fallback for handling potential reads while renaming the system. One approach is to spin up the new instance while the old is still running, then switch the configuration once the new instance has been set up. This will help mitigate downtime, but coordinating this will add complexity and potentially require more resources.

Another approach is to simple take down the existing instance, rename and wait until the system is available again. This requires less complexity to manage but does increase downtime.

Deciding on a best approach will depending on what level of unavailability is acceptable.

#### Restore

The Restoration will behave similar to the create process. The main difference is the restore will target a custom .bak file provided by a user, while the create will use a predefined .bak that will scaffold necessary tables and data.

The configuration used to set the backup path in AppSettings can be used to locate the .bak file the user would like restored. The restoration then becomes similar to the create, but with a targeted file to restore.

### 3. Low-friction SQL Server Setup

We will explore using the Docker Compose environment for configuring and setting up the SQL Server and connecting with the Admin API. This will require us to configure the SQL Server container and network settings in a way that works with the Admin API but also portable for easy for developers.

#### Build SQL Server compatible image

Ensure we can configure and run an SQL Server image with the right requirements.

#### Start up compose network

Reference the build file to create an image and configure compose network. This allows compose to con3figure the runtime settings of the SQL Server container.

#### Ensure running and connect directly via Docker host

Once the container is running using compose, confirm the services are behaving as expected by connecting via the Docker host. This might involve using the host machine to connect to the shell of the SQL Server container.

#### Pass connection settings to connect via application

Once the container has been verified, derive the connection string and pass to the application to ensure connection to SQL Server container is valid.

### 4. Seeding Data for restoration

This last section involves provisioning the Template data that will be used during the CREATE instance action. We will need the schema, tables, roles and DB set up so that the necessary configuration is provided when a user requests a new instance.

## Test Strategy

The following user journeys represent areas critical to instance management using SQL Server.

An admin can connect to a SQL Server.

* Provision the server
* Create connection string
* Ensure that the application connect to server successfully

An admin can add a new SQL Server ODS instance

* Connect to the SQL Server Instance
* Execute command to create a new DB Instance and tables
* Demonstrate new instance and tables are available

An admin can rename an existing SQL Server ODS instance

* Connect to the SQL Server Instance
* Execute command to rename ODS instance
* Ensure DB and corresponding tables and connection strings are updated
* Add additional path checking duplicate name for rename does not exist

An admin delete a new SQL Server ODS instance

* Connect to the SQL Server instance
* Execute command to delete instance
* Ensure instance and corresponding tables are properly marked for deletion.

An admin can restore a new SQL Server ODS instance

* Connect to the SQL Server instance
* Execute the create command providing a name and source of the restoration
* Ensure a new instance exists with provided restoration data (dbs, tables, and rows)

## Outstanding Questions

When renaming and instance, what is the expectation around down time?

When searching for an instance to rename or delete by key, is it possible for a client key to reference more than one ODS instance?
