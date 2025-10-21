# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

#requires -version 5

$ErrorActionPreference = "Stop"

Import-Module -Name "$PSScriptRoot/connection-strings.psm1"

function Remove-SqlServerDatabase {
    param (
        # Server or host name.
        [string]
        $Server = "localhost",

        # Database name.
        [string]
        $DatabaseName = "EdFi_Admin",

        # Port number, optional.
        [int]
        $Port,

        # Indicates that integrated security should be used instead of username and password.
        [switch]
        $UseIntegratedSecurity,

        # Database username if not using integrated security.
        [string]
        $Username,

        # Database password if not using integrated security.
        [string]
        $Password
    )

    $arguments = @{
        Server = $Server
        DatabaseName = "master"
        Port = $Port
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username = $Username
        Password = $Password
    }

    $masterConnection = New-ConnectionString @arguments

    $dropDatabase = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$databaseName')
    ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
GO
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$databaseName')
    DROP DATABASE [$databaseName]
GO
"@

    # Ensure the SqlServer module is installed. Don't re-install if already present. 
    if (-not (Get-InstalledModule -Name "SqlServer" -ErrorAction SilentlyContinue)) {
        Install-Module sqlserver -Force
    }
    Import-Module sqlserver -Force
    Invoke-SqlCmd -ConnectionString $masterConnection -Query $dropDatabase
}

function Install-EdFiDbDeploy {
    param (
        [string]
        $ToolsPath,

        [string]
        $Version,

        [string]
        $NuGetFeed
    )

    $toolPackageName = "EdFi.Suite3.Db.Deploy"
    $toolName = "EdFi.Db.Deploy"

    New-Item -Path $ToolsPath -Type Directory -Force | Out-Null

    $exePath = "$ToolsPath/$toolName.exe"

    # v6.2 uses Db.Deploy 3.2.27, version 4.1.52 is for ODS 7.x.
    # This means we need to uninstall any existing version before installing the requested version.

    Write-Host "Uninstalling old version of $toolPackageName"
    &dotnet tool uninstall $toolPackageName --tool-path $ToolsPath | Out-Host

    # if (Test-Path $exePath) {
    #     $existing = &dotnet tool list --tool-path $ToolsPath | Select-String -Pattern $toolPackageName | Out-String
    #     if ($existing.Contains("$Version")) {
    #         Write-Host "$toolPackageName is already installed" -ForegroundColor DarkGray
    #         return $exePath
    #     }
    #     else {
    #         Write-Host "Uninstalling old version of $toolPackageName"
    #         &dotnet tool uninstall $toolPackageName --tool-path $ToolsPath | Out-Host
    #     }
    # }

    Write-Host "Installing $toolPackageName version $Version in $ToolsPath"
    &dotnet tool install $toolPackageName --version $Version --tool-path $ToolsPath --add-source $NuGetFeed | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "Install of EdFi.Db.Deploy failed."
    }

    return $exePath
}

function Invoke-DbDeploy {
    param (
        [string]
        $DbDeployExe = ".tools/EdFi.Db.Deploy.exe",

        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion,

        [string]
        [ValidateSet("PostgreSQL", "SqlServer")]
        $DatabaseEngine = "SqlServer",

        [string]
        [ValidateSet("Admin", "ODS", "Security")]
        [Parameter(Mandatory=$true)]
        $DatabaseType,

        [string]
        [Parameter(Mandatory=$true)]
        $ConnectionString,

        [string[]]
        [Parameter(Mandatory=$true)]
        $FilePaths,

        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion
    )

    # Convert relative to absolute paths
    $paths = @()
    $FilePaths | ForEach-Object {
        $paths += (Resolve-Path $_)
    }

    $arguments = @(
        "deploy",
        "-d", $DatabaseType,
        "-e", $DatabaseEngine,
        "-c", $ConnectionString,
        "-p", ($paths -Join ",")
    )

    # Only include standardVersion parameter for DbDeployVersion 4.1.52 and later
    if ($DbDeployVersion -ne "3.2.27") {
        $arguments += @("--standardVersion", $StandardVersion)
    }

    Write-Host "Executing: $DbDeployExe $(Get-MaskedConnectionString $arguments)" -ForegroundColor Magenta
    if ($DbDeployExe -like "*.exe") {
        $DbDeployExe = $DbDeployExe -replace "\.exe$", ""
    }

    &$DbDeployExe @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Execution of EdFi.Db.Deploy failed."
    }
}

function Install-EdFiDatabase {
    param  (
        # Directory path for storing downloaded tools.
        [string]
        [Parameter(Mandatory=$true)]
        $ToolsPath,

        # EdFi.Db.Deploy tool version to use.
        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion,

        # Ed-Fi NuGet feed for tool download.
        [string]
        [Parameter(Mandatory=$true)]
        $NuGetFeed,

        [string]
        [ValidateSet("Admin", "ODS", "Security")]
        [Parameter(Mandatory=$true)]
        $DatabaseType = "Admin",

        # True if connection string is for a PostgreSQL database. Otherwise for SQL Server.
        [switch]
        $ForPostgreSQL,

        # Server or host name.
        [string]
        [Parameter(Mandatory=$true)]
        $Server,

        # Database name.
        [string]
        [Parameter(Mandatory=$true)]
        $DatabaseName,

        # Port number, optional.
        [int]
        $Port,

        # Indicates that integrated security should be used instead of username and password.
        [switch]
        $UseIntegratedSecurity,

        # Database username if not using integrated security.
        [string]
        $Username,

        # Database password if not using integrated security.
        [string]
        $Password,

        # Hierarchy of directory paths containing database install files.
        [string[]]
        [Parameter(Mandatory=$true)]
        $FilePaths,

        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion
    )

    $arguments = @{
        ToolsPath = $ToolsPath
        Version = $DbDeployVersion
        NuGetFeed = $NuGetFeed
    }

    $dbDeployExe = Install-EdFiDbDeploy @arguments

    $arguments = @{
        ForPostgreSQL = $ForPostgreSQL
        Server = $Server
        DatabaseName = $DatabaseName
        Port = $Port
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username = $Username
        Password = $Password
    }

    $connectionString = New-ConnectionString @arguments

    $arguments = @{
        DbDeployExe = $dbDeployExe
        DbDeployVersion = $DbDeployVersion
        DatabaseEngine = "SqlServer"
        DatabaseType = $DatabaseType
        ConnectionString = $connectionString
        FilePaths = $FilePaths
        StandardVersion = $StandardVersion
    }

    if ($ForPostgreSQL) {
        $arguments.DatabaseEngine = "PostgreSQL"
    }

    Invoke-DbDeploy @arguments
}

function BuildDefaultFilePathArray {
    param (
        [string]
        $FilePaths,

        [string]
        $RestApiPackagePath
    )

    if ($FilePaths) {
        return $FilePaths
    }
    elseif ($RestApiPackagePath) {
        return @(
            "$RestApiPackagePath/Ed-Fi-ODS",
            "$RestApiPackagePath/Ed-Fi-ODS/Application/EdFi.Ods.Standard",
            "$RestApiPackagePath/Ed-Fi-ODS-Implementation"
        )
    }
    else {
        throw "Both 'FilePaths' and 'RestApiPackagePath' arguments are empty"
    }
}

function Install-EdFiAdminDatabase {
    param  (
        # Directory path for storing downloaded tools.
        [string]
        $ToolsPath  = "$PSScriptRoot/.tools",

        # EdFi.Db.Deploy tool version to use.
        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion,

        # Ed-Fi NuGet feed for tool download.
        [string]
        $NuGetFeed,

        # True if connection string is for a PostgreSQL database. Otherwise for SQL Server.
        [switch]
        $ForPostgreSQL,

        # Server or host name.
        [string]
        $Server = "localhost",

        # Database name.
        [string]
        $DatabaseName = "EdFi_Admin",

        # Port number, optional.
        [int]
        $Port,

        # Indicates that integrated security should be used instead of username and password.
        [switch]
        $UseIntegratedSecurity,

        # Database username if not using integrated security.
        [string]
        $Username,

        # Database password if not using integrated security.
        [string]
        $Password,

        # Path to a previously-downloaded EdFi.RestApi.Databases package.
        # If left blank, then must provide $FilePaths.
        [string]
        $RestApiPackagePath,

        # Hierarchy of directory paths containing database install files.
        [string[]]
        $FilePaths,

        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion
    )

    $arguments = @{
        ToolsPath = $ToolsPath
        DbDeployVersion = $DbDeployVersion
        NuGetFeed = $NuGetFeed
        DatabaseType = "Admin"
        ForPostgreSQL = $ForPostgreSQL
        Server = $Server
        DatabaseName = $DatabaseName
        Port = $Port
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username  = $Username
        Password = $Password
        FilePaths = BuildDefaultFilePathArray -FilePath $FilePaths -RestApiPackagePath $RestApiPackagePath
        StandardVersion = $StandardVersion
    }

    Install-EdFiDatabase @arguments
}

function Install-EdFiODSDatabase {
    param  (
        # Directory path for storing downloaded tools.
        [string]
        $ToolsPath  = "$PSScriptRoot/.tools",

        # EdFi.Db.Deploy tool version to use.
        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion,

        # Ed-Fi NuGet feed for tool download.
        [string]
        $NuGetFeed,

        # True if connection string is for a PostgreSQL database. Otherwise for SQL Server.
        [switch]
        $ForPostgreSQL,

        # Server or host name.
        [string]
        $Server = "localhost",

        # Database name.
        [string]
        $DatabaseName = "EdFi_ODS",

        # Port number, optional.
        [int]
        $Port,

        # Indicates that integrated security should be used instead of username and password.
        [switch]
        $UseIntegratedSecurity,

        # Database username if not using integrated security.
        [string]
        $Username,

        # Database password if not using integrated security.
        [string]
        $Password,

        # Path to a previously-downloaded EdFi.RestApi.Databases package.
        # If left blank, then must provide $FilePaths.
        [string]
        $RestApiPackagePath,

        # Hierarchy of directory paths containing database install files.
        [string[]]
        $FilePaths,

        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion
    )

    $arguments = @{
        ToolsPath = $ToolsPath
        DbDeployVersion = $DbDeployVersion
        NuGetFeed = $NuGetFeed
        DatabaseType = "ODS"
        ForPostgreSQL = $ForPostgreSQL
        Server = $Server
        DatabaseName = $DatabaseName
        Port = $Port
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username  = $Username
        Password = $Password
        FilePaths = BuildDefaultFilePathArray -FilePath $FilePaths -RestApiPackagePath $RestApiPackagePath
        StandardVersion = $StandardVersion
    }

    Install-EdFiDatabase @arguments
}

function Install-EdFiSecurityDatabase {
    param  (
        # Directory path for storing downloaded tools.
        [string]
        $ToolsPath  = "$PSScriptRoot/.tools",

        # EdFi.Db.Deploy tool version to use.
        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion,

        # Ed-Fi NuGet feed for tool download.
        [string]
        $NuGetFeed,

        # True if connection string is for a PostgreSQL database. Otherwise for SQL Server.
        [switch]
        $ForPostgreSQL,

        # Server or host name.
        [string]
        $Server = "localhost",

        # Database name.
        [string]
        $DatabaseName = "EdFi_Security",

        # Port number, optional.
        [int]
        $Port,

        # Indicates that integrated security should be used instead of username and password.
        [switch]
        $UseIntegratedSecurity,

        # Database username if not using integrated security.
        [string]
        $Username,

        # Database password if not using integrated security.
        [string]
        $Password,

        # Path to a previously-downloaded EdFi.RestApi.Databases package.
        # If left blank, then must provide $FilePaths.
        [string]
        $RestApiPackagePath,

        # Hierarchy of directory paths containing database install files.
        [string[]]
        $FilePaths,

        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion
    )

    $arguments = @{
        ToolsPath = $ToolsPath
        DbDeployVersion = $DbDeployVersion
        NuGetFeed = $NuGetFeed
        DatabaseType = "Security"
        ForPostgreSQL = $ForPostgreSQL
        Server = $Server
        DatabaseName = $DatabaseName
        Port = $Port
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username  = $Username
        Password = $Password
        FilePaths = BuildDefaultFilePathArray -FilePath $FilePaths -RestApiPackagePath $RestApiPackagePath
        StandardVersion = $StandardVersion
    }

    Install-EdFiDatabase @arguments
}

function Install-AdminApiTables {
    param  (
        # Directory path for storing downloaded tools.
        [string]
        $ToolsPath  = "$PSScriptRoot/.tools",

        # EdFi.Db.Deploy tool version to use.
        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion,

        # Ed-Fi NuGet feed for tool download.
        [string]
        $NuGetFeed,

        [string]
        [ValidateSet("Admin", "ODS", "Security")]
        [Parameter(Mandatory=$true)]
        $DatabaseType = "Admin",

        # True if connection string is for a PostgreSQL database. Otherwise for SQL Server.
        [switch]
        $ForPostgreSQL,

        # Server or host name.
        [string]
        $Server = "localhost",

        # Database name.
        [string]
        $DatabaseName = "EdFi_Admin",

        # Port number, optional.
        [int]
        $Port,

        # Indicates that integrated security should be used instead of username and password.
        [switch]
        $UseIntegratedSecurity,

        # Database username if not using integrated security.
        [string]
        $Username,

        # Database password if not using integrated security.
        [string]
        $Password,

        # Hierarchy of directory paths containing database install files.
        [string[]]
        $FilePath  = "$PSScriptRoot/../Application/EdFi.Ods.AdminApi",

        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion

    )

    $arguments = @{
        ToolsPath = $ToolsPath
        DbDeployVersion = $DbDeployVersion
        NuGetFeed = $NuGetFeed
        DatabaseType = $DatabaseType
        ForPostgreSQL = $ForPostgreSQL
        Server = $Server
        DatabaseName = $DatabaseName
        Port = $Port
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username  = $Username
        Password = $Password
        FilePaths = @( $FilePath )
        StandardVersion = $StandardVersion
    }

    Install-EdFiDatabase @arguments
}

function Invoke-PrepareDatabasesForTesting {
    # This script installs the Ed-Fi databases in preparation for execution of
    # AdminApi integration tests. Requries a SQL Server database.

    param(
        [string]
        [Parameter(Mandatory=$true)]
        [ValidateSet("EdFi.Suite3.RestApi.Databases", "EdFi.Suite3.RestApi.Databases.Standard.5.2.0")]
        $RestApiPackageName,

        [string]
        [Parameter(Mandatory=$true)]
        $RestApiPackageVersion,

        [switch]
        $RestApiPackagePrerelease,

        # EdFi.Db.Deploy tool version to use.
        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion,

        # Ed-Fi NuGet feed for tool download.
        [string]
        $NuGetFeed,

        [string]
        $DbServer = "localhost",

        [int]
        $DbPort = 1433,

        [Switch]
        $UseIntegratedSecurity,

        [string]
        $DbUsername,

        [string]
        $DbPassword,

        [string]
        $PackagesPath = "$PSScriptRoot/.packages",

        [string]
        $ToolsPath = "$PSScriptRoot/.tools",
        
        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion
    )

    Import-Module -Name "$PSScriptRoot/package-manager.psm1" -Force
    Import-Module -Name "$PSScriptRoot/database-manager.psm1" -Force

    $arguments = @{
        RestApiPackageVersion = $RestApiPackageVersion
        RestApiPackageName = $RestApiPackageName
        PackagesPath = $PackagesPath
        NuGetFeed = $NuGetFeed
        ToolsPath = $ToolsPath
        RestApiPackagePrerelease = $RestApiPackagePrerelease
    }

    $dbPackagePath = Get-RestApiPackage @arguments

    $installArguments = @{
        ToolsPath = $ToolsPath
        DbDeployVersion = $DbDeployVersion
        NuGetFeed = $NuGetFeed
        Server = $DbServer
        DatabaseName = "EdFi_Ods_Empty_Test"
        Port = $DbPort
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username = $DbUsername
        Password = $DbPassword
        RestApiPackagePath = $dbPackagePath
        StandardVersion = $StandardVersion
    }
    $removeArguments = @{
        Server = $DbServer
        DatabaseName = $installArguments.DatabaseName
        Port = $DbPort
        UseIntegratedSecurity = $UseIntegratedSecurity
        Username = $DbUsername
        Password = $DbPassword
    }

    $installArguments.DatabaseName = "EdFi_Security_Test"
    $removeArguments.DatabaseName = "EdFi_Security_Test"
    Write-Host "Installing the Security database to $($installArguments.DatabaseName)" -ForegroundColor Cyan
    Remove-SqlServerDatabase @removeArguments

    Install-EdFiSecurityDatabase @installArguments

    $installArguments.DatabaseName = "EdFi_Admin_Test"
    $removeArguments.DatabaseName = "EdFi_Admin_Test"
    Write-Host "Installing the Admin database to $($installArguments.DatabaseName)" -ForegroundColor Cyan
    Remove-SqlServerDatabase @removeArguments
    Install-EdFiAdminDatabase @installArguments

    $installArguments.Remove("RestApiPackagePath")
    $installArguments.DatabaseType = "Admin"
    Write-Host "Installing the Admin App tables to $($installArguments.DatabaseName)" -ForegroundColor Cyan
    Install-AdminApiTables @installArguments
}

$exports = @(
    "Install-EdFiDbDeploy",
    "Install-EdFiAdminDatabase",
    "Install-EdFiODSDatabase",
    "Install-EdFiSecurityDatabase",
    "Install-AdminApiTables",
    "Remove-SqlServerDatabase",
    "Invoke-PrepareDatabasesForTesting"
)

Export-ModuleMember -Function $exports