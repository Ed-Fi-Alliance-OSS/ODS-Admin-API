# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

#requires -version 5

$ErrorActionPreference = "Stop"
Set-Variable DbDeployVersion -option Constant -value "4.1.52"

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

    Install-Module sqlserver -Force
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

    if (Test-Path $exePath) {
        $existing = &dotnet tool list --tool-path $ToolsPath | Select-String -Pattern $toolPackageName | Out-String
        if ($existing.Contains("$Version")) {
            Write-Host "$toolPackageName is already installed" -ForegroundColor DarkGray
            return $exePath
        }
        else {
            Write-Host "Uninstalling old version of $toolPackageName"
            &dotnet tool uninstall $toolPackageName --tool-path $ToolsPath | Out-Host
        }
    }

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
        $FilePaths
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
        "-p", ($paths -Join ","),
        "--standardVersion", "5.0.0"
    )

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
        $FilePaths
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
        DatabaseEngine = "SqlServer"
        DatabaseType = $DatabaseType
        ConnectionString = $connectionString
        FilePaths = $FilePaths
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
        $DbDeployVersion = $DbDeployVersion,

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
        $FilePaths
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
        $DbDeployVersion = $DbDeployVersion,

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
        $FilePaths
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
        $DbDeployVersion = $DbDeployVersion,

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
        $FilePaths
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
        $DbDeployVersion = $DbDeployVersion,

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
        $FilePath  = "$PSScriptRoot/../Application/EdFi.Ods.AdminApi"
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
    }

    Install-EdFiDatabase @arguments
}

function Invoke-PrepareDatabasesForTesting {
    # This script installs the Ed-Fi databases in preparation for execution of
    # AdminApi integration tests. Requries a SQL Server database.

    param(
        [string]
        [Parameter(Mandatory=$true)]
        [ValidateSet("EdFi.RestApi.Databases.EFA", "EdFi.Suite3.RestApi.Databases.Standard.5.0.0")]
        $RestApiPackageName,

        [string]
        [Parameter(Mandatory=$true)]
        $RestApiPackageVersion,

        [switch]
        $RestApiPackagePrerelease,

        # EdFi.Db.Deploy tool version to use.
        [string]
        $DbDeployVersion = $DbDeployVersion,

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
        $ToolsPath = "$PSScriptRoot/.tools"
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
    $installArguments.DatabaseType = "Security"
    $removeArguments.DatabaseName = "EdFi_Security_Test"
    Write-Host "Installing the Security database to $($installArguments.DatabaseName)" -ForegroundColor Cyan
    Remove-SqlServerDatabase @removeArguments

    Install-EdFiSecurityDatabase @installArguments

    $installArguments.DatabaseName = "EdFi_Admin_Test"
    $installArguments.DatabaseType = "Admin"
    $removeArguments.DatabaseName = "EdFi_Admin_Test"
    Write-Host "Installing the Admin database to $($installArguments.DatabaseName)" -ForegroundColor Cyan
    Remove-SqlServerDatabase @removeArguments
    Install-EdFiAdminDatabase @installArguments

    $installArguments.Remove("RestApiPackagePath")
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