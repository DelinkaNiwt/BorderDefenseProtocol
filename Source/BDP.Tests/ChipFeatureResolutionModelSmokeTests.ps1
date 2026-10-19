# 芯片功能解析结果模型冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$featureResolutionRoot = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution"
$nodePath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationNode.cs"
$propertyPath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationProperty.cs"
$requestPath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationRequest.cs"
$modelPath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationModel.cs"
Assert-True (Test-Path -LiteralPath $nodePath) "缺少统一信息节点。"
Assert-True (Test-Path -LiteralPath $propertyPath) "缺少统一信息属性。"

Assert-True (Test-Path -LiteralPath $requestPath) "缺少 ChipInformationRequest。"

$resolverText = Get-Utf8Text (Join-Path $featureResolutionRoot "ChipFeatureResolver.cs")
$nodeText = Get-Utf8Text $nodePath
$propertyText = Get-Utf8Text $propertyPath
$modelText = Get-Utf8Text $modelPath
$requestText = Get-Utf8Text $requestPath
Assert-True ($resolverText -match '"feature"' -and $resolverText -match "ChipInformationNode") `
    "功能解析必须直接输出统一节点。"
Assert-True ($nodeText -match "DefinitionKey" -and $nodeText -match "Children") `
    "功能节点必须保留来源定义和子节点。"
Assert-True ($propertyText -match "ValueKind" -and $propertyText -match "State") `
    "功能属性必须保留值类型和状态。"
Assert-True ($requestText -match "DeclaredConfig" -and $requestText -match "EffectiveConfig") `
    "统一请求必须区分声明配置和有效配置。"
Assert-True ($requestText -match "Runtime") "统一请求必须支持可选运行时快照。"
Assert-True ($requestText -match "CurrentModeKey" -and $requestText -match "CurrentStanceKey") `
    "统一请求必须携带当前形态和姿态键。"
Assert-True ($modelText -match "ChipInformationNode\s+Root") `
    "芯片信息模型必须只通过统一根节点输出内容。"
Assert-True ($modelText -notmatch "GeneralBlocks|ResolvedChipFeature|Contexts") `
    "芯片信息模型不得再保存并列结果集合。"
Assert-True ($modelText -notmatch "Dictionary<string,\s*object>") `
    "功能结果不得退化为无类型字典。"

Write-Host "PASS: 芯片功能解析结果模型和统一请求边界完整。"
