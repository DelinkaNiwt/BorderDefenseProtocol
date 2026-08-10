# 芯片制造中栏规格与动作属性预览测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$uiRoot = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI"
$requiredFiles = @(
    "ChipManufacturingPreviewModel.cs",
    "ChipManufacturingPreviewBuilder.cs",
    "ChipManufacturingPreviewPanel.cs",
    "ChipMetricBarScale.cs"
)
foreach ($fileName in $requiredFiles)
{
    Assert-True (Test-Path -LiteralPath (Join-Path $uiRoot $fileName)) "缺少中栏预览组件：$fileName"
}

$modelText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingPreviewModel.cs")
$builderText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingPreviewBuilder.cs")
$panelText = Get-Utf8Text (Join-Path $uiRoot "ChipManufacturingPreviewPanel.cs")
$scaleText = Get-Utf8Text (Join-Path $uiRoot "ChipMetricBarScale.cs")
$windowText = Get-Utf8Text (Join-Path $uiRoot "Window_ChipManufacturing.cs")
$languageText = Get-Utf8Text (Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml")

foreach ($field in @("ValueText", "NormalizedValue", "ShowBar", "IsModified"))
{
    Assert-True ($modelText -match $field) "可视化指标缺少字段：$field"
}
Assert-True ($modelText -match 'Specifications') "预览模型必须单独保存芯片规格组。"
Assert-True ($modelText -match 'ActionForms') "预览模型必须按形态保存动作属性组。"
Assert-True ($modelText -match 'GunShellAdjustments') "枪壳修正必须单独显示一次。"
Assert-True ($modelText -match 'GunShellMetrics') "枪壳共性属性必须单独保存为指标组。"
Assert-True ($modelText -match 'ProductLabel') "预览模型必须保存完整成品名称。"
Assert-True ($builderText -match 'ProductLabel\s*=\s*resolution\.ResolvedLabel') "预览必须使用统一解析后的完整成品名称。"
Assert-True ($builderText -match 'AddGunShellMetrics') "枪壳共性属性必须由预览构建器统一生成。"
Assert-True ($builderText -match 'BurstShotCount') "枪壳特征必须包含子弹数量。"
Assert-True ($builderText -notmatch 'AddMultiplier\s*\(\s*model\s*,\s*"ProjectileDamage"') "枪壳特征不得重复显示投射物伤害倍率。"
Assert-True ($builderText -match 'showGunShellCommonMetrics\s*=\s*gunShell\s*==\s*null') "动作属性构建必须区分已选枪壳场景。"

foreach ($spec in @("SlotRegion", "SlotOccupancy", "ActivationDelay", "DeactivationDelay", "CapacityCost", "ActivationCost", "Requirements"))
{
    Assert-True ($builderText -match $spec) "芯片规格缺少：$spec"
}
foreach ($metric in @("Range", "AccuracyTouch", "AccuracyShort", "AccuracyMedium", "AccuracyLong", "Warmup", "Cooldown", "ProjectileDamage", "ProjectileSpeed"))
{
    Assert-True ($builderText -match $metric) "动作属性缺少：$metric"
}
Assert-True ($builderText -match '→') "枪壳绝对覆盖必须使用 →。"
Assert-True ($builderText -match '×') "枪壳倍率必须使用 ×。"
Assert-True ($scaleText -match 'AccuracyMaximum\s*=\s*1') "精度条必须使用固定 0～100% 标尺。"
Assert-True ($scaleText -match 'RangeMaximum') "射程条必须使用集中固定上限。"
Assert-True ($scaleText -match 'DamageMaximum') "伤害条必须使用集中固定上限。"
Assert-True ($scaleText -match 'SpeedMaximum') "速度条必须使用集中固定上限。"
Assert-True ($scaleText -match 'WarmupMaximum') "预热条必须使用集中固定上限。"
Assert-True ($scaleText -match 'CooldownMaximum') "冷却条必须使用集中固定上限。"
Assert-True ($scaleText -match 'BurstShotCountMaximum') "子弹数量条必须使用集中固定上限。"
Assert-True ($panelText -match 'barRect[\s\S]*valueRect') "条形图和数值必须同一行，且数值区域位于条形图右侧。"
Assert-True ($panelText -match 'Text\.CalcHeight') "较长的规格标签与使用要求必须按内容自然换行。"
Assert-True ($panelText -match 'DrawProductTitle[\s\S]*GameFont\.Medium') "完整成品名称必须是中栏唯一中号主标题。"
Assert-True ($panelText -match 'DrawHeader[\s\S]*GameFont\.Small') "规格、枪壳和动作段标题必须使用小号层级。"
Assert-True ($panelText -match 'GameFont\s+oldFont\s*=\s*Text\.Font') "预览面板修改全局字体前必须保存原字体。"
Assert-True ($panelText -match 'finally[\s\S]*Text\.Font\s*=\s*oldFont') "预览面板必须在 finally 中恢复原字体。"
Assert-True ($panelText -match 'BDP_ChipManufacturing_Preview_Specifications"\.Translate\s*\(') "芯片规格标题必须翻译后再绘制。"
Assert-True ($panelText -match 'BDP_ChipManufacturing_Preview_GunShellAdjustments"\.Translate\s*\(') "枪壳修正标题必须翻译后再绘制。"
Assert-True ($panelText -match 'GunShellMetrics[\s\S]*DrawMetric') "枪壳共性属性必须以条形图指标形式绘制。"
Assert-True ($panelText -match 'DrawAdjustmentGrid') "枪壳修正必须使用紧凑双列排版。"
Assert-True ($languageText -match '<BDP_ChipManufacturing_ProductLabel>触发器芯片:\{0\}</BDP_ChipManufacturing_ProductLabel>') "无枪型成品名必须使用无空格英文冒号。"
Assert-True ($languageText -match '<BDP_ChipManufacturing_ProductLabelWithGunShell>触发器芯片:\{0\}\[\{1\}型\]</BDP_ChipManufacturing_ProductLabelWithGunShell>') "带枪型成品名必须使用无空格英文冒号。"
Assert-True ($languageText -match 'BDP_ChipManufacturing_Metric_BurstShotCount') "语言包必须包含子弹数量字段。"
Assert-True ($panelText -match 'foreach\s*\([^\)]*ActionForms') "动作形态必须按实际数量上下绘制，不预留空白第二块。"
Assert-True ($windowText -match 'ChipManufacturingPreviewPanel\.Draw') "中栏必须接入正式预览面板。"

Write-Host "PASS: 中栏按同级分组显示芯片规格、一次枪壳修正和上下动作形态，数值位于条形图右侧。"
