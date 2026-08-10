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

$keysPath = Join-Path $bdpSourceRoot 'AttackExecution\Context\AttackContextKeys.cs'
$confirmRecordPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmRecord.cs'
$targetingSourcePath = Join-Path $bdpSourceRoot 'AttackExecution\AttackExecutionTargetingSource.cs'
$confirmedTargetPath = Join-Path $bdpSourceRoot 'AttackExecution\TargetingProtocol\Model\ConfirmedTargetSnapshot.cs'

$keysText = Read-Source $keysPath
$confirmRecordText = Read-Source $confirmRecordPath
$targetingSourceText = Read-Source $targetingSourcePath
$confirmedTargetExists = Test-Path $confirmedTargetPath
$confirmedTargetText = if ($confirmedTargetExists) { Read-Source $confirmedTargetPath } else { '' }

Assert-True (
    $keysText -match 'ConfirmedTarget\s*=\s*"confirmed\.target"'
) 'AttackContextKeys must declare a confirmed.target node key.'

Assert-True (
    $confirmedTargetExists -and
    ($confirmedTargetText -match 'public sealed class ConfirmedTargetSnapshot') -and
    ($confirmedTargetText -match 'public LocalTargetInfo NavigationTarget') -and
    ($confirmedTargetText -match 'public LocalTargetInfo SemanticTarget')
) 'ConfirmedTargetSnapshot must expose NavigationTarget and SemanticTarget.'

Assert-True (
    $confirmRecordText -match 'public LocalTargetInfo SemanticTarget'
) 'ConfirmRecord must expose a SemanticTarget handoff slot.'

Assert-True (
    $targetingSourceText -match 'attackContext\.Set\(AttackContextKeys\.ConfirmedTarget'
) 'AttackExecutionTargetingSource must freeze a ConfirmedTargetSnapshot into AttackContext.'

Assert-True (
    $targetingSourceText -match 'NavigationTarget\s*=\s*confirmRecord\s*!=\s*null\s*\?\s*confirmRecord\.Target\s*:\s*LocalTargetInfo\.Invalid'
) 'Confirmed target snapshot must preserve confirmRecord.Target as navigation target.'

Assert-True (
    $targetingSourceText -match 'SemanticTarget\s*=\s*confirmRecord\s*!=\s*null\s*&&\s*confirmRecord\.SemanticTarget\.IsValid\s*\?\s*confirmRecord\.SemanticTarget\s*:'
) 'Confirmed target snapshot must prefer confirmRecord.SemanticTarget when present.'

Write-Output 'DualRangedConfirmedTargetSnapshotSmokeTests PASS'
