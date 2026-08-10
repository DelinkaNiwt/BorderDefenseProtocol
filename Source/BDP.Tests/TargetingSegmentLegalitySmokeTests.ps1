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

$requestPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Targeting\TargetingSegmentLegalityRequest.cs'
$resultPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Targeting\TargetingSegmentLegalityResult.cs'
$servicePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Targeting\ITargetingSegmentLegalityService.cs'
$defaultServicePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Targeting\DefaultTargetingSegmentLegalityService.cs'
$targetingRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\TargetingRecord.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'

$requestText = if (Test-Path -LiteralPath $requestPath) { Get-Content -LiteralPath $requestPath -Raw -Encoding utf8 } else { '' }
$resultText = if (Test-Path -LiteralPath $resultPath) { Get-Content -LiteralPath $resultPath -Raw -Encoding utf8 } else { '' }
$serviceText = if (Test-Path -LiteralPath $servicePath) { Get-Content -LiteralPath $servicePath -Raw -Encoding utf8 } else { '' }
$defaultServiceText = if (Test-Path -LiteralPath $defaultServicePath) { Get-Content -LiteralPath $defaultServicePath -Raw -Encoding utf8 } else { '' }
$targetingRecordText = if (Test-Path -LiteralPath $targetingRecordPath) { Get-Content -LiteralPath $targetingRecordPath -Raw -Encoding utf8 } else { '' }
$targetingSourceText = if (Test-Path -LiteralPath $targetingSourcePath) { Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8 } else { '' }

Assert-True (Test-Path -LiteralPath $requestPath) 'TargetingSegmentLegalityRequest.cs must exist.'
Assert-True (Test-Path -LiteralPath $resultPath) 'TargetingSegmentLegalityResult.cs must exist.'
Assert-True (Test-Path -LiteralPath $servicePath) 'ITargetingSegmentLegalityService.cs must exist.'
Assert-True (Test-Path -LiteralPath $defaultServicePath) 'DefaultTargetingSegmentLegalityService.cs must exist.'

Assert-True (
    ($requestText -match 'class\s+TargetingSegmentLegalityRequest') -and
    ($requestText -match 'IntVec3\s+OriginCell') -and
    ($requestText -match 'LocalTargetInfo\s+CandidateTarget') -and
    ($requestText -match 'bool\s+RequireHittableNow') -and
    ($requestText -match 'FromRecord')
) 'TargetingSegmentLegalityRequest must carry neutral origin-to-candidate facts and build from TargetingRecord.'

Assert-True (
    ($requestText -notmatch 'Viper|Anchor|Path')
) 'TargetingSegmentLegalityRequest must not carry Viper, anchor, or path business terms.'

Assert-True (
    ($resultText -match 'class\s+TargetingSegmentLegalityResult') -and
    ($resultText -match 'bool\s+IsLegal') -and
    ($resultText -match 'string\s+RejectReason')
) 'TargetingSegmentLegalityResult must expose only legal state and rejection reason.'

Assert-True (
    ($serviceText -match 'interface\s+ITargetingSegmentLegalityService') -and
    ($serviceText -match 'TargetingSegmentLegalityResult\s+Evaluate\s*\(')
) 'ITargetingSegmentLegalityService must expose one neutral Evaluate entry.'

Assert-True (
    ($defaultServiceText -match 'class\s+DefaultTargetingSegmentLegalityService') -and
    ($defaultServiceText -match 'ITargetingSegmentLegalityService') -and
    ($defaultServiceText -match 'CanHitTargetFrom') -and
    ($defaultServiceText -match 'TargetingParameters') -and
    ($defaultServiceText -notmatch 'Viper|Anchor|Path')
) 'DefaultTargetingSegmentLegalityService must reuse current Verb and targeting parameters without business-specific rules.'

Assert-True (
    ($targetingRecordText -match 'ITargetingSegmentLegalityService\s+SegmentLegality')
) 'TargetingRecord must expose the neutral segment legality query surface to targeting modules.'

Assert-True (
    ($targetingRecordText -match 'HasCurrentTargetLegalityOverride') -and
    ($targetingRecordText -match 'current-frame candidate')
) 'TargetingRecord must lock current-candidate legality override to the current frame only.'

Assert-True (
    ($targetingSourceText -match 'TryEvaluateCurrentTargetLegality') -and
    ($targetingSourceText -match 'current-candidate probe')
) 'AttackExecutionTargetingSource must keep TryEvaluateCurrentTargetLegality as a current-candidate probe only.'

Assert-True (
    ($defaultServiceText -match 'segment-only verdict') -and
    ($defaultServiceText -notmatch 'Confirm|FinalConfirm|AttackAllowed')
) 'DefaultTargetingSegmentLegalityService must stay inside segment-legality responsibility only.'

Write-Output 'TargetingSegmentLegalitySmokeTests PASS'
