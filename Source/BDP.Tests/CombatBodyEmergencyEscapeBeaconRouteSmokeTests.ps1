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

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$routerPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\CombatBodyEmergencyEscapeRouter.cs'
$buildingPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\Building_EmergencyEscapeBeacon.cs'
$placeWorkerPath = Join-Path $repoRoot 'Source\BDP.Content\CombatBody\Escape\PlaceWorker_EmergencyEscapeBeaconOnlyOnePerMap.cs'
$trionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Trion\TrionSurfaceAccess.cs'
$xmlPath = Join-Path $repoRoot '1.6\Content\Defs\Buildings\CombatBody\ThingDefs_EmergencyEscapeBeacon.xml'

Assert-True (Test-Path -LiteralPath $routerPath) 'CombatBodyEmergencyEscapeRouter must exist.'
Assert-True (Test-Path -LiteralPath $trionSurfacePath) 'TrionSurfaceAccess must exist.'
Assert-True (Test-Path -LiteralPath $buildingPath) 'Building_EmergencyEscapeBeacon must exist.'
Assert-True (Test-Path -LiteralPath $placeWorkerPath) 'PlaceWorker_OnlyOnePerMap must exist.'
Assert-True (Test-Path -LiteralPath $xmlPath) 'Emergency escape beacon XML must exist.'

$routerText = Get-Content -LiteralPath $routerPath -Raw -Encoding utf8
$buildingText = Get-Content -LiteralPath $buildingPath -Raw -Encoding utf8
$placeWorkerText = Get-Content -LiteralPath $placeWorkerPath -Raw -Encoding utf8
$trionSurfaceText = Get-Content -LiteralPath $trionSurfacePath -Raw -Encoding utf8
$xmlText = Get-Content -LiteralPath $xmlPath -Raw -Encoding utf8

Assert-True (
    ($xmlText -match '<defName>BDP_EmergencyEscapeBeacon</defName>') -and
    ($xmlText -match '<thingClass>BDP\.Content\.CombatBody\.Escape\.Building_EmergencyEscapeBeacon</thingClass>') -and
    ($xmlText -match '<texPath>Things/Building/Misc/DropBeacon</texPath>') -and
    ($xmlText -match 'CompProperties_Power') -and
    ($xmlText -match 'CompPowerTrader') -and
    ($xmlText -match 'CompProperties_Flickable') -and
    ($xmlText -match 'BDP\.Core\.Trion\.CompProperties_Trion') -and
    ($xmlText -match 'BDP\.Content\.CombatBody\.Escape\.PlaceWorker_EmergencyEscapeBeaconOnlyOnePerMap')
) 'Emergency escape beacon XML must define powered orbital-beacon-textured Trion-ready building.'

Assert-True (
    ($buildingText -match 'class\s+Building_EmergencyEscapeBeacon\s*:\s*Building') -and
    ($buildingText -match 'public\s+bool\s+IsActiveAnchor') -and
    ($buildingText -match 'CompPowerTrader') -and
    ($buildingText -match 'TrionReader') -and
    ($buildingText -match 'TrionCommands')
) 'Emergency escape beacon building must expose active anchor and Trion surfaces.'

Assert-True (
    ($placeWorkerText -match 'class\s+PlaceWorker_EmergencyEscapeBeaconOnlyOnePerMap\s*:\s*PlaceWorker') -and
    ($placeWorkerText -match 'AllBuildingsColonistOfDef')
) 'PlaceWorker_OnlyOnePerMap must limit duplicate colonist buildings of the same def.'

Assert-True (
    ($routerText -match 'TryFindBeaconDestination') -and
    ($routerText -match 'TryFindColonistAreaDestination') -and
    ($routerText -match 'TryFindLocalSafeDestination') -and
    ($routerText -match 'TryFindMapSafeDestination') -and
    ($routerText -match 'AllBuildingsColonistOfClass<Building_EmergencyEscapeBeacon>')
) 'Emergency escape router must preserve beacon/colonist/local/map fallback order.'

Assert-True (
    ($trionSurfaceText -match 'ResolveReader\(ThingWithComps thing\)') -and
    ($trionSurfaceText -match 'ResolveCommands\(ThingWithComps thing\)') -and
    ($trionSurfaceText -match 'ResolveEvents\(ThingWithComps thing\)')
) 'TrionSurfaceAccess must expose non-Pawn ThingWithComps surfaces for future building integration.'

Assert-True (
    ($routerText -notmatch 'ChipSurfaceAccess|SupportsEmergencyEscape|TryGetPassive|Resolve\(pawn\)')
) 'Router must not re-own emergency escape availability or chip parsing.'

Write-Output 'CombatBodyEmergencyEscapeBeaconRoute PASS'
