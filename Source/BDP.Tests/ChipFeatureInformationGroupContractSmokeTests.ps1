# 芯片功能信息组中性契约冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$featureRoot = Join-Path $modRoot "Source\BDP\Core\Information\Features"
$requiredFiles = @(
    "Defs\ChipFeatureGroupDef.cs",
    "Defs\ChipFeatureGroupSlot.cs",
    "Config\ChipFeatureGroupDeclaration.cs",
    "Config\ChipFeatureSubgroupDeclaration.cs",
    "Resolution\ChipFeatureResolver.cs"
)

foreach ($fileName in $requiredFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $featureRoot $fileName)) `
        "缺少芯片功能信息组契约文件：$fileName"
}

$groupDefText = Get-Utf8Text (Join-Path $featureRoot "Defs\ChipFeatureGroupDef.cs")
$groupSlotText = Get-Utf8Text (Join-Path $featureRoot "Defs\ChipFeatureGroupSlot.cs")
$groupDeclarationText = Get-Utf8Text (Join-Path $featureRoot "Config\ChipFeatureGroupDeclaration.cs")
$subgroupDeclarationText = Get-Utf8Text (Join-Path $featureRoot "Config\ChipFeatureSubgroupDeclaration.cs")
$resolverText = Get-Utf8Text (Join-Path $featureRoot "Resolution\ChipFeatureResolver.cs")
$nodeText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationNode.cs")
$propertyText = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationProperty.cs")
$featureDefText = Get-Utf8Text (Join-Path $featureRoot "Defs\ChipFeatureDef.cs")
$declarationText = Get-Utf8Text (Join-Path $featureRoot "Config\ChipFeatureDeclaration.cs")

Assert-True ($groupDefText -match "class\s+ChipFeatureGroupDef\s*:\s*Def") `
    "ChipFeatureGroupDef 必须是 RimWorld Def。"
Assert-True ($groupDefText -match "List<ChipFeaturePropertySlot>\s+Properties") `
    "信息组 Def 必须声明允许接收的属性槽位。"
Assert-True ($groupDefText -match "List<ChipFeatureGroupSlot>\s+Subgroups") `
    "信息组 Def 必须声明允许的一级子组槽位。"
Assert-True ($groupSlotText -match "ChipFeatureGroupDef\s+Group") `
    "功能 Def 必须通过 ChipFeatureGroupSlot 引用信息组 Def。"
Assert-True ($groupSlotText -match "int\s+Order") `
    "信息组槽位必须声明显示顺序。"
Assert-True ($groupDeclarationText -match "string\s+Id") `
    "信息组声明必须具有稳定 Id。"
Assert-True ($groupDeclarationText -match "ChipFeatureGroupDef\s+Group") `
    "信息组声明必须引用信息组 Def。"
Assert-True ($groupDeclarationText -match "string\s+LabelKey") `
    "信息组声明必须支持可选名称语言键。"
Assert-True ($groupDeclarationText -match "List<ChipFeatureSourceConfig>\s+Sources") `
    "信息组声明必须能读取权威属性来源。"
Assert-True ($groupDeclarationText -match "List<ChipFeatureSubgroupDeclaration>\s+Subgroups") `
    "信息组声明必须支持有界的一层子组。"
Assert-True ($subgroupDeclarationText -match "class\s+ChipFeatureSubgroupDeclaration") `
    "一级子组必须使用不可递归的独立声明类型。"
Assert-True ($subgroupDeclarationText -notmatch "Subgroups|Children") `
    "一级子组不得再嵌套子组或完整子功能。"
Assert-True ($featureDefText -match "List<ChipFeatureGroupSlot>\s+Groups") `
    "功能 Def 必须声明允许的信息组槽位。"
Assert-True ($declarationText -match "List<ChipFeatureGroupDeclaration>\s+Groups") `
    "功能实例声明必须支持信息组。"
Assert-True ($resolverText -match '"feature_group"' -and $resolverText -match '"feature_subgroup"') `
    "信息组和一级子组必须直接作为统一节点输出。"
Assert-True ($nodeText -match "IReadOnlyList<ChipInformationNode>\s+Children") `
    "统一节点必须保留功能、组和子组层级。"
Assert-True ($propertyText -match "IReadOnlyList<ChipInformationProvenance>\s+Sources") `
    "统一属性必须保留组属性来源。"
Assert-True ($groupDefText -notmatch "Children") `
    "信息组不得形成第二套任意递归树。"
Assert-True ($groupDeclarationText -notmatch "Children") `
    "信息组声明不得把完整玩家子功能伪装成组子项。"

Write-Host "PASS: Core 芯片功能信息组契约中性且不引入第二套递归树。"
