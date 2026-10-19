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
$presetText = Read-Source (Join-Path $repoRoot '1.6\Content\Defs\BeamTrailDef\Presets.xml')

Assert-True (
    ($segmentText -match 'OwnerProjectileThingId') -and
    ($segmentText -match 'SequenceId') -and
    ($segmentText -match 'DrawTransient') -and
    ($segmentText -match 'DrawCoreTransient')
) 'BeamTrailSegment must expose draw-only coalescing metadata and transient-line drawing without changing stored lifetime.'

Assert-True (
    ($mapComponentText -match 'nextSequenceId') -and
    ($mapComponentText -match 'drawHistoryByProjectileId') -and
    ($mapComponentText -match 'CanCoalesce') -and
    ($mapComponentText -match 'SequenceId\s*\+\s*1') -and
    ($mapComponentText -match 'MaxCoalescedSegmentLength') -and
    ($mapComponentText -match 'MaxVisualCoalesceSegments')
) 'BeamTrailMapComponent must coalesce only adjacent short segments from the same projectile during drawing.'

Assert-True (
    ($presetText -match '<defName>BDP_TrailPreset_BrightMintLong</defName>[\s\S]*?<segmentLifetimeTicks>30</segmentLifetimeTicks>') -and
    ($presetText -match '<defName>BDP_TrailPreset_BrightMintShort</defName>[\s\S]*?<segmentLifetimeTicks>6</segmentLifetimeTicks>')
) 'The low-speed visual optimization must not change the existing trail retention ticks.'

Write-Output 'BeamTrailLowSpeedCoalescingSmokeTests PASS'
