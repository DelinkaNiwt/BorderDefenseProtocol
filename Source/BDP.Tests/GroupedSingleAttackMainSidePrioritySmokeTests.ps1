$ErrorActionPreference = 'Stop'

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
$memberSourcePath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$groupedSourcePath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\GroupedAttackExecutionTargetingSource.cs'

$memberSourceText = Get-Content -LiteralPath $memberSourcePath -Raw -Encoding utf8
$groupedSourceText = Get-Content -LiteralPath $groupedSourcePath -Raw -Encoding utf8

Assert-True (
    $memberSourceText -match 'internal\s+ExpressionOriginKind\?\s+ResolvedOriginKind'
) '成员目标源必须只读公开正式结果的 OriginKind（来源侧别），供组级派单使用。'

Assert-True (
    ($groupedSourceText -match 'SelectPreferredSourcesByPawn') -and
    ($groupedSourceText -match 'ExpressionOriginKind\.Main') -and
    ($groupedSourceText -match 'ExpressionOriginKind\.Sub')
) '聚合单武器派单必须按 Pawn 选择来源，并明确优先 Main、回退 Sub。'

$orderMethod = [regex]::Match(
    $groupedSourceText,
    '(?s)public void OrderForceTarget\(LocalTargetInfo target\).*?\n        \}')
Assert-True $orderMethod.Success '组级 OrderForceTarget 方法必须存在。'
Assert-True (
    ($orderMethod.Value -match 'SelectPreferredSourcesByPawn\(target\)') -and
    ($orderMethod.Value -notmatch 'for\s*\(int i = 0; i < sources\.Count; i\+\+\)')
) '组级正式下单不得再盲目遍历全部成员，必须只提交每个 Pawn 选中的唯一来源。'

Write-Output 'GroupedSingleAttackMainSidePrioritySmokeTests PASS'
