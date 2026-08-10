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
$trackingModulePath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingModule.cs'
$projectileContributionPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs'
$projectilePlanPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$projectileInitStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$projectileHostPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
$trackingDefPath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'

$trackingConfigText = Read-Source $trackingConfigPath
$trackingModuleText = Read-Source $trackingModulePath
$projectileContributionText = Read-Source $projectileContributionPath
$projectilePlanText = Read-Source $projectilePlanPath
$projectileInitStageServiceText = Read-Source $projectileInitStageServicePath
$projectileHostText = Read-Source $projectileHostPath
$trackingDefText = Read-Source $trackingDefPath

Assert-True (
    ($trackingConfigText -match '\bInitialSegmentTriggerRatio\b') -and
    ($trackingConfigText -match 'InitialSegmentTriggerRatio\s*=\s*0\.5f') -and
    ($trackingDefText -match '<InitialSegmentTriggerRatio>0\.5</InitialSegmentTriggerRatio>')
) 'TrackingModuleConfig and DevHarness module def must expose InitialSegmentTriggerRatio with default 0.5.'

Assert-True (
    ($projectileContributionText -match '\bHasInitialSegmentTriggerRatio\b') -and
    ($projectileContributionText -match '\bInitialSegmentTriggerRatio\b')
) 'ProjectileInit plan contribution surface must expose an explicit initial-segment trigger ratio channel.'

Assert-True (
    ($projectilePlanText -match '\bHasInitialSegmentTriggerRatio\b') -and
    ($projectilePlanText -match '\bInitialSegmentTriggerRatio\b')
) 'ProjectileInitPlan must persist the initial-segment trigger ratio for the projectile host.'

Assert-True (
    ($projectileInitStageServiceText -match 'planContribution\.HasInitialSegmentTriggerRatio') -and
    ($projectileInitStageServiceText -match 'plan\.HasInitialSegmentTriggerRatio\s*=\s*true') -and
    ($projectileInitStageServiceText -match 'plan\.InitialSegmentTriggerRatio\s*=\s*planContribution\.InitialSegmentTriggerRatio')
) 'ProjectileInitStageService must copy the initial-segment trigger ratio from module contribution into the frozen projectile plan.'

Assert-True (
    ($trackingModuleText -match 'HasInitialSegmentTriggerRatio\s*=\s*true') -and
    ($trackingModuleText -match 'InitialSegmentTriggerRatio\s*=\s*frozenConfig\.InitialSegmentTriggerRatio')
) 'TrackingModule projectile init stage must freeze InitialSegmentTriggerRatio into each emit plan contribution.'

Assert-True (
    ($projectileHostText -match 'launchPlan != null && launchPlan\.HasInitialSegmentTriggerRatio') -and
    ($projectileHostText -match 'InitialFlightPathSnapshot') -and
    ($projectileHostText -match 'BuildLinearFlightPathSnapshot\(this\.origin,\s*destination\)') -and
    ($projectileHostText -match 'InitialSegmentTriggerRatio') -and
    ($projectileHostText -match 'Mathf\.Clamp')
) 'BdpProjectile.Launch must shorten the first physical segment from the real launch destination using the frozen initial-segment trigger ratio when no explicit initial path snapshot is supplied.'

Write-Output 'TrackingInitialSegmentTriggerSmokeTests PASS'
