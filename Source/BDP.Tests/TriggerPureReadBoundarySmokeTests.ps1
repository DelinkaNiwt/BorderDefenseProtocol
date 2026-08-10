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

$expressionReaderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Contracts\IExpressionReader.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$manualResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
$manualBridgePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\ExpressionManualGizmoBridge.cs'
$gizmoServicePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\External\TriggerEquippedGizmoService.cs'
$readsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Reads.cs'
$semanticResolverPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyPartSemanticResolver.cs'
$bodyConstraintEvaluatorPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyDisableEvaluator.cs'
$triggerDisableSyncPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerDisableSync.cs'

$expressionReaderText = Get-Content -LiteralPath $expressionReaderPath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$manualResolverText = Get-Content -LiteralPath $manualResolverPath -Raw -Encoding utf8
$manualBridgeText = Get-Content -LiteralPath $manualBridgePath -Raw -Encoding utf8
$gizmoServiceText = Get-Content -LiteralPath $gizmoServicePath -Raw -Encoding utf8
$readsText = Get-Content -LiteralPath $readsPath -Raw -Encoding utf8
$semanticResolverText = if (Test-Path -LiteralPath $semanticResolverPath) { Get-Content -LiteralPath $semanticResolverPath -Raw -Encoding utf8 } else { '' }
$bodyConstraintEvaluatorText = if (Test-Path -LiteralPath $bodyConstraintEvaluatorPath) { Get-Content -LiteralPath $bodyConstraintEvaluatorPath -Raw -Encoding utf8 } else { '' }
$triggerDisableSyncText = if (Test-Path -LiteralPath $triggerDisableSyncPath) { Get-Content -LiteralPath $triggerDisableSyncPath -Raw -Encoding utf8 } else { '' }

Assert-True (
    ($expressionReaderText -notmatch 'GetSnapshot\(Pawn pawn\)') -and
    ($expressionReaderText -match 'TriggerCombatProjectionState\s+GetCombatProjection\(Pawn pawn\)')
) 'IExpressionReader must stop treating GetSnapshot(Pawn) as the main read contract and expose published combat projection instead.'

Assert-True (
    ($expressionSurfaceText -match 'public TriggerCombatProjectionState GetCombatProjection\(Pawn pawn\)') -and
    ($expressionSurfaceText -notmatch 'PreparePublishedReadState\(\)') -and
    ($expressionSurfaceText -notmatch 'PrepareReadState\(\)')
) 'ExpressionService published read surfaces must read published state directly and must not reconcile trigger runtime on ordinary reads.'

Assert-True (
    ($manualResolverText -notmatch 'GetSnapshot\(pawn\)') -and
    ($manualResolverText -match 'GetCombatProjection\(pawn\)')
) 'DefaultManualEntryGizmoResolver must read published combat projection directly instead of asking IExpressionReader for a full snapshot.'

Assert-True (
    ($manualBridgeText -match 'GetManualProjection\(pawn\)') -and
    ($gizmoServiceText -match 'ExpressionManualGizmoBridge\.BuildGizmos')
) 'Manual gizmo path must stay on published manual projection and keep hanging from the same bridge/service chain.'

Assert-True (
    ($readsText -notmatch 'PreparePublishedReadState\(') -and
    ($readsText -notmatch 'PrepareReadState\(')
) 'CompTriggerBody ordinary read path must delete PreparePublishedReadState/PrepareReadState so reads become pure.'

Assert-True (
    ($readsText -match 'GetAllSlots\(\)[\s\S]*EnsureSlots\(\)') -and
    ($readsText -notmatch 'GetHeldChips\(\)') -and
    ($readsText -notmatch 'IsSlotContainerConsistent\(')
) 'CompTriggerBody pure reads must retain formal slot reads while retired diagnostics-only container reads stay deleted.'

Assert-True (
    (Test-Path -LiteralPath $semanticResolverPath) -and
    ($semanticResolverPath -match 'Core\\BodyConstraints\\TriggerBodyPartSemanticResolver\.cs$')
) 'TriggerBodyPartSemanticResolver must stay inside Core/BodyConstraints.'

Assert-True (
    ($triggerDisableSyncText -notmatch 'TriggerBodyPartSemanticResolver') -and
    ($triggerDisableSyncText -notmatch 'BodyPartRecord') -and
    ($triggerDisableSyncText -match 'TriggerBodyDisableEvaluator\.EvaluateSideDisableReason')
) 'TriggerDisableSync must remain a pure consumer of evaluator results.'

Assert-True (
    ($bodyConstraintEvaluatorText -match 'TriggerBodyPartSemanticResolver') -and
    ($bodyConstraintEvaluatorText -notmatch 'LabelShort') -and
    ($bodyConstraintEvaluatorText -notmatch 'customLabel')
) 'Body constraint evaluation must stay inside BodyConstraints and avoid UI label truth.'

Write-Output 'TriggerPureReadBoundarySmokeTests PASS'

