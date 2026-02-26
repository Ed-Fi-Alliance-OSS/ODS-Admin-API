#requires -version 5
$ErrorActionPreference = "Stop"

$utilityDirectory = Join-Path (Split-Path -Path $PSScriptRoot -Parent) "Utility"
Import-Module -Force -Scope Global "$utilityDirectory\TaskHelper.psm1"

function Uninstall-EdFiApplicationFromIIS {
    <#
    .SYNOPSIS
        Removes an Ed-Fi application from IIS.
    .DESCRIPTION
        Removes an Ed-Fi application from IIS, including its application pool (if
        not used for any other application). Removes the web site as well if there
        are no remaining applications, and the site's app pool.

        Does not remove IIS or the URL Rewrite module.
    #>
    [CmdletBinding()]
    param (
        [string]
        [Parameter(Mandatory=$true)]
        $WebApplicationPath,

        [string]
        [Parameter(Mandatory=$true)]
        $WebApplicationName,

        [string]
        $WebSiteName = "Ed-Fi",

        [switch]
        $NoDuration
    )

    Write-InvocationInfo $MyInvocation
    Clear-Error

    Import-Module -Force -Scope Global "$PSScriptRoot\..\IIS\IIS-Components.psm1"

    $config = @{
        WebApplicationPath = $WebApplicationPath
        WebApplicationName = $WebApplicationName
        WebSiteName = $WebSiteName
        NoDuration = $NoDuration
    }

    $result = @()

    $elapsed = Use-StopWatch {

        $result += Invoke-Task -name "Uninstall-WebApplication" -task {
            $parameters = @{
                WebSiteName = $config.WebSiteName
                WebApplicationName = $config.WebApplicationName
                WebApplicationPath = $config.WebApplicationPath
            }
            Uninstall-WebApplication @parameters
        }

        $result += Invoke-Task -name "Uninstall-WebSite" -task {
            Uninstall-WebSite -WebSiteName $config.WebSiteName
        }
    }

    Test-Error

    if (-not $NoDuration) {
        $result += New-TaskResult -name '-' -duration '-'
        $result += New-TaskResult -name $MyInvocation.MyCommand.Name -duration $elapsed.format
        return $result | Format-Table
    }

    return $result
}

Export-ModuleMember -Function Uninstall-EdFiApplicationFromIIS
