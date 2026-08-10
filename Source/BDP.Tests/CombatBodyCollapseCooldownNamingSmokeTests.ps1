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

$propsPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompProperties_CombatBodyHost.cs'
$hostPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$coordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Flow\CombatBodyCoordinator.cs'
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$presenterPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\External\CombatBodyCollapseReasonPresenter.cs'

$propsText = Get-Content -LiteralPath $propsPath -Raw -Encoding utf8
$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$coordinatorText = Get-Content -LiteralPath $coordinatorPath -Raw -Encoding utf8
$exitTransactionText = Get-Content -LiteralPath $exitTransactionPath -Raw -Encoding utf8
$presenterText = Get-Content -LiteralPath $presenterPath -Raw -Encoding utf8

Assert-True -Condition ($propsText -match 'public int collapseCooldownTicks = 0;') -Message '宿主配置必须使用中性的 collapseCooldownTicks。'
Assert-True -Condition ($hostText -match 'Props\.collapseCooldownTicks') -Message '宿主初始化必须读取 collapseCooldownTicks。'
Assert-True -Condition ($coordinatorText -match 'private readonly int collapseCooldownTicks;') -Message '战斗体服务必须保存 collapseCooldownTicks。'
Assert-True -Condition ($coordinatorText -match 'int collapseCooldownTicks\)' -and $coordinatorText -match 'internal int CollapseCooldownTicks') -Message '战斗体服务构造参数和读取属性必须使用 CollapseCooldownTicks。'
Assert-True -Condition ($exitTransactionText -match 'rawCombatBodyService\.CollapseCooldownTicks') -Message '崩解退出事务必须读取 CollapseCooldownTicks。'

Assert-True -Condition ($propsText -notmatch 'emergencyCooldownTicks|EmergencyCooldownTicks') -Message '宿主配置不得残留紧急冷却命名。'
Assert-True -Condition ($hostText -notmatch 'emergencyCooldownTicks|EmergencyCooldownTicks') -Message '宿主接线不得残留紧急冷却命名。'
Assert-True -Condition ($coordinatorText -notmatch 'emergencyCooldownTicks|EmergencyCooldownTicks') -Message '战斗体服务不得残留紧急冷却命名。'
Assert-True -Condition ($exitTransactionText -notmatch 'emergencyCooldownTicks|EmergencyCooldownTicks') -Message '崩解退出事务不得残留紧急冷却命名。'

Assert-True -Condition ($presenterText -match 'TrionAvailableDepleted') -Message '通用崩解原因文字映射必须保留。'
Assert-True -Condition ($presenterText -match 'Trion可用值耗尽') -Message 'Trion耗尽原因的玩家提示文字必须保持不变。'

Write-Output 'CombatBodyCollapseCooldownNaming PASS'
