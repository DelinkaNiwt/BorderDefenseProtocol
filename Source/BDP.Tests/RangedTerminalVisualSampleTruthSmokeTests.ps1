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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$projectilePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'

Assert-True (Test-Path -LiteralPath $projectilePath) 'BdpProjectile.cs must exist.'

$projectileText = Read-Source $projectilePath

Assert-True (
    $projectileText -notmatch 'base\.Position\.ToVector3Shifted\s*\('
) 'Terminal visual sample truth must never fall back to base.Position.ToVector3Shifted(), because grid-center coordinates are not real flight-path truth.'

Assert-True (
    $projectileText -match 'private\s+Vector3\?\s+terminalVisualExactPosition'
) 'BdpProjectile must keep a dedicated terminal visual truth cache so same-tick destroy paths can freeze the real visual endpoint before cleanup.'

Assert-True (
    ($projectileText -match 'private\s+void\s+FreezeTerminalVisualExactPosition') -and
    ($projectileText -match 'private\s+void\s+ClearTerminalVisualExactPosition')
) 'BdpProjectile must explicitly freeze and clear terminal visual truth instead of inferring it later from stale host state.'

Assert-True (
    ($projectileText -match 'ResolveVisualSampleEnd\s*\(\s*Vector3\s+sampleStart\s*\)[\s\S]*?terminalVisualExactPosition\.HasValue') -and
    ($projectileText -notmatch 'ResolveVisualSampleEnd\s*\(\s*Vector3\s+sampleStart\s*\)[\s\S]*?return\s+currentFlightPathSnapshot\.End\s*;') -and
    ($projectileText -notmatch 'ResolveVisualSampleEnd\s*\(\s*Vector3\s+sampleStart\s*\)[\s\S]*?return\s+sampleStart\s*;')
) 'ResolveVisualSampleEnd must prioritize frozen terminal truth and must no longer use theoretical path end or sampleStart as the primary destroyed-path visual endpoint.'

Assert-True (
    ($projectileText -match 'FreezeTerminalVisualExactPosition\s*\(\s*NormalizeFlightPlanePoint\s*\(\s*ExactPosition\s*\)\s*\)\s*;') -or
    ($projectileText -match 'FreezeTerminalVisualExactPosition\s*\(\s*ResolveCurrentTerminalVisualExactPosition\s*\(\s*\)\s*\)\s*;')
) 'BdpProjectile must freeze the real current projectile truth before destroy-time cleanup.'

Write-Output 'RangedTerminalVisualSampleTruthSmokeTests PASS'
