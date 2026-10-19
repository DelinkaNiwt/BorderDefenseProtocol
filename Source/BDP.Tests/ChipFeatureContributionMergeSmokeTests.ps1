# 芯片功能多来源值裁定冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$mergePath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureContributionMergeService.cs"
$propertyPath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationProperty.cs"

Assert-True (Test-Path -LiteralPath $mergePath) "缺少功能贡献裁定服务。"
Assert-True (Test-Path -LiteralPath $propertyPath) "缺少统一功能属性模型。"

$mergeText = Get-Utf8Text $mergePath
$propertyText = Get-Utf8Text $propertyPath

Assert-True ($mergeText -match "ChipFeatureValueLayer\.Runtime") "缺少运行时值优先级。"
Assert-True ($mergeText -match "ChipFeatureValueLayer\.EffectiveDefinition") "缺少有效定义值优先级。"
Assert-True ($mergeText -match "ChipFeatureValueLayer\.DeclaredDefinition") "缺少声明值优先级。"
Assert-True ($mergeText -match "feature\.property_value_conflict") "同层不同值必须进入独立诊断。"
Assert-True ($mergeText -match "feature\.property_value_type_mismatch") "类型不符必须进入独立诊断。"
Assert-True ($mergeText -match "Sources") "值裁定必须保留来源链。"
Assert-True ($mergeText -match "ValueKind") "值裁定必须校验属性值类型。"
Assert-True ($propertyText -notmatch "\bState\b") "统一属性只保留客观值和来源，不附加解释状态。"
Assert-True ($propertyText -match "ArgumentNullException") "统一属性不得以空值冒充事实。"
Assert-True ($propertyText -match "IReadOnlyList<ChipInformationProvenance>\s+Sources") `
    "统一属性必须保留只读来源链。"

Write-Host "PASS: 芯片功能多来源值裁定规则已表达。"
