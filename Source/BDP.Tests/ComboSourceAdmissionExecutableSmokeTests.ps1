$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$indexText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Expressions\Runtime\ComboRuntimeIndex.cs")
Assert-True ($indexText -match "ComboMatchAmbiguous") "多份 Combo 候选同时通过时必须拒绝定义歧义。"

$managedRoot = "C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "UnityEngine.CoreModule.dll"))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managedRoot "Assembly-CSharp.dll"))
$coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot "1.6\Assemblies\BDP.Core.dll"))

$snapshotType = $coreAssembly.GetType("BDP.Core.Combos.ComboSourceAdmissionSnapshot", $true)
$contractType = $coreAssembly.GetType("BDP.Core.Combos.ComboSourceAdmissionContract", $true)
$evaluatorType = $coreAssembly.GetType("BDP.Core.Combos.ComboSourceAdmissionEvaluator", $true)
$indexType = $coreAssembly.GetType("BDP.Core.Expressions.Runtime.ComboRuntimeIndex", $true)
$flags = [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static
$isAllowedMethod = $evaluatorType.GetMethod("IsAllowed", $flags)
$sourceVariantCompatibilityMethod = $evaluatorType.GetMethod("AreSourceVariantsCompatible", $flags)
$matchesMethod = $indexType.GetMethod("MatchesAssignment", $flags)
Assert-True ($null -ne $sourceVariantCompatibilityMethod) "来源准入求值器缺少来源变体配对判断方法。"

function New-StringList
{
    param([string[]]$Values)
    $list = [Collections.Generic.List[string]]::new()
    foreach ($value in $Values) { $list.Add($value) }
    return ,$list.AsReadOnly()
}

function Set-Field
{
    param($Target, [string]$Name, $Value)
    $Target.GetType().GetField($Name, [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance).SetValue($Target, $Value)
}

function New-Snapshot
{
    param([string]$Profession, [string]$Category, [string[]]$Tags, [string]$ArmamentForm)
    $snapshot = [Activator]::CreateInstance($snapshotType, $true)
    Set-Field $snapshot "ProfessionKey" $Profession
    Set-Field $snapshot "CategoryKey" $Category
    Set-Field $snapshot "TagKeys" (New-StringList $Tags)
    Set-Field $snapshot "SourceVariantKey" $ArmamentForm
    return $snapshot
}

function New-Admission
{
    param([string]$Profession, [string[]]$RequiredTags, [string[]]$DeniedTags, [string]$ArmamentForm)
    $contract = [Activator]::CreateInstance($contractType, $true)
    Set-Field $contract "AllowedProfessions" (New-StringList @($Profession))
    Set-Field $contract "DeniedProfessions" (New-StringList @())
    Set-Field $contract "AllowedCategories" (New-StringList @("BDP_ChipCategory_Weapon"))
    Set-Field $contract "DeniedCategories" (New-StringList @())
    Set-Field $contract "AllowedTags" (New-StringList @())
    Set-Field $contract "RequiredTags" (New-StringList $RequiredTags)
    Set-Field $contract "DeniedTags" (New-StringList $DeniedTags)
    Set-Field $contract "AllowedSourceVariants" (New-StringList @($ArmamentForm))
    Set-Field $contract "DeniedSourceVariants" (New-StringList @())
    return $contract
}

$shooter = New-Snapshot "BDP_ChipProfession_Shooter" "BDP_ChipCategory_Weapon" @("Entity", "Offensive") "BDP_GunClass_AssaultRifle"
$gunner = New-Snapshot "BDP_ChipProfession_Gunner" "BDP_ChipCategory_Weapon" @("Entity", "Offensive") "BDP_GunClass_AssaultRifle"
$missingTag = New-Snapshot "BDP_ChipProfession_Shooter" "BDP_ChipCategory_Weapon" @("Entity") "BDP_GunClass_AssaultRifle"
$deniedTag = New-Snapshot "BDP_ChipProfession_Shooter" "BDP_ChipCategory_Weapon" @("Entity", "Offensive", "Energy") "BDP_GunClass_AssaultRifle"
$bare = New-Snapshot "BDP_ChipProfession_Shooter" "BDP_ChipCategory_Weapon" @("Entity", "Offensive") $null
$shooterAdmission = New-Admission "BDP_ChipProfession_Shooter" @("Entity", "Offensive") @("Energy") "BDP_GunClass_AssaultRifle"
$gunnerAdmission = New-Admission "BDP_ChipProfession_Gunner" @("Entity") @() "BDP_GunClass_AssaultRifle"

Assert-True ([bool]$isAllowedMethod.Invoke($null, @($shooter, $shooterAdmission))) "完整射手身份必须通过。"
Assert-True (-not [bool]$isAllowedMethod.Invoke($null, @($gunner, $shooterAdmission))) "枪手最终职业不得冒充射手。"
Assert-True (-not [bool]$isAllowedMethod.Invoke($null, @($missingTag, $shooterAdmission))) "必须标签缺失时必须拒绝。"
Assert-True (-not [bool]$isAllowedMethod.Invoke($null, @($deniedTag, $shooterAdmission))) "标签黑名单命中时必须拒绝。"
Assert-True (-not [bool]$isAllowedMethod.Invoke($null, @($bare, $shooterAdmission))) "非空枪壳白名单必须拒绝裸芯。"
Assert-True ([bool]$isAllowedMethod.Invoke($null, @($bare, $null))) "旧 Combo 空准入必须保持兼容。"

function Assert-SourceVariantPair
{
    param($Left, $Right, [bool]$Expected, [string]$Description)
    $actual = [bool]$sourceVariantCompatibilityMethod.Invoke($null, @($Left, $Right))
    Assert-True ($actual -eq $Expected) $Description
}

Assert-SourceVariantPair $null $null $true "两个来源项均无来源变体时必须允许组合。"
Assert-SourceVariantPair (New-Snapshot "Profession" "Category" @() $null) $null $true "空白来源变体与空快照必须视为无来源变体。"
Assert-SourceVariantPair (New-Snapshot "Profession" "Category" @() "source_form_standard") (New-Snapshot "Profession" "Category" @() "SOURCE_FORM_STANDARD") $true "不区分大小写的同一来源变体必须允许组合。"
Assert-SourceVariantPair (New-Snapshot "Profession" "Category" @() "source_form_standard") (New-Snapshot "Profession" "Category" @() "source_form_precision") $false "不同来源变体必须拒绝组合。"
Assert-SourceVariantPair (New-Snapshot "Profession" "Category" @() "source_form_standard") $null $false "一方有来源变体、另一方无来源变体时必须拒绝组合。"
Assert-SourceVariantPair $null (New-Snapshot "Profession" "Category" @() "source_form_precision") $false "第一来源项无来源变体而第二来源项有来源变体时必须拒绝组合。"

$forward = $matchesMethod.Invoke($null, @("Asteroid", "Viper", $shooter, $gunner, "Asteroid", "Viper", $shooterAdmission, $gunnerAdmission, $true))
$reverse = $matchesMethod.Invoke($null, @("Viper", "Asteroid", $gunner, $shooter, "Viper", "Asteroid", $gunnerAdmission, $shooterAdmission, $true))
Assert-True ([bool]$forward) "第一、第二来源项的正向分配必须通过。"
Assert-True ([bool]$reverse) "第一、第二来源项的反向分配必须通过。"

$standardVariantSnapshot = New-Snapshot "BDP_ChipProfession_Shooter" "BDP_ChipCategory_Weapon" @("Entity") "source_form_standard"
$precisionVariantSnapshot = New-Snapshot "BDP_ChipProfession_Gunner" "BDP_ChipCategory_Weapon" @("Entity") "source_form_precision"
$standardVariantAdmission = New-Admission "BDP_ChipProfession_Shooter" @("Entity") @() "source_form_standard"
$precisionVariantAdmission = New-Admission "BDP_ChipProfession_Gunner" @("Entity") @() "source_form_precision"
$forwardMismatch = $matchesMethod.Invoke($null, @(
    "Asteroid", "Viper", $standardVariantSnapshot, $precisionVariantSnapshot,
    "Asteroid", "Viper", $standardVariantAdmission, $precisionVariantAdmission, $true))
$reverseMismatch = $matchesMethod.Invoke($null, @(
    "Viper", "Asteroid", $precisionVariantSnapshot, $standardVariantSnapshot,
    "Viper", "Asteroid", $precisionVariantAdmission, $standardVariantAdmission, $true))
Assert-True (-not [bool]$forwardMismatch) "第一、第二来源项使用不同来源变体时正向分配必须拒绝。"
Assert-True (-not [bool]$reverseMismatch) "交换 Main/Sub 后使用不同来源变体时反向分配也必须拒绝。"

$legacyMixedVariantMatch = $matchesMethod.Invoke($null, @(
    "Asteroid", "Viper", $standardVariantSnapshot, $precisionVariantSnapshot,
    "Asteroid", "Viper", $standardVariantAdmission, $precisionVariantAdmission, $false))
Assert-True ([bool]$legacyMixedVariantMatch) "显式关闭来源变体一致性条件时必须保留兼容行为。"

Write-Host "PASS: Combo 来源准入真实执行覆盖职业、分类、标签、枪壳、裸芯、旧规则与正反向来源项分配。"
