$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$contentRoot = Join-Path $sourceRoot 'BDP.Content'

function Read-Utf8([string]$path) {
    return Get-Content -LiteralPath $path -Raw -Encoding utf8
}

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    if (-not $text.Contains($needle)) { throw "FAIL: $message" }
}

function Assert-NotContains([string]$text, [string]$needle, [string]$message) {
    if ($text.Contains($needle)) { throw "FAIL: $message" }
}

$model = Read-Utf8 (Join-Path $contentRoot 'Assembly\ChipManufacturing\UI\ChipManufacturingPreviewModel.cs')
$builder = Read-Utf8 (Join-Path $contentRoot 'Assembly\ChipManufacturing\UI\ChipManufacturingPreviewBuilder.cs')
$panel = Read-Utf8 (Join-Path $contentRoot 'Assembly\ChipManufacturing\UI\ChipManufacturingPreviewPanel.cs')
$detailPanel = Read-Utf8 (Join-Path $contentRoot 'Assembly\Window\Panel_ChipDetail.cs')
$modelBuilder = Read-Utf8 (Join-Path $contentRoot 'Assembly\Window\ChipAssemblyDetailModelBuilder.cs')

Assert-Contains $model 'ChipInformationComparisonModel' '制造预览必须保存共享当前/基准信息模型。'
Assert-Contains $model 'ProductLabel' '制造预览必须保存芯片名称。'
Assert-Contains $model 'StatusText' '制造预览必须保存状态文本。'
Assert-NotContains $model 'ValueText' '制造预览不应再维护旧指标文本模型。'
Assert-NotContains $model 'ArmamentFormMetrics' '制造预览不应单独维护构型指标。'
Assert-NotContains $model 'ArmamentFormAdjustments' '制造预览不应单独维护构型修正。'
Assert-Contains $builder 'BuildManufacturingComparison' '制造预览必须使用共享比较模型构建入口。'
Assert-Contains $panel 'Panel_ChipDetail.DrawManufacturingChipDetail' '制造台必须调用共享详情面板。'
Assert-Contains $detailPanel 'ChipInformationDetailRenderer.DrawContent' '共享详情面板必须调用统一渲染器。'
Assert-Contains $modelBuilder 'ResolveComparisonBaseline' '制造台必须沿用可配置比较基准。'
Assert-Contains $modelBuilder 'ManufacturedChipInformationModelBuilder.Build' '制造台必须使用统一信息模型构建器。'

Write-Output 'ChipManufacturingPreviewSmokeTests PASS'
