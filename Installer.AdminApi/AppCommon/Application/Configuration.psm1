#requires -version 5

$ErrorActionPreference = "Stop"
function Set-Attribute {
    [Cmdletbinding()]
    param(
        $FilePath,
        $XPath,
        $Attribute,
        $Value
    )

    [xml] $fileXml = Get-Content $FilePath
    $node = $fileXml.SelectSingleNode($XPath)
    
    if ($null -eq $node) {
        Write-Debug "could not find node @ $XPath";
        return;
    }
   
    $node.SetAttribute($Attribute, $Value)
    $fileXml.Save($FilePath)
}

function Set-AppSetting {
    [Cmdletbinding()]
    param (
        [string] $ConfigFile,
        [string] $Key,
        [string] $Value
    )

    $config = @{
        FilePath = $ConfigFile
        XPath = "/configuration/appSettings/add[@key=""$($Key)""]"
        Attribute = "value"
        Value = $Value
    }

    Set-Attribute @config

    Write-Debug "Set app setting with key ""$($Key)"" to value ""$($Value)"""
}

function Protect-ConnectionStringPassword {
    [Cmdletbinding()]
    param (
        [string] $ConnectionString
    )

    $pattern = "password=([^;]+)"
    $ConnectionString -Replace $pattern, "********"
}

function Set-ConnectionString {
    [Cmdletbinding()]
    param (
        [string] $ConfigFile,
        [string] $Name,
        [string] $ConnectionString
    )

    $config = @{
        filePath = $ConfigFile
        xpath = "/configuration/connectionStrings/add[@name=""$Name""]"
        attribute = "connectionString"
        value = $ConnectionString
    }

    Set-Attribute @config

    Write-Debug "Set connection string with name ""$($Name)"" to value ""$(Protect-ConnectionStringPassword $ConnectionString)"""
}

function Set-ApplicationSettings {
    [Cmdletbinding()]
    param (
        [string] $ConfigFile,
        [hashtable] $AppSettings
    )

    Write-Debug "Writing app settings to: $ConfigFile"

    foreach ($entry in $AppSettings.Keys) {
        Set-AppSetting $ConfigFile $entry $AppSettings.$entry
    }
}


function Invoke-ConfigTransformation {
    [Cmdletbinding()]
    param (
        $SourceFile,
        $TransformFile,
        $DestinationFile,
        $ToolsPath
    )
    $utilityDirectory = Join-Path (Split-Path -Path $PSScriptRoot -Parent) "Utility"
    Import-Module -Force -Scope Global "$utilityDirectory\ToolsHelper.psm1"
    Install-DotNetTool -Path $ToolsPath -name ConfigTransformerCore -version 2.0.0
    
    $parameters = @(
        "-s", $SourceFile
        "-t", $TransformFile
        "-d", $DestinationFile
    )
    
    # This tool prints out its own invocation information
    &"$toolsPath\ConfigTransformerCore" $parameters
}

function Assert-DatabaseConnectionInfo {
    <#
    .EXAMPLE
        PS c:\> $table = @{
            Engine = "PostgreSQL"
            Server = "myserver"
            Port = 5430
            UseIntegratedSecurity = $false
            Username = "postgres"
            Password = $null
        }
        PS c:\> Assert-DatabaseConnectionInfo -DbConnectionInfo $table
        
        In some cases, database name validation is unnecessary.

    .EXAMPLE        
        PS c:\> $table = @{
            Engine = "PostgreSQL"
            Server = "myserver"
            Port = 5430
            UseIntegratedSecurity = $false
            Username = "postgres"
            Password = $null
            DatabaseName = "EdFi_Admin"
        }
        PS c:\> Assert-DatabaseConnectionInfo -DbConnectionInfo $table - RequireDatabaseName
        
        In other cases, the database name needs to be validated as well.
    #>
    [CmdletBinding()]
    Param(
        [hashtable]
        $DbConnectionInfo,

        [switch]
        $RequireDatabaseName
    )
    $template = "Database connection info is missing key: "

    if (-not $DbConnectionInfo.ContainsKey("Engine")) {
        $DbConnectionInfo.Engine = "SqlServer"
    }
    if (-not $DbConnectionInfo.Engine.toLower -in ("sqlserver","postgresql","postgres")) {
        throw "Database connection info specifies an invalid engine: $($DbConnectionInfo.Engine). " +
              "Valid engines: SqlServer, PostgreSQL"
    }

    if (-not $DbConnectionInfo.ContainsKey("Server")) {
        throw $template + "Server"
    }
    if (-not $DbConnectionInfo.ContainsKey("UseIntegratedSecurity")) {
        if (-not $DbConnectionInfo.ContainsKey("Username")) {
            throw $template + "Username"
        }
        if ("sqlserver" -ieq $DbConnectionInfo.Engine) {
            if (-not $DbConnectionInfo.ContainsKey("Password")) {
                throw $template + "Password"
            }
        }
    }

    if ($RequireDatabaseName) {
        if (-not $DbConnectionInfo.ContainsKey("DatabaseName")) {
            throw $template + "DatabaseName"
        }
    }
}

function New-ConnectionString {
    [Cmdletbinding()]
    param(
        [hashtable]
        [Parameter(Mandatory=$true)]
        $ConnectionInfo,
        [string]
        $SspiUsername
    )

    Assert-DatabaseConnectionInfo -DbConnectionInfo $ConnectionInfo -RequireDatabaseName
    $integratedSecurityValue = "Integrated Security=true;"

    if (Test-IsPostgreSQL -Engine $ConnectionInfo.Engine) {
        $serverKey = "host"

        if (0 -eq $ConnectionInfo.Port  -Or [string]::IsNullOrEmpty($ConnectionInfo.Port)) {
            $ConnectionInfo.Port = 5432
        }

        $serverValue = "$($ConnectionInfo.Server);port=$($ConnectionInfo.Port)"

        $usernameKey = "username"  
        
        # Even with SSPI (windows auth/trusted conn/integrated security) enabled
        # npgsql still needs a username to send to postgres. It will attempt to
        # make a best guess at this username, and will pull the name from the identity
        # running the application pool, just like SqlServer scenario. However, npgsql
        # does *not* do a good job at casing, and unlike postgres which "ToLower()'s"
        # everything it touches, npgsql will try to connect with case specific username.
        # In the case of an app pool named "AdminApp", it will attempt to connect the
        # pascal cased version of that username, despite postgres toLowering the usernames.
        #
        # To avoid a connection error, we should include the specific username for npgsql.
        if($SspiUsername)  
        {
            $integratedSecurityValue += "$usernameKey=$($SspiUsername.ToLower());"
        }         
    }
    else {
        $serverKey = "server"

        if (0 -eq [int]$ConnectionInfo.Port) {
            $serverValue = $ConnectionInfo.Server
        }
        else {            
            $serverValue = "$($ConnectionInfo.Server),$($ConnectionInfo.Port)"
        }

        $usernameKey = "user id"
    }

    $connectionString = "$serverKey=$serverValue;database=$($ConnectionInfo.DatabaseName);"

    if ($ConnectionInfo.UseIntegratedSecurity) {
        $connectionString += $integratedSecurityValue
    }
    else {
        $connectionString += "$usernameKey=$($ConnectionInfo.Username);"
        if ($ConnectionInfo.Password) {
            $connectionString += "Password=$($ConnectionInfo.Password);"
        }
    }

    if ($ConnectionInfo.ContainsKey("ApplicationName")) {
        $connectionString += "Application Name=$($ConnectionInfo.ApplicationName)"
    }

    $connectionString
}

$functions = @(
    "Set-ApplicationSettings"
    "Set-ConnectionString"
    "Invoke-ConfigTransformation"
    "Assert-DatabaseConnectionInfo",
    "Protect-ConnectionStringPassword"
    "New-ConnectionString"
)

Export-ModuleMember -Function $functions