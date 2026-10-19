$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$rendererRoot = Join-Path $sourceRoot 'BDP.Content\Information\Features\Rendering'
$resultPath = Join-Path $rendererRoot 'ChipFeatureRenderResult.cs'
$groupResultPath = Join-Path $rendererRoot 'ChipFeatureGroupRenderResult.cs'
$groupRendererPath = Join-Path $rendererRoot 'GenericChipFeatureGroupRenderer.cs'
$registryPath = Join-Path $rendererRoot 'ChipFeatureRendererRegistry.cs'
$detailRendererPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs'
$manufacturingPanelPath = Join-Path $sourceRoot 'BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewPanel.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Read-Source([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return '' }
    return Get-Content -LiteralPath $path -Raw -Encoding UTF8
}

$resultText = Read-Source $resultPath
$groupResultText = Read-Source $groupResultPath
$groupRendererText = Read-Source $groupRendererPath
$registryText = Read-Source $registryPath
$detailRendererText = Read-Source $detailRendererPath
$manufacturingPanelText = Read-Source $manufacturingPanelPath

Assert-True ($resultText -match 'IReadOnlyList<ChipFeatureGroupRenderResult> Groups') '功能展示结果必须保留语义信息组。'
Assert-True ($resultText -match 'IReadOnlyList<ChipFeatureGroupRenderResult> Subgroups') '功能展示结果必须保留直接归属功能的额外效果子组。'
Assert-True ($groupResultText -match 'IReadOnlyList<ChipFeatureGroupRenderResult> Subgroups') '信息组展示结果必须保留其所有的额外效果子组。'
Assert-True ($groupResultText -match 'string Path') '通用组展示结果必须保留稳定结构路径。'
Assert-True ($registryText -match 'groupRenderer\.RenderGroups\(feature\.Groups\)' -and $registryText -match 'groupRenderer\.RenderSubgroups\(feature\.Subgroups\)') '展示注册表必须同时把功能信息组与功能级子组交给通用组投影。'
Assert-True ($groupRendererText -match 'group\.Properties' -and $groupRendererText -match 'subgroup\.Properties') '通用组投影必须从各自的解析属性读取信息，不猜测实现类型。'
Assert-True ($groupRendererText -match 'group\.Subgroups') '通用组投影必须保留信息组拥有的子组。'
Assert-True ($registryText -match 'GenericChipFeatureGroupRenderer') '展示注册表必须将组投影附加到同一功能展示结果。'
Assert-True ($detailRendererText -match 'DrawFeatureGroup' -and $detailRendererText -match 'MeasureFeatureGroup') '触发器装配详情必须绘制并测量嵌套组和子组。'
Assert-True ($manufacturingPanelText -match 'DrawFeatureGroups' -and $manufacturingPanelText -match 'FeatureGroupsHeight') '制造台预览必须绘制并测量同一结构的组和子组。'
Assert-True ($detailRendererText -notmatch 'Verb|AbilityDef|HediffDef') '触发器装配详情不能按游戏技术类型解释语义组。'
Assert-True ($manufacturingPanelText -notmatch 'Verb|AbilityDef|HediffDef') '制造台预览不能按游戏技术类型解释语义组。'

Write-Output 'ChipFeatureInformationGroupRenderer PASS'
