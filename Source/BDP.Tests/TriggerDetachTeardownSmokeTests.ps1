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

$triggerBodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$triggerLifecyclePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$triggerDetachPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.DetachTeardown.cs'
$detachTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerDetachTeardownTransaction.cs'

$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8
$triggerLifecycleText = Get-Content -LiteralPath $triggerLifecyclePath -Raw -Encoding utf8
$triggerDetachText = ''
if (Test-Path -LiteralPath $triggerDetachPath) {
    $triggerDetachText = Get-Content -LiteralPath $triggerDetachPath -Raw -Encoding utf8
}
$detachTransactionText = ''
if (Test-Path -LiteralPath $detachTransactionPath) {
    $detachTransactionText = Get-Content -LiteralPath $detachTransactionPath -Raw -Encoding utf8
}

Assert-True (
    Test-Path -LiteralPath $triggerDetachPath
) 'Detach teardown requires a dedicated CompTriggerBody.DetachTeardown.cs file.'

Assert-True (
    $triggerDetachText -match 'private void ForceTeardownOnDetach\(Pawn pawn\)'
) 'CompTriggerBody must expose a dedicated ForceTeardownOnDetach(Pawn pawn) entry.'

Assert-True (
    (Test-Path -LiteralPath $detachTransactionPath) -and
    ($triggerDetachText -match 'runtimeServices\.TriggerDetachTeardownTransaction\.Execute\(')
) 'ForceTeardownOnDetach must delegate to TriggerDetachTeardownTransaction.'

Assert-True (
    $detachTransactionText -match 'internal sealed class TriggerDetachTeardownTransaction'
) 'Task 8 must introduce TriggerDetachTeardownTransaction.'

Assert-True (
    $detachTransactionText -match 'internal void Execute\('
) 'TriggerDetachTeardownTransaction must expose an Execute entry.'

Assert-True (
    $detachTransactionText -match 'setSwitchContext\?\.Invoke\(TriggerSide\.Main, null\);' -and
    $detachTransactionText -match 'setSwitchContext\?\.Invoke\(TriggerSide\.Sub, null\);' -and
    $detachTransactionText -match 'setSwitchContext\?\.Invoke\(TriggerSide\.Special, null\);'
) 'Detach teardown transaction must clear all switch contexts immediately.'

Assert-True (
    $detachTransactionText -match 'foreach \(TriggerSlotState slot in slots\)[\s\S]*slot\.SetActive\(false\);'
) 'Detach teardown transaction must immediately clear every active slot truth.'

Assert-True (
    $detachTransactionText -match 'triggerTrionBindingService\.UnregisterCombatBodyMaintenanceDrain\(pawn\);'
) 'Detach teardown transaction must unregister the combat body drain from the detached pawn.'

Assert-True (
    $detachTransactionText -notmatch 'UnregisterSlotDrain'
) 'Detach teardown transaction must not own expression sustain-drain cleanup.'

Assert-True (
    $detachTransactionText -match 'runtimeCoordinator\?\.ClearPublishedProjection\(pawn\);'
) 'Detach teardown transaction must clear published trigger projections.'

Assert-True (
    $detachTransactionText -notmatch 'AttackExecutionPostLoadRecovery\.InterruptInvalidAttackSession\(pawn\);'
) 'Detach teardown transaction must stop duplicating attack-session invalidation outside the unified publish boundary.'

Assert-True (
    $triggerBodyText -match 'public override void Notify_Unequipped\(Pawn pawn\)[\s\S]*ForceTeardownOnDetach\(pawn\);'
) 'Notify_Unequipped must route detach cleanup through ForceTeardownOnDetach(pawn).'

Write-Output 'TriggerDetachTeardown PASS'
