$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$expressionConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionSourceTrionConfig.cs'
$sustainRowPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionSustainCostBySourceCountConfig.cs'
$sustainPolicyPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ExpressionSustainCostPolicy.cs'
$chipConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Config\ChipTrionConfig.cs'
$chipContractPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Contract\ChipTrionContract.cs'
$comboConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs'
$oldHediffCompPath = Join-Path $repoRoot 'Source\BDP\Core\Hediffs\HediffComp_BdpTrionDrain.cs'
$oldHediffPropsPath = Join-Path $repoRoot 'Source\BDP\Core\Hediffs\HediffCompProperties_BdpTrionDrain.cs'

$expressionConfigText = Get-Content -LiteralPath $expressionConfigPath -Raw -Encoding utf8
$chipConfigText = Get-Content -LiteralPath $chipConfigPath -Raw -Encoding utf8
$chipContractText = Get-Content -LiteralPath $chipContractPath -Raw -Encoding utf8
$comboConfigText = Get-Content -LiteralPath $comboConfigPath -Raw -Encoding utf8
$sustainRowText = if (Test-Path -LiteralPath $sustainRowPath) {
    Get-Content -LiteralPath $sustainRowPath -Raw -Encoding utf8
} else {
    ''
}
$sustainPolicyText = if (Test-Path -LiteralPath $sustainPolicyPath) {
    Get-Content -LiteralPath $sustainPolicyPath -Raw -Encoding utf8
} else {
    ''
}

# 表达侧只保留一张版面统一的“有效来源数 -> 总每秒费用”表。
Assert-True (
    $expressionConfigText -match 'List<ExpressionSustainCostBySourceCountConfig>\s+SustainCostBySourceCount'
) 'ExpressionSourceTrionConfig must expose SustainCostBySourceCount as a typed list.'
Assert-True (
    ($sustainRowText -match 'public\s+int\s+SourceCount\s*;') -and
    ($sustainRowText -match 'public\s+float\s+TotalPerSecond\s*;')
) 'Each sustain tier must contain SourceCount and TotalPerSecond.'

# 统一策略必须明确约束：从 1 连续递增、费用有限且非负，超出最高档沿用最后一档。
Assert-True (
    ($sustainPolicyText -match 'SourceCount') -and
    ($sustainPolicyText -match 'TotalPerSecond') -and
    ($sustainPolicyText -match 'IsNaN') -and
    ($sustainPolicyText -match 'IsInfinity')
) 'Expression sustain policy must validate count and finite per-second totals.'
Assert-True (
    $sustainPolicyText -match 'ResolveTotalPerSecond'
) 'Expression sustain policy must resolve the total per-second cost.'
Assert-True (
    $sustainPolicyText -match 'effectiveSourceCount'
) 'Expression sustain policy must select a tier by effective source count.'

# 芯片本体不再拥有持续消耗；组合技也不得添加费用表继承开关。
Assert-True ($chipConfigText -notmatch 'ActiveDrainPerSecond') 'ChipTrionConfig must not own active sustain drain.'
Assert-True ($chipContractText -notmatch 'ActiveDrainPerSecond') 'ChipTrionContract must not carry active sustain drain.'
Assert-True (
    $comboConfigText -notmatch 'SustainCostBySourceCountResolve|SustainCostResolve'
) 'Combo definitions must configure sustain tiers explicitly instead of inheriting them.'

# 旧的 Hediff 专用扣费组件不能复活；新版必须覆盖所有正式表达种类。
Assert-True (-not (Test-Path -LiteralPath $oldHediffCompPath)) 'Legacy Hediff-only sustain drain component must stay removed.'
Assert-True (-not (Test-Path -LiteralPath $oldHediffPropsPath)) 'Legacy Hediff-only sustain drain properties must stay removed.'

Write-Output 'ExpressionSustainCostDefinitionBoundarySmokeTests PASS'
