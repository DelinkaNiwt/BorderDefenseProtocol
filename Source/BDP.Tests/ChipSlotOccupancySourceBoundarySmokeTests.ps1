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

$enumPath = Join-Path $coreRoot 'Chips\Config\ChipSlotOccupancy.cs'
$configPath = Join-Path $coreRoot 'Chips\Config\ChipLoadoutConfig.cs'
$contractPath = Join-Path $coreRoot 'Chips\Contract\ChipLoadoutContract.cs'
$resolverPath = Join-Path $coreRoot 'Chips\Contract\DefaultChipDefinitionContractResolver.cs'
$validatorPath = Join-Path $coreRoot 'Chips\Validation\DefaultChipDefinitionValidator.cs'
$runtimePayloadPath = Join-Path $coreRoot 'Expressions\Model\ExpressionRuntimePayload.cs'
$collectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'
$loadoutServicePath = Join-Path $coreRoot 'Trigger\Loadout\TriggerLoadoutService.cs'
$switchServicePath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchService.cs'
$transitionServicePath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchTransitionService.cs'

Assert-True (Test-Path -LiteralPath $enumPath) `
    'ChipSlotOccupancy enum source must exist.'

$enumText = Get-Content -LiteralPath $enumPath -Raw -Encoding utf8
$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8
$contractText = Get-Content -LiteralPath $contractPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$runtimePayloadText = Get-Content -LiteralPath $runtimePayloadPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8
$loadoutServiceText = Get-Content -LiteralPath $loadoutServicePath -Raw -Encoding utf8
$switchServiceText = Get-Content -LiteralPath $switchServicePath -Raw -Encoding utf8
$transitionServiceText = Get-Content -LiteralPath $transitionServicePath -Raw -Encoding utf8

Assert-True (
    ($enumText -match '\bUnspecified\s*=\s*0\b') -and
    ($enumText -match '\bSingle\s*=\s*1\b') -and
    ($enumText -match '\bPairedHands\s*=\s*2\b')
) 'ChipSlotOccupancy must expose the confirmed sentinel and two legal occupancy modes.'

Assert-True (
    ($configText -match '\bChipSlotOccupancy\s+SlotOccupancy\b') -and
    ($configText -match 'SlotOccupancy\s*=\s*ChipSlotOccupancy\.Unspecified')
) 'Loadout config must publish required SlotOccupancy with an Unspecified sentinel.'

Assert-True (
    ($contractText -match '\bChipSlotOccupancy\s+SlotOccupancy\b') -and
    ($resolverText -match 'SlotOccupancy\s*=\s*config\.SlotOccupancy')
) 'Loadout contract and resolver must carry the strong occupancy type.'

Assert-True (
    ($validatorText -match 'ChipSlotOccupancy\.Unspecified') -and
    ($validatorText -match 'SlotOccupancyMissingOrInvalid') -and
    ($validatorText -match 'SlotOccupancyRegionConflict') -and
    ($validatorText -match 'ChipSlotRegion\.Special') -and
    ($validatorText -match 'ChipSlotOccupancy\.PairedHands')
) 'Definition validation must reject missing occupancy and Special plus PairedHands.'

Assert-True (
    ($runtimePayloadText -match '\bChipSlotOccupancy\?\s+LoadoutSlotOccupancy\b') -and
    ($collectorText -match '\bLoadoutSlotOccupancy\b')
) 'Expression runtime payload must carry the neutral occupancy snapshot.'

Assert-True (
    ($loadoutServiceText -match 'SlotOccupancy\s*==\s*ChipSlotOccupancy\.PairedHands') -and
    ($switchServiceText -match 'TryResolvePairedOccupancyLoad') -and
    ($switchServiceText -match 'IsSlotOccupancyAllowed') -and
    ($transitionServiceText -match 'IsPairedOccupancySlot')
) 'Loadout and switching must use neutral paired-occupancy names.'

$productionTexts = @(
    $configText,
    $contractText,
    $resolverText,
    $validatorText,
    $runtimePayloadText,
    $collectorText,
    $loadoutServiceText,
    $switchServiceText,
    $transitionServiceText
) -join "`n"

Assert-True (
    $productionTexts -notmatch
        '\bIsDualWieldBinding\b|\bIsDualWieldBindingSlot\b|\bTryResolveBindingLoad\b'
) 'Current production sources must not retain the old dual-wield occupancy names.'

Write-Output 'ChipSlotOccupancySourceBoundarySmokeTests PASS'
