# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

#requires -version 5

$ErrorActionPreference = "Stop"

function Set-TlsVersion {

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls11 -bor [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
}

$appCommonDirectory = "$PSScriptRoot/AppCommon"
$RequiredDotNetHostingBundleVersion = "6.0.0"

Import-Module -Force "$appCommonDirectory/Environment/Prerequisites.psm1" -Scope Global
Set-TlsVersion

Import-Module -Force "$appCommonDirectory/Utility/hashtable.psm1" -Scope Global
Import-Module -Force "$appCommonDirectory/Utility/nuget-helper.psm1"
Import-Module -Force "$appCommonDirectory/Utility/TaskHelper.psm1"
Import-Module -Force "$appCommonDirectory/Utility/ToolsHelper.psm1"

# Import the following with global scope so that they are available inside of script blocks
Import-Module -Force "$appCommonDirectory/Application/Install.psm1" -Scope Global
Import-Module -Force "$appCommonDirectory/Application/Uninstall.psm1" -Scope Global
Import-Module -Force "$appCommonDirectory/Application/Configuration.psm1" -Scope Global

$DbDeployVersion = "3.0.1"

function Install-EdFiOdsAdminApi {
    <#
    .SYNOPSIS
        Installs the AdminApi application into IIS.

    .DESCRIPTION
        Installs and configures the AdminApi application in IIS running in Windows 10 or
        Windows Server 2016+. As needed, will create a new "Ed-Fi" website in IIS, configure it
        for HTTPS, and load the Admin Api binaries as an an application. Transforms the appsettings
        and invokes dbup migrations.
    .EXAMPLE
        PS c:\> $dbConnectionInfo = @{
            Server = "(local)"
            Engine = "SqlServer"
            UseIntegratedSecurity=$true
        }
        PS c:\> $parameters = @{
            ToolsPath = "C:/temp/tools"
            DbConnectionInfo = $dbConnectionInfo
        }
        PS c:\> Install-EdFiOdsAdminApi @parameters

        Installs Admin Api to SQL Server with mainly defaults, except for the custom Sandbox ODS
        and the required ODS/API URL.

    .EXAMPLE
        PS c:\> $parameters = @{
            ToolsPath = "C:/temp/tools"
            AdminDbConnectionInfo = @{
                Engine="SqlServer"
                Server="edfi-auth.my-sql-server.example"
                UseIntegratedSecurity=$true
            }
            SecurityDbConnectionInfo = @{
                Engine="SqlServer"
                Server="edfi-auth.my-sql-server.example"
                UseIntegratedSecurity=$true
            }
        }
        PS c:\> Install-EdFiOdsAdminApi @parameters

        Installs Admin Api to multiple SQL Servers for each of the databases with some
        using integrated security and some using username and password credentials,
        along with a custom staging ODS name and the required ODS/API URL.

    .EXAMPLE
        PS c:\> $dbConnectionInfo = @{
            Server = "localhost"
            Engine = "PostgreSQL"
            Username="install-user"
            Password="@#$%^&*(GHJ%^&*YUKSDF"
        }
        PS c:\> $parameters = @{
            ToolsPath = "C:/temp/tools"
            DbConnectionInfo = $dbConnectionInfo
            InstallCredentialsUseIntegratedSecurity = $true
        }
        PS c:\> Install-EdFiOdsAdminApi @parameters

        Installs Admin Api to PostgreSQL Server with integrated security install credentials and
        username and password application credentials, along with the required ODS/API URL.

    .EXAMPLE
        PS c:\> $dbConnectionInfo = @{
            Server = "(local)"
            Engine = "SqlServer"
            UseIntegratedSecurity=$true
        }
        PS c:\> $parameters = @{
            ToolsPath = "C:/temp/tools"
            DbConnectionInfo = $dbConnectionInfo
        }
        PS c:\> Install-EdFiOdsAdminApi @parameters

        The installer will install Admin Api in ASP.NET Identity mode, rather than AD Authentication.
    #>
    [CmdletBinding()]
    param (
        # NuGet package name. Default: EdFi.Suite3.ODS.AdminApi.
        [string]
        $PackageName = "EdFi.Suite3.ODS.AdminApi",

        # NuGet package version. This value is replaced by the build process.
        [string]
        $PackageVersion = "__ADMINAPI_VERSION__",

        # NuGet package source. Please specify the path to the AdminApi sub-directory within the AdminApi nuget package.
        [Parameter(Mandatory=$true)]
        [string]
        $PackageSource,

        # Path for storing installation tools, e.g. nuget.exe. Default: "C:\temp\tools".
        [string]
        $ToolsPath = "C:\temp\tools",

        # Path for storing downloaded packages
        [string]
        $DownloadPath = "C:\temp\downloads",

        # Path for the IIS WebSite. Default: c:\inetpub\Ed-Fi.
        [string]
        $WebSitePath = "c:\inetpub\Ed-Fi",

        # Web site name. Default: "Ed-Fi".
        [string]
        $WebsiteName = "Ed-Fi",

        # Web site port number. Default: 443.
        [int]
        $WebSitePort = 443,

        # Web application name. Default: "AdminApi".
        [string]
        $WebApplicationName = "AdminApi",

        # TLS certificiate thumbprint, optional. When not set, a self-signed certificate will be created.
        [string]
        $CertThumbprint,

        # Install Credentials: User
        [string]
        $InstallCredentialsUser,

        # Install Credentials: Password
        [string]
        $InstallCredentialsPassword,

        # Install Credentials: UseIntegratedSecurity setting
        [switch]
        $InstallCredentialsUseIntegratedSecurity,

        # Admin Database Name
        [string]
        [Parameter(ParameterSetName="SharedCredentials")]
        $AdminDatabaseName = "EdFi_Admin",

        # ODS Database Name
        [string]
        [Parameter(ParameterSetName="SharedCredentials")]
        $OdsDatabaseName = "EdFi_Ods",

        # Security Database Name
        [string]
        [Parameter(ParameterSetName="SharedCredentials")]
        $SecurityDatabaseName = "EdFi_Security",

        # Shared database connectivity information.
        #
        # The hashtable must include: Server, Engine (SqlServer or PostgreSQL), and
        # either UseIntegratedSecurity or Username and Password (Password can be skipped
        # for PostgreSQL when using pgconf file). Optionally can include Port.
        # This can be used with IsMultiTenant flag.
        [hashtable]
        [Parameter(Mandatory=$true, ParameterSetName="SharedCredentials")]
        [Parameter(ParameterSetName="MultiTenant")]
        $DbConnectionInfo,

        # Database connectivity only for the admin database.
        #
        # The hashtable must include: Server, Engine (SqlServer or PostgreSQL), and
        # either UseIntegratedSecurity or Username and Password (Password can be skipped
        # for PostgreSQL when using pgconf file). Optionally can include Port and
        # DatabaseName.
        [hashtable]
        [Parameter(Mandatory=$true, ParameterSetName="SeparateCredentials")]
        $AdminDbConnectionInfo,

        # Database connectivity only for the security database.
        #
        # The hashtable must include: Server, Engine (SqlServer or PostgreSQL), and
        # either UseIntegratedSecurity or Username and Password (Password can be skipped
        # for PostgreSQL when using pgconf file). Optionally can include Port and
        # DatabaseName.
        [hashtable]
        [Parameter(Mandatory=$true, ParameterSetName="SeparateCredentials")]
        $SecurityDbConnectionInfo,

        # Authentication settings for Admin Api.
        [hashtable]
        [Parameter(Mandatory=$true)]
        $AuthenticationSettings,

        # Database Config
        [switch]
        $NoDuration,

        # Deploy Admin Api with MultiTenant support.
        # Passing this flag, requires to pass Tenants configuration.
        # When true, this flag will enable the MultiTenancy flag in Appsettings.
        [switch]
        [Parameter(Mandatory=$true, ParameterSetName="MultiTenant")]
        $IsMultiTenant,

        # List of Tenants with information required by the Tenants section in appsettings.json
        #
        # Each tenant hashtable can include:
        #   - AdminDatabaseName and SecurityDatabaseName when used with DbConnectionInfo.
        #   - AdminDbConnectionInfo and SecurityDbConnectionInfo when DbConnectionInfo is not used.
        [hashtable]
        [Parameter(Mandatory=$true, ParameterSetName="MultiTenant")]
        $Tenants,

        # Set Encrypt=false for all connection strings
        # Not recomended for production environment.
        [switch]
        $UnEncryptedConnection
    )

    Write-InvocationInfo $MyInvocation

    Clear-Error

    $result = @()

    $Config = @{
        WebApplicationPath = (Join-Path $WebSitePath $WebApplicationName)
        PackageName = $PackageName
        PackageVersion = $PackageVersion
        PackageSource = $PackageSource
        ToolsPath = $ToolsPath
        DownloadPath = $DownloadPath
        WebSitePath = $WebSitePath
        WebSiteName = $WebsiteName
        WebSitePort = $WebsitePort
        CertThumbprint = $CertThumbprint
        WebApplicationName = $WebApplicationName
        DatabaseInstallCredentials = @{
            DatabaseUser = $InstallCredentialsUser
            DatabasePassword = $InstallCredentialsPassword
            UseIntegratedSecurity = $InstallCredentialsUseIntegratedSecurity
        }
        AdminDatabaseName = $AdminDatabaseName
        OdsDatabaseName = $OdsDatabaseName
        SecurityDatabaseName = $SecurityDatabaseName
        DbConnectionInfo = $DbConnectionInfo
        AdminDbConnectionInfo = $AdminDbConnectionInfo
        SecurityDbConnectionInfo = $SecurityDbConnectionInfo
        AuthenticationSettings = $AuthenticationSettings
        NoDuration = $NoDuration
        IsMultiTenant = $IsMultiTenant.IsPresent
        Tenants = $Tenants
        UnEncryptedConnection = $UnEncryptedConnection
    }

    if($IsMultiTenant.IsPresent)
    {
        Write-Warning "Please make sure required tenant specific Admin, Security databases are already available on the data server."
    }

    $elapsed = Use-StopWatch {
        $result += Invoke-InstallationPreCheck -Config $Config
        $result += Initialize-Configuration -Config $config
        $result += Set-AdminApiPackageSource -Config $Config
        $result += Get-DbDeploy -Config $Config
        $result += Invoke-TransformAppSettings -Config $Config

        if ($IsMultiTenant.IsPresent) {
            $result += Invoke-TransformMultiTenantConnectionStrings -Config $config
        } else {
            $result += Invoke-TransformConnectionStrings -Config $config
        }

        $result += Install-Application -Config $Config
        $result += Set-SqlLogins -Config $Config
        $result += Invoke-DbUpScripts -Config $Config

        $result
    }

    Test-Error

    if (-not $NoDuration) {
        $result += New-TaskResult -name "-" -duration "-"
        $result += New-TaskResult -name $MyInvocation.MyCommand.Name -duration $elapsed.format
        $result | Format-Table
    }
}

function Update-EdFiOdsAdminApi {
    <#
    .SYNOPSIS
        Upgrade the AdminApi application in IIS.

    .DESCRIPTION
        Upgrades and configures the AdminApi application in IIS running in Windows 10 or
        Windows Server 2016+. Admin Api will be upgraded and reside under "Ed-Fi" website in IIS.
        Appsettings values and connection strings will be copied over from existing Admin Api application.
        Invokes dbup migrations for updating the EdFi_Admin database accordingly.
    .EXAMPLE
        PS c:\> $parameters = @{
            PackageVersion = "__ADMINAPI_VERSION__"
        }
        PS c:\> Upgrade-AdminApi @parameters

        Upgrades Admin Api to specified version and preseve the appsettings values and connection strings from existing application.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Default')]
    param (
        # NuGet package name. Default: EdFi.Suite3.ODS.AdminApi.
        [string]
        $PackageName = "EdFi.Suite3.ODS.AdminApi",

        # NuGet package version. This value is replaced by the build process.
        [string]
        $PackageVersion = "__ADMINAPI_VERSION__",

        # NuGet package source. Please specify the path to the AdminApi sub-directory within the AdminApi nuget package.
        [Parameter(Mandatory=$true)]
        [string]
        $PackageSource,

        # Path for storing installation tools, e.g. nuget.exe. Default: "C:\temp\tools".
        [string]
        $ToolsPath = "C:\temp\tools",

        # Path for storing downloaded packages
        [string]
        $DownloadPath = "C:\temp\downloads",

        # Path for the IIS WebSite. Default: c:\inetpub\Ed-Fi.
        [string]
        $WebSitePath = "c:\inetpub\Ed-Fi",

        # Web site name. Default: "Ed-Fi".
        [string]
        $WebsiteName = "Ed-Fi",

        # Web site port number. Default: 443.
        [int]
        $WebSitePort = 443,

        # Web application name. Default: "AdminApi".
        [string]
        $WebApplicationName = "AdminApi",

        # TLS certificiate thumbprint, optional. When not set, a self-signed certificate will be created.
        [string]
        $CertThumbprint,

        # Install Credentials: User
        [Parameter(ParameterSetName="InstallCredentials")]
        [string]
        $InstallCredentialsUser,

        # Install Credentials: Password
        [Parameter(ParameterSetName="InstallCredentials")]
        [string]
        $InstallCredentialsPassword,

        # Install Credentials: UseIntegratedSecurity setting
        [Parameter(ParameterSetName="InstallCredentials")]
        [switch]
        $InstallCredentialsUseIntegratedSecurity,

        # The hashtable must include: Server, Engine (SqlServer or PostgreSQL), and
        # either UseIntegratedSecurity or Username and Password (Password can be skipped
        # for PostgreSQL when using pgconf file). Optionally can include Port and
        # DatabaseName.
        [hashtable]
        [Parameter(Mandatory=$true, ParameterSetName="InstallCredentials")]
        $AdminDbConnectionInfo,

        # Database Config
        [switch]
        $NoDuration
    )

    Write-InvocationInfo $MyInvocation

    Clear-Error

    $result = @()

    $Config = @{
        WebApplicationPath = (Join-Path $WebSitePath $WebApplicationName)
        PackageName = $PackageName
        PackageVersion = $PackageVersion
        PackageSource = $PackageSource
        DatabaseInstallCredentials = @{
            DatabaseUser = $InstallCredentialsUser
            DatabasePassword = $InstallCredentialsPassword
            UseIntegratedSecurity = $InstallCredentialsUseIntegratedSecurity
        }
        ToolsPath = $ToolsPath
        DownloadPath = $DownloadPath
        WebSitePath = $WebSitePath
        WebSiteName = $WebsiteName
        WebSitePort = $WebsitePort
        CertThumbprint = $CertThumbprint
        WebApplicationName = $WebApplicationName
        NoDuration = $NoDuration
        ApplicationInstallType = "Upgrade"
        AdminDbConnectionInfo = $AdminDbConnectionInfo
    }

    $elapsed = Use-StopWatch {
        $result += Invoke-ResetIIS
        $result += Invoke-ApplicationUpgrade -Config $Config
        $result += Set-AdminApiPackageSource -Config $Config
        $result += Get-DbDeploy -Config $Config
        $result += Invoke-TransferAppsettings -Config $Config
        $result += Invoke-TransferConnectionStrings -Config $Config
        $result += Install-Application -Config $Config
        $result += Invoke-DbUpScripts -Config $Config
        $result += Invoke-StartWebSite $Config.WebSiteName $Config.WebSitePort

        $result
    }

    Test-Error

    if (-not $NoDuration) {
        $result += New-TaskResult -name "-" -duration "-"
        $result += New-TaskResult -name $MyInvocation.MyCommand.Name -duration $elapsed.format
        $result | Format-Table
    }
}

function Uninstall-EdFiOdsAdminApi {
    <#
    .SYNOPSIS
        Removes the AdminApi web application from IIS.
    .DESCRIPTION
        Removes the AdminApi web application from IIS, including its application
        pool (if not used for any other application). Removes the web site as well if
        there are no remaining applications, and the site's app pool.

        Does not remove IIS or the URL Rewrite module.

    .EXAMPLE
        PS c:\> Uninstall-EdFiOdsAdminApi

        Uninstall using all default values.
    .EXAMPLE
        PS c:\> $p = @{
            WebSiteName="Ed-Fi-3"
            WebApplicationPath="c:/inetpub/Ed-Fi-3/AdminApi-3"
            WebApplicationName = "AdminApi-3"
        }
        PS c:\> Uninstall-EdFiOdsAdminApi @p

        Uninstall when the web application and web site were setup with non-default values.
    #>
    [CmdletBinding()]
    param (
        # Path for storing installation tools, e.g. nuget.exe. Default: "./tools".
        [string]
        $ToolsPath = "$PSScriptRoot/tools",

        # Path for the web application. Default: "c:\inetpub\Ed-Fi\AdminApi".
        [string]
        $WebApplicationPath = "C:\inetpub\Ed-Fi\AdminApi",

        # Web application name. Default: "AdminApi".
        [string]
        $WebApplicationName = "AdminApi",

        # Web site name. Default: "Ed-Fi".
        [string]
        $WebSiteName = "Ed-Fi",

        # Turns off display of script run-time duration.
        [switch]
        $NoDuration
    )

    $config = @{
        ToolsPath = $ToolsPath
        WebApplicationPath = $WebApplicationPath
        WebApplicationName = $WebApplicationName
        WebSiteName = $WebSiteName
    }

    $result = @()

    $elapsed = Use-StopWatch {

        Invoke-ResetIIS

        UninstallAdminApi $config

        $result
    }

    Test-Error

    if (-not $NoDuration) {
        $result += New-TaskResult -name "-" -duration "-"
        $result += New-TaskResult -name $MyInvocation.MyCommand.Name -duration $elapsed.format
        $result | Format-Table
    }
}

function UninstallAdminApi($config)
{
    $parameters = @{
        WebApplicationPath = $config.WebApplicationPath
        WebApplicationName = $config.WebApplicationName
        WebSiteName = $config.WebSiteName
    }

    Uninstall-EdFiApplicationFromIIS @parameters
}

function Invoke-InstallationPreCheck{
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        $existingWebSiteName = $Config.WebsiteName
        $webSite = Get-Website | Where-Object { $_.name -eq $existingWebSiteName }
        $existingAdminApiApplication = get-webapplication $Config.WebApplicationName

        if($webSite -AND $existingAdminApiApplication)
        {
            $existingApplicationPath, $versionString = GetExistingAppVersion $webSite.PhysicalPath $existingAdminApiApplication
            $installVersionString = $Config.PackageVersion

            $targetIsNewer = IsVersionHigherThanOther $installVersionString $versionString

            if($targetIsNewer) {
                Write-Host "We found a preexisting Admin Api package version $versionString installation. If you are seeking to upgrade to the new version, consider using the included upgrade script instead." -ForegroundColor Green
                Write-Host "Note: Using the upgrade script, all the appsettings and database connection string values would be copied forward from the existing installation, so only continue if you are you seeking to change the configuration." -ForegroundColor Yellow

                $confirmation = Request-Information -DefaultValue 'y' -Prompt "Please enter 'y' to continue the installation process, or enter 'n' to cancel the installation so that you can instead run the upgrade script"

                if(-not ($confirmation -ieq 'y')) {
                    Write-Host "Exiting."
                    exit
                }else {
                    $appsettingsFile =  Join-Path $existingApplicationPath "appsettings.json"
                    if(Test-Path -Path $appsettingsFile)
                    {
                        Write-Host "To ensure your existing ODS / API Key and Secret values will continue to work, your existing encryption key is being copied forward from the appsettings.json file at $appsettingsFile"
                        $appSettings = Get-Content $appsettingsFile | ConvertFrom-Json | ConvertTo-Hashtable
                    }
                }
            }elseif ($targetIsNewer) {
                Write-Warning "We found a preexisting Admin Api package version $versionString installation. That version cannot be automatically upgraded in-place by this script. Please refer to https://techdocs.ed-fi.org/display/ADMIN/Upgrading+Admin+App+from+1.x+Line for setting up the newer version of AdminApi. Exiting."
                exit
            }else {
                Write-Warning "We found a preexisting Admin Api package version $versionString installation newer than the target version $installVersionString. Downgrades are not supported. Please fully uninstall the existing Admin Api first and retry. Exiting."
                exit
            }
        }
    }
}

function Invoke-ApplicationUpgrade {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )
    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {

        $existingWebSiteName = $Config.WebsiteName
        $webSite = Get-Website | Where-Object { $_.name -eq $existingWebSiteName }
        if($null -eq $webSite)
        {
            Write-Warning "Unable to find $existingWebSiteName on IIS."
            $customWebSiteName = Request-Information -DefaultValue "Ed-Fi" -Prompt "Ed-Fi applications are usually deployed in IIS underneath a 'Ed-Fi' website entry. If you previously installed with a custom name for that entry other than 'Ed-Fi', please enter that custom name"
            $customWebSite = Get-Website | Where-Object { $_.name -eq $customWebSiteName }
            if($null -eq $customWebSite)
            {
                throw "Unable to find $customWebSite on IIS. Please use install.ps1 for installing Ed-Fi website."
            }
            $webSite = $customWebSite
            $existingWebSiteName =  $customWebSiteName
        }
        $existingWebSitePath = ($webSite).PhysicalPath

        $existingAppName = $Config.WebApplicationName
        $existingAdminApi = get-webapplication $existingAppName
        if($null -eq $existingAdminApi)
        {
            Write-Warning "Unable to find $existingAppName on IIS."
            $customApplicationName = Request-Information -DefaultValue "AdminApi" -Prompt "If you previously installed AdminApi with a custom name, please enter that custom name"
            $customAdminApiApplication = get-webapplication $customApplicationName
            if($null -eq $customAdminApiApplication)
            {
                throw "Unable to find $customAdminApiApplication on IIS. Please use install.ps1 for installing AdminApi application."
            }
            $existingAdminApi =  $customAdminApiApplication
            $existingAppName = $customApplicationName
        }

        $existingApplicationPath, $versionString = CheckForCompatibleUpdate $existingWebSitePath $existingAdminApi $Config.PackageVersion

        Write-Host "Stopping the $existingWebSiteName before taking application files back up."
        Stop-IISSite -Name $existingWebSiteName

        Write-Host "Taking back up on existing application folder $existingApplicationPath"
        $date = Get-Date -Format yyyy-MM-dd-hh-mm
        $backupApplicationFolderName = "$existingAppName-$versionString-$date"
        $basePath = (get-item $existingApplicationPath).parent.FullName
        $destinationBackupPath = "$basePath\$backupApplicationFolderName\"

        if(Test-Path -Path $destinationBackupPath)
        {
            Write-Warning "Back up folder already exists $destinationBackupPath."
            $overwriteConfirmation = Request-Information -DefaultValue 'y' -Prompt "Please enter 'y' to overwrite the content. Else enter 'n' to create new back up folder"
            if($overwriteConfirmation -ieq 'y')
            {
                Get-ChildItem -Path $destinationBackupPath -Force -Recurse | Sort-Object -Property FullName -Descending | Remove-Item
            }
            else {
                $newDirectory = Request-Information -DefaultValue "\" -Prompt "Please enter back up folder name"
                $destinationBackupPath = "$basePath\$newDirectory\"
                New-Item -ItemType directory -Path $destinationBackupPath
            }
        }
        else {
            New-Item -ItemType directory -Path $destinationBackupPath
        }

        Copy-Item -Path "$existingApplicationPath\*" -Destination $destinationBackupPath  -Recurse
        Write-Host "Completed application files back up on $destinationBackupPath"

        $Config.ApplicationBackupPath = $destinationBackupPath
        $Config.WebApplicationName = $existingAppName
        $Config.WebApplicationPath = $existingApplicationPath
        $Config.WebSiteName = $existingWebSiteName
        $Config.WebSitePath = $existingWebSitePath

        $parameters = @{
            WebApplicationPath = $existingApplicationPath
            WebApplicationName = $existingAppName
            WebSiteName = $existingWebSiteName
        }
        UninstallAdminApi $parameters
    }
}

function GetExistingAppVersion($webSitePath,  $existingAdminApi) {
    $existingApplicationPath = ($existingAdminApi).PhysicalPath
    if(!$existingApplicationPath)
    {
        # In case of existingApplicationPath is empty, then generating application physical path by combining site physical path and application path
        $appPath = $existingAdminApi.path.trimstart('/')
        $existingApplicationPath = "$webSitePath\$appPath"
    }

    $versionString = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$existingApplicationPath\EdFi.Ods.AdminApi.dll").FileVersion

    return $existingApplicationPath, $versionString
}

function CheckForCompatibleUpdate($webSitePath,  $existingAdminApi, $targetVersionString) {
    $existingApplicationPath, $versionString = GetExistingAppVersion $webSitePath $existingAdminApi

    $targetIsNewer = IsVersionHigherThanOther $targetVersionString $versionString

    if(-not $targetIsNewer)
    {
        Write-Warning "Upgrade version $targetVersionString is the same or lower than existing installation $versionString. Downgrades are not supported. Instead,  fully uninstall the existing Admin Api and install the desired version."
        exit
    }

    return $existingApplicationPath, $versionString
}

function IsVersionHigherThanOther($versionString, $otherVersionString) {
    $version = ParseVersion($versionString)
    $otherVersion = ParseVersion($otherVersionString)

    $result = $version.CompareTo($otherVersion)
    return $result -gt 0
}

function IsVersionMismatch($versionString, $otherVersionString) {
    $version = ParseVersion($versionString)
    $otherVersion = ParseVersion($otherVersionString)

    $result = $version.CompareTo($otherVersion)
    return $result -ne 0
}

function ParseVersion($versionString) {
    $splitByTags = $versionString -split '-'
    $version = $splitByTags[0];

    for ($i = 1; $i -lt $splitByTags.Length; $i++) {
        $preVersion = $splitByTags[$i] -replace '[^0-9.]', ''
        $cleanedPreVersion = $preVersion.Trim('.')
        if($cleanedPreVersion -ne '') {
            $version += ".$cleanedPreVersion"
        }
    }

    try { return [System.Version]::Parse($version) }
    catch
    {
        Write-Warning "Failed to parse version configuration $versionString. Please correct and try again."
        exit
    }
}

function Invoke-TransferAppsettings {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {


        $backUpPath = $Config.ApplicationBackupPath
        Write-Warning "The following appsettings will be copied over from existing application: "
        $appSettings = @('DatabaseEngine', 'ApiStartupType', 'ApiExternalUrl', 'PathBase', 'Log4NetConfigFileName', 'Authority', 'IssuerUrl', 'SigningKey', 'AllowRegistration')
        foreach ($property in $appSettings) {
           Write-Host $property;
        }
        $oldSettingsFile =  Join-Path $backUpPath "appsettings.json"
        $oldSettings = Get-Content $oldSettingsFile | ConvertFrom-Json | ConvertTo-Hashtable

        $newSettingsFile = Join-Path $Config.WebConfigLocation "appsettings.json"
        $newSettings = Get-Content $newSettingsFile | ConvertFrom-Json | ConvertTo-Hashtable

        $newSettings.AppSettings.DatabaseEngine = $oldSettings.AppSettings.DatabaseEngine
        $newSettings.AppSettings.ApiStartupType = $oldSettings.AppSettings.ApiStartupType
        $newSettings.AppSettings.ApiExternalUrl =  $oldSettings.AppSettings.ApiExternalUrl
        $newSettings.AppSettings.PathBase = $oldSettings.AppSettings.PathBase
        $newSettings.Log4NetCore.Log4NetConfigFileName = $oldSettings.Log4NetCore.Log4NetConfigFileName

        $newSettings.Authentication.Authority = $oldSettings.Authentication.Authority
        $newSettings.Authentication.IssuerUrl = $oldSettings.Authentication.IssuerUrl
        $newSettings.Authentication.SigningKey = $oldSettings.Authentication.SigningKey
        $newSettings.Authentication.AllowRegistration = $oldSettings.Authentication.AllowRegistration

        $EmptyHashTable=@{}
        $mergedSettings = Merge-Hashtables $newSettings, $EmptyHashTable
        New-JsonFile $newSettingsFile $mergedSettings -Overwrite
    }
}

function Invoke-TransferConnectionStrings{
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        $backUpPath = $Config.ApplicationBackupPath
        Write-Warning "Connection strings will be copied over from existing appsettings ($backUpPath)."
        $oldSettingsFile =  Join-Path $backUpPath "appsettings.json"
        $oldSettings = Get-Content $oldSettingsFile | ConvertFrom-Json | ConvertTo-Hashtable

        $newSettingsFile = Join-Path $Config.WebConfigLocation "appsettings.json"
        $newSettings = Get-Content $newSettingsFile | ConvertFrom-Json | ConvertTo-Hashtable

        $connectionstrings = @{
            ConnectionStrings = @{
                Admin = $oldSettings.ConnectionStrings.Admin
                Security = $oldSettings.ConnectionStrings.Security
            }
        }

        $Config.AdminConnectionString = $oldSettings.ConnectionStrings.Admin
        $Config.engine = $oldSettings.AppSettings.DatabaseEngine
        $mergedSettings = Merge-Hashtables $newSettings, $connectionstrings
        New-JsonFile $newSettingsFile  $mergedSettings -Overwrite
    }
}

function Invoke-StartWebSite($webSiteName, $portNumber){

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        $webSite = Get-Website | Where-Object { $_.name -eq $webSiteName -and $_.State -eq 'Stopped'}
        if($webSite)
        {
            $Websites = Get-ChildItem IIS:\Sites
            foreach ($Site in $Websites)
            {
                if($Site.Name -ne $webSiteName -and $Site.State -eq 'Started')
                {
                    $webBinding = Get-WebBinding -Port $portNumber -Name $Site.Name -Protocol 'HTTPS'
                    if($webBinding)
                    {
                        $webSiteUsingSamePort = $true
                        break
                    }
                }
            }

            if(-not $webSiteUsingSamePort)
            {
                Write-Host "Starting $webSiteName."
                Start-IISSite -Name $webSiteName
            }
            else
            {
                Write-Warning "Can not start the website: $webSiteName. Since, the same port: $portNumber is in use."
            }
        }
    }
}

function Invoke-ResetIIS {
    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        $default = 'n'
        Write-Warning "NOTICE: In order to upgrade or uninstall, Information Internet Service (IIS) needs to be stopped during the process. This will impact availability if users are using applications hosted with IIS."
        $confirmation = Request-Information -DefaultValue 'y' -Prompt "Please enter 'y' to proceed with an IIS reset or enter 'n' to stop the upgrade or uninstall. [Default Action: '$default']"

        if (!$confirmation) { $confirmation = $default}
        if ($confirmation -ieq 'y') {
            & {iisreset}
        }
        else {
            Write-Warning "Exiting the uninstall/upgrade process."
            exit
        }
    }
}

function Initialize-Configuration {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )
    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        # Validate the input parameters. Couldn't do so in the parameter declaration
        # because the function is contained in the Configuration module imported above.
        $Config.usingSharedCredentials = $Config.ContainsKey("DbConnectionInfo") -and (-not $null -eq $Config.DbConnectionInfo)
        if ($Config.usingSharedCredentials) {
            Assert-DatabaseConnectionInfo -DbConnectionInfo $Config.DbConnectionInfo
            $Config.DbConnectionInfo.ApplicationName = "AdminApi"
            $Config.engine = $Config.DbConnectionInfo.Engine
        }
        else {
            if ($Config.IsMultiTenant) {
                foreach ($tenantKey in $Config.Tenants.Keys) {
                    Assert-DatabaseConnectionInfo -DbConnectionInfo $Config.Tenants[$tenantKey].AdminDbConnectionInfo -RequireDatabaseName
                    Assert-DatabaseConnectionInfo -DbConnectionInfo $Config.Tenants[$tenantKey].SecurityDbConnectionInfo -RequireDatabaseName
                }
            }
            else{
            Assert-DatabaseConnectionInfo -DbConnectionInfo $Config.AdminDbConnectionInfo
            Assert-DatabaseConnectionInfo -DbConnectionInfo $Config.SecurityDbConnectionInfo
            $Config.AdminDbConnectionInfo.ApplicationName = "AdminApi"
            $Config.SecurityDbConnectionInfo.ApplicationName = "AdminApi"
            $Config.engine = $Config.AdminDbConnectionInfo.Engine
            }
        }
    }
}

function Get-DbDeploy {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )
    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        $parameters = @{
            toolVersion = $DbDeployVersion
            toolsPath = $Config.ToolsPath
        }
        Install-ToolDbDeploy @parameters
    }
}

function Set-AdminApiPackageSource {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {

        $adminApiSource = $Config.PackageSource
        $configVersionString = $Config.PackageVersion
        $versionString = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$adminApiSource\EdFi.Ods.AdminApi.dll").FileVersion

        if (IsVersionMismatch $versionString $configVersionString) {
            Write-Warning "The specified Admin Api package version $configVersionString in the configuration does not match the file version $versionString of the package used as source. Please specify the correct version in the installer configuration or use the correct source."
            exit
        }

        $Config.PackageDirectory = $adminApiSource
        $Config.WebConfigLocation = $adminApiSource
    }
}

function New-JsonFile {
    param(
        [string] $FilePath,

        [hashtable] $Hashtable,

        [switch] $Overwrite
    )

    if (-not $Overwrite -and (Test-Path $FilePath)) { return }

    $Hashtable | ConvertTo-Json -Depth 10 | Out-File -FilePath $FilePath -NoNewline -Encoding UTF8
}

function Invoke-TransformAppSettings {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        $settingsFile = Join-Path $Config.WebConfigLocation "appsettings.json"
        $settings = Get-Content $settingsFile | ConvertFrom-Json | ConvertTo-Hashtable
        $settings.AppSettings.DatabaseEngine = $config.engine

        $settings.AppSettings.MultiTenancy = $config.IsMultiTenant

        $missingAuthenticationSettings = @()
        if ($Config.AuthenticationSettings.ContainsKey("Authority")) {
            $settings.Authentication.Authority = $Config.AuthenticationSettings.Authority
        } else {
            $missingAuthenticationSettings += 'Authority'
        }

        if ($Config.AuthenticationSettings.ContainsKey("IssuerUrl")) {
            $settings.Authentication.IssuerUrl = $Config.AuthenticationSettings.IssuerUrl
        } else {
            $missingAuthenticationSettings += 'IssuerUrl'
        }

        if ($Config.AuthenticationSettings.ContainsKey("SigningKey")) {
            $settings.Authentication.SigningKey = $Config.AuthenticationSettings.SigningKey
        } else {
            $missingAuthenticationSettings += 'SigningKey'
        }

        if ($Config.AuthenticationSettings.ContainsKey("AllowRegistration")) {
            $settings.Authentication.AllowRegistration = $Config.AuthenticationSettings.AllowRegistration
        } else {
            $missingAuthenticationSettings += 'AllowRegistration'
        }

        if ($missingAuthenticationSettings -gt 0) {
            Write-Warning "Please ensure all Admin Api authentication settings are configured correctly. The following Admin Api authentication settings are missing from the configuration: "
            foreach ($property in $missingAuthenticationSettings) {
               Write-Host $property;
            }
        }

        $EmptyHashTable=@{}
        $mergedSettings = Merge-Hashtables $settings, $EmptyHashTable
        New-JsonFile $settingsFile $mergedSettings -Overwrite
    }
}

function Invoke-TransformConnectionStrings {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )
    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        if ($Config.usingSharedCredentials) {
            $Config.AdminDbConnectionInfo = $Config.DbConnectionInfo.Clone()
            $Config.AdminDbConnectionInfo.DatabaseName = $Config.AdminDatabaseName

            $Config.SecurityDbConnectionInfo = $Config.DbConnectionInfo.Clone()
            $Config.SecurityDbConnectionInfo.DatabaseName = $Config.SecurityDatabaseName
        }
        else {
            # Inject default database names if not provided
            if (-not $Config.AdminDbConnectionInfo.DatabaseName) {
                $Config.AdminDbConnectionInfo.DatabaseName = "EdFi_Admin"
            }
            if (-not $Config.SecurityDbConnectionInfo.DatabaseName) {
                $Config.SecurityDbConnectionInfo.DatabaseName = "EdFi_Security"
            }
        }

        $settingsFile = Join-Path $Config.WebConfigLocation "appsettings.json"
        $settings = Get-Content $settingsFile | ConvertFrom-Json | ConvertTo-Hashtable

        Write-Host "Setting database connections in $($Config.WebConfigLocation)"
        $adminconnString = New-ConnectionString -ConnectionInfo $Config.AdminDbConnectionInfo -SspiUsername $Config.WebApplicationName
        $securityConnString = New-ConnectionString -ConnectionInfo $Config.SecurityDbConnectionInfo -SspiUsername $Config.WebApplicationName

        if ($Config.UnEncryptedConnection) {
            $adminconnString += ";Encrypt=false"
            $securityConnString += ";Encrypt=false"
        }

        $connectionstrings = @{
            ConnectionStrings = @{
                EdFi_Admin = $adminconnString
                EdFi_Security = $securityConnString
            }
        }

        $mergedSettings = Merge-Hashtables $settings, $connectionstrings
        New-JsonFile $settingsFile  $mergedSettings -Overwrite
    }
}

function Invoke-TransformMultiTenantConnectionStrings {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {
        # $webConfigPath = "$($Config.PackageDirectory)/appsettings.json"
        # $settings = Get-Content $webConfigPath | ConvertFrom-Json | ConvertTo-Hashtable

        $settingsFile = Join-Path $Config.WebConfigLocation "appsettings.json"
        $settings = Get-Content $settingsFile | ConvertFrom-Json | ConvertTo-Hashtable

        Write-Host "Setting database connections in $($Config.WebConfigLocation)"

        $newSettings = @{
            Tenants = @{}
        }

        foreach ($tenantKey in $Config.Tenants.Keys) {

            if ($Config.usingSharedCredentials) {
                $Config.Tenants[$tenantKey].AdminDbConnectionInfo = $Config.DbConnectionInfo.Clone()
                $Config.Tenants[$tenantKey].AdminDbConnectionInfo.DatabaseName = $Config.Tenants[$tenantKey].AdminDatabaseName

                $Config.Tenants[$tenantKey].SecurityDbConnectionInfo = $Config.DbConnectionInfo.Clone()
                $Config.Tenants[$tenantKey].SecurityDbConnectionInfo.DatabaseName = $Config.Tenants[$tenantKey].SecurityDatabaseName
            }

            $adminconnString = New-ConnectionString -ConnectionInfo $Config.Tenants[$tenantKey].AdminDbConnectionInfo -SspiUsername $Config.WebApplicationName
            $securityConnString = New-ConnectionString -ConnectionInfo $Config.Tenants[$tenantKey].SecurityDbConnectionInfo -SspiUsername $Config.WebApplicationName

            if ($Config.UnEncryptedConnection) {
                $adminconnString += ";Encrypt=false"
                $securityConnString += ";Encrypt=false"
            }

            $newSettings.Tenants += @{
                $tenantKey = @{
                    ConnectionStrings = @{
                        EdFi_Admin = $adminconnString
                        EdFi_Security = $securityConnString
                    }
                }
            }
        }

        $mergedSettings = Merge-Hashtables $settings, $newSettings
        New-JsonFile $settingsFile  $mergedSettings -Overwrite
    }
}

function Get-AdminInstallConnectionString {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $AdminDbConnectionInfo
    )

    $dbInstallCredentials = $Config.DatabaseInstallCredentials
    $adminDbConnectionInfo = $AdminDbConnectionInfo

    $useInstallCredentials = ($dbInstallCredentials.UseIntegratedSecurity) -or ($dbInstallCredentials.DatabaseUser -and $dbInstallCredentials.DatabasePassword -and -not $dbInstallCredentials.UseIntegratedSecurity)

    if ($useInstallCredentials){
        $adminDbConnectionInfo.UseIntegratedSecurity = $dbInstallCredentials.UseIntegratedSecurity
        $adminDbConnectionInfo.Username = $dbInstallCredentials.DatabaseUser
        $adminDbConnectionInfo.Password = $dbInstallCredentials.DatabasePassword
    }
    else
    {
        if($Config.ApplicationInstallType -ieq "Upgrade" -and $AdminDbConnectionInfo)
        {
            return $AdminDbConnectionInfo
        }
    }

    return New-ConnectionString $adminDbConnectionInfo
}

function Invoke-DbUpScripts {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {

        $engine = "PostgreSql"

        if(!(Test-IsPostgreSQL -Engine $Config.engine)){
            $engine = "SqlServer"
        }

        $params = @{
            Verb = "Deploy"
            Engine = $engine
            Database = "Admin"
            ConnectionString = ""
            FilePaths = $Config.WebApplicationPath
            ToolsPath = $Config.ToolsPath
        }

        if($Config.IsMultiTenant)
        {
            foreach ($tenantKey in $Config.Tenants.Keys) {

                $adminConnectionString = Get-AdminInstallConnectionString  $Config.Tenants[$tenantKey].AdminDbConnectionInfo
                $params["ConnectionString"] = $adminConnectionString
                Invoke-DbDeploy @params
            }
        }
        else
        {
            $adminConnectionString = Get-AdminInstallConnectionString $Config.AdminDbConnectionInfo
            $params["ConnectionString"] = $adminConnectionString
            Invoke-DbDeploy @params
        }
    }
}

function Request-Information {
  [CmdletBinding()]
  param (
      [Parameter(Mandatory=$true)]
      $Prompt,
      [Parameter(Mandatory=$true)]
      $DefaultValue
  )

  $isInteractive = [Environment]::UserInteractive
  if($isInteractive) {
      $confirmation = Read-Host -Prompt $Prompt
  } else {
      $confirmation = $DefaultValue
  }

  return $confirmation
}

function Install-Application {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {

        $iisParams = @{
            SourceLocation = $Config.PackageDirectory
            WebApplicationPath = $Config.WebApplicationPath
            WebApplicationName = $Config.WebApplicationName
            WebSitePath = $Config.WebSitePath
            WebSitePort = $Config.WebSitePort
            WebSiteName = $Config.WebSiteName
            CertThumbprint = $Config.CertThumbprint
            DotNetVersion = $RequiredDotNetHostingBundleVersion
        }

        Install-EdFiApplicationIntoIIS @iisParams
    }
}

function Set-SqlLogins {
    [CmdletBinding()]
    param (
        [hashtable]
        [Parameter(Mandatory=$true)]
        $Config
    )

    Invoke-Task -Name ($MyInvocation.MyCommand.Name) -Task {

        if($Config.usingSharedCredentials)
        {
            Add-SqlLogins $Config.DbConnectionInfo $Config.WebApplicationName -IsCustomLogin
        }
        else
        {
            if ($Config.IsMultiTenant) {
                foreach ($tenantKey in $Config.Tenants.Keys) {
                    if ($Config.UseAlternateUserName ) { Write-Host ""; Write-Host "Adding Sql Login for Admin Database:"; }
                    Add-SqlLogins $Config.Tenants[$tenantKey].AdminDbConnectionInfo $Config.WebApplicationName -IsCustomLogin:$Config.UseAlternateUserName

                    if ($Config.UseAlternateUserName ) { Write-Host ""; Write-Host "Adding Sql Login for Security Database:"; }
                    Add-SqlLogins $Config.Tenants[$tenantKey].SecurityDbConnectionInfo $Config.WebApplicationName -IsCustomLogin:$Config.UseAlternateUserName
                }
            }
            else{
            Write-Host "Adding Sql Login for Admin Database:";
            Add-SqlLogins $Config.AdminDbConnectionInfo $Config.WebApplicationName -IsCustomLogin

            Write-Host "Adding Sql Login for Security Database:";
            Add-SqlLogins $Config.SecurityDbConnectionInfo $Config.WebApplicationName -IsCustomLogin
            }
        }
    }
}

Export-ModuleMember -Function Install-EdFiOdsAdminApi, Uninstall-EdFiOdsAdminApi, Update-EdFiOdsAdminApi
