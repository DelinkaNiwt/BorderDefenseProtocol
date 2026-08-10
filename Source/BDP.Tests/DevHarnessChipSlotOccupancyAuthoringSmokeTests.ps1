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
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$versionRoot = Join-Path $devHarnessRoot '1.6'
$xmlFiles = @(Get-ChildItem -LiteralPath $versionRoot -Recurse -Filter '*.xml' -File)
$chipDefinitions = [System.Collections.Generic.List[System.Xml.XmlElement]]::new()

foreach ($xmlFile in $xmlFiles) {
    [xml]$document = Get-Content -LiteralPath $xmlFile.FullName -Raw -Encoding utf8
    $nodes = $document.SelectNodes(
        "/Defs/ThingDef[modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']]")
    foreach ($node in $nodes) {
        [void]$chipDefinitions.Add($node)
    }
}

Assert-True ($chipDefinitions.Count -gt 0) `
    'DevHarness must retain formal chip definitions for occupancy authoring validation.'

$pairedDefName = 'BDP_TestChipDualWieldRanged'
$legalCount = 0
$singleCount = 0
$pairedCount = 0

foreach ($thingDef in $chipDefinitions) {
    $defName = [string]$thingDef.SelectSingleNode('defName').InnerText
    $loadout = $thingDef.SelectSingleNode(
        "modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']/Loadout")

    $legalCount++
    Assert-True ($null -ne $loadout) ($defName + ' must retain one Loadout block.')
    $occupancyNodes = $loadout.SelectNodes('SlotOccupancy')
    Assert-True ($occupancyNodes.Count -eq 1) `
        ($defName + ' must explicitly declare exactly one SlotOccupancy.')
    $occupancyValue = [string]$occupancyNodes[0].InnerText
    $regionValue = [string]$loadout.SelectSingleNode('SlotRegion').InnerText

    if ($defName -eq $pairedDefName) {
        Assert-True ($occupancyValue -eq 'PairedHands') `
            'The dual-weapon business sample must use PairedHands physical occupancy.'
        Assert-True ($regionValue -eq 'MainSub') `
            'PairedHands occupancy must belong to the MainSub region.'
        $pairedCount++
    }
    else {
        Assert-True ($occupancyValue -eq 'Single') `
            ($defName + ' must use Single physical occupancy.')
        $singleCount++
    }
}

Assert-True ($legalCount -eq 16) `
    ('Expected 16 legal chip definitions, found ' + $legalCount + '.')
Assert-True ($singleCount -eq 15) `
    ('Expected 15 Single chip definitions, found ' + $singleCount + '.')
Assert-True ($pairedCount -eq 1) `
    ('Expected one PairedHands chip definition, found ' + $pairedCount + '.')

$versionXmlText = ($xmlFiles | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True ($versionXmlText -notmatch '<IsDualWieldBinding>') `
    'DevHarness version XML must not retain the old dual-wield occupancy field.'

Write-Output 'DevHarnessChipSlotOccupancyAuthoringSmokeTests PASS'
