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
    'DevHarness must retain formal chip definitions for slot-region authoring validation.'

$legalCount = 0
foreach ($thingDef in $chipDefinitions) {
    $defName = [string]$thingDef.SelectSingleNode('defName').InnerText
    $loadout = $thingDef.SelectSingleNode(
        "modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']/Loadout")
    $legalCount++
    Assert-True ($null -ne $loadout) ($defName + ' must retain one Loadout block.')
    $regionNodes = $loadout.SelectNodes('SlotRegion')
    Assert-True ($regionNodes.Count -eq 1) `
        ($defName + ' must explicitly declare exactly one SlotRegion.')
    $regionValue = [string]$regionNodes[0].InnerText
    Assert-True ($regionValue -eq 'MainSub' -or $regionValue -eq 'Special') `
        ($defName + ' must use MainSub or Special as SlotRegion.')
}

Assert-True ($legalCount -gt 0) `
    'DevHarness must retain legal chips that explicitly declare SlotRegion.'

$versionXmlText = ($xmlFiles | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

Assert-True (
    ($versionXmlText -notmatch '<SidePolicy>') -and
    ($versionXmlText -notmatch '\bHandsOnly\b|\bSpecialOnly\b')
) 'DevHarness version XML must not retain the old slot-region authoring names.'

Write-Output 'DevHarnessChipSlotRegionAuthoringSmokeTests PASS'
