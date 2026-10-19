$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$rendererPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs'
$formatterPath = Join-Path $sourceRoot 'BDP.Content\Information\ChipInformationTextFormatter.cs'
$languagePath = Join-Path (Split-Path -Parent $sourceRoot) 'Languages\ChineseSimplified (简体中文)\Keyed\ChipInformation.xml'

$renderer = Get-Content -LiteralPath $rendererPath -Raw -Encoding utf8
$formatter = Get-Content -LiteralPath $formatterPath -Raw -Encoding utf8
$language = Get-Content -LiteralPath $languagePath -Raw -Encoding utf8

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    if (-not $text.Contains($needle)) { throw "FAIL: $message" }
}

function Assert-NotContains([string]$text, [string]$needle, [string]$message) {
    if ($text.Contains($needle)) { throw "FAIL: $message" }
}

# 消耗等尚未被单独建模为语义功能时，必须通过通用信息块兜底展示，不得再由详情 UI 猜技术字段。
Assert-Contains $renderer 'DrawGeneralBlocks' '详情必须保留通用信息块兜底展示。'
Assert-Contains $renderer 'GetFieldText' '通用字段必须经过统一文本格式化。'
Assert-Contains $formatter 'ApplyUnit' '通用字段单位必须由共享格式化器套用。'
Assert-NotContains $renderer 'consumption.UseCosts' '详情不得恢复旧的技术消耗集合布局。'
Assert-NotContains $renderer 'RadarAxisLabelSafePadding' '详情不得继续依赖退役雷达标签布局。'
Assert-Contains $language 'BDP_Info_Field_Consumption' '语言包可以继续为原版信息卡提供消耗标签。'

Write-Output 'TriggerAssemblerConsumptionAndRadarLabels PASS'
