# 芯片制造台原版工作台架构测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$buildingPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Building\Building_ChipFabricator.cs"
$buildingXmlPath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\Buildings\ChipFabricator.xml"
$recipePath = Join-Path $modRoot "1.6\Content\Defs\RecipeDef\ChipProduction.xml"
$workGiverPath = Join-Path $modRoot "1.6\Content\Defs\WorkGiverDef\Assembly.xml"

$buildingText = Get-Utf8Text $buildingPath
$buildingXml = Get-Utf8Text $buildingXmlPath
$workGiverXml = Get-Utf8Text $workGiverPath
Assert-True ($buildingText -match 'class\s+Building_ChipFabricator\s*:\s*Building_WorkTable') "制造台必须继承原版 Building_WorkTable。"
Assert-True ($buildingXml -match '<recipes>[\s\S]*BDP_Recipe_ProduceChip') "制造台必须挂接通用芯片配方。"
Assert-True ($buildingXml -match '<hasInteractionCell>\s*true\s*</hasInteractionCell>') "原版 DoBill 制造台必须显式启用交互格。"
Assert-True ($buildingXml -match '<interactionCellOffset>\s*\(0,\s*0,\s*-1\)\s*</interactionCellOffset>') "芯片制造台交互格必须保持在建筑前方。"
Assert-True ($buildingText -match 'Window_ChipManufacturing') "制造台必须通过 Gizmo 打开专用制造窗口。"
Assert-True ($buildingXml -notmatch 'ITab_ChipManufacturing') "制造台不得继续挂接旧检查页签。"
Assert-True (Test-Path -LiteralPath $recipePath) "缺少通用芯片 RecipeDef。"
$recipeText = Get-Utf8Text $recipePath
Assert-True ($recipeText -match 'RecipeWorker_ChipProduction') "通用配方必须使用动态芯片配方工作器。"
Assert-True ($recipeText -match '<unfinishedThingDef>BDP_Chip_Unfinished</unfinishedThingDef>') "通用配方必须使用原版半成品链。"
Assert-True ($recipeText -match '<allowMixingIngredients>false</allowMixingIngredients>') "通用配方必须禁止混料。"
Assert-True ($workGiverXml -notmatch 'BDP_WorkAtChipFabricator') "旧自制制造 WorkGiver 必须删除。"
Assert-True ($workGiverXml -match 'WorkGiver_DoBill') "制造台必须由原版 DoBill 工作分配器驱动。"
Assert-True ($workGiverXml -match 'BDP_HaulToChipStorage') "芯片仓搬运 WorkGiver 必须保留。"

foreach ($legacyPath in @(
    "Source\BDP.Content\Assembly\Job\JobDriver_UseChipFabricator.cs",
    "Source\BDP.Content\Assembly\Job\JobDriver_WorkAtChipFabricator.cs",
    "Source\BDP.Content\Assembly\Job\WorkGiver_WorkAtChipFabricator.cs",
    "1.6\Content\Defs\JobDef\ChipManufacture.xml"
))
{
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $modRoot $legacyPath))) "旧制造工作流文件必须删除：$legacyPath"
}

Write-Host "PASS: 芯片制造台改为原版工作台、账单、DoBill 和 UnfinishedThing 流程。"
