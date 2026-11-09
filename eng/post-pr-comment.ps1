# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

<#
.SYNOPSIS
    Creates or updates a single pull request comment identified by a marker.

.DESCRIPTION
    Looks for an existing comment on the pull request whose body contains the
    marker (an HTML comment) and updates it in place; otherwise creates a new
    comment. Uses the GitHub CLI, so GH_TOKEN (or a logged-in gh) is required
    and the token needs pull-requests: write.

.PARAMETER PullRequest
    Pull request number.

.PARAMETER Marker
    HTML comment that identifies the comment, e.g. "<!-- my-report -->". It is
    prepended to the body when the body does not already contain it.

.PARAMETER BodyPath
    File with the markdown body.

.PARAMETER Repository
    owner/name. Defaults to $env:GITHUB_REPOSITORY.

.PARAMETER MaxLength
    Bodies longer than this are truncated with a note (GitHub's limit is 65536).

.EXAMPLE
    ./eng/post-pr-comment.ps1 -PullRequest 546 -Marker "<!-- test-summary -->" -BodyPath comment.md -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [int]
    $PullRequest,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^<!--.*-->$')]
    [string]
    $Marker,

    [Parameter(Mandatory = $true)]
    [string]
    $BodyPath,

    [string]
    $Repository = $env:GITHUB_REPOSITORY,

    [ValidateRange(1000, 65536)]
    [int]
    $MaxLength = 65000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $false

if (-not $Repository) { throw "Repository is required (owner/name) when GITHUB_REPOSITORY is not set." }
if (-not (Test-Path -LiteralPath $BodyPath)) { throw "Body file not found: $BodyPath" }

$body = Get-Content -LiteralPath $BodyPath -Raw
if (-not $body.Contains($Marker)) { $body = "$Marker`n$body" }

if ($body.Length -gt $MaxLength) {
    $note = "`n`n_Comment truncated; the full report is on the workflow run page._`n"
    $body = $body.Substring(0, $MaxLength - $note.Length) + $note
}

# Find an existing comment carrying the marker (first page is 100 comments, paginate to be safe).
$jqMarker = $Marker.Replace('\', '\\').Replace('"', '\"')
$existing = @(gh api "repos/$Repository/issues/$PullRequest/comments?per_page=100" --paginate `
        --jq ".[] | select(.body | contains(`"$jqMarker`")) | .id")
if ($LASTEXITCODE -ne 0) { throw "Could not list comments on $Repository#$PullRequest (gh exit $LASTEXITCODE)." }
$existing = @($existing | Where-Object { $_ })

# Not New-TemporaryFile: it honours -WhatIf and would return nothing.
$payload = [System.IO.Path]::GetTempFileName()
@{ body = $body } | ConvertTo-Json -Compress | Set-Content -LiteralPath $payload -Encoding utf8 -NoNewline

try {
    if ($existing.Count -gt 0) {
        $id = $existing[0]
        if ($PSCmdlet.ShouldProcess("$Repository#$PullRequest comment $id", "Update comment ($($body.Length) chars)")) {
            $url = gh api -X PATCH "repos/$Repository/issues/comments/$id" --input $payload --jq ".html_url"
            if ($LASTEXITCODE -ne 0) { throw "Updating comment $id failed (gh exit $LASTEXITCODE)." }
            Write-Host "Updated comment $url"
        }
    }
    else {
        if ($PSCmdlet.ShouldProcess("$Repository#$PullRequest", "Create comment ($($body.Length) chars)")) {
            $url = gh api -X POST "repos/$Repository/issues/$PullRequest/comments" --input $payload --jq ".html_url"
            if ($LASTEXITCODE -ne 0) { throw "Creating the comment failed (gh exit $LASTEXITCODE)." }
            Write-Host "Created comment $url"
        }
    }
}
finally {
    Remove-Item -LiteralPath $payload -ErrorAction SilentlyContinue
}
