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
$mainBodyPath = Join-Path $repoRoot '1.6\Content\Defs\Things\Equipment\Trigger\ThingDefs_TriggerBodies.xml'
$coreBodyPath = Join-Path $repoRoot '1.6\Defs\Things\Equipment\Trigger\ThingDefs_TriggerBodies.xml'
$oldCandidatePath = Join-Path $repoRoot '..\BorderDefenseProtocol.DevHarness\1.6\Defs\Things\Equipment\Trigger\Test\ThingDefs_TestTriggerBody.xml'
$texturePath = Join-Path $repoRoot '1.6\Textures\Things\Equipment\Trigger\BDP_BorderStandardTriggerBody.png'

Assert-True (Test-Path -LiteralPath $mainBodyPath -PathType Leaf) 'Formal BorderStandard trigger body must exist in the main mod.'
Assert-True (-not (Test-Path -LiteralPath $coreBodyPath -PathType Leaf)) 'Content trigger body must not remain in the Core Def root.'
Assert-True (-not (Test-Path -LiteralPath $oldCandidatePath -PathType Leaf)) 'The old candidate test trigger body must be removed.'
Assert-True (Test-Path -LiteralPath $texturePath -PathType Leaf) 'The BorderStandard trigger body texture must exist.'

$bodyText = Get-Content -LiteralPath $mainBodyPath -Raw -Encoding UTF8

Assert-True (
    ($bodyText -match '<defName>BDP_TriggerBody_BorderStandard</defName>') -and
    ($bodyText -match '<label>边境标准触发体</label>') -and
    ($bodyText -match '<triggerCategory>BDP_TriggerCategory_Border</triggerCategory>') -and
    ($bodyText -match '<texPath>Things/Equipment/Trigger/BDP_BorderStandardTriggerBody</texPath>')
) 'BorderStandard trigger body identity, category and texture path are incorrect.'

Assert-True (
    ($bodyText -match 'BDP\.Content\.Trigger\.UI\.TriggerLoadoutPanelExtension') -and
    ($bodyText -match '<mainSlotCount>4</mainSlotCount>') -and
    ($bodyText -match '<subSlotCount>4</subSlotCount>') -and
    ($bodyText -match '<specialSlotCount>2</specialSlotCount>')
) 'BorderStandard trigger body must preserve panel permission and 4/4/2 slot counts.'

Assert-True ($bodyText -notmatch 'BDP_TestTriggerBody|测试触发体') `
    'Formal BorderStandard trigger body must not retain the old test identity.'

Write-Output 'BorderStandardTriggerBodyFormalizationSmokeTests PASS'
