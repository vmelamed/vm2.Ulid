#!/usr/bin/env pwsh
<#!
Self-Test.ps1 - Lightweight self-test harness for Common.psm1 utilities.
#>
[CmdletBinding()] param([switch]$Verbose)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $PSCommandPath
Import-Module -Force "$ScriptDir/Common.psm1"

$tests = 0
$fail = 0
function Assert-True($expr,[string]$name) { $script:tests++ ; if (-not $expr) { $script:fail++; Write-Host "[FAIL] $name" -ForegroundColor Red; exit 1 } elseif ($Verbose) { Write-Host "[OK] $name" -ForegroundColor DarkGreen } }
function Assert-Eq($expected,$actual,[string]$name) { $script:tests++ ; if ($expected -ne $actual) { $script:fail++; Write-Host "[FAIL] $name (expected=$expected actual=$actual)" -ForegroundColor Red; exit 1 } elseif ($Verbose) { Write-Host "[OK] $name" -ForegroundColor DarkGreen } }

Assert-Eq 'abc' (Convert-ToLower 'AbC') 'lower'
Assert-Eq 'ABC' (Convert-ToUpper 'aBc') 'upper'
Assert-True (Test-Positive '7') 'positive'
Assert-True (Test-NonNegative '0') 'nonneg'
Assert-True (Test-Negative '-5') 'negative'
Assert-True (Test-Decimal '3.14') 'decimal'
Assert-True (Test-Integer '42') 'integer'

$yn = Read-YesNo -Prompt 'Proceed?' -Default y
Assert-Eq 'y' $yn 'yesno default'

$sel = Select-FromList -Prompt 'Pick' -Options @('One','Two','Three') -Default 2
Assert-Eq 2 $sel 'select default'

Write-Host "All PowerShell self-tests passed: $tests" -ForegroundColor Green
