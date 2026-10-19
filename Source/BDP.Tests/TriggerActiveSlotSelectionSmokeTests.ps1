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
$readsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Reads.cs'
$contextsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Contexts.cs'
$slotPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\TriggerSlotState.cs'
$requirementsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.ActivationRequirements.cs'
$monitorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerActivationRequirementMonitor.cs'
$interpreterPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Interaction\TriggerInteractionInterpreter.cs'
$exitPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'

$readsText = Get-Content -LiteralPath $readsPath -Raw -Encoding utf8
$contextsText = Get-Content -LiteralPath $contextsPath -Raw -Encoding utf8
$slotText = Get-Content -LiteralPath $slotPath -Raw -Encoding utf8
$requirementsText = Get-Content -LiteralPath $requirementsPath -Raw -Encoding utf8
$monitorText = Get-Content -LiteralPath $monitorPath -Raw -Encoding utf8
$interpreterText = Get-Content -LiteralPath $interpreterPath -Raw -Encoding utf8
$exitText = Get-Content -LiteralPath $exitPath -Raw -Encoding utf8

Assert-True ($slotText -notmatch 'AutomaticSwitchContext|automaticSwitchContext') 'Slot state must not retain passive automatic-transition context.'
Assert-True ($slotText -notmatch 'IsPassiveActive|passiveActive') 'Slot state must not add a second passive activation truth.'
Assert-True ($readsText -notmatch 'automaticSlot|TriggerActivationPolicyRegistry\.IsAutomaticallyActivated') 'Single-slot reads must not distinguish passive chips as automatic chips.'
Assert-True ($contextsText -notmatch 'automaticSlot|TriggerActivationPolicyRegistry\.IsAutomaticallyActivated') 'Raw control-slot reads must use the ordinary single-slot semantics.'
Assert-True ($requirementsText -match 'rootSlot\.Index') 'Requirement failure must close the exact failed slot.'
Assert-True ($monitorText -match 'IsDeactivatingSlotContext') 'Requirement monitor must compare the exact deactivating slot.'
Assert-True ($interpreterText -notmatch 'isAutomaticallyActivated') 'Interaction interpretation must not carry automatic-chip-only behavior.'
Assert-True ((Get-Content -LiteralPath (Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchService.cs') -Raw -Encoding utf8) -notmatch 'RequestDeactivate\(TriggerSwitchContext context, TriggerSide side\)[\s\S]*IsAutomaticallyActivated') 'Side-level deactivation must not special-case passive chips as automatic.'
Assert-True ($exitText -match 'RequestDeactivate\(slot\.Side, slot\.Index\)') 'Combat-body exit must close every automatic slot by exact coordinate.'

Write-Output 'TriggerActiveSlotSelectionSmokeTests PASS'
