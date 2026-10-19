# 芯片制造最终语言包覆盖测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$languageRoot = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)"
$languageFiles = Get-ChildItem -LiteralPath $languageRoot -Recurse -Filter "*.xml"
$languageText = ($languageFiles | ForEach-Object { Get-Utf8Text $_.FullName }) -join "`n"

$requiredKeys = @(
    "BDP_ChipCategory_Weapon.label",
    "BDP_ChipCategory_Defense.label",
    "BDP_ChipCategory_GeneralSkill.label",
    "BDP_ChipCategory_Accessory.label",
    "BDP_ChipProfession_Attacker.label",
    "BDP_ChipProfession_Shooter.label",
    "BDP_ChipProfession_Gunner.label",
    "BDP_ChipProfession_Sniper.label",
    "BDP_ChipManufacturing_TabLabel",
    "BDP_ChipManufacturing_ArmamentFormSuffix",
    "BDP_ChipManufacturing_Order_Enqueue",
    "BDP_ChipManufacturing_Queue_Waiting",
    "BDP_ChipManufacturing_SourceVariantLabel",
    "BDP_ArmamentForm_Revolver",
    "BDP_ArmamentForm_Revolver_Description",
    "BDP_ArmamentForm_AssaultRifle",
    "BDP_ArmamentForm_AssaultRifle_Description",
    "BDP_ArmamentForm_SubmachineGun",
    "BDP_ArmamentForm_SubmachineGun_Description",
    "BDP_ArmamentForm_Shotgun",
    "BDP_ArmamentForm_Shotgun_Description",
    "BDP_ArmamentForm_TrionCube",
    "BDP_ArmamentForm_TrionCube_Description",
    "BDP_ChipMigration_LetterLabel",
    "BDP_ChipMigration_LetterBody",
    "BDP_InvalidChipRemnant.label",
    "BDP_InvalidChipRemnant.description"
)
foreach ($key in $requiredKeys)
{
    Assert-True ($languageText -match ("<" + [regex]::Escape($key) + ">")) "语言包缺少键：$key"
}

$oldArmamentFormLanguage = Join-Path $languageRoot "DefInjected\ArmamentFormDef\Presets.xml"
Assert-True (-not (Test-Path -LiteralPath $oldArmamentFormLanguage)) "旧 ArmamentFormDef 语言包必须删除。"
Assert-True ($languageText -notmatch 'BDP_ChipManufacturing_MissingSourceLabel|来源暂时缺失而保留') `
    "语言包不得继续提供来源缺失保留文案。"

$migrationRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Migration"
$migrationText = (Get-ChildItem -LiteralPath $migrationRoot -Filter "*.cs" | ForEach-Object {
    Get-Utf8Text $_.FullName
}) -join "`n"
Assert-True ($migrationText -notmatch '"[^"\r\n]*[\u4e00-\u9fff][^"\r\n]*"') `
    "迁移 C# 不得硬编码玩家可见中文文案。"

# 制造台所有预设入口必须共用统一名称解析，避免芯片功能模板退回 Def.LabelCap 英文原文。
$manufacturingUiRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI"
$labelResolverPath = Join-Path $manufacturingUiRoot "ChipManufacturingDefLabelResolver.cs"
$manufacturingWindowPath = Join-Path $manufacturingUiRoot "Window_ChipManufacturing.cs"
$presetInfoPath = Join-Path $manufacturingUiRoot "Window_ChipManufacturingDefInfo.cs"
Assert-True (Test-Path -LiteralPath $labelResolverPath) "制造 UI 缺少统一预设名称解析器。"
$labelResolverText = Get-Utf8Text $labelResolverPath
$manufacturingWindowText = Get-Utf8Text $manufacturingWindowPath
$presetInfoText = Get-Utf8Text $presetInfoPath
Assert-True ($labelResolverText -match 'ChipManufacturingPresetDef[\s\S]*ResolvedLabel') `
    "芯片功能模板名称必须读取 ResolvedLabel。"
Assert-True ($labelResolverText -match 'definition\.LabelCap') `
    "非芯片功能模板必须继续回退原版 LabelCap。"
Assert-True ($manufacturingWindowText -match 'ChipManufacturingDefLabelResolver\.Resolve\(definition\)') `
    "制造预设列表必须使用统一名称解析器。"
Assert-True ($presetInfoText -match 'ChipManufacturingDefLabelResolver\.Resolve\(definition\)') `
    "预设信息弹窗标题必须使用统一名称解析器。"
Assert-True ($labelResolverText -match 'ChipManufacturingPresetDef[\s\S]*ResolvedDescription') `
    "芯片功能模板说明必须读取 ResolvedDescription。"
Assert-True ($presetInfoText -match 'ChipManufacturingDefLabelResolver\.ResolveDescription\(definition\)') `
    "预设信息弹窗说明必须使用统一解析器。"

Write-Host "PASS: 芯片制造分类、职业、面板、状态、迁移信件与遗留物均有语言包覆盖。"
