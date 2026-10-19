# 芯片信息持续时间单位格式化冒烟测试。

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'TestSupport\ChipManufacturingTestSupport.ps1')

$modRoot = Get-BdpModRoot
$formatterPath = Join-Path $modRoot 'Source\BDP.Content\Information\ChipInformationTextFormatter.cs'
$publisherPath = Join-Path $modRoot 'Source\BDP.Content\Information\ManufacturedChipInfoCardPublisher.cs'
$detailRendererPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs'

$formatterText = Get-Utf8Text $formatterPath
$publisherText = Get-Utf8Text $publisherPath
$detailRendererText = Get-Utf8Text $detailRendererPath

Assert-True ($formatterText -match 'FormatDuration') '共享格式化器必须集中处理持续时间。'
Assert-True ($formatterText -match 'unitKey\.NullOrEmpty') '持续时间无单位键时必须走默认秒文本。'
Assert-True ($formatterText -match 'ApplyUnit\(unitKey, raw\)') '持续时间有单位键时必须只套用一次单位。'
Assert-True ($formatterText -match 'FormatValueWithUnit' -and $formatterText -match 'kind == ChipInformationValueKind\.Duration') '通用字段值和单位必须在共享入口一次性处理，避免持续时间重复附加单位。'
Assert-True ($publisherText -match 'ChipInformationTextFormatter\.FormatValueWithUnit') '制造信息卡必须使用共享值与单位格式化器。'
Assert-True ($detailRendererText -match 'ChipInformationTextFormatter\.FormatFeatureProperty' -and $detailRendererText -match 'ChipInformationTextFormatter\.FormatValueWithUnit') '装配台详情必须用共享入口格式化语义属性与普通字段持续时间。'

Write-Output 'ChipInformationDurationUnit PASS'
