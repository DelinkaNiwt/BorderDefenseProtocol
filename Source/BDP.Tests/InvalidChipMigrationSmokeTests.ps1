# 明确非法芯片统一废弃物迁移测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$migrationRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Migration"
$requiredFiles = @(
    "GameComponent_ChipManufacturingMigration.cs",
    "InvalidChipItemCollector.cs",
    "InvalidChipReplacementService.cs",
    "InvalidChipPlacementService.cs",
    "InvalidChipMigrationReport.cs",
    "TriggerInvalidChipEvacuationAdapter.cs"
)
foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $migrationRoot $fileName)) "缺少非法芯片迁移组件：$fileName"
}

$componentText = Get-Utf8Text (Join-Path $migrationRoot "GameComponent_ChipManufacturingMigration.cs")
$collectorText = Get-Utf8Text (Join-Path $migrationRoot "InvalidChipItemCollector.cs")
$replacementText = Get-Utf8Text (Join-Path $migrationRoot "InvalidChipReplacementService.cs")
$placementText = Get-Utf8Text (Join-Path $migrationRoot "InvalidChipPlacementService.cs")
$reportText = Get-Utf8Text (Join-Path $migrationRoot "InvalidChipMigrationReport.cs")
$triggerText = Get-Utf8Text (Join-Path $migrationRoot "TriggerInvalidChipEvacuationAdapter.cs")

Assert-True ($componentText -match 'LoadedGame') "迁移必须在读档完成后的安全入口执行。"
Assert-True ($componentText -match 'completedThisSession') "同一游戏会话只能扫描一次。"
Assert-True ($componentText -match 'ReceiveLetter') "整次扫描必须只在组件末尾发送一封汇总信。"
Assert-True ($collectorText -match 'ThingID') "跨地图与容器收集必须按 ThingID 去重。"
Assert-True ($collectorText -match 'GetAllThingsRecursively') "收集器必须递归覆盖 ThingOwner。"
Assert-True ($replacementText -match 'ChipCombinationResolutionStatus\.Invalid') "只允许替换明确 Invalid 物品。"
Assert-True ($replacementText -match 'ChipCombinationResolutionStatus\.MissingSource') "来源缺失物品必须识别并保留。"
Assert-True ($replacementText -match 'BDP_InvalidChipRemnant') "所有明确非法物品必须变成同一种废弃物。"
Assert-True ($replacementText -notmatch 'refund|返还|StartingWorkAmount|ingredients') "迁移不得返还或读取旧材料与工作量。"
Assert-True ($placementText -match 'DestroyMode\.Vanish') "旧物品必须直接消失，不触发半成品取消返还。"
Assert-True ($placementText -match 'RollbackPlacedRemnant') "遗留物预放置后若原物移除失败，必须可以回滚。"
Assert-True ($placementText -match 'RestoreOriginalOwner') "离地图容器替换失败时必须恢复原物，禁止静默丢失。"
Assert-True ($placementText -match 'InterruptRelatedPawnJobs') "销毁正在加工或携带的半成品前必须安全结束相关工作。"
Assert-True ($triggerText -match 'TryDestroyLoadedChip') "触发体内非法芯片必须先经正式命令安全卸除引用。"
Assert-True ($reportText -match 'ReplacedItemCount') "报告必须汇总物品替换数。"
Assert-True ($reportText -match 'DeletedBillCount') "报告必须汇总非法账单删除数。"
Assert-True ($reportText -match 'PreservedMissingSourceCount') "报告必须汇总来源缺失保留数。"

$remnantPath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Items\InvalidChipRemnant.xml"
Assert-True (Test-Path -LiteralPath $remnantPath) "缺少统一废弃物 ThingDef。"
$remnantText = Get-Utf8Text $remnantPath
Assert-True ($remnantText -match '<defName>BDP_InvalidChipRemnant</defName>') "废弃物 DefName 不正确。"
Assert-True ($remnantText -match '<stackLimit>[2-9][0-9]*</stackLimit>') "废弃物必须是普通可堆叽物品。"
Assert-True ($remnantText -match '<tradeability>None</tradeability>') "废弃物不得贸易。"
Assert-True ($remnantText -match '<smeltProducts>[\s\S]*BDP_InvalidChipRemnant') "熔炼废弃物只能得到同一种废弃物。"
Assert-True ($remnantText -notmatch '<thingClass>|CompArt|Art|Install|ChipDefinitionConfig') "废弃物不得有自定义类、艺术、安装或芯片运行组件。"

Write-Host "PASS: 明确非法物品一对一变为统一普通废弃物，来源缺失保留，非法账单删除且整次只汇总一封信。"
