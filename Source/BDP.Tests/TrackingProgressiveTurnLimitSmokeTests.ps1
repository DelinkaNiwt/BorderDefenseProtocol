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

$trackingConfigPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingModuleConfig.cs'
$trackingPathBuilderPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingPathBuilder.cs'
$trackingDefPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'

$trackingConfigText = Read-Source $trackingConfigPath
$trackingPathBuilderText = Read-Source $trackingPathBuilderPath
$trackingDefText = Read-Source $trackingDefPath

Assert-True (
    ($trackingConfigText -match '\bPursuitMinTurnAngle\b') -and
    ($trackingConfigText -match '\bPursuitTurnResponsiveness\b') -and
    ($trackingConfigText -match '\bRelockMinTurnAngle\b') -and
    ($trackingConfigText -match '\bRelockTurnResponsiveness\b') -and
    ($trackingDefText -match '<PursuitMinTurnAngle>') -and
    ($trackingDefText -match '<PursuitTurnResponsiveness>') -and
    ($trackingDefText -match '<RelockMinTurnAngle>') -and
    ($trackingDefText -match '<RelockTurnResponsiveness>')
) 'Tracking config and Def surface must expose progressive pursuit/relock turn-limit tuning knobs.'

Assert-True (
    ($trackingPathBuilderText -match '\bComputeProgressiveTurnAngle\b') -and
    ($trackingPathBuilderText -match '\bComputeProgressiveTurnDirection\b') -and
    ($trackingPathBuilderText -match 'BuildTrackingPath') -and
    ($trackingPathBuilderText -match 'BuildRelockPath') -and
    ($trackingPathBuilderText -match 'Vector3\.RotateTowards')
) 'Tracking path builder must implement progressive turn-angle resolution for both pursuit and relock paths.'

Write-Output 'TrackingProgressiveTurnLimitSmokeTests PASS'
