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

$recoveryPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionPostLoadRecovery.cs'
$pawnPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_ExposeData_PostLoadAttackRecovery.cs'
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$rangedJobPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\JobDriver_BdpRangedAttackExecution.cs'
$lifecyclePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$runtimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$slotStatePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\TriggerSlotState.cs'
$equipmentTickPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$bodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$weaponVisualStageResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\WeaponVisualStageResolver.cs'

Assert-True (
    Test-Path -LiteralPath $recoveryPath
) 'BDP must define an explicit post-load attack-session recovery helper.'

Assert-True (
    Test-Path -LiteralPath $pawnPatchPath
) 'BDP must hook Pawn.ExposeData for post-load attack-session recovery.'

Assert-True (
    Test-Path -LiteralPath $weaponVisualStageResolverPath
) 'Weapon visual stage recovery must remain a read-only view over restored combat truth.'

$recoveryText = Get-Content -LiteralPath $recoveryPath -Raw -Encoding utf8
$pawnPatchText = Get-Content -LiteralPath $pawnPatchPath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$rangedJobText = Get-Content -LiteralPath $rangedJobPath -Raw -Encoding utf8
$lifecycleText = Get-Content -LiteralPath $lifecyclePath -Raw -Encoding utf8
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8
$slotStateText = Get-Content -LiteralPath $slotStatePath -Raw -Encoding utf8
$equipmentTickPatchText = Get-Content -LiteralPath $equipmentTickPatchPath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$bodyText = Get-Content -LiteralPath $bodyPath -Raw -Encoding utf8
$weaponVisualStageResolverText = Get-Content -LiteralPath $weaponVisualStageResolverPath -Raw -Encoding utf8

Assert-True (
    $pawnPatchText -match 'HarmonyPatch\(typeof\(Pawn\), nameof\(Pawn\.ExposeData\)\)'
) 'Post-load attack-session recovery must hook Pawn.ExposeData.'

Assert-True (
    $pawnPatchText -notmatch 'LoadSaveMode\.Saving'
) 'Pawn.ExposeData post-load recovery patch must stop using save-time projection.'

Assert-True (
    $pawnPatchText -match 'LoadSaveMode\.PostLoadInit'
) 'Post-load attack-session recovery patch must run only during PostLoadInit.'

Assert-True (
    $pawnPatchText -match 'AttackExecutionPostLoadRecovery'
) 'Pawn.ExposeData recovery patch must delegate to the dedicated post-load helper.'

Assert-True (
    $recoveryText -notmatch 'TryPreparePlan\('
) 'Post-load recovery must not rebuild attack plans during load.'

Assert-True (
    $recoveryText -notmatch 'TryExecute\('
) 'Post-load recovery must not re-enter the execution pipeline during load.'

Assert-True (
    $recoveryText -match 'HostSessionToken'
) 'Post-load recovery must validate persisted formal-host session token before deciding whether to terminate a loaded session.'

Assert-True (
    $recoveryText -match 'pendingWindowIndex|pendingWindowProjectilePlanIndex|CanResumeLoadedSession'
) 'Post-load recovery must act as a safety net for invalid loaded cursor state rather than cancel every session blindly.'

Assert-True (
    ($recoveryText -match 'HostSessionToken') -and
    ($recoveryText -match 'projection\.ProjectionVersion')
) 'Post-load recovery must rebind preserved formal-host sessions onto the current published session token instead of killing every loaded session.'

Assert-True (
    $recoveryText -match 'HasPendingPostLoadProjectionRefresh'
) 'Post-load recovery must defer reconciliation while trigger projection publication is still pending after load.'

Assert-True (
    $shootVerbText -match 'TryPreparePendingEmission'
) 'BdpVerb_Shoot must lazily rebuild emission plans after load.'

Assert-True (
    $shootVerbText -match 'WarmupComplete\(\)[\s\S]*TryPreparePendingEmission'
) 'WarmupComplete must allow lazy plan rebuild for resumed loaded sessions.'

Assert-True (
    $shootVerbText -match 'TryCastShot\(\)[\s\S]*TryPreparePendingEmission'
) 'TryCastShot must allow lazy plan rebuild when resuming a saved burst.'

Assert-True (
    $shootVerbText -match 'pendingWindowIndex'
) 'BdpVerb_Shoot must use the persisted burst cursor when resuming after load.'

Assert-True (
    $rangedJobText -match 'public override void ExposeData\(\)'
) 'The ranged execution job must remain save-aware for post-load formal-host continuation.'

Assert-True (
    $lifecycleText -match 'pendingPostLoadProjectionRefresh|TryFinalizePostLoadProjectionRefresh'
) 'CompTriggerBody lifecycle must defer post-load projection refresh when trigger truth is not fully ready.'

Assert-True (
    $lifecycleText -match 'LoadSaveMode\.ResolvingCrossRefs[\s\S]*RestoreShellsPostLoad'
) 'CompTriggerBody lifecycle must reconnect restored formal host shells during ResolvingCrossRefs so loaded attack sessions reach PostLoadInit with a live verb surface.'

Assert-True (
    $equipmentTickPatchText -match 'triggerBody\?\.RuntimeTick\(\);'
) 'Post-load recovery must continue from the unified RuntimeTick() owner after the trigger becomes the current primary weapon.'

Assert-True (
    $runtimeCoordinatorText -match 'RuntimeTick\(\)[\s\S]*TryFinalizePostLoadProjectionRefresh\(\)'
) 'Unified trigger runtime ownership must keep post-load projection finalization inside the runtime tick path.'

Assert-True (
    $equipmentTickPatchText -notmatch 'TryFinalizePostLoadProjectionRefresh'
) 'Equipment tick bridge must stop finalizing post-load refresh directly and delegate that responsibility to RuntimeTick().'

Assert-True (
    $bodyText -match 'HasPendingPostLoadProjectionRefresh'
) 'CompTriggerBody must expose whether post-load projection publication is still pending so recovery can defer instead of miskilling sessions.'

Assert-True (
    $runtimeCoordinatorText -match 'TryFinalizePostLoadProjectionRefresh\(\)[\s\S]*RecoverStaleAttackSession'
) 'Trigger post-load projection finalization must re-run BDP attack-session reconciliation from the unified publish boundary after the projection becomes ready.'

Assert-True (
    $expressionSurfaceText -match 'BuildSelectedSnapshot\(Pawn pawn, ITriggerLoadoutReader triggerLoadoutReader\)'
) 'ExpressionService must support building a snapshot directly from a known trigger loadout reader during post-load restore.'

Assert-True (
    ($runtimeCoordinatorText -match 'BuildProjectionBuildInput\(\)') -and
    ($runtimeCoordinatorText -notmatch 'BuildSelectedSnapshot\(ownerPawn, owner\.LoadoutReaderSurface\)')
) 'Post-load refresh must publish from the trigger body''s own owner snapshot input instead of resolving back through pawn.Primary or public loadout reader.'

Assert-True (
    $slotStateText -notmatch 'RestoreLoadedChipReference\(Thing chip\)[\s\S]*if \(loadedChip == null\)[\s\S]*isActive = false;'
) 'TriggerSlotState must not clear the saved active state just because the chip reference is temporarily unresolved during post-load restore.'

Assert-True (
    ($weaponVisualStageResolverText -notmatch 'Scribe') -and
    ($weaponVisualStageResolverText -match 'HostSessionToken') -and
    ($weaponVisualStageResolverText -match 'CompositeReferenceIndex')
) 'Weapon visual stages must not add persistence; the post-load fallback may only read the restored formal-host token and published projection.'

Write-Output 'PostLoadAttackSessionRecoverySmokeTests PASS'
