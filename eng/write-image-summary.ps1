# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

<#
.SYNOPSIS
    Summarizes Docker Scout SARIF files into one markdown table of vulnerability counts per image.

.DESCRIPTION
    Reads every sarif-<image>.output.json in -SarifDir (written by docker/scout-action with
    command "cves"), counts the results per severity and writes a markdown table. The table is
    written to -OutputPath (used as a pull request comment body) and appended to
    $env:GITHUB_STEP_SUMMARY when that variable is set. The script always exits 0.

    Images named in -ExpectedImages that have no SARIF file get a "not analyzed" row, so an
    image whose build or scan failed does not silently drop out of the table.

    Severity comes from the rule's tags (CRITICAL, HIGH, MEDIUM, LOW, UNSPECIFIED). When a rule
    has no such tag, its security-severity (CVSS) score is bucketed: >= 9.0 critical,
    >= 7.0 high, >= 4.0 medium, otherwise low.

.PARAMETER SarifDir
    Folder containing the sarif-*.output.json files.

.PARAMETER RunUrl
    URL of the workflow run, linked from the summary.

.PARAMETER OutputPath
    Markdown file to write.

.PARAMETER ExpectedImages
    Image names that should have a SARIF file (the docker-analysis matrix names). Empty entries
    are ignored. Images in this list without a file are reported as not analyzed.

.PARAMETER Marker
    HTML comment written as the first line of the output. The workflow reads it back to find and
    update the existing PR comment.

.EXAMPLE
    ./eng/write-image-summary.ps1 -SarifDir scout-sarif -ExpectedImages postgres,development -RunUrl "https://github.com/org/repo/actions/runs/1" -OutputPath image-summary.md
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]
    $SarifDir,

    [Parameter(Mandatory = $true)]
    [string]
    $RunUrl,

    [Parameter(Mandatory = $true)]
    [string]
    $OutputPath,

    [string[]]
    $ExpectedImages = @(),

    [string]
    $Marker = "<!-- docker-scout-image-summary -->"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$severities = @("CRITICAL", "HIGH", "MEDIUM", "LOW")

function Get-Severity {
    param($RuleProperties)

    if ($null -ne $RuleProperties) {
        $tags = @($RuleProperties.PSObject.Properties["tags"]?.Value)
        $tag = $tags | Where-Object { $_ -in @("CRITICAL", "HIGH", "MEDIUM", "LOW", "UNSPECIFIED") } | Select-Object -First 1
        if ($tag) { return "$tag" }

        $score = $RuleProperties.PSObject.Properties["security-severity"]?.Value
        if ($score) {
            $value = [double]::Parse("$score", [cultureinfo]::InvariantCulture)
            if ($value -ge 9.0) { return "CRITICAL" }
            if ($value -ge 7.0) { return "HIGH" }
            if ($value -ge 4.0) { return "MEDIUM" }
            return "LOW"
        }
    }
    return "UNSPECIFIED"
}

function Get-Prop {
    # Returns a property value, or $null when the object or the property is missing.
    # SARIF allows "rules" and "results" to be absent or null on a clean image, and under
    # Set-StrictMode a direct access such as $run.results throws when the key is not there.
    param($Object, [string] $Name)

    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-ImageCounts {
    param([System.IO.FileInfo] $File)

    $image = $File.Name -replace '^sarif-', '' -replace '\.output\.json$', ''
    $counts = [ordered]@{ Image = $image; CRITICAL = 0; HIGH = 0; MEDIUM = 0; LOW = 0; UNSPECIFIED = 0; Error = $null }

    try {
        $sarif = Get-Content -LiteralPath $File.FullName -Raw | ConvertFrom-Json
        foreach ($run in @(Get-Prop $sarif "runs")) {
            if ($null -eq $run) { continue }
            $rules = @{}
            $driver = Get-Prop (Get-Prop $run "tool") "driver"
            foreach ($rule in @(Get-Prop $driver "rules")) {
                $id = Get-Prop $rule "id"
                if ($id) { $rules["$id"] = $rule }
            }
            foreach ($result in @(Get-Prop $run "results")) {
                if ($null -eq $result) { continue }
                $rule = $null
                $ruleId = Get-Prop $result "ruleId"
                if ($ruleId) { $rule = $rules["$ruleId"] }
                $severity = Get-Severity -RuleProperties (Get-Prop $rule "properties")
                $counts[$severity]++
            }
        }
    }
    catch {
        $counts.Error = "could not read SARIF: $($_.Exception.Message)"
    }

    return [pscustomobject] $counts
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add($Marker)
$lines.Add("## Docker Scout: vulnerabilities in images built from this PR")
$lines.Add("")

$files = @(Get-ChildItem -Path (Join-Path $SarifDir "sarif-*.output.json") -File -ErrorAction SilentlyContinue | Sort-Object Name)
$rows = @($files | ForEach-Object { Get-ImageCounts -File $_ })

$found = @($rows | ForEach-Object { $_.Image })
$missing = @($ExpectedImages | ForEach-Object { "$_".Trim() } | Where-Object { $_ -and ($_ -notin $found) } | Sort-Object -Unique)
foreach ($image in $missing) {
    $rows += [pscustomobject]@{
        Image = $image; CRITICAL = 0; HIGH = 0; MEDIUM = 0; LOW = 0; UNSPECIFIED = 0
        Error = "not analyzed: no SARIF file, the build or Scout step failed or was skipped"
    }
}
$rows = @($rows | Sort-Object Image)

if ($rows.Count -eq 0) {
    $lines.Add("No Scout SARIF files were found in ``$SarifDir``; see [this run]($RunUrl) for the image analysis jobs.")
    Write-Host "No SARIF files found in $SarifDir"
}
else {
    $hasUnspecified = @($rows | Where-Object { $_.UNSPECIFIED -gt 0 }).Count -gt 0

    $header = "| Image | Critical | High | Medium | Low |"
    $align  = "| --- | ---: | ---: | ---: | ---: |"
    if ($hasUnspecified) { $header += " Unspecified |"; $align += " ---: |" }
    $lines.Add($header)
    $lines.Add($align)

    foreach ($row in $rows) {
        if ($row.Error) {
            $lines.Add("| $($row.Image) | $($row.Error.Replace('|', '\|')) |" + (" |" * $(if ($hasUnspecified) { 4 } else { 3 })))
            continue
        }
        $cells = "| $($row.Image) | $($row.CRITICAL) | $($row.HIGH) | $($row.MEDIUM) | $($row.LOW) |"
        if ($hasUnspecified) { $cells += " $($row.UNSPECIFIED) |" }
        $lines.Add($cells)
    }

    $valid = @($rows | Where-Object { -not $_.Error })
    if ($valid.Count -gt 1) {
        $totals = "| **Total** |" + (($severities | ForEach-Object { $s = $_; " **$(($valid | Measure-Object -Property $s -Sum).Sum)** |" }) -join "")
        if ($hasUnspecified) { $totals += " **$(($valid | Measure-Object -Property UNSPECIFIED -Sum).Sum)** |" }
        $lines.Add($totals)
    }

    $lines.Add("")
    if ($missing.Count -gt 0) {
        $missingList = ($missing | ForEach-Object { '`' + $_ + '`' }) -join ', '
        $lines.Add("**$($valid.Count) of $($rows.Count) images analyzed.** Not analyzed: $missingList. Check the failed jobs in [this run]($RunUrl).")
        $lines.Add("")
        Write-Warning "Not analyzed: $($missing -join ', ')"
    }
    $lines.Add("Counts from ``docker scout cves`` on each image built in [this run]($RunUrl). Full tables are in each job's summary. Alerts for ``main`` are in the Security tab.")
    Write-Host ("Images: {0}; totals critical {1}, high {2}, medium {3}, low {4}" -f $valid.Count,
        ($valid | Measure-Object -Property CRITICAL -Sum).Sum, ($valid | Measure-Object -Property HIGH -Sum).Sum,
        ($valid | Measure-Object -Property MEDIUM -Sum).Sum, ($valid | Measure-Object -Property LOW -Sum).Sum)
}

$lines.Add("")
$text = ($lines -join "`n") + "`n"
Set-Content -LiteralPath $OutputPath -Value $text -Encoding utf8 -NoNewline
if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $text -Encoding utf8
}

exit 0
