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

$arrivalContributionPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Arrival\ArrivalContribution.cs'
$arrivalRecordPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Model\ArrivalRecord.cs'
$arrivalServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Arrival\ArrivalStageService.cs'
$projectilePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
$trackingModulePath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingModule.cs'

$arrivalContributionText = Read-Source $arrivalContributionPath
$arrivalRecordText = Read-Source $arrivalRecordPath
$arrivalServiceText = Read-Source $arrivalServicePath
$projectileText = Read-Source $projectilePath
$trackingModuleText = Read-Source $trackingModulePath

Assert-True (
    ($arrivalContributionText -match '\bHasNextBindingTarget\b') -and
    ($arrivalContributionText -match '\bLocalTargetInfo\s+NextBindingTarget\b')
) 'ArrivalContribution must expose a separate next-segment vanilla binding target surface.'

Assert-True (
    $arrivalRecordText -match '\bLocalTargetInfo\s+NextBindingTarget\b'
) 'ArrivalRecord must carry the next-segment vanilla binding target separately from the semantic target.'

Assert-True (
    ($arrivalServiceText -match 'NextBindingTarget\s*=\s*flight\s*!=\s*null\s*\?\s*flight\.CurrentTarget\s*:\s*Verse\.LocalTargetInfo\.Invalid') -and
    ($arrivalServiceText -match 'if\s*\(contribution\.HasNextBindingTarget\)') -and
    ($arrivalServiceText -match 'record\.NextBindingTarget\s*=\s*contribution\.NextBindingTarget')
) 'ArrivalStageService must seed and apply the separate next binding target field.'

Assert-True (
    ($trackingModuleText -match 'HasNextBindingTarget\s*=\s*true') -and
    ($trackingModuleText -match 'NextBindingTarget\s*=\s*new\s+LocalTargetInfo\(flyAwayPath\.End\.ToIntVec3\(\)\)') -and
    ($trackingModuleText -match 'NextTarget\s*=\s*state\.LockedTarget')
) 'TrackingModule fly-away path must keep the semantic target while rebinding vanilla impact to the fly-away landing cell.'

Assert-True (
    ($trackingModuleText -match 'TrackingPhase\.Released') -and
    ($trackingModuleText -match 'ReleaseExpectedImpactPoint') -and
    ($trackingModuleText -match 'ContinueTracking\(contribution,\s*state\.LockedTarget,\s*releaseFlightPathSnapshot\)') -and
    ($trackingModuleText -notmatch 'state\.ReleaseExpectedImpactPoint\.ToIntVec3\(\)')
) 'TrackingModule release path must keep vanilla binding on the locked target so stationary targets stay deterministic after release.'

Assert-True (
    ($projectileText -match '\bLocalTargetInfo\s+nextBindingTarget\b') -and
    ($projectileText -match 'arrival\.NextBindingTarget\.IsValid') -and
    ($projectileText -match 'usedTarget\s*=\s*nextBindingTarget') -and
    ($projectileText -match 'currentFlightRecord\.CurrentTarget\s*=\s*nextTarget') -and
    ($projectileText -match 'arrivalNextBindingTarget') -and
    ($projectileText -match 'resolvedNextBindingTarget')
) 'BdpProjectile continuation must bind usedTarget from the separate binding target while preserving semantic current target tracking and diagnostics.'

Write-Output 'TrackingContinuationBindingSmokeTests PASS'
