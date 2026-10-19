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
$segmentText = Read-Source (Join-Path $beamTrailRoot 'BeamTrailSegment.cs')
$mapComponentText = Read-Source (Join-Path $beamTrailRoot 'BeamTrailMapComponent.cs')

Assert-True (
    ($mapComponentText -match 'liveRenderEnds') -and
    ($mapComponentText -match 'ResolveLiveDrawEnd') -and
    ($mapComponentText -match 'RealTime\.deltaTime') -and
    ($mapComponentText -match 'DrawLiveSegmentIfVisible')
) 'BeamTrailMapComponent must interpolate the transient live head during rendering.'

Assert-True (
    ($segmentText -match 'DrawTransient') -and
    ($segmentText -match 'DrawCoreTransient') -and
    ($segmentText -match 'SegmentLifetimeTicks') -and
    ($segmentText -match 'TicksAlive')
) 'Live-head interpolation must reuse transient drawing while retaining the existing lifetime state.'

Assert-True (
    ($mapComponentText -match 'liveRenderEnds\.Remove') -and
    ($mapComponentText -match 'FinalizeLiveSegment') -and
    ($mapComponentText -match 'ClearLiveSegment')
) 'Live-head interpolation state must be cleared on promotion, finalization, and termination.'

Write-Output 'BeamTrailLiveHeadInterpolationSmokeTests PASS'
