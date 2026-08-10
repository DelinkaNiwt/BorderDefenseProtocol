# Content 成品芯片中性提供器测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$thingRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Thing"
$compPath = Join-Path $thingRoot "CompManufacturedChip.cs"
$propsPath = Join-Path $thingRoot "CompProperties_ManufacturedChip.cs"
$thingPath = Join-Path $thingRoot "Thing_ManufacturedChip.cs"
foreach ($path in @($compPath, $propsPath, $thingPath))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少 Content 成品类型：$path"
}

$compText = Get-Utf8Text $compPath
$thingText = Get-Utf8Text $thingPath
$xmlText = Get-Utf8Text (Join-Path $modRoot "1.6\Content\Defs\ThingDef\Items\Chips.xml")

Assert-True ($compText -match 'IChipInstanceDefinitionProvider') "成品组件必须实现 Core 中性定义提供器。"
Assert-True ($compText -match 'IChipSourceReferenceProvider') "成品组件必须实现 Core 中性来源提供器。"
Assert-True ($compText -match 'IChipCombinationRecordHolder') "成品组件必须实现 Content 组合记录持有器。"
Assert-True ($compText -match 'new\s+ChipCombinationResolver\s*\(\)\.Resolve') "每次读取必须从当前预设重新解析。"
Assert-True ($compText -match 'MissingSource') "来源缺失必须保留物品并提供状态名称。"
Assert-True ($compText -match 'SourceVariantKey') "枪壳必须通过中性来源变体键提供给 Core。"
Assert-True ($thingText -match 'Notify_RecipeProduced') "原版生成成品时必须从当前芯片账单复制组合记录。"
Assert-True ($thingText -match 'LabelNoCount') "成品物品必须显示当前动态名称。"
Assert-True ($xmlText -match 'BDP\.Content\.Assembly\.ChipManufacturing\.Thing\.Thing_ManufacturedChip') "成品 XML 必须改用 Content 物品类型。"
Assert-True ($xmlText -match 'BDP\.Content\.Assembly\.ChipManufacturing\.Thing\.CompProperties_ManufacturedChip') "成品 XML 必须改用 Content 组件属性。"

# 从正式程序集验证组件确实实现三个边界接口。
$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"))
$compType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Thing.CompManufacturedChip", $true)
$definitionProvider = $coreAssembly.GetType("BDP.Core.Chips.IChipInstanceDefinitionProvider", $true)
$sourceProvider = $coreAssembly.GetType("BDP.Core.Chips.IChipSourceReferenceProvider", $true)
$recordHolder = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Model.IChipCombinationRecordHolder", $true)
Assert-True ($definitionProvider.IsAssignableFrom($compType)) "运行时成品组件未实现定义提供器。"
Assert-True ($sourceProvider.IsAssignableFrom($compType)) "运行时成品组件未实现来源提供器。"
Assert-True ($recordHolder.IsAssignableFrom($compType)) "运行时成品组件未实现组合记录持有器。"

Write-Host "PASS: Content 成品组件实时解析组合，并只通过 Core 中性提供器公开配置与来源。"
