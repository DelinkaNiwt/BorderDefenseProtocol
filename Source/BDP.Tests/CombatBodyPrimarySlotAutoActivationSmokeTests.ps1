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
$activationTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs'

Assert-True (
    Test-Path -LiteralPath $activationTransactionPath
) 'CombatBodyActivationTransaction.cs must exist.'

$text = Get-Content -LiteralPath $activationTransactionPath -Raw -Encoding utf8

Assert-True (
    $text -match 'rawCombatBodyService\.TryEnterActive\(allocateAmount\)[\s\S]*TryAutoActivatePrimarySlots\(ownerPawn\)[\s\S]*trionBinding\.BindActiveRuntime\(\)'
) 'Primary slot automation must run after successful combat-body activation and before runtime binding completes.'

Assert-True (
    $text -match 'private void TryAutoActivatePrimarySlots\(Pawn ownerPawn\)'
) 'Primary slot automation must be isolated behind a dedicated helper.'

Assert-True (
    ($text -match 'TriggerSurfaceAccess\.ResolveInteractionReader\(ownerPawn\)') -and
    ($text -match 'TriggerSurfaceAccess\.ResolveLoadoutCommands\(ownerPawn\)')
) 'Primary slot automation must use the formal Trigger interaction reader and command surface.'

Assert-True (
    ($text -match 'TryAutoActivatePrimarySlot\(interactionReader, loadoutCommands, TriggerSide\.Main\)') -and
    ($text -match 'TryAutoActivatePrimarySlot\(interactionReader, loadoutCommands, TriggerSide\.Sub\)')
) 'Primary slot automation must inspect Main[0] and Sub[0].'

Assert-True (
    $text -match 'GetSlotInteraction\(side, 0\)'
) 'Primary slot automation must inspect slot index zero on the requested side.'

Assert-True (
    ($text -match 'TriggerInteractionAvailability\.Available') -and
    ($text -match 'TriggerInteractionOperationKind\.Activate') -and
    ($text -match 'TriggerInteractionOperationKind\.SwitchTo')
) 'Primary slot automation must require an available Activate or SwitchTo interaction.'

Assert-True (
    $text -match 'RequestActivate\(interaction\.ControlSide, interaction\.ControlSlotIndex\)'
) 'Primary slot automation must submit the formal control slot, including mirror resolution.'

Write-Output 'CombatBodyPrimarySlotAutoActivationSmokeTests PASS'
