# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information. 

import-module -force "$PSScriptRoot/Install-AdminApi.psm1"

<#
Review and edit the following connection information for your database server

.EXAMPLE
Installs and connects the applications to the database using SQL Authentication

    $dbConnectionInfo = @{
        Server = "(local)"
        Engine = "SqlServer"
        UseIntegratedSecurity = $false
        Username = "exampleAdmin"
        Password = "examplePassword"
        UnEncryptedConnection = $true # Optional. Set Encrypt=false for all connection strings. Not recommended for production environment.
    }

Installs and connects the applications to the database using PostgreSQL Authentication

    $dbConnectionInfo = @{
        Server = "localhost"
        Engine = "PostgreSQL"
        UseIntegratedSecurity = $false
        Username = "postgres"
        Password = "examplePassword"
    }
#>

$dbConnectionInfo = @{
    Server = "(local)"
    Engine = "SqlServer"
    UseIntegratedSecurity=$true
}

<#
Review and edit the following application settings and connection information for Admin Api

.EXAMPLE
Configure Admin Api with V1

    $p = @{
        ToolsPath = "C:/temp/tools"
        AdminApiMode = "v1"
        DbConnectionInfo = $dbConnectionInfo
        PackageVersion = "__ADMINAPI_VERSION__"
    }


Configure Admin Api with Single tenant

    $p = @{
        ToolsPath = "C:/temp/tools"
        AdminApiMode = "v2"
        DbConnectionInfo = $dbConnectionInfo
        PackageVersion = "__ADMINAPI_VERSION__"
    }

Configure Admin Api with Multi tenant
    $p = @{
        AdminApiMode = "v2"
        IsMultiTenant = $true
        ToolsPath = "C:/temp/tools"
        DbConnectionInfo = $dbConnectionInfo
        PackageVersion = "__ADMINAPI_VERSION__"
        Tenants = @{
            Tenant1 = @{
                AdminDatabaseName = "EdFi_Admin_Tenant1"
                SecurityDatabaseName = "EdFi_Security_Tenant1"
            }
            Tenant2 = @{
                AdminDatabaseName = "EdFi_Admin_Tenant2"
                SecurityDatabaseName = "EdFi_Security_Tenant2"
            }
        }
    }
#>

# Authentication Settings
# Authentication:SigningKey must be a Base64-encoded string
# Authentication:Authority and Authentication:IssuerUrl should be the same URL as your application
# Changing Authentication:AllowRegistration to true allows unrestricted registration of new Admin API clients. This is not recommended for production.
$authenticationSettings = @{
    Authority = ""
    IssuerUrl = ""
    SigningKey = ""
    AllowRegistration = $false
}

# Encryption key for securing sensitive data. Required for AdminApiMode v2.
# Must be a valid base64-encoded 256-bit (32 byte) key.
# IMPORTANT: When using AdminApiMode v2, this key MUST match the OdsConnectionStringEncryptionKey
# used in your Ed-Fi ODS / API installation. See:
# https://docs.ed-fi.org/reference/ods-api/getting-started/binary-installation/singlemulti-tenant-installation-steps/#prepare-installation-script
$odsEncryptionKey = "" 

$packageSource = Split-Path $PSScriptRoot -Parent
$adminApiSource = "$packageSource/AdminApi"

$p = @{
    ToolsPath = "C:/temp/tools"
    AdminApiMode = "v2"
    DbConnectionInfo = $dbConnectionInfo
    PackageVersion = "__ADMINAPI_VERSION__"
    PackageSource = $adminApiSource
    AuthenticationSettings = $authenticationSettings
    StandardVersion = '5.2.0'
    EncryptionKey = $odsEncryptionKey
}

if ([string]::IsNullOrWhiteSpace($p.AuthenticationSettings.Authority) -or [string]::IsNullOrWhiteSpace($p.AuthenticationSettings.IssuerUrl) -or [string]::IsNullOrWhiteSpace($p.AuthenticationSettings.SigningKey) -or $p.AuthenticationSettings.AllowRegistration -isnot [bool]) {
    Write-Error "Authentication Settings have not been configured correctly. Edit install.ps1 to pass in valid authentication settings for Admin Api."
}
else {
    Install-EdFiOdsAdminApi @p
}
