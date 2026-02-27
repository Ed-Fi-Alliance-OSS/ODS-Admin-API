#requires -version 5

$ErrorActionPreference = "Stop"

function Enable-IisFeature {
    Param (
        [string] [Parameter(Mandatory=$true)] $featureName
    )

    $feature = Get-WindowsOptionalFeature -FeatureName $featureName -Online
    if (-not $feature -or $feature.State -ne "Enabled") {
        Write-Debug "Enabling Windows feature: $($featureName)"

        $result = Enable-WindowsOptionalFeature -Online -FeatureName $featureName -NoRestart
        return $result.RestartNeeded
    }
    return $false
}

function Invoke-ThrowIfDotnetHostingBundleMissing {
    param ([string]$VersionString = "6.0.0")

    try {
        $requiredVersion = [System.Version]::Parse($VersionString)
    }
    catch [System.ArgumentException],[System.ArgumentOutOfRangeException],[System.FormatException],[System.OverflowException] {
        throw "The input Version '$VersionString' could not be parsed and may be invalid. Version must be a valid semantic version number."
    }

    $updatesPath = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Updates\.NET"
    $items = Get-Item -ErrorAction SilentlyContinue -Path $updatesPath
    $requiredVersionInstalled = $False

    if($items)
    {
        $items.GetSubKeyNames() | Where-Object { $_ -Match "Microsoft .NET .*Windows Server Hosting" } | ForEach-Object {
                $registryKeyPath = Get-Item -Path "$updatesPath\$_"
                $dotNetCoreVersion = $registryKeyPath.GetValue("PackageVersion")
                $installedDotNetCoreVersion = [System.Version]::Parse($dotNetCoreVersion)

                if($installedDotNetCoreVersion -ge $requiredVersion) {
                    Write-Host "The host has the following .NET Core Hosting Bundle: $_ (MinimumVersion requirement: $requiredVersion)"
                    $requiredVersionInstalled = $True
                }
        }
    }

    if(-Not $requiredVersionInstalled)
    {
        throw "The $VersionString .NET Hosting Bundle couldn't be found on the system, and is required for install.
        Please install $requiredVersion or greater. Can be downloaded from https://dotnet.microsoft.com/en-us/download/dotnet"
    }
}


function Enable-RequiredIisFeatures {
    Write-Host "Installing IIS Features in Windows" -ForegroundColor Cyan

    $restartNeeded = Enable-IisFeature IIS-WebServerRole
    $restartNeeded += Enable-IisFeature IIS-WebServer
    $restartNeeded += Enable-IisFeature IIS-CommonHttpFeatures
    $restartNeeded += Enable-IisFeature IIS-HttpErrors
    $restartNeeded += Enable-IisFeature IIS-HttpRedirect
    $restartNeeded += Enable-IisFeature IIS-ApplicationDevelopment
    $restartNeeded += Enable-IisFeature IIS-HealthAndDiagnostics
    $restartNeeded += Enable-IisFeature IIS-HttpLogging
    $restartNeeded += Enable-IisFeature IIS-LoggingLibraries
    $restartNeeded += Enable-IisFeature IIS-RequestMonitor
    $restartNeeded += Enable-IisFeature IIS-HttpTracing
    $restartNeeded += Enable-IisFeature IIS-Security
    $restartNeeded += Enable-IisFeature IIS-RequestFiltering
    $restartNeeded += Enable-IisFeature IIS-Performance
    $restartNeeded += Enable-IisFeature IIS-WebServerManagementTools
    $restartNeeded += Enable-IisFeature IIS-IIS6ManagementCompatibility
    $restartNeeded += Enable-IisFeature IIS-Metabase
    $restartNeeded += Enable-IisFeature IIS-ManagementConsole
    $restartNeeded += Enable-IisFeature IIS-BasicAuthentication
    $restartNeeded += Enable-IisFeature IIS-WindowsAuthentication
    $restartNeeded += Enable-IisFeature IIS-StaticContent
    $restartNeeded += Enable-IisFeature IIS-DefaultDocument
    $restartNeeded += Enable-IisFeature IIS-WebSockets
    $restartNeeded += Enable-IisFeature IIS-ApplicationInit
    $restartNeeded += Enable-IisFeature IIS-ISAPIExtensions
    $restartNeeded += Enable-IisFeature IIS-ISAPIFilter
    $restartNeeded += Enable-IisFeature IIS-HttpCompressionStatic
    $restartNeeded += Enable-IisFeature NetFx4Extended-ASPNET45
    $restartNeeded += Enable-IisFeature IIS-NetFxExtensibility45
    $restartNeeded += Enable-IisFeature IIS-ASPNET45

    return $restartNeeded
}

function Get-FileFromInternet {
    param (
        [string] [Parameter(Mandatory=$true)] $url
    )

    New-Item -Force -ItemType Directory "downloads" | Out-Null

    $fileName = $url.split('/')[-1]
    $output = "downloads\$fileName"

    if (Test-Path $output) {
        # File already exists, don't attempt to re-download
        return $output
    }

    Invoke-WebRequest -Uri $url -OutFile $output

    return $output
}

function Test-FileHash {
    param(
        [string] [Parameter(Mandatory=$true)] $ExpectedHashValue,
        [string] [Parameter(Mandatory=$true)] $FilePath
    )

    $calculated = (Get-FileHash $FilePath -Algorithm SHA512).Hash

    if ($ExpectedHashValue -ne $calculated) {
        throw "Aborting install: cannot be sure of the integrity of the downloaded file " +
            "$FilePath. Please contact techsupport@ed-fi.org or create a " +
            "bug report at https://tracker.ed-fi.org"
    }
}

function Install-IISUrlRewriteModule {
    Write-Host "Downloading IIS Rewrite Module 2"
    $url = "https://odsassets.blob.core.windows.net/public/installers/url_rewrite/rewrite_amd64_en-US.msi"
    $downloadedFile = Get-FileFromInternet $url

    $expectedHash = "90C5B7934F69FAA77DE946678747FFE4BD0E80647DF79C373C73FA3C5864F85C2DF1C3BD6B6B47A767D2E75493D2E986D2382769ABE9BEB143C5E196715AE6BF"
    Test-FileHash -FilePath $downloadedFile -ExpectedHashValue $expectedHash

    $logFile = "rewrite_amd64_install.log"
    $absoluteLogFile = join-path (resolve-path .) $logFile

    Write-Host "Installing IIS Rewrite Module 2" -ForegroundColor Cyan
    Write-Host "Appending to log file $absoluteLogFile"

    $command = "msiexec"
    $argumentList = "/i $downloadedFile /quiet /l*v+! $logFile"

    Write-Host "$command $argumentList" -ForegroundColor Magenta
    $msiExitCode = (Start-Process $command -ArgumentList $argumentList -PassThru -Wait).ExitCode

    if ($msiExitCode) {
        Write-Host "Installation of IIS Rewrite Module 2:" -ForegroundColor Yellow
        Write-Host "    msiexec returned status code $msiExitCode." -ForegroundColor Yellow
        Write-Host "    See $absoluteLogFile for details." -ForegroundColor Yellow
    } else {
        Write-Host "Installation of IIS Rewrite Module 2:"
        Write-Host "    msiexec returned status code $msiExitCode (normal)."
        Write-Host "    See $absoluteLogFile for details."
    }
}

function Initialize-IISWithPrerequisites{
    param()

    $restartNeeded = Enable-RequiredIisFeatures

    if (Get-Command "AI_GetMsiProperty" -ErrorAction SilentlyContinue) {
        $explanation = "Because the Advanced Installer package is running and responsible for MSI"
        $explanation = $explanation + " prerequisites, skipping unnecessary invocation of IIS Rewrite Module MSI."
        Write-Host $explanation
    } else {
        Install-IISUrlRewriteModule
    }

    Install-NuGetAndSqlServer

    Write-Host "Prerequisites verified" -ForegroundColor Green

    return $restartNeeded
}

function Install-NuGetAndSqlServer {
    $isNugetAvailable = Get-PackageProvider  | Where-Object {$_.Name -eq "Nuget"} | Select-Object -ExpandProperty Name

    if ([string]::IsNullOrEmpty($isNugetAvailable)) {
        Install-PackageProvider -Name NuGet -MinimumVersion "2.8.5.201" -Scope CurrentUser -Force
        Write-Host "Nuget  installed successfully" -ForegroundColor Green
    }
}

function Install-DotNetCore {
    param (
        $toolsPath
    )

    $installDir = "$toolsPath\dotnet"

    & "$PSScriptRoot\dotnet-install.ps1" -Version 3.1.301 -InstallDir $installDir

    $absoluteInstallDir = (Resolve-Path $installDir).Path

    Write-Host "Setting DOTNET_ROOT to $($absoluteInstallDir)"
    Write-Host "    Previous Value: $($env:DOTNET_ROOT)"
    $env:DOTNET_ROOT = $absoluteInstallDir
}

$functions = @(
    "Initialize-IISWithPrerequisites"
    "Test-MinimumPowershellInstalled"
    "Install-DotNetCore"
    "Invoke-ThrowIfDotnetHostingBundleMissing"
)

Export-ModuleMember -Function $functions
