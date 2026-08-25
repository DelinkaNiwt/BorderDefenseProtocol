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

$abilitySynchronizerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionAbilityHostSynchronizer.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionSurfaceAccess.cs'
$abilityCostPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\CompAbilityEffect_BdpTrionCost.cs'

Assert-True (Test-Path -LiteralPath $abilitySynchronizerPath) 'Main mod must keep DefaultExpressionAbilityHostSynchronizer.'
Assert-True (Test-Path -LiteralPath $expressionSurfacePath) 'Main mod must keep ExpressionSurfaceAccess.'
Assert-True (Test-Path -LiteralPath $abilityCostPath) 'Main mod must keep CompAbilityEffect_BdpTrionCost.'

$abilitySynchronizerText = Get-Content -LiteralPath $abilitySynchronizerPath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$abilityCostText = Get-Content -LiteralPath $abilityCostPath -Raw -Encoding utf8

Assert-True (
    ($abilitySynchronizerText -match 'AllAbilitiesForReading') -and
    ($abilitySynchronizerText -match 'CollectOwnedAbilityDefs') -and
    ($abilitySynchronizerText -match 'IsExpressionOwnedAbilityDef')
) 'Ability synchronizer must scan current Pawn ability hosts and build an owned-def set instead of trusting only AddedAbilityDefsByPawn.'

Assert-True (
    ($abilitySynchronizerText -match 'CompProperties_AbilityEffect_BdpTrionCost') -and
    ($abilitySynchronizerText -match 'IBdpExpressionAbilityVerb')
) 'Ability synchronizer must identify expression-owned ability shells by their formal host shell markers.'

Assert-True (
    ($abilitySynchronizerText -match 'RemoveInactiveAbilities\(\s*Pawn pawn,\s*HashSet<string> currentDefNames,\s*HashSet<string> ownedDefNames,\s*HashSet<string> addedDefNames\s*\)') -and
    ($abilitySynchronizerText -match 'foreach\s*\(string defName in ownedDefNames\)')
) 'Ability synchronizer must revoke stale ability hosts from the owned-def set, not only from AddedAbilityDefsByPawn.'

Assert-True (
    ($expressionSurfaceText -match 'TryResolveAbilityHost') -and
    ($abilityCostText -match 'TryResolveBoundAbilityResult')
) 'Live ability resolution and Trion cost lookup must continue to depend on the current formal binding truth.'

Write-Output 'ExpressionAbilityHostTruthOwnershipSmokeTests PASS'
