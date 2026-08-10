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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP'

# AttackExecution request contract.
$attackExecutionRequestPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionRequest.cs'

# AttackExecution entry contract.
$attackExecutionEntryPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\DefaultAttackExecutionEntry.cs'

# Deleted legacy resolved-request contract.
$resolvedRequestPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionResolvedRequest.cs'
$runtimeStorePath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionPlanRuntimeStore.cs'

# Targeting bridge contract.
$targetingSourcePath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionTargetingSource.cs'

# Auto attack bridge contract.
$attackSurfacePath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionSurfaceAccess.cs'

# Trigger published projection owner contract.
$triggerBodyPath = Join-Path $bdpSourceRoot 'Core\Trigger\State\CompTriggerBody.cs'
$runtimeCoordinatorPath = Join-Path $bdpSourceRoot 'Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'

# Projection invalidation guard contracts.
$postLoadRecoveryPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\AttackExecutionPostLoadRecovery.cs'
$rangedJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'
$meleeJobPath = Join-Path $bdpSourceRoot 'Core\AttackExecution\JobDriver_BdpMeleeAttackExecution.cs'
$shootVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_Shoot.cs'
$continuationPlannerPath = Join-Path $bdpSourceRoot 'Core\Verbs\RangedVerbContinuationPlanner.cs'
$meleeVerbPath = Join-Path $bdpSourceRoot 'Core\Verbs\BdpVerb_MeleeAttackDamage.cs'

$attackExecutionRequestText = Get-Content -LiteralPath $attackExecutionRequestPath -Raw -Encoding utf8
$attackExecutionEntryText = Get-Content -LiteralPath $attackExecutionEntryPath -Raw -Encoding utf8
$targetingSourceText = Get-Content -LiteralPath $targetingSourcePath -Raw -Encoding utf8
$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8
$postLoadRecoveryText = Get-Content -LiteralPath $postLoadRecoveryPath -Raw -Encoding utf8
$rangedJobText = Get-Content -LiteralPath $rangedJobPath -Raw -Encoding utf8
$meleeJobText = Get-Content -LiteralPath $meleeJobPath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$continuationPlannerText = Get-Content -LiteralPath $continuationPlannerPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8

Assert-True (
    $attackExecutionRequestText -match 'AttackSessionToken\s+SessionToken\s*\{'
) 'AttackExecutionRequest must carry a single AttackSessionToken.'

Assert-True (
    $attackExecutionRequestText -notmatch 'int\s+ProjectionVersion\s*\{\s*get\s*;\s*set\s*;'
) 'AttackExecutionRequest must stop storing standalone settable ProjectionVersion state.'

Assert-True (
    -not (Test-Path -LiteralPath $resolvedRequestPath)
) 'Task 5 must delete the legacy AttackExecutionResolvedRequest type entirely.'

Assert-True (
    -not (Test-Path -LiteralPath $runtimeStorePath)
) 'Task 1 must delete AttackExecutionPlanRuntimeStore because published projection identity is now the only runtime source of truth.'

Assert-True (
    $triggerBodyText -match 'TriggerCombatProjectionState\s+PublishedCombatProjection'
) 'CompTriggerBody must expose the published combat projection for attack execution consumers.'

Assert-True (
    $attackExecutionEntryText -notmatch 'TryGetSelectedResult\s*\('
) 'Attack execution entry must not resolve by calling ExpressionService.TryGetSelectedResult anymore.'

Assert-True (
    $attackExecutionEntryText -notmatch 'SnapshotAttackExecutionResolver'
) 'Attack execution entry must delete the snapshot re-resolve helper.'

Assert-True (
    ($attackExecutionEntryText -match 'PublishedCombatProjection') -and
    ($attackExecutionEntryText -match 'SessionToken')
) 'Attack execution entry must read from the published combat projection and validate SessionToken.'

Assert-True (
    ($targetingSourceText -notmatch 'TryGetSelectedResult\s*\(') -and
    ($targetingSourceText -notmatch 'BuildSelectedSnapshot\s*\(')
) 'AttackExecutionTargetingSource must not trigger expression re-resolve on demand.'

Assert-True (
    (($targetingSourceText -match 'PublishedCombatProjection') -or
     ($targetingSourceText -match 'TryGetPublishedResult')) -and
    ($targetingSourceText -match 'SessionToken')
) 'AttackExecutionTargetingSource must read from the published combat projection and create an AttackSessionToken.'

Assert-True (
    ($targetingSourceText -notmatch 'cachedContext') -and
    ($targetingSourceText -notmatch 'cachedProjectionVersion') -and
    ($targetingSourceText -notmatch 'GetOrRefreshResolvedContext\s*\(')
) 'AttackExecutionTargetingSource must delete the leftover per-session targeting cache and resolve directly from the published projection each read.'

Assert-True (
    $attackSurfaceText -notmatch 'BuildSelectedSnapshot\s*\('
) 'AttackExecutionSurfaceAccess auto attack bridge must not rebuild expression snapshots.'

Assert-True (
    ($attackSurfaceText -match 'PublishedCombatProjection') -and
    ($attackSurfaceText -match 'SessionToken')
) 'AttackExecutionSurfaceAccess auto attack bridge must read from the published combat projection and seed an AttackSessionToken.'

Assert-True (
    $attackSurfaceText -match 'HostSessionToken'
) 'Auto-ranged bridge must seed the selected published session token into the formal host verb before vanilla auto attack starts.'

Assert-True (
    $postLoadRecoveryText -match 'IsCurrentAttackSessionValid'
) 'AttackExecution post-load/session recovery helper must expose a dedicated current-session validity guard for projection invalidation handling.'

Assert-True (
    ($postLoadRecoveryText -match 'HostSessionToken') -and
    ($postLoadRecoveryText -notmatch '\bHostProjectionVersion\b')
) 'AttackExecution post-load/session recovery must validate the host session token instead of detached projection fields.'

Assert-True (
    ($rangedJobText -match 'IsCurrentAttackSessionValid') -and
    ($meleeJobText -match 'IsCurrentAttackSessionValid')
) 'BDP continuous attack jobs must guard each tick against projection-invalidated sessions instead of silently pushing stale jobs forward.'

Assert-True (
    ($shootVerbText -match 'PrepareContinuation\(') -and
    ($continuationPlannerText -match 'IsCurrentAttackSessionValid\(verb\)') -and
    ($continuationPlannerText -match 'HostSessionToken')
) 'BdpVerb_Shoot must refuse to continue a stale formal-host session through the shared continuation planner after projection invalidation.'

Assert-True (
    ($meleeVerbText -match 'HostSessionToken') -and
    ($meleeVerbText -notmatch '\bHostProjectionVersion\b') -and
    ($meleeVerbText -match 'ExposeData') -and
    ($meleeVerbText -match 'ApplyExecutionContext')
) 'BDP melee formal-host execution must also persist and apply host session token identity for invalidation checks.'

Assert-True (
    ($runtimeCoordinatorText -match 'AttackExecutionPostLoadRecovery\.InterruptInvalidAttackSession') -and
    ($triggerBodyText -notmatch 'InterruptInvalidAttackSession')
) 'Projection publication entry points must actively interrupt invalidated BDP attack sessions from the unified publish boundary.'

Write-Output 'AttackExecutionProjectionVersionSmokeTests PASS'
