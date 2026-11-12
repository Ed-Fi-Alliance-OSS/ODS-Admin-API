# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

<#
.SYNOPSIS
    Groups test results by area: .NET TRX files by feature namespace, Bruno JUnit files by resource folder.

.DESCRIPTION
    Writes one markdown table per .NET test project (rows = the first two namespace segments
    below the project namespace, e.g. Features.Vendors or Infrastructure.Database) and one table
    per E2E version (rows = the folder below v1/, v2/ or v3/, summed over all matrix runs).
    The markdown goes to -OutputPath and is appended to $env:GITHUB_STEP_SUMMARY when set.
    Always exits 0.

.PARAMETER Path
    Folder searched recursively for *.trx and report-*.xml files.

.PARAMETER OutputPath
    Markdown file to write.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $Path,
    [Parameter(Mandatory = $true)] [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Adds one result to $Groups[$Section][$Group], creating the counters on first use.
function Add-Count {
    param([hashtable] $Groups, [string] $Section, [string] $Group, [string] $Outcome)

    if (-not $Groups.ContainsKey($Section)) { $Groups[$Section] = [ordered]@{} }
    if (-not $Groups[$Section].Contains($Group)) { $Groups[$Section][$Group] = @{ Total = 0; Passed = 0; Failed = 0; Skipped = 0 } }
    $Groups[$Section][$Group].Total++
    $Groups[$Section][$Group][$Outcome]++
}

$dotnet = @{}   # section = project, group = feature namespace
$e2e    = @{}   # section = version, group = resource folder
$notes  = [System.Collections.Generic.List[string]]::new()

foreach ($file in Get-ChildItem -Path $Path -Recurse -File -Include *.trx, report-*.xml -ErrorAction SilentlyContinue) {
    try {
        [xml] $xml = Get-Content -LiteralPath $file.FullName -Raw
        switch ($xml.DocumentElement.LocalName) {
            "TestRun" {
                $project = $file.Name -replace '\.csproj\.trx$', '' -replace '\.trx$', ''
                $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
                $ns.AddNamespace("t", $xml.DocumentElement.NamespaceURI)
                $classByTestId = @{}
                foreach ($definition in $xml.SelectNodes("//t:TestDefinitions/t:UnitTest", $ns)) {
                    $method = $definition.SelectSingleNode("t:TestMethod", $ns)
                    if ($method) { $classByTestId[$definition.GetAttribute("id")] = ($method.GetAttribute("className") -split ",")[0].Trim() }
                }
                foreach ($result in $xml.SelectNodes("//t:UnitTestResult", $ns)) {
                    $className = $classByTestId[$result.GetAttribute("testId")]
                    # Namespace below the project namespace, without the class: first two segments are the feature.
                    $relative = if ($className -and $className.StartsWith("$project.")) { $className.Substring($project.Length + 1) } else { "$className" }
                    $segments = @($relative -split '\.')
                    $feature = @($segments | Select-Object -SkipLast 1 | Select-Object -First 2) -join "."
                    if (-not $feature) { $feature = "(root)" }
                    $outcome = switch ($result.GetAttribute("outcome")) { "Passed" { "Passed" } "NotExecuted" { "Skipped" } default { "Failed" } }
                    Add-Count -Groups $dotnet -Section $project -Group $feature -Outcome $outcome
                }
            }
            { $_ -in "testsuites", "testsuite" } {
                foreach ($suite in $xml.SelectNodes("//testsuite")) {
                    # Bruno: file="v2/ClaimSets/Sorting and Filtering/GET - X.bru"; version = v2, resource = ClaimSets.
                    $parts = @(($suite.GetAttribute("file") -replace '\\', '/') -split '/')
                    $version  = if ($parts.Count -gt 2) { $parts[0].ToUpperInvariant() } else { "E2E" }
                    $resource = if ($parts.Count -gt 2) { $parts[1] } else { $suite.GetAttribute("name") }
                    foreach ($case in $suite.SelectNodes("testcase")) {
                        $outcome = if ($case.SelectSingleNode("failure | error")) { "Failed" } elseif ($case.SelectSingleNode("skipped")) { "Skipped" } else { "Passed" }
                        Add-Count -Groups $e2e -Section $version -Group $resource -Outcome $outcome
                    }
                }
            }
            default { $notes.Add("Skipped ``$($file.Name)``: unsupported root element ``$($xml.DocumentElement.LocalName)``") }
        }
    }
    catch {
        $notes.Add("Could not read ``$($file.Name)``: $($_.Exception.Message)")
    }
}

# Renders one collapsed table per section; sections and groups sorted by name, failing rows marked.
function Write-Section {
    param([System.Collections.Generic.List[string]] $Lines, [string] $Title, [string] $GroupHeader, [hashtable] $Groups)

    $Lines.Add("### $Title")
    if ($Groups.Count -eq 0) { $Lines.Add(""); $Lines.Add("No result files found."); $Lines.Add(""); return }
    foreach ($section in ($Groups.Keys | Sort-Object)) {
        $rows = $Groups[$section]
        $total = ($rows.Values | Measure-Object -Property Total -Sum).Sum
        $passed = ($rows.Values | Measure-Object -Property Passed -Sum).Sum
        $failed = ($rows.Values | Measure-Object -Property Failed -Sum).Sum
        $skipped = ($rows.Values | Measure-Object -Property Skipped -Sum).Sum
        $status = if ($failed -gt 0) { "❌" } else { "✅" }
        $Lines.Add("")
        $summary = "$status ${section}: $total tests, $passed passed"
        if ($failed -gt 0) { $summary += ", $failed failed" }
        $summary += ", $skipped skipped"
        $Lines.Add("<details><summary>$summary</summary>")
        $Lines.Add("")
        # The Failed column appears only when something in this table failed.
        $failedHeader = if ($failed -gt 0) { " Failed |" } else { "" }
        $failedAlign  = if ($failed -gt 0) { " ---: |" } else { "" }
        $Lines.Add("| $GroupHeader | Tests | Passed |$failedHeader Skipped |")
        $Lines.Add("| --- | ---: | ---: |$failedAlign ---: |")
        foreach ($group in ($rows.Keys | Sort-Object)) {
            $r = $rows[$group]
            $mark = if ($r.Failed -gt 0) { "❌ " } else { "" }
            $failedCell = if ($failed -gt 0) { " $($r.Failed) |" } else { "" }
            $Lines.Add("| $mark$group | $($r.Total) | $($r.Passed) |$failedCell $($r.Skipped) |")
        }
        $Lines.Add("")
        $Lines.Add("</details>")
    }
    $Lines.Add("")
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("<!-- devops-162-test-groups -->")
$lines.Add("## Test results by area")
$lines.Add("")
Write-Section -Lines $lines -Title ".NET tests by feature namespace" -GroupHeader "Feature" -Groups $dotnet
Write-Section -Lines $lines -Title "E2E tests by resource folder (all database and tenant runs)" -GroupHeader "Resource" -Groups $e2e
foreach ($note in $notes) { $lines.Add("- $note") }

$text = ($lines -join "`n") + "`n"
Set-Content -LiteralPath $OutputPath -Value $text -Encoding utf8 -NoNewline
if ($env:GITHUB_STEP_SUMMARY) { Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $text -Encoding utf8 }
Write-Host "Projects: $($dotnet.Count); E2E versions: $($e2e.Count); notes: $($notes.Count)"
exit 0
