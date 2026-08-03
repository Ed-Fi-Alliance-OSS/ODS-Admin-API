# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

<#
.SYNOPSIS
    Runs the Admin API DBTests suites locally against containerized SQL
    Server and/or PostgreSQL instances.

.DESCRIPTION
    Starts bare database engine containers (via eng/db-tests-compose.yml —
    no Admin API application container, just the databases), applies the
    Admin API DbUp migrations to a fresh EdFi_Admin_Test database on each
    requested engine, then runs `dotnet test` for the given DBTests
    project(s).

    This exists because the repo has no local database available by
    default for `*.DBTests` projects (unlike the Bruno E2E tests, which
    have eng/run-bruno-e2e.ps1 to stand up full containers). Modeled on
    that script's structure: build/health-check containers, prepare state,
    run tests, optional teardown.

    NOTE: as of this writing, EdFi.Ods.AdminApi.DBTests and
    EdFi.Ods.AdminApi.V3.DBTests hard-code SQL Server
    (AdminApiDbContextTestBase.GetAdminApiDbContextOptions calls
    UseSqlServer unconditionally) — so -DbEngine pgsql only stands up
    PostgreSQL and applies migrations to it for manual verification (e.g.
    Task 9's "repeat against PostgreSQL" step); it does not change what
    engine the NUnit suite itself connects to.

.PARAMETER Project
    Which DBTests project(s) to run:
    "V2"  — Application/EdFi.Ods.AdminApi.DBTests (default)
    "V3"  — Application/EdFi.Ods.AdminApi.V3.DBTests
    "All" — both

.PARAMETER DbEngine
    "mssql" — SQL Server only (default; the only engine the DBTests suites can actually connect to today)
    "pgsql" — PostgreSQL only (migrations applied and verified; NUnit suite still runs against mssql if also requested)
    "both"  — start both containers, migrate both, run tests against mssql

.PARAMETER Filter
    Optional `dotnet test --filter` value, e.g. "FullyQualifiedName~AuditLog".

.PARAMETER SkipDockerUp
    Skip starting containers (assume they're already running).

.PARAMETER TearDown
    Tear down containers after the run.

.EXAMPLE
    # Stand up SQL Server, migrate, run the V2 audit log writer DB test
    .\eng\run-db-tests.ps1 -Project V2 -Filter "FullyQualifiedName~AuditLog" -TearDown

.EXAMPLE
    # Verify migrations apply cleanly on both engines (Task 9 style check), then tear down
    .\eng\run-db-tests.ps1 -DbEngine both -Project All -TearDown
#>

param(
    [ValidateSet("V2", "V3", "All")]
    [string]$Project = "V2",

    [ValidateSet("mssql", "pgsql", "both")]
    [string]$DbEngine = "mssql",

    [string]$Filter,

    [switch]$SkipDockerUp,
    [switch]$TearDown
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$composeFile = Join-Path $PSScriptRoot "db-tests-compose.yml"

$mssqlPassword = "P@55w0rd"
$pgsqlPassword = "P@55w0rd"

$startMssql = $DbEngine -in @("mssql", "both")
$startPgsql = $DbEngine -in @("pgsql", "both")

function Assert-ExitCode([string]$step) {
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ $step failed (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Admin API DB Tests — $Project / $DbEngine" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

# ---------------------------------------------------------------------------
# 1. Start containers
# ---------------------------------------------------------------------------
if (-not $SkipDockerUp) {
    $services = @()
    if ($startMssql) { $services += "mssql" }
    if ($startPgsql) { $services += "pgsql" }

    Write-Host ""
    Write-Host "🐳 Starting containers: $($services -join ', ')..." -ForegroundColor Yellow
    docker compose -f $composeFile up -d @services
    Assert-ExitCode "docker compose up"
}

# ---------------------------------------------------------------------------
# 2. Wait for readiness
# ---------------------------------------------------------------------------
if ($startMssql) {
    Write-Host ""
    Write-Host "⏳ Waiting for SQL Server to accept connections..." -ForegroundColor Yellow
    $timeout = 120
    $elapsed = 0
    $ready = $false
    while ($elapsed -lt $timeout) {
        docker exec adminapi-dbtests-mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $mssqlPassword -C -Q "SELECT 1" *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 3
        $elapsed += 3
    }
    if (-not $ready) {
        Write-Host "❌ SQL Server did not become ready within ${timeout}s" -ForegroundColor Red
        docker logs adminapi-dbtests-mssql --tail 50
        exit 1
    }
    Write-Host "✅ SQL Server ready" -ForegroundColor Green
}

if ($startPgsql) {
    Write-Host ""
    Write-Host "⏳ Waiting for PostgreSQL to accept connections..." -ForegroundColor Yellow
    $timeout = 60
    $elapsed = 0
    $ready = $false
    while ($elapsed -lt $timeout) {
        docker exec adminapi-dbtests-pgsql pg_isready -U postgres *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 3
        $elapsed += 3
    }
    if (-not $ready) {
        Write-Host "❌ PostgreSQL did not become ready within ${timeout}s" -ForegroundColor Red
        docker logs adminapi-dbtests-pgsql --tail 50
        exit 1
    }
    Write-Host "✅ PostgreSQL ready" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# 3. Apply Admin API migrations
# ---------------------------------------------------------------------------
Import-Module -Name "$PSScriptRoot/database-manager.psm1" -Force

# Known-good EdFi.Db.Deploy / standard versions for this repo's supported ODS
# version (see build.ps1's $supportedApiVersions7x). run-dbup-migrations.ps1
# does not currently pass these through to Install-AdminApiTables, which
# requires them — so this script calls Install-AdminApiTables directly.
$dbDeployVersion = "4.1.52"
$standardVersion = "5.2.0"
$nugetFeed = "https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_packaging/EdFi/nuget/v3/index.json"

if ($startMssql) {
    Write-Host ""
    Write-Host "📐 Applying migrations to SQL Server (EdFi_Admin, EdFi_Admin_Test)..." -ForegroundColor Yellow
    $mssqlArgs = @{
        ToolsPath             = ".tools"
        DbDeployVersion       = $dbDeployVersion
        StandardVersion       = $standardVersion
        NuGetFeed             = $nugetFeed
        DatabaseType          = "Admin"
        ForPostgreSQL         = $false
        Server                = "localhost"
        Port                  = 1433
        UseIntegratedSecurity = $false
        Username              = "sa"
        Password              = $mssqlPassword
    }
    Push-Location $PSScriptRoot
    try {
        foreach ($db in @("EdFi_Admin", "EdFi_Admin_Test")) {
            Write-Host "  -> $db"
            Install-AdminApiTables @mssqlArgs -DatabaseName $db
        }
    } finally {
        Pop-Location
    }
    Write-Host "✅ SQL Server migrations applied" -ForegroundColor Green
}

if ($startPgsql) {
    Write-Host ""
    Write-Host "📐 Applying migrations to PostgreSQL (edfi_admin, edfi_admin_test)..." -ForegroundColor Yellow

    docker exec adminapi-dbtests-pgsql psql -U postgres -c "SELECT 1 FROM pg_database WHERE datname = 'edfi_admin_test'" | Out-Null

    $pgsqlArgs = @{
        ToolsPath             = ".tools"
        DbDeployVersion       = $dbDeployVersion
        StandardVersion       = $standardVersion
        NuGetFeed             = $nugetFeed
        DatabaseType          = "Admin"
        ForPostgreSQL         = $true
        Server                = "localhost"
        Port                  = 5433
        UseIntegratedSecurity = $false
        Username              = "postgres"
        Password              = $pgsqlPassword
    }
    Push-Location $PSScriptRoot
    try {
        foreach ($db in @("edfi_admin", "edfi_admin_test")) {
            Write-Host "  -> $db"
            Install-AdminApiTables @pgsqlArgs -DatabaseName $db
        }
    } finally {
        Pop-Location
    }
    Write-Host "✅ PostgreSQL migrations applied" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# 4. Run dotnet test
# ---------------------------------------------------------------------------
$projects = @()
if ($Project -in @("V2", "All")) { $projects += "Application/EdFi.Ods.AdminApi.DBTests" }
if ($Project -in @("V3", "All")) { $projects += "Application/EdFi.Ods.AdminApi.V3.DBTests" }

$testExitCode = 0
foreach ($proj in $projects) {
    $projPath = Join-Path $repoRoot $proj
    if (-not (Test-Path $projPath)) {
        Write-Host "⚠️  Skipping $proj — project directory not found" -ForegroundColor Yellow
        continue
    }

    $filterSuffix = if ($Filter) { " --filter $Filter" } else { "" }
    Write-Host ""
    Write-Host "🧪 Running: dotnet test $proj$filterSuffix" -ForegroundColor Yellow

    if ($Filter) {
        dotnet test $projPath --filter $Filter --nologo
    } else {
        dotnet test $projPath --nologo
    }
    if ($LASTEXITCODE -ne 0) { $testExitCode = $LASTEXITCODE }
}

# ---------------------------------------------------------------------------
# 5. Optional teardown
# ---------------------------------------------------------------------------
if ($TearDown) {
    Write-Host ""
    Write-Host "🧹 Tearing down containers..." -ForegroundColor Yellow
    docker compose -f $composeFile down -v
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  docker compose down exited with $LASTEXITCODE" -ForegroundColor Yellow
    } else {
        Write-Host "✅ Containers removed" -ForegroundColor Green
    }
}

if ($testExitCode -ne 0) {
    Write-Host ""
    Write-Host "❌ DB tests failed (exit $testExitCode)" -ForegroundColor Red
    exit $testExitCode
}

Write-Host ""
Write-Host "✅ All DB tests passed!" -ForegroundColor Green
