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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

$meleeContextPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\MeleeAttackExecutionContext.cs'
$meleeExecutorPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\DefaultMeleeAttackExecutor.cs'
$meleeJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs'
$meleeVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_MeleeAttackDamage.cs'
$continuationPlannerPath = Join-Path $bdpSourceRoot 'Core\Verbs\MeleeVerbContinuationPlanner.cs'
$diagnosticsPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionDiagnostics.cs'

$meleeContextText = Read-Source $meleeContextPath
$meleeExecutorText = Read-Source $meleeExecutorPath
$meleeJobText = Read-Source $meleeJobPath
$meleeVerbText = Read-Source $meleeVerbPath
$diagnosticsText = Read-Source $diagnosticsPath
$continuationPlannerExists = Test-Path $continuationPlannerPath
$continuationPlannerText = if ($continuationPlannerExists) { Read-Source $continuationPlannerPath } else { '' }

Assert-True (
    $meleeContextText -match 'TryCreateForStepIndex'
) 'MeleeAttackExecutionContext must provide a step-index based entry for rebuilding the current melee run.'

Assert-True (
    $meleeContextText -match 'CollectRunSteps'
) 'MeleeAttackExecutionContext must collect the current continuous melee run instead of assuming the whole request is one host segment.'

Assert-True (
    -not ($meleeContextText -match 'RuntimeSteps\[0\]\s*:\s*null')
) 'MeleeAttackExecutionContext must not hardcode RuntimeSteps[0] as the only melee start point anymore.'

Assert-True (
    ($meleeVerbText -match 'PlanSessionToken') -and
    ($meleeVerbText -match 'NextRuntimeStepIndex') -and
    ($meleeVerbText -match 'AttackContextSnapshot')
) 'BdpVerb_MeleeAttackDamage must persist the minimum melee continuation state: plan token, next step index, and attack context snapshot.'

Assert-True (
    $continuationPlannerExists -and
    ($continuationPlannerText -match 'class\s+MeleeVerbContinuationPlanner') -and
    ($continuationPlannerText -match 'TryPrepareNextRun')
) 'The melee execution chain must add a neutral MeleeVerbContinuationPlanner for next-run continuation.'

Assert-True (
    ($meleeExecutorText -match 'PlanSessionToken') -and
    ($meleeExecutorText -match 'NextRuntimeStepIndex')
) 'DefaultMeleeAttackExecutor must bind melee continuation metadata when starting the first run.'

Assert-True (
    ($meleeJobText -match 'TryPrepareNextRun') -or
    ($meleeJobText -match 'TryContinueWithPreparedRun')
) 'JobDriver_BdpMeleeAttackExecution must attempt to continue into the next melee run after the current run is consumed.'

Assert-True (
    ($diagnosticsText -match 'melee_continuation_prepare') -and
    ($diagnosticsText -match 'melee_continuation_switch') -and
    ($diagnosticsText -match 'melee_continuation_end')
) 'AttackExecutionDiagnostics must expose searchable melee continuation events.'

Write-Output 'MeleeRuntimeStepContinuationSmokeTests PASS'
