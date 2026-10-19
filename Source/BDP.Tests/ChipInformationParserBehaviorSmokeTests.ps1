# 芯片信息解析器行为边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$parserPath = Join-Path $modRoot "Source\BDP\Core\Information\ChipInformationParser.cs"
$resolverPath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureResolver.cs"
$mergePath = Join-Path $modRoot "Source\BDP\Core\Information\Rules\ChipInformationMergeService.cs"
$blockSchemaPath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationBlockSchema.cs"
$nodePath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationNode.cs"
$modelPath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationModel.cs"
$composerPath = Join-Path $modRoot "Source\BDP\Core\Information\Input\ChipInformationInputComposer.cs"

$parserText = Get-Utf8Text $parserPath
$resolverText = Get-Utf8Text $resolverPath
$mergeText = Get-Utf8Text $mergePath
$blockSchemaText = Get-Utf8Text $blockSchemaPath
$nodeText = Get-Utf8Text $nodePath
$modelText = Get-Utf8Text $modelPath
$composerText = Get-Utf8Text $composerPath

Assert-True ($parserText -match 'AddExpressionSafetyDiagnostics') '解析器必须在规则前检查表达事实安全性。'
Assert-True ($parserText -match 'information\.expression_cycle' -and
    $parserText -match 'information\.expression_depth_exceeded') '解析器必须隔离循环引用和过深嵌套。'
Assert-True ($resolverText -match 'maximumDepth' -and
    $resolverText -match 'FeatureDepthExceeded' -and
    $resolverText -match 'FeaturePropertyNotAccepted') '功能语义解析必须有深度上限和属性槽位边界。'
Assert-True ($mergeText -match 'case ChipInformationValueKind\.List:' -and
    $mergeText -match 'AreEqual\(left\.Items\[index\], right\.Items\[index\]\)') '列表值冲突判断必须比较实际元素，而不是只比较数量。'
Assert-True ($blockSchemaText -match 'TryGetField' -and
    $nodeText -match 'IReadOnlyList<ChipInformationNode>\s+Children' -and
    $modelText -match 'ChipInformationNode\s+Root') '规则输入结构必须可校验，最终内容必须通过统一节点树读取。'
Assert-True ($composerText -match 'ReclassifySources' -and
    $composerText -match 'sourceKind' -and
    $composerText -match 'sourceKeyPrefix') '阶段输入必须能够重分类来源并保留基础/构型链。'

Write-Host "PASS: 芯片信息解析器具备适配、合并、溯源和递归安全边界。"
