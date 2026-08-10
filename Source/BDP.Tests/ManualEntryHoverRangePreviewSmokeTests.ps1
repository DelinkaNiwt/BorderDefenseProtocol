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
$commandPath = Join-Path $sourceRoot 'BDP\Core\Expressions\Projection\Command_BdpManualEntryTarget.cs'
$targetingSourcePath = Join-Path $sourceRoot 'BDP\Core\AttackExecution\AttackExecutionTargetingSource.cs'
$commandText = Get-Content -LiteralPath $commandPath -Raw -Encoding utf8
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8

Assert-True (
    $commandText -match 'public override void GizmoUpdateOnMouseover\(\)' -and
    $commandText -match 'DrawGizmoRangePreview\(\)' -and
    $commandText -match 'foreach \(AttackExecutionTargetingSource groupedSource in groupedTargetingSources\)'
) '手动攻击按钮必须在悬停时预览自身及已合并武器的全部射程。'

Assert-True (
    $commandText -match 'public override void MergeWith\(Gizmo other\)' -and
    $commandText -match 'base\.MergeWith\(other\);' -and
    $commandText -match 'AddGroupedTargetingSource\(groupedCommand\.targetingSource\);' -and
    $commandText -match 'AddGroupedTargetingSource\(groupedSource\);'
) '手动攻击按钮必须在原版 Gizmo 合并阶段收集同组武器，供悬停预览使用。'

Assert-True (
    $targetingSourceText -match 'internal void DrawGizmoRangePreview\(\)' -and
    $targetingSourceText -match 'DrawVanillaRangeRing\(ResolveCurrentContext\(\)\.Verb\);'
) '目标选择适配源必须集中复用原版射程圈绘制，避免按钮层复制攻击范围逻辑。'

Write-Output 'ManualEntryHoverRangePreviewSmokeTests PASS'
