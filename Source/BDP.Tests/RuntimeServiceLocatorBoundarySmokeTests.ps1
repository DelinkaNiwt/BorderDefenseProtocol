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

$attackExecutionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionSurfaceAccess.cs'
$rangedProtocolSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolSurfaceAccess.cs'
$chipSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Access\ChipSurfaceAccess.cs'
$comboSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Access\ComboSurfaceAccess.cs'

$attackExecutionSurfaceText = Get-Content -LiteralPath $attackExecutionSurfacePath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$rangedProtocolSurfaceText = Get-Content -LiteralPath $rangedProtocolSurfacePath -Raw -Encoding utf8
$chipSurfaceText = Get-Content -LiteralPath $chipSurfacePath -Raw -Encoding utf8
$comboSurfaceText = Get-Content -LiteralPath $comboSurfacePath -Raw -Encoding utf8

Assert-True (
    $attackExecutionSurfaceText -notmatch 'private\s+static\s+readonly\s+AttackExecutionService\s+ExecutionEntry'
) 'AttackExecutionSurfaceAccess must not own a runtime static AttackExecutionService.'

Assert-True (
    $expressionSurfaceText -notmatch 'private\s+static\s+readonly\s+ExpressionRuntimeRepository\s+runtimeRepository'
) 'ExpressionSurfaceAccess must not own a runtime static ExpressionRuntimeRepository.'

Assert-True (
    $expressionSurfaceText -notmatch 'private\s+static\s+readonly\s+ExpressionService\s+service'
) 'ExpressionSurfaceAccess must not own a runtime static ExpressionService.'

Assert-True (
    $rangedProtocolSurfaceText -notmatch 'private\s+static\s+readonly\s+RangedAttackProtocolService\s+Service'
) 'RangedAttackProtocolSurfaceAccess must not own a runtime static RangedAttackProtocolService.'

Assert-True (
    $rangedProtocolSurfaceText -notmatch 'private\s+static\s+readonly\s+RangedAttackTrionGate\s+TrionGate'
) 'RangedAttackProtocolSurfaceAccess must not own a runtime static RangedAttackTrionGate.'

Assert-True (
    ($chipSurfaceText -match 'static') -and
    ($comboSurfaceText -match 'static')
) 'Definition-facing caches remain allowed to stay static in Chip and Combo surfaces.'

Write-Output 'RuntimeServiceLocatorBoundarySmokeTests PASS'
