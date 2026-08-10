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

$continuationPath = Join-Path $bdpSourceRoot 'Verbs\RangedVerbContinuationPlanner.cs'
$continuationText = Read-Source $continuationPath

Assert-True (
    ($continuationText -match 'TryCreateSnapshotBackedModuleSession') -and
    ($continuationText -match 'host_snapshot_over_staged') -and
    ($continuationText -match 'published_result_with_host_snapshot') -and
    ($continuationText -match 'TryApplyHostAttackContextSnapshot')
) 'Ranged continuation must be able to rebuild a module session from the host snapshot before trusting a staged entry session.'

$snapshotHelperIndex = $continuationText.IndexOf('TryCreateSnapshotBackedModuleSession')
$stagedIndex = $continuationText.IndexOf('source = "staged_entry"')
Assert-True (
    ($snapshotHelperIndex -ge 0) -and
    ($stagedIndex -ge 0) -and
    ($snapshotHelperIndex -lt $stagedIndex)
) 'Host snapshot restoration must be attempted before staged_entry fallback, otherwise an empty staged session can erase path-latch state.'

Write-Output 'RangedContinuationHostSnapshotPrioritySmokeTests PASS'
