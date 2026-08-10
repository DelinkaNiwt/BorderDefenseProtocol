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
    ($trackingConfigText -match '\bSoftReleaseDistance\b') -and
    ($trackingConfigText -match '\bHardReleaseDistance\b') -and
    ($trackingConfigText -match '\bReleaseDistanceDecayPerRelock\b') -and
    ($trackingDefText -match '<SoftReleaseDistance>') -and
    ($trackingDefText -match '<HardReleaseDistance>') -and
    ($trackingDefText -match '<ReleaseDistanceDecayPerRelock>')
) 'Tracking config and Def surface must expose soft/hard release distances and per-relock decay.'

Assert-True (
    ($trackingStateText -match '\bReleased\b') -and
    ($trackingStateText -match '\bReleaseExpectedImpactPoint\b')
) 'Tracking state must persist the Released phase and the frozen expected impact point.'

Assert-True (
    ($trackingPathBuilderText -match '\bResolveReleaseDistances\b') -and
    ($trackingPathBuilderText -match '\bComputeReleaseTurnScale\b') -and
    ($trackingPathBuilderText -match '\bBuildReleasePath\b') -and
    ($trackingPathBuilderText -match 'Vector3\.Dot')
) 'Tracking path builder must resolve release thresholds, fade pursuit turn strength, and build a straight release path from a frozen expected impact point.'

Assert-True (
    ($trackingModuleText -match 'TrackingPhase\.Released') -and
    ($trackingModuleText -match '"enter_release"') -and
    ($trackingModuleText -match '"released_enter_hit_check"') -and
    ($trackingModuleText -match '"released_relock"') -and
    ($trackingModuleText -match '"released_flyaway"') -and
    ($trackingModuleText -match 'pathType=" \+ SafeDiagnosticText\(pathType\)')
) 'Tracking module must expose explicit release branches, including release-to-hit-check recovery, and keep diagnostics flowing through the shared path-build logger.'

Write-Output 'TrackingReleaseWindowSmokeTests PASS'
