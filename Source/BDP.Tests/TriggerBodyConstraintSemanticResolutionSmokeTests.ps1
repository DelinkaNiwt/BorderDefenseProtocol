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

$evaluatorPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyDisableEvaluator.cs'
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyPartSemanticResolver.cs'
$resultPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\TriggerBodyPartSemanticResult.cs'
$syncPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Switching\Flow\TriggerDisableSync.cs'
$addDirectPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_HediffSet_AddDirect_BodyConstraintSignal.cs'
$removeHediffPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_HealthTracker_RemoveHediff_BodyConstraintSignal.cs'
$miliraBodyPath = Join-Path $repoRoot '..\..\参考资源\模组资源\Milira_米莉拉\1.6\Defs\BodyDefs\Bodies_Milira.xml'
$milianBodyPath = Join-Path $repoRoot '..\..\参考资源\模组资源\Milira_米莉拉\1.6\Defs\BodyDefs\Bodies_Milian.xml'
$milianBodyPartDefsPath = Join-Path $repoRoot '..\..\参考资源\模组资源\Milira_米莉拉\1.6\Defs\BodyPartDefs\BodyParts_Milian.xml'

$evaluatorText = Get-Content -LiteralPath $evaluatorPath -Raw -Encoding utf8
$syncText = Get-Content -LiteralPath $syncPath -Raw -Encoding utf8
$addDirectPatchText = Get-Content -LiteralPath $addDirectPatchPath -Raw -Encoding utf8
$removeHediffPatchText = Get-Content -LiteralPath $removeHediffPatchPath -Raw -Encoding utf8
$resolverText = if (Test-Path -LiteralPath $resolverPath) { Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8 } else { '' }
$resultText = if (Test-Path -LiteralPath $resultPath) { Get-Content -LiteralPath $resultPath -Raw -Encoding utf8 } else { '' }
$miliraBodyText = if (Test-Path -LiteralPath $miliraBodyPath) { Get-Content -LiteralPath $miliraBodyPath -Raw -Encoding utf8 } else { '' }
$milianBodyText = if (Test-Path -LiteralPath $milianBodyPath) { Get-Content -LiteralPath $milianBodyPath -Raw -Encoding utf8 } else { '' }
$milianBodyPartDefsText = if (Test-Path -LiteralPath $milianBodyPartDefsPath) { Get-Content -LiteralPath $milianBodyPartDefsPath -Raw -Encoding utf8 } else { '' }
$publishMissingPartChangedCall = 'PawnBodyConstraintSignalHub.Publish(hediff.pawn, PawnBodyConstraintChangeKind.MissingPartChanged)'

Assert-True (Test-Path -LiteralPath $resolverPath) 'TriggerBodyPartSemanticResolver must exist under BodyConstraints.'

Assert-True (Test-Path -LiteralPath $resultPath) 'TriggerBodyPartSemanticResult must exist.'

Assert-True (
    ($resultText -match 'IsManipulationLimb') -and
    ($resultText -match 'ResolvedSide') -and
    ($resultText -match 'CanDisableTrigger')
) 'TriggerBodyPartSemanticResult must expose minimal semantic members.'

Assert-True (
    ($evaluatorText -match 'TriggerBodyPartSemanticResolver') -and
    ($evaluatorText -match 'EvaluateSideDisableReason')
) 'TriggerBodyDisableEvaluator must delegate semantic resolution.'

Assert-True (
    ($evaluatorText -notmatch 'partDefName\s*=') -and
    ($evaluatorText -notmatch '"Hand"') -and
    ($evaluatorText -notmatch '"Arm"') -and
    ($evaluatorText -notmatch '"Shoulder"') -and
    ($evaluatorText -notmatch 'LabelShort') -and
    ($evaluatorText -notmatch 'customLabel') -and
    ($evaluatorText -notmatch 'ToLowerInvariant')
) 'Evaluator must not rely on defName or label text guessing.'

Assert-True (
    ($resolverText -match 'BodyPartTagDefOf\.ManipulationLimbCore') -and
    ($resolverText -match 'BodyPartTagDefOf\.ManipulationLimbSegment') -and
    ($resolverText -match 'BodyPartTagDefOf\.ManipulationLimbDigit')
) 'Resolver must use manipulation limb tags.'

Assert-True (
    ($resolverText -match 'BodyPartGroupDefOf\.LeftHand') -and
    ($resolverText -match 'BodyPartGroupDefOf\.RightHand')
) 'Resolver must resolve side from stable hand body groups.'

Assert-True (
    ($miliraBodyText -match '<def>Hand</def>') -and
    ($miliraBodyText -match '<customLabel>left hand</customLabel>') -and
    ($miliraBodyText -match '<customLabel>right hand</customLabel>') -and
    ($miliraBodyText -match '<li>LeftHand</li>') -and
    ($miliraBodyText -match '<li>RightHand</li>')
) 'Milira sample must confirm side information lives in hand descendants and custom labels.'

Assert-True (
    ($milianBodyText -match '<def>Milian_Shoulder</def>') -and
    ($milianBodyText -match '<def>Milian_Arm</def>') -and
    ($milianBodyText -match '<def>Milian_Hand</def>') -and
    ($milianBodyText -match '<li>LeftHand</li>') -and
    ($milianBodyText -match '<li>RightHand</li>')
) 'Milian sample must confirm custom body-part defs keep stable left and right hand groups.'

Assert-True (
    ($milianBodyPartDefsText -match 'ManipulationLimbCore') -and
    ($milianBodyPartDefsText -match 'ManipulationLimbSegment') -and
    ($milianBodyPartDefsText -match 'ManipulationLimbDigit')
) 'Milian sample must confirm custom body-part defs declare manipulation limb tags.'

Assert-True (
    ($resolverText -match 'woundAnchorTag') -and
    ($resolverText -notmatch 'LabelShort') -and
    ($resolverText -notmatch 'customLabel') -and
    ($resolverText -notmatch '\.Label') -and
    ($resolverText -notmatch 'Milira') -and
    ($resolverText -notmatch 'Milian') -and
    ($resolverText -notmatch 'Race')
) 'Resolver may use structural anchors but must avoid UI labels and race branches.'

Assert-True (
    ($syncText -match 'TriggerBodyDisableEvaluator\.EvaluateSideDisableReason') -and
    ($syncText -notmatch 'TriggerBodyPartSemanticResolver') -and
    ($syncText -notmatch 'BodyPartRecord') -and
    ($syncText -notmatch 'Hediff_MissingPart')
) 'TriggerDisableSync must consume evaluator results only.'

Assert-True (
    ($addDirectPatchText.Contains($publishMissingPartChangedCall)) -and
    ($addDirectPatchText -notmatch 'TriggerDisableSync') -and
    ($addDirectPatchText -notmatch 'TriggerBodyPartSemanticResolver')
) 'AddDirect patch must only publish missing-part facts.'

Assert-True (
    ($removeHediffPatchText.Contains($publishMissingPartChangedCall)) -and
    ($removeHediffPatchText -notmatch 'TriggerDisableSync') -and
    ($removeHediffPatchText -notmatch 'TriggerBodyPartSemanticResolver')
) 'RemoveHediff patch must only publish missing-part facts.'

Write-Output 'TriggerBodyConstraintSemanticResolutionSmokeTests PASS'


