#requires -version 5
$ErrorActionPreference = "Stop"

$utilityDirectory = Join-Path (Split-Path -Path $PSScriptRoot -Parent) "Utility"
Import-Module -Force "$utilityDirectory\ToolsHelper.psm1"
Import-Module -Scope Global -Force "$utilityDirectory\TaskHelper.psm1"

function Test-IsPostgreSQL {
    <#
    .SYNOPSIS
        Checks to see if the argument is for PostgreSQL

    .EXAMPLE
        Test-IsPostgreSQL "postgres"

        returns $True

    .EXAMPLE
        Test-IsPostgreSQL "PostgreSQL"

        returns $True

    .EXAMPLE
        Test-IsPostgreSQL "SqlServer"

        returns $False
    #>
    [Cmdletbinding()]
    param(
        # Database engine. Must be one of SqlServer, PostgreSQL, or Postgres (case insensitive)
        [string]
        [ValidateSet("PostgreSQL","Postgres", "SqlServer")]
        $Engine
    )

    $Engine.ToLower() -In ("postgresql", "postgres")
}

function Set-DatabaseConnections {
    <#
    .SYNOPSIS
        Set connction strings in a web.config file.

    .DESCRIPTION
        Sets the EdFi_Admin, EdFi_ODS, and EdFi_Security connection strings in a
        web.config file.

    .EXAMPLE
        ps c:\>$parameters=@{
            ConfigFile="d:\apps\EdFi\WebApi\web.config"
            AdminDbConnectionInfo=@{
                Engine="SqlServer"
                Server="my-sql-server.example"
                UseIntegratedSecurity=$true
            }
            OdsDbConnectionInfo=@{
                Engine="SqlServer"
                Server="my-sql-server.example"
                UseIntegratedSecurity=$true
            }
            SecurityDbConnectionInfo=@{
                Engine="SqlServer"
                Server="my-sql-server.example"
                UseIntegratedSecurity=$true
            }
        }

        Using the same server for all three connections, with default database names.

    .EXAMPLE
        ps c:\>$parameters=@{
            ConfigFile="d:\apps\EdFi\WebApi\web.config"
            AdminDbConnectionInfo=@{
                DatabaseName="EdFi_3x_Admin"
                Engine="PostgreSQL"
                Port=5001
                Server="ed-fi-auth.my-pg-server.example"
                Username="ods-admin"
            }
            OdsDbConnectionInfo=@{
                DatabaseName="EdFi_3x_ODS_{0}"
                Engine="PostgreSQL"
                Server="ed-fi-ods.my-pg-server.example"
                UseIntegratedSecurity=$true
                Username="ods-admin"
            }
            SecurityDbConnectionInfo=@{
                DatabaseName="EdFi_3x_Security"
                Engine="PostgreSQL"
                Port=5001
                Server="ed-fi-auth.my-pg-server.example"
                UseIntegratedSecurity=$true
                Username="ods-admin"
            }
        }

        Install on PostgreSQL with username, alternate database names, and different
        servers for Admin/Security and ODS databases. Password is assumed to be
        set via $env:PGPASSWORD or pgconf file. ODS database name assumes a Year
        Specific, Sandbox, or District Specific installation. Uses an alternate port
        instead of the default 5432 on of the servers.
    #>
    [CmdletBinding()]
    param (
        # Full path to a web or app.config file.
        [string]
        [Parameter(Mandatory=$true)]
        $ConfigFile,

        # Hashtable containing database connectivity information for the Admin database.
        # Must include: Engine, Server, and either UseIntegratedSecurity or Username.
        # Optionally include DatabaseName, Port.
        [hashtable]
        [Parameter(Mandatory=$true)]
        $AdminDbConnectionInfo,

        # Hashtable containing database connectivity information for the ODS database.
        # Must include: Engine, Server, and either UseIntegratedSecurity or Username.
        # Optionally include DatabaseName, Port.
        [hashtable]
        [Parameter(Mandatory=$true)]
        $OdsDbConnectionInfo,

        # Hashtable containing database connectivity information for the Security database.
        # Must include: Engine, Server, and either UseIntegratedSecurity or Username.
        # Optionally include DatabaseName, Port.
        [hashtable]
        [Parameter(Mandatory=$true)]
        $SecurityDbConnectionInfo,

        # String value of the Admin database connection string name in the config file
        [string]
        $AdminConnectionName = "EdFi_Admin",

        # String value of the Ods database connection string name in the config file
        [string]
        $OdsConnectionName = "EdFi_Ods",

        # String value of the Security database connection string name in the config file
        [string]
        $SecurityConnectionName = "EdFi_Security",

        # sspi user name is used for setting the username value, along with integrated security on Postgresql connection string
        [string]
        $SspiUsername
    )

    Assert-DatabaseConnectionInfo $AdminDbConnectionInfo -RequireDatabaseName
    Assert-DatabaseConnectionInfo $OdsDbConnectionInfo -RequireDatabaseName
    Assert-DatabaseConnectionInfo $SecurityDbConnectionInfo -RequireDatabaseName

    Write-Debug "Writing connection strings to: $ConfigFile"

    $connString = New-ConnectionString -ConnectionInfo $AdminDbConnectionInfo -SspiUsername $SspiUsername
    Set-ConnectionString -ConfigFile $ConfigFile -Name $AdminConnectionName -ConnectionString $connString

    $connString = New-ConnectionString -ConnectionInfo $OdsDbConnectionInfo -SspiUsername $SspiUsername
    Set-ConnectionString -ConfigFile $ConfigFile -Name $OdsConnectionName -ConnectionString $connString

    $connString = New-ConnectionString -ConnectionInfo $SecurityDbConnectionInfo -SspiUsername $SspiUsername
    Set-ConnectionString -ConfigFile $ConfigFile -Name $SecurityConnectionName -ConnectionString $connString
}

function New-EdFiWebsite {
    [CmdletBinding()]
    param(
        [hashtable]
        $Configuration
    )

    $websiteParams = @{
        SiteName = $Configuration.WebSiteName
        Port = $Configuration.WebSitePort
        WebsitePath = $Configuration.WebSitePath
        CertThumbprint = $Configuration.CertThumbprint
    }

    $createdWebsite = New-IISWebsite @websiteParams

    if ($createdWebsite) {
        New-SafeDirectory $websiteParams.websitePath
        Grant-FolderAccessToIISUsers $websiteParams.websitePath
    }
}

function Invoke-PrepareOperatingSystem {
    param([hashtable]$Configuration)

    Invoke-Task -name ($MyInvocation.MyCommand.Name) -task {
        Import-Module -Force "$PSScriptRoot\..\Environment\Prerequisites.psm1"

        Invoke-ThrowIfDotnetHostingBundleMissing -VersionString $configuration.DotNetVersion

        Write-Info "Ensure all IIS modules are installed"
        Initialize-IISWithPrerequisites
    }
}

function Invoke-InstallIntoFileSystem {
    [CmdletBinding()]
    param(
        [hashtable]
        $Configuration
    )

    Invoke-Task -name ($MyInvocation.MyCommand.Name) -task {
        Import-Module -Force "$PSScriptRoot\..\IIS\IIS-Components.psm1"
        Import-Module -Force "$PSScriptRoot\..\Environment\FolderAdmin.psm1"

        Write-Info "Moving source files into installation directory..."

        $parameters = @{
            sourceLocation = "$($Configuration.SourceLocation)\*"
            InstallLocation = $Configuration.WebApplicationPath
        }
        Copy-ArchiveOrDirectory @parameters

        Grant-FolderAccessToIISUsers $Configuration.WebApplicationPath

        Write-Info "Installing IIS Components for $($Configuration.WebApplicationName)..."
    }
}

function Invoke-ConfigureIIS {
    [CmdletBinding()]
    param(
        [hashtable]
        $Configuration
    )

    Invoke-Task -name ($MyInvocation.MyCommand.Name) -task {
        Open-IISManager

        New-EdFiWebsite $Configuration

        $appPoolName = New-IISApplicationPool $Configuration.WebApplicationName

        $applicationParams = @{
            WebsiteName = $Configuration.WebSiteName
            WebApplicationName = $Configuration.WebApplicationName
            WebApplicationPath = $Configuration.WebApplicationPath
            AppPoolName = $appPoolName
        }
        New-IISWebApplication @applicationParams

        $authParameters = @{
            WebSiteName = $Configuration.WebSiteName
            WebApplicationName = $Configuration.WebApplicationName
            EnableAnonymousAuth = $Configuration.EnableAnonymousAuth
            EnableWindowsAuth = $Configuration.EnableWindowsAuth
        }
        Set-AuthenticationSettings @authParameters

        Close-IISManager

        Write-Host "IIS installation succeeded for $($Configuration.WebApplicationName)" -ForegroundColor Green
    }
}

function Install-EdFiApplicationIntoIIS {
    <#
    .SYNOPSIS
        Installs an Ed-Fi application into IIS.

    .DESCRIPTION
        Creates a new "Ed-Fi" website in IIS if required, then copies files from
        a source location to a destination location and configures a that destination
        as a new Application under the Ed-Fi website. The new application will have
        a dedicated app pool.

        Implicitly installs IIS and all required features if they are missing from
        the operating system.

    .EXAMPLE
        $parameters = @{
            sourceLocation = "c:\temp\EdFi.Ods.WebApi.EFA.3.4.0-b12345"
            WebApplicationPath = "c:\inetpub\OdsRoot\WebApi"
            webApplicationName = "WebApi"
        }
        Install-EdFiApplicationIntoIIS @parameters
    #>
    [CmdletBinding()]
    param (
        # Source file system path for the web application files.
        [string]
        [Parameter(Mandatory=$true)]
        $SourceLocation,

        # Destination directory into which the source files are copied.
        [string]
        [Parameter(Mandatory=$true)]
        $WebApplicationPath,

        [string]
        [Parameter(Mandatory=$true)]
        $WebApplicationName,

        # Web site folder path. Default: "c:\inetpub\Ed-Fi".
        [string]
        $WebSitePath = "c:\inetpub\Ed-Fi",

        # TCP port number. Default: 443.
        [int]
        $WebSitePort = 443,

        # IIS web site name. Default: "Ed-Fi".
        [string]
        $WebSiteName = "Ed-Fi",

        # Thumbprint of a TLS security certificate, which will be used to setup
        # the HTTPS binding in IIS. Optional. If not supplied, a self-signed
        # certificate will be created.
        [string]
        $CertThumbprint,

        # When true, allow anonymous authentication
        [switch]
        $EnableAnonymousAuth,

        # When true, allow Windows authentication
        [switch]
        $EnableWindowsAuth,

        # Optionally disbable reporting of execution time.
        [switch]
        $NoDuration,

        # Optionally reqiure specific dotnet version.
        [string]
        $DotNetVersion = "6.0.0"
    )
    $configuration = @{
        SourceLocation = $SourceLocation
        WebApplicationPath = $WebApplicationPath
        WebSiteName = $WebSiteName
        WebApplicationName = $WebApplicationName
        CertThumbprint = $CertThumbprint
        WebSitePath = $WebSitePath
        WebSitePort = $WebSitePort
        EnableAnonymousAuth = $EnableAnonymousAuth
        EnableWindowsAuth = $EnableWindowsAuth
        DotNetVersion = $DotNetVersion
    }

    Write-InvocationInfo $MyInvocation

    Clear-Error

    $result = @()

    $elapsed = Use-StopWatch {
        $result += Invoke-PrepareOperatingSystem -Configuration $configuration
        $result += Invoke-InstallIntoFileSystem -Configuration $configuration
        $result += Invoke-ConfigureIIS -Configuration $configuration
    }

    Test-Error

    if (-not $NoDuration) {
        $result += New-TaskResult -name '-' -duration '-'
        $result += New-TaskResult -name $MyInvocation.MyCommand.Name -duration $elapsed.format
        return $result | Format-Table
    }

    return $result
}

function Set-AuthenticationSettings {
    [CmdletBinding()]
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $WebSiteName,

        [string]
        [Parameter(Mandatory=$true)]
        $WebApplicationName,

        [switch]
        $EnableAnonymousAuth,

        [switch]
        $EnableWindowsAuth
    )

    $iisAppName = "$WebSiteName/$WebApplicationName"

    Write-Host "Setting Authentication Settings for $($iisAppName)" -ForegroundColor Green

    if(!$EnableAnonymousAuth -and !$EnableWindowsAuth) {
        $EnableAnonymousAuth = $true
    }

    $manager = Get-IISServerManager
    $config = $manager.GetApplicationHostConfiguration()
    $section = $config.GetSection("system.webServer/security/authentication/anonymousAuthentication", $iisAppName)
    $section.Attributes["enabled"].Value = $EnableAnonymousAuth.IsPresent
    $section = $config.GetSection("system.webServer/security/authentication/windowsAuthentication", $iisAppName)
    $section.Attributes["enabled"].Value = $EnableWindowsAuth.IsPresent
    $windowsAuthString = if ($EnableWindowsAuth) { "Enabled" } else { "Disabled" }
    $anonAuthString = if ($EnableAnonymousAuth) { "Enabled" } else { "Disabled" }
    Write-Host $anonAuthString Anonymous Authentication
    Write-Host $windowsAuthString Windows Authentication
}

function Test-DbLoginExistsWithSspi {
    [CmdletBinding()]
    Param(
        [string]
        [Parameter(Mandatory=$true)]
        $serverName,
        [string]
        [Parameter(Mandatory=$true)]
        $userName,
        [switch]
        $isPostgres
    )
    $loginExists = $False
    if($isPostgres){
        $queryResult = &psql -U postgres -c "SELECT 1 FROM pg_user WHERE usename = '$userName'" | Select-String -Pattern '1'
        $loginExists = $queryResult -ne "" -and $queryResult -ne [String]::Empty -and $null -ne $queryResult
    } else {
        Import-Module SqlServer
        $server = New-Object ('Microsoft.SqlServer.Management.Smo.Server') $serverName
        $loginExists = ($server.logins).Name -contains $userName
    }
    return $loginExists
}

function Test-DbLoginExistsWithBasicAuth {
    Param(
        $dbServer,
        $userName
    )
}

function Request-Information ($prompt, $defaultValue) {
  $isInteractive = [Environment]::UserInteractive
  if($isInteractive) {
      $confirmation = Read-Host -Prompt $prompt
  } else {
      $confirmation = $defaultValue
  }

  return $confirmation
}

function Prompt-YN-Retry-Loop ($prompt, $default){
  if(-not ([Environment]::UserInteractive)) {
    return $default
  }

  $result = Read-Host -Prompt $prompt

  if($result -eq '') {
    return $default
  }
  if($result -ne 'y' -and $result -ne 'n') {
    return Prompt-YN-Retry-Loop $prompt $default
  }

  return $result
}

function Prompt-For-SQLServer-Username ($sqlServerUsername) {
  $sqlServerUsername = Request-Information -DefaultValue $sqlServerUsername -Prompt "Please enter a custom username for the SQL Login. You can use the username for an existing user login or a new user login will be created if it does not already exist. Please enter the username"
  return $sqlServerUsername
}

function Prompt-For-PostgreSQL-Username ($postgresUsername) {
  $postgresUsername = Request-Information -DefaultValue $postgresUsername -Prompt "Please enter a custom username for the PostgreSQL Login. You can use the username for an existing user login or a new user login will be created if it does not already exist. Please enter the username"
  $identityMapMessage = "Created user ""$postgresUsername"" in PostgreSQL. Identity map for ""$postgresUsername"" should be manually created."
  $postgresPromptInfo = @{
      Username = $postgresUsername
      IdentityMapMessage = $identityMapMessage
  }
  return $postgresPromptInfo
}

function Add-SqlServerLogin ($databaseServer, $sqlServerUsername) {
    $sqlLoginCreated = $False
    try {
        if(!(Test-DbLoginExistsWithSspi $databaseServer $sqlServerUsername)) {
            Add-SqlLogin -ServerInstance $databaseServer -LoginName $sqlServerUsername -LoginType "WindowsUser" -Enable -GrantConnectSql
            $server = New-Object ('Microsoft.SqlServer.Management.Smo.Server') $databaseServer
            $serverRole = $server.Roles | Where-Object {$_.Name -eq 'sysadmin'}
            $serverRole.AddMember($sqlServerUsername)
            Write-Host "SQL Login, $sqlServerUsername, created in $databaseServer" -ForegroundColor Green
        } else {
            Write-Host "SQL Login, $sqlServerUsername, already exists in $databaseServer"
        }
        $sqlLoginCreated = $True
    } catch {
        Write-Host "Failed to add a SQL Login: " -NoNewLine -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
    return $sqlLoginCreated
}

function Add-PostgreSqlLogin ($databaseServer, $postgresUsername, $identityMapMessage) {
    $sqlLoginCreated = $False
    try {
        if(!(Test-DbLoginExistsWithSspi $databaseServer $postgresUsername -isPostgres)){
            &psql -d postgres -c "CREATE USER $postgresUsername LOGIN SUPERUSER INHERIT CREATEDB CREATEROLE;"  | Out-Host
            Write-Host $identityMapMessage -ForegroundColor Green
        } else {
            Write-Host "PostgreSQL Login, $postgresUsername, already exists in $databaseServer"
        }
        $sqlLoginCreated = $True
    } catch {
        Write-Host "Failed to add a PostrgeSQL Login: " -NoNewLine -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
    return $sqlLoginCreated
}

function Get-DefaultPostgresPromptInfo ($userToCreate) {
    $postgresUsername = $userToCreate.ToLower()
    $identityMapMessage = "Created user ""$postgresUsername"" in PostgreSQL. Identity map for ""$userToCreate@IIS APPPOOL"" to ""$postgresUsername"" should be manually created."
    $postgresPromptInfo = @{
        Username = $postgresUsername
        IdentityMapMessage = $identityMapMessage
    }
    return $postgresPromptInfo
}

function Add-SqlLogins {
    [CmdletBinding()]
    Param(
        [hashtable]
        [Parameter(Mandatory=$true)]
        $DbConnectionInfo,
        [string]
        [Parameter(Mandatory=$true)]
        $UserToCreate,
        [switch]
        $IsCustomLogin
    )

    $databaseServer = $DbConnectionInfo.Server

    if($DbConnectionInfo.UseIntegratedSecurity){
        if(Test-IsPostgreSQL $DbConnectionInfo.Engine){
            # Psql will end up lowercasing our username regardless.
            # The call to ToLower ensures the username we use here is the same one we see in the database
            $postgresPromptInfo = Get-DefaultPostgresPromptInfo $UserToCreate
            $postgresUsername = $postgresPromptInfo.Username
            $identityMapMessage = $postgresPromptInfo.IdentityMapMessage
            if($IsCustomLogin){
                $customUsernameConfirmation = Prompt-YN-Retry-Loop -Default 'y' -Prompt "Continue with ""$postgresUsername"" for PostgreSQL Login? Press 'n' to customize [Y/n]"
                if($customUsernameConfirmation -ieq 'n')
                {
                    $postgresPromptInfo = Prompt-For-PostgreSQL-Username $postgresUsername
                    $postgresUsername = $postgresPromptInfo.Username
                    $identityMapMessage = $postgresPromptInfo.IdentityMapMessage
                }
                $sqlLoginCreated = Add-PostgreSqlLogin $databaseServer $postgresUsername $identityMapMessage
                while(!$sqlLoginCreated) {
                    $retry = Prompt-YN-Retry-Loop 'Press y to re-enter the username and try again. Or press n to use the default username [y/N]?' 'n'
                    if ($retry -eq "y") {
                        $postgresPromptInfo = Prompt-For-PostgreSQL-Username $postgresUsername
                        $postgresUsername = $postgresPromptInfo.Username
                        $identityMapMessage = $postgresPromptInfo.IdentityMapMessage
                    } else {
                        $postgresPromptInfo = Get-DefaultPostgresPromptInfo $UserToCreate
                        $postgresUsername = $postgresPromptInfo.Username
                        $identityMapMessage = $postgresPromptInfo.IdentityMapMessage
                    }
                    $sqlLoginCreated = Add-PostgreSqlLogin $databaseServer $postgresUsername $identityMapMessage
                }
            } else {
                Add-PostgreSqlLogin $databaseServer $postgresUsername $identityMapMessage
            }
        } else {
            If(-not(Get-Module -ListAvailable -Name SqlServer -ErrorAction silentlycontinue)){
                Install-Module SqlServer -Confirm:$False -Force -AllowClobber
            }
            $sqlServerUsername = "IIS APPPOOL\$UserToCreate"
            if($IsCustomLogin){
                $customUsernameConfirmation = Prompt-YN-Retry-Loop -DefaultValue 'y' -Prompt "Continue with ""$sqlServerUsername"" for SQL Login? Press 'n' to customize [Y/n]"
                if($customUsernameConfirmation -ieq 'n')
                {
                    $sqlServerUsername = Prompt-For-SQLServer-Username $sqlServerUsername
                }
                $sqlLoginCreated = Add-SqlServerLogin $databaseServer $sqlServerUsername
                while(!$sqlLoginCreated) {
                    $retry = Prompt-YN-Retry-Loop 'Press y to re-enter the username and try again. Or press n to use the default username [y/N]?' 'n'
                    if ($retry -eq "y") {
                        $sqlServerUsername = Prompt-For-SQLServer-Username $sqlServerUsername
                    } else {
                        $sqlServerUsername = "IIS APPPOOL\$UserToCreate"
                    }
                    $sqlLoginCreated = Add-SqlServerLogin $databaseServer $sqlServerUsername
                }
            } else {
                Add-SqlServerLogin $databaseServer $sqlServerUsername
            }
        }
    } else {
        Write-Warning "Cannot automatically create application logins to the database. Operation currently not supported"
        return;
    }
}

$functions = @(
    "New-ConnectionString"
    "Test-IsPostgreSQL"
    "Set-DatabaseConnections",
    "Install-EdFiApplicationIntoIIS"
    "Add-SqlLogins"
)

Export-ModuleMember -Function $functions
