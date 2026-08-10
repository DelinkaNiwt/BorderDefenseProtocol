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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$attackExecutionDiagnosticsPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionDiagnostics.cs'
$attackEffectDiagnosticsPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackEffectDiagnostics.cs'
$rangedProtocolDiagnosticsPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\RangedProtocol\RangedAttackProtocolDiagnostics.cs'
$rangedFlightDiagnosticsPath = Join-Path $bdpSourceRoot 'Core\Projectiles\RangedFlightProtocol\RangedFlightProtocolDiagnostics.cs'
$rangedExecutorPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\DefaultRangedAttackExecutor.cs'
$meleeExecutorPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\DefaultMeleeAttackExecutor.cs'
$jobDriverRangedPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'
$shootVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_Shoot.cs'
$formalHostShootPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_FormalHostShoot.cs'
$formalHostMeleePath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_FormalHostMelee.cs'
$formalHostManagerPath = Join-Path $bdpSourceRoot 'Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$runtimeCoordinatorPath = Join-Path $bdpSourceRoot 'Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$triggerBodyPath = Join-Path $bdpSourceRoot 'Core\Trigger\State\CompTriggerBody.cs'
$equipmentTickPatchPath = Join-Path $bdpSourceRoot 'Patches\Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs'
$surfaceAccessPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$continuationPlannerPath = Join-Path $bdpSourceRoot 'Core\Verbs\RangedVerbContinuationPlanner.cs'
$postLoadRecoveryPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionPostLoadRecovery.cs'
$attackStagesPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionService.Stages.cs'
$protocolPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'

$attackExecutionDiagnosticsText = Get-Content -LiteralPath $attackExecutionDiagnosticsPath -Raw -Encoding utf8
$rangedProtocolDiagnosticsText = Get-Content -LiteralPath $rangedProtocolDiagnosticsPath -Raw -Encoding utf8
$rangedExecutorText = Get-Content -LiteralPath $rangedExecutorPath -Raw -Encoding utf8
$meleeExecutorText = Get-Content -LiteralPath $meleeExecutorPath -Raw -Encoding utf8
$jobDriverRangedText = Get-Content -LiteralPath $jobDriverRangedPath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$formalHostShootText = Get-Content -LiteralPath $formalHostShootPath -Raw -Encoding utf8
$formalHostMeleeText = Get-Content -LiteralPath $formalHostMeleePath -Raw -Encoding utf8
$formalHostManagerText = Get-Content -LiteralPath $formalHostManagerPath -Raw -Encoding utf8
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8
$equipmentTickPatchText = Get-Content -LiteralPath $equipmentTickPatchPath -Raw -Encoding utf8
$surfaceAccessText = Get-Content -LiteralPath $surfaceAccessPath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8
$postLoadRecoveryText = Get-Content -LiteralPath $postLoadRecoveryPath -Raw -Encoding utf8
$attackStagesText = Get-Content -LiteralPath $attackStagesPath -Raw -Encoding utf8
$protocolText = Get-Content -LiteralPath $protocolPath -Raw -Encoding utf8

$removedMethodNames = @(
    'LogResolved',
    'LogPlanBuilt',
    'LogDispatch',
    'LogGroupDispatch',
    'LogRangedContext',
    'LogMeleeContext',
    'LogRangedCastStart',
    'LogRangedCastPose',
    'LogMeleeCastStart',
    'LogRangedLaunchOrigin'
)

foreach ($methodName in $removedMethodNames) {
    Assert-True (
        $attackExecutionDiagnosticsText -notmatch ("public static void " + [regex]::Escape($methodName) + "\(")
    ) ("AttackExecutionDiagnostics must delete noisy trace method: " + $methodName)
}

$requiredMethodNames = @(
    'LogRangedExecutionStart',
    'LogMeleeExecutionStart',
    'LogVerbEmissionSummary',
    'LogStalePendingEmissionPlanCleared',
    'LogPreparedTargetMismatch',
    'LogFormalHostRebind',
    'LogContinuationSessionResolved',
    'LogHostSessionBound',
    'LogPostLoadSessionValidation',
    'LogVerbCastAttempt',
    'LogVerbCastResult',
    'LogContinuousJobCastAttempt',
    'LogContinuousJobCastResult',
    'LogDualRangedPlanStart',
    'LogDualRangedSideLegality',
    'LogDualRangedPlanResult',
    'LogDualRangedHostLosProbe'
)

foreach ($methodName in $requiredMethodNames) {
    Assert-True (
        $attackExecutionDiagnosticsText -match ("public static void " + [regex]::Escape($methodName) + "\(")
    ) ("AttackExecutionDiagnostics must provide concise summary/anomaly log: " + $methodName)
}

Assert-True (
    -not (Test-Path -LiteralPath $attackEffectDiagnosticsPath)
) 'Routine AttackEffectDiagnostics trace file must be deleted entirely.'

Assert-True (
    $rangedProtocolDiagnosticsText -notmatch 'LogSummary\('
) 'RangedAttackProtocolDiagnostics must delete the routine protocol summary log.'

Assert-True (
    $rangedProtocolDiagnosticsText -match 'LogFailure\('
) 'RangedAttackProtocolDiagnostics must keep the protocol failure log.'

Assert-True (
    -not (Test-Path -LiteralPath $rangedFlightDiagnosticsPath)
) 'Routine projectile flight diagnostics file must be deleted entirely.'

$removedKeywords = @(
    'attack.auto_ranged.missing_host',
    'expression.snapshot_missing_loadout_reader',
    '表达快照构建失败：缺少 Trigger loadout reader',
    'stage=resolved',
    'stage=plan',
    'stage=dispatch',
    'stage=group_dispatch',
    'stage=executor_context',
    'executor_start_continuous_force_order',
    'executor_start_continuous',
    'executor_start_immediate',
    'stage=cast_start',
    'cast_pose_before_start',
    'stage=verb_emission',
    'stage=launch_origin',
    'stage=step_emit',
    'stage=emit',
    'stage=cast_emit',
    'stage=effect_apply',
    'projectile_flight_tick',
    'explosive_flight_tick',
    'projectile_impact',
    'explosive_impact',
    'ranged_protocol_built'
)

$sourceFiles = Get-ChildItem -LiteralPath $bdpSourceRoot -Recurse -File -Filter *.cs
foreach ($file in $sourceFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    foreach ($keyword in $removedKeywords) {
        Assert-True (
            $text -notmatch [regex]::Escape($keyword)
        ) ("Source must delete noisy logging keyword '" + $keyword + "' from " + $file.FullName)
    }
}

Assert-True (
    $rangedExecutorText -match 'LogRangedExecutionStart\('
) 'DefaultRangedAttackExecutor must emit the concise ranged start summary.'

Assert-True (
    $meleeExecutorText -match 'LogMeleeExecutionStart\('
) 'DefaultMeleeAttackExecutor must emit the concise melee start summary.'

Assert-True (
    $shootVerbText -match 'LogVerbEmissionSummary\('
) 'BdpVerb_Shoot must emit the concise verb emission summary.'

Assert-True (
    $shootVerbText -match 'LogStalePendingEmissionPlanCleared\('
) 'BdpVerb_Shoot must log when a stale pending emission plan is cleared.'

Assert-True (
    $shootVerbText -match 'LogPreparedTargetMismatch\('
) 'BdpVerb_Shoot must log the prepared-target mismatch anomaly.'

Assert-True (
    ($formalHostShootText -match 'LogFormalHostRebind\(') -or
    ($formalHostMeleeText -match 'LogFormalHostRebind\(')
) 'Formal host shells must log binding rebind changes when the binding truth actually changes.'

Assert-True (
    ($attackExecutionDiagnosticsText -notmatch 'event=auto_ranged_seed') -and
    ($surfaceAccessText -notmatch 'LogAutoRangedVerbSessionSeeded\(')
) 'Auto-ranged seed diagnostics must not spam every vanilla attack-verb query.'

Assert-True (
    $continuationPlannerText -match 'LogContinuationSessionResolved\('
) 'RangedVerbContinuationPlanner must log which session source continuation actually consumed.'

Assert-True (
    $shootVerbText -match 'LogHostSessionBound\('
) 'BdpVerb_Shoot must log host-session bind snapshots when execution context is applied.'

Assert-True (
    $postLoadRecoveryText -match 'LogPostLoadSessionValidation\('
) 'AttackExecutionPostLoadRecovery must log post-load/session validation outcomes.'

Assert-True (
    $shootVerbText -match 'LogVerbCastAttempt\('
) 'BdpVerb_Shoot must log cast-attempt preconditions for both auto and manual routes.'

Assert-True (
    $shootVerbText -match 'LogVerbCastResult\('
) 'BdpVerb_Shoot must log cast-attempt results after calling base.TryStartCastOn.'

Assert-True (
    ($attackExecutionDiagnosticsText -notmatch 'LogWarmupCompleteState\(') -and
    ($shootVerbText -notmatch 'LogWarmupCompleteState\(') -and
    ($attackExecutionDiagnosticsText -notmatch 'event=warmup_complete_state')
) 'Temporary warmup-complete trace logs must be removed after root-cause investigation ends.'

Assert-True (
    $jobDriverRangedText -match 'LogContinuousJobCastAttempt\('
) 'JobDriver_BdpRangedAttackExecution must log manual continuous-job cast attempts.'

Assert-True (
    $jobDriverRangedText -match 'LogContinuousJobCastResult\('
) 'JobDriver_BdpRangedAttackExecution must log manual continuous-job cast results.'

Assert-True (
    ($attackExecutionDiagnosticsText -notmatch 'event=formal_host_queue') -and
    ($attackExecutionDiagnosticsText -notmatch 'event=formal_host_verb_tick') -and
    ($formalHostManagerText -notmatch 'LogFormalHostQueueSnapshot\(') -and
    ($formalHostManagerText -notmatch 'LogFormalHostVerbTick\(')
) 'Formal host queue and VerbTick diagnostics must be removed from routine per-tick paths.'

Assert-True (
    ($attackExecutionDiagnosticsText -match 'event=dual_ranged_plan_result') -and
    ($attackExecutionDiagnosticsText -match 'event=dual_ranged_host_los_probe') -and
    ($attackExecutionDiagnosticsText -match 'effectiveCanHit=') -and
    ($attackStagesText -match 'ResolveRangedStepHostResultId') -and
    ($formalHostShootText -match 'TryEvaluateDualAdapterLegality')
) 'Dual ranged diagnostics must cover plan pruning, effective host ownership, and adapter-side legality.'

Assert-True (
    ($protocolText -match 'ShouldUseDualSourceLaneIsolation') -and
    ($protocolText -match 'TryBuildDualSourceLaneProtocol') -and
    ($protocolText -match 'BuildMergedVerbEmissionPlan') -and
    ($rangedExecutorText -notmatch 'DualSourceLane') -and
    ($attackStagesText -notmatch 'BuildDualSourceLane')
) 'Dual source-lane isolation must stay inside the ranged protocol layer and must not leak into executor or staged planner.'

Assert-True (
    ($attackExecutionDiagnosticsText -notmatch 'LogVerbTryCastShot\(') -and
    ($shootVerbText -notmatch 'LogVerbTryCastShot\(') -and
    ($attackExecutionDiagnosticsText -notmatch 'event=verb_try_cast_shot')
) 'Temporary TryCastShot cursor/state trace logs must be removed after diagnostics are complete.'

Assert-True (
    ($attackExecutionDiagnosticsText -notmatch 'LogVerbEmitPlan\(') -and
    ($shootVerbText -notmatch 'LogVerbEmitPlan\(') -and
    ($attackExecutionDiagnosticsText -notmatch 'event=verb_emit_plan')
) 'Temporary TryEmitPlan per-plan trace logs must be removed after diagnostics are complete.'

Assert-True (
    ($attackExecutionDiagnosticsText -notmatch 'LogProjectileInitOriginSnapshot\(') -and
    ($attackExecutionDiagnosticsText -notmatch 'event=projectile_init_origin_snapshot') -and
    ($attackExecutionDiagnosticsText -notmatch 'LogEmitOriginEvidence\(') -and
    ($attackExecutionDiagnosticsText -notmatch 'event=emit_origin_evidence')
) 'Temporary projectile-origin investigation logs must be removed after launch-root diagnostics are complete.'

Assert-True (
    ($triggerBodyText -notmatch 'HasFormalHostRuntimeForDiagnostics\(') -and
    ($triggerBodyText -notmatch 'LogFormalHostRuntimeStateForDiagnostics\(') -and
    ($runtimeCoordinatorText -notmatch 'LogFormalHostRuntimeStateForDiagnostics\(') -and
    ($equipmentTickPatchText -notmatch 'LogFormalHostRuntimeStateForDiagnostics\(')
) 'Runtime/equipment tick bridge diagnostics must not emit per-tick formal-host snapshots.'

Write-Output 'AttackExecutionLoggingSmokeTests PASS'
