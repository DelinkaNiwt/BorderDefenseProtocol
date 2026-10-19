$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$comparisonPath = Join-Path $repoRoot 'Source\BDP.Content\Assembly\Window\ChipInformationComparisonModel.cs'
$comparisonText = Get-Content -LiteralPath $comparisonPath -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ($comparisonText -match 'BuildFeatureComparisonKey') '比较模型必须提供语义功能比较键。'
Assert-True ($comparisonText -match 'contextKey.*feature\.Path.*propertyId') '比较键必须包含上下文、功能路径和属性 Def。'
Assert-True ($comparisonText -match 'TryGetBaselineFeatureProperty') '比较模型必须支持读取语义功能属性基准值。'
Assert-True ($comparisonText -notmatch 'Feature.*GetType\(') '比较键不得通过实现类型推断功能身份。'

Write-Host 'ChipFeatureComparisonKey PASS'
