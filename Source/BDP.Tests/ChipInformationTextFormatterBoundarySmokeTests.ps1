# 芯片信息基础文本格式化边界测试。
# 所有信息值均经共享格式器解释；面板只负责字段筛选和布局。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$formatterPath = Join-Path $modRoot "Source\BDP.Content\Information\ChipInformationTextFormatter.cs"
$previewPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewPanel.cs"
$detailPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs"

Assert-True (Test-Path -LiteralPath $formatterPath) "缺少芯片信息基础文本格式化器。"

$formatterText = Get-Utf8Text $formatterPath
$previewText = Get-Utf8Text $previewPath
$detailText = Get-Utf8Text $detailPath

Assert-True ($formatterText -match "TryFormatScalar") "共享格式化器必须提供基础标量入口。"
Assert-True ($formatterText -match "ApplyUnit") "共享格式化器必须集中套用单位语言键。"
foreach ($kind in @("Text", "Boolean", "Integer", "Number"))
{
    Assert-True ($formatterText -match "ChipInformationValueKind\.$kind") "共享格式化器缺少基础类型：$kind"
}

foreach ($kind in @("Percentage", "Duration", "Enum", "List"))
{
    Assert-True ($formatterText -match "ChipInformationValueKind\.$kind") "共享格式器必须处理完整类型：$kind"
}

Assert-True ($previewText -match "Panel_ChipDetail\.DrawManufacturingChipDetail") "制造预览必须调用共享详情面板。"
Assert-True ($detailText -match "ChipInformationTextFormatter\.FormatValueWithUnit") "装配台详情必须调用共享完整值格式化器和单位入口。"
Assert-True ($detailText -match "ChipInformationTextFormatter\.FormatFeatureProperty") "装配台详情必须通过语义属性入口处理策略敏感值。"
Assert-True ($previewText -notmatch "EnumKey\.Translate\(\)" -and $detailText -notmatch "EnumKey\.Translate\(\)") `
    "两个面板不得各自将值键假定为 Keyed 文本键。"

Write-Host "PASS: 两个面板共享所有信息值及嵌套列表格式化规则。"
