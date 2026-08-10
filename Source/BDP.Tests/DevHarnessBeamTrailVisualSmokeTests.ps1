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

function Get-ThingDefBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<ThingDef\s+ParentName=""BaseBullet"">.*?<defName>$DefName</defName>.*?</ThingDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$beamTrailRoot = Join-Path $repoRoot 'Source\BDP.Content\Projectiles\BeamTrail'
$projectileDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Projectiles\Test\ThingDefs_TestProjectiles.xml'
$texturePath = Join-Path $repoRoot '1.6\Content\Textures\Things\Projectile\BDP_BeamTrail.png'

$configPath = Join-Path $beamTrailRoot 'BeamTrailConfig.cs'
$appearancePath = Join-Path $beamTrailRoot 'BeamTrailAppearanceSnapshot.cs'
$attachmentPath = Join-Path $beamTrailRoot 'BeamTrailAttachment.cs'
$segmentPath = Join-Path $beamTrailRoot 'BeamTrailSegment.cs'
$mapComponentPath = Join-Path $beamTrailRoot 'BeamTrailMapComponent.cs'

$appearanceText = if (Test-Path -LiteralPath $appearancePath) { Read-Source $appearancePath } else { '' }
$attachmentText = if (Test-Path -LiteralPath $attachmentPath) { Read-Source $attachmentPath } else { '' }
$segmentText = if (Test-Path -LiteralPath $segmentPath) { Read-Source $segmentPath } else { '' }
$mapComponentText = if (Test-Path -LiteralPath $mapComponentPath) { Read-Source $mapComponentPath } else { '' }
$projectileDefsText = if (Test-Path -LiteralPath $projectileDefsPath) { Read-Source $projectileDefsPath } else { '' }
$projectileBlock = Get-ThingDefBlock $projectileDefsText 'BDP_TestBulletSemantic'

Assert-True (-not (Test-Path -LiteralPath $configPath)) '正式系统不得保留 BeamTrailConfig.cs。'
Assert-True (Test-Path -LiteralPath $appearancePath) 'BeamTrailAppearanceSnapshot.cs must exist.'
Assert-True (Test-Path -LiteralPath $attachmentPath) 'BeamTrailAttachment.cs must exist.'
Assert-True (Test-Path -LiteralPath $segmentPath) 'BeamTrailSegment.cs must exist.'
Assert-True (Test-Path -LiteralPath $mapComponentPath) 'BeamTrailMapComponent.cs must exist.'

Assert-True (
    ($appearanceText -match 'class\s+BeamTrailAppearanceSnapshot') -and
    ($appearanceText -match 'TrailTexPath') -and
    ($appearanceText -match 'TrailColor') -and
    ($appearanceText -match 'TrailWidth') -and
    ($appearanceText -match 'TrailWidth\s*=\s*Mathf\.Max\(0\.01f,\s*preset\s*!=\s*null\s*\?\s*preset\.trailWidth\s*:\s*0\.1105f\)') -and
    ($appearanceText -match 'SegmentLifetimeTicks') -and
    ($appearanceText -match 'StartOpacity') -and
    ($appearanceText -match 'FadeRatio') -and
    ($appearanceText -match 'FadeExponent') -and
    ($appearanceText -match 'AltitudeOffset') -and
    ($appearanceText -notmatch 'NominalStepLength') -and
    ($appearanceText -notmatch 'CreateFrom\s*\(\s*BeamTrailConfig')
) 'BeamTrailAppearanceSnapshot must freeze only the visual fields that are actually rendered.'

Assert-True (
    ($attachmentText -match 'class\s+BeamTrailAttachment') -and
    ($attachmentText -match 'IProjectileVisualAttachment') -and
    ($attachmentText -match 'hasAnchor') -and
    ($attachmentText -match 'lastAnchor') -and
    ($attachmentText -match 'OnLaunch') -and
    ($attachmentText -match 'OnFlightSample') -and
    ($attachmentText -match 'OnRestored') -and
    ($attachmentText -match 'OnTerminate') -and
    ($attachmentText -match 'BeamTrailMapComponent') -and
    ($attachmentText -match 'PromoteLiveSegment\s*\(\s*context\.ProjectileThingId\s*\)') -and
    ($attachmentText -match 'SetLiveSegment\s*\(\s*context\.ProjectileThingId,\s*start,\s*end,\s*appearance\s*\)') -and
    ($attachmentText -match 'ClearLiveSegment\s*\(\s*context\.ProjectileThingId\s*\)') -and
    ($attachmentText -notmatch 'AppendSegment\(start,\s*end,\s*appearance\)') -and
    ($attachmentText -notmatch 'splitCount') -and
    ($attachmentText -notmatch 'Vector3\.Lerp') -and
    ($attachmentText -notmatch 'nominalStepLength')
) 'BeamTrailAttachment must keep anchor state, keep only the newest sample as a transient live head, and avoid solidifying the final segment on termination.'

Assert-True (
    ($segmentText -match 'class\s+BeamTrailSegment') -and
    ($segmentText -match 'IExposable') -and
    ($segmentText -match 'Start') -and
    ($segmentText -match 'End') -and
    ($segmentText -match 'TrailWidth\s*=\s*appearance\s*!=\s*null\s*\?\s*appearance\.TrailWidth\s*:\s*0\.1105f') -and
    ($segmentText -match 'Scribe_Values\.Look\(ref TrailWidth,\s*"trailWidth",\s*0\.1105f\)') -and
    ($segmentText -match 'TicksAlive') -and
    ($segmentText -match 'ExposeData') -and
    ($segmentText -match 'Tick') -and
    ($segmentText -match 'ResolveOpacity')
) 'BeamTrailSegment must be persistable and must own lifetime plus opacity resolution.'

Assert-True (
    ($mapComponentText -match 'class\s+BeamTrailMapComponent') -and
    ($mapComponentText -match 'MapComponent') -and
    ($mapComponentText -match 'activeSegments') -and
    ($mapComponentText -match 'liveSegmentsByProjectileId') -and
    ($mapComponentText -match 'pool') -and
    ($mapComponentText -match 'materialCache') -and
    ($mapComponentText -match 'AppendSegment') -and
    ($mapComponentText -match 'SetLiveSegment') -and
    ($mapComponentText -match 'PromoteLiveSegment') -and
    ($mapComponentText -match 'ClearLiveSegment') -and
    ($mapComponentText -match 'MapComponentTick') -and
    ($mapComponentText -match 'MapComponentDraw') -and
    ($mapComponentText -match 'ExposeData') -and
    ($mapComponentText -notmatch 'Scribe_Collections\.Look\(ref liveSegmentsByProjectileId')
) 'BeamTrailMapComponent must own pooled history segments plus transient live heads, draw both, and never persist live heads.'

Assert-True ($projectileBlock -ne $null) 'ThingDefs_TestProjectiles.xml must keep BDP_TestBulletSemantic.'
Assert-True (
    ($projectileBlock -notmatch 'BDP\.DevHarness\.Projectiles\.BeamTrail\.BeamTrailConfig')
) 'BDP_TestBulletSemantic must not own BeamTrailConfig after chip-side preset migration.'

Assert-True (Test-Path -LiteralPath $texturePath) 'BDP_BeamTrail.png must exist.'

Write-Output 'DevHarnessBeamTrailVisualSmokeTests PASS'
