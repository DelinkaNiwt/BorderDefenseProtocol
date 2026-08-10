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

$resolverPath = Join-Path $bdpSourceRoot 'Expressions\Projection\DefaultManualEntryGizmoResolver.cs'
$surfaceAccessPath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionSurfaceAccess.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$commandPath = Join-Path $bdpSourceRoot 'Expressions\Projection\Command_BdpManualEntryTarget.cs'
$sessionPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Runtime\RangedAttackModuleSession.cs'
$interactionSessionPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInteractionSession.cs'
$inputFramePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInputFrame.cs'
$advanceDecisionPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingAdvanceDecision.cs'
$advanceKindPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingAdvanceKind.cs'

$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$surfaceAccessText = Get-Content -LiteralPath $surfaceAccessPath -Raw -Encoding utf8
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8
$commandText = Get-Content -LiteralPath $commandPath -Raw -Encoding utf8
$sessionText = Get-Content -LiteralPath $sessionPath -Raw -Encoding utf8
$interactionSessionText = if (Test-Path -LiteralPath $interactionSessionPath) { Get-Content -LiteralPath $interactionSessionPath -Raw -Encoding utf8 } else { '' }
$inputFrameText = if (Test-Path -LiteralPath $inputFramePath) { Get-Content -LiteralPath $inputFramePath -Raw -Encoding utf8 } else { '' }
$advanceDecisionText = if (Test-Path -LiteralPath $advanceDecisionPath) { Get-Content -LiteralPath $advanceDecisionPath -Raw -Encoding utf8 } else { '' }
$advanceKindText = if (Test-Path -LiteralPath $advanceKindPath) { Get-Content -LiteralPath $advanceKindPath -Raw -Encoding utf8 } else { '' }

Assert-True (
    ($resolverText -match 'CreateSession') -and
    ($surfaceAccessText -match 'CreateTargetingSource') -and
    ($surfaceAccessText -match 'RangedAttackModuleSession')
) 'Manual entry must create a ranged module session and pass it into the targeting source.'

Assert-True (
    ($targetingSourceText -match 'private\s+readonly\s+RangedAttackModuleSession\s+moduleSession') -and
    ($targetingSourceText -notmatch 'ResolveCurrentContext\(\)[\s\S]*CreateSession')
) 'AttackExecutionTargetingSource must hold one stable module session and must not rebuild it in ResolveCurrentContext().'

Assert-True (
    ($commandText -match 'Find\.Targeter\.BeginTargeting') -and
    ($commandText -notmatch 'CreateSession')
) 'Command_BdpManualEntryTarget must stay a thin router and must not create module sessions itself.'

Assert-True (
    (Test-Path -LiteralPath $interactionSessionPath) -and
    (Test-Path -LiteralPath $inputFramePath) -and
    (Test-Path -LiteralPath $advanceDecisionPath) -and
    (Test-Path -LiteralPath $advanceKindPath)
) 'Targeting interaction protocol must expose session, input frame, advance decision, and advance kind objects.'

Assert-True (
    ($interactionSessionText -match 'class\s+TargetingInteractionSession') -and
    ($interactionSessionText -match 'StepIndex') -and
    ($interactionSessionText -match 'IsCompleted')
) 'TargetingInteractionSession must carry neutral interaction session facts.'

Assert-True (
    ($inputFrameText -match 'class\s+TargetingInputFrame') -and
    ($inputFrameText -match 'HoveredTarget') -and
    ($inputFrameText -match 'SelectedTarget')
) 'TargetingInputFrame must carry neutral per-frame targeting input facts.'

Assert-True (
    ($advanceDecisionText -match 'class\s+TargetingAdvanceDecision') -and
    ($advanceDecisionText -match 'TargetingAdvanceKind')
) 'TargetingAdvanceDecision must exist as a first-class targeting interaction result.'

Assert-True (
    ($advanceKindText -match 'enum\s+TargetingAdvanceKind') -and
    ($advanceKindText -match 'Continue') -and
    ($advanceKindText -match 'Complete') -and
    ($advanceKindText -match 'Cancel') -and
    ($advanceKindText -match 'Reject')
) 'TargetingAdvanceKind must define the neutral interaction progression states.'

Assert-True (
    ($sessionText -match 'TargetingInteractionSession') -and
    ($targetingSourceText -match 'TargetingInteractionSession') -and
    ($targetingSourceText -match 'TargetingInputFrame') -and
    ($targetingSourceText -match 'TargetingAdvanceDecision')
) 'RangedAttackModuleSession and AttackExecutionTargetingSource must carry one stable targeting interaction session and its per-frame progression objects.'

Write-Output 'RangedTargetingSessionContinuitySmokeTests PASS'
