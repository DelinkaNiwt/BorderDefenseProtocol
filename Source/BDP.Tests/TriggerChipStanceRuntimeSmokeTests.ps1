$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$coreRoot = Join-Path $sourceRoot 'BDP\Core'
$slotPath = Join-Path $coreRoot 'Trigger\State\TriggerSlotState.cs'
$servicePath = Join-Path $coreRoot 'Trigger\Modes\TriggerChipModeService.cs'
$snapshotPath = Join-Path $coreRoot 'Trigger\Modes\ChipStanceOptionSnapshot.cs'
$readerPath = Join-Path $coreRoot 'Trigger\Access\Contracts\ITriggerLoadoutReader.cs'
$commandsPath = Join-Path $coreRoot 'Trigger\Access\Contracts\ITriggerLoadoutCommands.cs'
$readsPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Reads.cs'
$bodyPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.cs'
$integrityPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Integrity.cs'
$dirtyReasonPath = Join-Path $coreRoot 'Trigger\Runtime\ProjectionDirtyReason.cs'

foreach ($path in @($slotPath, $servicePath, $snapshotPath, $readerPath, $commandsPath, $readsPath, $bodyPath, $integrityPath, $dirtyReasonPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('姿态运行时设施缺少文件：' + $path)
}

$slotText = Get-Content -LiteralPath $slotPath -Raw -Encoding UTF8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding UTF8
$snapshotText = Get-Content -LiteralPath $snapshotPath -Raw -Encoding UTF8
$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding UTF8
$commandsText = Get-Content -LiteralPath $commandsPath -Raw -Encoding UTF8
$readsText = Get-Content -LiteralPath $readsPath -Raw -Encoding UTF8
$bodyText = Get-Content -LiteralPath $bodyPath -Raw -Encoding UTF8
$integrityText = Get-Content -LiteralPath $integrityPath -Raw -Encoding UTF8
$dirtyReasonText = Get-Content -LiteralPath $dirtyReasonPath -Raw -Encoding UTF8

Assert-True (
    ($slotText -match 'private\s+string\s+currentStanceKey') -and
    ($slotText -match 'internal\s+string\s+CurrentStanceKey') -and
    ($slotText -match 'SetCurrentStanceKey') -and
    ($slotText -match 'Scribe_Values\.Look\(ref\s+currentStanceKey,\s*"currentStanceKey"')
) '根槽必须拥有并保存唯一的当前姿态真值。'

Assert-True (
    ($slotText -match 'SetLoadedChip\(Thing chip\)[\s\S]*?currentStanceKey\s*=\s*null') -and
    ($slotText -match 'SetActive\(bool active\)[\s\S]*?currentStanceKey\s*=\s*null') -and
    ($slotText -match 'SetDisabled\(bool disabled,[\s\S]*?currentStanceKey\s*=\s*null') -and
    ($slotText -match 'isBindingMirror[\s\S]*?currentStanceKey\s*=\s*null')
) '卸载、停用、禁用和镜像槽都不得残留姿态真值。'

Assert-True (
    ($serviceText -match 'TrySwitchActiveRootStance') -and
    ($serviceText -match 'TryCycleActiveRootStance') -and
    ($serviceText -match 'BuildStanceOptions') -and
    ($serviceText -match 'IsStanceKeyValid')
) '中性形态服务必须提供姿态直切、轮换、选项和合法性读取。'

Assert-True (
    ($serviceText -match 'previousModeKey') -and
    ($serviceText -match 'previousStanceKey') -and
    ($serviceText -match 'ResolveDefaultStanceKey\(targetMode\)') -and
    ($serviceText -match 'SetCurrentModeKey\(previousModeKey\)') -and
    ($serviceText -match 'SetCurrentStanceKey\(previousStanceKey\)')
) '切换形态必须重置目标默认姿态，并在发布失败时一起回滚形态与姿态。'

Assert-True (
    ($snapshotText -match 'string\s+StanceKey') -and
    ($snapshotText -match 'string\s+DisplayLabel') -and
    ($snapshotText -match 'string\s+GizmoIconTexPath')
) '姿态选项必须是只读显示快照。'

Assert-True (
    ($readerText -match 'string\s+GetChipStanceKey\(Thing chip\)') -and
    ($readerText -match 'IReadOnlyList<ChipStanceOptionSnapshot>\s+GetChipStanceOptions\(Thing chip\)') -and
    ($commandsText -match 'RequestSwitchChipStance\(Thing chip,\s*string targetStanceKey\)') -and
    ($commandsText -match 'RequestCycleChipStance\(Thing chip\)')
) '正式读写表面必须完整暴露姿态操作。'

Assert-True (
    ($readsText -match 'CurrentStanceKey') -and
    ($readsText -match 'GetChipStanceOptions') -and
    ($bodyText -match 'TrySwitchActiveRootStance') -and
    ($bodyText -match 'TryCycleActiveRootStance') -and
    ($bodyText -match 'ProjectionDirtyReason\.ChipStanceChanged') -and
    ($dirtyReasonText -match '\bChipStanceChanged\b')
) 'CompTriggerBody 必须从根槽读取姿态，并通过独立失效原因发布姿态切换。'

Assert-True (
    ($integrityText -match 'NormalizeRestoredChipModesAndStances') -and
    ($integrityText -match 'discardedStanceKey') -and
    ($integrityText -match 'trigger\.chip_stance_post_load_fallback')
) '读档恢复必须保留合法姿态，并把失效姿态回退到当前形态默认值。'

Write-Output 'TriggerChipStanceRuntimeSmokeTests PASS'
