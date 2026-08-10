$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modProjectsRoot = Split-Path -Parent $repoRoot

$trackingModulePath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingModule.cs'
$trackingPathBuilderPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingPathBuilder.cs'

$trackingModuleText = Read-Source $trackingModulePath
$trackingPathBuilderText = Read-Source $trackingPathBuilderPath

Assert-True (
    ($trackingModuleText -match 'BdpDiagnostics\.AttackExecution') -and
    ($trackingModuleText -match 'event=tracking_arrival_decision') -and
    ($trackingModuleText -match 'event=tracking_path_build') -and
    ($trackingModuleText -match 'event=tracking_hit_review')
) 'TrackingModule must emit attack-execution diagnostics for arrival decisions, path builds, and final hit review.'

Assert-True (
    ($trackingPathBuilderText -match '\bComputeProgressiveTurnAngle\b') -and
    ($trackingPathBuilderText -match '\bComputeProgressiveTurnDirection\b')
) 'TrackingPathBuilder must expose progressive turn diagnostics helpers so runtime logs can report actual turn-limit calculations.'

Write-Output 'TrackingDiagnosticsSmokeTests PASS'
