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

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$manualStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\ManualEntry\IManualEntryStageModule.cs'
$targetingStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Targeting\ITargetingStageModule.cs'
$previewStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Preview\IPreviewStageModule.cs'
$confirmStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Confirm\IConfirmStageModule.cs'
$aimStagePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Aim\IAimStageModule.cs'
$aimContributionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Aim\AimContribution.cs'
$aimRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\AimRecord.cs'
$fireStagePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Fire\IFireStageModule.cs'
$fireContributionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Fire\FireContribution.cs'
$fireRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Model\FireRecord.cs'
$flightStagePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Flight\IFlightStageModule.cs'
$flightContributionPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Flight\FlightContribution.cs'
$impactStagePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Impact\IImpactStageModule.cs'
$impactContributionPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactContribution.cs'
$impactPlanPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Model\ImpactPlan.cs'
$prepareStagePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Prepare\IPrepareStageModule.cs'
$prepareContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Prepare\PrepareStageContext.cs'
$prepareContributionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\Prepare\PrepareContribution.cs'
$projectileInitStagePath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\IProjectileInitStageModule.cs'
$projectileInitContextPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageContext.cs'
$projectileInitContributionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitContribution.cs'
$arrivalStagePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Arrival\IArrivalStageModule.cs'
$arrivalContextPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Arrival\ArrivalStageContext.cs'
$arrivalContributionPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Arrival\ArrivalContribution.cs'
$hitStagePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Hit\IHitStageModule.cs'
$hitContextPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Hit\HitStageContext.cs'
$hitContributionPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Hit\HitContribution.cs'
$manualRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ManualEntryRecord.cs'
$targetingRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\TargetingRecord.cs'
$previewRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\PreviewRecord.cs'
$confirmRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmRecord.cs'

$manualStageText = Get-Content -LiteralPath $manualStagePath -Raw -Encoding utf8
$targetingStageText = Get-Content -LiteralPath $targetingStagePath -Raw -Encoding utf8
$previewStageText = Get-Content -LiteralPath $previewStagePath -Raw -Encoding utf8
$confirmStageText = Get-Content -LiteralPath $confirmStagePath -Raw -Encoding utf8
$aimStageText = Get-Content -LiteralPath $aimStagePath -Raw -Encoding utf8
$aimContributionText = Get-Content -LiteralPath $aimContributionPath -Raw -Encoding utf8
$aimRecordText = Get-Content -LiteralPath $aimRecordPath -Raw -Encoding utf8
$fireStageText = Get-Content -LiteralPath $fireStagePath -Raw -Encoding utf8
$fireContributionText = Get-Content -LiteralPath $fireContributionPath -Raw -Encoding utf8
$fireRecordText = Get-Content -LiteralPath $fireRecordPath -Raw -Encoding utf8
$flightStageText = Get-Content -LiteralPath $flightStagePath -Raw -Encoding utf8
$flightContributionText = Get-Content -LiteralPath $flightContributionPath -Raw -Encoding utf8
$impactStageText = Get-Content -LiteralPath $impactStagePath -Raw -Encoding utf8
$impactContributionText = Get-Content -LiteralPath $impactContributionPath -Raw -Encoding utf8
$impactPlanText = Get-Content -LiteralPath $impactPlanPath -Raw -Encoding utf8
$prepareStageText = Get-Content -LiteralPath $prepareStagePath -Raw -Encoding utf8
$prepareContextText = Get-Content -LiteralPath $prepareContextPath -Raw -Encoding utf8
$prepareContributionText = Get-Content -LiteralPath $prepareContributionPath -Raw -Encoding utf8
$projectileInitStageText = Get-Content -LiteralPath $projectileInitStagePath -Raw -Encoding utf8
$projectileInitContextText = Get-Content -LiteralPath $projectileInitContextPath -Raw -Encoding utf8
$projectileInitContributionText = Get-Content -LiteralPath $projectileInitContributionPath -Raw -Encoding utf8
$arrivalStageText = Get-Content -LiteralPath $arrivalStagePath -Raw -Encoding utf8
$arrivalContextText = Get-Content -LiteralPath $arrivalContextPath -Raw -Encoding utf8
$arrivalContributionText = Get-Content -LiteralPath $arrivalContributionPath -Raw -Encoding utf8
$hitStageText = Get-Content -LiteralPath $hitStagePath -Raw -Encoding utf8
$hitContextText = Get-Content -LiteralPath $hitContextPath -Raw -Encoding utf8
$hitContributionText = Get-Content -LiteralPath $hitContributionPath -Raw -Encoding utf8
$manualRecordText = Get-Content -LiteralPath $manualRecordPath -Raw -Encoding utf8
$targetingRecordText = Get-Content -LiteralPath $targetingRecordPath -Raw -Encoding utf8
$previewRecordText = Get-Content -LiteralPath $previewRecordPath -Raw -Encoding utf8
$confirmRecordText = Get-Content -LiteralPath $confirmRecordPath -Raw -Encoding utf8

$privateContextCarrierTexts = @(
    $manualRecordText,
    $targetingRecordText,
    $previewRecordText,
    $confirmRecordText,
    $prepareContextText,
    $projectileInitContextText,
    $arrivalContextText,
    $hitContextText
)

Assert-True ($manualStageText -match 'public\s+interface\s+IManualEntryStageModule') 'ManualEntry stage must stay public for authors.'
Assert-True ($targetingStageText -match 'public\s+interface\s+ITargetingStageModule') 'Targeting stage must stay public for authors.'
Assert-True ($previewStageText -match 'public\s+interface\s+IPreviewStageModule') 'Preview stage must stay public for authors.'
Assert-True ($confirmStageText -match 'public\s+interface\s+IConfirmStageModule') 'Confirm stage must stay public for authors.'
Assert-True ($aimStageText -match 'public\s+interface\s+IAimStageModule') 'Aim stage must stay public for authors.'
Assert-True ($fireStageText -match 'public\s+interface\s+IFireStageModule') 'Fire stage must stay public for authors.'
Assert-True ($flightStageText -match 'public\s+interface\s+IFlightStageModule') 'Flight stage must stay public for authors.'
Assert-True ($impactStageText -match 'public\s+interface\s+IImpactStageModule') 'Impact stage must stay public for authors.'

Assert-True ($prepareStageText -match 'public\s+interface\s+IPrepareStageModule') 'Prepare stage must be public for authors.'
Assert-True ($projectileInitStageText -match 'public\s+interface\s+IProjectileInitStageModule') 'ProjectileInit stage must be public for authors.'
Assert-True ($arrivalStageText -match 'public\s+interface\s+IArrivalStageModule') 'Arrival stage must be public for authors.'
Assert-True ($hitStageText -match 'public\s+interface\s+IHitStageModule') 'Hit stage must be public for authors.'

Assert-True ($prepareContextText -match 'public\s+readonly\s+struct\s+PrepareStageContext') 'Prepare context must be public because it is in the author method signature.'
Assert-True ($prepareContributionText -match 'public\s+sealed\s+class\s+PrepareContribution') 'Prepare contribution must be public because authors write into it.'
Assert-True ($projectileInitContextText -match 'public\s+readonly\s+struct\s+ProjectileInitStageContext') 'ProjectileInit context must be public because it is in the author method signature.'
Assert-True ($projectileInitContributionText -match 'public\s+sealed\s+class\s+ProjectileInitContribution') 'ProjectileInit contribution must be public because authors write into it.'
Assert-True ($projectileInitContributionText -match 'public\s+sealed\s+class\s+ProjectileInitPlanContribution') 'ProjectileInit plan contribution must be public because authors write per-projectile plan facts.'
Assert-True ($arrivalContextText -match 'public\s+readonly\s+struct\s+ArrivalStageContext') 'Arrival context must be public because it is in the author method signature.'
Assert-True ($arrivalContributionText -match 'public\s+sealed\s+class\s+ArrivalContribution') 'Arrival contribution must be public because authors write into it.'
Assert-True ($hitContextText -match 'public\s+readonly\s+struct\s+HitStageContext') 'Hit context must be public because it is in the author method signature.'
Assert-True ($hitContributionText -match 'public\s+sealed\s+class\s+HitContribution') 'Hit contribution must be public because authors write into it.'

Assert-True ($manualRecordText -notmatch 'public\s+RangedAttackModuleSession') 'ManualEntryRecord must not publicly expose module session.'
Assert-True ($targetingRecordText -notmatch 'public\s+RangedAttackModuleSession') 'TargetingRecord must not publicly expose module session.'
Assert-True ($previewRecordText -notmatch 'public\s+RangedAttackModuleSession') 'PreviewRecord must not publicly expose module session.'
Assert-True ($confirmRecordText -notmatch 'public\s+RangedAttackModuleSession') 'ConfirmRecord must not publicly expose module session.'

foreach ($text in $privateContextCarrierTexts) {
    Assert-True ($text -match 'GetPrivateContext') 'Author surface must expose GetPrivateContext.'
    Assert-True ($text -match 'GetOrCreatePrivateContext') 'Author surface must expose GetOrCreatePrivateContext.'
    Assert-True ($text -match 'TryGetPrivateContext') 'Author surface must expose TryGetPrivateContext.'
}

Assert-True (
    ($aimContributionText -match 'AccuracyFactorMultiplier') -and
    ($aimContributionText -match 'ForcedMissRadiusCandidate') -and
    ($aimContributionText -notmatch 'HasOverrideAimPointWorld') -and
    ($aimContributionText -notmatch 'AddedAimOffsetWorld') -and
    ($aimContributionText -notmatch 'OverridePathCells') -and
    ($aimContributionText -notmatch 'OverridePathVariance') -and
    ($aimContributionText -notmatch 'OverrideGuideCell') -and
    ($aimContributionText -notmatch 'ValidationPassed')
) 'Aim author contribution surface must expose only target/accuracy/forced-miss intent.'

Assert-True (
    ($aimRecordText -match 'FinalTarget') -and
    ($aimRecordText -match 'AccuracyFactor') -and
    ($aimRecordText -match 'ForcedMissRadius') -and
    ($aimRecordText -notmatch 'AimPointWorld') -and
    ($aimRecordText -notmatch 'AimOffsetWorld') -and
    ($aimRecordText -notmatch 'PathCells') -and
    ($aimRecordText -notmatch 'PathVariance') -and
    ($aimRecordText -notmatch 'GuideCell') -and
    ($aimRecordText -notmatch 'ValidationPassed')
) 'Aim formal record must shrink to real launch-consumed facts.'

Assert-True (
    ($fireContributionText -notmatch 'EnableAutoRouteFire') -and
    ($fireRecordText -notmatch 'EnableAutoRouteFire')
) 'Fire surface must not keep auto-route placeholders without a real host consumer.'

Assert-True (
    ($flightContributionText -match 'RedirectDestination') -and
    ($flightContributionText -match 'OverrideCurrentTarget') -and
    ($flightContributionText -match 'ContinueFlight') -and
    ($flightContributionText -notmatch 'OverrideLockedTarget') -and
    ($flightContributionText -notmatch 'OverridePhase')
) 'Flight author contribution surface must keep only neutral redirect/current-target/continue controls.'

Assert-True (
    ($impactContributionText -notmatch 'ExtraEffectsToAppend') -and
    ($impactPlanText -notmatch 'ExtraEffects')
) 'Impact author surface must not expose extra effect placeholders without an executor.'

Write-Output 'RangedModuleAuthorSurfaceBoundarySmokeTests PASS'
