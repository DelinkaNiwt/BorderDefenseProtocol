$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$windowRoot = Join-Path $sourceRoot 'BDP.Content\Assembly\Window'
$builderPath = Join-Path $windowRoot 'ChipAssemblyDetailModelBuilder.cs'
$comparisonPath = Join-Path $windowRoot 'ChipInformationComparisonModel.cs'
$windowPath = Join-Path $windowRoot 'Window_TriggerAssembly.cs'
$panelPath = Join-Path $windowRoot 'Panel_ChipDetail.cs'
$rendererPath = Join-Path $windowRoot 'ChipInformationDetailRenderer.cs'
$languagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Windows.xml'
$armamentFormPath = Join-Path $modRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "FAIL: $Message"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    Assert-True $Text.Contains($Needle) $Message
}

Assert-True (Test-Path -LiteralPath $comparisonPath) '必须存在 Content 层比较模型。'

$builderText = Get-Content -LiteralPath $builderPath -Raw -Encoding utf8
$comparisonText = Get-Content -LiteralPath $comparisonPath -Raw -Encoding utf8
$windowText = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8
$panelText = Get-Content -LiteralPath $panelPath -Raw -Encoding utf8
$rendererText = Get-Content -LiteralPath $rendererPath -Raw -Encoding utf8
$languageText = Get-Content -LiteralPath $languagePath -Raw -Encoding utf8
$armamentXml = [xml](Get-Content -LiteralPath $armamentFormPath -Raw -Encoding utf8)

Assert-Contains $comparisonText 'ChipInformationModel Current' '比较模型必须保留当前最终信息模型。'
Assert-Contains $comparisonText 'ChipInformationModel Baseline' '比较模型必须保留比较基准信息模型。'
Assert-Contains $comparisonText 'ChipInformationComparisonBaselineMode' '比较模型必须保留可切换的基准策略类型。'

Assert-Contains $builderText 'ChipInformationComparisonModel' '详情模型入口必须返回当前/基准比较模型。'
Assert-Contains $builderText 'DefaultComparisonBaselineMode' '详情入口必须集中声明当前默认比较策略。'
Assert-Contains $builderText 'baselineMode' '详情入口必须允许调用方切换比较策略。'
Assert-Contains $builderText 'ChipCombinationResolver' '比较基准必须复用统一组合解析器。'
Assert-Contains $builderText 'ManufacturedChipInformationModelBuilder.Build' '比较基准必须复用统一信息模型构建器。'
Assert-True ($builderText -notmatch 'ChipManufacturingPreviewBuilder\.Build') '比较基准不得借道制造预览构建信息模型。'

Assert-Contains $builderText 'sourceResolution.DeclaredConfig' '信息图基准必须从芯片功能基础表达配置开始构建。'
Assert-Contains $builderText 'ChipConfigurationMergeService.CloneConfig' '信息图基准必须复制基础配置而不是重解析制造记录。'
Assert-Contains $builderText 'ChipArmamentFormApplicationService.Apply' '信息图基准必须直接应用 Trion 立方体构型修正。'
Assert-Contains $builderText 'BDP_ArmamentForm_TrionCube' '信息图基准必须明确使用 Trion 立方体构型。'
Assert-True ($builderText -notmatch 'baselineRecord\.ArmamentFormDefName\s*=\s*null') `
    '信息图基准不得通过清空构型后依赖当前职业寻找隐藏默认型。'

Assert-Contains $windowText 'selectedChipInformationComparison' '窗口必须缓存当前芯片的比较模型。'
Assert-Contains $panelText 'ChipInformationComparisonModel' '详情面板必须消费比较模型。'
Assert-Contains $rendererText 'comparison?.Current' '语义详情渲染器必须读取统一比较模型的当前最终模型。'
Assert-Contains $rendererText 'FeatureRendererRegistry' '语义详情渲染器必须通过功能展示器处理功能。'
Assert-True ($rendererText -notmatch 'FindComparisonBaselineValue|AddRangedCadenceAxis|AddMeleeCadenceAxis') `
    '语义详情渲染器不得重新引入已退役的技术派生比较图。'

$trionCube = $armamentXml.SelectNodes('//*[contains(local-name(),"ChipArmamentFormDef")]') |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_TrionCube' } |
    Select-Object -First 1
Assert-True ($null -ne $trionCube -and $trionCube.implicitDefault -eq 'true') `
    'Trion 立方体必须继续作为隐藏默认武装型参与基准解析。'

Write-Output 'ChipInformationComparisonBaselineSmokeTests PASS'
