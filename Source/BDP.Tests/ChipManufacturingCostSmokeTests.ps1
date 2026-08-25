# 芯片制造动态材料与工作量冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$recipeRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Recipe"
$requiredFiles = @(
    "ChipManufacturingCost.cs",
    "ChipManufacturingCostCalculator.cs",
    "ChipRecipeIngredientUniverse.cs",
    "RecipeWorker_ChipProduction.cs"
)
foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $recipeRoot $fileName)) "缺少动态成本组件：$fileName"
}

$costText = Get-Utf8Text (Join-Path $recipeRoot "ChipManufacturingCost.cs")
$calculatorText = Get-Utf8Text (Join-Path $recipeRoot "ChipManufacturingCostCalculator.cs")
$universeText = Get-Utf8Text (Join-Path $recipeRoot "ChipRecipeIngredientUniverse.cs")
$workerText = Get-Utf8Text (Join-Path $recipeRoot "RecipeWorker_ChipProduction.cs")

Assert-True ($costText -match 'List<ThingDefCountClass>\s+Ingredients') "成本结果必须提供合并后的具体材料。"
Assert-True ($costText -match 'float\s+WorkAmount') "成本结果必须提供单枚总工作量。"
Assert-True ($calculatorText -match 'recipe\.workAmount') "总工作量必须包含通用配方基础工作量。"
Assert-True ($calculatorText -match 'costList') "总材料必须累加每个动作材料。"
Assert-True ($calculatorText -match 'additionalCost') "总材料必须最多累加一次武装型材料。"
Assert-True ($calculatorText -match 'additionalWorkAmount') "工作量必须累加动作与武装型附加工时。"
Assert-True ($calculatorText -match 'Dictionary<ThingDef,\s*int>') "同类材料必须按 ThingDef 合并。"

Assert-True ($universeText -match 'DefDatabase<ChipActionPresetDef>') "材料槽全集必须包含所有动作出现过的材料。"
Assert-True ($universeText -match 'DefDatabase<ChipArmamentFormDef>') "材料槽全集必须包含所有武装型出现过的材料。"
Assert-True ($universeText -match 'allowMixingIngredients\s*=\s*false') "芯片配方必须禁止混合材料。"
Assert-True ($workerText -match 'Bill_ChipProduction') "动态材料数只允许芯片生产账单读取。"
Assert-True ($workerText -match 'GetIngredientCount') "配方工作器必须覆盖原版材料计数入口。"
Assert-True ($workerText -match 'base\.GetIngredientCount') "普通账单必须回退原版。"

function Assert-Equal
{
    param($Actual, $Expected, [string]$Message)
    if ($Actual -ne $Expected) { throw "$Message 实际=$Actual，预期=$Expected" }
}

function New-TypedList
{
    param([Type]$ElementType, [object[]]$Items = @())
    $listType = [System.Collections.Generic.List``1].MakeGenericType([Type[]]@($ElementType))
    $list = [Activator]::CreateInstance($listType)
    foreach ($item in $Items) { [void]$list.Add($item) }
    return ,$list
}

# 加载正式程序集，实际构造基础配方、两个动作与一个武装型验证累加结果。
$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.dll"))
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"))

$thingDefType = $gameAssembly.GetType("Verse.ThingDef", $true)
$countType = $gameAssembly.GetType("Verse.ThingDefCountClass", $true)
$recipeDefType = $gameAssembly.GetType("Verse.RecipeDef", $true)
$chipBase = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($thingDefType)
$steel = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($thingDefType)
$silver = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($thingDefType)
$recipe = [Activator]::CreateInstance($recipeDefType)
$recipe.workAmount = 1000

function New-Count
{
    param($ThingDef, [int]$Count)
    return [Activator]::CreateInstance($countType, @($ThingDef, $Count))
}

$baseCosts = New-TypedList $countType @((New-Count $chipBase 1))
$universeType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Recipe.ChipRecipeIngredientUniverse", $true)
$baseField = $universeType.GetField("BaseIngredients", [Reflection.BindingFlags]"NonPublic,Static")
$baseField.GetValue($null).Add($recipe, $baseCosts)

$actionType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef", $true)
$actionA = [Activator]::CreateInstance($actionType)
$actionA.costList = New-TypedList $countType @((New-Count $steel 10), (New-Count $silver 15))
$actionA.additionalWorkAmount = 500
$actionB = [Activator]::CreateInstance($actionType)
$actionB.costList = New-TypedList $countType @((New-Count $steel 5), (New-Count $silver 25))
$actionB.additionalWorkAmount = 300
$actions = New-TypedList $actionType @($actionA, $actionB)

$shellType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef", $true)
$shell = [Activator]::CreateInstance($shellType)
$shell.additionalCost = New-TypedList $countType @((New-Count $steel 20))
$shell.additionalWorkAmount = 800

$calculatorType = $contentAssembly.GetType("BDP.Content.Assembly.ChipManufacturing.Recipe.ChipManufacturingCostCalculator", $true)
$calculateMethod = $calculatorType.GetMethods() |
    Where-Object { $_.Name -eq "Calculate" -and $_.GetParameters().Count -eq 3 } |
    Select-Object -First 1
$cost = $calculateMethod.Invoke($null, @($recipe, $actions, $shell))
Assert-Equal ($cost.CountOf($chipBase)) 1 "基础材料必须保留。"
Assert-Equal ($cost.CountOf($steel)) 35 "动作与武装型的同类钢铁必须相加。"
Assert-Equal ($cost.CountOf($silver)) 40 "两个动作的同类白银必须相加。"
Assert-Equal $cost.WorkAmount 2600 "工作量必须按基础、动作和武装型相加。"
Assert-Equal $cost.Ingredients.Count 3 "同类材料必须合并为一个条目。"

Write-Host "PASS: 芯片单枚成本由基础配方、全部动作和可选武装型简单累加，同类材料合并。"
