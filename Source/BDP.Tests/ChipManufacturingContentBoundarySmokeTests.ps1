# 芯片制造最终程序集边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$coreRoot = Join-Path $modRoot "Source\BDP"
$contentRoot = Join-Path $modRoot "Source\BDP.Content"
$coreFiles = Get-ChildItem -LiteralPath $coreRoot -Recurse -Filter "*.cs"
$coreText = ($coreFiles | ForEach-Object { Get-Utf8Text $_.FullName }) -join "`n"

$forbidden = @(
    "ChipPresetDef",
    "GunClassDef",
    "ChipConfigBuilder",
    "ExpressionEntryMergeService",
    "ChipManufacturingSettings",
    "CompManufacturedChip",
    "Thing_CustomChip",
    "gunClassName",
    "gunClassLabel",
    "GunClassName",
    "GunClassLabel"
)
foreach ($name in $forbidden)
{
    Assert-True ($coreText -notmatch [regex]::Escape($name)) "Core 仍含制造业务类型或术语：$name"
}

Assert-True ($coreText -match "IChipInstanceDefinitionProvider") "Core 应保留中性芯片实例提供器。"
Assert-True ($coreText -match "SourceVariantKey") "Core 表达运行时应使用中性来源变体键。"
Assert-True ($coreText -match "SourceVariantLabel") "Core 表达运行时应使用中性来源变体标签。"

$comboSurfaceText = Get-Utf8Text (Join-Path $coreRoot "Core\Combos\Access\ComboSurfaceAccess.cs")
Assert-True ($comboSurfaceText -notmatch "SourceVariantKey|HasGunShell") `
    "Core Combo 准入不得把中性来源变体解释成枪壳业务。"
$collectorText = Get-Utf8Text (Join-Path $coreRoot "Core\Expressions\Pipeline\ExpressionSourceCollector.cs")
$gizmoText = Get-Utf8Text (Join-Path $coreRoot "Core\Expressions\Projection\DefaultManualEntryGizmoResolver.cs")
Assert-True (($collectorText + $gizmoText) -notmatch 'SourceVariantLabel[^\r\n]*型|sourceVariantLabel[^\r\n]*型') `
    "Core 不得给任意中性变体标签强加“型”业务后缀。"

$validatorText = Get-Utf8Text (Join-Path $coreRoot "Core\Combos\Validation\ComboDefinitionValidator.cs")
Assert-True ($validatorText -notmatch "DefDatabase<ChipActionPresetDef>|DefDatabase<ChipPresetDef>") `
    "Core 组合技校验不得查询 Content 动作预设。"

$consumerPaths = @(
    "Trigger\UI\ChipModes\ChipModeGizmoProvider.cs",
    "CombatBody\Escape\CombatBodyEmergencyEscapeBadgeStateResolver.cs"
)
$consumerText = ($consumerPaths | ForEach-Object {
    Get-Utf8Text (Join-Path $contentRoot $_)
}) -join "`n"
Assert-True ($consumerText -notmatch "CompManufacturedChip") "正式消费者不得识别具体制造组件。"
Assert-True ($consumerText -match "ChipInstanceSurfaceAccess") "正式消费者应通过 Core 中性读取面读取芯片。"

Write-Host "PASS: Core 只保留中性芯片实例/来源边界，具体制造业务全部位于 Content。"
