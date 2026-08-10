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

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot "Source\\BDP\\Core"

$pathKindPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Model\\ProjectileFlightPathKind.cs"
$pathSnapshotPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Model\\ProjectileFlightPathSnapshot.cs"
$pathUtilityPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Projection\\ProjectileFlightPathUtility.cs"
$projectileInitPlanPath = Join-Path $bdpSourceRoot "AttackExecution\\RangedProtocol\\Model\\ProjectileInitPlan.cs"
$projectileInitContributionPath = Join-Path $bdpSourceRoot "AttackExecution\\RangedProtocol\\ProjectileInit\\ProjectileInitContribution.cs"
$arrivalContributionPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Arrival\\ArrivalContribution.cs"
$arrivalRecordPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Model\\ArrivalRecord.cs"
$projectileHostPath = Join-Path $bdpSourceRoot "Projectiles\\BdpProjectile.cs"

$pathKindText = if (Test-Path -LiteralPath $pathKindPath) { Read-Source $pathKindPath } else { '' }
$pathSnapshotText = if (Test-Path -LiteralPath $pathSnapshotPath) { Read-Source $pathSnapshotPath } else { '' }
$pathUtilityText = if (Test-Path -LiteralPath $pathUtilityPath) { Read-Source $pathUtilityPath } else { '' }
$projectileInitPlanText = if (Test-Path -LiteralPath $projectileInitPlanPath) { Read-Source $projectileInitPlanPath } else { '' }
$projectileInitContributionText = if (Test-Path -LiteralPath $projectileInitContributionPath) { Read-Source $projectileInitContributionPath } else { '' }
$arrivalContributionText = if (Test-Path -LiteralPath $arrivalContributionPath) { Read-Source $arrivalContributionPath } else { '' }
$arrivalRecordText = if (Test-Path -LiteralPath $arrivalRecordPath) { Read-Source $arrivalRecordPath } else { '' }
$projectileHostText = if (Test-Path -LiteralPath $projectileHostPath) { Read-Source $projectileHostPath } else { '' }

Assert-True (Test-Path -LiteralPath $pathKindPath) 'ProjectileFlightPathKind.cs must exist.'
Assert-True (Test-Path -LiteralPath $pathSnapshotPath) 'ProjectileFlightPathSnapshot.cs must exist.'
Assert-True (Test-Path -LiteralPath $pathUtilityPath) 'ProjectileFlightPathUtility.cs must exist.'

Assert-True (
    ($pathKindText -match 'enum\s+ProjectileFlightPathKind') -and
    ($pathKindText -match 'Linear') -and
    ($pathKindText -match 'CubicBezier')
) 'ProjectileFlightPathKind must declare Linear and CubicBezier.'

Assert-True (
    ($pathSnapshotText -match 'class\s+ProjectileFlightPathSnapshot') -and
    ($pathSnapshotText -match 'ProjectileFlightPathKind') -and
    ($pathSnapshotText -match 'ApproximateLength') -and
    ($pathSnapshotText -match 'ExposeData')
) 'ProjectileFlightPathSnapshot must expose path kind, control points, length, and ExposeData.'

Assert-True (
    ($pathUtilityText -match 'class\s+ProjectileFlightPathUtility') -and
    ($pathUtilityText -match 'CreateLinear') -and
    ($pathUtilityText -match 'CreateCubicBezier') -and
    ($pathUtilityText -match 'EvaluatePosition') -and
    ($pathUtilityText -match 'EvaluateTangent') -and
    ($pathUtilityText -match 'EstimateLength')
) 'ProjectileFlightPathUtility must provide creation, evaluation, and length helpers.'

Assert-True (
    ($projectileInitPlanText -match 'InitialFlightPathSnapshot') -and
    ($projectileInitPlanText -match 'Scribe_Deep\.Look\(ref initialFlightPathSnapshot')
) 'ProjectileInitPlan must persist InitialFlightPathSnapshot.'

Assert-True (
    ($projectileInitContributionText -match 'HasInitialFlightPathSnapshot') -and
    ($projectileInitContributionText -match 'InitialFlightPathSnapshot')
) 'ProjectileInitPlanContribution must expose initial flight path override fields.'

Assert-True (
    ($arrivalContributionText -match 'HasNextFlightPathSnapshot') -and
    ($arrivalContributionText -match 'NextFlightPathSnapshot')
) 'ArrivalContribution must expose next-flight-path override fields.'

Assert-True (
    ($arrivalRecordText -match 'NextFlightPathSnapshot')
) 'ArrivalRecord must carry NextFlightPathSnapshot.'

Assert-True (
    ($projectileHostText -match 'currentFlightPathSnapshot') -and
    ($projectileHostText -match 'currentFlightPathStartingTicksToImpact') -and
    ($projectileHostText -match 'BindFlightPathSnapshot') -and
    ($projectileHostText -match 'AlignFlightPathSnapshotStart') -and
    ($projectileHostText -match 'BuildLinearFlightPathSnapshot') -and
    ($projectileHostText -match 'ResolveCurrentFlightProgress') -and
    ($projectileHostText -match 'ResolveCurrentFlightStartDirection')
) 'BdpProjectile must expose the curve-host field set and helper methods.'

Assert-True (
    ($projectileHostText -match 'Scribe_Deep\.Look\(ref currentFlightPathSnapshot') -and
    ($projectileHostText -match 'Scribe_Values\.Look\(ref currentFlightPathStartingTicksToImpact')
) 'BdpProjectile must persist the current flight path snapshot and starting ticks.'

Assert-True (
    ($projectileHostText -match 'ProjectileFlightPathUtility\.EvaluatePosition') -and
    ($projectileHostText -match 'ProjectileFlightPathUtility\.EvaluateTangent') -and
    ($projectileHostText -match 'arrival\.NextFlightPathSnapshot')
) 'BdpProjectile must evaluate position and tangent from ProjectileFlightPathUtility and prefer arrival.NextFlightPathSnapshot.'

Write-Output 'RangedFlightPathSnapshotHostSmokeTests PASS'
