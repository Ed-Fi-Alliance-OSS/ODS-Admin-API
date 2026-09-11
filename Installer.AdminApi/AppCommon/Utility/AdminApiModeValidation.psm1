# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

# Single source of truth for which Ed-Fi Data Standard versions this repo's
# installer supports, and which EdFi.Db.Deploy tool version to use for each.
# This is NOT a technical constraint of the -s/--standardVersion CLI flag
# itself: EdFi.Db.Deploy ships no embedded per-standard SQL and its -s flag
# takes any string (confirmed against the cached 4.3.2 tool -- '--help' shows
# no ValidateSet, just a free-text default of '5.1.0'), so a single tool build
# can run any Data Standard version whose migration scripts exist at the
# --filePaths it's given. 4.3.2 (.NET 10, matching this repo's own TargetFramework)
# is confirmed to work for 4.0.0, 5.2.0, 6.0.0, and 6.1.0, so all four map to it
# here. The table stays keyed by StandardVersion (rather than collapsing to one
# constant) so a future Data Standard version can be added with whatever
# DbDeployVersion it actually needs, without disturbing the existing entries.
$script:SupportedStandardVersions = [ordered]@{
    '4.0.0' = '4.3.2'
    '5.2.0' = '4.3.2'
    '6.0.0' = '4.3.2'
    '6.1.0' = '4.3.2'
}

function Test-EncryptionKeyFormat {
    <#
    .SYNOPSIS
        Validates the format of an Admin Api EncryptionKey value.
    .DESCRIPTION
        An empty or whitespace-only value is considered valid here (callers
        decide whether a key is required for a given AdminApiMode). A
        non-empty value must be a valid base64-encoded string that decodes
        to exactly 32 bytes (256 bits), matching the
        OdsConnectionStringEncryptionKey contract used by the Ed-Fi ODS/API.
    #>
    [CmdletBinding()]
    param (
        [string]
        $EncryptionKey
    )

    if ([string]::IsNullOrWhiteSpace($EncryptionKey)) {
        return $true
    }

    try {
        $bytes = [Convert]::FromBase64String($EncryptionKey)
    }
    catch [FormatException] {
        throw "Encryption key must be a valid base64-encoded string. This key must match the OdsConnectionStringEncryptionKey used in your Ed-Fi ODS / API installation."
    }

    if ($bytes.Length -ne 32) {
        throw "Encryption key must be exactly 32 bytes (256 bits) when decoded. Provided key is $($bytes.Length) bytes. This key must match the OdsConnectionStringEncryptionKey used in your Ed-Fi ODS / API installation."
    }

    return $true
}

function Assert-AdminApiModeCompatibility {
    <#
    .SYNOPSIS
        Validates an AdminApiMode / StandardVersion / EncryptionKey combination.
    .DESCRIPTION
        Throws a descriptive error for the first invalid condition found:
        unsupported AdminApiMode, unsupported StandardVersion, v1 mode paired
        with a non-4.0.0 StandardVersion, v2/v3 mode missing an EncryptionKey,
        or a malformed EncryptionKey.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]
        $AdminApiMode,

        [Parameter(Mandatory = $true)]
        [string]
        $StandardVersion,

        [string]
        $EncryptionKey
    )

    if ($AdminApiMode -notin @('v1', 'v2', 'v3')) {
        throw "AdminApiMode must be one of: v1, v2, v3. Received: $AdminApiMode."
    }

    if ($StandardVersion -notin $script:SupportedStandardVersions.Keys) {
        throw "StandardVersion must be one of: $($script:SupportedStandardVersions.Keys -join ', '). Received: $StandardVersion."
    }

    if ($AdminApiMode -eq 'v1' -and $StandardVersion -ne '4.0.0') {
        throw "Admin API v1 mode only supports StandardVersion 4.0.0."
    }

    if (($AdminApiMode -eq 'v2' -or $AdminApiMode -eq 'v3') -and [string]::IsNullOrWhiteSpace($EncryptionKey)) {
        throw "EncryptionKey is required for Admin API v2 and v3 modes. This key must match the OdsConnectionStringEncryptionKey used in your Ed-Fi ODS / API installation."
    }

    Test-EncryptionKeyFormat -EncryptionKey $EncryptionKey | Out-Null

    return $true
}

function Get-RedactedBoundParameters {
    <#
    .SYNOPSIS
        Returns a copy of a bound-parameters dictionary with sensitive keys masked.
    .DESCRIPTION
        Never mutates the input. Used to build a safe object to pass to
        invocation-logging helpers (e.g. Write-InvocationInfo) without ever
        exposing a secret parameter's real value in console output or logs.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        $BoundParameters,

        [Parameter(Mandatory = $true)]
        [string[]]
        $SensitiveKeys
    )

    $redacted = @{}
    foreach ($key in $BoundParameters.Keys) {
        $redacted[$key] = $BoundParameters[$key]
    }

    foreach ($key in $SensitiveKeys) {
        if ($redacted.ContainsKey($key) -and -not [string]::IsNullOrWhiteSpace([string]$redacted[$key])) {
            $redacted[$key] = '***REDACTED***'
        }
    }

    return $redacted
}

function Get-CarriedForwardAppSetting {
    <#
    .SYNOPSIS
        Chooses between a value carried forward from a prior install and a
        newly-deployed package's default.
    .DESCRIPTION
        Returns OldValue when it is present (not null), so an existing
        installation's setting survives an upgrade. Returns CurrentValue when
        OldValue is null/absent (e.g. upgrading from an install that predates
        this setting), so the newly-deployed package's shipped default is not
        silently overwritten with null.
    #>
    [CmdletBinding()]
    param (
        $OldValue,

        $CurrentValue
    )

    if ($null -ne $OldValue) {
        return $OldValue
    }

    return $CurrentValue
}

function Get-SupportedStandardVersions {
    <#
    .SYNOPSIS
        Returns the Data Standard versions this repo's installer supports.
    .DESCRIPTION
        Exposes $script:SupportedStandardVersions.Keys so other modules (e.g.
        ToolsHelper.psm1's Invoke-DbDeploy) can validate against the single
        source of truth instead of keeping their own hardcoded list.
    #>
    [CmdletBinding()]
    param ()

    return $script:SupportedStandardVersions.Keys
}

function Get-DbDeployVersionForStandardVersion {
    <#
    .SYNOPSIS
        Maps a Data Standard version to the known-good EdFi.Db.Deploy tool
        version this repo tests it against.
    .DESCRIPTION
        Looks up $script:SupportedStandardVersions — the same table
        Assert-AdminApiModeCompatibility validates against — so the installer
        downloads the DbDeploy build this repo pairs with the install's
        StandardVersion, which Invoke-DbUpScripts then passes through to
        Invoke-DbDeploy's -s flag (AppCommon/Utility/ToolsHelper.psm1). An
        empty/unset StandardVersion (e.g. the upgrade path, which does not
        track it) defaults to 5.2.0's mapping, matching Invoke-DbDeploy's own
        default.
    #>
    [CmdletBinding()]
    param (
        [string]
        $StandardVersion
    )

    if ([string]::IsNullOrWhiteSpace($StandardVersion)) {
        $StandardVersion = '5.2.0'
    }

    if (-not $script:SupportedStandardVersions.Contains($StandardVersion)) {
        throw "No known EdFi.Db.Deploy version for StandardVersion: $StandardVersion."
    }

    return $script:SupportedStandardVersions[$StandardVersion]
}
