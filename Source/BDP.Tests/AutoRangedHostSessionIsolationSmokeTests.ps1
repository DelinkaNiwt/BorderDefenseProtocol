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
$attackSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$continuationPlannerPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbContinuationPlanner.cs'

$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8

Assert-True (
    $attackSurfaceText -notmatch 'shootVerb\.HostModuleSession\s*='
) 'Auto-ranged bridge must not overwrite the committed host module session directly.'

Assert-True (
    $attackSurfaceText -match 'StageEntryModuleSession\s*\('
) 'Auto-ranged bridge must stage entry module state through a dedicated staging surface.'

Assert-True (
    $shootVerbText -match 'private\s+RangedAttackModuleSession\s+stagedEntryModuleSession'
) 'BdpVerb_Shoot must keep a staged entry module session separate from the committed execution session.'

Assert-True (
    $shootVerbText -match 'StageEntryModuleSession\s*\('
) 'BdpVerb_Shoot must expose a dedicated entry-session staging method.'

Assert-True (
    $shootVerbText -match 'ResolveEntryModuleSession\s*\('
) 'BdpVerb_Shoot must expose a staged-session read surface for entry preparation.'

Assert-True (
    $continuationPlannerText -match 'ResolveEntryModuleSession\s*\('
) 'RangedVerbContinuationPlanner must prefer the dedicated entry-session surface instead of rebuilding from a fresh published session first.'

Write-Output 'AutoRangedHostSessionIsolation PASS'
