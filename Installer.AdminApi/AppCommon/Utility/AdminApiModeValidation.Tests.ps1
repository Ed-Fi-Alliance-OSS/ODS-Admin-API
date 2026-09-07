# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

#Requires -Modules @{ModuleName='Pester'; ModuleVersion='5.0.0'}

BeforeAll {
    Import-Module "$PSScriptRoot/AdminApiModeValidation.psm1" -Force
}

Describe 'Test-EncryptionKeyFormat' {
    It 'returns true for an empty key' {
        Test-EncryptionKeyFormat -EncryptionKey '' | Should -Be $true
    }

    It 'returns true for a valid 32-byte base64 key' {
        $validKey = [Convert]::ToBase64String((New-Object byte[] 32))
        Test-EncryptionKeyFormat -EncryptionKey $validKey | Should -Be $true
    }

    It 'throws for a non-base64 string' {
        { Test-EncryptionKeyFormat -EncryptionKey 'not-base64!!!' } | Should -Throw '*valid base64-encoded string*'
    }

    It 'throws when the decoded key is not 32 bytes' {
        $shortKey = [Convert]::ToBase64String((New-Object byte[] 16))
        { Test-EncryptionKeyFormat -EncryptionKey $shortKey } | Should -Throw '*exactly 32 bytes*'
    }
}

Describe 'Assert-AdminApiModeCompatibility' {
    BeforeAll {
        $script:validKey = [Convert]::ToBase64String((New-Object byte[] 32))
    }

    It 'throws for an unsupported AdminApiMode' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v4' -StandardVersion '4.0.0' } | Should -Throw '*AdminApiMode must be one of*'
    }

    It 'throws for an unsupported StandardVersion' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v2' -StandardVersion '3.0.0' -EncryptionKey $script:validKey } | Should -Throw '*StandardVersion must be one of*'
    }

    It 'throws when v1 mode is combined with a non-4.0.0 StandardVersion' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v1' -StandardVersion '5.2.0' } | Should -Throw '*v1 mode only supports StandardVersion 4.0.0*'
    }

    It 'does not throw for v1 mode with StandardVersion 4.0.0 and no EncryptionKey' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v1' -StandardVersion '4.0.0' } | Should -Not -Throw
    }

    It 'throws when v2 mode has no EncryptionKey' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v2' -StandardVersion '5.2.0' } | Should -Throw '*EncryptionKey is required*'
    }

    It 'throws when v3 mode has no EncryptionKey' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v3' -StandardVersion '5.2.0' } | Should -Throw '*EncryptionKey is required*'
    }

    It 'does not throw for v2 mode with a valid EncryptionKey' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v2' -StandardVersion '5.2.0' -EncryptionKey $script:validKey } | Should -Not -Throw
    }

    It 'does not throw for v3 mode with a valid EncryptionKey' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v3' -StandardVersion '5.2.0' -EncryptionKey $script:validKey } | Should -Not -Throw
    }

    It 'throws when v2 mode is given a malformed (non-base64) EncryptionKey' {
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v2' -StandardVersion '5.2.0' -EncryptionKey 'not-base64!!!' } | Should -Throw '*valid base64-encoded string*'
    }

    It 'throws when v3 mode is given an EncryptionKey of the wrong decoded length' {
        $shortKey = [Convert]::ToBase64String((New-Object byte[] 16))
        { Assert-AdminApiModeCompatibility -AdminApiMode 'v3' -StandardVersion '5.2.0' -EncryptionKey $shortKey } | Should -Throw '*exactly 32 bytes*'
    }
}

Describe 'Get-RedactedBoundParameters' {
    It 'masks a sensitive key that is present and non-empty' {
        $bound = @{ EncryptionKey = 'super-secret-value'; AdminApiMode = 'v2' }
        $redacted = Get-RedactedBoundParameters -BoundParameters $bound -SensitiveKeys @('EncryptionKey')
        $redacted['EncryptionKey'] | Should -Be '***REDACTED***'
    }

    It 'leaves non-sensitive keys untouched' {
        $bound = @{ EncryptionKey = 'super-secret-value'; AdminApiMode = 'v2' }
        $redacted = Get-RedactedBoundParameters -BoundParameters $bound -SensitiveKeys @('EncryptionKey')
        $redacted['AdminApiMode'] | Should -Be 'v2'
    }

    It 'does not mutate the original dictionary' {
        $bound = @{ EncryptionKey = 'super-secret-value' }
        Get-RedactedBoundParameters -BoundParameters $bound -SensitiveKeys @('EncryptionKey') | Out-Null
        $bound['EncryptionKey'] | Should -Be 'super-secret-value'
    }

    It 'leaves an absent sensitive key absent' {
        $bound = @{ AdminApiMode = 'v1' }
        $redacted = Get-RedactedBoundParameters -BoundParameters $bound -SensitiveKeys @('EncryptionKey')
        $redacted.ContainsKey('EncryptionKey') | Should -Be $false
    }

    It 'leaves an empty sensitive key unredacted' {
        $bound = @{ EncryptionKey = '' }
        $redacted = Get-RedactedBoundParameters -BoundParameters $bound -SensitiveKeys @('EncryptionKey')
        $redacted['EncryptionKey'] | Should -Be ''
    }
}

Describe 'Get-CarriedForwardAppSetting' {
    It 'returns OldValue when OldValue is present' {
        Get-CarriedForwardAppSetting -OldValue 'v2' -CurrentValue 'v3' | Should -Be 'v2'
    }

    It 'returns CurrentValue when OldValue is null (pre-feature install)' {
        Get-CarriedForwardAppSetting -OldValue $null -CurrentValue 'v3' | Should -Be 'v3'
    }

    It 'returns null when both OldValue and CurrentValue are null' {
        Get-CarriedForwardAppSetting -OldValue $null -CurrentValue $null | Should -Be $null
    }
}
