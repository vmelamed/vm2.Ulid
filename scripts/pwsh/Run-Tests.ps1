#!/usr/bin/env pwsh
<#!
Run-Tests.ps1 - PowerShell port of scripts/bash/run-tests.sh
Performs: test execution, coverage generation (text + html), threshold enforcement.
Requires: .NET SDK, reportgenerator global tool (auto installed to a temp tools folder).
#>
[CmdletBinding()] param(
    [string]$TestProject = './test/UlidType.Tests/UlidType.Tests.csproj',
    [int]$CoverageThreshold = 80,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$Artifacts = './TestArtifacts',
    [switch]$Quiet,
    [switch]$DryRun,
    [switch]$Trace,
    [switch]$Debug
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $PSCommandPath
Import-Module -Force "$ScriptDir/Common.psm1"

Set-CommonOptions -Debug:$Debug -Trace:$Trace -DryRun:$DryRun -Quiet:$Quiet

# Resolve full paths
$TestProject = (Resolve-Path -LiteralPath $TestProject).Path
$Artifacts = (Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force -Path $Artifacts)).Path
$CoverageRoot = Join-Path $Artifacts 'CoverageResults'
$ResultsDir = Join-Path $Artifacts 'Results'
$CoverageRaw = Join-Path $CoverageRoot 'coverage'
$CoverageFile = Join-Path $CoverageRaw 'coverage.cobertura.xml'
$ReportDir = Join-Path $CoverageRoot 'coverage_reports'
$SummaryFile = Join-Path $ReportDir 'Summary.txt'
$ExportTextDir = Join-Path $Artifacts 'coverage/text'
$ExportHtmlDir = Join-Path $Artifacts 'coverage/html'
$BaseName = [IO.Path]::GetFileNameWithoutExtension($TestProject)
$ExportSummary = Join-Path $ExportTextDir "$BaseName-TextSummary.txt"

# Clean or rotate existing artifacts directory if not empty
if (Test-Path $Artifacts -PathType Container) {
    $hasContent = (Get-ChildItem -Path $Artifacts -Recurse -Force | Select-Object -First 1)
    if ($hasContent -and -not $Quiet) {
        $choice = Select-FromList -Prompt "Artifacts directory '$Artifacts' exists. Action?" -Options @(
            'Delete and continue', 'Rename with UTC timestamp and continue', 'Exit script'
        ) -Default 1
        switch ($choice) {
            1 { Invoke-Exec { Remove-Item -Recurse -Force $Artifacts; New-Item -ItemType Directory -Path $Artifacts | Out-Null } -Description 'Remove old artifacts' }
            2 { $new = "$Artifacts" + '_' + (Get-Date -Format 'yyyyMMddTHHmmss'); Rename-Item $Artifacts $new; New-Item -ItemType Directory -Path $Artifacts | Out-Null }
            3 { Write-Host 'Exiting.'; exit 0 }
            default { throw 'Invalid selection.' }
        }
    }
}

# Create required directories
foreach ($d in @($CoverageRoot, $ResultsDir, $CoverageRaw, $ExportTextDir)) { if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d | Out-Null } }

Write-Trace "Running tests: $TestProject ($Configuration)"
Invoke-Exec -Description 'dotnet test' -Script {
    dotnet test $TestProject --configuration $Configuration -- \
    --results-directory $ResultsDir \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output $CoverageFile
}

if (-not $DryRun) {
    if (-not (Test-Path $CoverageFile -PathType Leaf)) { throw "Coverage file not found: $CoverageFile" }
    if ((Get-Item $CoverageFile).Length -lt 50) { throw 'Coverage file appears too small or empty.' }
}

# Install reportgenerator locally (temp tools folder under script dir)
$ToolsPath = Join-Path $ScriptDir '.tools'
if (-not (Test-Path $ToolsPath)) { New-Item -ItemType Directory -Path $ToolsPath | Out-Null }

$reportExe = Join-Path $ToolsPath 'reportgenerator'
if (-not (Test-Path $reportExe)) {
    Write-Trace 'Installing reportgenerator tool...'
    Invoke-Exec -Description 'dotnet tool install reportgenerator' -Script { dotnet tool install dotnet-reportgenerator-globaltool --tool-path $ToolsPath --version 5.* }
}

Invoke-Exec -Description 'Generate coverage reports' -Script {
    & $reportExe -reports:$CoverageFile -targetdir:$ReportDir -reporttypes:TextSummary, Html
}

if (-not (Test-Path $SummaryFile)) { throw "Coverage summary not found: $SummaryFile" }

Move-Item -Force $SummaryFile $ExportSummary
# Move HTML bundle (leave folder naming consistent with bash version)
if (Test-Path $ReportDir) {
    if (Test-Path $ExportHtmlDir) { Remove-Item -Recurse -Force $ExportHtmlDir }
    Move-Item -Force $ReportDir $ExportHtmlDir
}

# Extract coverage percent
defaultProgressPreference = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'
$summaryContent = Get-Content -Raw $ExportSummary
$ProgressPreference = $defaultProgressPreference
$match = [regex]::Match($summaryContent, 'Method coverage: ([0-9]+(?:\.[0-9]+)?)%')
if (-not $match.Success) { throw 'Could not parse coverage percent.' }
$pct = [double]$match.Groups[1].Value
Write-Host ("Coverage: {0}% (threshold: {1}%)" -f $pct, $CoverageThreshold)
if ($pct -lt $CoverageThreshold) { throw "Coverage $pct% below threshold $CoverageThreshold%" }

Write-Host 'Test run + coverage succeeded.' -ForegroundColor Green
