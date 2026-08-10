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
$executorPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\DefaultRangedAttackExecutor.cs'
$jobDriverPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'
$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'

$executorText = Get-Content -LiteralPath $executorPath -Raw -Encoding utf8
$jobDriverText = Get-Content -LiteralPath $jobDriverPath -Raw -Encoding utf8
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8

# 原版 AttackStatic 在暂时无法射击时保留 job，直到目标重新满足当前射击条件。
Assert-True (
    $executorText -match 'endIfCantShootTargetFromCurPos\s*=\s*false\s*;'
) 'BDP ranged execution must retain the target job while the target is temporarily unhittable.'

$rangeGuard = [regex]::Match(
    $jobDriverText,
    '(?s)if\s*\(!CanHitCurrentTarget\(verb\)\).*?\{.*?return\s+true\s*;'
)
Assert-True $rangeGuard.Success 'Out-of-range targets must keep the continuous ranged job alive for retry.'

$losGuard = [regex]::Match(
    $jobDriverText,
    '(?s)if\s*\(!canHitTargetFromCurrentPos\).*?\{.*?return\s+true\s*;'
)
Assert-True $losGuard.Success 'Temporarily unavailable line-of-sight must keep the continuous ranged job alive for retry.'

$targetGateIndex = $verbText.IndexOf('if (!canHitTarget)')
$clearPlanIndex = $verbText.IndexOf('ClearPendingEmissionPlan();')
Assert-True (
    $targetGateIndex -ge 0 -and
    $clearPlanIndex -ge 0 -and
    $targetGateIndex -lt $clearPlanIndex
) 'BdpVerb_Shoot must reject an unhittable target before rebuilding or clearing the emission plan.'

Write-Output 'RangedTargetRetention PASS'
