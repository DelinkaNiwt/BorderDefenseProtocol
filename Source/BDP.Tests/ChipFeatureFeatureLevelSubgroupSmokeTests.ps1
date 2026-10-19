# 功能级子组契约、解析和复制冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$featureRoot = Join-Path $modRoot "Source\BDP\Core\Information\Features"
$featureDef = Get-Utf8Text (Join-Path $featureRoot "Defs\ChipFeatureDef.cs")
$declaration = Get-Utf8Text (Join-Path $featureRoot "Config\ChipFeatureDeclaration.cs")
$node = Get-Utf8Text (Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationNode.cs")
$resolver = Get-Utf8Text (Join-Path $featureRoot "Resolution\ChipFeatureResolver.cs")
$validator = Get-Utf8Text (Join-Path $featureRoot "Validation\ChipFeatureDefinitionValidator.cs")
$projector = Get-Utf8Text (Join-Path $featureRoot "Resolution\ChipFeatureContextProjector.cs")
$mergeRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Resolution"
$featureMerge = Get-Utf8Text (Join-Path $mergeRoot "ChipFeatureMergeService.cs")

Assert-True ($featureDef -match "List<ChipFeatureGroupSlot>\s+Subgroups") `
    "功能语义 Def 必须声明功能整体允许的子组。"
Assert-True ($declaration -match "List<ChipFeatureSubgroupDeclaration>\s+Subgroups") `
    "功能实例必须能声明归属于功能整体的子组。"
Assert-True ($resolver -match '"feature_subgroup"') `
    "功能级子组必须直接作为统一节点输出。"
Assert-True ($node -match "IReadOnlyList<ChipInformationNode>\s+Children") `
    "统一节点必须能容纳功能级子组。"
Assert-True ($resolver -match "declaration\.Feature\.Subgroups") `
    "解析器必须按功能 Def 声明的槽位解析功能级子组。"
Assert-True ($resolver -match 'path\s*\+\s*"/subgroups/') `
    "功能级子组路径必须归属于对应功能实例，不能按共享 Def 合并。"
Assert-True ($validator -match "FeatureSubgroupNotAccepted") `
    "功能语义未允许的功能级子组必须产生独立诊断。"
Assert-True ($validator -match "declaration\.Feature\??\.Subgroups") `
    "结构校验器必须检查功能级子组的允许范围。"
Assert-True ($projector -match "declaration\.Subgroups") `
    "上下文可达性必须纳入功能级子组来源。"
Assert-True ($featureMerge -match "Subgroups\s*=\s*CloneSubgroups\(source\.Subgroups") `
    "制造复制必须独立复制功能级子组，避免双芯片合并时丢失或串组。"

Write-Host "PASS: 功能整体与重复信息组均可独立拥有额外效果子组。"
