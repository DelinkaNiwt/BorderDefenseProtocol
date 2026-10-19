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
$verbPath = Join-Path $sourceRoot 'BDP\Core\Abilities\BdpVerb_AbilityShoot.cs'
$flowPath = Join-Path $sourceRoot 'BDP.Development\Abilities\FujinRangedStrikeCastFlowDiagnostics.cs'
$verbText = Read-Source $verbPath
$flowText = Read-Source $flowPath

Assert-True ($verbText -match 'class\s+BdpVerb_AbilityShoot\s*:\s*BdpVerb_CastAbility\s*,\s*IBdpExpressionAbilityVerb') 'Fujin must use the BDP Ability Verb base so target confirmation reaches Ability.QueueCastingJob.'
Assert-True ($verbText -notmatch 'class\s+BdpVerb_AbilityShoot\s*:\s*Verb_AbilityShoot') 'Fujin must not use the raw projectile Verb base that routes to AttackStatic.'
Assert-True ($verbText -match 'using\s+BDP\.Support\.Diagnostics;') 'Fujin cast diagnostics must use the shared diagnostics sink.'
Assert-True ($verbText -match 'AttackExecutionThrottled') 'Fujin cast diagnostics must be rate limited.'
Assert-True ($verbText -match 'LogCastDiagnostic\(\s*"cast_enter"') 'Actual TryCastShot entry must be diagnosable.'
Assert-True ($verbText -match 'LogCastDiagnostic\(\s*"start"') 'Cast start must be diagnosable.'
Assert-True ($verbText -match 'surface\.bound_result_missing') 'Missing bound Ability result must be diagnosable.'
Assert-True ($verbText -match 'published_result_missing') 'Missing published result must be diagnosable.'
Assert-True ($verbText -match 'attack_entry_missing') 'Missing attack execution entry must be diagnosable.'
Assert-True ($verbText -match 'runtime_steps_missing') 'Missing runtime steps must be diagnosable.'
Assert-True ($verbText -match 'ranged_step_missing') 'Missing ranged runtime step must be diagnosable.'
Assert-True ($verbText -match 'protocol_missing') 'Missing ranged protocol must be diagnosable.'
Assert-True ($verbText -match 'projectile_plans_empty') 'Empty projectile plans must be diagnosable.'
Assert-True ($verbText -match 'trion_cost_commit_failed') 'Trion cost commit failure must be diagnosable.'
Assert-True ($verbText -match 'projectile_emit_zero') 'Zero emitted projectiles must be diagnosable.'
Assert-True ($verbText -match 'cast_emitted') 'A completed cast must be diagnosable.'

$launchPlansStart = $verbText.IndexOf('private int LaunchPlans(')
$tryLaunchPlanStart = $verbText.IndexOf('private bool TryLaunchPlan(')
Assert-True ($launchPlansStart -ge 0 -and $tryLaunchPlanStart -gt $launchPlansStart) 'LaunchPlans method boundary must remain recognizable.'
$launchPlansText = $verbText.Substring($launchPlansStart, $tryLaunchPlanStart - $launchPlansStart)
Assert-True ($launchPlansText -notmatch 'LogCastDiagnostic') 'Projectile loop must not emit one diagnostic per projectile.'
Assert-True (
    ($launchPlansText -match 'HasOriginSpreadRange') -and
    ($launchPlansText -match 'Rand\.Bool') -and
    ($launchPlansText -match 'originSpreadStartsRight')
) 'Ability projectile loop must prepare one lateral spread side for a multi-projectile burst.'

$tryLaunchPlanEnd = $verbText.IndexOf('private static Vector3 ResolveTargetPoint(', $tryLaunchPlanStart)
Assert-True ($tryLaunchPlanEnd -gt $tryLaunchPlanStart) 'TryLaunchPlan method boundary must remain recognizable.'
$tryLaunchPlanText = $verbText.Substring($tryLaunchPlanStart, $tryLaunchPlanEnd - $tryLaunchPlanStart)
Assert-True (
    ($tryLaunchPlanText -match 'ResolveRandomOriginSpreadOffset') -and
    ($tryLaunchPlanText -match 'plan\.HasOriginSpreadRange')
) 'Ability TryLaunchPlan must consume the declared launch-origin spread.'

Assert-True ($flowText -match 'Command_Ability.*ProcessInput|ProcessInput.*Command_Ability') 'Ability button click boundary must be patched.'
Assert-True ($flowText -match 'ui_button_click') 'Ability button click must be diagnosable.'
Assert-True ($flowText -match 'QueueCastingJob') 'Target-confirmation queue boundary must be patched.'
Assert-True ($flowText -match 'target_confirmed_queue_attempt') 'Target-confirmation queue must be diagnosable.'
Assert-True ($flowText -match 'Verb\.OrderForceTarget') 'Generic Verb target-confirmation boundary must be patched.'
Assert-True ($flowText -match 'target_confirmed_verb_order') 'Generic Verb target-confirmation route must be diagnosable.'
Assert-True ($flowText -match 'target_confirmed_verb_order_returned') 'Generic Verb target-confirmation return must be diagnosable.'
Assert-True ($flowText -match 'TryTakeOrderedJob') 'Generic job queue boundary must be patched.'
Assert-True ($flowText -match 'generic_job_queue_attempt') 'Generic job queue attempt must be diagnosable.'
Assert-True ($flowText -match 'generic_job_queue_result') 'Generic job queue result must be diagnosable.'
Assert-True ($flowText -match 'StartJob') 'Generic job start boundary must be patched.'
Assert-True ($flowText -match 'generic_job_start_attempt') 'Generic job start attempt must be diagnosable.'
Assert-True ($flowText -match 'generic_job_driver_started') 'Generic job driver start must be diagnosable.'
Assert-True ($flowText -match 'SetupToils') 'Generic toil setup boundary must be patched.'
Assert-True ($flowText -match 'generic_toils_setup_enter') 'Generic toil setup entry must be diagnosable.'
Assert-True ($flowText -match 'generic_toils_setup_returned') 'Generic toil setup return must be diagnosable.'
Assert-True ($flowText -match 'ReadyForNextToil') 'Generic toil progression boundary must be patched.'
Assert-True ($flowText -match 'generic_toil_ready_attempt') 'Generic toil progression must be diagnosable.'
Assert-True ($flowText -match 'job_created') 'Ability job creation must be diagnosable.'
Assert-True ($flowText -match 'JobDriver_CastAbility.*Notify_Starting|Notify_Starting.*JobDriver_CastAbility') 'Ability job start boundary must be patched.'
Assert-True ($flowText -match 'job_started') 'Ability job start must be diagnosable.'
Assert-True ($flowText -match 'AttackExecutionThrottled') 'Flow diagnostics must be rate limited.'
Assert-True ($flowText -notmatch 'DriverTick|GizmoUpdateOnMouseover|GizmoOnGUI') 'Flow diagnostics must not hook per-tick or GUI drawing methods.'

Write-Output 'FujinCastDiagnosticsSmokeTests PASS'
