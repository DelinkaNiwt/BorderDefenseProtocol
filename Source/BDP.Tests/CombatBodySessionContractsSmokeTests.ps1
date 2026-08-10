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

$exitModePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionExitMode.cs'
$policyPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionPolicy.cs'
$servicePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionService.cs'
$activationTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyActivationTransaction.cs'
$exitTransactionPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodyExitTransaction.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionTrionBinding.cs'
$runtimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$hostPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompCombatBodyHost.cs'
$surfacePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Access\Surfaces\CombatBodySurfaceAccess.cs'

Assert-True (
    Test-Path -LiteralPath $exitModePath
) 'Task 1 requires CombatBodySessionExitMode.cs.'

Assert-True (
    Test-Path -LiteralPath $policyPath
) 'Task 1 requires CombatBodySessionPolicy.cs.'

Assert-True (
    Test-Path -LiteralPath $servicePath
) 'Task 1 requires CombatBodySessionService.cs.'

Assert-True (
    (Test-Path -LiteralPath $activationTransactionPath) -and
    (Test-Path -LiteralPath $exitTransactionPath) -and
    (Test-Path -LiteralPath $bindingPath)
) 'Task 4 requires activation, exit, and Trion binding collaborators.'

$hostText = Get-Content -LiteralPath $hostPath -Raw -Encoding utf8
$surfaceText = Get-Content -LiteralPath $surfacePath -Raw -Encoding utf8
$policyText = Get-Content -LiteralPath $policyPath -Raw -Encoding utf8
$serviceText = Get-Content -LiteralPath $servicePath -Raw -Encoding utf8
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8

Assert-True (
    $hostText -match 'private CombatBodyService rawCombatBodyService;'
) 'CompCombatBodyHost must hold a rawCombatBodyService field.'

Assert-True (
    $hostText -match 'private CombatBodySessionService combatBodySessionService;'
) 'CompCombatBodyHost must hold a combatBodySessionService field.'

Assert-True (
    $hostText -match 'internal CombatBodySessionService Service'
) 'CompCombatBodyHost.Service must return CombatBodySessionService.'

Assert-True (
    $hostText -match 'internal CombatBodyService RawService'
) 'CompCombatBodyHost must expose an internal RawService for CombatBodySessionService.'

Assert-True (
    $surfaceText -match 'return comp != null \? comp\.Service : null;'
) 'CombatBodySurfaceAccess must route CombatBody surfaces through CompCombatBodyHost.Service.'

Assert-True (
    $serviceText -match 'ICombatBodyReader' -and
    $serviceText -match 'ICombatBodyCommands' -and
    $serviceText -match 'ICombatBodyEvents'
) 'CombatBodySessionService must implement CombatBody reader, command, and event contracts.'

Assert-True (
    ($serviceText -match 'CombatBodyActivationTransaction') -and
    ($serviceText -match 'CombatBodyExitTransaction') -and
    ($serviceText -match 'CombatBodySessionTrionBinding')
) 'CombatBodySessionService must compose dedicated activation, exit, and Trion binding collaborators.'

Assert-True (
    ($serviceText -match 'public bool TryActivate\(\)[\s\S]*if \(!CanActivate\(\)\)[\s\S]*bool activated = activationTransaction\.TryActivate\(OwnerPawn\);') -and
    ($serviceText -match 'if \(activated\)[\s\S]*BeginManualTransformLock\(\);[\s\S]*return activated;')
) 'CombatBodySessionService.TryActivate() must guard admission, delegate activation order, and lock only after success.'

Assert-True (
    ($serviceText -match 'private void ExecuteExit\(CombatBodySessionExitMode exitMode\)') -and
    ($serviceText -match 'if \(isExitInProgress\)[\s\S]*return;') -and
    ($serviceText -match 'try[\s\S]*exitTransaction\.Execute\(OwnerPawn, exitMode\);[\s\S]*finally[\s\S]*isExitInProgress = false;')
) 'CombatBodySessionService exit orchestration must guard reentry and delegate to CombatBodyExitTransaction.'

Assert-True (
    $serviceText -match 'internal void RestoreAfterLoad\(\)\s*\{\s*trionBinding\.RestoreAfterLoad\(\);'
) 'CombatBodySessionService post-load recovery must delegate runtime binding restoration to CombatBodySessionTrionBinding.'

Assert-True (
    $policyText -match 'ShouldPublishCombatProjection'
) 'CombatBodySessionPolicy must keep the combat projection publication gating contract.'

Assert-True (
    ($runtimeCoordinatorText -match 'CombatBodySessionPolicy') -and
    ($runtimeCoordinatorText -match 'ShouldPublishCombatProjection\(ownerPawn, owner\)')
) 'TriggerRuntimeCoordinator must continue deferring combat projection publication to CombatBodySessionPolicy.'

Write-Output 'CombatBodySessionContracts PASS'
