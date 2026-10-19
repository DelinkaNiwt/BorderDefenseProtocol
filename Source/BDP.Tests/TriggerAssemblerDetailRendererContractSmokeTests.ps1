$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$rendererPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs'
$registryPath = Join-Path $sourceRoot 'BDP.Content\Information\Features\Rendering\ChipFeatureRendererRegistry.cs'
$panelPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Panel_ChipDetail.cs'
$builderPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\ChipAssemblyDetailModelBuilder.cs'

function Read-Source([string]$path) {
    return Get-Content -LiteralPath $path -Raw -Encoding utf8
}

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    if (-not $text.Contains($needle)) { throw "FAIL: $message" }
}

function Assert-NotContains([string]$text, [string]$needle, [string]$message) {
    if ($text.Contains($needle)) { throw "FAIL: $message" }
}

$renderer = Read-Source $rendererPath
$registry = Read-Source $registryPath
$panel = Read-Source $panelPath
$builder = Read-Source $builderPath

Assert-Contains $renderer 'FeatureRendererRegistry' '详情渲染器必须通过语义展示器注册表。'
Assert-Contains $renderer 'RenderAll' '详情渲染器必须统一读取已解析功能。'
Assert-Contains $renderer 'DrawFeatureSection' '详情渲染器必须按功能区绘制。'
Assert-Contains $renderer 'DrawGeneralBlocks' '一般信息必须保留通用字段兜底。'
Assert-Contains $renderer 'MeasureContentHeight' '详情渲染器必须提供滚动区高度测量。'
Assert-NotContains $renderer 'RangedVerbInformationRule' '详情渲染器不得识别旧技术规则。'
Assert-NotContains $renderer 'IsPrototypeChannel' '详情渲染器不得按旧原型通道白名单推断功能。'
Assert-NotContains $renderer 'GetShieldPresentationBlock' '详情渲染器不得按旧护盾技术块识别功能。'
Assert-Contains $registry 'RangedAttackFeatureRenderer' '展示器注册表必须包含远程攻击语义。'
Assert-Contains $registry 'MeleeAttackFeatureRenderer' '展示器注册表必须包含近战攻击语义。'
Assert-Contains $registry 'ShieldFeatureRenderer' '展示器注册表必须包含护盾语义。'
Assert-Contains $registry 'GenericChipFeatureRenderer' '展示器注册表必须保留未知语义通用兜底。'
Assert-Contains $panel 'DrawScrollableContent' '详情面板必须使用共享滚动内容绘制。'
Assert-Contains $renderer 'DrawContextTree' '详情面板必须保留形态和姿态树形关系。'
Assert-Contains $builder 'ChipInformationInstanceRequestAdapter' '详情模型必须从非成品芯片统一构造信息请求。'

Write-Output 'TriggerAssemblerDetailRendererContract PASS'
