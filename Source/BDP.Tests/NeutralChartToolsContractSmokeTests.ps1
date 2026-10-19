$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$chartRoot = Join-Path $sourceRoot 'BDP\Core\UI\Charts'
$contentWindowRoot = Join-Path $sourceRoot 'BDP.Content\Assembly\Window'
$detailRendererPath = Join-Path $contentWindowRoot 'ChipInformationDetailRenderer.cs'
$developmentChartRoot = Join-Path $sourceRoot 'BDP.Development\UI\Charts'
$developmentLanguagePath = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Development\Languages\ChineseSimplified (简体中文)\Keyed\ChartTools.xml'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) {
        throw "FAIL: $message"
    }
}

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    Assert-True $text.Contains($needle) $message
}

Assert-True (Test-Path -LiteralPath $chartRoot) 'Core 必须存在中性图表工具目录。'

$requiredFiles = @(
    'ChartPoint.cs',
    'ChartRange.cs',
    'ChartSeriesStyle.cs',
    'ChartStyle.cs',
    'ChartViewport.cs',
    'ChartDrawingContext.cs',
    'ChartCanvas.cs',
    'ChartGeometryRenderer.cs',
    'RadarChartData.cs',
    'RadarChartRenderer.cs',
    'LineChartData.cs',
    'LineChartRenderer.cs'
)
foreach ($fileName in $requiredFiles) {
    Assert-True (Test-Path -LiteralPath (Join-Path $chartRoot $fileName)) "缺少图表工具文件：$fileName。"
}

$chartSource = (Get-ChildItem -LiteralPath $chartRoot -Filter '*.cs' -File | ForEach-Object {
    Get-Content -Raw -LiteralPath $_.FullName
}) -join "`n"
Assert-Contains $chartSource 'namespace BDP.Core.UI.Charts' '图表工具必须属于 Core 中性命名空间。'
Assert-Contains $chartSource 'public static class RadarChartRenderer' '雷达图必须提供公开渲染器。'
Assert-Contains $chartSource 'public static class LineChartRenderer' '折线图必须提供公开渲染器。'
Assert-Contains $chartSource 'public sealed class ChartCanvas' '图表必须提供公开受控画布。'
Assert-Contains $chartSource 'public sealed class ChartDrawingContext' '图表必须提供公开绘图上下文。'
Assert-Contains $chartSource 'public sealed class RadarChartData' '雷达图必须提供公开数据模型。'
Assert-Contains $chartSource 'public sealed class LineChartData' '折线图必须提供公开数据模型。'
Assert-Contains $chartSource 'public sealed class ChartViewport' '图表必须提供公开坐标视口。'
Assert-Contains $chartSource 'internal static class ChartGeometryRenderer' '底层几何实现必须保持内部。'
Assert-Contains $chartSource 'clipPolygon' '通用几何必须允许按多边形边界裁切图形。'
Assert-Contains $chartSource 'VisibleAxes' '通用雷达数据必须允许标记缺失轴并中断基准线。'
Assert-Contains $chartSource 'ShowRadarDecreaseSegments' '通用雷达图必须允许显示轴向基准差值。'
Assert-Contains $chartSource 'XValueFormatter' '通用折线图必须允许横纵轴使用不同刻度格式。'

foreach ($forbidden in @('ChipInformation', 'ChipFeature', 'Verb', 'Hediff', 'Ability', 'BDP.Content')) {
    Assert-True (-not $chartSource.Contains($forbidden)) "Core 图表工具不得耦合业务词：$forbidden。"
}

Assert-True (Test-Path -LiteralPath $contentWindowRoot) '正式 Content 窗口目录必须存在。'
Assert-True (Test-Path -LiteralPath $detailRendererPath) '正式详情渲染器必须存在。'
$detailRendererSource = Get-Content -Raw -LiteralPath $detailRendererPath
Assert-Contains $detailRendererSource 'using BDP.Core.UI.Charts;' '正式详情渲染器必须引用 Core 图表工具。'
Assert-Contains $detailRendererSource 'RadarChartRenderer.Draw' '正式详情渲染器必须复用雷达图工具。'
Assert-Contains $detailRendererSource 'LineChartRenderer.Draw' '正式详情渲染器必须复用折线图工具。'
Assert-Contains $detailRendererSource 'new[] { 3f, 12f, 25f, 40f }' '精度图必须保留原版四个距离锚点。'
Assert-Contains $detailRendererSource 'new[] { 0f, 3f, 12f, 25f, 40f, 50f }' '精度图必须保留原版距离刻度。'
Assert-True ($detailRendererSource -notmatch 'DrawRadarRing|DrawRadarPolygon|DrawLineSeries') '正式详情渲染器不得重复实现雷达/折线图几何绘制。'

Assert-True (Test-Path -LiteralPath (Join-Path $developmentChartRoot 'Window_ChartToolsPreview.cs')) 'Development 必须提供图表工具视觉预览。'
Assert-True (Test-Path -LiteralPath (Join-Path $developmentChartRoot 'ChartToolsDebugActions.cs')) 'Development 必须提供图表工具调试入口。'
Assert-True (Test-Path -LiteralPath $developmentLanguagePath) 'Development 图表预览文案必须进入语言包。'
$developmentSource = (Get-ChildItem -LiteralPath $developmentChartRoot -Filter '*.cs' -File | ForEach-Object {
    Get-Content -Raw -LiteralPath $_.FullName
}) -join "`n"
Assert-Contains $developmentSource 'RadarChartRenderer.Draw' 'Development 预览必须实际调用雷达图工具。'
Assert-Contains $developmentSource 'LineChartRenderer.Draw' 'Development 预览必须实际调用折线图工具。'

Write-Output 'NeutralChartToolsContractSmokeTests PASS'
