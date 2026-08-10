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

$combatBodySessionServicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$battleActivationTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$battleExitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$combatBodyServicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Flow\CombatBodyCoordinator.cs'
$chipTrionConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Config\ChipTrionConfig.cs'
$chipTrionContractPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Contract\ChipTrionContract.cs'
$triggerServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerSwitchService.cs'
$triggerBodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$triggerLifecyclePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$triggerIntegrityPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Integrity.cs'
$triggerInteractionReasonPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Interaction\TriggerInteractionReason.cs'
$triggerInteractionInterpreterPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Interaction\TriggerInteractionInterpreter.cs'
$projectionDirtyReasonPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\ProjectionDirtyReason.cs'
$triggerRuntimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$triggerRuntimeServicesPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeServices.cs'
$triggerTrionBindingServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerTrionBindingService.cs'
$triggerDetachTeardownTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerDetachTeardownTransaction.cs'
$trionDrainKeyPath = Join-Path $repoRoot 'Source\BDP\Core\Trion\TrionDrainKey.cs'
$triggerDrainKeyFactoryPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerDrainKeyFactory.cs'
$combatBodyTriggerGizmoProviderPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\External\CombatBodyTriggerGizmoProvider.cs'
$combatBodyTriggerGizmoBootstrapPath = Join-Path $repoRoot 'Source\BDP\Core\Bootstrap\CombatBodyTriggerGizmoBootstrap.cs'
$triggerDisableReasonPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Model\TriggerDisableReason.cs'

$combatBodySessionServiceText = Get-Content -LiteralPath $combatBodySessionServicePath -Raw -Encoding utf8
$battleActivationTransactionText = Get-Content -LiteralPath $battleActivationTransactionPath -Raw -Encoding utf8
$battleExitTransactionText = Get-Content -LiteralPath $battleExitTransactionPath -Raw -Encoding utf8
$combatBodyServiceText = Get-Content -LiteralPath $combatBodyServicePath -Raw -Encoding utf8
$chipTrionConfigText = Get-Content -LiteralPath $chipTrionConfigPath -Raw -Encoding utf8
$chipTrionContractText = Get-Content -LiteralPath $chipTrionContractPath -Raw -Encoding utf8
$triggerServiceText = Get-Content -LiteralPath $triggerServicePath -Raw -Encoding utf8
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8
$triggerLifecycleText = Get-Content -LiteralPath $triggerLifecyclePath -Raw -Encoding utf8
$triggerIntegrityText = Get-Content -LiteralPath $triggerIntegrityPath -Raw -Encoding utf8
$triggerInteractionReasonText = Get-Content -LiteralPath $triggerInteractionReasonPath -Raw -Encoding utf8
$triggerInteractionInterpreterText = Get-Content -LiteralPath $triggerInteractionInterpreterPath -Raw -Encoding utf8
$projectionDirtyReasonText = Get-Content -LiteralPath $projectionDirtyReasonPath -Raw -Encoding utf8
$triggerRuntimeCoordinatorText = Get-Content -LiteralPath $triggerRuntimeCoordinatorPath -Raw -Encoding utf8
$triggerRuntimeServicesText = Get-Content -LiteralPath $triggerRuntimeServicesPath -Raw -Encoding utf8
$triggerTrionBindingServiceText = Get-Content -LiteralPath $triggerTrionBindingServicePath -Raw -Encoding utf8
$triggerDetachTeardownTransactionText = Get-Content -LiteralPath $triggerDetachTeardownTransactionPath -Raw -Encoding utf8
$combatBodyTriggerGizmoProviderText = if (Test-Path -LiteralPath $combatBodyTriggerGizmoProviderPath) { Get-Content -LiteralPath $combatBodyTriggerGizmoProviderPath -Raw -Encoding utf8 } else { '' }
$combatBodyTriggerGizmoBootstrapText = if (Test-Path -LiteralPath $combatBodyTriggerGizmoBootstrapPath) { Get-Content -LiteralPath $combatBodyTriggerGizmoBootstrapPath -Raw -Encoding utf8 } else { '' }
$triggerDisableReasonText = Get-Content -LiteralPath $triggerDisableReasonPath -Raw -Encoding utf8

Assert-True (
    $combatBodyServiceText -match 'internal sealed class CombatBodyService : ICombatBodyReader, ICombatBodyEvents'
) 'CombatBodyService must shrink back to the raw phase service surface.'

Assert-True (
    $combatBodyServiceText -notmatch 'TrionSurfaceAccess\.ResolveCommands'
) 'CombatBodyService must stop resolving Trion commands directly.'

Assert-True (
    $combatBodyServiceText -notmatch 'TriggerSurfaceAccess\.ResolveLoadoutCommands'
) 'CombatBodyService must stop resolving Trigger loadout commands directly.'

Assert-True (
    $combatBodyServiceText -match 'internal bool TryEnterActive\(float allocatedTrion\)'
) 'CombatBodyService must expose internal TryEnterActive(float allocatedTrion).'

Assert-True (
    $combatBodyServiceText -match 'internal void EnterCooldown\(int cooldownTicks, string reason\)'
) 'CombatBodyService must expose internal EnterCooldown(int cooldownTicks, string reason).'

Assert-True (
    $combatBodyServiceText -match 'internal void EnterCollapsing\(string reason\)'
) 'CombatBodyService must expose internal EnterCollapsing(string reason).'

Assert-True (
    ($combatBodySessionServiceText -match 'public bool TryActivate\(\)[\s\S]*return activationTransaction\.TryActivate\(OwnerPawn\);') -and
    ($battleActivationTransactionText -match 'public bool TryActivate\(Pawn ownerPawn\)[\s\S]*rawCombatBodyService\.CanActivate\(\)[\s\S]*policy\.TryResolvePrimaryTrigger\(ownerPawn, out CompTriggerBody trigger\)[\s\S]*CalculateCombatBodyAllocateAmount\(trigger\)[\s\S]*trionCommands\.CanAfford\(allocateAmount\)[\s\S]*trionCommands\.Allocate\(allocateAmount\)[\s\S]*rawCombatBodyService\.TryEnterActive\(allocateAmount\)[\s\S]*TryAutoActivateSpecialSlots\(trigger\)[\s\S]*trionBinding\.BindActiveRuntime\(\)[\s\S]*trionCommands\.SetFrozen\(true\)[\s\S]*notifyCombatBodySessionStateChanged\(\)')
) 'CombatBodySessionService.TryActivate() must delegate to CombatBodyActivationTransaction, and the transaction must own the activation chain order.'

Assert-True (
    ($combatBodySessionServiceText -match 'private void ExecuteExit\(CombatBodySessionExitMode exitMode\)[\s\S]*exitTransaction\.Execute\(OwnerPawn, exitMode\);') -and
    ($battleExitTransactionText -match 'public void Execute\(Pawn ownerPawn, CombatBodySessionExitMode exitMode\)[\s\S]*TriggerSurfaceAccess\.ResolveLoadoutCommands\(ownerPawn\)[\s\S]*DeactivateAllSlots\(triggerLoadoutCommands\)[\s\S]*trionBinding\.ClearActiveRuntime\(\)[\s\S]*trionCommands\.Release\(rawCombatBodyService\.AllocatedTrion\)[\s\S]*trionCommands\.SetFrozen\(false\)[\s\S]*rawCombatBodyService\.EnterCooldown\(ResolveCooldownTicks\(exitMode\), ResolveExitReason\(exitMode\)\)[\s\S]*notifyCombatBodySessionStateChanged\(\)')
) 'CombatBodySessionService release/collapse exit chain must delegate to CombatBodyExitTransaction, and the transaction must own the ordered cleanup.'

Assert-True (
    (Test-Path -LiteralPath $trionDrainKeyPath) -and
    (-not (Test-Path -LiteralPath $triggerDrainKeyFactoryPath))
) 'The central TrionDrainKey must remain, while the obsolete chip drain key factory must be removed.'

Assert-True (
    (Test-Path -LiteralPath $triggerRuntimeServicesPath) -and
    (Test-Path -LiteralPath $triggerTrionBindingServicePath) -and
    (Test-Path -LiteralPath $triggerDetachTeardownTransactionPath)
) 'Task 8 requires TriggerRuntimeServices, TriggerTrionBindingService, and TriggerDetachTeardownTransaction.'

Assert-True (
    $chipTrionConfigText -notmatch 'ActiveDrainPerSecond'
) 'ChipTrionConfig must not expose ActiveDrainPerSecond.'

Assert-True (
    $chipTrionContractText -notmatch 'ActiveDrainPerSecond'
) 'ChipTrionContract must not expose ActiveDrainPerSecond.'

Assert-True (
    $triggerServiceText -match 'public ChipTrionContract GetChipTrionContract\(Thing chip\)'
) 'TriggerService must expose GetChipTrionContract(Thing chip).'

Assert-True (
    $triggerBodyText -match 'private float CalculateReservedTrionCost\(\)'
) 'CompTriggerBody must expose CalculateReservedTrionCost().'

Assert-True (
    $triggerBodyText -match 'private void SyncReservedTrion\(\)'
) 'CompTriggerBody must expose SyncReservedTrion().'

Assert-True (
    ($triggerBodyText -match 'CalculateReservedTrionCost\(\)[\s\S]*runtimeServices\.TriggerTrionBindingService\.CalculateReservedTrionCost') -and
    ($triggerBodyText -match 'SyncReservedTrion\(Pawn pawn, float reservedTrion\)[\s\S]*runtimeServices\.TriggerTrionBindingService\.SyncReservedTrion')
) 'CompTriggerBody reserved Trion helpers must delegate to TriggerTrionBindingService.'

Assert-True (
    ($triggerRuntimeServicesText -match 'TriggerTrionBindingService TriggerTrionBindingService') -and
    ($triggerRuntimeServicesText -match 'TriggerDetachTeardownTransaction TriggerDetachTeardownTransaction')
) 'TriggerRuntimeServices must expose the Trigger binding and detach collaborators.'

Assert-True (
    $triggerBodyText -match 'public override void Notify_Equipped\(Pawn pawn\)[\s\S]*SyncReservedTrion\(\);'
) 'Notify_Equipped must synchronize reserved Trion.'

Assert-True (
    $triggerLifecycleText -match 'public override void PostSpawnSetup\(bool respawningAfterLoad\)[\s\S]*OwnerPawn != null[\s\S]*SyncReservedTrion\(\);'
) 'Trigger post-spawn restore path must synchronize reserved Trion when an owner Pawn is already present.'

Assert-True (
    $triggerLifecycleText -match 'TryFinalizePostLoadProjectionRefresh\(\)[\s\S]*OwnerPawn == null[\s\S]*return false;[\s\S]*SyncReservedTrion\(\);'
) 'Trigger post-load finalize path must resynchronize reserved Trion after slot truth is restored.'

Assert-True (
    $triggerBodyText -match 'internal bool RequestActivate\(TriggerSide side, int slotIndex\)[\s\S]*CombatBodySurfaceAccess\.ResolveReader\(OwnerPawn\)\?\.Phase != CombatBodyPhase\.Active'
) 'Trigger activation requests must be gated by CombatBodyPhase.Active.'

Assert-True (
    $triggerInteractionReasonText -match 'BattleModeUnavailable'
) 'TriggerInteractionReason must expose BattleModeUnavailable.'

Assert-True (
    $triggerInteractionInterpreterText -match 'TriggerInteractionReason\.BattleModeUnavailable'
) 'TriggerInteractionInterpreter must surface BattleModeUnavailable when battle mode is not active.'

Assert-True (
    ($triggerIntegrityText -match 'TryCommitSlotActivationTrion\(Thing chip\)[\s\S]*runtimeServices\.TriggerTrionBindingService\.TryCommitSlotActivation') -and
    ($triggerTrionBindingServiceText -match 'TryCommitSlotActivation\([\s\S]*TryConsume\(') -and
    ($triggerTrionBindingServiceText -notmatch '\.RegisterDrain\(')
) 'Slot activation commit must delegate only the one-time activation cost to TriggerTrionBindingService.'

Assert-True (
    ($triggerIntegrityText -notmatch 'UnregisterSlotDrain') -and
    ($triggerTrionBindingServiceText -notmatch 'UnregisterSlotDrain')
) 'Slot deactivation must not own expression sustain-drain cleanup.'

Assert-True (
    ($triggerIntegrityText -notmatch 'RebuildActiveChipDrainRegistrations') -and
    ($triggerTrionBindingServiceText -notmatch 'RebuildActiveChipDrainRegistrations')
) 'Post-load must rebuild expression drains through publication instead of chip slots.'

Assert-True (
    $projectionDirtyReasonText -match 'CombatBodySessionStateChanged'
) 'ProjectionDirtyReason must expose CombatBodySessionStateChanged.'

Assert-True (
    $combatBodySessionServiceText -match 'NotifyCombatBodySessionStateChanged\(\)'
) 'CombatBodySessionService must expose NotifyCombatBodySessionStateChanged().'

Assert-True (
    $triggerRuntimeCoordinatorText -match 'RebuildAndPublish\(\)[\s\S]*ShouldPublishCombatProjection\(ownerPawn, owner\)[\s\S]*TriggerCombatProjectionState\.CreateEmpty'
) 'TriggerRuntimeCoordinator must clear published projection when battle session policy says not to publish.'

Assert-True (
    $triggerBodyText -match 'public override void Notify_Unequipped\(Pawn pawn\)[\s\S]*CombatBodySurfaceAccess\.ResolveReader\(pawn\)\?\.Phase == CombatBodyPhase\.Active[\s\S]*CombatBodySurfaceAccess\.ResolveCommands\(pawn\)\?\.RequestRelease\(\);[\s\S]*SyncReservedTrion\(pawn, 0f\);'
) 'Notify_Unequipped must release combat body before clearing reserved Trion.'

Assert-True (
    Test-Path -LiteralPath $combatBodyTriggerGizmoProviderPath
) 'CombatBodyTriggerGizmoProvider must exist.'

Assert-True (
    Test-Path -LiteralPath $combatBodyTriggerGizmoBootstrapPath
) 'CombatBodyTriggerGizmoBootstrap must exist.'

Assert-True (
    ($combatBodyTriggerGizmoProviderText -match 'ITriggerExternalGizmoProvider') -and
    ($combatBodyTriggerGizmoProviderText -match 'CombatBodySurfaceAccess\.ResolveReader\(pawn\)') -and
    ($combatBodyTriggerGizmoProviderText -match 'CombatBodySurfaceAccess\.ResolveCommands\(pawn\)') -and
    ($combatBodyTriggerGizmoProviderText -match 'TrionGlandEligibility\.HasActiveTrionGland\(pawn\)')
) 'CombatBodyTriggerGizmoProvider must build gene-gated gizmos through CombatBody formal surfaces.'

Assert-True (
    ($combatBodyTriggerGizmoBootstrapText -match 'TriggerExternalGizmoRegistry\.Register\(new CombatBodyTriggerGizmoProvider\(\)\);')
) 'CombatBodyTriggerGizmoBootstrap must register CombatBodyTriggerGizmoProvider into TriggerExternalGizmoRegistry.'

Assert-True (
    ($combatBodyTriggerGizmoProviderText -match 'defaultLabel = "开启战斗体"') -and
    ($combatBodyTriggerGizmoProviderText -match 'defaultLabel = "解除战斗体"') -and
    ($combatBodyTriggerGizmoProviderText -match 'action = commands\.RequestRelease') -and
    ($combatBodyTriggerGizmoProviderText -notmatch 'RequestDeactivate\(false\)')
) 'Formal combat-body buttons must expose the confirmed labels and use RequestRelease.'

Assert-True (
    $triggerDisableReasonText -match 'CombatBodyUnavailable'
) 'TriggerDisableReason must expose CombatBodyUnavailable so collapse can reuse the formal disabled state.'

Assert-True (
    ($combatBodySessionServiceText -match 'TriggerCollapse\(string reason\)[\s\S]*SetCombatBodyUnavailableDisabled\(true\)') -and
    ($battleActivationTransactionText -match 'rawCombatBodyService\.TryEnterActive\(allocateAmount\)[\s\S]*SetCombatBodyUnavailableDisabled\(false\)')
) 'CombatBody collapse must enter the formal disabled state immediately, and activation must clear that disabled state before re-entering battle mode.'

Write-Output 'CombatBodyTriggerTrionIntegration PASS'

