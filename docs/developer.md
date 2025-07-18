# Admin API Developer Instructions

## Contents

* [Admin API Developer Instructions](#admin-api-developer-instructions)
  * [Contents](#contents)
  * [Development Pre-Requisites](#development-pre-requisites)
  * [Build Script](#build-script)
  * [Running on Localhost](#running-on-localhost)
    * [Configuring Admin API to Run with the ODS/API](#configuring-admin-api-to-run-with-the-odsapi)
    * [Resetting the Database State](#resetting-the-database-state)
    * [Running Locally in Docker](#running-locally-in-docker)
    * [Using Keycloak (IDP)](#using-keycloak-idp)
    * [Running Unit Tests, Integration Tests, and Generating Code Coverage Reports](#running-unit-tests-integration-tests-and-generating-code-coverage-reports)
  * [Application Architecture](#application-architecture)
    * [Database Layer](#database-layer)
    * [Validation](#validation)

## Development Pre-Requisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Suggested to have either:
  * [Visual Studio 2022](https://visualstudio.microsoft.com/downloads), or
  * [Visual Studio 2022 Build
    Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)
    (install the ".NET Build Tools" component)
* Clone [this
  repository](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-ODS-AdminApi) locally
* To work with the official Ed-Fi Docker solution, also clone the [Docker
  repository](https://github.com/Ed-Fi-Alliance-OSS/Ed-Fi-ODS-Docker).

## Build Script

The PowerShell script `build.ps1` in the root directory contains functions for
running standard build operations at the command line . This script assumes that
.NET 8.0 SDK or newer is installed. Other dependencies tools are downloaded
as needed (nuget, nunit).

Available command (e.g. `./build.ps1 clean`) (commands are not case sensitive):

* `clean`: runs `dotnet clean`
* `build`: runs `dotnet build` with several implicit steps
    (clean, restore, inject version information).
* `unitTest`: executes NUnit tests in projects named `*.UnitTests`, which
    do not connect to a database.
* `integrationTest`: executes NUnit tests in projects named `*.Test`,
    which connect to a database. Includes drop and deploy operations for
    installing fresh test databases compatible with Ed-Fi ODS/API 3.4 and 5.3.
* `buildAndTest`: executes the Build, UnitTest, and IntegrationTest
    commands.
* `package`: builds pre-release and release NuGet packages for the Admin
    App web application.
* `push`: uploads a NuGet package to the NuGet feed.
* `buildAndDeployToAdminApiDockerContainer`: runs the build operation, update
    the `appsettings.json` with provided Docker environment variables and copy
    over the latest files to existing AdminApi docker container for testing.
* `run`: runs the Admin API

Note: use the `IsLocalBuild` switch to install NuGet.exe if you do not already
have it in your local path.

Review the parameters at the top of `build.ps1` for additional command line
arguments.

## Running on Localhost

There are three ways of running Admin API:

* `build.ps1 run` to run from the command line.
* Run inside a container with the help of
  [compose-build-dev.yml](../Docker/Compose/pgsql/compose-build-dev.yml).
* Start from Visual Studio to take advantage of easy debugging integration.

There are several launch profiles available either with `build.ps1` or when
running from Visual Studio. Review
[launchSettings](../Application/EdFi.Ods.AdminApi/Properties/launchSettings.json)
for more information. Note that you should use the "Prod" launch setting when
running the Postman-based E2E tests.

### Configuring Admin API to Run with the ODS/API

Some features of the Admin API require interaction with the ODS/API. For that
reason, you may need to launch an instance of the ODS/API. The
`compose-build-dev.yml` file handles this for you.

With the other two options, you will need to startup the [ODS/API on your
own]((https://techdocs.ed-fi.org/display/ETKB/Ed-Fi+Operational+Data+Store+and+API)),
using Visual Studio or the command line. Do not start the ODS/API in a separate
Docker network from Admin API, because at present both need to access the same
`EdFi_Admin` and `EdFi_Security` databases. If starting up manually, make sure
that both Admin API and ODS/API have the same deployment setting:

* `sharedinstance`
* `yearspecific`
* `districtspecific`

In Admin API, you set this value in the `appsettings.json` file under
`AppSettings.ApiStartupType`. In the ODS/API, you set it as the
`ApiSettings.Mode` in the `appsettings.json` (ODS/API 5.x and 6.x) or in
`web.config` (ODS/API 3.x). Please see ODS/API documentation for more
information about the differences between these startup modes.

Admin API has a small set of database tables for internal management, which need
to be installed into the `EdFi_Admin` database. The SQL scripts for this are
under the [Artifacts](../Application/EdFi.Ods.AdminApi/Artifacts/) directory.
They can be installed from PowerShell with the following command:

```powershell
# From AdminApi clone directory
cd eng

# Accepting the default options: MSSQL, on localhost, using integrated security, using EdFi_Admin
./run-dbup-migrations.ps1

# Alternately, override all available settings
$config = @{
    "engine" = "PostgreSQL"
    "databaseServer" = "alternate-hostname"
    "databasePort" = "5466"
    "databaseUser" = "postgres"
    "databasePassword" = "a-strong-password"
    "useIntegratedSecurity" = $false
    "adminDatabaseName" = "EdFi_Admin_543"
}
./run-dbup-migrations.ps1 $config
```

### Resetting the Database State

If you need to reset your databases to a clean slate, run appropriate PowerShell
commands in the `Ed-Fi-ODS-Implementation` repository.

```powershell
pushd ../Ed-Fi-ODS-Implementation

# Checkout the ODS/API version you need to test with, for example v6.1, v5.3, v5.2, v5.1.1
git checkout v6.1

# Load PowerShell functions
./Initialize-PowershellForDevelopment.ps1

# Wipe out and re-install both databases
Reset-AdminDatabase
Reset-SecurityDatabase

# Wipe out and re-install the databases used by the DB integration tests
Reset-TestAdminDatabase
Reset-TestSecurityDatabase

# Re-run the Admin API migration scripts
popd

eng/run-dbup-migrations.ps1
```

### Running Locally in Docker

As mentioned above, you can run locally in Docker. See [docker.md](docker.md)
for more information.

### Using Keycloak (IDP)

To use Keycloak for authenticating the API, you need to configure the parameters in the OIDC section. Additionally, you must specify with `"UseSelfcontainedAuthorization": false`, that the API’s own authentication will be disabled in favor of using Keycloak.

Furthermore, when using Keycloak, the Register and Token endpoints will not be available in Swagger or for direct calls. Attempting to access these endpoints will result in a 404 error.

```json
{
  "Authentication": {
    "IssuerUrl": "",
    "SigningKey": "",
    "AllowRegistration": false,
    "OIDC": {
      "Authority": "https://localhost/auth/realms/edfi-admin-console",
      "ValidateIssuer": true,
      "RequireHttpsMetadata": false,
      "EnableServerCertificateCustomValidationCallback": true
    }
  }
}
```

### Running Unit Tests, Integration Tests, and Generating Code Coverage Reports

The source code includes two main types of test projects:

* **Unit tests** (`*.UnitTests`):
  * Run with: `build.ps1 -Command UnitTest`
  * To collect code coverage, use: `build.ps1 -Command UnitTest -RunCoverageAnalysis`  
    This will generate an HTML report in the `coveragereport` directory.

* **Integration tests** (`*.DBTests`):  
  These tests exercise the repository layer and require a database connection.
  * Run with: `build.ps1 -Command IntegrationTest`
  * To collect code coverage, use: `build.ps1 -Command IntegrationTest -RunCoverageAnalysis`  
    This will also generate an HTML report in the `coveragereport` directory.

Alternatively, you can run both unit and integration tests together with:  
`build.ps1 -Command BuildAndTest [-RunCoverageAnalysis]`

> [!NOTE]
> Code coverage analysis requires the `reportgenerator` tool.  
> Install it with: `dotnet tool install -g dotnet-reportgenerator-globaltool`

Additionally, there is a set of end-to-end (E2E) tests in Postman.  
See [E2E Tests/README.md](../Application/EdFi.Ods.AdminApi/E2E%20Tests/README.md) for more information.

All three test suites should pass successfully before merging new code into the `main` branch.

## Application Architecture

The Admin API source code uses ASP.NET but it does not follow the traditional
ASP.NET model-view-controller (MVC) approach. Instead it relies on defining
Features and then automating the API routing to those features. Conceptually,
each Feature _is_ a Controller, though it does not inherit from the .NET
`Controller` class.

Each Feature implements the `IFeature` interface, which has a single function.
That function enables the feature to define its own HTTP endpoint route. The
Feature then has `Handle` function the contains the normal controller logic. Its
arguments are automatically interpreted and handled by the ASP.NET Core
dependency injection framework.

### Database Layer

Database interaction is mediated through [Entity Framework
Core](https://learn.microsoft.com/en-us/ef/core/) with two interfaces:
`IUsersContext` handles the `EdFi_Admin` database and `ISecurityContext` handles
the `EdFi_Security` database. The name "IUsersContext" derives from the admin
database being used to hold Admin App users and now Admin API client
credentials.

### Validation

Validation of API requests is configured via
[FluentValidation](https://docs.fluentvalidation.net/en/latest/).
