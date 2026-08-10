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
$protocolPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$groupedTargetingSourcePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\GroupedAttackExecutionTargetingSource.cs'
$attackProtocolSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolSurfaceAccess.cs'
$executorPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\DefaultRangedAttackExecutor.cs'
$effectEmitterPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\DefaultAttackEffectEmitter.cs'
$jobDriverPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'
$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$continuationPlannerPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbContinuationPlanner.cs'
$roundStatePath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbRoundState.cs'
$triggerRuntimeServicesPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeServices.cs'
$attackExecutionEmitPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionEmit.cs'
$attackExecutionStagesPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionService.Stages.cs'
$aimStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Aim\AimStageService.cs'
$prepareStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Prepare\PrepareStageService.cs'
$prepareRecordPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\PrepareRecord.cs'
$fireStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Fire\FireStageService.cs'
$projectileInitStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageService.cs'
$prepareModulePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Prepare\RangedTrionPrepareModule.cs'
$trionGatePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackTrionGate.cs'
$combatBodySessionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$fireEmitRecordPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\FireEmitRecord.cs'
$aimStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Aim\IAimStageModule.cs'
$prepareStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Prepare\IPrepareStageModule.cs'
$fireStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Fire\IFireStageModule.cs'
$projectileInitStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\IProjectileInitStageModule.cs'
$aimStageDimensionPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Aim\AimStageDimension.cs'
$prepareStageDimensionPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Prepare\PrepareStageDimension.cs'
$fireStageDimensionPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Fire\FireStageDimension.cs'
$projectileInitStageDimensionPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\ProjectileInit\ProjectileInitStageDimension.cs'
$projectileInitPlanPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\ProjectileInitPlan.cs'
$targetingSourcePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$flightProtocolPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\RangedFlightProtocolService.cs'
$flightProtocolSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\RangedFlightProtocolSurfaceAccess.cs'
$projectilePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
$arrivalContributionPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Arrival\ArrivalContribution.cs'
$arrivalRecordPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Model\ArrivalRecord.cs'
$hitContributionPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Hit\HitContribution.cs'
$hitRecordPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Model\HitRecord.cs'
$flightStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Flight\FlightStageService.cs'
$arrivalStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Arrival\ArrivalStageService.cs'
$hitStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Hit\HitStageService.cs'
$impactStageServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Impact\ImpactStageService.cs'
$flightStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Flight\IFlightStageModule.cs'
$arrivalStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Arrival\IArrivalStageModule.cs'
$hitStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Hit\IHitStageModule.cs'
$impactStageModulePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Impact\IImpactStageModule.cs'
$flightStageDimensionPolicyPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Flight\FlightStageDimensionPolicy.cs'
$arrivalStageDimensionPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Arrival\ArrivalStageDimension.cs'
$hitStageDimensionPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Hit\HitStageDimension.cs'
$impactStageDimensionPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Impact\ImpactStageDimension.cs'
$protocolResultPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedAttackProtocolResult.cs'
$emissionModePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedVerbEmissionMode.cs'
$emissionPlanPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedVerbEmissionPlan.cs'
$emissionWindowPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\RangedVerbEmissionWindowPlan.cs'

$protocolText = Get-Content -LiteralPath $protocolPath -Raw -Encoding utf8
$groupedTargetingSourceExists = Test-Path -LiteralPath $groupedTargetingSourcePath
$groupedTargetingSourceText = if ($groupedTargetingSourceExists) { Get-Content -LiteralPath $groupedTargetingSourcePath -Raw -Encoding utf8 } else { '' }
$attackProtocolSurfaceExists = Test-Path -LiteralPath $attackProtocolSurfacePath
$attackProtocolSurfaceText = if ($attackProtocolSurfaceExists) { Get-Content -LiteralPath $attackProtocolSurfacePath -Raw -Encoding utf8 } else { '' }
$executorText = Get-Content -LiteralPath $executorPath -Raw -Encoding utf8
$effectEmitterText = Get-Content -LiteralPath $effectEmitterPath -Raw -Encoding utf8
$jobDriverText = Get-Content -LiteralPath $jobDriverPath -Raw -Encoding utf8
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8
$roundStateText = Get-Content -LiteralPath $roundStatePath -Raw -Encoding utf8
$triggerRuntimeServicesText = Get-Content -LiteralPath $triggerRuntimeServicesPath -Raw -Encoding utf8
$attackExecutionEmitText = Get-Content -LiteralPath $attackExecutionEmitPath -Raw -Encoding utf8
$attackExecutionStagesText = Get-Content -LiteralPath $attackExecutionStagesPath -Raw -Encoding utf8
$prepareRecordText = Get-Content -LiteralPath $prepareRecordPath -Raw -Encoding utf8
$prepareModuleExists = Test-Path -LiteralPath $prepareModulePath
$prepareModuleText = if ($prepareModuleExists) { Get-Content -LiteralPath $prepareModulePath -Raw -Encoding utf8 } else { '' }
$trionGateExists = Test-Path -LiteralPath $trionGatePath
$trionGateText = if ($trionGateExists) { Get-Content -LiteralPath $trionGatePath -Raw -Encoding utf8 } else { '' }
$combatBodySessionText = Get-Content -LiteralPath $combatBodySessionPath -Raw -Encoding utf8
$fireEmitRecordText = Get-Content -LiteralPath $fireEmitRecordPath -Raw -Encoding utf8
$allStageServiceText = @(
    Get-Content -LiteralPath $aimStageServicePath -Raw -Encoding utf8
    Get-Content -LiteralPath $prepareStageServicePath -Raw -Encoding utf8
    Get-Content -LiteralPath $fireStageServicePath -Raw -Encoding utf8
    Get-Content -LiteralPath $projectileInitStageServicePath -Raw -Encoding utf8
    Get-Content -LiteralPath $flightStageServicePath -Raw -Encoding utf8
    Get-Content -LiteralPath $arrivalStageServicePath -Raw -Encoding utf8
    Get-Content -LiteralPath $hitStageServicePath -Raw -Encoding utf8
    Get-Content -LiteralPath $impactStageServicePath -Raw -Encoding utf8
) -join "`n"
$allStageModuleInterfaceText = @(
    Get-Content -LiteralPath $aimStageModulePath -Raw -Encoding utf8
    Get-Content -LiteralPath $prepareStageModulePath -Raw -Encoding utf8
    Get-Content -LiteralPath $fireStageModulePath -Raw -Encoding utf8
    Get-Content -LiteralPath $projectileInitStageModulePath -Raw -Encoding utf8
    Get-Content -LiteralPath $flightStageModulePath -Raw -Encoding utf8
    Get-Content -LiteralPath $arrivalStageModulePath -Raw -Encoding utf8
    Get-Content -LiteralPath $hitStageModulePath -Raw -Encoding utf8
    Get-Content -LiteralPath $impactStageModulePath -Raw -Encoding utf8
) -join "`n"
$flightProtocolText = Get-Content -LiteralPath $flightProtocolPath -Raw -Encoding utf8
$flightProtocolSurfaceExists = Test-Path -LiteralPath $flightProtocolSurfacePath
$flightProtocolSurfaceText = if ($flightProtocolSurfaceExists) { Get-Content -LiteralPath $flightProtocolSurfacePath -Raw -Encoding utf8 } else { '' }
$protocolResultText = Get-Content -LiteralPath $protocolResultPath -Raw -Encoding utf8
$emissionModeExists = Test-Path -LiteralPath $emissionModePath
$emissionPlanExists = Test-Path -LiteralPath $emissionPlanPath
$emissionWindowExists = Test-Path -LiteralPath $emissionWindowPath
$emissionModeText = if ($emissionModeExists) { Get-Content -LiteralPath $emissionModePath -Raw -Encoding utf8 } else { '' }
$emissionPlanText = if ($emissionPlanExists) { Get-Content -LiteralPath $emissionPlanPath -Raw -Encoding utf8 } else { '' }
$emissionWindowText = if ($emissionWindowExists) { Get-Content -LiteralPath $emissionWindowPath -Raw -Encoding utf8 } else { '' }
$aimStageDimensionExists = Test-Path -LiteralPath $aimStageDimensionPath
$prepareStageDimensionExists = Test-Path -LiteralPath $prepareStageDimensionPath
$fireStageDimensionExists = Test-Path -LiteralPath $fireStageDimensionPath
$projectileInitStageDimensionExists = Test-Path -LiteralPath $projectileInitStageDimensionPath
$flightStageDimensionPolicyExists = Test-Path -LiteralPath $flightStageDimensionPolicyPath
$arrivalStageDimensionExists = Test-Path -LiteralPath $arrivalStageDimensionPath
$hitStageDimensionExists = Test-Path -LiteralPath $hitStageDimensionPath
$impactStageDimensionExists = Test-Path -LiteralPath $impactStageDimensionPath
$aimStageDimensionText = if ($aimStageDimensionExists) { Get-Content -LiteralPath $aimStageDimensionPath -Raw -Encoding utf8 } else { '' }
$prepareStageDimensionText = if ($prepareStageDimensionExists) { Get-Content -LiteralPath $prepareStageDimensionPath -Raw -Encoding utf8 } else { '' }
$fireStageDimensionText = if ($fireStageDimensionExists) { Get-Content -LiteralPath $fireStageDimensionPath -Raw -Encoding utf8 } else { '' }
$projectileInitStageDimensionText = if ($projectileInitStageDimensionExists) { Get-Content -LiteralPath $projectileInitStageDimensionPath -Raw -Encoding utf8 } else { '' }
$projectileInitPlanText = Get-Content -LiteralPath $projectileInitPlanPath -Raw -Encoding utf8
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8
$flightStageDimensionPolicyText = if ($flightStageDimensionPolicyExists) { Get-Content -LiteralPath $flightStageDimensionPolicyPath -Raw -Encoding utf8 } else { '' }
$arrivalStageDimensionText = if ($arrivalStageDimensionExists) { Get-Content -LiteralPath $arrivalStageDimensionPath -Raw -Encoding utf8 } else { '' }
$hitStageDimensionText = if ($hitStageDimensionExists) { Get-Content -LiteralPath $hitStageDimensionPath -Raw -Encoding utf8 } else { '' }
$impactStageDimensionText = if ($impactStageDimensionExists) { Get-Content -LiteralPath $impactStageDimensionPath -Raw -Encoding utf8 } else { '' }
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8
$arrivalContributionText = Get-Content -LiteralPath $arrivalContributionPath -Raw -Encoding utf8
$arrivalRecordText = Get-Content -LiteralPath $arrivalRecordPath -Raw -Encoding utf8
$hitContributionText = Get-Content -LiteralPath $hitContributionPath -Raw -Encoding utf8
$hitRecordText = Get-Content -LiteralPath $hitRecordPath -Raw -Encoding utf8

Assert-True $attackProtocolSurfaceExists 'Attack protocol surface access must exist.'
Assert-True $flightProtocolSurfaceExists 'Flight protocol surface access must exist.'
Assert-True $groupedTargetingSourceExists 'GroupedAttackExecutionTargetingSource must exist for grouped manual targeting.'

# New rule:
# 1. module order comes from assembled list order
# 2. same-dimension overrides are resolved by later modules
# 3. no compatibility path for legacy Priority-based arbitration
Assert-True ($allStageServiceText -notmatch '\.Priority\b') 'Stage services must not sort or arbitrate by Priority anymore.'
Assert-True ($allStageModuleInterfaceText -notmatch 'int\s+Priority\s*\{') 'Stage module interfaces must not expose Priority anymore.'
Assert-True ($attackProtocolSurfaceText -match 'CreateAimModules') 'Surface access must remain the assembly owner.'

Assert-True $aimStageDimensionExists 'Aim stage dimension declaration must exist.'
Assert-True $prepareStageDimensionExists 'Prepare stage dimension declaration must exist.'
Assert-True $fireStageDimensionExists 'Fire stage dimension declaration must exist.'
Assert-True $projectileInitStageDimensionExists 'ProjectileInit stage dimension declaration must exist.'
Assert-True $flightStageDimensionPolicyExists 'Flight stage dimension policy must exist.'
Assert-True $arrivalStageDimensionExists 'Arrival stage dimension declaration must exist.'
Assert-True $hitStageDimensionExists 'Hit stage dimension declaration must exist.'
Assert-True $impactStageDimensionExists 'Impact stage dimension declaration must exist.'

Assert-True ($aimStageDimensionText -match 'Target') 'Aim dimensions must declare Target.'
Assert-True ($prepareStageDimensionText -match 'Abort') 'Prepare dimensions must declare Abort.'
Assert-True ($fireStageDimensionText -match 'Projectile') 'Fire dimensions must declare Projectile.'
Assert-True ($projectileInitStageDimensionText -match 'Origin') 'ProjectileInit dimensions must declare Origin.'
Assert-True ($flightStageDimensionPolicyText -match 'FlightDimension') 'Flight dimension policy must bind FlightDimension.'
Assert-True ($arrivalStageDimensionText -match 'ContinueFlight') 'Arrival dimensions must declare ContinueFlight.'
Assert-True ($hitStageDimensionText -match 'HitThing') 'Hit dimensions must declare HitThing.'
Assert-True ($impactStageDimensionText -match 'DirectDamage') 'Impact dimensions must declare DirectDamage.'

Assert-True (
    ($attackProtocolSurfaceText -match 'class\s+RangedAttackProtocolSurfaceAccess') -and
    ($attackProtocolSurfaceText -match 'Resolve\s*\(') -and
    ($attackProtocolSurfaceText -match 'CreateAimModules') -and
    ($attackProtocolSurfaceText -match 'CreatePrepareModules') -and
    ($attackProtocolSurfaceText -match 'CreateFireModules') -and
    ($attackProtocolSurfaceText -match 'CreateProjectileInitModules')
) 'Attack protocol surface access must expose a Resolve entry.'

Assert-True (
    ($flightProtocolSurfaceText -match 'class\s+RangedFlightProtocolSurfaceAccess') -and
    ($flightProtocolSurfaceText -match 'Resolve\s*\(') -and
    ($flightProtocolSurfaceText -match 'CreateFlightModules') -and
    ($flightProtocolSurfaceText -match 'CreateArrivalModules') -and
    ($flightProtocolSurfaceText -match 'CreateHitModules') -and
    ($flightProtocolSurfaceText -match 'CreateImpactModules')
) 'Flight protocol surface access must expose a Resolve entry.'

Assert-True (
    ($triggerRuntimeServicesText -match 'RangedAttackProtocolService\s*=\s*new\s+RangedAttackProtocolService\s*\(') -and
    ($triggerRuntimeServicesText -match 'RangedAttackTrionGate\s*=\s*new\s+RangedAttackTrionGate\s*\(')
) 'TriggerRuntimeServices must remain the owner-held composition root for ranged protocol runtime services.'

Assert-True (
    ($executorText -match 'RangedAttackProtocolSurfaceAccess\.Resolve\s*\(') -and
    ($effectEmitterText -match 'RangedAttackProtocolSurfaceAccess\.Resolve\s*\(') -and
    ($continuationPlannerText -match 'RangedAttackProtocolSurfaceAccess\.Resolve\s*\(') -and
    ($executorText -notmatch 'new\s+RangedAttackProtocolService\s*\(') -and
    ($effectEmitterText -notmatch 'new\s+RangedAttackProtocolService\s*\(') -and
    ($continuationPlannerText -notmatch 'new\s+RangedAttackProtocolService\s*\(') -and
    ($jobDriverText -notmatch 'new\s+RangedAttackProtocolService\s*\(') -and
    ($verbText -notmatch 'new\s+RangedAttackProtocolService\s*\(')
) 'Runtime ranged attack callers must resolve the protocol through owner-held services or the continuation planner, not directly new RangedAttackProtocolService.'

Assert-True (
    ($projectileText -match 'RangedFlightProtocolSurfaceAccess\.Resolve\s*\(') -and
    ($projectileText -notmatch 'new\s+RangedFlightProtocolService\s*\(') -and
    ($projectileText -notmatch 'TryConsume\s*\(')
) 'Unified projectile host must resolve the flight protocol through owner-held services.'

Assert-True (
    $protocolText -match 'TryBuild\s*\(\s*AttackExecutionPreparedContext\s+request\s*,\s*AttackRuntimeStep\s+step\s*,\s*FormalExpressionResult\s+result\s*,\s*out\s+RangedAttackProtocolResult\s+protocolResult\s*\)'
) 'RangedAttackProtocolService.TryBuild must accept AttackExecutionPreparedContext and AttackRuntimeStep.'

Assert-True (
    $protocolText -match 'BuildEntry\s*\(\s*AttackExecutionPreparedContext\s+request\s*,\s*AttackRuntimeStep\s+step\s*,\s*FormalExpressionResult\s+result\s*\)'
) 'RangedAttackProtocolService.BuildEntry must accept AttackExecutionPreparedContext and AttackRuntimeStep.'

Assert-True (
    $executorText -match 'TryBuild\s*\(\s*request\s*,\s*context\.Step\s*,\s*context\.Result\s*,\s*out\s+var\s+protocolResult\s*\)'
) 'DefaultRangedAttackExecutor must pass context.Step into the ranged protocol.'

Assert-True (
    $effectEmitterText -match 'TryBuild\s*\(\s*request\s*,\s*context\.Step\s*,\s*context\.Result\s*,\s*out\s+var\s+protocolResult\s*\)'
) 'AttackEffectEmitter must pass context.Step into the ranged protocol.'

Assert-True (
    ($jobDriverText -match 'PrepareContinuation\(') -and
    ($continuationPlannerText -match 'TryBuild\s*\(\s*preparedContext\s*,\s*context\.Step\s*,\s*context\.Result\s*,\s*out\s+var\s+protocolResult\s*\)')
) 'JobDriver_BdpRangedAttackExecution must route per-step protocol build through the shared continuation planner.'

Assert-True (
    ($verbText -match 'PrepareContinuation\(') -and
    ($continuationPlannerText -match 'TryBuild\s*\(\s*preparedContext\s*,\s*context\.Step\s*,\s*context\.Result\s*,\s*out\s+var\s+protocolResult\s*\)')
) 'BdpVerb_Shoot must pass context.Step into the ranged protocol through the shared continuation planner.'

Assert-True (
    ($targetingSourceText -match 'Messages\.Message\s*\(') -and
    ($targetingSourceText -match 'RejectReason')
) 'AttackExecutionTargetingSource must surface Confirm.RejectReason to the player when confirmation is rejected.'

Assert-True (
    ($prepareRecordText -match 'ResourceCost') -and
    ($prepareRecordText -match 'MinimumRequired')
) 'PrepareRecord must own ranged round Trion semantics.'

Assert-True (
    $prepareModuleExists -and
    ($prepareModuleText -match 'entry\.SourceResult\.Trion')
) 'Ranged round Trion cost derivation must live in prepare-stage modules.'

Assert-True (
    ($verbText -match 'RangedVerbRoundState') -and
    ($roundStateText -match 'RangedAttackTrionGate') -and
    ($verbText -notmatch 'CombatBodySessionService')
) 'BdpVerb_Shoot must consume ranged Trion through RangedVerbRoundState and the ranged gate, not through CombatBodySession.'

Assert-True (
    ($combatBodySessionText -notmatch 'RangedAttackTrionGate') -and
    ($combatBodySessionText -notmatch 'TryAdmitWarmup') -and
    ($combatBodySessionText -notmatch 'TryCommitBeforeFirstEmission')
) 'CombatBodySession must stay outside ranged Trion round charging.'

Assert-True $emissionModeExists 'RangedVerbEmissionMode must exist.'

Assert-True (
    ($emissionModeText -match 'enum\s+RangedVerbEmissionMode') -and
    ($emissionModeText -match 'SimultaneousStep') -and
    ($emissionModeText -match 'SequentialBurst')
) 'RangedVerbEmissionMode must define SimultaneousStep and SequentialBurst.'

Assert-True $emissionPlanExists 'RangedVerbEmissionPlan must exist.'
Assert-True $emissionWindowExists 'RangedVerbEmissionWindowPlan must exist.'

Assert-True (
    ($emissionPlanText -match 'class\s+RangedVerbEmissionPlan') -and
    ($emissionPlanText -match 'IReadOnlyList<RangedVerbEmissionWindowPlan>\s+Windows') -and
    ($emissionPlanText -match 'ExpectedEmitCount')
) 'RangedVerbEmissionPlan must carry ordered emission windows.'

Assert-True (
    ($emissionWindowText -match 'class\s+RangedVerbEmissionWindowPlan') -and
    ($emissionWindowText -match 'RangedVerbEmissionMode\s+EmissionMode') -and
    ($emissionWindowText -match 'IReadOnlyList<ProjectileInitPlan>\s+ProjectilePlans')
) 'RangedVerbEmissionWindowPlan must carry per-window emission mode and projectile plans.'

Assert-True (
    $protocolResultText -match 'RangedVerbEmissionPlan\s+VerbEmissionPlan'
) 'RangedAttackProtocolResult must expose VerbEmissionPlan.'

Assert-True (
    ($executorText -match 'BindVerbEmissionPlan\s*\(') -and
    ($effectEmitterText -match 'BindVerbEmissionPlan\s*\(') -and
    ($continuationPlannerText -match 'BindVerbEmissionPlan\s*\(')
) 'All ranged host binding sites must bind VerbEmissionPlan.'

Assert-True (
    $protocolText -match 'VerbEmissionPlan\s*='
) 'RangedAttackProtocolService must build VerbEmissionPlan.'

Assert-True (
    ($verbText -match 'TryGetCurrentWindow') -and
    ($verbText -match 'window\.EmissionMode\s*==\s*RangedVerbEmissionMode\.SimultaneousStep')
) 'BdpVerb_Shoot must branch on current window emission mode.'

Assert-True (
    $verbText -match 'burstShotsLeft\s*=\s*ResolveRemainingWindowCount'
) 'BdpVerb_Shoot must use remaining emission windows as burst session count.'

Assert-True (
    ($projectileInitPlanText -match 'Vector3\s+OriginOffsetWorld') -and
    ($projectileInitPlanText -match 'bool\s+HasAbsoluteOriginWorld') -and
    ($projectileInitPlanText -match 'Vector3\s+AbsoluteOriginWorld')
) 'ProjectileInitPlan must distinguish baseline relative origin offset from explicit absolute origin override.'

Assert-True (
    ($projectileInitPlanText -match 'float\s+ForcedMissRadius') -and
    ($projectileInitPlanText -match 'float\s+AccuracyFactor') -and
    ($projectileInitPlanText -notmatch 'LockedTarget') -and
    ($projectileInitPlanText -notmatch 'InitialPhase') -and
    ($projectileInitPlanText -notmatch 'PathSeedCells') -and
    ($projectileInitPlanText -notmatch 'GuideSeedCell') -and
    ($projectileInitPlanText -notmatch 'RetargetingEnabled')
) 'ProjectileInitPlan must carry real launch truth and must not keep unused path/lock/retarget placeholders.'

Assert-True (
    $allStageServiceText -notmatch 'entry\s*!=\s*null\s*&&\s*entry\.Pawn\s*!=\s*null\s*\?\s*entry\.Pawn\.DrawPos\s*\+\s*emit\.OriginOffsetWorld'
) 'ProjectileInit baseline must not pre-freeze absolute launch world position from pawn DrawPos.'

Assert-True (
    $allStageServiceText -notmatch 'plan\.HasAbsoluteOriginWorld\s*=\s*true;\s*[\r\n\s]*plan\.AbsoluteOriginWorld\s*=\s*resolution\.RootOriginWorld'
) 'ProjectileInit visual muzzle path must not write visual-driven roots into AbsoluteOriginWorld.'

Assert-True (
    ($verbText -match 'ResolveLaunchRoot') -and
    ($verbText -match 'Vector3\s+theoreticalOrigin\s*=\s*rootOrigin\s*\+\s*plan\.OriginOffsetWorld')
) 'BdpVerb_Shoot must treat launch root origin and OriginOffsetWorld as one offset chain instead of a mutually exclusive choice.'

Assert-True (
    ($attackExecutionEmitText -match 'bool\s+HasOriginSpreadRange') -and
    ($attackExecutionEmitText -match 'float\s+OriginSpreadLateralMin') -and
    ($attackExecutionEmitText -match 'float\s+OriginSpreadLateralMax') -and
    ($attackExecutionEmitText -match 'float\s+OriginSpreadForwardMin') -and
    ($attackExecutionEmitText -match 'float\s+OriginSpreadForwardMax')
) 'AttackExecutionEmit must carry random origin-spread range metadata instead of only a frozen world spread offset.'

Assert-True (
    ($fireEmitRecordText -match 'bool\s+HasOriginSpreadRange') -and
    ($fireEmitRecordText -match 'float\s+OriginSpreadLateralMin') -and
    ($fireEmitRecordText -match 'float\s+OriginSpreadLateralMax') -and
    ($fireEmitRecordText -match 'float\s+OriginSpreadForwardMin') -and
    ($fireEmitRecordText -match 'float\s+OriginSpreadForwardMax')
) 'FireEmitRecord must preserve random origin-spread range metadata through the protocol fire stage.'

Assert-True (
    ($projectileInitPlanText -match 'bool\s+HasOriginSpreadRange') -and
    ($projectileInitPlanText -match 'float\s+OriginSpreadLateralMin') -and
    ($projectileInitPlanText -match 'float\s+OriginSpreadLateralMax') -and
    ($projectileInitPlanText -match 'float\s+OriginSpreadForwardMin') -and
    ($projectileInitPlanText -match 'float\s+OriginSpreadForwardMax')
) 'ProjectileInitPlan must preserve random origin-spread range metadata until the host emits the projectile.'

Assert-True (
    ($attackExecutionStagesText -match 'HasOriginSpreadRange\s*=\s*emit\.HasOriginSpreadRange') -and
    ($attackExecutionStagesText -match 'OriginSpreadLateralMin\s*=\s*emit\.OriginSpreadLateralMin') -and
    ($attackExecutionStagesText -match 'OriginSpreadLateralMax\s*=\s*emit\.OriginSpreadLateralMax') -and
    ($attackExecutionStagesText -match 'OriginSpreadForwardMin\s*=\s*emit\.OriginSpreadForwardMin') -and
    ($attackExecutionStagesText -match 'OriginSpreadForwardMax\s*=\s*emit\.OriginSpreadForwardMax')
) 'CloneWithGroup must preserve declared random spread metadata when regrouping dual casts.'

Assert-True (
    ($groupedTargetingSourceText -match 'class\s+GroupedAttackExecutionTargetingSource\s*:\s*ITargetingSource') -and
    ($groupedTargetingSourceText -match 'IReadOnlyList<AttackExecutionTargetingSource>')
) 'GroupedAttackExecutionTargetingSource must be a thin ITargetingSource adapter over member AttackExecutionTargetingSource entries.'

Assert-True (
    ($groupedTargetingSourceText -match 'public bool CanHitTarget\(LocalTargetInfo target\)') -and
    ($groupedTargetingSourceText -match 'public bool ValidateTarget\(LocalTargetInfo target, bool showMessages = true\)')
) 'GroupedAttackExecutionTargetingSource must expose group-wide hit and validation gates.'

Assert-True (
    ($groupedTargetingSourceText -match 'public void OrderForceTarget\(LocalTargetInfo target\)') -and
    ($groupedTargetingSourceText -match '\.OrderForceTarget\(target\)')
) 'GroupedAttackExecutionTargetingSource must fan out confirmed targets through underlying member targeting sources.'

Assert-True (
    $attackExecutionStagesText -notmatch 'ResolveEmitOriginOffset\s*\('
) 'AttackExecutionService.Stages must not pre-resolve origin spread into world offsets during plan assembly.'

Assert-True (
    ($verbText -match 'ResolveRandomOriginSpreadOffset') -and
    ($verbText -match '!plan\.HasOriginSpreadRange') -and
    ($verbText -match 'Rand\.Range')
) 'BdpVerb_Shoot must resolve declared random origin spread at fire time and only when spread was explicitly declared.'

Assert-True (
    ($verbText -match 'plan\.ForcedMissRadius') -and
    ($verbText -match 'plan\.AccuracyFactor')
) 'BdpVerb_Shoot must consume protocol-resolved forced miss and accuracy truth from ProjectileInitPlan.'

Assert-True (
    ($arrivalContributionText -notmatch 'NewCurrentTarget') -and
    ($arrivalContributionText -notmatch 'NewLockedTarget') -and
    ($arrivalContributionText -notmatch 'ForceGroundImpact') -and
    ($arrivalRecordText -notmatch 'NewCurrentTarget') -and
    ($arrivalRecordText -notmatch 'NewLockedTarget') -and
    ($arrivalRecordText -notmatch 'ForceGroundImpact')
) 'Arrival must shrink to the minimal continue-flight bridge.'

Assert-True (
    ($hitContributionText -notmatch 'ContinueAfterHit') -and
    ($hitContributionText -notmatch 'PassthroughRemaining') -and
    ($hitContributionText -notmatch 'SpawnSecondaryProjectiles') -and
    ($hitRecordText -notmatch 'ContinueAfterHit') -and
    ($hitRecordText -notmatch 'PassthroughRemaining') -and
    ($hitRecordText -notmatch 'SpawnSecondaryProjectiles')
) 'Hit must shrink to the minimal impact snapshot instead of carrying passthrough/secondary-projectile placeholders.'

Write-Output 'RangedProtocolBoundary PASS'

