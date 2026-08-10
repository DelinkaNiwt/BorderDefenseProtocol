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

$formalResultPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\FormalExpressionResult.cs'
$infoEntryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionInfoProjectionEntry.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$abilitySynchronizerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionAbilityHostSynchronizer.cs'
$hediffSynchronizerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionHediffHostSynchronizer.cs'
$hostSynchronizerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionHostSynchronizer.cs'
$publicationSnapshotPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\ExpressionPublicationSnapshot.cs'
$runtimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$triggerBodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$validatorPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Validation\DefaultChipDefinitionValidator.cs'
$hostHediffPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\BdpExpressionHostHediff.cs'
$abilityCostPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\CompAbilityEffect_BdpTrionCost.cs'

$formalResultText = Get-Content -LiteralPath $formalResultPath -Raw -Encoding utf8
$infoEntryText = Get-Content -LiteralPath $infoEntryPath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$abilitySynchronizerText = Get-Content -LiteralPath $abilitySynchronizerPath -Raw -Encoding utf8
$hediffSynchronizerText = Get-Content -LiteralPath $hediffSynchronizerPath -Raw -Encoding utf8
$hostSynchronizerText = Get-Content -LiteralPath $hostSynchronizerPath -Raw -Encoding utf8
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$hostHediffText = if (Test-Path -LiteralPath $hostHediffPath) { Get-Content -LiteralPath $hostHediffPath -Raw -Encoding utf8 } else { '' }
$abilityCostText = Get-Content -LiteralPath $abilityCostPath -Raw -Encoding utf8

Assert-True -Condition $formalResultText.Contains('public string AbilityDefName { get; set; }') -Message 'Ability 与 Hediff 最小闭环必须继续把 Ability 当成正式表达结果字段。'

Assert-True -Condition $formalResultText.Contains('public string HediffDefName { get; set; }') -Message 'Ability 与 Hediff 最小闭环必须继续把 Hediff 当成正式表达结果字段。'

Assert-True -Condition (
    $formalResultText.Contains('public string HediffApplyModeKey { get; set; }') -and
    (-not $formalResultText.Contains('public string ApplyModeKey { get; set; }'))
) -Message '正式结果层必须统一使用 HediffApplyModeKey，不再保留旧 ApplyModeKey 口径。'

Assert-True -Condition (
    $infoEntryText.Contains('public string HediffApplyModeKey { get; set; }') -and
    (-not $infoEntryText.Contains('public string ApplyModeKey { get; set; }'))
) -Message '说明投影条目必须同步改成 HediffApplyModeKey。'

Assert-True -Condition (
    $expressionSurfaceText.Contains('internal void SyncProjectedHosts(Pawn pawn, ExpressionSnapshot snapshot)') -and
    $expressionSurfaceText.Contains('new DefaultExpressionAbilityHostSynchronizer()') -and
    $expressionSurfaceText.Contains('new DefaultExpressionHediffHostSynchronizer()')
) -Message '宿主同步主链必须继续保持 Trigger → Expression → HostSync → 原版宿主。'

Assert-True -Condition (
    $hostSynchronizerText.Contains('abilitySynchronizer') -and
    $hostSynchronizerText.Contains('hediffSynchronizer') -and
    $hostSynchronizerText.Contains('Sync(pawn') -and
    $hostSynchronizerText.Contains('snapshot')
) -Message '默认宿主同步器必须继续把 Ability 与 Hediff 同步都挂在同一宿主同步入口上。'

Assert-True -Condition (
    $abilitySynchronizerText.Contains('GainAbility(def);') -and
    $abilitySynchronizerText.Contains('RemoveAbility(def);') -and
    $abilitySynchronizerText.Contains('AddedAbilityDefsByPawn') -and
    $abilitySynchronizerText.Contains('SourceResultIds') -and
    $abilitySynchronizerText.Contains('TryResolveBoundAbilityResult')
) -Message 'Ability 最小闭环必须继续通过 GainAbility/RemoveAbility 对齐原版宿主，并记录由表达系统补入的集合。'

Assert-True -Condition (
    $abilityCostText.Contains('TryResolveBoundAbilityResult') -and
    $abilityCostText.Contains('MinimumRequired') -and
    (-not $abilityCostText.Contains('Props.TrionCost'))
) -Message 'Ability 的 BDP Trion 成本必须来自表达结果绑定，不允许继续从 AbilityDef 的组件配置读取。'

Assert-True -Condition (
    (-not $runtimeCoordinatorText.Contains('GainAbility')) -and
    (-not $runtimeCoordinatorText.Contains('RemoveAbility')) -and
    (-not $runtimeCoordinatorText.Contains('AddHediff')) -and
    (-not $runtimeCoordinatorText.Contains('RemoveHediff')) -and
    (-not $triggerBodyText.Contains('GainAbility')) -and
    (-not $triggerBodyText.Contains('RemoveAbility')) -and
    (-not $triggerBodyText.Contains('AddHediff')) -and
    (-not $triggerBodyText.Contains('RemoveHediff'))
) -Message 'Trigger 层不应直接做 Ability/Hediff 宿主副作用，这些行为必须留在宿主同步链。'

Assert-True -Condition (
    $hediffSynchronizerText.Contains('CountToSeverityApplyModeKey') -and
    $hediffSynchronizerText.Contains('"countToSeverity"') -and
    (-not $hediffSynchronizerText.Contains('"stack"')) -and
    (-not $hediffSynchronizerText.Contains('LegacyStackApplyModeKey')) -and
    (-not $hediffSynchronizerText.Contains('IsCountToSeverityMode(')) -and
    $hediffSynchronizerText.Contains('ResultCount') -and
    $hediffSynchronizerText.Contains('SourceResultIds') -and
    $hediffSynchronizerText.Contains('RemoveAllHediffsOfDef')
) -Message 'Hediff 最小闭环必须只保留 countToSeverity 正式语义，不能残留任何 stack 兼容分支，同时保留按 Def 回收的首轮边界。'

Assert-True -Condition (
    Test-Path -LiteralPath $publicationSnapshotPath
) -Message 'Ability 与 Hediff 最小闭环必须提供独立的发布追踪快照结构。'

Assert-True -Condition (
    $hostHediffText.Contains('class BdpExpressionHostHediff') -and
    $hostHediffText.Contains('SyncExpressionResults') -and
    $validatorText.Contains('hediffClass') -and
    $validatorText.Contains('BdpExpressionHostHediff') -and
    (-not $validatorText.Contains('HediffApplyModeKey=stack')) -and
    (-not $validatorText.Contains('过渡兼容')) -and
    (-not $validatorText.Contains('LegacyStack')) -and
    (-not $validatorText.Contains('LooksLikeBdpDedicatedHediffDef')) -and
    (-not $validatorText.Contains('退化为“只保证存在”'))
) -Message '芯片定义校验器必须把 Hediff 宿主边界收成 hediffClass 正式协议，并且不能残留 stack 或退化执行口径。'

Assert-True -Condition (-not $formalResultText.Contains('IChipEffect')) -Message '新 BDP 的 Ability/Hediff 最小闭环不应回退到旧版 IChipEffect 总控。'

Write-Output 'AbilityHediffExpressionMinimalClosureSmokeTests PASS'
