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
$cursorPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbEmissionCursor.cs'
$cursorText = Get-Content -LiteralPath $cursorPath -Raw -Encoding utf8

Assert-True (
    ($cursorText -notmatch 'CloneProjectilePlans') -and
    ($cursorText -notmatch 'CloneEmissionWindows')
) 'RangedVerbEmissionCursor must not clone formal emission plans.'

Assert-True (
    $cursorText -notmatch 'new\s+ProjectileInitPlan'
) 'RangedVerbEmissionCursor must not rebuild ProjectileInitPlan objects.'

Assert-True (
    ($cursorText -match 'pendingWindowIndex') -and
    ($cursorText -match 'pendingWindowProjectilePlanIndex') -and
    ($cursorText -match 'pendingEmissionConsumedCount')
) 'RangedVerbEmissionCursor must keep only cursor state for host-side consumption.'

Write-Output 'RangedEmissionCursorBoundarySmokeTests PASS'
