<#!
Common.psm1 - PowerShell module providing equivalents of selected bash utility functions.
Intended for local dev or CI on Windows runners.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Global-like state (mirroring bash script semantics)
$script:InitialDirectory = Get-Location
$script:Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'

$script:DebugEnabled   = $false
$script:TraceEnabled   = $false
$script:DryRun         = $false
$script:Quiet          = $false
$script:LastCommand    = $null

function Set-CommonOptions {
    param(
        [switch]$Debug,
        [switch]$Trace,
        [switch]$DryRun,
        [switch]$Quiet
    )
    if ($Debug) { $script:DebugEnabled = $true }
    if ($Trace) { $script:TraceEnabled = $true }
    if ($DryRun) { $script:DryRun = $true }
    if ($Quiet) { $script:Quiet = $true }
}

function Write-Trace {
    param([Parameter(ValueFromRemainingArguments)] [string[]]$Message)
    if ($script:TraceEnabled -or $script:DebugEnabled) {
        Write-Host "TRACE: $($Message -join ' ')" -ForegroundColor DarkCyan
    }
}

function Invoke-Exec {
    [CmdletBinding()] param(
        [Parameter(Mandatory)][scriptblock]$Script,
        [string]$Description = ''
    )
    if ($script:DryRun) {
        Write-Host "dry-run$ $Description" -ForegroundColor Yellow
        return
    }
    if ($Description) { Write-Trace $Description }
    & $Script
}

function Convert-ToLower { param([Parameter(Mandatory)][string]$Text) $Text.ToLowerInvariant() }
function Convert-ToUpper { param([Parameter(Mandatory)][string]$Text) $Text.ToUpperInvariant() }

function Test-Integer { param([Parameter(Mandatory)][string]$Value) return $Value -match '^[+-]?[0-9]+$' }
function Test-Positive { param([Parameter(Mandatory)][string]$Value) if ($Value -match '^[+]?[0-9]+$' -and $Value -notmatch '^[+]?0+$') { return $true } else { return $false } }
function Test-NonNegative { param([Parameter(Mandatory)][string]$Value) return $Value -match '^[+]?[0-9]+$' }
function Test-Negative { param([Parameter(Mandatory)][string]$Value) if ($Value -match '^-[0-9]+$' -and $Value -notmatch '^-0+$') { return $true } else { return $false } }
function Test-Decimal { param([Parameter(Mandatory)][string]$Value) return ($Value -match '^[+-]?[0-9]*(\.[0-9]+)?$') }

function Select-FromList {
    [CmdletBinding()] param(
        [Parameter(Mandatory)][string]$Prompt,
        [Parameter(Mandatory)][string[]]$Options,
        [int]$Default = 1
    )
    if ($script:Quiet) { return $Default }
    if ($Options.Count -lt 2) { throw "Need at least 2 options" }
    Write-Host $Prompt
    for ($i = 0; $i -lt $Options.Count; $i++) {
        if ($i -eq ($Default - 1)) { Write-Host ("  {0}. {1} (default)" -f ($i+1), $Options[$i]) }
        else { Write-Host ("  {0}. {1}" -f ($i+1), $Options[$i]) }
    }
    while ($true) {
        $sel = Read-Host ("Enter choice [1-{0}]" -f $Options.Count)
        if (-not $sel) { return $Default }
        if ($sel -match '^[0-9]+$' -and [int]$sel -ge 1 -and [int]$sel -le $Options.Count) {
            return [int]$sel
        }
        Write-Host "Invalid choice: $sel" -ForegroundColor Yellow
    }
}

function Read-YesNo {
    [CmdletBinding()] param(
        [Parameter(Mandatory)][string]$Prompt,
        [ValidateSet('y','n')][string]$Default = 'y'
    )
    if ($script:Quiet) { return $Default }
    while ($true) {
        $suffix = if ($Default -eq 'y') { '[Y/n]' } elseif ($Default -eq 'n') { '[y/N]' } else { '[y/n]' }
        $resp = Read-Host "$Prompt $suffix"
        if (-not $resp) { return $Default }
        $resp = $resp.ToLowerInvariant()
        if ($resp -in @('y','n')) { return $resp }
        Write-Host 'Please enter y or n.' -ForegroundColor Yellow
    }
}

function Read-Credentials {
    [CmdletBinding()] param(
        [string]$UserPrompt = 'Enter the user ID:',
        [string]$PasswordPrompt = 'Enter the password:',
        [string]$ConfirmPrompt
    )
    while ($true) {
        $user = Read-Host $UserPrompt
        $pass = Read-Host -AsSecureString $PasswordPrompt
        if ($ConfirmPrompt) {
            $ans = Read-YesNo -Prompt $ConfirmPrompt -Default y
            if ($ans -ne 'y') { continue }
        }
        return [PSCustomObject]@{ User = $user; Password = $pass }
    }
}

function Get-FromYaml {
    [CmdletBinding()] param(
        [Parameter(Mandatory)][string]$Query,
        [Parameter(Mandatory)][string]$Path
    )
    if (-not (Test-Path -Path $Path -PathType Leaf)) { return $null }
    # Prefer yq if installed, otherwise try powershell-yaml
    if (Get-Command yq -ErrorAction SilentlyContinue) {
        $r = yq eval $Query $Path 2>$null
        if ($r -and $r -ne 'null') { return $r } else { return $null }
    } elseif (Get-Module -ListAvailable -Name powershell-yaml) {
        Import-Module powershell-yaml -ErrorAction Stop
        $obj = ConvertFrom-Yaml -Path $Path
        # Very simple path navigation for dot notation
        $segments = $Query.TrimStart('.') -split '\.'
        foreach ($seg in $segments) {
            if ($null -eq $obj) { break }
            $obj = $obj.$seg
        }
        return $obj
    } else {
        Write-Trace 'YAML query skipped (no yq or powershell-yaml)'
        return $null
    }
}

function Show-VariablesTable {
    [CmdletBinding()] param(
        [Parameter(ValueFromRemainingArguments)][string[]]$Names
    )
    if ($script:Quiet -or -not $Names) { return }
    $border = '─' * 59
    Write-Host "┌$border"
    foreach ($n in $Names) {
        if (-not $n) { continue }
        if ($n -match '^-line$') { Write-Host "├$border"; continue }
        if ($n -match '^-blank$') { Write-Host "│"; continue }
        if (-not (Get-Variable -Name $n -Scope 1 -ErrorAction SilentlyContinue)) {
            Write-Host ("│ {0,-20} : (undef)" -f $n)
        } else {
            $val = (Get-Variable -Name $n -Scope 1).Value
            if ($val -is [array]) { $val = "[" + ($val -join ', ') + "]" }
            Write-Host ("│ {0,-20} : {1}" -f $n, $val)
        }
    }
    Write-Host "└$border"
}

Export-ModuleMember -Function *
