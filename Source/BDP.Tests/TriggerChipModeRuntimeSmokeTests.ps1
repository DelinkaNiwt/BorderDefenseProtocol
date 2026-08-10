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
$slotPath = Join-Path $coreRoot 'Trigger\State\TriggerSlotState.cs'
$modeServicePath = Join-Path $coreRoot 'Trigger\Modes\TriggerChipModeService.cs'
$readerPath = Join-Path $coreRoot 'Trigger\Access\Contracts\ITriggerLoadoutReader.cs'
$commandsPath = Join-Path $coreRoot 'Trigger\Access\Contracts\ITriggerLoadoutCommands.cs'
$readsPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Reads.cs'
$bodyPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.cs'
$integrityPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Integrity.cs'
$dirtyReasonPath = Join-Path $coreRoot 'Trigger\Runtime\ProjectionDirtyReason.cs'

Assert-True (Test-Path -LiteralPath $modeServicePath) `
    'Core must provide one neutral TriggerChipModeService for chip-mode runtime truth.'

$slotText = Get-Content -LiteralPath $slotPath -Raw -Encoding utf8
$modeServiceText = Get-Content -LiteralPath $modeServicePath -Raw -Encoding utf8
$readerText = Get-Content -LiteralPath $readerPath -Raw -Encoding utf8
$commandsText = Get-Content -LiteralPath $commandsPath -Raw -Encoding utf8
$readsText = Get-Content -LiteralPath $readsPath -Raw -Encoding utf8
$bodyText = Get-Content -LiteralPath $bodyPath -Raw -Encoding utf8
$integrityText = Get-Content -LiteralPath $integrityPath -Raw -Encoding utf8
$dirtyReasonText = Get-Content -LiteralPath $dirtyReasonPath -Raw -Encoding utf8

Assert-True (
    ($slotText -match 'private\s+string\s+currentModeKey') -and
    ($slotText -match 'internal\s+string\s+CurrentModeKey') -and
    ($slotText -match 'Scribe_Values\.Look\(ref\s+currentModeKey,\s*"currentModeKey"')
) 'TriggerSlotState must own and save exactly one currentModeKey truth.'

Assert-True (
    ($slotText -match 'SetActive\(bool active\)[\s\S]*?if\s*\(!isActive\)[\s\S]*?currentModeKey\s*=\s*null') -and
    ($slotText -match 'SetLoadedChip\(Thing chip\)[\s\S]*?currentModeKey\s*=\s*null') -and
    ($slotText -match 'SetDisabled\(bool disabled,[\s\S]*?currentModeKey\s*=\s*null')
) 'Inactive, unloaded, replaced, or disabled slots must not retain a current mode.'

Assert-True (
    ($modeServiceText -match 'TryInitializeActiveRootMode') -and
    ($modeServiceText -match 'TrySwitchActiveRootMode') -and
    ($modeServiceText -match 'TryCycleActiveRootMode') -and
    ($modeServiceText -match 'NormalizeRestoredActiveRootMode') -and
    ($modeServiceText -match 'IsBindingMirror')
) 'The mode service must cover activation, direct switching, ordered cycling, load repair, and mirror rejection.'

Assert-True (
    ($modeServiceText -match 'StringComparison\.OrdinalIgnoreCase') -and
    ($modeServiceText -match 'previousModeKey') -and
    ($modeServiceText -match 'SetCurrentModeKey\(previousModeKey\)') -and
    ($modeServiceText -match 'publish')
) 'Mode switching must be case-insensitive and atomically restore the old truth when publication fails.'

Assert-True (
    ($readerText -match 'IReadOnlyList<ChipModeOptionSnapshot>\s+GetChipModeOptions\(Thing chip\)') -and
    ($readerText -match 'string\s+GetChipModeKey\(Thing chip\)')
) 'Core formal readers must expose pure current-mode and ordered-option reads.'

Assert-True (
    ($commandsText -match 'bool\s+RequestSwitchChipMode\(Thing chip,\s*string targetModeKey\)') -and
    ($commandsText -match 'bool\s+RequestCycleChipMode\(Thing chip\)')
) 'Core formal commands must expose direct and ordered mode switching.'

Assert-True (
    ($readsText -match 'NormalizeDirectControlSlot') -and
    ($readsText -match 'GetChipModeOptions') -and
    ($readsText -match 'CurrentModeKey')
) 'CompTriggerBody reads must normalize paired chips to the root slot and return root mode truth.'

Assert-True (
    $integrityText -match 'NotifySlotActivationCommitted[\s\S]*?TryCommitSlotActivationTrion\(chip\)[\s\S]*?TryInitializeActiveRootMode\(rootSlot,\s*chip\)[\s\S]*?PublishCombatProjection\(ProjectionDirtyReason\.SlotActivationCommitted\)'
) 'Activation must pay its one-time cost before establishing the default mode and publishing it.'

Assert-True (
    ($integrityText -match 'NormalizeRestoredChipModes') -and
    ($integrityText -match 'NormalizeRestoredActiveRootMode') -and
    ($integrityText -match 'trigger\.chip_mode_post_load_fallback')
) 'Post-load recovery must retain valid modes and diagnose invalid-mode fallback once.'

Assert-True (
    ($bodyText -match 'RequestSwitchChipMode\(Thing chip,\s*string targetModeKey\)[\s\S]*?PrepareCommandState\(\)[\s\S]*?TrySwitchActiveRootMode') -and
    ($bodyText -match 'RequestCycleChipMode\(Thing chip\)[\s\S]*?PrepareCommandState\(\)[\s\S]*?TryCycleActiveRootMode') -and
    ($bodyText -match 'SetCurrentModeKey\(previousModeKey\)|MarkCombatProjectionDirty\(ProjectionDirtyReason\.ChipModeChanged\)') -and
    ($dirtyReasonText -match '\bChipModeChanged\b')
) 'Owner commands must settle prior transitions, publish through the atomic mode service, and retain a dedicated dirty reason.'

Write-Output 'TriggerChipModeRuntimeSmokeTests PASS'
