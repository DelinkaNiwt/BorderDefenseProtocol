$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$configPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboDefinitionConfig.cs'
$defPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Defs\ComboDef.cs'
$projectionPath = Join-Path $repoRoot 'Source\BDP\Core\Information\Features\Projection\ComboFeatureInformationSnapshotBuilder.cs'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

Assert-True ((Get-Content -LiteralPath $configPath -Raw) -match 'List<ChipFeatureDeclaration>\s+Features') '组合配置必须承载独立 Features。'
Assert-True ((Get-Content -LiteralPath $defPath -Raw) -match 'Features\s*=\s*Features') 'ComboDef 必须把最终功能声明复制到配置镜像。'
Assert-True ((Get-Content -LiteralPath $projectionPath -Raw) -match 'comboDef\.Features' -and (Get-Content -LiteralPath $projectionPath -Raw) -match 'ChipFeatureResolver') '组合功能必须从自身声明进入统一功能解析器。'

Write-Host 'ChipFeatureComboResolution PASS'
