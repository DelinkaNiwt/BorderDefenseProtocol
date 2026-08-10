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
$trackingStatePath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingModuleState.cs'
$trackingModulePath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingModule.cs'
$trackingPathBuilderPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingPathBuilder.cs'
$trackingDefPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'

$trackingConfigText = Read-Source $trackingConfigPath
$trackingStateText = Read-Source $trackingStatePath
$trackingModuleText = Read-Source $trackingModulePath
$trackingPathBuilderText = Read-Source $trackingPathBuilderPath
$trackingDefText = Read-Source $trackingDefPath

Assert-True (
    ($trackingConfigText -match '\bLossBehindAngle\b') -and
    ($trackingConfigText -match '\bLossDistanceGrowthTolerance\b') -and
    ($trackingConfigText -match '\bRelockLeadRatio\b') -and
    ($trackingConfigText -match '\bRelockTurnAngle\b') -and
    ($trackingDefText -match '<LossBehindAngle>') -and
    ($trackingDefText -match '<LossDistanceGrowthTolerance>') -and
    ($trackingDefText -match '<RelockLeadRatio>') -and
    ($trackingDefText -match '<RelockTurnAngle>')
) 'Tracking config and Def surface must expose lost-state and relock-specific tuning knobs.'

Assert-True (
    ($trackingStateText -match '\bPursuing\b') -and
    ($trackingStateText -match '\bLost\b') -and
    ($trackingStateText -match '\bRelocking\b') -and
    ($trackingStateText -match '\bReleased\b') -and
    ($trackingStateText -match '\bHitCheckPending\b') -and
    ($trackingStateText -match '\bFlyAway\b') -and
    ($trackingStateText -match '\bFinished\b')
) 'Tracking phase enum must separate pursuing, lost, relocking, released, hit-check, fly-away, and finished states.'

Assert-True (
    ($trackingModuleText -match 'TrackingPhase\.Pursuing') -and
    ($trackingModuleText -match 'TrackingPhase\.Lost') -and
    ($trackingModuleText -match 'TrackingPhase\.Relocking') -and
    ($trackingModuleText -match 'TrackingPhase\.Released') -and
    ($trackingModuleText -match 'BuildTrackingPath') -and
    ($trackingModuleText -match 'BuildRelockPath') -and
    ($trackingModuleText -match 'BuildReleasePath')
) 'Tracking module must distinguish pursuing, released, and relocking paths with separate path-builder entry points.'

Assert-True (
    ($trackingPathBuilderText -match 'BuildTrackingPath') -and
    ($trackingPathBuilderText -match 'BuildRelockPath') -and
    ($trackingPathBuilderText -match 'BuildReleasePath') -and
    ($trackingPathBuilderText -notmatch '\bResolveLateralSign\b') -and
    ($trackingPathBuilderText -notmatch '\bsideBias\b')
) 'Tracking path builder must keep pursuit/release/relock paths free of side-intercept bias.'

Write-Output 'TrackingPursuitStateMachineSmokeTests PASS'
