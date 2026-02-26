#requires -version 5

$ErrorActionPreference = "Stop"
function New-SafeDirectory {
    param (
        [string] [Parameter(Mandatory=$true)] $folderPath
    )

    Write-Debug "Attemping to create new directory: $($folderPath)"

    if (!(Test-Path $folderPath)) {
        New-Item -ItemType Directory -Path $folderPath | Out-Null
        Write-Debug "Created new directory: $($folderPath)"
    } else {
        Write-Debug "Did not create new directory $($folderPath) because it already exists"
    }
}

function Grant-FolderAccessToIISUsers {
    param (
        [string] [Parameter(Mandatory=$true)] $folderPath,
        [string] $fileSystemRights = "FullControl"
    )

    Write-Debug "Granting access to $($folderPath) to IIS_IUSRS"

    $acl = Get-Acl $folderPath

    $ar_iis_iusrs = New-Object System.Security.AccessControl.FileSystemAccessRule "IIS_IUSRS", $fileSystemRights, "Allow"
    $acl.SetAccessRule($ar_iis_iusrs)

    Set-Acl $folderPath $acl

    Write-Debug "Assigned $($fileSystemRights) rights to path: $($folderPath)"
}

function Copy-ArchiveOrDirectory {
    param (
        [string] [Parameter(Mandatory=$true)] $sourceLocation,
        [string] [Parameter(Mandatory=$true)] $installLocation
    )

    New-Item -ItemType Directory -Path $installLocation -Force -ErrorAction Stop | Out-Null
    $extension = [IO.Path]::GetExtension($sourceLocation)
    if (".zip" -eq $extension -or ".nupkg" -eq $extension) {
        Write-Debug "Expanding archive: ""$sourceLocation"" to destination path: ""$installLocation"""
        Expand-Archive -path $sourceLocation -destination $installLocation -Force
    } else {
        Write-info "Copying folder: ""$sourceLocation"" to destination: ""$installLocation"""
        $parameters = @{
            Path = $sourceLocation
            Recurse = $true
            Exclude = @(
                "*.nupkg"
                "Web.*.config"
            )
            Destination = $installLocation
            Force = $true
        }

        Copy-Item @parameters
    }
}