# 芯片信息模型来源链冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$informationRoot = Join-Path $modRoot "Source\BDP\Core\Information"
$provenancePath = Join-Path $informationRoot "Model\ChipInformationProvenance.cs"
$propertyPath = Join-Path $informationRoot "Model\ChipInformationProperty.cs"
$contributionPath = Join-Path $informationRoot "Model\ChipInformationContributionKind.cs"
$sourcePath = Join-Path $informationRoot "Model\ChipInformationSourceKind.cs"

Assert-True (Test-Path -LiteralPath $provenancePath) "缺少来源链模型。"
Assert-True (Test-Path -LiteralPath $propertyPath) "缺少统一属性模型。"
Assert-True (Test-Path -LiteralPath $contributionPath) "缺少字段贡献类型。"
Assert-True (Test-Path -LiteralPath $sourcePath) "缺少字段来源类型。"

$provenanceText = Get-Utf8Text $provenancePath
$propertyText = Get-Utf8Text $propertyPath
$contributionText = Get-Utf8Text $contributionPath
$sourceText = Get-Utf8Text $sourcePath
Assert-True ($provenanceText -match "SourceKey|SourceKind") "来源链必须保留稳定来源键和来源类别。"
Assert-True ($provenanceText -match "Value|Operation|Applied") "来源链必须保留贡献值、操作和是否生效。"
Assert-True ($propertyText -match "Sources") "统一属性必须可以读取来源链。"
Assert-True ($contributionText -match "Base|Variant|Multiplier|Replacement|Derived") "贡献类型必须覆盖基础值、构型值和通用运算。"
Assert-True ($sourceText -match "Definition|Variant|Runtime|Derived") "来源类型必须覆盖定义、构型、运行和派生来源。"

Write-Host "PASS: 字段可以区分基础定义、构型贡献、运行结果和最终值。"
