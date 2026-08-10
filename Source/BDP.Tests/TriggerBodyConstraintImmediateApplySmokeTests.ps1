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

$bridgePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Bridge\TriggerBodyConstraintSignalBridge.cs'
$triggerBodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$triggerBodyReadsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Reads.cs'
$triggerBodyLifecyclePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$runtimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$signalHubPath = Join-Path $repoRoot 'Source\BDP\Core\BodyConstraints\PawnBodyConstraintSignalHub.cs'

$bridgeText = if (Test-Path -LiteralPath $bridgePath) { Get-Content -LiteralPath $bridgePath -Raw -Encoding utf8 } else { '' }
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8
$triggerBodyReadsText = Get-Content -LiteralPath $triggerBodyReadsPath -Raw -Encoding utf8
$triggerBodyLifecycleText = Get-Content -LiteralPath $triggerBodyLifecyclePath -Raw -Encoding utf8
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8
$signalHubText = Get-Content -LiteralPath $signalHubPath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $bridgePath) 'TriggerBodyConstraintSignalBridge must exist.'

Assert-True (
    ($bridgeText -match 'StaticConstructorOnStartup') -and
    ($bridgeText -match 'PawnBodyConstraintSignalHub\.Changed \+=') -and
    ($bridgeText -match 'TriggerSurfaceAccess\.ResolveComp') -and
    ($bridgeText -match 'ApplyBodyConstraintChangeImmediately\(')
) 'Body constraint bridge must subscribe to signal hub and apply changes immediately through trigger owner.'

Assert-True (
    ($signalHubText -match 'event Action<PawnBodyConstraintChangedArgs> Changed') -and
    ($signalHubText -match 'Changed\?\.Invoke')
) 'Signal hub must remain a fact publisher with a concrete Changed event.'

Assert-True (
    ($triggerBodyText -match 'ApplyBodyConstraintChangeImmediately\(') -and
    ($triggerBodyReadsText -match 'ForceSyncDisabledStateFromOwnerPawn\(')
) 'CompTriggerBody must expose an immediate body-constraint apply entry instead of waiting for runtime tick.'

Assert-True (
    ($triggerBodyText -match 'Notify_Equipped\(Pawn pawn\)[\s\S]*ApplyBodyConstraintChangeImmediately\(\);') -and
    ($triggerBodyLifecycleText -match 'PostSpawnSetup\(bool respawningAfterLoad\)[\s\S]*ApplyBodyConstraintChangeImmediately\(\);') -and
    ($triggerBodyLifecycleText -match 'TryFinalizePostLoadProjectionRefresh\(\)[\s\S]*ForceSyncDisabledStateFromOwnerPawn\(\);')
) 'Equip and post-load paths must restore disabled state without relying on time progression.'

Assert-True (
    ($runtimeCoordinatorText -notmatch 'SyncDisabledStateForRuntimeTick\(') -and
    ($runtimeCoordinatorText -notmatch 'ProjectionDirtyReason\.DisableStateChanged')
) 'TriggerRuntimeCoordinator must stop polling body constraints on runtime tick.'

Write-Output 'TriggerBodyConstraintImmediateApplySmokeTests PASS'
