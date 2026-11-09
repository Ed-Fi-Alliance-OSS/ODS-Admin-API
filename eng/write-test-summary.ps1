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
    table with totals per file to $env:GITHUB_STEP_SUMMARY. Each file gets a
    collapsed per-suite table (TRX: test class namespace, JUnit: request folder)
    and failed tests are listed in a collapsed section.

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

.PARAMETER HeadingLevel
    Markdown heading level of the title (1-6). Default 2.

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
    $MaxFailures = 50,

    [ValidateRange(1, 6)]
    [int]
    $HeadingLevel = 2
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

    if ($Seconds -lt 60) {
        return $Seconds.ToString("0.0", [cultureinfo]::InvariantCulture) + "s"
    }
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
    param([string] $Name, [int] $Total, [int] $Passed, [int] $Failed, [int] $Skipped, [double] $Seconds, [string[]] $Failures, [object[]] $Suites)

    return [pscustomobject]@{
        Name     = $Name
        Total    = $Total
        Passed   = $Passed
        Failed   = $Failed
        Skipped  = $Skipped
        Seconds  = $Seconds
        Failures = @($Failures | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        Suites   = @($Suites | Where-Object { $null -ne $_ })
    }
}

# Returns the longest common prefix of the names, cut at a separator boundary.
function Get-CommonPrefix {
    param([string[]] $Names, [string] $Separator)

    $distinct = @($Names | Where-Object { $_ } | Sort-Object -Unique)
    if ($distinct.Count -lt 2) { return "" }

    $parts = $distinct[0].Split($Separator)
    $common = @()
    for ($i = 0; $i -lt $parts.Count; $i++) {
        $candidate = ($parts[0..$i] -join $Separator)
        # A name equal to the candidate is the root itself (e.g. a test class directly in the project namespace).
        if (@($distinct | Where-Object { $_ -ne $candidate -and -not $_.StartsWith($candidate + $Separator) }).Count -gt 0) { break }
        $common = $parts[0..$i]
    }
    if ($common.Count -eq 0) { return "" }
    return ($common -join $Separator) + $Separator
}

# Builds one row per suite from a list of (Suite, Outcome, Seconds) samples, stripping the shared prefix.
function Group-Suites {
    param([object[]] $Samples, [string] $Separator)

    $samples = @($Samples | Where-Object { $null -ne $_ })
    if ($samples.Count -eq 0) { return @() }

    $prefix = Get-CommonPrefix -Names @($samples | ForEach-Object { $_.Suite }) -Separator $Separator

    $rows = foreach ($group in ($samples | Group-Object -Property Suite | Sort-Object -Property Name)) {
        $name = $group.Name
        if ($prefix -and $name.StartsWith($prefix)) {
            $name = $name.Substring($prefix.Length)
        }
        elseif ($prefix -and ($name + $Separator) -eq $prefix) {
            $name = ""
        }
        if (-not $name) { $name = "(root)" }
        [pscustomobject]@{
            Name    = $name
            Total   = $group.Count
            Passed  = @($group.Group | Where-Object { $_.Outcome -eq "Passed" }).Count
            Failed  = @($group.Group | Where-Object { $_.Outcome -eq "Failed" }).Count
            Skipped = @($group.Group | Where-Object { $_.Outcome -eq "Skipped" }).Count
            Seconds = ($group.Group | Measure-Object -Property Seconds -Sum).Sum
        }
    }
    return @($rows)
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

    # Test class per test id, so results can be grouped by namespace (the "suite").
    $classByTestId = @{}
    foreach ($definition in $Xml.SelectNodes("//t:TestDefinitions/t:UnitTest", $ns)) {
        $method = $definition.SelectSingleNode("t:TestMethod", $ns)
        if ($method) {
            # className may carry the assembly after a comma: "Ns.Class, Assembly, Version=..."
            $classByTestId[$definition.GetAttribute("id")] = ($method.GetAttribute("className") -split ",")[0].Trim()
        }
    }

    $samples = [System.Collections.Generic.List[object]]::new()
    $failures = foreach ($result in $Xml.SelectNodes("//t:UnitTestResult", $ns)) {
        $outcome = $result.GetAttribute("outcome")
        $className = $classByTestId[$result.GetAttribute("testId")]
        $suite = if ($className -and $className.Contains(".")) { $className.Substring(0, $className.LastIndexOf(".")) } elseif ($className) { $className } else { "(unknown)" }
        $resultSeconds = 0.0
        $duration = $result.GetAttribute("duration")
        if ($duration) { $resultSeconds = ([TimeSpan]::Parse($duration, [cultureinfo]::InvariantCulture)).TotalSeconds }
        $samples.Add([pscustomobject]@{
            Suite   = $suite
            Outcome = switch ($outcome) { "Passed" { "Passed" } "NotExecuted" { "Skipped" } default { "Failed" } }
            Seconds = $resultSeconds
        })

        if ($outcome -eq "Passed" -or $outcome -eq "NotExecuted") { continue }
        $message = $result.SelectSingleNode("t:Output/t:ErrorInfo/t:Message", $ns)
        Format-Failure -Name $result.GetAttribute("testName") -Message $(if ($message) { $message.InnerText } else { $outcome })
    }

    $suites = Group-Suites -Samples $samples.ToArray() -Separator "."

    return New-ResultRow -Name $Name -Total $total -Passed $passed -Failed $failed -Skipped $skipped -Seconds $seconds -Failures $failures -Suites $suites
}

function ConvertFrom-JUnitReport {
    param([xml] $Xml, [string] $Name)

    $total = 0; $failed = 0; $skipped = 0; $seconds = 0.0
    $samples = [System.Collections.Generic.List[object]]::new()
    foreach ($suite in $Xml.SelectNodes("//testsuite")) {
        $suiteTotal   = [int] $suite.GetAttribute("tests")
        $suiteFailed  = [int] $suite.GetAttribute("failures") + [int] $suite.GetAttribute("errors")
        $suiteSkipped = [int] $suite.GetAttribute("skipped")
        $suiteSeconds = 0.0
        $time = $suite.GetAttribute("time")
        if ($time) { $suiteSeconds = [double]::Parse($time, [cultureinfo]::InvariantCulture) }

        $total += $suiteTotal; $failed += $suiteFailed; $skipped += $suiteSkipped; $seconds += $suiteSeconds

        # Bruno writes one <testsuite> per request with file="folder/request.bru"; the folder is the suite.
        $file = $suite.GetAttribute("file")
        $suiteName = if ($file -and $file.Contains("/")) { $file.Substring(0, $file.LastIndexOf("/")) } else { $suite.GetAttribute("name") }
        if (-not $suiteName) { $suiteName = "(unknown)" }

        # Expand the suite counters into one sample per test; time is attributed to the first sample.
        for ($i = 0; $i -lt $suiteTotal; $i++) {
            $outcome = if ($i -lt $suiteFailed) { "Failed" } elseif ($i -lt $suiteFailed + $suiteSkipped) { "Skipped" } else { "Passed" }
            $samples.Add([pscustomobject]@{ Suite = $suiteName; Outcome = $outcome; Seconds = $(if ($i -eq 0) { $suiteSeconds } else { 0.0 }) })
        }
    }
    $passed = [Math]::Max(0, $total - $failed - $skipped)
    $suites = Group-Suites -Samples $samples.ToArray() -Separator "/"

    $failures = foreach ($case in $Xml.SelectNodes("//testcase[failure or error]")) {
        $detail = $case.SelectSingleNode("failure | error")
        $message = $detail.GetAttribute("message")
        if (-not $message) { $message = $detail.InnerText }
        $caseName = $case.GetAttribute("name")
        $className = $case.GetAttribute("classname")
        if ($className) { $caseName = "$className :: $caseName" }
        Format-Failure -Name $caseName -Message $message
    }

    return New-ResultRow -Name $Name -Total $total -Passed $passed -Failed $failed -Skipped $skipped -Seconds $seconds -Failures $failures -Suites $suites
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
$heading = "#" * $HeadingLevel

try {
    $files = @(Resolve-ResultFile -Patterns $Path | Sort-Object FullName -Unique)

    if ($files.Count -eq 0) {
        $lines.Add("$heading $Title")
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

        $lines.Add("$heading $status $Title")
        $lines.Add("")
        $lines.Add("| File | Total | Passed | Failed | Skipped | Duration |")
        $lines.Add("| --- | ---: | ---: | ---: | ---: | ---: |")
        foreach ($row in $rows) {
            $lines.Add("| $($row.Name) | $($row.Total) | $($row.Passed) | $($row.Failed) | $($row.Skipped) | $(Format-Duration $row.Seconds) |")
        }
        if ($rows.Count -gt 1) {
            $lines.Add("| **Total** | **$sumTotal** | **$sumPassed** | **$sumFailed** | **$sumSkipped** | **$(Format-Duration $sumSeconds)** |")
        }

        foreach ($row in $rows) {
            if ($row.Suites.Count -eq 0) { continue }
            $suiteStatus = if (@($row.Suites | Where-Object { $_.Failed -gt 0 }).Count -gt 0) { "❌" } else { "✅" }
            $lines.Add("")
            $lines.Add("<details><summary>$suiteStatus $($row.Name) by suite ($($row.Suites.Count))</summary>")
            $lines.Add("")
            $lines.Add("| Suite | Total | Passed | Failed | Skipped | Duration |")
            $lines.Add("| --- | ---: | ---: | ---: | ---: | ---: |")
            foreach ($suite in $row.Suites) {
                $marker = if ($suite.Failed -gt 0) { "❌ " } else { "" }
                $lines.Add("| $marker$($suite.Name.Replace('|', '\|')) | $($suite.Total) | $($suite.Passed) | $($suite.Failed) | $($suite.Skipped) | $(Format-Duration $suite.Seconds) |")
            }
            $lines.Add("")
            $lines.Add("</details>")
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
    $lines.Add("$heading $Title")
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
