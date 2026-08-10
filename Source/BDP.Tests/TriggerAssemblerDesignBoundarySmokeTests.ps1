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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP.Content'

$assemblyRoot = Join-Path $bdpSourceRoot 'Assembly'
$assemblerPath = Join-Path $assemblyRoot 'Building\Building_TriggerAssembler.cs'
$chipContainerPath = Join-Path $assemblyRoot 'Building\CompChipContainer.cs'
$itabsPath = Join-Path $assemblyRoot 'Building\ITab_ChipStorageContents.cs'
$providerPath = Join-Path $assemblyRoot 'Facility\DefaultAssemblerFacilityProvider.cs'
$assemblerDefPath = Join-Path $repoRoot '1.6\Content\Defs\Buildings\Assembly\ThingDefs_TriggerAssembler.xml'
$storageDefPath = Join-Path $repoRoot '1.6\Content\Defs\Buildings\Assembly\ThingDefs_ChipStorage.xml'

Assert-True (Test-Path -LiteralPath $assemblerPath) 'Building_TriggerAssembler must exist.'
Assert-True (Test-Path -LiteralPath $chipContainerPath) 'CompChipContainer must exist.'
Assert-True (Test-Path -LiteralPath $itabsPath) 'ITab_ChipStorageContents must exist.'
Assert-True (Test-Path -LiteralPath $providerPath) 'DefaultAssemblerFacilityProvider must exist.'
Assert-True (Test-Path -LiteralPath $assemblerDefPath) 'Trigger assembler ThingDef must exist.'
Assert-True (Test-Path -LiteralPath $storageDefPath) 'Chip storage ThingDef must exist.'

$assemblerText = Get-Content -LiteralPath $assemblerPath -Raw -Encoding utf8
$chipContainerText = Get-Content -LiteralPath $chipContainerPath -Raw -Encoding utf8
$itabsText = Get-Content -LiteralPath $itabsPath -Raw -Encoding utf8
$providerText = Get-Content -LiteralPath $providerPath -Raw -Encoding utf8
$assemblerDefText = Get-Content -LiteralPath $assemblerDefPath -Raw -Encoding utf8
$storageDefText = Get-Content -LiteralPath $storageDefPath -Raw -Encoding utf8

Assert-True (
    $assemblerText -match 'class\s+Building_TriggerAssembler\s*:\s*Building'
) 'Building_TriggerAssembler must inherit vanilla Building.'

Assert-True (
    $storageDefText -notmatch 'StorageShelfBase|ITab_Storage|Building_Storage|CompProperties_AssemblerChipStorage|Building_ChipStorage'
) 'Chip storage ThingDef must not use the old shelf or Building_Storage route.'

Assert-True (
    $storageDefText -match '<thingClass>\s*Building\s*</thingClass>'
) 'Chip storage ThingDef must use vanilla Building as thingClass.'

Assert-True (
    $storageDefText -match 'BDP\.Content\.Assembly\.CompProperties_ChipContainer'
) 'Chip storage ThingDef must use CompProperties_ChipContainer.'

Assert-True (
    $storageDefText -match '<texPath>\s*Things/Building/Misc/Genebank/Genebank\s*</texPath>'
) 'Chip storage ThingDef must temporarily use the vanilla Genebank texture.'

Assert-True (
    $assemblerDefText -match '<texPath>\s*Things/Building/Misc/GeneAssembler/GeneAssembler\s*</texPath>'
) 'Trigger assembler ThingDef must temporarily use the vanilla GeneAssembler texture.'

Assert-True (
    $assemblerDefText -match 'CompProperties_AffectedByFacilities|CompAffectedByFacilities'
) 'Trigger assembler ThingDef must use vanilla CompAffectedByFacilities.'

Assert-True (
    $storageDefText -match 'CompProperties_Facility|CompFacility'
) 'Chip storage ThingDef must use vanilla CompFacility.'

Assert-True (
    $chipContainerText -match 'class\s+CompChipContainer\s*:\s*ThingComp\s*,\s*IThingHolder'
) 'CompChipContainer must be a ThingComp and IThingHolder.'

Assert-True (
    $chipContainerText -match 'ThingOwner<Thing>' -and
    $chipContainerText -match 'TryAcceptChip' -and
    $chipContainerText -match 'TryTakeChip' -and
    $chipContainerText -match 'EjectContents'
) 'CompChipContainer must hold chips internally and expose accept/take/eject methods.'

Assert-True (
    $chipContainerText -match 'canMergeWithExistingStacks:\s*false'
) 'CompChipContainer must keep chip Thing identity by disabling stack merges.'

Assert-True (
    $itabsText -match 'TryGetComp<CompChipContainer>' -and
    $itabsText -notmatch 'OccupiedRect|Building_Storage'
) 'ITab_ChipStorageContents must read CompChipContainer instead of ground cells.'

Assert-True (
    $itabsText -match 'protected\s+override\s+void\s+DoItemsLists' -and
    $itabsText -match 'UI/Buttons/Drop' -and
    $itabsText -match 'Widgets\.ButtonImage'
) 'ITab_ChipStorageContents must draw a genebank-style chip list with a drop arrow.'

Assert-True (
    $itabsText -notmatch 'CaravanThingsTabUtility\.AbandonButtonTex|CaravanThingsTabUtility\.AbandonSpecificCountButtonTex'
) 'ITab_ChipStorageContents must not expose the generic red discard buttons.'

Assert-True (
    $itabsText -notmatch 'Dialog_MessageBox\.CreateConfirmation'
) 'ITab_ChipStorageContents drop arrow must eject chips directly without a confirmation dialog.'

Assert-True (
    $itabsText -match 'Widgets\.ButtonImage\(dropRect,\s*DropTex\.Texture\)[\s\S]*?OnDropThing\(chip,\s*chip\.stackCount\)'
) 'ITab_ChipStorageContents drop arrow must directly call OnDropThing for the selected chip.'

Assert-True (
    $providerText -match 'CompChipContainer' -and
    $providerText -notmatch 'CompAssemblerChipStorage'
) 'DefaultAssemblerFacilityProvider must read CompChipContainer.'

if (Test-Path -LiteralPath $assemblyRoot) {
    $assemblyTexts = Get-ChildItem -LiteralPath $assemblyRoot -Recurse -File -Filter '*.cs' |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8 }

    Assert-True (
        ($assemblyTexts -join "`n") -notmatch 'CompAssemblerChipStorage|CompProperties_AssemblerChipStorage|Building_ChipStorage|Building_Storage'
    ) 'Assembly source must not retain old shelf-storage adapter types.'
}

Write-Output 'TriggerAssemblerDesignBoundary PASS'
