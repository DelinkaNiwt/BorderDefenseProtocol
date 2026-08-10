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

$meleeContextPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\MeleeAttackExecutionContext.cs'
$meleeJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs'
$meleeVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_MeleeAttackDamage.cs'
$attackStagesPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionService.Stages.cs'
$meleeContextText = Get-Content -LiteralPath $meleeContextPath -Raw -Encoding utf8
$meleeJobText = Get-Content -LiteralPath $meleeJobPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8
$attackStagesText = Get-Content -LiteralPath $attackStagesPath -Raw -Encoding utf8

Assert-True (
    $attackStagesText -match 'AppendMeleeSteps'
) 'AttackExecutionService must still expand melee into per-cast runtime steps.'

Assert-True (
    $attackStagesText -match 'IntervalTicksAfter = cast\.IntervalTicksAfter'
) 'Melee runtime steps must still carry per-step interval timing.'

Assert-True (
    $meleeContextText -match 'return\s+int\.MaxValue;'
) 'MeleeAttackExecutionContext must keep ForceTargetOrder and AutoAttackOrder as continuous melee orders across combo rounds.'

Assert-True (
    $meleeJobText -match 'nextStepDelayTicks'
) 'JobDriver_BdpMeleeAttackExecution must persist an explicit per-step delay counter.'

Assert-True (
    ($meleeJobText -match 'currentStepIndex') -or
    ($meleeJobText -match 'nextStepIndex')
) 'JobDriver_BdpMeleeAttackExecution must persist an explicit melee step cursor.'

Assert-True (
    $meleeJobText -match 'IntervalTicksAfter'
) 'JobDriver_BdpMeleeAttackExecution must consume melee step interval timing instead of chaining attacks back-to-back.'

Assert-True (
    $meleeVerbText -match 'ResolvePreparedStepCount'
) 'BdpVerb_MeleeAttackDamage must expose prepared melee combo step count for round-local cursor cycling.'

Assert-True (
    ($meleeJobText -match '%\s*stepCount') -or
    ($meleeJobText -match 'currentStepIndex\s*=\s*0')
) 'JobDriver_BdpMeleeAttackExecution must cycle combo step cursor across rounds instead of exhausting it once.'

Write-Output 'MeleeMultiHitStepScheduling PASS'
