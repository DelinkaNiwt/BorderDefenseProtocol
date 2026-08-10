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
$protocolPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\RangedAttackProtocolService.cs'
$protocolText = Get-Content -LiteralPath $protocolPath -Raw -Encoding utf8

Assert-True (
    $protocolText -match 'private sealed class DualSourceLane'
) 'RangedAttackProtocolService must declare the dual source-lane helper type.'

Assert-True (
    $protocolText -match 'private static List<DualSourceLane> CollectDualSourceLanes'
) 'RangedAttackProtocolService must collect dual source lanes before protocol stage execution.'

Assert-True (
    $protocolText -match 'private static RangedAttackEntry BuildDualSourceLaneEntry'
) 'RangedAttackProtocolService must build a single-source ranged entry for each dual lane.'

Assert-True (
    ($protocolText -match 'SessionResultId = lane\.SourceResult\.Id') -and
    ($protocolText -match 'SourceResultId = lane\.SourceResult\.Id') -and
    ($protocolText -match 'SessionResult = lane\.SourceResult') -and
    ($protocolText -match 'SourceResult = lane\.SourceResult')
) 'Dual lane entry must bind both session/source truth back to the lane source result.'

Assert-True (
    ($protocolText -match 'ExecutionStyle = lane\.SourceResult\.ExecutionStyle') -and
    ($protocolText -match 'SemanticContext = lane\.SourceResult\.SemanticContext')
) 'Dual lane entry must inherit execution style and semantic context from its own source result.'

Assert-True (
    ($protocolText -match 'HostResultId = lane\.SourceResult\.Id') -and
    ($protocolText -match 'AttackContext = AttackContext\.FromSnapshot')
) 'Dual lane runtime step must host on the lane source result and rebuild AttackContext from snapshot.'

Write-Output 'DualRangedProtocolSourceLaneIsolationSmokeTests PASS'
