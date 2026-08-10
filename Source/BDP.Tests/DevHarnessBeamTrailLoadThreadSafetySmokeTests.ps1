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
$beamTrailRoot = Join-Path $repoRoot 'Source\BDP.Content\Projectiles\BeamTrail'
$mapComponentPath = Join-Path $beamTrailRoot 'BeamTrailMapComponent.cs'
$mapComponentText = if (Test-Path -LiteralPath $mapComponentPath) { Read-Source $mapComponentPath } else { '' }

Assert-True (Test-Path -LiteralPath $mapComponentPath) 'BeamTrailMapComponent.cs must exist.'

Assert-True (
    $mapComponentText -notmatch 'RebuildMaterialCache\s*\(\s*\)\s*;'
) 'BeamTrailMapComponent.PostLoadInit must not rebuild materials on the loading thread.'

Assert-True (
    ($mapComponentText -match 'ResolveMaterial') -and
    ($mapComponentText -match 'MapComponentDraw')
) 'BeamTrailMapComponent must keep lazy material resolution in draw-time path.'

Write-Output 'DevHarnessBeamTrailLoadThreadSafetySmokeTests PASS'
