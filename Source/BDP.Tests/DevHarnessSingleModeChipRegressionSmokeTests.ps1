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

function Find-ThingDef {
    param(
        [xml[]]$Documents,
        [string]$DefName
    )

    foreach ($document in $Documents) {
        $node = $document.SelectSingleNode("/Defs/ThingDef[defName='$DefName']")
        if ($null -ne $node) {
            return $node
        }
    }

    return $null
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$candidateChipDefRoot = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test'
$mainChipDefRoot = Join-Path $repoRoot '1.6\Content\Defs\Things\Items\Chips\Shield'

$documents = @(
    [xml](Get-Content -LiteralPath (Join-Path $candidateChipDefRoot 'ThingDefs_TestChips_Combat.xml') -Raw -Encoding utf8),
    [xml](Get-Content -LiteralPath (Join-Path $mainChipDefRoot 'ThingDefs_Chip_EnergyShield.xml') -Raw -Encoding utf8),
    [xml](Get-Content -LiteralPath (Join-Path $candidateChipDefRoot 'ThingDefs_TestChips_AbilityHediff.xml') -Raw -Encoding utf8),
    [xml](Get-Content -LiteralPath (Join-Path $candidateChipDefRoot 'ThingDefs_TestChips_PassiveMixed.xml') -Raw -Encoding utf8)
)

$expectedKinds = @{
    BDP_TestChipPathLatchVolley = 'PrimaryVerb'
    BDP_Chip_EnergyShield = 'Hediff'
    BDP_TestChipHediff = 'Hediff'
    BDP_TestChipAbility = 'Ability'
    BDP_TestChipPassive = 'Passive'
}

foreach ($defName in $expectedKinds.Keys) {
    $thingDef = Find-ThingDef $documents $defName
    Assert-True ($null -ne $thingDef) ("Required single-mode chip is missing: " + $defName)

    $expression = $thingDef.SelectSingleNode(
        "modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']/Expression")
    Assert-True ($null -ne $expression) ($defName + ' must retain one formal Expression block.')

    $entries = $expression.SelectNodes('Entries/li')
    Assert-True ($null -ne $entries -and $entries.Count -gt 0) `
        ($defName + ' must retain at least one unified expression entry.')
    Assert-True ($null -eq $expression.SelectSingleNode('Modes')) `
        ($defName + ' must remain a single-mode chip without Modes.')
    Assert-True ($null -eq $expression.SelectSingleNode('DefaultModeKey')) `
        ($defName + ' must not declare DefaultModeKey as a single-mode chip.')
    Assert-True ($null -eq $thingDef.SelectSingleNode('.//InitialModeKey')) `
        ($defName + ' must not use removed InitialModeKey.')
    Assert-True ($null -eq $thingDef.SelectSingleNode('.//Operations')) `
        ($defName + ' must not use removed mode Operations.')

    $expectedKind = $expectedKinds[$defName]
    $matchingKind = $entries | Where-Object { [string]$_.Kind -eq $expectedKind }
    Assert-True ($null -ne $matchingKind) `
        ($defName + ' must retain its key expression kind: ' + $expectedKind)
}

$passiveDef = Find-ThingDef $documents 'BDP_TestChipPassive'
$emergencyEscape = $passiveDef.SelectSingleNode(
    "modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']/Expression/Entries/li[Kind='Passive' and PassiveKey='EmergencyEscape']")
Assert-True ($null -ne $emergencyEscape) `
    'The single-mode passive chip must retain the EmergencyEscape declaration.'

$hediffDef = Find-ThingDef $documents 'BDP_TestChipHediff'
$sustainRows = $hediffDef.SelectNodes(
    "modExtensions/li[@Class='BDP.Core.Chips.ChipDefinitionConfig']/Expression/Entries/li[Kind='Hediff']/Trion/SustainCostBySourceCount/li")
Assert-True ($sustainRows.Count -eq 2) `
    'The single-mode state chip must retain two sustain-cost tiers.'
Assert-True (
    ([int]$sustainRows[0].SourceCount -eq 1) -and
    ([float]$sustainRows[0].TotalPerSecond -eq 2.0)
) 'The state chip first sustain tier must remain 1 source = 2 Trion/second.'
Assert-True (
    ([int]$sustainRows[1].SourceCount -eq 2) -and
    ([float]$sustainRows[1].TotalPerSecond -eq 5.0)
) 'The state chip second sustain tier must remain 2 sources = 5 Trion/second.'

Write-Output 'ChipSingleModeRegressionSmokeTests PASS'
