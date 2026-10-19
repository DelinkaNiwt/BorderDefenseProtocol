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
$facilityRoot = Join-Path $sourceRoot 'BDP.Content\Trion\Facility'
$mapPath = Join-Path $facilityRoot 'TrionFacilityMapComponent.cs'
$gatePath = Join-Path $facilityRoot 'TrionRecoveryFacilityGate.cs'
$buildingPath = Join-Path $facilityRoot 'Building_TrionFacility.cs'
$bootstrapPath = Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs'

Assert-True (Test-Path -LiteralPath $mapPath) 'Content must own a map-level Trion facility index.'
Assert-True (Test-Path -LiteralPath $gatePath) 'Content must provide the Trion recovery gate.'

$mapText = Get-Content -LiteralPath $mapPath -Raw -Encoding utf8
$gateText = Get-Content -LiteralPath $gatePath -Raw -Encoding utf8
$buildingText = Get-Content -LiteralPath $buildingPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding utf8

Assert-True (
    ($mapText -match 'MapComponent') -and
    ($mapText -match 'HasActivePlayerTrionFacility') -and
    ($mapText -match 'Building_TrionFacility')
) 'The map component must index Trion facilities and expose the active player-faction query.'

Assert-True (
    ($mapText -match 'AllBuildingsColonistOfClass') -or
    ($mapText -match 'Rebuild')
) 'The map component must be able to rebuild its index after loading.'

Assert-True (
    ($gateText -match 'ITrionRecoveryGate') -and
    ($gateText -match 'Faction\.OfPlayer') -and
    ($gateText -match 'Map') -and
    ($gateText -match 'HasActivePlayerTrionFacility')
) 'The Content gate must restrict only mapped player-faction pawns based on active facilities.'

Assert-True (
    ($buildingText -match 'SpawnSetup') -and
    ($buildingText -match 'DeSpawn') -and
    ($buildingText -match 'Register') -and
    ($buildingText -match 'Unregister')
) 'The landmark building must update the map index across its spawn lifecycle.'

Assert-True (
    ($bootstrapText -match 'TrionRecoveryGateRegistry') -and
    ($bootstrapText -match 'TrionRecoveryFacilityGate') -and
    ($bootstrapText -match 'Register')
) 'ContentBootstrap must register the Content recovery gate into the Core registry.'

Write-Output 'TrionFacilityMapGate PASS'
