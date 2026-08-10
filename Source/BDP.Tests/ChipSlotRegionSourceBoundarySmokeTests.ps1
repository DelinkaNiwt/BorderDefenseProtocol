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
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'

$newEnumPath = Join-Path $coreRoot 'Chips\Config\ChipSlotRegion.cs'
$oldEnumPath = Join-Path $coreRoot 'Chips\Config\ChipLoadoutSidePolicy.cs'
$configPath = Join-Path $coreRoot 'Chips\Config\ChipLoadoutConfig.cs'
$contractPath = Join-Path $coreRoot 'Chips\Contract\ChipLoadoutContract.cs'
$validatorPath = Join-Path $coreRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'
$runtimePayloadPath = Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs'
$collectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'
$switchServicePath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchService.cs'

Assert-True (Test-Path -LiteralPath $newEnumPath) `
    'ChipSlotRegion enum source must exist.'
Assert-True (-not (Test-Path -LiteralPath $oldEnumPath)) `
    'The old ChipLoadoutSidePolicy enum source must be removed.'

$enumText = Get-Content -LiteralPath $newEnumPath -Raw -Encoding utf8
$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8
$contractText = Get-Content -LiteralPath $contractPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$runtimePayloadText = Get-Content -LiteralPath $runtimePayloadPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8
$switchServiceText = Get-Content -LiteralPath $switchServicePath -Raw -Encoding utf8

Assert-True (
    ($enumText -match '\bUnspecified\s*=\s*0\b') -and
    ($enumText -match '\bMainSub\s*=\s*1\b') -and
    ($enumText -match '\bSpecial\s*=\s*2\b') -and
    ($enumText -notmatch '\bHandsOnly\b|\bSpecialOnly\b')
) 'ChipSlotRegion must expose only the confirmed sentinel and two legal regions.'

Assert-True (
    ($configText -match '\bChipSlotRegion\s+SlotRegion\b') -and
    ($configText -match 'SlotRegion\s*=\s*ChipSlotRegion\.Unspecified') -and
    ($configText -notmatch '\bSidePolicy\b')
) 'Loadout config must publish required SlotRegion with an Unspecified sentinel.'

Assert-True (
    ($contractText -match '\bChipSlotRegion\s+SlotRegion\b') -and
    ($contractText -notmatch '\bSidePolicy\b')
) 'Loadout contract must publish SlotRegion only.'

Assert-True (
    ($validatorText -match 'ChipSlotRegion\.Unspecified') -and
    ($validatorText -match 'SlotRegionMissingOrInvalid') -and
    ($validatorText -match 'ChipDefinitionValidationSeverity\.Error')
) 'Missing or invalid SlotRegion must be a definition error.'

Assert-True (
    ($runtimePayloadText -match '\bChipSlotRegion\?\s+LoadoutSlotRegion\b') -and
    ($runtimePayloadText -notmatch '\bLoadoutSidePolicy\b') -and
    ($collectorText -match '\bLoadoutSlotRegion\b') -and
    ($collectorText -notmatch '\bLoadoutSidePolicy\b')
) 'Expression runtime payload must carry LoadoutSlotRegion only.'

Assert-True (
    ($switchServiceText -match 'IsSlotRegionAllowed') -and
    ($switchServiceText -match 'ChipSlotRegion\.MainSub') -and
    ($switchServiceText -match 'ChipSlotRegion\.Special') -and
    ($switchServiceText -notmatch 'IsSideAllowed|ChipLoadoutSidePolicy')
) 'Trigger switching must enforce the new slot region enum only.'

$productionTexts = @(
    $enumText,
    $configText,
    $contractText,
    $validatorText,
    $runtimePayloadText,
    $collectorText,
    $switchServiceText
) -join "`n"

Assert-True (
    $productionTexts -notmatch
        '\bChipLoadoutSidePolicy\b|\bLoadoutSidePolicy\b|\bHandsOnly\b|\bSpecialOnly\b'
) 'Core slot-region production sources must not retain the old authoring names.'

Write-Output 'ChipSlotRegionSourceBoundarySmokeTests PASS'
