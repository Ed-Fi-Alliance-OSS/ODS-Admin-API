# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

<#
.SYNOPSIS
    Runs Bruno E2E tests locally for Admin API v1, v2, or v3.

.DESCRIPTION
    Builds Docker containers, waits for the API to be healthy, obtains an auth
    token, writes the Bruno environment file, and runs the test suite — exactly
    as the CI workflows do.

    Requires PowerShell 7+ (uses -SkipCertificateCheck on web requests).

    NOTE: If using -UseGlobalBru and you see errors related to 'ajv' or missing
    modules, run 'npm install' inside the Bruno collection folder first, e.g.:
        cd Application\EdFi.Ods.AdminApi.V3\E2E Tests\Bruno Admin API E2E 3.0
        npm install

.PARAMETER ApiVersion
    1  — runs V1 tests (single tenant only, pgsql or mssql)
    2  — runs V2 tests
    3  — runs V3 tests (default)

.PARAMETER TenantMode
    singletenant  — uses the single-tenant compose file (default)
    multitenant   — uses the multi-tenant compose file (V2 and V3 only)

.PARAMETER DbEngine
    pgsql  — PostgreSQL (default)
    mssql  — SQL Server

.PARAMETER SkipDockerBuild
    When set, skips docker compose up (assumes containers are already running).

.PARAMETER TearDown
    When set, runs docker compose down after the tests finish.

.PARAMETER UseGlobalBru
    When set, invokes the globally installed 'bru' CLI instead of npx.
    Useful if npx has local cache issues. Requires: npm install -g @usebruno/cli

.PARAMETER BrunoFilter
    One or more sub-paths inside the Bruno collection to run.
    V1 default: "v1/Vendors","v1/OdsInstances","v1/ClaimSets","v1/Application"
    V2 default: "v2/"
    V3 default: "v3/"

.EXAMPLE
    # Run V3 multi-tenant pgsql (same as CI)
    .\eng\run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode multitenant

.EXAMPLE
    # Run V1 mssql, tear down when done
    .\eng\run-bruno-e2e.ps1 -ApiVersion 1 -DbEngine mssql -TearDown

.EXAMPLE
    # Run V2 single-tenant pgsql, only Jobs folder
    .\eng\run-bruno-e2e.ps1 -ApiVersion 2 -BrunoFilter "v2/Jobs"

.EXAMPLE
    # Run V3 using globally installed bru CLI
    .\eng\run-bruno-e2e.ps1 -ApiVersion 3 -UseGlobalBru
#>

param(
    [ValidateSet("1", "2", "3")]
    [string]$ApiVersion = "3",

    [ValidateSet("singletenant", "multitenant")]
    [string]$TenantMode = "singletenant",

    [ValidateSet("pgsql", "mssql")]
    [string]$DbEngine = "pgsql",

    [switch]$SkipDockerBuild,
    [switch]$TearDown,
    [switch]$UseGlobalBru,
    [string[]]$BrunoFilter = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Validate parameter combinations
# ---------------------------------------------------------------------------
if ($ApiVersion -eq "1" -and $TenantMode -eq "multitenant") {
    Write-Host "❌ V1 does not support multitenant mode." -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------
# Resolve version-specific paths and configuration
# ---------------------------------------------------------------------------
$repoRoot  = (Resolve-Path "$PSScriptRoot\..").Path
$dockerDir = Join-Path $repoRoot "Docker"

$brunoRecursive  = $true
$composeFile     = $null
$envFile         = $null
$brunoDir        = $null
$projectsToCopy  = @()

switch ($ApiVersion) {
    "1" {
        $appDir      = Join-Path $repoRoot "Application\EdFi.Ods.AdminApi"
        $brunoDir    = Join-Path $appDir "E2E Tests\V1\Bruno Admin API E2E refactor"
        $ghSetup     = Join-Path $appDir "E2E Tests\V1\gh-action-setup"
        $envFile     = Join-Path $ghSetup ".automation.env"
        $composeFile = Join-Path $dockerDir "V1\Compose\$DbEngine\compose-build-dev.yml"
        $projectsToCopy = @("EdFi.Ods.AdminApi")
        if ($BrunoFilter.Count -eq 0) {
            $BrunoFilter = @("v1/Vendors", "v1/OdsInstances", "v1/ClaimSets", "v1/Application")
        }
        $brunoRecursive = $false
    }
    "2" {
        $appDir       = Join-Path $repoRoot "Application\EdFi.Ods.AdminApi"
        $brunoDir     = Join-Path $appDir "E2E Tests\V2\Bruno Admin API E2E 2.0 refactor"
        $ghSetup      = Join-Path $appDir "E2E Tests\V2\gh-action-setup"
        $envFile      = Join-Path $ghSetup ".automation_$DbEngine.env"
        $tenantFolder = if ($TenantMode -eq "multitenant") { "MultiTenant" } else { "SingleTenant" }
        $composeName  = if ($TenantMode -eq "multitenant") { "compose-build-dev-multi-tenant.yml" } else { "compose-build-dev.yml" }
        $composeFile  = Join-Path $dockerDir "V2\Compose\$DbEngine\$tenantFolder\$composeName"
        $projectsToCopy = @(
            "EdFi.Ods.AdminApi",
            "EdFi.Ods.AdminApi.Common",
            "EdFi.Ods.AdminApi.InstanceManagement",
            "EdFi.Ods.AdminApi.V1"
        )
        if ($BrunoFilter.Count -eq 0) { $BrunoFilter = @("v2/") }
    }
    "3" {
        $appDir       = Join-Path $repoRoot "Application\EdFi.Ods.AdminApi.V3"
        $brunoDir     = Join-Path $appDir "E2E Tests\Bruno Admin API E2E 3.0"
        $ghSetup      = Join-Path $appDir "E2E Tests\gh-action-setup"
        $envFile      = Join-Path $ghSetup ".automation_$DbEngine.env"
        $tenantFolder = if ($TenantMode -eq "multitenant") { "MultiTenant" } else { "SingleTenant" }
        $composeName  = if ($TenantMode -eq "multitenant") { "compose-build-dev-multi-tenant.yml" } else { "compose-build-dev.yml" }
        $composeFile  = Join-Path $dockerDir "V3\Compose\$DbEngine\$tenantFolder\$composeName"
        $projectsToCopy = @(
            "EdFi.Ods.AdminApi",
            "EdFi.Ods.AdminApi.Common",
            "EdFi.Ods.AdminApi.InstanceManagement",
            "EdFi.Ods.AdminApi.V1",
            "EdFi.Ods.AdminApi.V3"
        )
        if ($BrunoFilter.Count -eq 0) { $BrunoFilter = @("v3/") }
    }
}

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  V$ApiVersion Bruno E2E — $TenantMode / $DbEngine" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Compose : $composeFile"
Write-Host "  Env     : $envFile"
Write-Host "  Bruno   : $brunoDir"
Write-Host ""

# ---------------------------------------------------------------------------
# Helper: fail fast
# ---------------------------------------------------------------------------
function Assert-ExitCode([string]$step) {
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ $step failed (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# ---------------------------------------------------------------------------
# 1. Prepare Docker context
# ---------------------------------------------------------------------------
if (-not $SkipDockerBuild) {
    Write-Host "📦 Preparing Docker build context..." -ForegroundColor Yellow

    $dockerAppDest = Join-Path $dockerDir "Application"
    if (Test-Path $dockerAppDest) {
        Remove-Item $dockerAppDest -Recurse -Force
    }
    New-Item -ItemType Directory -Path $dockerAppDest | Out-Null

    foreach ($proj in $projectsToCopy) {
        $src = Join-Path $repoRoot "Application\$proj"
        Write-Host "  Copying $proj..."
        Copy-Item -Path $src -Destination $dockerAppDest -Recurse -Force
    }
    Copy-Item -Path (Join-Path $repoRoot "Application\NuGet.Config") `
              -Destination $dockerAppDest -Force

    # SSL certs
    $sslSrc  = Join-Path $repoRoot "eng\test-certs\ssl"
    $sslDest = Join-Path $dockerDir "Settings"
    if (Test-Path $sslSrc) {
        Write-Host "  Copying SSL certs..."
        Copy-Item -Path $sslSrc -Destination $sslDest -Recurse -Force
    } else {
        Write-Host "  ⚠️  SSL source not found at $sslSrc — skipping cert copy" -ForegroundColor Yellow
    }

    # ---------------------------------------------------------------------------
    # 2. Docker Compose up
    # ---------------------------------------------------------------------------
    Write-Host ""
    Write-Host "🐳 Starting containers..." -ForegroundColor Yellow
    docker compose -f $composeFile --env-file $envFile up -d --build
    Assert-ExitCode "docker compose up"
}

# ---------------------------------------------------------------------------
# 3. Wait for adminapi container to be healthy
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "⏳ Waiting for 'adminapi' container to become healthy..." -ForegroundColor Yellow

$timeout  = 300  # 5 minutes
$elapsed  = 0
$interval = 5

while ($elapsed -lt $timeout) {
    $status = docker inspect -f "{{.State.Health.Status}}" adminapi 2>$null
    if ($status -eq "healthy") { break }
    Write-Host "  Status: $status — waiting..."
    Start-Sleep -Seconds $interval
    $elapsed += $interval
}

$finalStatus = docker inspect -f "{{.State.Health.Status}}" adminapi 2>$null
if ($finalStatus -ne "healthy") {
    Write-Host "❌ Container did not become healthy within ${timeout}s" -ForegroundColor Red
    docker ps
    docker logs adminapi --tail 50
    exit 1
}
Write-Host "✅ Container is healthy" -ForegroundColor Green

# Verify HTTP health endpoint
Write-Host "🌐 Verifying Admin API health endpoint..."
$healthUrl = "https://localhost/adminapi/health"
try {
    $resp = Invoke-WebRequest -Uri $healthUrl -SkipCertificateCheck -UseBasicParsing -TimeoutSec 10
    if ($resp.StatusCode -ne 200) { throw "Status $($resp.StatusCode)" }
    Write-Host "✅ Admin API is responding (HTTP 200)" -ForegroundColor Green
} catch {
    Write-Host "❌ Health check failed: $_" -ForegroundColor Red
    exit 2
}

# ---------------------------------------------------------------------------
# 4. Generate client credentials & token
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "🔑 Generating auth token..." -ForegroundColor Yellow

$apiUrl       = "https://localhost/adminapi"
$clientId     = [System.Guid]::NewGuid().ToString().ToLower()
$chars        = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#`$%^&*()_+{}:<>?|[],./".ToCharArray()
$clientSecret = "aA1!" + (-join ((5..64) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] }))

Write-Host "  Client ID: $clientId"
Write-Host "  Client Secret: $($clientSecret.Substring(0, 20))... (length $($clientSecret.Length))"

$isMultitenant   = $TenantMode -eq "multitenant"
$registerHeaders = @{ "Content-Type" = "application/x-www-form-urlencoded" }
if ($isMultitenant) { $registerHeaders["Tenant"] = "tenant1" }

$registerBody = "ClientId=$([uri]::EscapeDataString($clientId))&ClientSecret=$([uri]::EscapeDataString($clientSecret))&DisplayName=$([uri]::EscapeDataString($clientId))"

Write-Host "  Registering client..."
$regResp = Invoke-RestMethod -Uri "$apiUrl/connect/register" `
    -Method POST -Headers $registerHeaders -Body $registerBody `
    -SkipCertificateCheck -ContentType "application/x-www-form-urlencoded"
Write-Host "  Register response: $($regResp | ConvertTo-Json -Compress)"

$tokenHeaders = @{ "Content-Type" = "application/x-www-form-urlencoded" }
if ($isMultitenant) { $tokenHeaders["Tenant"] = "tenant1" }

$tokenBody = "client_id=$([uri]::EscapeDataString($clientId))&client_secret=$([uri]::EscapeDataString($clientSecret))&grant_type=client_credentials&scope=edfi_admin_api%2Ffull_access"

Write-Host "  Requesting token..."
$tokenResp = Invoke-RestMethod -Uri "$apiUrl/connect/token" `
    -Method POST -Headers $tokenHeaders -Body $tokenBody `
    -SkipCertificateCheck -ContentType "application/x-www-form-urlencoded"

$token = $tokenResp.access_token
if (-not $token) {
    Write-Host "❌ Failed to obtain token" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Token obtained (length: $($token.Length))" -ForegroundColor Green

# ---------------------------------------------------------------------------
# 5. Write Bruno environment file
# ---------------------------------------------------------------------------
$envFilePath = Join-Path $brunoDir "environments\local.bru"

if ($ApiVersion -eq "1") {
    $bruEnvContent = @"
vars {
  API_URL: https://localhost/adminapi
  TOKEN: $token
  CLIENT_ID: $clientId
  CLIENT_SECRET: $clientSecret
  VENDORSCOUNT: 10
  APPLICATIONCOUNT: 10
  CLAIMSETCOUNT: 10
  ODSINSTANCESCOUNT: 10
}
"@
} else {
    $connectionString = if ($DbEngine -eq "pgsql") {
        "host=test;port=90;username=test;password=test;database=EdFi_Admin;pooling=false"
    } else {
        "Data Source=.;Initial Catalog=EdFi_Admin;Integrated Security=True;TrustServerCertificate=True"
    }

    $bruEnvContent = @"
vars {
  API_URL: https://localhost/adminapi
  TOKEN: $token
  CLIENT_ID: $clientId
  CLIENT_SECRET: $clientSecret
  limit: 100
  offset: 0
  tenant1: tenant1
  tenant2: tenant2
  TOKEN_TENANT2:
  CreatedOdsInstanceId:
  connectionString: $connectionString
  isMultitenant: $($isMultitenant.ToString().ToLower())
  NotExistOdsInstancesContextId: 786
  NotExistOdsInstancesDerivativeId: 90
  RESOURCENAMEFILTER: candidate
  FILTERAPPLICATIONNAME:
  FILTERCLAIMSETNAME:
  APPLICATIONCOUNT: 5
  VENDORTODELETE:
  CLAIMSETSTODELETE:
  ODSINSTANCETODELETE:
  FILTERCLAIMSETSNAME:
  CLAIMSETCOUNT: 2
  CLAIMSETSTODELETE:
  FILTERNAMEODS:
  FILTERINSTANCETYPE:
  ODSINSTANCECOUNT: 5
  ODSINSTANCESTODELETE:
  FILTEPROFILENAME: 0
  PROFILECOUNT: 4
  PROFILESTODELETE:
  FILTERCOMPANY:
  FILTERCONTACTNAME:
  FILTERNAMESPACEPREFIXES:
  FILTERCONTACTEMAILADDRESS:
  VENDORSCOUNT: 4
  VENDORSTODELETE:
}
"@
}

# Write without BOM — Bruno's v2 parser fails if the file starts with a UTF-8 BOM
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($envFilePath, $bruEnvContent, $utf8NoBom)
Write-Host "📁 Bruno environment written to: $envFilePath"

# ---------------------------------------------------------------------------
# 6. Run Bruno tests
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "🧪 Running Bruno tests ($($BrunoFilter -join ', '))..." -ForegroundColor Yellow

$env:NODE_TLS_REJECT_UNAUTHORIZED = "0"
$brunoExitCode = 0

$commonArgs = @("run", "--env", "local", "--sandbox=developer", "--insecure")
$reportArgs = @("--reporter-html", "./results.html", "--reporter-junit", "./report.xml")

if ($brunoRecursive) {
    # V2/V3: use -r (recursive) with the filter path
    $runArgs = $commonArgs + @("-r") + $BrunoFilter + $reportArgs
} else {
    # V1: pass test folder paths directly without -r
    $runArgs = $commonArgs + $BrunoFilter + $reportArgs
}

Push-Location $brunoDir
try {
    if ($UseGlobalBru) {
        & bru @runArgs
    } else {
        # Disable strict mode temporarily — npx.ps1 references $MyInvocation.Statement
        # which is not available under Set-StrictMode -Version Latest
        Set-StrictMode -Off
        npx --yes @usebruno/cli@latest @runArgs
        Set-StrictMode -Version Latest
    }
    $brunoExitCode = $LASTEXITCODE
} finally {
    Pop-Location
    $env:NODE_TLS_REJECT_UNAUTHORIZED = $null

    # ---------------------------------------------------------------------------
    # 7. Optional tear-down — runs whether tests passed or failed
    # ---------------------------------------------------------------------------
    if ($TearDown) {
        Write-Host ""
        Write-Host "🧹 Tearing down containers..." -ForegroundColor Yellow
        docker compose -f $composeFile --env-file $envFile down -v
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠️  docker compose down exited with $LASTEXITCODE" -ForegroundColor Yellow
        } else {
            Write-Host "✅ Containers removed" -ForegroundColor Green
        }
    }
}

if ($brunoExitCode -ne 0) {
    Write-Host ""
    Write-Host "❌ Bruno tests failed (exit $brunoExitCode)" -ForegroundColor Red
    Write-Host "   HTML report : $brunoDir\results.html"
    Write-Host "   XML report  : $brunoDir\report.xml"
    exit $brunoExitCode
}

Write-Host ""
Write-Host "✅ All Bruno tests passed!" -ForegroundColor Green
Write-Host "   HTML report : $brunoDir\results.html"
Write-Host "   XML report  : $brunoDir\report.xml"
