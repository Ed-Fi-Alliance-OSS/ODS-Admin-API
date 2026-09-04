# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

#requires -version 5

$ErrorActionPreference = "Stop"

# Azure DevOps hosts the Ed-Fi packages, and it requires TLS 1.2. Older Windows
# PowerShell hosts don't always enable it by default.
if (-not [Net.ServicePointManager]::SecurityProtocol.HasFlag([Net.SecurityProtocolType]::Tls12)) {
    [Net.ServicePointManager]::SecurityProtocol += [Net.SecurityProtocolType]::Tls12
}

function Get-NuGetPackageContent {
    <#
    .SYNOPSIS
        Downloads and extracts the content of a NuGet package without requiring
        nuget.exe or a project-based `dotnet restore`.

    .DESCRIPTION
        Queries the feed's NuGet v3 service index for the PackageBaseAddress
        (flat container) resource, resolves the requested version -- an exact
        match, or the latest available (including prerelease) when
        -Prerelease is set -- downloads that version's .nupkg over HTTP, and
        expands it. A .nupkg is just a zip file, so no NuGet client is needed.

    .OUTPUTS
        Path to the directory containing the extracted package content, named
        "<PackageName>.<ResolvedVersion>" so callers can resolve it the same
        way they did with nuget.exe's install output.
    #>
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $PackageName,

        [string]
        $PackageVersion,

        [Switch]
        $Prerelease,

        [string]
        [Parameter(Mandatory=$true)]
        $NuGetFeed,

        [string]
        [Parameter(Mandatory=$true)]
        $OutputDirectory
    )

    if (-not $Prerelease -and -not $PackageVersion) {
        throw "Get-NuGetPackageContent requires either -PackageVersion or -Prerelease."
    }

    $serviceIndex = Invoke-RestMethod -Uri $NuGetFeed
    $packageBaseAddress = $serviceIndex.resources `
        | Where-Object { $_."@type" -like "PackageBaseAddress*" } `
        | Select-Object -First 1 -ExpandProperty "@id"

    if (-not $packageBaseAddress) {
        throw "Feed $NuGetFeed does not advertise a PackageBaseAddress resource."
    }

    $lowerId = $PackageName.ToLowerInvariant()
    $availableVersions = (Invoke-RestMethod -Uri "$packageBaseAddress$lowerId/index.json").versions

    if ($Prerelease) {
        # The flat container index returns versions in ascending order, so the
        # last entry is the newest available, prerelease or not.
        $version = $availableVersions | Select-Object -Last 1
    }
    else {
        $version = $availableVersions | Where-Object { $_ -eq $PackageVersion } | Select-Object -First 1
        if (-not $version) {
            throw "Version $PackageVersion of package $PackageName was not found on feed $NuGetFeed."
        }
    }

    New-Item -Path $OutputDirectory -ItemType Directory -Force | Out-Null

    $nupkgPath = Join-Path $OutputDirectory "$lowerId.$version.nupkg"
    Invoke-RestMethod -Uri "$packageBaseAddress$lowerId/$version/$lowerId.$version.nupkg" -OutFile $nupkgPath

    $extractPath = Join-Path $OutputDirectory "$PackageName.$version"
    Expand-Archive -Path $nupkgPath -DestinationPath $extractPath -Force
    Remove-Item -Path $nupkgPath -Force

    return $extractPath
}

function Push-Package {
    param (
        [string]
        $PackageFile,

        [string]
        $NuGetFeed,

        [string]
        $NuGetApiKey
    )

    Write-Host "Pushing $PackageFile to $NuGetFeed"
    dotnet nuget push $PackageFile --api-key $NuGetApiKey --source $NuGetFeed
}

function Test-PackageCache {
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $PackageName,

        [string]
        [Parameter(Mandatory=$true)]
        $PackageVersion,

        [string]
        [Parameter(Mandatory=$true)]
        $PackagesPath
    )

    $cacheManifestPath = "$PackagesPath/.package-cache-manifest.json"
    $packageKey = "$PackageName-$PackageVersion"
    $wildcardPath = "$PackagesPath/$PackageName.$($PackageVersion.Split('-')[0])*"

    # Check if package is already cached
    if (Test-Path $cacheManifestPath) {
        try {
            $cacheManifest = Get-Content $cacheManifestPath | ConvertFrom-Json -AsHashtable
            if ($cacheManifest[$packageKey]) {
                $existing = Resolve-Path $wildcardPath -ErrorAction SilentlyContinue
                if ($existing) {
                    Write-Host "Package $PackageName version $PackageVersion already cached, skipping download" -ForegroundColor Green
                    return $true
                }
            }
        }
        catch {
            # If manifest is corrupted, we'll redownload
            Write-Host "Cache manifest corrupted, will redownload packages" -ForegroundColor Yellow
        }
    }

    return $false
}

function Update-PackageCache {
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $PackageName,

        [string]
        [Parameter(Mandatory=$true)]
        $PackageVersion,

        [string]
        [Parameter(Mandatory=$true)]
        $PackagesPath
    )

    $cacheManifestPath = "$PackagesPath/.package-cache-manifest.json"
    $packageKey = "$PackageName-$PackageVersion"

    # Update cache manifest
    $cacheManifest = @{}
    if (Test-Path $cacheManifestPath) {
        try {
            $cacheManifest = Get-Content $cacheManifestPath | ConvertFrom-Json -AsHashtable
        }
        catch {
            $cacheManifest = @{}
        }
    }
    $cacheManifest[$packageKey] = @{
        timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        version = $PackageVersion
    }
    $cacheManifest | ConvertTo-Json | Set-Content $cacheManifestPath
    Write-Host "Package $PackageName version $PackageVersion downloaded and cached" -ForegroundColor Cyan
}

function Move-AppCommon {
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $AppCommonSourceDirectory,

        [string]
        [Parameter(Mandatory=$true)]
        $AppCommonDestinationDirectory
    )

    # Move AppCommon's modules to a destination directory
    @(
        "Application"
        "Environment"
        "IIS"
        "Utility"
    ) | ForEach-Object {
        $parameters = @{
            Recurse = $true
            Force = $true
            Path = "$AppCommonSourceDirectory/$_"
            Destination = "$AppCommonDestinationDirectory/AppCommon/$_"
        }
        Copy-Item @parameters
    }
}

function Get-RestApiPackage {
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $RestApiPackageName,

        [string]
        [Parameter(Mandatory=$true)]
        $RestApiPackageVersion,

        [Switch]
        $RestApiPackagePrerelease,

        [string]
        [Parameter(Mandatory=$true)]
        $PackagesPath,

        [string]
        [Parameter(Mandatory=$true)]
        $NuGetFeed,

        [string]
        $ToolsPath = "$PSScriptRoot/.tools"
    )

    # Determine the full package version string including prerelease
    $fullPackageVersion = if ($RestApiPackagePrerelease) { "$RestApiPackageVersion-prerelease" } else { $RestApiPackageVersion }

    # Check if package is already cached
    $needsDownload = -not (Test-PackageCache -PackageName $RestApiPackageName -PackageVersion $fullPackageVersion -PackagesPath $PackagesPath)

    if ($needsDownload) {
        $wildcardPath = "$PackagesPath/$RestApiPackageName.$RestApiPackageVersion*"

        # Remove anything that already exists, so that it is always easy to
        # use Resolve-Path with a wildcard to find the installed path without
        # having to parse pre-release number of the package.
        $existing = Resolve-Path $wildcardPath -ErrorAction SilentlyContinue
        if ($existing) {
            Remove-Item -Path $existing -Force -ErrorAction SilentlyContinue -Recurse | Out-Null
        }

        New-Item -Path $PackagesPath -ItemType Directory -Force | Out-Null

        Write-Host "Downloading $RestApiPackageName from $NuGetFeed" -ForegroundColor Magenta
        Get-NuGetPackageContent -PackageName $RestApiPackageName `
            -PackageVersion $RestApiPackageVersion `
            -Prerelease:$RestApiPackagePrerelease `
            -NuGetFeed $NuGetFeed `
            -OutputDirectory $PackagesPath | Out-Null

        Update-PackageCache -PackageName $RestApiPackageName -PackageVersion $fullPackageVersion -PackagesPath $PackagesPath
    }

    $wildcardPath = "$PackagesPath/$RestApiPackageName.$RestApiPackageVersion*"
    return (Resolve-Path $wildcardPath)
}

function Add-AppCommon {
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $AppCommonPackageName,

        [string]
        [Parameter(Mandatory=$true)]
        $AppCommonPackageVersion,

        [string]
        [Parameter(Mandatory=$true)]
        $NuGetFeed,

        [string]
        [Parameter(Mandatory=$true)]
        $DestinationPath,

        [string]
        $PackagesPath = "$PSScriptRoot/.packages",

        [string]
        $ToolsPath = "$PSScriptRoot/.tools"
    )

    # Check if package is already cached
    $needsDownload = -not (Test-PackageCache -PackageName $AppCommonPackageName -PackageVersion $AppCommonPackageVersion -PackagesPath $PackagesPath)

    if ($needsDownload) {
        $wildcardPath = "$PackagesPath/$AppCommonPackageName.$AppCommonPackageVersion*"

        # Remove anything that already exists, so that it is always easy to
        # use Resolve-Path with a wildcard to find the installed path without
        # having to parse pre-release number of the package.
        $existing = Resolve-Path $wildcardPath -ErrorAction SilentlyContinue
        if ($existing) {
            Remove-Item -Path $existing -Force -ErrorAction SilentlyContinue -Recurse | Out-Null
        }

        New-Item -Path $PackagesPath -ItemType Directory -Force | Out-Null

        Write-Host "Downloading AppCommon"
        Get-NuGetPackageContent -PackageName $AppCommonPackageName `
            -PackageVersion $AppCommonPackageVersion `
            -NuGetFeed $NuGetFeed `
            -OutputDirectory $PackagesPath | Out-Null

        Update-PackageCache -PackageName $AppCommonPackageName -PackageVersion $AppCommonPackageVersion -PackagesPath $PackagesPath
    }

    $wildcardPath = "$PackagesPath/$AppCommonPackageName.$AppCommonPackageVersion*"
    $appCommonDirectory = Resolve-Path $wildcardPath | Select-Object -Last 1

    Move-AppCommon $appCommonDirectory $DestinationPath
}

$functions = @(
    "Get-NuGetPackageContent",
    "Get-RestApiPackage",
    "Push-Package",
    "Add-AppCommon",
    "Test-PackageCache",
    "Update-PackageCache"
)

Export-ModuleMember -Function $functions
