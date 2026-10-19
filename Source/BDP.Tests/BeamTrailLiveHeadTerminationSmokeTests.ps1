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

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$beamTrailRoot = Join-Path $repoRoot 'Source\BDP.Content\Projectiles\BeamTrail'

$attachmentPath = Join-Path $beamTrailRoot 'BeamTrailAttachment.cs'
$mapComponentPath = Join-Path $beamTrailRoot 'BeamTrailMapComponent.cs'

$attachmentText = Read-Source $attachmentPath
$mapComponentText = Read-Source $mapComponentPath
$terminateStart = $attachmentText.IndexOf('public void OnTerminate')
$terminateText = if ($terminateStart -ge 0) { $attachmentText.Substring($terminateStart) } else { '' }

Assert-True (
    ($mapComponentText -match 'Dictionary<string,\s*BeamTrailSegment>\s+liveSegmentsByProjectileId') -and
    ($mapComponentText -match 'void\s+SetLiveSegment\s*\(\s*string\s+projectileThingId,\s*Vector3\s+start,\s*Vector3\s+end,\s*BeamTrailAppearanceSnapshot\s+appearance\s*\)') -and
    ($mapComponentText -match 'void\s+PromoteLiveSegment\s*\(\s*string\s+projectileThingId\s*\)') -and
    ($mapComponentText -match 'void\s+FinalizeLiveSegment\s*\(\s*string\s+projectileThingId,\s*Vector3\s+terminalPosition\s*\)') -and
    ($mapComponentText -match 'void\s+ClearLiveSegment\s*\(\s*string\s+projectileThingId\s*\)') -and
    ($mapComponentText -notmatch 'Scribe_Collections\.Look\(ref liveSegmentsByProjectileId')
) 'BeamTrailMapComponent must keep transient live-head segments by projectile id, expose sample/promote/finalize/clear entry points, and must not persist live heads into saves.'

Assert-True (
    ($attachmentText -match 'PromoteLiveSegment\s*\(\s*context\.ProjectileThingId\s*\)') -and
    ($attachmentText -match 'SetLiveSegment\s*\(\s*context\.ProjectileThingId,\s*start,\s*end,\s*appearance\s*\)') -and
    ($terminateText -match 'FinalizeLiveSegment\s*\(\s*context\.ProjectileThingId,\s*context\.CurrentPosition\s*\)') -and
    ($terminateText -match 'IsVisualEndpointSafe') -and
    ($terminateText -match 'ClearLiveSegment\s*\(\s*context\.ProjectileThingId\s*\)') -and
    ($attachmentText -notmatch 'AppendSegment\s*\(\s*start,\s*end,\s*appearance\s*\)')
) 'BeamTrailAttachment must keep the newest sample as a live head during flight, finalize it at the actual visual terminal position, and never rebuild it toward a target.'

Write-Output 'BeamTrailLiveHeadTerminationSmokeTests PASS'
