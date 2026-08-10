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
$transitionPath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchTransitionService.cs'
$integrityPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Integrity.cs'
$trionBindingPath = Join-Path $coreRoot 'Trigger\Runtime\TriggerTrionBindingService.cs'
$collectorPath = Join-Path $coreRoot 'Expressions\Pipeline\ExpressionSourceCollector.cs'

$transitionText = Get-Content -LiteralPath $transitionPath -Raw -Encoding utf8
$integrityText = Get-Content -LiteralPath $integrityPath -Raw -Encoding utf8
$trionBindingText = Get-Content -LiteralPath $trionBindingPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8

$activateStart = $transitionText.IndexOf('private static bool ActivateBoundSlotImmediate')
$activateEnd = $transitionText.IndexOf('private static bool ActivateSlot', $activateStart + 1)
Assert-True ($activateStart -ge 0 -and $activateEnd -gt $activateStart) `
    'The paired activation method must remain inspectable.'
$activateBody = $transitionText.Substring($activateStart, $activateEnd - $activateStart)

Assert-True (
    ($activateBody -match 'ActivateSlot\(rootSlot\)') -and
    ($activateBody -match 'ActivateSlot\(mirrorSlot\)')
) 'Paired activation must still activate both the root and mirror slots.'

$activationNotificationCount = [regex]::Matches(
    $activateBody,
    'notifySlotActivationCommitted\?\.Invoke').Count
Assert-True ($activationNotificationCount -eq 1) `
    'One paired activation must publish exactly one formal activation notification.'
Assert-True (
    $activateBody -match
        'notifySlotActivationCommitted\?\.Invoke\(rootSlot\.Side,\s*rootSlot\.Index,\s*rootSlot\.LoadedChip\)'
) 'The root slot must represent the paired chip for activation settlement.'

$deactivateStart = $transitionText.IndexOf('public static void DeactivateBoundSlotImmediate')
$deactivateEnd = $transitionText.IndexOf('public static bool SharesActivationControlScope', $deactivateStart + 1)
Assert-True ($deactivateStart -ge 0 -and $deactivateEnd -gt $deactivateStart) `
    'The paired deactivation method must remain inspectable.'
$deactivateBody = $transitionText.Substring($deactivateStart, $deactivateEnd - $deactivateStart)

Assert-True (
    ($deactivateBody -match 'DeactivateSlot\(rootSlot\)') -and
    ($deactivateBody -match 'DeactivateSlot\(mirrorSlot\)')
) 'Paired deactivation must still deactivate both physical slots.'

$deactivationNotificationCount = [regex]::Matches(
    $deactivateBody,
    'notifySlotDeactivated\?\.Invoke').Count
Assert-True ($deactivationNotificationCount -eq 1) `
    'One paired deactivation must publish exactly one formal deactivation notification.'
Assert-True (
    $deactivateBody -match
        'notifySlotDeactivated\?\.Invoke\(rootSlot\.Side,\s*rootSlot\.Index,\s*rootSlot\.LoadedChip\)'
) 'The root slot must represent the paired chip for deactivation settlement.'

Assert-True (
    ($integrityText -match 'TryCommitSlotActivationTrion\(chip\)') -and
    ($integrityText -match 'DeactivateBoundSlotImmediate')
) 'Activation payment failure must still deactivate the whole paired structure.'
Assert-True (
    ($trionBindingText -match 'BindingRootSide') -and
    ($trionBindingText -match 'BindingRootIndex')
) 'Reserved Trion must continue deduplicating paired slots by binding root.'
Assert-True ($collectorText -match 'slot\.IsBindingMirror') `
    'Expression collection must continue skipping the mirror slot.'

Write-Output 'PairedSlotOccupancySettlementSmokeTests PASS'
