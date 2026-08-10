$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$phasePath = Join-Path $coreRoot 'Trigger\Switching\Model\SwitchPhase.cs'
$contextPath = Join-Path $coreRoot 'Trigger\Switching\Model\SwitchContext.cs'
$transitionPath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchTransitionService.cs'
$contextsPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Contexts.cs'
$readsPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Reads.cs'

$phaseText = Get-Content -LiteralPath $phasePath -Raw -Encoding utf8
$contextText = Get-Content -LiteralPath $contextPath -Raw -Encoding utf8
$transitionText = Get-Content -LiteralPath $transitionPath -Raw -Encoding utf8
$contextsText = Get-Content -LiteralPath $contextsPath -Raw -Encoding utf8
$readsText = Get-Content -LiteralPath $readsPath -Raw -Encoding utf8

Assert-True ($phaseText -match '\bWaitingForConflicts\b') `
    'SwitchPhase must publish the waiting-for-conflicts phase.'
Assert-True (
    ($contextText -match 'string\s+targetChipThingId') -and
    ($contextText -match 'Scribe_Values\.Look\(ref targetChipThingId,\s*"targetChipThingId"')
) 'SwitchContext must save the target ThingID.'
Assert-True (
    ($contextsText -match 'targetChipThingId\s*=\s*context\.targetChipThingId') -and
    ($readsText -match 'context\.targetChipThingId')
) 'Projection cloning and runtime stamps must retain target identity.'
Assert-True (
    $transitionText -match
        'context\.phase\s*==\s*SwitchPhase\.WaitingForConflicts'
) 'Waiting must remain active without relying on phaseEndTick.'
Assert-True (
    ($transitionText -match 'IsPendingTargetValid') -and
    ($transitionText -match 'resolveActivationBlockers') -and
    ($transitionText -notmatch 'blockingSlotThingIds|savedBlockers')
) 'Post-load waiting must validate the target and rescan live blockers.'
Assert-True (
    ($transitionText -match 'ClearInvalidPendingTarget') -and
    ($transitionText -notmatch 'RestorePrevious|ReactivatePrevious')
) 'Invalid pending targets must be cleared without restoring old chips.'
Assert-True (
    ($contextsText -match
        'TriggerSlotState\s+bindingPartner\s*=\s*GetBindingPartnerSlot\(targetSlot\)') -and
    ($contextsText -match 'IsPendingTargetSlotValid\(bindingPartner\)')
) 'A paired pending target must validate its binding partner before synchronized activation.'

Write-Output 'TriggerActivationExclusionWaitingPersistenceSmokeTests PASS'
