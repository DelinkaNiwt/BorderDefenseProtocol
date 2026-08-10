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

$presentationStatePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerPresentationState.cs'
$runtimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$bodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$projectionBuildInputPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Projection\TriggerProjectionBuildInput.cs'
$combatProjectionBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Projection\TriggerCombatProjectionBuilder.cs'
$presentationBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Projection\TriggerPresentationBuilder.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$expressionPublicSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionSurfaceAccess.cs'
$publishedBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionPublishedSnapshotBuilder.cs'
$publishedChannelKindPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionPublishedChannelKind.cs'
$publishedDatumPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionPublishedDatum.cs'
$publishedCompositePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionPublishedCompositeReference.cs'
$publishedResultPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionPublishedResultSnapshot.cs'
$publishedProjectionPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionPublishedProjectionSnapshot.cs'
$manualBridgePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\ExpressionManualGizmoBridge.cs'
$gizmoServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\External\TriggerEquippedGizmoService.cs'
$publicationSnapshotPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\ExpressionPublicationSnapshot.cs'
$hostSynchronizerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionHostSynchronizer.cs'
$abilitySynchronizerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionAbilityHostSynchronizer.cs'
$hediffSynchronizerPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionHediffHostSynchronizer.cs'
$infoProjectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionInfoProjector.cs'

$presentationStateText = if (Test-Path -LiteralPath $presentationStatePath) {
    Get-Content -LiteralPath $presentationStatePath -Raw -Encoding utf8
} else {
    ''
}
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8
$bodyText = Get-Content -LiteralPath $bodyPath -Raw -Encoding utf8
$projectionBuildInputText = if (Test-Path -LiteralPath $projectionBuildInputPath) {
    Get-Content -LiteralPath $projectionBuildInputPath -Raw -Encoding utf8
} else {
    ''
}
$combatProjectionBuilderText = if (Test-Path -LiteralPath $combatProjectionBuilderPath) {
    Get-Content -LiteralPath $combatProjectionBuilderPath -Raw -Encoding utf8
} else {
    ''
}
$presentationBuilderText = if (Test-Path -LiteralPath $presentationBuilderPath) {
    Get-Content -LiteralPath $presentationBuilderPath -Raw -Encoding utf8
} else {
    ''
}
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$expressionPublicSurfaceText = if (Test-Path -LiteralPath $expressionPublicSurfacePath) {
    Get-Content -LiteralPath $expressionPublicSurfacePath -Raw -Encoding utf8
} else {
    ''
}
$publishedProjectionText = if (Test-Path -LiteralPath $publishedProjectionPath) {
    Get-Content -LiteralPath $publishedProjectionPath -Raw -Encoding utf8
} else {
    ''
}
$publishedResultText = if (Test-Path -LiteralPath $publishedResultPath) {
    Get-Content -LiteralPath $publishedResultPath -Raw -Encoding utf8
} else {
    ''
}
$manualBridgeText = Get-Content -LiteralPath $manualBridgePath -Raw -Encoding utf8
$gizmoServiceText = Get-Content -LiteralPath $gizmoServicePath -Raw -Encoding utf8
$hostSynchronizerText = Get-Content -LiteralPath $hostSynchronizerPath -Raw -Encoding utf8
$abilitySynchronizerText = Get-Content -LiteralPath $abilitySynchronizerPath -Raw -Encoding utf8
$hediffSynchronizerText = Get-Content -LiteralPath $hediffSynchronizerPath -Raw -Encoding utf8
$infoProjectorText = Get-Content -LiteralPath $infoProjectorPath -Raw -Encoding utf8

Assert-True (
    Test-Path -LiteralPath $presentationStatePath
) 'Task 3 must introduce TriggerPresentationState as the published UI/presentation read surface.'

Assert-True (
    ($presentationStateText -match 'int\s+ProjectionVersion\s*\{') -and
    ($presentationStateText -match 'ExpressionInfoProjection\s+InfoProjection\s*\{') -and
    ($presentationStateText -match 'ManualEntryProjection\s+ManualProjection\s*\{') -and
    ($presentationStateText -match 'VisualExpressionProjection\s+VisualProjection\s*\{')
) 'TriggerPresentationState must expose projection version, info projection, manual projection, and visual projection.'

Assert-True (
    ($runtimeCoordinatorText -match 'TriggerPresentationState\s+currentPresentationProjection') -and
    ($runtimeCoordinatorText -match 'TriggerPresentationState\s+CurrentPresentationProjection')
) 'TriggerRuntimeCoordinator must continue owning and publishing a TriggerPresentationState that shares the same projection version.'

Assert-True (
    ($bodyText -match 'PublishedPresentationProjection') -and
    ($bodyText -match 'CurrentPresentationProjection')
) 'CompTriggerBody must expose a pure read surface for the published presentation projection.'

Assert-True (
    Test-Path -LiteralPath $projectionBuildInputPath
) 'Task 2 must introduce TriggerProjectionBuildInput so owner publication does not rebuild through public read surfaces.'

Assert-True (
    Test-Path -LiteralPath $combatProjectionBuilderPath
) 'Task 2 must introduce TriggerCombatProjectionBuilder so combat projection assembly leaves TriggerRuntimeCoordinator.'

Assert-True (
    Test-Path -LiteralPath $presentationBuilderPath
) 'Task 2 must introduce TriggerPresentationBuilder so presentation assembly leaves TriggerRuntimeCoordinator.'

Assert-True (
    (-not ($projectionBuildInputText -match 'ITriggerLoadoutReader')) -and
    ($projectionBuildInputText -match 'TriggerSlotState')
) 'TriggerProjectionBuildInput must capture owner truth directly and must not depend on the public ITriggerLoadoutReader read contract.'

Assert-True (
    ($combatProjectionBuilderText -match 'Build\(TriggerProjectionBuildInput buildInput') -and
    ($combatProjectionBuilderText -notmatch 'ITriggerLoadoutReader') -and
    ($combatProjectionBuilderText -notmatch 'PreparePublishedReadState')
) 'TriggerCombatProjectionBuilder must consume owner build input directly, not bounce back into public reader surfaces.'

Assert-True (
    ($presentationBuilderText -match 'Build\(ExpressionService expressionService, ExpressionSnapshot snapshot, int projectionVersion\)') -and
    ($presentationBuilderText -notmatch 'Pawn pawn') -and
    ($presentationBuilderText -notmatch 'ITriggerLoadoutReader')
) 'TriggerPresentationBuilder must only consume the selected snapshot and projection version, not re-enter pawn or reader lookups.'

Assert-True (
    ($expressionSurfaceText -notmatch 'GetSnapshot\(Pawn pawn\)\s*\{\s*return BuildSelectedSnapshot\(pawn\);') -and
    ($expressionSurfaceText -notmatch 'GetInfoProjection\(Pawn pawn, bool includeDiagnostics = false\)\s*\{[\s\S]*infoProjector\.Build\(BuildSelectedSnapshot\(pawn\)\)') -and
    ($expressionSurfaceText -notmatch 'GetManualProjection\(Pawn pawn\)\s*\{[\s\S]*manualProjector\.Build\(BuildSelectedSnapshot\(pawn\)\)') -and
    ($expressionSurfaceText -notmatch 'GetVisualProjection\(Pawn pawn\)\s*\{[\s\S]*visualProjector\.Build\(BuildSelectedSnapshot\(pawn\)\)')
) 'ExpressionService default read surfaces must stop rebuilding selected snapshots on ordinary reads.'

Assert-True (
    ($expressionSurfaceText -match 'PublishedCombatProjection') -and
    ($expressionSurfaceText -match 'PublishedPresentationProjection')
) 'ExpressionService default read surfaces must read from published combat/presentation state.'

Assert-True (
    ($expressionSurfaceText -match 'GetCombatProjection\(Pawn pawn\)') -and
    ($expressionSurfaceText -notmatch 'PreparePublishedReadState\(\)') -and
    ($expressionSurfaceText -notmatch 'PrepareReadState\(\)')
) 'ExpressionService published reads must expose combat projection directly and must not reconcile trigger runtime during ordinary reads.'

Assert-True (
    ($expressionSurfaceText -notmatch 'BuildSelectedSnapshot\(Pawn pawn\)\s*\{') -and
    ($expressionSurfaceText -match 'BuildSelectedSnapshot\(Pawn pawn, ITriggerLoadoutReader triggerLoadoutReader\)')
) 'ExpressionService must delete the generic pawn-only selected snapshot rebuild entry and keep only the owner-supplied loadout-reader build path.'

Assert-True (
    $expressionSurfaceText -notmatch 'TryGetSelectedResult\s*\('
) 'ExpressionService must delete the old selected-result helper that encouraged read-time re-resolve.'

Assert-True (
    ($expressionSurfaceText -match 'SyncProjectedHosts\(Pawn pawn, ExpressionSnapshot snapshot\)') -and
    ($expressionSurfaceText -notmatch 'SyncProjectedHosts\(Pawn pawn, ExpressionSnapshot snapshot = null\)') -and
    ($expressionSurfaceText -notmatch 'snapshot \?\? BuildSelectedSnapshot\(pawn\)')
) 'ExpressionService host synchronization must consume an already-built snapshot and must not keep a hidden generic rebuild fallback.'

Assert-True (
    ($expressionSurfaceText -match 'new DefaultExpressionAbilityHostSynchronizer\(\)') -and
    ($expressionSurfaceText -match 'new DefaultExpressionHediffHostSynchronizer\(\)') -and
    ($hostSynchronizerText -match 'abilitySynchronizer\?\.Sync\(pawn, snapshot\)') -and
    ($hostSynchronizerText -match 'hediffSynchronizer\?\.Sync\(pawn, snapshot\)')
) 'Published projection pipeline must continue hanging Ability/Hediff host sync on the explicit published snapshot path.'

Assert-True (
    Test-Path -LiteralPath $publicationSnapshotPath
) 'Published projection diagnostics must introduce ExpressionPublicationSnapshot.'

Assert-True (
    ($hostSynchronizerText -match 'ExpressionPublicationSnapshot') -and
    ($hostSynchronizerText -match 'BuildPublicationSnapshot')
) 'Host synchronizer must expose a side-band publication snapshot without changing sync behavior.'

Assert-True (
    ($abilitySynchronizerText -match 'ResultId') -and
    ($abilitySynchronizerText -match 'SourceResultIds')
) 'Ability host synchronizer must retain result-level publication trace.'

Assert-True (
    ($hediffSynchronizerText -match 'ResultId') -and
    ($hediffSynchronizerText -match 'SourceResultIds')
) 'Hediff host synchronizer must retain result-level publication trace.'

Assert-True (
    ($expressionSurfaceText -match 'PublicationSnapshot') -and
    ($infoProjectorText -match 'PublishedKey')
) 'Info projection pipeline must be able to carry side-band publication diagnostics.'

Assert-True (
    ($abilitySynchronizerText -match 'ExpressionResultKind\.Ability') -and
    ($hediffSynchronizerText -match 'ExpressionResultKind\.Hediff')
) 'Ability/Hediff minimal closure must stay in the published host synchronization chain rather than moving back into Trigger truth mutation.'

Assert-True (
    ($runtimeCoordinatorText -match 'BuildProjectionBuildInput\(\)') -and
    ($runtimeCoordinatorText -notmatch 'BuildSelectedSnapshot\(ownerPawn, owner\.LoadoutReaderSurface\)') -and
    ($runtimeCoordinatorText -notmatch 'BuildCombatProjectionState\(') -and
    ($runtimeCoordinatorText -notmatch 'BuildPresentationProjectionState\(')
) 'TriggerRuntimeCoordinator must publish from owner build input and dedicated builders instead of reconstructing projection inline through the public reader path.'

Assert-True (
    $manualBridgeText -match 'GetManualProjection\(pawn\)'
) 'ExpressionManualGizmoBridge must continue reading manual entries through the published reader surface.'

Assert-True (
    $gizmoServiceText -match 'ExpressionManualGizmoBridge\.BuildGizmos'
) 'TriggerEquippedGizmoService must continue hanging expression gizmos from the published manual bridge.'

Assert-True (
    (Test-Path -LiteralPath $expressionPublicSurfacePath) -and
    (Test-Path -LiteralPath $publishedBuilderPath) -and
    (Test-Path -LiteralPath $publishedChannelKindPath) -and
    (Test-Path -LiteralPath $publishedDatumPath) -and
    (Test-Path -LiteralPath $publishedCompositePath) -and
    (Test-Path -LiteralPath $publishedResultPath) -and
    (Test-Path -LiteralPath $publishedProjectionPath)
) 'Public published-expression read surface must introduce a dedicated surface file, builder, and public DTO files.'

Assert-True (
    ($expressionPublicSurfaceText -match 'public\s+static\s+class\s+ExpressionSurfaceAccess') -and
    ($expressionPublicSurfaceText -match 'ResolvePublishedProjection\(Pawn pawn\)') -and
    ($expressionPublicSurfaceText -match 'TryGetPublishedResult') -and
    ($expressionPublicSurfaceText -match 'TryResolveVerbHost') -and
    ($expressionPublicSurfaceText -match 'TryResolveAbilityHost') -and
    ($expressionPublicSurfaceText -match 'TryResolveHediffHost')
) 'ExpressionSurfaceAccess must expose a public published projection reader and per-channel live host resolvers.'

Assert-True (
    ($publishedProjectionText -match 'ResultIndex') -and
    ($publishedProjectionText -match 'VerbResultsBySlotKey') -and
    ($publishedProjectionText -match 'AbilityResultsByDefName') -and
    ($publishedProjectionText -match 'HediffResultsByDefName') -and
    ($publishedProjectionText -match 'PassiveResultsByKey') -and
    ($publishedProjectionText -match 'CompositeReferenceIndex')
) 'Public published projection snapshot must expose unified result index, four-channel grouping, and composite reference index.'

Assert-True (
    ($publishedResultText -match 'ChannelKind') -and
    ($publishedResultText -match 'PublishedKey') -and
    ($publishedResultText -match 'SourceResultIds') -and
    ($publishedResultText -match 'TrionUseCost') -and
    ($publishedResultText -match 'ExposedData')
) 'Public published result snapshot must expose channel identity, stable published key, source references, Trion facts, and exposed data.'

Write-Output 'ExpressionPublishedProjectionSmokeTests PASS'
