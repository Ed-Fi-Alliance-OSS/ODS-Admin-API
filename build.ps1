# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

[CmdLetBinding()]
<#
    .SYNOPSIS
        Automation script for running build operations from the command line.

    .DESCRIPTION
        Provides automation of the following tasks:

        * Clean: runs `dotnet clean`
        * Build: runs `dotnet build` with several implicit steps
          (clean, restore, inject version information).
        * UnitTest: executes NUnit tests in projects named `*.UnitTests`, which
          do not connect to a database.
        * IntegrationTest: executes NUnit tests in projects named `*.Test`,
          which connect to a database. Includes drop and deploy operations for
          installing fresh test databases.
        * BuildAndTest: executes the Build, UnitTest, and IntegrationTest
          commands.
        * Package: builds pre-release and release NuGet packages for the Admin
          App web application.
        * Push: uploads a NuGet package to the NuGet feed.
        * BuildAndDeployToAdminApiDockerContainer: runs the build operation, update the appsettings.json with provided
          DockerEnvValues and copy over the latest files to existing AdminApi docker container for testing.

    .EXAMPLE
        .\build.ps1 build -Configuration Release -Version "2.0" -BuildCounter 45

        Overrides the default build configuration (Debug) to build in release
        mode with assembly version 2.0.45.

    .EXAMPLE
        .\build.ps1 unittest

        Output: test results displayed in the console and saved to XML files.

    .EXAMPLE
        .\build.ps1 integrationtest

        Output: test results displayed in the console and saved to XML files.

    .EXAMPLE
        .\build.ps1 push -NuGetApiKey $env:nuget_key

    .EXAMPLE
       $p = @{
            Authority        = "http://api"
            IssuerUrl        = "https://localhost:5001"
            DatabaseEngine   = "PostgreSql"
            PathBase         = "adminapi"
            SigningKey       = "<Generated encryption key>"
            AdminDB          = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Admin;Application Name=EdFi.Ods.AdminApi;"
            SecurityDB       = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Security;Application Name=EdFi.Ods.AdminApi;"
        }

        .\build.ps1 -APIVersion "2.2.0" -Configuration Release -DockerEnvValues $p -Command BuildAndDeployToAdminApiDockerContainer
    .EXAMPLE
       $p = @{
            Authority        = "http://api"
            IssuerUrl        = "https://localhost"
            DatabaseEngine   = "PostgreSql"
            PathBase         = "adminapi"
            SigningKey       = "test"
            AdminDB          = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Admin;Application Name=EdFi.Ods.AdminApi;"
            SecurityDB       = "host=db-admin;port=5432;username=username;password=password;database=EdFi_Security;Application Name=EdFi.Ods.AdminApi;"
        }

        ./build.ps1 -APIVersion ${{ inputs.version }} -Configuration Release -DockerEnvValues $p -Command GenerateOpenAPIAndMD

#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', '', Justification = 'False positive')]
param(
    # Command to execute, defaults to "Build".
    [string]
    [ValidateSet("Clean", "Build", "GenerateOpenAPIAndMD", "BuildAndPublish", "UnitTest", "IntegrationTest", "PackageApi"
        , "Push", "BuildAndTest", "BuildAndDeployToAdminApiDockerContainer"
        , "BuildAndRunAdminApiDevDocker", "RunAdminApiDevDockerContainer", "RunAdminApiDevDockerCompose", "Run", "CopyToDockerContext", "RemoveDockerContextFiles")]
    $Command = "Build",

    # Assembly and package version number for Admin API. The current package number is
    # configured in the build automation tool and passed to this script.
    [string]
    $APIVersion = "0.1",

    # .NET project build configuration, defaults to "Debug". Options are: Debug, Release.
    [string]
    [ValidateSet("Debug", "Release")]
    $Configuration = "Debug",

    # Ed-Fi's official NuGet package feed for package download and distribution.
    [string]
    $EdFiNuGetFeed = "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_packaging/EdFi/nuget/v3/index.json",

    # API key for accessing the feed above. Only required with with the Push
    # command.
    [string]
    $NuGetApiKey,

    # Full path of a package file to push to the NuGet feed. Optional, only
    # applies with the Push command. If not set, then the script looks for a
    # NuGet package corresponding to the provided $APIVersion and $BuildCounter.
    [string]
    $PackageFile,

    # Environment values for updating the appsettings on existing AdminApi docker container.

    # Only required with the BuildAndDeployToAdminApiDockerContainer or BuildAndDeployToAdminApiDockerContainer commands.
    [hashtable]
    $DockerEnvValues,

    # Only required with the Run command.
    [string]
    [ValidateSet("EdFi.Ods.AdminApi (Dev)", "EdFi.Ods.AdminApi (Prod)", "EdFi.Ods.AdminApi (Docker)", "IIS Express")]
    $LaunchProfile,

    # Only required with local builds and testing.
    [switch]
    $IsLocalBuild,

    # Option to run coverlet for code coverage analysis, only applicable when running tests
    [switch]
    $RunCoverageAnalysis,

    # Option to run integration tests with or without integrated security
    [Switch]
    $UseIntegratedSecurity = $true,

    # Option to run integration tests with no integrated security
    [string]
    $DbUsername,

    # Option to run integration tests with no integrated security
    [string]
    $DbPassword
)

$Env:MSBUILDDISABLENODEREUSE = "1"

$solutionRoot = "$PSScriptRoot/Application"
$dockerRoot = "$PSScriptRoot/Docker"

$supportedApiVersions7x = @(
    @{
        OdsPackageName = "EdFi.Suite3.RestApi.Databases.Standard.5.2.0"
        OdsVersion     = "7.3.10536"
        Prerelease     = $false
        StandardVersion = "5.2.0"
        DbDeployVersion = "4.1.52"
    }
)
$supportedApiVersions6x = @(
    @{
        OdsPackageName = "EdFi.Suite3.RestApi.Databases"
        OdsVersion     = "6.2.3630"
        Prerelease     = $false
        StandardVersion = "4.0.0"           # v6.2 uses Db.Deploy 3.2.27, version 4.1.52 is for ODS 7.x.
        DbDeployVersion = "3.2.27"
    }
)
$maintainers = "Ed-Fi Alliance, LLC and contributors"

$appCommonPackageName = "EdFi.Installer.AppCommon"
$appCommonPackageVersion = "3.0.0"

# Code coverage analysis
$script:coverageOutputFile = "coverage.cobertura.xml"
$script:targetDir = "coveragereport"

$script:RunCoverageAnalysis = $RunCoverageAnalysis

Import-Module -Name "$PSScriptRoot/eng/build-helpers.psm1" -Force
Import-Module -Name "$PSScriptRoot/eng/package-manager.psm1" -Force
Import-Module -Name "$PSScriptRoot/eng/database-manager.psm1" -Force
function DotNetClean {
    Invoke-Execute { dotnet clean $solutionRoot -c $Configuration --nologo -v minimal }
}
function InitializeNuGet {
    Invoke-Execute { $script:nugetExe = Install-NugetCli }
}

function Restore {
    Invoke-Execute { dotnet restore $solutionRoot }
}

function SetAdminApiAssemblyInfo {
    Invoke-Execute {
        $assembly_version = $APIVersion

        Invoke-RegenerateFile "$solutionRoot/EdFi.Ods.AdminApi/Directory.Build.props" @"
<Project>
    <!-- This file is generated by the build script. -->
    <PropertyGroup>
        <Product>Ed-Fi ODS Admin API</Product>
        <Authors>$maintainers</Authors>
        <Company>$maintainers</Company>
        <Copyright>Copyright © ${(Get-Date).year)} Ed-Fi Alliance</Copyright>
        <VersionPrefix>$assembly_version</VersionPrefix>
        <VersionSuffix></VersionSuffix>
    </PropertyGroup>
</Project>
"@
    }
}

function Compile {
    Invoke-Execute {
        dotnet build $solutionRoot -c $Configuration --nologo --no-restore
    }
}

function GenerateOpenAPI {
    Invoke-Execute {
        Push-Location $solutionRoot/EdFi.Ods.AdminApi/
        $outputOpenAPI = "../../docs/api-specifications/openapi-yaml/admin-api-$APIVersion.yaml"
        $dllPath = "./bin/Release/net8.0/EdFi.Ods.AdminApi.dll"

        try {
            dotnet tool run swagger tofile --output $outputOpenAPI --yaml $dllPath v2
        }
        finally {
            Pop-Location
        }
    }
}

function GenerateDocumentation {
    Invoke-Execute {
        $outputOpenAPI = "docs/api-specifications/openapi-yaml/admin-api-$APIVersion.yaml"
        $outputMD = "docs/api-specifications/markdown/admin-api-$APIVersion-summary.md"
        widdershins --search false --omitHeader true --code true --summary $outputOpenAPI -o $outputMD
    }
}

function PublishAdminApi {
    Invoke-Execute {
        $project = "$solutionRoot/EdFi.Ods.AdminApi/"
        $outputPath = "$project/publish"
        dotnet publish $project -c $Configuration /p:EnvironmentName=Production -o $outputPath --no-build --nologo
    }
}

function RunTests {
    param (
        # File search filter
        [string]
        $Filter
    )

    $testAssemblyPath = "$solutionRoot/$Filter/"
    $testAssemblies = Get-ChildItem -Path $testAssemblyPath -Filter "$Filter.csproj" -Recurse

    if ($testAssemblies.Length -eq 0) {
        Write-Output "no test assemblies found in $testAssemblyPath"
    }

    $testAssemblies | ForEach-Object {
        Write-Output "Executing: dotnet test $($_)"
        Invoke-Execute {
            if ($script:RunCoverageAnalysis)
            {
                & dotnet test $_ --collect 'XPlat Code Coverage' --logger "trx;LogFileName=$($_).trx" --nologo
            }
            else
            {
                & dotnet test $_ --logger "trx;LogFileName=$($_).trx" --nologo
            }
        }
    }
}

function GenerateCoverageReport {
    param (
        [string]
        $Filter = "**"
    )

    Invoke-Execute {
        reportgenerator -reports:"Application/$Filter/TestResults/**/*.cobertura.xml" -targetdir:"$script:targetDir" -reporttypes:Html
    }
}

function UnitTests {
    Invoke-Execute { RunTests -Filter "*.UnitTests" }
}

function ResetTestDatabases {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'unused', Justification = 'False positive')]
    param (
        [string]
        $OdsPackageName,

        [string]
        $OdsVersion,

        [switch]
        $Prerelease,

        [switch]
        $UseIntegratedSecurity,

        [string]
        $DbUsername,

        [string]
        $DbPassword,
        
        [string]
        [Parameter(Mandatory=$true)]
        $StandardVersion,

        [string]
        [Parameter(Mandatory=$true)]
        $DbDeployVersion
    )

    Invoke-Execute {
        $arguments = @{
            RestApiPackageVersion    = $OdsVersion
            RestApiPackageName       = $OdsPackageName
            UseIntegratedSecurity    = $UseIntegratedSecurity
            StandardVersion          = $StandardVersion
            RestApiPackagePrerelease = $Prerelease
            NuGetFeed                = $EdFiNuGetFeed
            DbUsername               = $DbUsername
            DbPassword               = $DbPassword
            DbDeployVersion          = $DbDeployVersion
        }

        Invoke-PrepareDatabasesForTesting @arguments
    }
}

function IntegrationTests7x {
    Invoke-Execute { RunTests -Filter "*AdminApi.DBTests" }
}

function IntegrationTests6x {
    Invoke-Execute { RunTests -Filter "*AdminApi.V1.DBTests" }
}

function RunNuGetPack {
    param (
        [string]
        $ProjectPath,

        [string]
        $PackageVersion,

        [string]
        $nuspecPath
    )

    # NU5100 is the warning about DLLs outside of a "lib" folder. We're
    # deliberately using that pattern, therefore we don't care about the
    # warning.
    # NU5110 is the warning about ps1 files outside the "tools" folder
    # NU5111 is a warning about an unrecognized ps1 filename
    dotnet pack $ProjectPath --output $PSScriptRoot -p:NuspecFile=$nuspecPath -p:NuspecProperties="version=$PackageVersion" /p:NoWarn='"NU5100;NU5110;NU5111"'
}

function NewDevCertificate {
    Invoke-Command { dotnet dev-certs https -c }
    if ($lastexitcode) {
        Write-Output "Generating a new Dev Certificate"
        Invoke-Execute { dotnet dev-certs https --clean }
        Invoke-Execute { dotnet dev-certs https -t }
    }
    else {
        Write-Output "Dev Certificate already exists"
    }
}

function AddAppCommonPackageForInstaller {
    $project = "EdFi.Ods.AdminApi"
    $mainPath = "$solutionRoot/$project"
    $destinationPath = "$mainPath/publish"

    $arguments = @{
        AppCommonPackageName    = $appCommonPackageName
        AppCommonPackageVersion = $appCommonPackageVersion
        NuGetFeed               = $EdFiNuGetFeed
        DestinationPath         = $destinationPath
    }

    Add-AppCommon @arguments
}

function BuildApiPackage {
    $project = "EdFi.Ods.AdminApi"
    $mainPath = "$solutionRoot/$project"
    $projectPath = "$mainPath/$project.csproj"
    $nugetSpecPath = "$mainPath/publish/$project.nuspec"

    RunNuGetPack -ProjectPath $projectPath -PackageVersion $APIVersion $nugetSpecPath
}

function Invoke-Build {
    Invoke-Step { DotNetClean }
    Invoke-Step { Restore }
    Invoke-Step { Compile }
}

function Invoke-GenerateOpenAPIAndMD {
    Invoke-Step { UpdateAppSettingsForAdminApi }
    Invoke-Step { DotNetClean }
    Invoke-Step { Restore }
    Invoke-Step { Compile }
    Invoke-Step { GenerateOpenAPI }
    Invoke-Step { GenerateDocumentation }
}

function Invoke-SetAssemblyInfo {
    Write-Output "Setting Assembly Information"

    Invoke-Step { SetAdminApiAssemblyInfo }
}

function Invoke-Publish {
    Write-Output "Building Version AdminApi ($APIVersion)"

    Invoke-Step { PublishAdminApi }
}

function Invoke-Run {
    Write-Output "Running Admin API"

    Invoke-Step { NewDevCertificate }

    $projectFilePath = "$solutionRoot/EdFi.Ods.AdminApi"

    if ([string]::IsNullOrEmpty($LaunchProfile)) {
        Write-Error "LaunchProfile parameter is required for running Admin Api. Please " +
        "specify the LaunchProfile parameter. Valid values include ""EdFi.Ods.AdminApi (Dev)""" +
        ", ""EdFi.Ods.AdminApi (Prod)"", ""EdFi.Ods.AdminApi (Docker)"", and ""IIS Express"""
    }
    else {
        Invoke-Execute { dotnet run --project $projectFilePath --launch-profile $LaunchProfile }
    }
}

function Invoke-Clean {
    Invoke-Step { DotNetClean }
}

function Invoke-UnitTestSuite {
    Invoke-Step { UnitTests }
}

function Invoke-IntegrationTestSuite {
    param (
        [Switch]
        $UseIntegratedSecurity,

        [string]
        $DbUsername,

        [string]
        $DbPassword
    )

    $supportedApiVersions7x | ForEach-Object {
        Write-Output "Running Integration Tests for ODS Version" $_.OdsVersion

        Invoke-Step {
            $arguments = @{
                OdsVersion              = $_.OdsVersion
                OdsPackageName          = $_.OdsPackageName
                Prerelease              = $_.Prerelease
                StandardVersion         = $_.StandardVersion
                DbDeployVersion         = $_.DbDeployVersion
                UseIntegratedSecurity   = $UseIntegratedSecurity
                DbUsername              = $DbUsername
                DbPassword              = $DbPassword
            }

            ResetTestDatabases @arguments
        }
        Invoke-Step {
            IntegrationTests7x
        }
    }

    $supportedApiVersions6x | ForEach-Object {
        Write-Output "Running Integration Tests for ODS Version" $_.OdsVersion

        Invoke-Step {
            $arguments = @{
                OdsVersion              = $_.OdsVersion
                OdsPackageName          = $_.OdsPackageName
                Prerelease              = $_.Prerelease
                StandardVersion         = $_.StandardVersion
                DbDeployVersion         = $_.DbDeployVersion
                UseIntegratedSecurity   = $UseIntegratedSecurity
                DbUsername              = $DbUsername
                DbPassword              = $DbPassword
            }

            ResetTestDatabases @arguments
        }
        Invoke-Step {
            IntegrationTests6x
        }
    }
}

function Invoke-BuildApiPackage {
    Invoke-Step { AddAppCommonPackageForInstaller }
    Invoke-Step { BuildApiPackage }
}

function Invoke-BuildDatabasePackage {
    Invoke-Step { BuildDatabasePackage }
}

function UpdateAppSettingsForAdminApiDocker {
    $filePath = "$solutionRoot/EdFi.Ods.AdminApi/appsettings.json"
    $json = (Get-Content -Path $filePath) | ConvertFrom-Json
    $json.AppSettings.DatabaseEngine = $DockerEnvValues["DatabaseEngine"]
    $json.AppSettings.PathBase = $DockerEnvValues["PathBase"]

    $json.Authentication.IssuerUrl = $DockerEnvValues["IssuerUrl"]
    $json.Authentication.SigningKey = $DockerEnvValues["SigningKey"]

    $json.ConnectionStrings.EdFi_Admin = $DockerEnvValues["AdminDB"]
    $json.ConnectionStrings.EdFi_Security = $DockerEnvValues["SecurityDB"]
    $json.Log4NetCore.Log4NetConfigFileName = "./log4net.config"
    $json | ConvertTo-Json -Depth 10 | Set-Content $filePath
}

function UpdateAppSettingsForAdminApi {
    $filePath = "$solutionRoot/EdFi.Ods.AdminApi/appsettings.json"
    $json = (Get-Content -Path $filePath) | ConvertFrom-Json
    $json.AppSettings.DatabaseEngine = $DockerEnvValues["DatabaseEngine"]
    $json.AppSettings.PathBase = $DockerEnvValues["PathBase"]

    $json.Authentication.IssuerUrl = $DockerEnvValues["IssuerUrl"]
    $json.Authentication.SigningKey = $DockerEnvValues["SigningKey"]

    $json.ConnectionStrings.EdFi_Admin = $DockerEnvValues["AdminDB"]
    $json.ConnectionStrings.EdFi_Security = $DockerEnvValues["SecurityDB"]
    $json.Log4NetCore.Log4NetConfigFileName = "log4net/log4net.config"
    $json | ConvertTo-Json -Depth 10 | Set-Content $filePath
}

function CopyLatestFilesToAdminApiContainer {
    $source = "$solutionRoot/EdFi.Ods.AdminApi/bin/Release/net8.0/."
    &docker cp $source adminapi:/app/AdminApi
}

function RestartAdminApiContainer {
    &docker restart adminapi
}

function BuildAdminApiDevDockerImage {
    Push-Location $dockerRoot
    try {
        ">>> Building dev.pgsql.Dockerfile" | Out-Host
        &docker build `
            -t adminapi-dev-pgsql `
            --build-context assets=$(Resolve-Path "..") `
            --no-cache `
            -f "dev.pgsql.Dockerfile" `
            .

        ">>> Building dev.mssql.Dockerfile" | Out-Host
        &docker build `
            -t adminapi-dev-mssql `
            --build-context assets=$(Resolve-Path "..") `
            --no-cache `
            -f "dev.mssql.Dockerfile" `
            .
    }
    finally {
        Pop-Location
    }
}

function RunAdminApiDevDockerContainer {
    &docker run --env-file "$solutionRoot/EdFi.Ods.AdminApi/.env" -p 80:80 -v "$dockerRoot/Settings/ssl:/ssl/" adminapi-dev-pgsql
}

function RunAdminApiDevDockerCompose {
    &docker compose -f "$dockerRoot/Compose/pgsql/compose-build-dev.yml" --env-file "$solutionRoot/EdFi.Ods.AdminApi/E2E Tests/gh-action-setup/.automation.env" -p "ods_admin_api" up -d
}

function PushPackage {
    if (-not $NuGetApiKey) {
        throw "Cannot push a NuGet package without providing an API key in the `NuGetApiKey` argument."
    }

    if (-not $PackageFile) {
        $PackageFile = "$PSScriptRoot/EdFi.Suite3.ODS.AdminApi.$APIVersion.nupkg"
    }

    $arguments = @{
        PackageFile = $PackageFile
        NuGetApiKey = $NuGetApiKey
        NuGetFeed   = $EdFiNuGetFeed
    }

    Invoke-Execute { Push-Package @arguments }
}
function Invoke-AdminApiDockerDeploy {
    Invoke-Step { UpdateAppSettingsForAdminApiDocker }
    Invoke-Step { CopyLatestFilesToAdminApiContainer }
    Invoke-Step { RestartAdminApiContainer }
}

function Invoke-BuildAdminApiDevDockerImage {
    Invoke-Step { BuildAdminApiDevDockerImage }
}

function Invoke-RunAdminApiDevDockerContainer {
    Invoke-Step { RunAdminApiDevDockerContainer }
}

function Invoke-RunAdminApiDevDockerCompose {
    Invoke-Step { RunAdminApiDevDockerCompose }
}

function Invoke-PushPackage {
    Invoke-Step { PushPackage }
}

Invoke-Main {
    if ($IsLocalBuild) {
        $nugetExePath = Install-NugetCli
        Set-Alias nuget $nugetExePath -Scope Global -Verbose
    }
    switch ($Command) {
        Clean { Invoke-Clean }
        Build { Invoke-Build }
        GenerateOpenAPIAndMD { Invoke-GenerateOpenAPIAndMD }
        BuildAndPublish {
            Invoke-SetAssemblyInfo
            Invoke-Build
            Invoke-Publish
        }
        Run { Invoke-Run }
        UnitTest {
            Invoke-UnitTestSuite

            if ($script:RunCoverageAnalysis) {
                Invoke-Step { GenerateCoverageReport }
            }
        }
        IntegrationTest {
            $arguments = @{
                UseIntegratedSecurity    = $UseIntegratedSecurity
                DbUsername               = $DbUsername
                DbPassword               = $DbPassword
            }

            Invoke-IntegrationTestSuite @arguments

            if ($script:RunCoverageAnalysis) {
                Invoke-Step { GenerateCoverageReport }
            }
        }
        BuildAndTest {
            $arguments = @{
                UseIntegratedSecurity    = $UseIntegratedSecurity
                DbUsername               = $DbUsername
                DbPassword               = $DbPassword
            }

            Invoke-Build
            Invoke-UnitTestSuite
            Invoke-IntegrationTestSuite @arguments

            if ($script:RunCoverageAnalysis) {
                Invoke-Step { GenerateCoverageReport }
            }
        }
        Package { Invoke-BuildPackage }
        PackageApi { Invoke-BuildApiPackage }
        Push { Invoke-PushPackage }
        BuildAndDeployToAdminApiDockerContainer {
            Invoke-Build
            Invoke-AdminApiDockerDeploy
        }
        BuildAndRunAdminApiDevDocker {
            Invoke-BuildAdminApiDevDockerImage
            Invoke-RunAdminApiDevDockerContainer
        }
        RunAdminApiDevDockerContainer {
            Invoke-RunAdminApiDevDockerContainer
        }
        RunAdminApiDevDockerCompose {
            Invoke-RunAdminApiDevDockerCompose
        }
        default { throw "Command '$Command' is not recognized" }
    }
}