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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$targetingRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\TargetingRecord.cs'
$previewRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\PreviewRecord.cs'
$confirmRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmRecord.cs'
$targetingInputStatePath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\TargetingInputState.cs'
$interactionSessionPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Interaction\TargetingInteractionSession.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'

$targetingRecordText = Read-Source $targetingRecordPath
$previewRecordText = Read-Source $previewRecordPath
$confirmRecordText = Read-Source $confirmRecordPath
$targetingInputStateText = Read-Source $targetingInputStatePath
$interactionSessionText = Read-Source $interactionSessionPath
$targetingSourceText = Read-Source $targetingSourcePath

Assert-True (
    ($targetingRecordText -match 'AttackContext\s+AttackContext') -and
    ($previewRecordText -match 'AttackContext\s+AttackContext') -and
    ($confirmRecordText -match 'AttackContext\s+AttackContext')
) 'TargetingRecord / PreviewRecord / ConfirmRecord must all carry the unified AttackContext.'

Assert-True (
    $targetingRecordText -notmatch 'new\s+TargetingInputState'
) 'TargetingRecord must stop owning a standalone TargetingInputState trunk.'

Assert-True (
    $targetingRecordText -notmatch 'TargetingInteractionSession\s+InteractionSession\s*\{\s*get;\s*set;'
) 'TargetingRecord must stop owning a standalone TargetingInteractionSession trunk.'

Assert-True (
    $previewRecordText -notmatch 'TargetingInteractionSession\s+InteractionSession\s*\{\s*get;\s*set;'
) 'PreviewRecord must read interaction state through AttackContext instead of a standalone field.'

Assert-True (
    $confirmRecordText -notmatch 'ConfirmedInputSnapshot' -and
    $confirmRecordText -notmatch 'ConfirmedInteractionSnapshot'
) 'ConfirmRecord must stop owning standalone confirmed snapshot payloads.'

Assert-True (
    ($targetingInputStateText -match 'IAttackContextNode') -and
    ($interactionSessionText -match 'IAttackContextNode')
) 'TargetingInputState and TargetingInteractionSession must become AttackContext nodes.'

Assert-True (
    $targetingSourceText -match 'AttackContext\s+attackContext' -and
    $targetingSourceText -notmatch 'ConfirmedInputSnapshot\s*=' -and
    $targetingSourceText -notmatch 'ConfirmedInteractionSnapshot\s*='
) 'AttackExecutionTargetingSource must flow a shared AttackContext and stop writing detached confirm snapshots into the request.'

Write-Output 'AttackContextTargetingCarrySmokeTests PASS'
