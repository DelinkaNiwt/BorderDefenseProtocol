# 芯片功能语义声明契约冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$featureRoot = Join-Path $modRoot "Source\BDP\Core\Information\Features"
$requiredFiles = @(
    "Defs\ChipFeatureDef.cs",
    "Defs\ChipFeaturePropertyDef.cs",
    "Defs\ChipFeaturePropertySlot.cs",
    "Config\ChipFeatureDeclaration.cs",
    "Config\ChipFeatureSourceConfig.cs"
)

foreach ($fileName in $requiredFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $featureRoot $fileName)) `
        "缺少芯片功能语义契约文件：$fileName"
}

$featureText = ($requiredFiles | ForEach-Object {
    Get-Utf8Text (Join-Path $featureRoot $_)
}) -join "`n"

Assert-True ($featureText -match "class\s+ChipFeatureDef\s*:\s*Def") `
    "ChipFeatureDef 必须是 RimWorld Def。"
Assert-True ($featureText -match "class\s+ChipFeaturePropertyDef\s*:\s*Def") `
    "ChipFeaturePropertyDef 必须是 RimWorld Def。"
Assert-True ($featureText -match "class\s+ChipFeaturePropertySlot") `
    "缺少功能属性槽位。"
Assert-True ($featureText -match "class\s+ChipFeatureDeclaration") `
    "缺少功能实例声明。"
Assert-True ($featureText -match "abstract\s+class\s+ChipFeatureSourceConfig") `
    "来源配置必须支持 XML 多态扩展。"
Assert-True ($featureText -match "List<ChipFeaturePropertySlot>\s+Properties") `
    "功能 Def 必须拥有可被 XML Patch 扩充的属性槽位列表。"
Assert-True ($featureText -match "List<ChipFeatureDeclaration>\s+Children") `
    "功能声明必须支持结构化子功能。"
Assert-True ($featureText -notmatch "Ranged|Melee|Shield|Ability|Hediff|Passive") `
    "Core 功能契约不得内置具体业务语义。"

$chipConfigPath = Join-Path $modRoot "Source\BDP\Core\Chips\Config\ChipDefinitionConfig.cs"
$comboConfigPath = Join-Path $modRoot "Source\BDP\Core\Combos\Config\ComboDefinitionConfig.cs"
Assert-True ((Get-Utf8Text $chipConfigPath) -match "List<ChipFeatureDeclaration>\s+Features") `
    "ChipDefinitionConfig 缺少 Features 声明入口。"
Assert-True ((Get-Utf8Text $comboConfigPath) -match "List<ChipFeatureDeclaration>\s+Features") `
    "ComboDefinitionConfig 缺少 Features 声明入口。"

Write-Host "PASS: Core 芯片功能语义声明契约结构完整且保持业务中性。"
