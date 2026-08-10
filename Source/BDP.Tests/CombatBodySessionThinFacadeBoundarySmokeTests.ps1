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

$servicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$activationTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionTrionBinding.cs'

$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8

Assert-True (
    Test-Path -LiteralPath $activationTransactionPath
) 'Task 5 requires CombatBodyActivationTransaction.cs.'

Assert-True (
    Test-Path -LiteralPath $exitTransactionPath
) 'Task 5 requires CombatBodyExitTransaction.cs.'

Assert-True (
    Test-Path -LiteralPath $bindingPath
) 'Task 5 requires CombatBodySessionTrionBinding.cs.'

$hostBridgePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$snapshotServicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Snapshot\CombatBodySnapshotService.cs'

Assert-True (
    Test-Path -LiteralPath $hostBridgePath
) 'Task 5 requires PawnCombatBodyBridge.cs.'

Assert-True (
    Test-Path -LiteralPath $snapshotServicePath
) 'Task 5 requires CombatBodySnapshotService.cs.'

Assert-True (
    $serviceText -notmatch 'RegisterDrain\s*\('
) 'CombatBodySessionService must not keep inline drain registration logic.'

Assert-True (
    $serviceText -notmatch 'UnregisterDrain\s*\('
) 'CombatBodySessionService must not keep inline drain unregistration logic.'

Assert-True (
    $serviceText -notmatch '\bavailableDepletedHandler\b'
) 'CombatBodySessionService must not keep drain subscription lifecycle state inline.'

Assert-True (
    $serviceText -notmatch 'DeactivateAllSlots\s*\('
) 'CombatBodySessionService must not keep trigger slot mass-deactivation logic inline.'

Assert-True (
    $serviceText -notmatch 'TryAutoActivateSpecialSlots\s*\('
) 'CombatBodySessionService must not keep activation settlement details inline.'

Assert-True (
    $serviceText -notmatch 'Capture\s*\('
) 'CombatBodySessionService 不应直接承担宿主快照捕获。'

Assert-True (
    $serviceText -notmatch 'Restore\s*\('
) 'CombatBodySessionService 不应直接承担宿主快照恢复。'

Write-Output 'CombatBodySessionThinFacadeBoundarySmokeTests PASS'

