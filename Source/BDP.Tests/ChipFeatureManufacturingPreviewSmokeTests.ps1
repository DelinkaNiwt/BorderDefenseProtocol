$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$builderPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewBuilder.cs'
$modelPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewModel.cs'
$panelPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingPreviewPanel.cs'
$builderText = Get-Content $builderPath -Raw -Encoding UTF8
$modelText = Get-Content $modelPath -Raw -Encoding UTF8
$panelText = Get-Content $panelPath -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($builderText -match 'MapSemanticFeatures') '制造预览必须把语义功能结果映射到预览模型。'
Assert-True ($builderText -match 'registry\.RenderAll') '制造预览必须通过统一展示器处理功能结果。'
Assert-True ($builderText -match 'information\.Contexts') '制造预览必须保留所有形态/姿态上下文。'
Assert-True ($builderText -notmatch 'IsExpressionBlock') '制造预览不得再按技术块分类功能。'
Assert-True ($builderText -notmatch 'VerbProperties|AbilityDef|HediffDef|RangedModule') '制造预览不得读取实现技术字段。'
Assert-True ($modelText -match 'FeatureResults') '预览模型必须保存根级语义功能结果。'
Assert-True ($modelText -match 'FeatureContexts') '预览模型必须保存上下文语义功能结果。'
Assert-True ($panelText -match 'DrawFeatureResult') '制造预览面板必须绘制语义功能结果。'
Assert-True ($panelText -notmatch 'rangedAttack|meleeAttack|shield') '制造预览面板不得按技术块名称选择功能。'

Write-Host 'ChipFeatureManufacturingPreview PASS'
