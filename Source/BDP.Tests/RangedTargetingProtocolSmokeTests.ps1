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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$manualEntryRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ManualEntryRecord.cs'
$targetingRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\TargetingRecord.cs'
$previewRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\PreviewRecord.cs'
$confirmRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmRecord.cs'
$manualEntryStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\ManualEntry\IManualEntryStageModule.cs'
$targetingStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Targeting\ITargetingStageModule.cs'
$previewStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Preview\IPreviewStageModule.cs'
$previewDimensionPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Preview\PreviewDimension.cs'
$previewDimensionPolicyPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Preview\PreviewDimensionPolicy.cs'
$previewDrawItemPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Preview\PreviewDrawItem.cs'
$previewDrawItemKindPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Preview\PreviewDrawItemKind.cs'
$confirmStagePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Confirm\IConfirmStageModule.cs'
$targetingInputStatePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\TargetingInputState.cs'
$confirmedInputSnapshotPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmedInputSnapshot.cs'
$confirmedInteractionSnapshotPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmedInteractionSnapshot.cs'
$targetingInputFramePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInputFrame.cs'
$targetingAdvanceDecisionPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingAdvanceDecision.cs'
$targetingAdvanceKindPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingAdvanceKind.cs'
$sessionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSession.cs'
$gizmoResolverPath = Join-Path $bdpSourceRoot 'Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
$commandPath = Join-Path $bdpSourceRoot 'Expressions\Projection\Command_BdpManualEntryTarget.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'

$manualEntryRecordText = if (Test-Path -LiteralPath $manualEntryRecordPath) { Get-Content -LiteralPath $manualEntryRecordPath -Raw -Encoding utf8 } else { '' }
$targetingRecordText = if (Test-Path -LiteralPath $targetingRecordPath) { Get-Content -LiteralPath $targetingRecordPath -Raw -Encoding utf8 } else { '' }
$previewRecordText = if (Test-Path -LiteralPath $previewRecordPath) { Get-Content -LiteralPath $previewRecordPath -Raw -Encoding utf8 } else { '' }
$confirmRecordText = if (Test-Path -LiteralPath $confirmRecordPath) { Get-Content -LiteralPath $confirmRecordPath -Raw -Encoding utf8 } else { '' }
$manualEntryStageText = if (Test-Path -LiteralPath $manualEntryStagePath) { Get-Content -LiteralPath $manualEntryStagePath -Raw -Encoding utf8 } else { '' }
$targetingStageText = if (Test-Path -LiteralPath $targetingStagePath) { Get-Content -LiteralPath $targetingStagePath -Raw -Encoding utf8 } else { '' }
$previewStageText = if (Test-Path -LiteralPath $previewStagePath) { Get-Content -LiteralPath $previewStagePath -Raw -Encoding utf8 } else { '' }
$previewDimensionText = if (Test-Path -LiteralPath $previewDimensionPath) { Get-Content -LiteralPath $previewDimensionPath -Raw -Encoding utf8 } else { '' }
$previewDimensionPolicyText = if (Test-Path -LiteralPath $previewDimensionPolicyPath) { Get-Content -LiteralPath $previewDimensionPolicyPath -Raw -Encoding utf8 } else { '' }
$previewDrawItemText = if (Test-Path -LiteralPath $previewDrawItemPath) { Get-Content -LiteralPath $previewDrawItemPath -Raw -Encoding utf8 } else { '' }
$previewDrawItemKindText = if (Test-Path -LiteralPath $previewDrawItemKindPath) { Get-Content -LiteralPath $previewDrawItemKindPath -Raw -Encoding utf8 } else { '' }
$confirmStageText = if (Test-Path -LiteralPath $confirmStagePath) { Get-Content -LiteralPath $confirmStagePath -Raw -Encoding utf8 } else { '' }
$targetingInputStateText = if (Test-Path -LiteralPath $targetingInputStatePath) { Get-Content -LiteralPath $targetingInputStatePath -Raw -Encoding utf8 } else { '' }
$confirmedInputSnapshotText = if (Test-Path -LiteralPath $confirmedInputSnapshotPath) { Get-Content -LiteralPath $confirmedInputSnapshotPath -Raw -Encoding utf8 } else { '' }
$confirmedInteractionSnapshotText = if (Test-Path -LiteralPath $confirmedInteractionSnapshotPath) { Get-Content -LiteralPath $confirmedInteractionSnapshotPath -Raw -Encoding utf8 } else { '' }
$targetingInputFrameText = if (Test-Path -LiteralPath $targetingInputFramePath) { Get-Content -LiteralPath $targetingInputFramePath -Raw -Encoding utf8 } else { '' }
$targetingAdvanceDecisionText = if (Test-Path -LiteralPath $targetingAdvanceDecisionPath) { Get-Content -LiteralPath $targetingAdvanceDecisionPath -Raw -Encoding utf8 } else { '' }
$targetingAdvanceKindText = if (Test-Path -LiteralPath $targetingAdvanceKindPath) { Get-Content -LiteralPath $targetingAdvanceKindPath -Raw -Encoding utf8 } else { '' }
$sessionText = Get-Content -LiteralPath $sessionPath -Raw -Encoding utf8
$gizmoResolverText = Get-Content -LiteralPath $gizmoResolverPath -Raw -Encoding utf8
$commandText = Get-Content -LiteralPath $commandPath -Raw -Encoding utf8
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $manualEntryRecordPath) 'ManualEntryRecord.cs must exist.'
Assert-True (Test-Path -LiteralPath $targetingRecordPath) 'TargetingRecord.cs must exist.'
Assert-True (Test-Path -LiteralPath $previewRecordPath) 'PreviewRecord.cs must exist.'
Assert-True (Test-Path -LiteralPath $confirmRecordPath) 'ConfirmRecord.cs must exist.'
Assert-True (Test-Path -LiteralPath $manualEntryStagePath) 'IManualEntryStageModule.cs must exist.'
Assert-True (Test-Path -LiteralPath $targetingStagePath) 'ITargetingStageModule.cs must exist.'
Assert-True (Test-Path -LiteralPath $previewStagePath) 'IPreviewStageModule.cs must exist.'
Assert-True (Test-Path -LiteralPath $previewDimensionPath) 'PreviewDimension.cs must exist.'
Assert-True (Test-Path -LiteralPath $previewDimensionPolicyPath) 'PreviewDimensionPolicy.cs must exist.'
Assert-True (Test-Path -LiteralPath $previewDrawItemPath) 'PreviewDrawItem.cs must exist.'
Assert-True (Test-Path -LiteralPath $previewDrawItemKindPath) 'PreviewDrawItemKind.cs must exist.'
Assert-True (Test-Path -LiteralPath $confirmStagePath) 'IConfirmStageModule.cs must exist.'
Assert-True (Test-Path -LiteralPath $targetingInputStatePath) 'TargetingInputState.cs must exist.'
Assert-True (Test-Path -LiteralPath $confirmedInputSnapshotPath) 'ConfirmedInputSnapshot.cs must exist.'
Assert-True (Test-Path -LiteralPath $confirmedInteractionSnapshotPath) 'ConfirmedInteractionSnapshot.cs must exist.'
Assert-True (Test-Path -LiteralPath $targetingInputFramePath) 'TargetingInputFrame.cs must exist.'
Assert-True (Test-Path -LiteralPath $targetingAdvanceDecisionPath) 'TargetingAdvanceDecision.cs must exist.'
Assert-True (Test-Path -LiteralPath $targetingAdvanceKindPath) 'TargetingAdvanceKind.cs must exist.'

Assert-True (
    ($manualEntryRecordText -match 'class\s+ManualEntryRecord') -and
    ($manualEntryRecordText -match 'RangedAttackModuleSession\s+ModuleSession')
) 'ManualEntryRecord must carry the module session.'

Assert-True (
    ($targetingRecordText -match 'class\s+TargetingRecord') -and
    ($targetingRecordText -match 'TargetingParameters') -and
    ($targetingRecordText -match 'TargetingInputState\s+InputState')
) 'TargetingRecord must carry targeting parameters.'

Assert-True (
    ($previewRecordText -match 'class\s+PreviewRecord') -and
    ($previewRecordText -match 'LocalTargetInfo\s+Target') -and
    ($previewRecordText -match 'UseVanillaRangeRing') -and
    ($previewRecordText -match 'UseVanillaTargetHighlight') -and
    ($previewRecordText -match 'UseVanillaFieldRadius') -and
    ($previewRecordText -match 'UseVanillaMouseAttachment') -and
    ($previewRecordText -match 'PreviewDrawItem')
) 'PreviewRecord must carry the preview target.'

Assert-True (
    ($previewRecordText -notmatch 'UseVanillaHighlight') -and
    ($previewRecordText -notmatch 'UseVanillaOnGui')
) 'PreviewRecord must not keep coarse preview toggles.'

Assert-True (
    ($confirmRecordText -match 'class\s+ConfirmRecord') -and
    ($confirmRecordText -match 'AttackExecutionReason') -and
    ($confirmRecordText -match 'AttackDispatchIntent') -and
    ($confirmRecordText -match 'AttackContext\s+AttackContext') -and
    ($confirmRecordText -notmatch 'ConfirmedInputSnapshot')
) 'ConfirmRecord must carry confirm-stage dispatch facts.'

Assert-True (
    ($targetingInputStateText -match 'class\s+TargetingInputState') -and
    ($targetingInputStateText -match 'StepIndex') -and
    ($targetingInputStateText -match 'IsComplete')
) 'TargetingInputState must expose neutral input progress facts.'

Assert-True (
    ($previewDrawItemKindText -match 'enum\s+PreviewDrawItemKind') -and
    ($previewDrawItemKindText -match 'Line') -and
    ($previewDrawItemKindText -match 'Ring') -and
    ($previewDrawItemKindText -match 'CellGroup')
) 'PreviewDrawItemKind must expose neutral preview draw kinds.'

Assert-True (
    ($previewDrawItemText -match 'class\s+PreviewDrawItem') -and
    ($previewDrawItemText -match 'PreviewDrawItemKind') -and
    ($previewDrawItemText -match 'Color')
) 'PreviewDrawItem must expose neutral draw fields.'

Assert-True (
    ($confirmedInputSnapshotText -match 'class\s+ConfirmedInputSnapshot') -and
    ($confirmedInputSnapshotText -match 'StepIndex') -and
    ($confirmedInputSnapshotText -match 'Tags')
) 'ConfirmedInputSnapshot must expose neutral frozen input facts.'

Assert-True (
    ($targetingInputFrameText -match 'class\s+TargetingInputFrame') -and
    ($targetingInputFrameText -match 'HoveredTarget') -and
    ($targetingInputFrameText -match 'SelectedTarget')
) 'TargetingInputFrame must expose neutral per-frame interaction facts.'

Assert-True (
    ($targetingAdvanceDecisionText -match 'class\s+TargetingAdvanceDecision') -and
    ($targetingAdvanceDecisionText -match 'TargetingAdvanceKind')
) 'TargetingAdvanceDecision must expose the neutral targeting progression result.'

Assert-True (
    ($targetingAdvanceKindText -match 'enum\s+TargetingAdvanceKind') -and
    ($targetingAdvanceKindText -match 'Continue') -and
    ($targetingAdvanceKindText -match 'Complete') -and
    ($targetingAdvanceKindText -match 'Cancel') -and
    ($targetingAdvanceKindText -match 'Reject')
) 'TargetingAdvanceKind must define the neutral targeting progression states.'

Assert-True (
    ($confirmedInteractionSnapshotText -match 'class\s+ConfirmedInteractionSnapshot') -and
    ($confirmedInteractionSnapshotText -match 'StepIndex') -and
    ($confirmedInteractionSnapshotText -match 'IsComplete')
) 'ConfirmedInteractionSnapshot must expose neutral confirmed interaction facts.'

Assert-True (
    ($manualEntryStageText -match 'interface\s+IManualEntryStageModule') -and
    ($manualEntryStageText -match 'ManualEntryRecord')
) 'IManualEntryStageModule must consume ManualEntryRecord.'

Assert-True (
    ($targetingStageText -match 'interface\s+ITargetingStageModule') -and
    ($targetingStageText -match 'TargetingRecord')
) 'ITargetingStageModule must consume TargetingRecord.'

Assert-True (
    ($previewStageText -match 'interface\s+IPreviewStageModule') -and
    ($previewStageText -match 'PreviewRecord')
) 'IPreviewStageModule must consume PreviewRecord.'

Assert-True (
    ($previewDimensionText -match 'enum\s+PreviewDimension') -and
    ($previewDimensionText -match 'RangeRing') -and
    ($previewDimensionText -match 'TargetHighlight') -and
    ($previewDimensionText -match 'FieldRadius') -and
    ($previewDimensionText -match 'MouseAttachment')
) 'PreviewDimension must define independent preview dimensions.'

Assert-True (
    ($previewDimensionPolicyText -match 'class\s+PreviewDimensionPolicy') -and
    ($previewDimensionPolicyText -match 'ApplyBaseline') -and
    ($previewDimensionPolicyText -match 'UsesVanilla')
) 'PreviewDimensionPolicy must own preview baseline and dimension reads.'

Assert-True (
    ($confirmStageText -match 'interface\s+IConfirmStageModule') -and
    ($confirmStageText -match 'ConfirmRecord')
) 'IConfirmStageModule must consume ConfirmRecord.'

Assert-True (
    ($sessionText -match 'GetManualEntryModules') -and
    ($sessionText -match 'GetTargetingModules') -and
    ($sessionText -match 'GetPreviewModules') -and
    ($sessionText -match 'GetConfirmModules')
) 'RangedAttackModuleSession must expose front-chain stage module reads.'

Assert-True (
    $gizmoResolverText -match 'ManualEntryRecord'
) 'DefaultManualEntryGizmoResolver must build a ManualEntryRecord before final command emission.'

Assert-True (
    $commandText -notmatch 'PreviewRecord|ConfirmRecord'
) 'Command_BdpManualEntryTarget must stay a thin button router and must not own preview/confirm business state.'

Assert-True (
    ($targetingSourceText -match 'TargetingRecord') -and
    ($targetingSourceText -match 'PreviewRecord') -and
    ($targetingSourceText -match 'ConfirmRecord') -and
    ($targetingSourceText -match 'ConfirmedTargetSnapshot') -and
    ($targetingSourceText -match 'PreviewDrawItem') -and
    ($targetingSourceText -match 'TargetingInputFrame') -and
    ($targetingSourceText -match 'TargetingAdvanceDecision') -and
    ($targetingSourceText -match 'ConfirmedInteractionSnapshot')
) 'AttackExecutionTargetingSource must build TargetingRecord / PreviewRecord / ConfirmRecord from the current published result and verb baseline.'

Assert-True (
    ($targetingSourceText -match 'PreviewDimensionPolicy') -and
    ($targetingSourceText -match 'PreviewDimension\.RangeRing') -and
    ($targetingSourceText -match 'PreviewDimension\.TargetHighlight') -and
    ($targetingSourceText -match 'PreviewDimension\.FieldRadius') -and
    ($targetingSourceText -match 'PreviewDimension\.MouseAttachment')
) 'AttackExecutionTargetingSource must read preview dimensions independently.'

Assert-True (
    ($targetingSourceText -notmatch 'Verb\.DrawHighlight\(target\)') -and
    ($targetingSourceText -notmatch 'UseVanillaHighlight') -and
    ($targetingSourceText -notmatch 'UseVanillaOnGui')
) 'AttackExecutionTargetingSource must not gate preview by coarse vanilla highlight toggles.'

Assert-True (
    ($targetingSourceText -match 'TryEvaluateDualWeaponTargetLegality') -and
    ($targetingSourceText -match 'EvaluateDualWeaponSideTargetLegality') -and
    ($targetingSourceText -match 'resolvedSpec\.RequiresDirectTargetLineOfSight') -and
    ($targetingSourceText -match 'allowMain \|\| allowSub')
) 'Manual dual target legality must aggregate side legality instead of trusting the composite formal host.'

Write-Output 'RangedTargetingProtocolSmokeTests PASS'
