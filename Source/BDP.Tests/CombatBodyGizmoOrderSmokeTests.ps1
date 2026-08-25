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

$combatBodyGizmoPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\External\CombatBodyTriggerGizmoProvider.cs'
$trionGizmoPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\Gizmo_TrionStatus.cs'

$combatBodyGizmoText = Get-Content -LiteralPath $combatBodyGizmoPath -Raw -Encoding utf8
$trionGizmoText = Get-Content -LiteralPath $trionGizmoPath -Raw -Encoding utf8

$combatBodyOrderMatch = [regex]::Match($combatBodyGizmoText, 'private const float GizmoOrder = (-?[0-9]+(?:\.[0-9]+)?)f;')
$trionOrderMatch = [regex]::Match($trionGizmoText, 'Order = (-?[0-9]+(?:\.[0-9]+)?)f;')

Assert-True -Condition $combatBodyOrderMatch.Success -Message '战斗体 Gizmo（操作按钮）必须声明明确的排序值。'
Assert-True -Condition $trionOrderMatch.Success -Message 'Trion（触力能）资源面板必须保留明确的排序值。'

$combatBodyOrder = [float]$combatBodyOrderMatch.Groups[1].Value
$trionOrder = [float]$trionOrderMatch.Groups[1].Value

Assert-True -Condition ($combatBodyGizmoText -match 'command\.Order = GizmoOrder;') -Message '激活/解除战斗体按钮必须使用统一的战斗体排序值。'
Assert-True -Condition ($combatBodyOrder -gt $trionOrder) -Message '战斗体按钮必须排在 Trion（触力能）资源面板之后。'
Assert-True -Condition ($combatBodyOrder -lt 0.0) -Message '战斗体按钮必须排在使用原版默认排序值的其它 BDP 按钮之前。'

Write-Output 'CombatBodyGizmoOrderSmokeTests PASS'
