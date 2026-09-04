# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

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

    if ($StandardVersion -notin @('4.0.0', '5.2.0')) {
        throw "StandardVersion must be one of: 4.0.0, 5.2.0. Received: $StandardVersion."
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
