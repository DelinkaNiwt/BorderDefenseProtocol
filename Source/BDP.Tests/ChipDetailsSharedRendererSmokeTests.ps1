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

$renderer = Read-Utf8 (Join-Path $contentRoot 'Assembly\Window\ChipInformationDetailRenderer.cs')
$triggerPanel = Read-Utf8 (Join-Path $contentRoot 'Assembly\Window\Panel_ChipDetail.cs')
$manufacturingPanel = Read-Utf8 (Join-Path $contentRoot 'Assembly\ChipManufacturing\UI\ChipManufacturingPreviewPanel.cs')
$manufacturingModel = Read-Utf8 (Join-Path $contentRoot 'Assembly\ChipManufacturing\UI\ChipManufacturingPreviewModel.cs')

Assert-Contains $triggerPanel 'ChipInformationDetailRenderer.DrawContent' '装配台必须使用共享详情渲染器。'
Assert-Contains $manufacturingPanel 'Panel_ChipDetail.DrawManufacturingChipDetail' '制造台必须接入同一详情渲染器。'
Assert-Contains $renderer 'DrawContextTree' '形态和姿态必须递归保留父子树并默认展开。'
Assert-Contains $renderer 'DrawFeatureCharts' '图表必须作为所属功能的子项绘制。'
Assert-Contains $renderer 'DrawRadarChart' '攻击指标必须绘制雷达图。'
Assert-Contains $renderer 'DrawAccuracyChart' '远程精度必须绘制独立走势图。'
Assert-Contains $renderer 'DrawRangeRing' '非攻击功能有效距离组合必须绘制范围环。'
Assert-Contains $renderer 'DrawBlockArc' '护盾抵挡角度必须绘制扇形示意。'
Assert-Contains $renderer 'new[] { 3f, 12f, 25f, 40f }' '精度折线必须使用旧版 3、12、25、40 格锚点。'
Assert-Contains $renderer 'new[] { 0f, 3f, 12f, 25f, 40f, 50f }' '精度折线必须显示旧版距离刻度。'
Assert-Contains $renderer 'for (int distance = 0; distance <= 50; distance++)' '精度折线必须按旧版 0 到 50 格插值绘制。'
Assert-Contains $renderer 'identity.source_profession' '雷达标尺必须从现有信息模型读取原版职业键。'
Assert-Contains $renderer 'baselineValueAvailability' '雷达基准线必须保留旧版逐轴缺失值的可见状态。'
Assert-Contains $renderer 'ShowRadarDecreaseSegments = true' '雷达图必须保留旧版逐轴下降差值虚线。'
Assert-Contains $renderer 'BDP_ChipProfession_Sniper' '雷达标尺必须保留狙击手职业分支。'
Assert-Contains $renderer 'new RadarMetric("BDP_Property_DamageAmount", "BDP_Info_Field_ProjectileDamage", 60f)' '雷达图必须使用旧版投射物伤害标签和狙击手量程。'
Assert-Contains $renderer 'new RadarMetric("BDP_Property_ProjectileSpeed", "BDP_Info_Field_ProjectileSpeed", 200f)' '雷达图必须使用旧版投射物速度标签和狙击手量程。'
Assert-Contains $renderer 'circleSegments = 36' '护盾扇形必须绘制旧版 36 段参考圆。'
Assert-Contains $renderer 'new Color(coverageColor.r, coverageColor.g, coverageColor.b, 0.22f)' '护盾角度必须绘制旧版半透明扇形填充。'
Assert-Contains $renderer 'BDP_Window_TriggerAssembly_Radar' '雷达图标题必须使用旧版文案键。'
Assert-Contains $renderer 'BDP_Window_TriggerAssembly_AccuracyCurve' '精度图标题必须使用旧版文案键。'
if ($renderer -match '(?s)private static void DrawRadarChart\(.*?new Rect\(rect\.x \+ rect\.width - 144f') {
    throw 'FAIL: 雷达图不得显示新版当前/基准图例。'
}
Assert-Contains $renderer 'ReadDeclaredRelations' '组合关系必须读取静态声明关系。'
Assert-Contains $renderer 'BDP_Info_Relation_DeclaredResult' '组合关系必须显示声明结果。'
Assert-NotContains $renderer 'BDP_Info_Feature_NoProperties' '合法无属性时不显示多余提示。'
Assert-NotContains $renderer 'model.Root?.Key' '不能把根节点内部键作为玩家身份文字。'
Assert-NotContains $manufacturingModel 'ArmamentFormMetrics' '制造预览不再单独展示构型指标。'
Assert-NotContains $manufacturingModel 'ArmamentFormAdjustments' '制造预览不再单独展示构型修正。'

Write-Output 'ChipDetailsSharedRendererSmokeTests PASS'
