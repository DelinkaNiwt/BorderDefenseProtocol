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

    $escapedDefName = [regex]::Escape($DefName)
    $matches = [regex]::Matches(
        $Text,
        "(?s)<ThingDef\b[^>]*>.*?</ThingDef>")

    for ($i = 0; $i -lt $matches.Count; $i++) {
        $block = $matches[$i].Value
        if ($block -match "<defName>$escapedDefName</defName>") {
            return $block
        }
    }

    return $null
}

function Assert-SequentialShortTrailChip {
    param(
        [string]$Text,
        [string]$DefName
    )

    $block = Get-ThingDefBlock $Text $DefName

    Assert-True ($block -ne $null) "Combat chip XML must keep $DefName."
    Assert-True (
        ($block -match '<Rhythm>Sequential</Rhythm>')
    ) "$DefName must explicitly declare sequential ranged execution rhythm."
    Assert-True (
        ($block -match '<preset>BDP_TrailPreset_BrightMintShort</preset>')
    ) "$DefName must use the very short tracking trail preset."
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$beamTrailRoot = Join-Path $repoRoot 'Source\BDP.Content\Projectiles\BeamTrail'

$presetDefPath = Join-Path $beamTrailRoot 'BeamTrailPresetDef.cs'
$chipExtensionPath = Join-Path $beamTrailRoot 'BeamTrailExtension.cs'
$appearancePath = Join-Path $beamTrailRoot 'BeamTrailAppearanceSnapshot.cs'
$presetXmlPath = Join-Path $repoRoot '1.6\Content\Defs\Projectiles\BeamTrail\BeamTrailPresetDefs.xml'
$candidatePresetXmlPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Projectiles\Test\BeamTrailPresetDefs.xml'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'

$presetDefText = if (Test-Path -LiteralPath $presetDefPath) { Read-Source $presetDefPath } else { '' }
$chipExtensionText = if (Test-Path -LiteralPath $chipExtensionPath) { Read-Source $chipExtensionPath } else { '' }
$appearanceText = if (Test-Path -LiteralPath $appearancePath) { Read-Source $appearancePath } else { '' }
$presetXmlText = if (Test-Path -LiteralPath $presetXmlPath) { Read-Source $presetXmlPath } else { '' }
$candidatePresetXmlText = if (Test-Path -LiteralPath $candidatePresetXmlPath) { Read-Source $candidatePresetXmlPath } else { '' }
$chipText = if (Test-Path -LiteralPath $chipDefsPath) { Read-Source $chipDefsPath } else { '' }
$trackingChipBlock = Get-ThingDefBlock $chipText 'BDP_TestChipTracking'
$trackingVolleyChipBlock = Get-ThingDefBlock $chipText 'BDP_TestChipTrackingVolley'
$volleyChipBlock = Get-ThingDefBlock $chipText 'BDP_TestChipRangedVolley'
$pathLatchVolleyChipBlock = Get-ThingDefBlock $chipText 'BDP_TestChipPathLatchVolley'

Assert-True (Test-Path -LiteralPath $presetDefPath) 'BeamTrailPresetDef.cs must exist.'
Assert-True (Test-Path -LiteralPath $chipExtensionPath) 'BeamTrailExtension.cs must exist.'

Assert-True (
    ($presetDefText -match 'class\s+BeamTrailPresetDef\s*:\s*Def') -and
    ($presetDefText -match 'trailTexPath') -and
    ($presetDefText -match 'trailColor') -and
    ($presetDefText -match 'trailWidth') -and
    ($presetDefText -match 'segmentLifetimeTicks') -and
    ($presetDefText -match 'fadeExponent')
) 'BeamTrailPresetDef must expose visual preset fields.'

Assert-True (
    ($chipExtensionText -match 'class\s+BeamTrailExtension\s*:\s*DefModExtension,\s*IProjectileVisualAttachmentProvider') -and
    ($chipExtensionText -match 'BeamTrailPresetDef\s+preset') -and
    ($chipExtensionText -notmatch 'public\s+bool\s+enabled') -and
    ($chipExtensionText -match 'CreateAttachment') -and
    ($chipExtensionText -match 'new\s+BeamTrailAttachment')
) 'BeamTrailExtension must create one BeamTrailAttachment per projectile from one preset field.'

Assert-True (
    ($appearanceText -match 'CreateFrom\s*\(\s*BeamTrailPresetDef\s+preset\s*\)')
) 'BeamTrailAppearanceSnapshot must support BeamTrailPresetDef.'

Assert-True (Test-Path -LiteralPath $presetXmlPath) 'BeamTrailPresetDefs.xml must exist.'
Assert-True (
    ($presetXmlText -match 'BDP\.Content\.Projectiles\.BeamTrail\.BeamTrailPresetDef') -and
    ($presetXmlText -match 'BDP_TrailPreset_BrightMintLong') -and
    ($presetXmlText -match 'BDP_TrailPreset_BrightMintShort') -and
    ($presetXmlText -notmatch 'BDP_TrailPreset_HotRed') -and
    ($presetXmlText -match '<segmentLifetimeTicks>6</segmentLifetimeTicks>') -and
    ($presetXmlText -match '<fadeExponent>2\.5</fadeExponent>')
) 'Main BeamTrailPresetDefs.xml must define the formal bright mint long and short presets.'

Assert-True (
    (-not (Test-Path -LiteralPath $candidatePresetXmlPath)) -and
    [string]::IsNullOrEmpty($candidatePresetXmlText)
) 'Candidate mod must not keep a local beam trail preset file after retiring the hot red preset.'

Assert-True (
    ($chipText -match 'BDP\.Content\.Projectiles\.BeamTrail\.BeamTrailExtension') -and
    ($chipText -match 'BDP_TrailPreset_BrightMintLong') -and
    ($chipText -match 'BDP_TrailPreset_BrightMintShort') -and
    ($chipText -notmatch 'BDP_TrailPreset_HotRed')
) 'Combat test chips must mount chip-side beam trail presets.'

Assert-SequentialShortTrailChip $chipText 'BDP_TestChipRanged'
Assert-SequentialShortTrailChip $chipText 'BDP_TestChipPathLatch'
Assert-SequentialShortTrailChip $chipText 'BDP_TestChipExplosiveRanged'

# 追踪两种芯片按用户需求改用长拖尾,不再受"短拖尾"契约约束。
Assert-True ($trackingChipBlock -ne $null) 'Combat chip XML must keep BDP_TestChipTracking.'
Assert-True (
    ($trackingChipBlock -match '<preset>BDP_TrailPreset_BrightMintLong</preset>')
) 'BDP_TestChipTracking must use the long tracking trail preset.'

Assert-True ($trackingVolleyChipBlock -ne $null) 'Combat chip XML must keep BDP_TestChipTrackingVolley.'
Assert-True (
    ($trackingVolleyChipBlock -match '<preset>BDP_TrailPreset_BrightMintLong</preset>')
) 'BDP_TestChipTrackingVolley must use the long tracking trail preset.'

Assert-True ($volleyChipBlock -ne $null) 'Combat chip XML must keep BDP_TestChipRangedVolley.'
Assert-True (
    ($volleyChipBlock -match '<preset>BDP_TrailPreset_BrightMintLong</preset>') -and
    ($volleyChipBlock -notmatch 'BDP_TrailPreset_BrightMintShort|BDP_TrailPreset_HotRed')
) 'BDP_TestChipRangedVolley must use the bright mint long trail preset.'

Assert-True ($pathLatchVolleyChipBlock -ne $null) 'Combat chip XML must keep BDP_TestChipPathLatchVolley.'
Assert-True (
    ($pathLatchVolleyChipBlock -match '<preset>BDP_TrailPreset_BrightMintLong</preset>') -and
    ($pathLatchVolleyChipBlock -notmatch 'BDP_TrailPreset_BrightMintShort|BDP_TrailPreset_HotRed')
) 'BDP_TestChipPathLatchVolley must use the bright mint long trail preset.'

Write-Output 'DevHarnessBeamTrailPresetInjectionSmokeTests PASS'
