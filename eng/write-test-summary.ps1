# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

<#
.SYNOPSIS
    Writes a markdown summary of test results to the GitHub Actions step summary.

.DESCRIPTION
    Reads one or more test result files, either Visual Studio TRX (dotnet test
    --logger trx) or JUnit XML (Bruno --reporter-junit), and appends a markdown
    table with totals per file to $env:GITHUB_STEP_SUMMARY. Failed tests are
    listed in a collapsed section.

    When GITHUB_STEP_SUMMARY is not set (local use) the markdown is written to
    standard output. The script always exits 0: the test step itself decides
    whether the job fails.

.PARAMETER Path
    One or more paths or wildcard patterns of result files. A "**/" segment
    matches any directory depth, e.g. "artifacts/**/*.trx".

.PARAMETER Title
    Heading for the summary section.

.PARAMETER OutputPath
    File to append to. Defaults to $env:GITHUB_STEP_SUMMARY; prints to stdout when empty.

.PARAMETER MaxFailures
    Maximum number of failed tests listed. Default 50.

.EXAMPLE
    ./eng/write-test-summary.ps1 -Path "Application/*.UnitTests/*.csproj.trx" -Title "Unit tests"

.EXAMPLE
    ./eng/write-test-summary.ps1 -Path "test-xml-results/report-*.xml" -Title "V2 E2E totals"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]
    $Path,

    [string]
    $Title = "Test results",

    [string]
    $OutputPath = $env:GITHUB_STEP_SUMMARY,

    [ValidateRange(0, 1000)]
    [int]
    $MaxFailures = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Expands the -Path patterns; "**/" recurses from the directory before it.
function Resolve-ResultFile {
    param([string[]] $Patterns)

    foreach ($pattern in $Patterns) {
        if ($pattern -match '^(.*?)\*\*[\\/](.+)$') {
            $basePattern = if ($Matches[1]) { $Matches[1] } else { "." }
            $leaf = $Matches[2]
            foreach ($base in @(Get-Item -Path $basePattern -ErrorAction SilentlyContinue | Where-Object { $_.PSIsContainer })) {
                Get-ChildItem -LiteralPath $base.FullName -Recurse -File -ErrorAction SilentlyContinue |
                    Where-Object { $_.FullName.Substring($base.FullName.Length).TrimStart('\', '/').Replace('\', '/') -like $leaf }
            }
        }
        else {
            Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue
        }
    }
}

function Format-Duration {
    param([double] $Seconds)

    $span = [TimeSpan]::FromSeconds([Math]::Round($Seconds))
    if ($span.TotalHours -ge 1) {
        return $span.ToString("h\:mm\:ss")
    }
    return $span.ToString("m\:ss")
}

function Format-Failure {
    param([string] $Name, [string] $Message)

    $line = "- ``$Name``"
    if ($Message) {
        $first = ($Message -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
        if ($first) {
            $first = $first.Trim()
            if ($first.Length -gt 200) { $first = $first.Substring(0, 200) + "..." }
            $line += ": " + $first.Replace("|", "\|")
        }
    }
    return $line
}

function New-ResultRow {
    param([string] $Name, [int] $Total, [int] $Passed, [int] $Failed, [int] $Skipped, [double] $Seconds, [string[]] $Failures)

    return [pscustomobject]@{
        Name     = $Name
        Total    = $Total
        Passed   = $Passed
        Failed   = $Failed
        Skipped  = $Skipped
        Seconds  = $Seconds
        Failures = @($Failures | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
}

function ConvertFrom-TrxReport {
    param([xml] $Xml, [string] $Name)

    $ns = New-Object System.Xml.XmlNamespaceManager($Xml.NameTable)
    $ns.AddNamespace("t", $Xml.DocumentElement.NamespaceURI)

    $counters = $Xml.SelectSingleNode("//t:ResultSummary/t:Counters", $ns)
    $total    = [int] $counters.GetAttribute("total")
    $executed = [int] $counters.GetAttribute("executed")
    $passed   = [int] $counters.GetAttribute("passed")
    $failed   = [int] $counters.GetAttribute("failed")
    $skipped  = [Math]::Max(0, $total - $executed)

    $seconds = 0.0
    $times = $Xml.SelectSingleNode("//t:Times", $ns)
    if ($times) {
        $start  = [datetime] $times.GetAttribute("start")
        $finish = [datetime] $times.GetAttribute("finish")
        $seconds = ($finish - $start).TotalSeconds
    }

    $failures = foreach ($result in $Xml.SelectNodes("//t:UnitTestResult", $ns)) {
        $outcome = $result.GetAttribute("outcome")
        if ($outcome -eq "Passed" -or $outcome -eq "NotExecuted") { continue }
        $message = $result.SelectSingleNode("t:Output/t:ErrorInfo/t:Message", $ns)
        Format-Failure -Name $result.GetAttribute("testName") -Message $(if ($message) { $message.InnerText } else { $outcome })
    }

    return New-ResultRow -Name $Name -Total $total -Passed $passed -Failed $failed -Skipped $skipped -Seconds $seconds -Failures $failures
}

function ConvertFrom-JUnitReport {
    param([xml] $Xml, [string] $Name)

    $total = 0; $failed = 0; $skipped = 0; $seconds = 0.0
    foreach ($suite in $Xml.SelectNodes("//testsuite")) {
        $total   += [int] $suite.GetAttribute("tests")
        $failed  += [int] $suite.GetAttribute("failures") + [int] $suite.GetAttribute("errors")
        $skipped += [int] $suite.GetAttribute("skipped")
        $time = $suite.GetAttribute("time")
        if ($time) { $seconds += [double]::Parse($time, [cultureinfo]::InvariantCulture) }
    }
    $passed = [Math]::Max(0, $total - $failed - $skipped)

    $failures = foreach ($case in $Xml.SelectNodes("//testcase[failure or error]")) {
        $detail = $case.SelectSingleNode("failure | error")
        $message = $detail.GetAttribute("message")
        if (-not $message) { $message = $detail.InnerText }
        $caseName = $case.GetAttribute("name")
        $className = $case.GetAttribute("classname")
        if ($className) { $caseName = "$className :: $caseName" }
        Format-Failure -Name $caseName -Message $message
    }

    return New-ResultRow -Name $Name -Total $total -Passed $passed -Failed $failed -Skipped $skipped -Seconds $seconds -Failures $failures
}

function ConvertFrom-ResultFile {
    param([System.IO.FileInfo] $File)

    try {
        [xml] $xml = Get-Content -LiteralPath $File.FullName -Raw
        switch ($xml.DocumentElement.LocalName) {
            "TestRun"    { return ConvertFrom-TrxReport -Xml $xml -Name $File.Name }
            "testsuites" { return ConvertFrom-JUnitReport -Xml $xml -Name $File.Name }
            "testsuite"  { return ConvertFrom-JUnitReport -Xml $xml -Name $File.Name }
            default      { return New-ResultRow -Name $File.Name -Failures @("- unsupported root element ``$($xml.DocumentElement.LocalName)``") }
        }
    }
    catch {
        return New-ResultRow -Name $File.Name -Failures @("- could not read file: $($_.Exception.Message)")
    }
}

$lines = [System.Collections.Generic.List[string]]::new()

try {
    $files = @(Resolve-ResultFile -Patterns $Path | Sort-Object FullName -Unique)

    if ($files.Count -eq 0) {
        $lines.Add("## $Title")
        $lines.Add("")
        $lines.Add("No result files found for: $($Path -join ', ')")
        Write-Host "$Title`: no result files found for $($Path -join ', ')"
    }
    else {
        $rows = @($files | ForEach-Object { ConvertFrom-ResultFile -File $_ })

        $sumTotal   = ($rows | Measure-Object -Property Total -Sum).Sum
        $sumPassed  = ($rows | Measure-Object -Property Passed -Sum).Sum
        $sumFailed  = ($rows | Measure-Object -Property Failed -Sum).Sum
        $sumSkipped = ($rows | Measure-Object -Property Skipped -Sum).Sum
        $sumSeconds = ($rows | Measure-Object -Property Seconds -Sum).Sum
        $allFailures = @($rows | ForEach-Object { $_.Failures })

        $status = if ($sumFailed -gt 0 -or $allFailures.Count -gt 0) { "❌" } else { "✅" }

        $lines.Add("## $status $Title")
        $lines.Add("")
        $lines.Add("| File | Total | Passed | Failed | Skipped | Duration |")
        $lines.Add("| --- | ---: | ---: | ---: | ---: | ---: |")
        foreach ($row in $rows) {
            $lines.Add("| $($row.Name) | $($row.Total) | $($row.Passed) | $($row.Failed) | $($row.Skipped) | $(Format-Duration $row.Seconds) |")
        }
        if ($rows.Count -gt 1) {
            $lines.Add("| **Total** | **$sumTotal** | **$sumPassed** | **$sumFailed** | **$sumSkipped** | **$(Format-Duration $sumSeconds)** |")
        }

        if ($allFailures.Count -gt 0) {
            $shown = @($allFailures | Select-Object -First $MaxFailures)
            $lines.Add("")
            $lines.Add("<details><summary>Failures ($($allFailures.Count))</summary>")
            $lines.Add("")
            $shown | ForEach-Object { $lines.Add($_) }
            if ($allFailures.Count -gt $shown.Count) {
                $lines.Add("- ... and $($allFailures.Count - $shown.Count) more")
            }
            $lines.Add("")
            $lines.Add("</details>")
        }

        Write-Host "$Title`: $sumTotal total, $sumPassed passed, $sumFailed failed, $sumSkipped skipped ($($files.Count) file(s))"
    }
}
catch {
    $lines.Add("## $Title")
    $lines.Add("")
    $lines.Add("Could not build the test summary: $($_.Exception.Message)")
    Write-Host "Could not build the test summary: $($_.Exception.Message)"
}

$lines.Add("")
$text = ($lines -join "`n") + "`n"

if ($OutputPath) {
    Add-Content -LiteralPath $OutputPath -Value $text -Encoding utf8
}
else {
    Write-Output $text
}

exit 0
