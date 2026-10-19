$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $sourceRoot
$windowPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Window_TriggerAssembly.cs'
$panelPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Panel_ChipDetail.cs'
$builderPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\ChipAssemblyDetailModelBuilder.cs'
$rendererPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs'
$languagePath = Join-Path $projectRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Windows.xml'

$window = Get-Content -Raw $windowPath
$panel = Get-Content -Raw $panelPath
$builder = Get-Content -Raw $builderPath
$renderer = Get-Content -Raw $rendererPath
$language = Get-Content -Raw $languagePath

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    if (-not $text.Contains($needle)) {
        throw "FAIL: $message"
    }
}

function Assert-NotContains([string]$text, [string]$needle, [string]$message) {
    if ($text.Contains($needle)) {
        throw "FAIL: $message"
    }
}

Assert-Contains $builder 'manufactured.CombinationRecord' '成品芯片详情必须读取持久化组合记录。'
Assert-Contains $builder 'ChipCombinationResolver' '成品芯片详情必须经过统一组合解析器。'
Assert-Contains $builder 'ManufacturedChipInformationModelBuilder.Build' '成品芯片详情必须调用共享信息模型构建器。'
Assert-NotContains $builder 'ChipManufacturingPreviewBuilder.Build' '成品芯片详情不得借道制造预览信息模型。'
Assert-Contains $builder 'ChipInformationInstanceRequestAdapter.TryCreate' '非成品芯片必须回退到统一实例信息请求适配器。'
Assert-NotContains $panel 'VerbProperties' '详情面板不得直接读取原始 VerbProperties。'
Assert-NotContains $panel 'ProjectileProperties' '详情面板不得直接读取原始 ProjectileProperties。'
Assert-NotContains $panel 'OVR' '详情面板不得恢复文字覆盖标记。'

Assert-Contains $window 'selectedChip = slot.LoadedChip' '点击空槽必须清除旧芯片选择。'
Assert-Contains $window 'InvalidateSelectedChipInformation' '装配事务后必须使详情缓存失效。'
Assert-Contains $window 'ChipAssemblyDetailModelBuilder.TryBuild' '窗口必须使用真实详情模型入口。'
Assert-Contains $panel 'DrawPanelFrame' '右栏必须绘制统一标题框架。'
Assert-Contains $panel 'HeaderHeight' '右栏内容滚动区必须位于标题下方。'
Assert-Contains $panel 'BDP_Window_TriggerAssembly_DetailPanelTitle' '右栏必须显示芯片详情中文标题。'

Assert-Contains $renderer 'FeatureRendererRegistry' '详情必须通过语义功能展示器注册表。'
Assert-Contains $renderer 'RenderAll' '详情必须统一读取已解析功能结果。'
Assert-Contains $renderer 'DrawGeneralBlocks' '未归入语义功能的通用信息必须保留兜底展示。'
Assert-NotContains $renderer 'RangedVerbInformationRule' '详情不得按旧远程技术规则识别功能。'
Assert-NotContains $renderer 'GetShieldPresentationBlock' '详情不得按旧护盾信息块识别功能。'

foreach ($key in @(
    'BDP_Window_TriggerAssembly_Description',
    'BDP_Window_TriggerAssembly_Radar',
    'BDP_Window_TriggerAssembly_AccuracyCurve',
    'BDP_Window_TriggerAssembly_ProvenanceTooltip',
    'BDP_Window_TriggerAssembly_DetailPanel',
    'BDP_Window_TriggerAssembly_DetailPanelTitle')) {
    Assert-Contains $language $key "语言包缺少 $key。"
}

foreach ($key in @(
    'BDP_Window_TriggerAssembly_OverrideMarkerTooltip',
    'BDP_Window_TriggerAssembly_OverrideLegend')) {
    Assert-NotContains $language $key "语言包不应继续保留 $key。"
}

Write-Output 'TriggerAssemblerChipDetailPanel PASS'
