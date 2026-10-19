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
$triggerServicePath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchService.cs'
$transitionServicePath = Join-Path $coreRoot 'Trigger\Switching\Flow\TriggerSwitchTransitionService.cs'
$interactionInterpreterPath = Join-Path $coreRoot 'Trigger\Interaction\TriggerInteractionInterpreter.cs'
$compTriggerBodyPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.cs'

$triggerServiceText = Get-Content -LiteralPath $triggerServicePath -Raw -Encoding utf8
$transitionServiceText = Get-Content -LiteralPath $transitionServicePath -Raw -Encoding utf8
$interactionInterpreterText = Get-Content -LiteralPath $interactionInterpreterPath -Raw -Encoding utf8
$compTriggerBodyText = Get-Content -LiteralPath $compTriggerBodyPath -Raw -Encoding utf8

Assert-True (
    $triggerServiceText -match 'nextSlot\.Side\s*==\s*TriggerSide\.Special\s*\|\|\s*TriggerChipActivationControlRegistry\.HasIndependentActivationControl\(nextSlot\.LoadedChip\)'
) 'Special activation must use the slot switch context regardless of chip category.'

Assert-True (
    $triggerServiceText -match 'targetSlot\.Side\s*==\s*TriggerSide\.Special[\s\S]{0,500}RequestSlotDeactivate\('
) 'Special exact deactivation must use the slot switch context regardless of chip category.'

Assert-True (
    $triggerServiceText -match 'if\s*\(side\s*==\s*TriggerSide\.Special\)[\s\S]{0,260}return false;'
) 'Special side-level deactivation must not write to the shared side switch context.'

Assert-True (
    $interactionInterpreterText -match 'activeRaw != null\s*\r?\n\s*&&\s*\(side\s*==\s*TriggerSide\.Special\s*\|\|'
) 'Special side interaction must not expose a shared side-level deactivation action.'

Assert-True (
    $compTriggerBodyText -match 'bool independentControl = slot != null[\s\S]{0,220}side\s*==\s*TriggerSide\.Special'
) 'Special diagnostics must report slot-scoped control.'

Assert-True (
    $transitionServiceText -match 'left\.Side\s*==\s*TriggerSide\.Special\s*&&\s*right\.Side\s*==\s*TriggerSide\.Special[\s\S]{0,120}return false;'
) 'Special slots must remain outside the shared activation-control scope.'

Write-Output 'SpecialSlotParallelActivationRoutingSmokeTests PASS'
