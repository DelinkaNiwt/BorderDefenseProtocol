$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$mergeRoot = Join-Path $repoRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution'
$configMerge = Get-Content -LiteralPath (Join-Path $mergeRoot 'ChipConfigurationMergeService.cs') -Raw -Encoding UTF8
$expressionMerge = Get-Content -LiteralPath (Join-Path $mergeRoot 'ChipExpressionMergeService.cs') -Raw -Encoding UTF8
$featureMerge = Get-Content -LiteralPath (Join-Path $mergeRoot 'ChipFeatureMergeService.cs') -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($configMerge -match 'ChipFeatureCloneContext context') '双预设合并必须创建单一复制上下文。'
Assert-True ($configMerge -match 'ChipExpressionMergeService\.MergeDual[\s\S]*context') '表达条目合并必须消费复制上下文。'
Assert-True ($configMerge -match 'ChipFeatureMergeService\.MergeDual[\s\S]*context') '功能声明合并必须消费同一复制上下文。'
Assert-True ($expressionMerge -match 'RegisterEntry') '表达条目复制必须登记 Id 映射。'
Assert-True ($featureMerge -match 'ResolveEntry') '功能来源必须通过上下文解析表达条目引用。'
Assert-True ($expressionMerge -match 'mfg_0' -and $expressionMerge -match 'mfg_1') '双预设表达必须保留两侧稳定前缀。'

Write-Host 'ChipFeatureDualMerge PASS'
