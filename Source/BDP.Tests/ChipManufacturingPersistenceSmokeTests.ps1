# 芯片账单、半成品与成品组合记录持久化测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$billPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Bill\Bill_ChipProduction.cs"
$unfinishedPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Thing\Thing_UnfinishedChip.cs"
$compPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Thing\CompManufacturedChip.cs"

foreach ($path in @($billPath, $unfinishedPath, $compPath))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少持久化数据链文件：$path"
}

$billText = Get-Utf8Text $billPath
$unfinishedText = Get-Utf8Text $unfinishedPath
$compText = Get-Utf8Text $compPath

Assert-True ($billText -match 'class\s+Bill_ChipProduction\s*:\s*Bill_ProductionWithUft') "芯片账单必须继承原版半成品生产账单。"
Assert-True ($unfinishedText -match 'class\s+Thing_UnfinishedChip\s*:\s*UnfinishedThing') "芯片半成品必须继承原版 UnfinishedThing。"
foreach ($text in @($billText, $unfinishedText, $compText))
{
    Assert-True ($text -match 'ChipCombinationRecord') "账单、半成品和成品组件必须持有同一种组合记录。"
    Assert-True ($text -match 'ExposeData|PostExposeData') "账单、半成品和成品组件必须保存组合记录。"
}

Assert-True ($billText -match 'ShouldDoNow') "芯片账单必须按当前解析状态决定是否工作。"
Assert-True ($billText -match 'GetWorkAmount') "芯片账单必须提供动态工作量。"
Assert-True ($billText -match 'Notify_BillWorkStarted') "开工时必须把组合与起始工作量写入新半成品。"
Assert-True ($billText -match 'Clone') "复制账单时必须复制组合记录。"
Assert-True ($unfinishedText -match 'StartingWorkAmount') "半成品必须保存开工时总工作量。"
Assert-True ($unfinishedText -notmatch 'Pawn\s+creator|creatorName|sourcePresetDefNames|queuedConfig') "半成品不得另建制作者或旧来源/配置字段。"
Assert-True ($compText -notmatch 'Scribe_(Deep|Values|Collections)\.Look\([^\r\n]*ChipDefinitionConfig|manufacturedConfig') "成品组件不得保存完整解析配置。"

$unfinishedXml = Get-Utf8Text (Join-Path $modRoot "1.6\Content\Defs\ThingDef\Items\ChipsUnfinished.xml")
Assert-True ($unfinishedXml -match 'ParentName="UnfinishedBase"') "芯片半成品应沿用原版 UnfinishedBase。"
Assert-True ($unfinishedXml -match 'BDP\.Content\.Assembly\.Thing_UnfinishedChip') "半成品 XML 必须引用 Content 类型。"

# 从已构建正式程序集验证真实继承关系，避免只靠源码文本误判。
$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"))
$billType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Bill.Bill_ChipProduction", $true)
$unfinishedType = $contentAssembly.GetType("BDP.Content.Assembly.Thing_UnfinishedChip", $true)
Assert-True ($billType.BaseType.FullName -eq "RimWorld.Bill_ProductionWithUft") "运行时芯片账单继承关系不正确。"
Assert-True ($unfinishedType.BaseType.FullName -eq "Verse.UnfinishedThing") "运行时芯片半成品继承关系不正确。"

Write-Host "PASS: 组合记录贯穿原版账单、半成品和成品，开工工作量锁定且不另建制作者绑定。"
