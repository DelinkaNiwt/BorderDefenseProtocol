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
$contentRoot = Join-Path $sourceRoot 'BDP.Content'
$facilityRoot = Join-Path $contentRoot 'Trion\Facility'

$resourcePath = Join-Path $facilityRoot 'CompTrionFacilityResource.cs'
$resourcePropsPath = Join-Path $facilityRoot 'CompProperties_TrionFacilityResource.cs'
$drainProviderPath = Join-Path $facilityRoot 'ITrionFacilityDrainRateProvider.cs'
$upgradePath = Join-Path $facilityRoot 'CompTrionFacilityUpgrade.cs'
$buildingPath = Join-Path $facilityRoot 'Building_TrionFacility.cs'
$defPath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Buildings\TrionFacility.xml'
$languagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml'

Assert-True (Test-Path -LiteralPath $resourcePath) 'Content must own the Trion facility resource component.'
Assert-True (Test-Path -LiteralPath $resourcePropsPath) 'The Trion facility resource component must have Def properties.'
Assert-True (Test-Path -LiteralPath $drainProviderPath) 'The facility drain rate must have an external provider boundary.'
Assert-True (Test-Path -LiteralPath $upgradePath) 'Content must reserve a persistent facility upgrade state.'
Assert-True (Test-Path -LiteralPath $buildingPath) 'Content must own the landmark building class.'
Assert-True (Test-Path -LiteralPath $defPath) 'The landmark building ThingDef must be under Content/Defs.'
Assert-True (Test-Path -LiteralPath $languagePath) 'Gameplay localization must contain Trion facility debug labels.'

$resourceText = Get-Content -LiteralPath $resourcePath -Raw -Encoding utf8
$resourcePropsText = Get-Content -LiteralPath $resourcePropsPath -Raw -Encoding utf8
$drainProviderText = Get-Content -LiteralPath $drainProviderPath -Raw -Encoding utf8
$upgradeText = Get-Content -LiteralPath $upgradePath -Raw -Encoding utf8
$buildingText = Get-Content -LiteralPath $buildingPath -Raw -Encoding utf8
$defText = Get-Content -LiteralPath $defPath -Raw -Encoding utf8
$languageText = Get-Content -LiteralPath $languagePath -Raw -Encoding utf8

Assert-True (
    ($resourceText -match 'ThingComp') -and
    ($resourceText -match 'CurrentResource') -and
    ($resourceText -match 'MaxResource') -and
    ($resourceText -match 'IsOperational') -and
    ($resourceText -match 'TryAddResource') -and
    ($resourceText -match 'SettleResourceToTick')
) 'The resource component must expose bounded resource, operation state, refill, and time settlement surfaces.'

Assert-True (
    ($resourceText -match 'CompGetGizmosExtra') -and
    ($resourceText -match 'DebugSettings\.ShowDevGizmos') -and
    ($resourceText -match 'Command_Action') -and
    ($resourceText -match 'Command_Toggle') -and
    ($resourceText -match 'AdjustResourceForDebug') -and
    ($resourceText -match 'SetResourceForDebug') -and
    ($resourceText -match 'forcedOperational') -and
    ($resourceText -match 'Scribe_Values\.Look')
) 'The facility resource component must provide god-mode-only resource and forced-operation debug gizmos with persisted override state.'

Assert-True (
    ($languageText -match 'BDP_TrionFacility_Debug_Add50') -and
    ($languageText -match 'BDP_TrionFacility_Debug_Remove50') -and
    ($languageText -match 'BDP_TrionFacility_Debug_Fill') -and
    ($languageText -match 'BDP_TrionFacility_Debug_Empty') -and
    ($languageText -match 'BDP_TrionFacility_Debug_ForceOperation') -and
    ($languageText -match 'BDP_TrionFacility_Status_ForcedRunning')
) 'The facility debug gizmos and forced-running status must use Chinese localization keys.'

Assert-True (
    ($resourceText -match 'Scribe_Values\.Look') -and
    ($resourceText -match 'lastSettledTick') -and
    ($resourceText -match '60000')
) 'The resource component must persist its time baseline and settle daily drain by game time.'

Assert-True (
    ($resourcePropsText -match 'maxResource') -and
    ($resourcePropsText -match 'baseDrainPerDay') -and
    ($resourcePropsText -match 'settleInterval')
) 'The resource Def properties must configure independent capacity and drain settings.'

Assert-True (
    ($drainProviderText -match 'interface\s+ITrionFacilityDrainRateProvider') -and
    ($drainProviderText -match 'Resolve.*Drain.*PerDay')
) 'Future mood-dependent drain calculation must enter through an external provider.'

Assert-True (
    ($upgradeText -match 'UpgradeLevel') -and
    ($upgradeText -match 'HasCapability') -and
    ($upgradeText -match 'Scribe_Values\.Look')
) 'The upgrade component must persist only level/capability reservation state at this stage.'

Assert-True (
    ($buildingText -match 'Building') -and
    ($buildingText -match 'TrionFacility')
) 'The landmark building must be a dedicated Content building class.'

Assert-True (
    ($defText -match '<size>\(7,7\)</size>') -and
    ($defText -match '<drawSize>\(7,7\)</drawSize>') -and
    ($defText -match '<texPath>Things/Building/Ship/ShipReactor</texPath>') -and
    ($defText -match '<designationCategory>Biotech</designationCategory>') -and
    ($defText -notmatch '<staticSunShadowHeight>') -and
    ($defText -notmatch 'CompProperties_Power|CompPowerTrader|CompProperties_Flickable')
) 'The landmark Def must be available in the Biotech tab, align its 7x7 occupied and visual bounds, reuse ShipReactor art, keep custom shadow data compatible, and have no power component.'

Write-Output 'TrionFacilityResource PASS'
