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

$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$snapshotServicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotService.cs'

Assert-True -Condition (Test-Path -LiteralPath $bridgePath) -Message 'PawnCombatBodyBridge.cs must exist.'
Assert-True -Condition (Test-Path -LiteralPath $snapshotServicePath) -Message 'CombatBodySnapshotService.cs must exist.'

$bridgeText = Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8
$snapshotServiceText = Get-Content -LiteralPath $snapshotServicePath -Raw -Encoding utf8

Assert-True -Condition (
    $snapshotServiceText -match 'RestoreHediffs\(pawn,\s*hostState\.SnapshotState\)[\s\S]*hostState\.SnapshotState\.ClearRecordedStates\(\);'
) -Message 'CombatBodySnapshotService.Restore() must still clear recorded snapshot state after restoring hediffs, so the next activation captures a fresh baseline.'

Assert-True -Condition (
    $bridgeText -match 'RestoreFromCombatBody\(\)[\s\S]*List<CombatBodySnapshotHediffRecord>\s+restoredHediffBaseline\s*=\s*CopyHediffBaselineForFinalCleanup\(\);[\s\S]*snapshotService\?\.Restore\(Pawn,\s*hostState\)[\s\S]*FinalCleanupResidualHediffs\(restoredHediffBaseline\);'
) -Message 'PawnCombatBodyBridge.RestoreFromCombatBody() must copy the pre-restore hediff baseline before snapshotService.Restore() clears it, then pass that copy to final cleanup.'

Assert-True -Condition (
    $bridgeText -match 'private\s+List<CombatBodySnapshotHediffRecord>\s+CopyHediffBaselineForFinalCleanup\(\)[\s\S]*new\s+List<CombatBodySnapshotHediffRecord>\(hostState\.SnapshotState\.HediffSnapshots\)'
) -Message 'PawnCombatBodyBridge must keep a per-exit copy of HediffSnapshots for final cleanup only.'

Assert-True -Condition (
    $bridgeText -match 'private\s+void\s+FinalCleanupResidualHediffs\(IReadOnlyList<CombatBodySnapshotHediffRecord>\s+restoredHediffBaseline\)[\s\S]*IsHediffInSnapshotBaseline\(hediff,\s*restoredHediffBaseline\)'
) -Message 'Final cleanup must judge restored old hediffs from the per-exit baseline copy.'

Assert-True -Condition (
    $bridgeText -match 'private\s+bool\s+IsHediffInSnapshotBaseline\(Hediff\s+hediff,\s*IReadOnlyList<CombatBodySnapshotHediffRecord>\s+baselineRecords\)'
) -Message 'IsHediffInSnapshotBaseline() must take an explicit baseline list instead of reading the mutable snapshot state.'

$baselineMethodMatch = [regex]::Match(
    $bridgeText,
    'private\s+bool\s+IsHediffInSnapshotBaseline\(Hediff\s+hediff,\s*IReadOnlyList<CombatBodySnapshotHediffRecord>\s+baselineRecords\)[\s\S]*?\n        \}'
)
Assert-True -Condition $baselineMethodMatch.Success -Message 'IsHediffInSnapshotBaseline() body must be findable.'
Assert-True -Condition (
    $baselineMethodMatch.Value -notmatch 'hostState\.SnapshotState\.HediffSnapshots'
) -Message 'IsHediffInSnapshotBaseline() must not read hostState.SnapshotState.HediffSnapshots after Restore() has cleared it.'

Write-Output 'CombatBodySnapshotRestoreBaselineSmokeTests PASS'
