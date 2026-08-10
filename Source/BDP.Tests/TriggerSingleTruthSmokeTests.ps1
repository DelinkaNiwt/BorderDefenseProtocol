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

$slotPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\TriggerSlotState.cs'
$bodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$readsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Reads.cs'
$contextsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Contexts.cs'
$integrityPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Integrity.cs'
$lifecyclePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$loadoutServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Loadout\TriggerLoadoutService.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$snapshotBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSnapshotBuilder.cs'
$verbHostManagerPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$projectionDirtyReasonPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\ProjectionDirtyReason.cs'
$combatProjectionPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerCombatProjectionState.cs'
$presentationProjectionPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerPresentationState.cs'
$runtimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$projectionBuildInputPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Projection\TriggerProjectionBuildInput.cs'
$combatProjectionBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Projection\TriggerCombatProjectionBuilder.cs'
$presentationBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Projection\TriggerPresentationBuilder.cs'
$equipmentTickPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs'
$attackSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$attackPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_TryGetAttackVerb.cs'
$formalHostsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.FormalHosts.cs'
$diagnosticsPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionDiagnostics.cs'
$bodyConstraintEvaluatorPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyDisableEvaluator.cs'
$semanticResolverPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyPartSemanticResolver.cs'
$semanticResultPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyPartSemanticResult.cs'
$triggerDisableSyncPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerDisableSync.cs'
$addDirectPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_HediffSet_AddDirect_BodyConstraintSignal.cs'
$removeHediffPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_HealthTracker_RemoveHediff_BodyConstraintSignal.cs'

$slotText = Get-Content -LiteralPath $slotPath -Raw -Encoding utf8
$bodyText = Get-Content -LiteralPath $bodyPath -Raw -Encoding utf8
$readsText = Get-Content -LiteralPath $readsPath -Raw -Encoding utf8
$contextsText = Get-Content -LiteralPath $contextsPath -Raw -Encoding utf8
$integrityText = Get-Content -LiteralPath $integrityPath -Raw -Encoding utf8
$lifecycleText = Get-Content -LiteralPath $lifecyclePath -Raw -Encoding utf8
$loadoutServiceText = Get-Content -LiteralPath $loadoutServicePath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$snapshotBuilderText = Get-Content -LiteralPath $snapshotBuilderPath -Raw -Encoding utf8
$verbHostManagerText = Get-Content -LiteralPath $verbHostManagerPath -Raw -Encoding utf8
$projectionDirtyReasonText = if (Test-Path -LiteralPath $projectionDirtyReasonPath) { Get-Content -LiteralPath $projectionDirtyReasonPath -Raw -Encoding utf8 } else { '' }
$combatProjectionText = if (Test-Path -LiteralPath $combatProjectionPath) { Get-Content -LiteralPath $combatProjectionPath -Raw -Encoding utf8 } else { '' }
$presentationProjectionText = if (Test-Path -LiteralPath $presentationProjectionPath) { Get-Content -LiteralPath $presentationProjectionPath -Raw -Encoding utf8 } else { '' }
$projectionBuildInputText = if (Test-Path -LiteralPath $projectionBuildInputPath) { Get-Content -LiteralPath $projectionBuildInputPath -Raw -Encoding utf8 } else { '' }
$combatProjectionBuilderText = if (Test-Path -LiteralPath $combatProjectionBuilderPath) { Get-Content -LiteralPath $combatProjectionBuilderPath -Raw -Encoding utf8 } else { '' }
$presentationBuilderText = if (Test-Path -LiteralPath $presentationBuilderPath) { Get-Content -LiteralPath $presentationBuilderPath -Raw -Encoding utf8 } else { '' }
$runtimeCoordinatorText = if (Test-Path -LiteralPath $runtimeCoordinatorPath) { Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8 } else { '' }
$equipmentTickPatchText = Get-Content -LiteralPath $equipmentTickPatchPath -Raw -Encoding utf8
$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8
$attackPatchText = Get-Content -LiteralPath $attackPatchPath -Raw -Encoding utf8
$formalHostsText = Get-Content -LiteralPath $formalHostsPath -Raw -Encoding utf8
$diagnosticsText = Get-Content -LiteralPath $diagnosticsPath -Raw -Encoding utf8
$bodyConstraintEvaluatorText = if (Test-Path -LiteralPath $bodyConstraintEvaluatorPath) { Get-Content -LiteralPath $bodyConstraintEvaluatorPath -Raw -Encoding utf8 } else { '' }
$semanticResolverText = if (Test-Path -LiteralPath $semanticResolverPath) { Get-Content -LiteralPath $semanticResolverPath -Raw -Encoding utf8 } else { '' }
$semanticResultText = if (Test-Path -LiteralPath $semanticResultPath) { Get-Content -LiteralPath $semanticResultPath -Raw -Encoding utf8 } else { '' }
$triggerDisableSyncText = if (Test-Path -LiteralPath $triggerDisableSyncPath) { Get-Content -LiteralPath $triggerDisableSyncPath -Raw -Encoding utf8 } else { '' }
$addDirectPatchText = if (Test-Path -LiteralPath $addDirectPatchPath) { Get-Content -LiteralPath $addDirectPatchPath -Raw -Encoding utf8 } else { '' }
$removeHediffPatchText = if (Test-Path -LiteralPath $removeHediffPatchPath) { Get-Content -LiteralPath $removeHediffPatchPath -Raw -Encoding utf8 } else { '' }

Assert-True (
    Test-Path -LiteralPath $projectionDirtyReasonPath
) 'Task 1 must introduce a ProjectionDirtyReason contract for published combat projection invalidation.'

Assert-True (
    Test-Path -LiteralPath $combatProjectionPath
) 'Task 1 must introduce a TriggerCombatProjectionState contract as the published combat projection surface.'

Assert-True (
    Test-Path -LiteralPath $presentationProjectionPath
) 'Task 3 must introduce a TriggerPresentationState contract as the published presentation projection surface.'

Assert-True (
    Test-Path -LiteralPath $runtimeCoordinatorPath
) 'Task 1 must introduce a TriggerRuntimeCoordinator as the unique published combat projection owner.'

Assert-True (
    Test-Path -LiteralPath $projectionBuildInputPath
) 'Task 2 must introduce TriggerProjectionBuildInput as the owner-only projection build contract.'

Assert-True (
    Test-Path -LiteralPath $combatProjectionBuilderPath
) 'Task 2 must introduce TriggerCombatProjectionBuilder to assemble published combat projection state from owner input.'

Assert-True (
    Test-Path -LiteralPath $presentationBuilderPath
) 'Task 2 must introduce TriggerPresentationBuilder to assemble published presentation state from owner input.'

Assert-True (
    $projectionDirtyReasonText -match 'enum ProjectionDirtyReason'
) 'ProjectionDirtyReason must be declared as an enum.'

Assert-True (
    ($combatProjectionText -match 'ProjectionVersion') -and
    ($combatProjectionText -match 'ExpressionSnapshot Snapshot') -and
    ($combatProjectionText -match 'IReadOnlyDictionary<string, FormalExpressionResult> ResultIndex') -and
    ($combatProjectionText -match 'IReadOnlyDictionary<string, CompositeExpressionReference> CompositeReferenceIndex') -and
    ($combatProjectionText -match 'IReadOnlyDictionary<string, BdpFormalVerbHostSlot> ResultIdToFormalSlot') -and
    ($combatProjectionText -match 'bool IsEmpty')
) 'TriggerCombatProjectionState must expose the minimal published combat projection members required by Task 1.'

Assert-True (
    ($presentationProjectionText -match 'ProjectionVersion') -and
    ($presentationProjectionText -match 'ExpressionInfoProjection InfoProjection') -and
    ($presentationProjectionText -match 'ManualEntryProjection ManualProjection') -and
    ($presentationProjectionText -match 'VisualExpressionProjection VisualProjection')
) 'TriggerPresentationState must expose the minimal published presentation members required by Task 3.'

Assert-True (
    ($runtimeCoordinatorText -match 'CompTriggerBody owner') -and
    ($runtimeCoordinatorText -match 'currentProjectionVersion') -and
    ($runtimeCoordinatorText -match 'currentCombatProjection') -and
    ($runtimeCoordinatorText -match 'currentPresentationProjection') -and
    ($runtimeCoordinatorText -match 'projectionDirty') -and
    ($runtimeCoordinatorText -match 'dirtyReason')
) 'TriggerRuntimeCoordinator must own the minimal runtime projection state required by Task 1.'

Assert-True (
    $runtimeCoordinatorText -match 'RebuildAndPublish'
) 'TriggerRuntimeCoordinator must own the published combat projection rebuild entry.'

Assert-True (
    ($projectionBuildInputText -match 'sealed class TriggerProjectionBuildInput') -and
    ($projectionBuildInputText -match 'IReadOnlyList<TriggerSlotState> MainSlots') -and
    ($projectionBuildInputText -match 'IReadOnlyList<TriggerSlotState> SubSlots') -and
    ($projectionBuildInputText -match 'IReadOnlyList<TriggerSlotState> SpecialSlots') -and
    ($projectionBuildInputText -match 'SwitchContext MainSwitchContext') -and
    ($projectionBuildInputText -match 'SwitchContext SubSwitchContext') -and
    ($projectionBuildInputText -match 'SwitchContext SpecialSwitchContext') -and
    ($projectionBuildInputText -match 'bool IsMainContainerConsistent') -and
    ($projectionBuildInputText -match 'bool IsSubContainerConsistent') -and
    ($projectionBuildInputText -match 'bool IsSpecialContainerConsistent')
) 'TriggerProjectionBuildInput must capture owner slot truth, switch truth, and container consistency directly from CompTriggerBody.'

Assert-True (
    ($combatProjectionBuilderText -match 'sealed class TriggerCombatProjectionBuilder') -and
    ($combatProjectionBuilderText -match 'Build\(TriggerProjectionBuildInput buildInput') -and
    ($combatProjectionBuilderText -match 'BuildResultIndex') -and
    ($combatProjectionBuilderText -match 'BuildCompositeReferenceIndex') -and
    ($combatProjectionBuilderText -match 'BuildFormalSlotIndex')
) 'TriggerCombatProjectionBuilder must build combat projection and indexes from owner-supplied input.'

Assert-True (
    ($presentationBuilderText -match 'sealed class TriggerPresentationBuilder') -and
    ($presentationBuilderText -match 'Build\(ExpressionService expressionService, ExpressionSnapshot snapshot, int projectionVersion\)') -and
    ($presentationBuilderText -match 'BuildPublishedInfoProjection') -and
    ($presentationBuilderText -match 'BuildPublishedManualProjection') -and
    ($presentationBuilderText -match 'BuildPublishedVisualProjection')
) 'TriggerPresentationBuilder must build published presentation state from an already-selected snapshot.'

Assert-True (
    $runtimeCoordinatorText -match 'RuntimeTick\(\)'
) 'TriggerRuntimeCoordinator must own a unified RuntimeTick() entry instead of acting only as a publisher.'

Assert-True (
    $runtimeCoordinatorText -match 'RuntimeTick\(\)[\s\S]*RebuildAndPublish\(\)[\s\S]*owner\?\.VerbHostManager\?\.Tick\(\);'
) 'TriggerRuntimeCoordinator.RuntimeTick() must own both projection publication and formal-host runtime advancement.'

Assert-True (
    ($runtimeCoordinatorText -match 'TriggerProjectionBuildInput') -and
    ($runtimeCoordinatorText -match 'TriggerCombatProjectionBuilder') -and
    ($runtimeCoordinatorText -match 'TriggerPresentationBuilder')
) 'TriggerRuntimeCoordinator must depend on owner-input builders instead of inlining projection assembly.'

Assert-True (
    $runtimeCoordinatorText -notmatch 'BuildSelectedSnapshot\(ownerPawn, owner\.LoadoutReaderSurface\)'
) 'TriggerRuntimeCoordinator must stop rebuilding owner published projection through ExpressionService.BuildSelectedSnapshot(ownerPawn, owner.LoadoutReaderSurface).'

Assert-True (
    $runtimeCoordinatorText -notmatch 'BuildCombatProjectionState'
) 'TriggerRuntimeCoordinator must stop owning inline combat projection assembly once Task 2 lands.'

Assert-True (
    $runtimeCoordinatorText -notmatch 'BuildPresentationProjectionState'
) 'TriggerRuntimeCoordinator must stop owning inline presentation projection assembly once Task 2 lands.'

Assert-True (
    $bodyText -match 'TriggerRuntimeCoordinator'
) 'CompTriggerBody must hold a TriggerRuntimeCoordinator.'

Assert-True (
    $bodyText -match 'internal bool RuntimeTick\(\)'
) 'CompTriggerBody must expose a unified RuntimeTick() entry for the primary-weapon runtime owner.'

Assert-True (
    $equipmentTickPatchText -match 'triggerBody\?\.RuntimeTick\(\);'
) 'Primary weapon equipment tick must advance trigger runtime only through the unified RuntimeTick() entry.'

Assert-True (
    $bodyText -match 'PublishedCombatProjection'
) 'CompTriggerBody must expose a pure read surface for the published combat projection.'

Assert-True (
    $bodyText -match 'PublishedPresentationProjection'
) 'CompTriggerBody must expose a pure read surface for the published presentation projection.'

Assert-True (
    ($bodyText -match 'BuildProjectionBuildInput\(\)') -and
    ($contextsText -match 'SnapshotSlotsForProjectionBuild') -and
    ($readsText -match 'IsContainerConsistentForProjectionBuild')
) 'CompTriggerBody must expose owner-internal build-input capture helpers for Task 2.'

Assert-True (
    $bodyText -notmatch 'TryLoadChip\(TriggerSide side, int slotIndex, Thing chip\)[\s\S]*RefreshProjectedOutputs\(\);'
) 'TryLoadChip must stop performing projection publication directly.'

Assert-True (
    $bodyText -notmatch 'TryUnloadChip\(TriggerSide side, int slotIndex\)[\s\S]*RefreshProjectedOutputs\(\);'
) 'TryUnloadChip must stop performing projection publication directly.'

Assert-True (
    $bodyText -match 'Notify_Unequipped\(Pawn pawn\)[\s\S]*ForceTeardownOnDetach\(pawn\);'
) 'Notify_Unequipped must delegate detach cleanup through ForceTeardownOnDetach.'

Assert-True (
    $integrityText -notmatch 'NotifySlotActivationCommitted\(TriggerSide side, int slotIndex, Thing chip\)[\s\S]*RefreshProjectedOutputs\(\);'
) 'Slot activation commit must stop publishing projections directly from CompTriggerBody integrity callbacks.'

Assert-True (
    $integrityText -notmatch 'NotifySlotDeactivated\(TriggerSide side, int slotIndex, Thing chip\)[\s\S]*RefreshProjectedOutputs\(\);'
) 'Slot deactivation must stop publishing projections directly from CompTriggerBody integrity callbacks.'

Assert-True (
    $lifecycleText -notmatch 'private void RefreshProjectedOutputs\(\)'
) 'CompTriggerBody lifecycle must stop owning the projection publication implementation directly.'

Assert-True (
    $lifecycleText -match 'TryFinalizePostLoadProjectionRefresh\(\)[\s\S]*runtimeCoordinator'
) 'Post-load projection finalization must delegate to TriggerRuntimeCoordinator.'

Assert-True (
    ($lifecycleText -notmatch 'SyncProjectedHosts') -and
    ($lifecycleText -notmatch 'verbHostManager\?\.Refresh')
) 'CompTriggerBody lifecycle must stop syncing projected hosts and formal hosts directly.'

Assert-True (
    $bodyText -match 'private bool isRestoringPostLoad;'
) 'CompTriggerBody must declare an explicit PostLoad restore-phase guard.'

Assert-True (
    $bodyText -match 'BeginPostLoadRestorePhase\(\)'
) 'CompTriggerBody must expose an explicit restore-phase entry method.'

Assert-True (
    $bodyText -match 'EndPostLoadRestorePhase\(\)'
) 'CompTriggerBody must expose an explicit restore-phase exit method.'

Assert-True (
    $lifecycleText -match 'BeginPostLoadRestorePhase\(\)[\s\S]*RestoreSlotTruth\(\)[\s\S]*RebuildContainerFromSlotTruth\(\)[\s\S]*TryFinalizePostLoadProjectionRefresh\(\)[\s\S]*finally[\s\S]*EndPostLoadRestorePhase\(\)'
) 'Post-load restore must be wrapped in an explicit restore phase and finish through TriggerRuntimeCoordinator publication.'

Assert-True (
    ($readsText -notmatch 'PreparePublishedReadState\(') -and
    ($readsText -notmatch 'PrepareReadState\(')
) 'Task 4 must remove ordinary read-time reconciliation helpers so CompTriggerBody reads stay pure.'

Assert-True (
    $readsText -match 'GetActiveSlots\(\)[\s\S]*slot\.IsActive'
) 'Trigger reads must derive active slots directly from slot truth.'

$businessTruthReadsUseSlotsOnly =
    ($readsText -match 'GetActiveSlots\(\)[\s\S]*slot\.IsActive') -and
    ($readsText -match 'GetActiveSlot\(TriggerSide side\)[\s\S]*slot\.IsActive')

Assert-True $businessTruthReadsUseSlotsOnly 'Trigger business-truth reads must continue deriving activation only from slot state.'

Assert-True (
    $slotText -match 'loadedChipThingId'
) 'TriggerSlotState must persist a slot-owned stable chip identity for load recovery.'

Assert-True (
    $slotText -match 'SetLoadedChip\(Thing chip\)[\s\S]*loadedChipThingId'
) 'SetLoadedChip must update the slot-owned chip identity together with the slot truth.'

Assert-True (
    $integrityText -match 'RestoreSlotTruth'
) 'Trigger integrity layer must expose explicit slot-truth restoration.'

Assert-True (
    $integrityText -match 'RebuildContainerFromSlotTruth'
) 'Trigger integrity layer must rebuild chipContainer from slot truth.'

Assert-True (
    $integrityText -match 'IsActuallyInChipContainer'
) 'Integrity layer must use a real chip-container membership check instead of trusting holdingOwner alone.'

Assert-True (
    $integrityText -notmatch 'chip\.holdingOwner == chipContainer \|\| chipContainer\.Contains\(chip\)'
) 'EnsureChipInContainer must not treat holdingOwner equality as proof that the chip is actually present in the formal container list.'

Assert-True (
    $integrityText -match 'TryTransferToContainer\(\s*chip,\s*chipContainer,\s*canMergeWithExistingStacks:\s*false\s*\)'
) 'Trigger formal container transfer must forbid stack merge so slot-owned chip identity is not absorbed into another stack.'

Assert-True (
    $integrityText -match 'chipContainer\.TryAdd\(\s*chip,\s*canMergeWithExistingStacks:\s*false\s*\)'
) 'Trigger formal container direct add must forbid stack merge so each slot keeps a stable chip object identity.'

Assert-True (
    $integrityText -notmatch 'container 有 chip、slot 没 chip'
) 'Integrity layer must no longer describe reverse guessing from container back into slot truth.'

Assert-True (
    $lifecycleText -match 'RestoreSlotTruth\(\)[\s\S]*RebuildContainerFromSlotTruth\(\)[\s\S]*TryFinalizePostLoadProjectionRefresh\(\)'
) 'Post-load restore order must be slot truth, then container rebuild, then coordinator-driven projection finalization.'

Assert-True (
    $lifecycleText -notmatch 'RestoreSlotContainerIntegrity\(\)[\s\S]*RefreshProjectedOutputs\(\)'
) 'Post-load restore must not rely on the old slot/container repair shortcut.'

Assert-True (
    $loadoutServiceText -match 'EnsureChipInFormalContainer\(chip\)[\s\S]*slot\.SetLoadedChip\(chip\)[\s\S]*context\.SyncContainerFromSlotTruth'
) 'Load flow must ensure the chip is in the formal container before committing slot truth and synchronizing derived container state.'

Assert-True (
    $contextsText -match 'EnsureChipInFormalContainer'
) 'Trigger loadout context must expose a pre-commit formal-container admission step for load transactions.'

Assert-True (
    $loadoutServiceText -match 'slot\.SetLoadedChip\(null\)[\s\S]*context\.SyncContainerFromSlotTruth'
) 'Unload flow must clear slot truth before synchronizing the derived container.'

Assert-True (
    $loadoutServiceText -notmatch 'TryAddChipToContainer'
) 'Load flow must not directly mutate chipContainer as a primary truth source.'

Assert-True (
    $loadoutServiceText -notmatch 'RemoveChipFromContainer'
) 'Unload flow must not directly mutate chipContainer as a primary truth source.'

Assert-True (
    $contextsText -match 'SyncContainerFromSlotTruth'
) 'Trigger loadout context must expose derived container synchronization, not direct add/remove delegates.'

$expressionUsesLoadoutReader =
    ($expressionSurfaceText -match 'ResolveLoadoutReader') -and
    ($expressionSurfaceText -notmatch 'chipContainer')

Assert-True $expressionUsesLoadoutReader 'Expression surface must derive its business input from the trigger loadout reader, not chipContainer.'

Assert-True (
    $verbHostManagerText -notmatch 'chipContainer'
) 'Verb host rebuild must not read chipContainer business state directly.'

Assert-True (
    $attackSurfaceText -notmatch 'chipContainer'
) 'Attack surface must not fall back to chipContainer-driven business semantics.'

Assert-True (
    $formalHostsText -notmatch 'public new List<VerbProperties> VerbProperties'
) 'Trigger baseline truth must not let CompTriggerBody override the vanilla VerbProperties declaration surface.'

$lifecycleNoInvestigationLogs =
    ($lifecycleText -notmatch 'trigger_postload_truth_') -and
    ($lifecycleText -notmatch 'LogTriggerTruthSnapshot') -and
    ($lifecycleText -notmatch 'LogPawnStanceSnapshot') -and
    ($lifecycleText -notmatch 'LogPostLoadRestoreSnapshot') -and
    ($lifecycleText -notmatch 'LogSaveStateSnapshot')

Assert-True $lifecycleNoInvestigationLogs 'Temporary post-load investigation logs must be removed from the lifecycle path.'

Assert-True (
    ($verbHostManagerText -notmatch 'LogVerbHostRefresh') -and
    ($verbHostManagerText -notmatch 'LogPrimaryBindingSelection')
) 'Temporary verb-host investigation logs must be removed.'

$diagnosticsNoInvestigationHelpers =
    ($diagnosticsText -notmatch 'LogVerbHostRefresh') -and
    ($diagnosticsText -notmatch 'LogAutoRangedVerbSelection') -and
    ($diagnosticsText -notmatch 'LogExpressionSnapshotEmpty') -and
    ($diagnosticsText -notmatch 'LogPawnStanceSnapshot')

Assert-True $diagnosticsNoInvestigationHelpers 'AttackExecutionDiagnostics must not retain the temporary investigation helpers.'

Assert-True (
    $loadoutServiceText -notmatch 'LogLoadoutMutation'
) 'Temporary loadout mutation evidence logs must be removed.'

Assert-True (
    $attackPatchText -notmatch 'LogOriginalAutoVerbSelection'
) 'Auto-ranged investigation logs about original verb picks must be removed from the runtime path.'

$permanentArchitectureDiagnosticsPresent =
    ($integrityText -match 'slot_truth_missing_after_load') -and
    ($integrityText -match 'orphan_container_chip') -and
    ($runtimeCoordinatorText -match 'expression_empty_with_nonempty_container')

Assert-True $permanentArchitectureDiagnosticsPresent 'Permanent compact architecture diagnostics must remain after temporary logs are removed.'

Assert-True (
    (Test-Path -LiteralPath $semanticResolverPath) -and
    (Test-Path -LiteralPath $semanticResultPath) -and
    ($semanticResultText -match 'CanDisableTrigger')
) 'Body constraint semantic files must exist as the new single-truth boundary.'

Assert-True (
    ($bodyConstraintEvaluatorText -match 'TriggerBodyPartSemanticResolver') -and
    ($bodyConstraintEvaluatorText -notmatch '"Hand"') -and
    ($bodyConstraintEvaluatorText -notmatch '"Arm"') -and
    ($bodyConstraintEvaluatorText -notmatch '"Shoulder"') -and
    ($bodyConstraintEvaluatorText -notmatch 'LabelShort')
) 'Body constraint evaluator must stop owning string-based part truth.'

Assert-True (
    ($semanticResolverText -match 'BodyPartTagDefOf\.ManipulationLimbCore') -and
    ($semanticResolverText -match 'BodyPartGroupDefOf\.LeftHand') -and
    ($semanticResolverText -match 'BodyPartGroupDefOf\.RightHand') -and
    ($semanticResolverText -notmatch 'Milira') -and
    ($semanticResolverText -notmatch 'Milian')
) 'Semantic resolver must use shared body semantics instead of race-specific truth.'

Assert-True (
    ($triggerDisableSyncText -match 'TriggerBodyDisableEvaluator\.EvaluateSideDisableReason') -and
    ($triggerDisableSyncText -notmatch 'TriggerBodyPartSemanticResolver')
) 'TriggerDisableSync must keep slot disable truth downstream from evaluator only.'

Assert-True (
    ($addDirectPatchText -match 'MissingPartChanged') -and
    ($addDirectPatchText -notmatch 'TriggerDisableSync') -and
    ($removeHediffPatchText -match 'MissingPartChanged') -and
    ($removeHediffPatchText -notmatch 'TriggerDisableSync')
) 'Missing-part patches must remain fact publishers instead of becoming business truth owners.'

Write-Output 'TriggerSingleTruth PASS'

