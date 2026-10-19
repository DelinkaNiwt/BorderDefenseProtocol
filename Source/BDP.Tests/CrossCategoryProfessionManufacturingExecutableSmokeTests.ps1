# 职业兼容规则跨主分类运行时冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))
$contentAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Content.dll"))

$professionType = $contentAssembly.GetType(
    "BDP.Content.Assembly.ChipManufacturing.Defs.ChipProfessionDef",
    $true)
$functionType = $contentAssembly.GetType(
    "BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef",
    $true)
$profileType = $coreAssembly.GetType("BDP.Core.Chips.ChipProfileConfig", $true)
$categoryType = $coreAssembly.GetType("BDP.Core.Chips.ChipCategoryDef", $true)
$configType = $coreAssembly.GetType("BDP.Core.Chips.ChipDefinitionConfig", $true)
$professionListType = [System.Collections.Generic.List``1].MakeGenericType([Type[]]@($professionType))
$selectionMethod = $contentAssembly.GetType(
    "BDP.Content.Assembly.ChipManufacturing.Resolution.ChipCombinationSelectionRules",
    $true).GetMethod("CanUseFunction")

$sourceProfession = [Activator]::CreateInstance($professionType)
$sourceProfession.defName = "BDP_ChipProfession_Source"
$acceptingProfession = [Activator]::CreateInstance($professionType)
$acceptingProfession.defName = "BDP_ChipProfession_Accepting"
$acceptingProfession.acceptedSourceProfessions =
    [Activator]::CreateInstance($professionListType)
[void]$acceptingProfession.acceptedSourceProfessions.Add($sourceProfession)
$rejectingProfession = [Activator]::CreateInstance($professionType)
$rejectingProfession.defName = "BDP_ChipProfession_Rejecting"
$rejectingProfession.acceptedSourceProfessions =
    [Activator]::CreateInstance($professionListType)

$function = [Activator]::CreateInstance($functionType)
$function.defName = "BDP_Test_CrossCategoryProfession"
$function.profession = $sourceProfession

foreach ($categoryName in @(
    "BDP_ChipCategory_Weapon",
    "BDP_ChipCategory_Defense",
    "BDP_ChipCategory_GeneralSkill",
    "BDP_ChipCategory_Accessory"))
{
    $category = [Activator]::CreateInstance($categoryType)
    $category.defName = $categoryName
    $profile = [Activator]::CreateInstance($profileType)
    $profile.Category = $category
    $config = [Activator]::CreateInstance($configType)
    $config.Profile = $profile
    $function.config = $config

    $accepted = [bool]$selectionMethod.Invoke(
        $null,
        [object[]]@($acceptingProfession, $function))
    $rejected = [bool]$selectionMethod.Invoke(
        $null,
        [object[]]@($rejectingProfession, $function))
    Assert-True $accepted "$categoryName 中，接纳表允许的职业必须通过。"
    Assert-True (-not $rejected) "$categoryName 中，接纳表拒绝的职业必须拒绝。"
}

$function.profession = $null
$unrestricted = [bool]$selectionMethod.Invoke(
    $null,
    [object[]]@($rejectingProfession, $function))
Assert-True $unrestricted "未声明原生职业的功能必须不限制最终职业。"

Write-Output "CrossCategoryProfessionManufacturingExecutableSmokeTests PASS"
