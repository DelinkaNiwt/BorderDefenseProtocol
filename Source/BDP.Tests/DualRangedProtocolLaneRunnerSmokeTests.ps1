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
    $protocolText -match 'private bool TryBuildFromEntry'
) 'RangedAttackProtocolService must extract the single-entry protocol runner.'

Assert-True (
    $protocolText -match 'private bool TryBuildDualSourceLaneProtocol'
) 'RangedAttackProtocolService must add a dedicated dual lane runner.'

Assert-True (
    $protocolText -match 'if \(ShouldUseDualSourceLaneIsolation\(entry\)\)'
) 'RangedAttackProtocolService.TryBuild must route eligible dual entries into the lane-isolation path.'

Assert-True (
    $protocolText -match 'bool laneSucceeded = TryBuildFromEntry\(request,\s*lane\.SourceResult,\s*laneEntry,\s*out RangedAttackProtocolResult laneProtocolResult\)'
) 'Dual lane runner must execute each lane through the existing single-entry protocol path.'

Assert-True (
    ($protocolText -match 'if \(successfulLaneProtocols\.Count == 0\)') -and
    ($protocolText -match 'protocolResult = firstFailedProtocol')
) 'Dual lane runner must surface the first failed protocol result when no lane succeeds.'

Write-Output 'DualRangedProtocolLaneRunnerSmokeTests PASS'
