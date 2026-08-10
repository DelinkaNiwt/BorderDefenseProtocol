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

$drainServicePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ExpressionSustainDrainService.cs'
$drainKeyFactoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Runtime\ExpressionSustainDrainKeyFactory.cs'
$formalSurfacesPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$compositeResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'

$drainServiceText = if (Test-Path -LiteralPath $drainServicePath) {
    Get-Content -LiteralPath $drainServicePath -Raw -Encoding utf8
} else {
    ''
}
$drainKeyFactoryText = if (Test-Path -LiteralPath $drainKeyFactoryPath) {
    Get-Content -LiteralPath $drainKeyFactoryPath -Raw -Encoding utf8
} else {
    ''
}
$formalSurfacesText = Get-Content -LiteralPath $formalSurfacesPath -Raw -Encoding utf8
$compositeResolverText = Get-Content -LiteralPath $compositeResolverPath -Raw -Encoding utf8

# 对账只看最终可用的正式表达，并使用最终效果身份合并相同宿主。
Assert-True (
    ($drainServiceText -match 'FormalExpressionResult') -and
    ($drainServiceText -match 'IsAvailable') -and
    ($drainServiceText -match 'SustainCostBySourceCount')
) 'Expression sustain drain must be derived from final available formal results.'
Assert-True (
    ($drainKeyFactoryText -match '"Expression"') -and
    ($drainKeyFactoryText -match 'AbilityDefName') -and
    ($drainKeyFactoryText -match 'HediffDefName') -and
    ($drainKeyFactoryText -match 'PassiveKey')
) 'Expression sustain drain keys must use stable final-effect identities.'

# 每次发布都以账本现况做增删改对账，空投影也能清掉旧费用，读档后也能重建。
Assert-True (
    ($drainServiceText -match 'GetDrainSnapshot\(') -and
    ($drainServiceText -match 'RegisterDrain\(') -and
    ($drainServiceText -match 'UnregisterDrain\(')
) 'Expression sustain drain service must reconcile against the central Trion ledger.'
Assert-True (
    $formalSurfacesText -match 'hostSynchronizer\?\.Sync\(pawn,\s*snapshot\);[\s\S]*sustainDrainService\?\.Reconcile\(pawn,\s*snapshot\);'
) 'Host synchronization must be followed by sustain-drain reconciliation on every publication.'

# 普通双主表达只是聚合展示结果，不得复制来源费用表造成重复计费。
Assert-True (
    $compositeResolverText -notmatch 'Trion\s*=\s*mainPrimary\.Trion'
) 'Dual-primary aggregate results must not reuse a source sustain table.'

Write-Output 'ExpressionSustainDrainRuntimeSmokeTests PASS'
