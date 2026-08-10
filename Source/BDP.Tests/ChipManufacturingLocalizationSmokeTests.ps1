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
    "BDP_ChipCategory_Ability.label",
    "BDP_ChipCategory_Status.label",
    "BDP_ChipCategory_Passive.label",
    "BDP_ChipProfession_Attacker.label",
    "BDP_ChipProfession_Shooter.label",
    "BDP_ChipProfession_Gunner.label",
    "BDP_ChipProfession_Sniper.label",
    "BDP_ChipManufacturing_TabLabel",
    "BDP_ChipManufacturing_Order_Enqueue",
    "BDP_ChipManufacturing_Queue_Waiting",
    "BDP_ChipManufacturing_MissingSourceLabel",
    "BDP_ChipManufacturing_SourceVariantLabel",
    "BDP_ChipMigration_LetterLabel",
    "BDP_ChipMigration_LetterBody",
    "BDP_InvalidChipRemnant.label",
    "BDP_InvalidChipRemnant.description"
)
foreach ($key in $requiredKeys)
{
    Assert-True ($languageText -match ("<" + [regex]::Escape($key) + ">")) "语言包缺少键：$key"
}

$oldGunClassLanguage = Join-Path $languageRoot "DefInjected\GunClassDef\Presets.xml"
Assert-True (-not (Test-Path -LiteralPath $oldGunClassLanguage)) "旧 GunClassDef 语言包必须删除。"

$migrationRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Migration"
$migrationText = (Get-ChildItem -LiteralPath $migrationRoot -Filter "*.cs" | ForEach-Object {
    Get-Utf8Text $_.FullName
}) -join "`n"
Assert-True ($migrationText -notmatch '"[^"\r\n]*[\u4e00-\u9fff][^"\r\n]*"') `
    "迁移 C# 不得硬编码玩家可见中文文案。"

Write-Host "PASS: 芯片制造分类、职业、面板、状态、迁移信件与遗留物均有语言包覆盖。"
