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

$compTriggerBodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$compTriggerBodyFormalHostsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.FormalHosts.cs'
$hostManagerPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$hostSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\VerbHostSurfaceAccess.cs'
$hostInstancePath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\VerbHostInstance.cs'
$buildSpecPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\VerbHostBuildSpec.cs'
$attackSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$attackPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_TryGetAttackVerb.cs'
$meleePatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs'
$proxyVerbPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\VerbHostAutoProxyVerb.cs'
$formalSlotPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\BdpFormalVerbHostSlot.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\BdpFormalVerbBinding.cs'
$bindingStatePath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\BdpFormalVerbBindingState.cs'
$combatProjectionPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerCombatProjectionState.cs'
$lifecyclePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.Lifecycle.cs'
$formalHostShootPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_FormalHostShoot.cs'
$formalHostMeleePath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_FormalHostMelee.cs'
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$emissionCursorPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\RangedVerbEmissionCursor.cs'
$equipmentTickPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs'

$compTriggerBodyText = (Get-Content -LiteralPath $compTriggerBodyPath -Raw -Encoding utf8) + [Environment]::NewLine + (Get-Content -LiteralPath $compTriggerBodyFormalHostsPath -Raw -Encoding utf8)
$hostManagerText = Get-Content -LiteralPath $hostManagerPath -Raw -Encoding utf8
$hostSurfaceText = Get-Content -LiteralPath $hostSurfacePath -Raw -Encoding utf8
$hostInstanceText = if (Test-Path -LiteralPath $hostInstancePath) { Get-Content -LiteralPath $hostInstancePath -Raw -Encoding utf8 } else { '' }
$buildSpecText = if (Test-Path -LiteralPath $buildSpecPath) { Get-Content -LiteralPath $buildSpecPath -Raw -Encoding utf8 } else { '' }
$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8
$attackPatchText = Get-Content -LiteralPath $attackPatchPath -Raw -Encoding utf8
$meleePatchText = Get-Content -LiteralPath $meleePatchPath -Raw -Encoding utf8
$lifecycleText = Get-Content -LiteralPath $lifecyclePath -Raw -Encoding utf8
$formalHostShootText = Get-Content -LiteralPath $formalHostShootPath -Raw -Encoding utf8
$formalHostMeleeText = Get-Content -LiteralPath $formalHostMeleePath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$emissionCursorText = Get-Content -LiteralPath $emissionCursorPath -Raw -Encoding utf8
$combatProjectionText = if (Test-Path -LiteralPath $combatProjectionPath) { Get-Content -LiteralPath $combatProjectionPath -Raw -Encoding utf8 } else { '' }
$equipmentTickPatchText = if (Test-Path -LiteralPath $equipmentTickPatchPath) { Get-Content -LiteralPath $equipmentTickPatchPath -Raw -Encoding utf8 } else { '' }

Assert-True (
    Test-Path -LiteralPath $combatProjectionPath
) 'Formal host projection binding must depend on the published TriggerCombatProjectionState contract.'

Assert-True (
    $combatProjectionText -match 'ResultIdToFormalSlot'
) 'TriggerCombatProjectionState must publish the formal-host slot index needed by TriggerBodyVerbHostManager.'

Assert-True (
    $hostManagerText -match 'public void Refresh\(TriggerCombatProjectionState projection\)'
) 'TriggerBodyVerbHostManager must refresh from the published combat projection surface instead of a raw snapshot.'

Assert-True (
    $hostManagerText -notmatch 'public void Refresh\(ExpressionSnapshot snapshot\)'
) 'TriggerBodyVerbHostManager must stop accepting a raw ExpressionSnapshot refresh contract.'

Assert-True (
    ($hostManagerText -match 'projection\.Snapshot') -or
    ($hostManagerText -match 'projection\.ResultIndex')
) 'TriggerBodyVerbHostManager must consume data from TriggerCombatProjectionState during binding refresh.'

Assert-True (
    ($hostManagerText -match 'projection\.ResultIdToFormalSlot') -and
    ($hostManagerText -match 'projection\.ResultIndex\.TryGetValue')
) 'TriggerBodyVerbHostManager refresh must rebuild formal bindings directly from the published combat projection indexes.'

Assert-True (
    ($hostManagerText -notmatch 'IExpressionReader') -and
    ($hostManagerText -notmatch 'ExpressionFormalSurfaces') -and
    ($hostManagerText -notmatch 'BuildSelectedSnapshot\s*\(')
) 'TriggerBodyVerbHostManager must not depend on public expression reader surfaces to rebuild owner formal-host state.'

Assert-True (
    Test-Path -LiteralPath $formalSlotPath
) 'Formal host architecture must define a fixed BdpFormalVerbHostSlot contract.'

Assert-True (
    Test-Path -LiteralPath $bindingPath
) 'Formal host architecture must define a BdpFormalVerbBinding contract.'

Assert-True (
    Test-Path -LiteralPath $bindingStatePath
) 'Formal host architecture must define a BdpFormalVerbBindingState contract.'

Assert-True (
    (Get-Content -LiteralPath $bindingStatePath -Raw -Encoding utf8) -match 'AttackSessionToken'
) 'Formal host binding state must carry AttackSessionToken-based session identity data.'

Assert-True (
    $hostManagerText -notmatch 'trackerVerbs\.Add\(verb\)'
) 'TriggerBodyVerbHostManager must not inject runtime verbs into VerbTracker.AllVerbs.'

Assert-True (
    $hostManagerText -notmatch 'Activator\.CreateInstance\(spec\.VerbClass\)'
) 'TriggerBodyVerbHostManager must not create runtime host verbs via Activator.CreateInstance.'

Assert-True (
    $hostManagerText -notmatch 'RegisterHostedVerb'
) 'TriggerBodyVerbHostManager must stop registering hosted verbs as a factory concern.'

Assert-True (
    $hostManagerText -notmatch 'UnregisterHostedVerbs'
) 'TriggerBodyVerbHostManager must stop unregistering hosted verbs as a lifecycle concern.'

Assert-True (
    $hostManagerText -match 'BdpFormalVerbBinding'
) 'TriggerBodyVerbHostManager must refresh formal binding state instead of building transient host objects.'

Assert-True (
    $buildSpecText -notmatch 'VerbClass'
) 'VerbHostBuildSpec must no longer carry runtime verb construction metadata.'

Assert-True (
    $hostInstanceText -notmatch 'public Verb Verb \{ get; set; \}'
) 'VerbHostInstance must no longer expose transient runtime Verb as the host truth.'

Assert-True (
    $hostSurfaceText -notmatch 'TryGetPrimaryRanged\(Pawn pawn, out VerbHostInstance host\)'
) 'VerbHostSurfaceAccess must stop handing transient VerbHostInstance objects to callers.'

Assert-True (
    $attackSurfaceText -notmatch 'host\?\.Verb'
) 'AttackExecutionSurfaceAccess must not depend on transient host.Verb references.'

Assert-True (
    $attackSurfaceText -notmatch 'TryCreateAutoRangedProxyVerb'
) 'AttackExecutionSurfaceAccess must not create proxy verbs for auto-ranged routing.'

Assert-True (
    -not (Test-Path -LiteralPath $proxyVerbPath)
) 'Proxy-verb architecture must be deleted completely.'

Assert-True (
    $attackPatchText -notmatch 'proxy'
) 'Patch_Pawn_TryGetAttackVerb must hand out a formal host shell rather than a proxy verb.'

Assert-True (
    $meleePatchText -notmatch 'proxy'
) 'Patch_Pawn_MeleeVerbs_TryMeleeAttack must not rely on proxy-verb routing.'

Assert-True (
    $compTriggerBodyText -match 'GetFormalHostFallbackVerbProps'
) 'CompTriggerBody must keep an internal formal-host fallback declaration surface for BDP-owned shells.'

Assert-True (
    $compTriggerBodyText -notmatch 'IVerbOwner\.VerbProperties'
) 'CompTriggerBody must not route IVerbOwner.VerbProperties to formal hosts anymore.'

Assert-True (
    $compTriggerBodyText -notmatch 'IVerbOwner\.Tools'
) 'CompTriggerBody must not route IVerbOwner.Tools to formal hosts anymore.'

Assert-True (
    $hostManagerText -notmatch 'owner\.VerbTracker\.AllVerbs'
) 'TriggerBodyVerbHostManager must not harvest formal host shells from vanilla VerbTracker anymore.'

Assert-True (
    $hostManagerText -match 'new BdpVerb_FormalHostShoot'
) 'TriggerBodyVerbHostManager must own internal ranged formal host shells.'

Assert-True (
    $hostManagerText -match 'new BdpVerb_FormalHostMelee'
) 'TriggerBodyVerbHostManager must own internal melee formal host shells.'

Assert-True (
    $hostManagerText -match 'public void Tick\(\)'
) 'TriggerBodyVerbHostManager must expose an explicit tick bridge for internally-owned formal host shells.'

Assert-True (
    $hostManagerText -match 'activeVerbsForTick'
) 'TriggerBodyVerbHostManager must maintain an active formal-host tick queue instead of treating every binding as live.'

Assert-True (
    ($formalHostShootText -match 'ShouldTickAsFormalHost') -and
    ($formalHostMeleeText -match 'ShouldTickAsFormalHost')
) 'Formal host shells must expose a minimal active-session query for the active tick queue.'

Assert-True (
    $hostManagerText -match 'for \(int i = activeVerbsForTick\.Count - 1; i >= 0; i--\)'
) 'TriggerBodyVerbHostManager.Tick() must drive steady-state ticking from the active queue.'

Assert-True (
    $lifecycleText -notmatch 'public override void CompTick\(\)[\s\S]*verbHostManager\?\.Tick\(\);'
) 'CompTriggerBody must stop driving internal formal host ticking from CompTick after the lifecycle split.'

Assert-True (
    Test-Path -LiteralPath $equipmentTickPatchPath
) 'Equipped trigger-body formal host lifecycle must add a Pawn_EquipmentTracker tick bridge.'

Assert-True (
    $equipmentTickPatchText -match 'EquipmentTrackerTick'
) 'Equipped trigger-body formal host lifecycle must hook Pawn_EquipmentTracker.EquipmentTrackerTick.'

Assert-True (
    $equipmentTickPatchText -match 'triggerBody\?\.RuntimeTick\(\);'
) 'Equipped trigger-body formal host lifecycle must advance formal host ticking through the unified RuntimeTick() pawn equipment bridge.'

Assert-True (
    $formalHostShootText -match 'loadID ='
) 'Internally-owned ranged formal host shells must assign a stable loadID during initialization.'

Assert-True (
    $formalHostMeleeText -match 'loadID ='
) 'Internally-owned melee formal host shells must assign a stable loadID during initialization.'

Assert-True (
    $formalHostShootText -match 'Reset\(\);'
) 'Internally-owned ranged formal host shells must reset stale burst state when binding changes.'

Assert-True (
    $formalHostMeleeText -match 'Reset\(\);'
) 'Internally-owned melee formal host shells must reset stale state when binding changes.'

Assert-True (
    $hostManagerText -match 'ExposeVerbShells'
) 'TriggerBodyVerbHostManager must expose formal host shells to the save tree.'

Assert-True (
    $hostManagerText -match 'Scribe_Collections\.Look\(ref .*LookMode\.Deep'
) 'TriggerBodyVerbHostManager must deep-save formal host shell collections.'

Assert-True (
    $hostManagerText -match 'RestoreShellsPostLoad'
) 'TriggerBodyVerbHostManager must restore deep-saved formal host shells after load.'

Assert-True (
    $lifecycleText -match 'verbHostManager\?\.ExposeVerbShells\(\)'
) 'CompTriggerBody lifecycle must route save/load through the formal-host shell persistence entry.'

Assert-True (
    $lifecycleText -match 'LoadSaveMode\.ResolvingCrossRefs[\s\S]*verbHostManager\?\.RestoreShellsPostLoad\(\)'
) 'CompTriggerBody lifecycle must reconnect restored formal host shells during ResolvingCrossRefs before vanilla PostLoadInit inspects loaded verbs.'

Assert-True (
    $lifecycleText -match 'verbHostManager\?\.RestoreShellsPostLoad\(\)'
) 'CompTriggerBody lifecycle must reconnect restored formal host shells before refreshing projections.'

Assert-True (
    $shootVerbText -match 'public override void ExposeData\(\)'
) 'BdpVerb_Shoot must persist the minimal formal-host session truth.'

Assert-True (
    $shootVerbText -match 'Scribe_Deep\.Look\(ref hostSessionToken'
) 'BdpVerb_Shoot must deep-persist HostSessionToken via a backing field.'

Assert-True (
    ($shootVerbText -match 'emissionCursor\.ExposeData\(\)') -and
    ($emissionCursorText -match 'Scribe_Values\.Look\(ref pendingWindowIndex')
) 'BdpVerb_Shoot must persist the pending window cursor through RangedVerbEmissionCursor.'

Assert-True (
    ($shootVerbText -match 'emissionCursor\.ExposeData\(\)') -and
    ($emissionCursorText -match 'Scribe_Values\.Look\(ref pendingWindowProjectilePlanIndex')
) 'BdpVerb_Shoot must persist the per-window projectile cursor through RangedVerbEmissionCursor.'

Assert-True (
    $shootVerbText -match 'TryStartCastOn\([\s\S]*ClearPendingEmissionPlan\(\)[\s\S]*TryPreparePendingEmission\(target\)'
) 'BdpVerb_Shoot must clear any stale pending emission plan before preparing a new cast target.'

Assert-True (
    ($formalHostShootText -match 'preserveLoadedStateOnce') -and
    ($formalHostShootText -match 'HostSessionToken')
) 'Formal host ranged shell must preserve loaded state across the first post-load rebind.'

Assert-True (
    ($formalHostMeleeText -match 'preserveLoadedStateOnce') -and
    ($formalHostMeleeText -match 'HostSessionToken')
) 'Formal host melee shell must preserve loaded state across the first post-load rebind.'

Assert-True (
    $formalHostShootText -match 'ApplyPostLoadFallbackSurface'
) 'Formal host ranged shell must expose a post-load fallback surface hook so vanilla does not kill the restored verb before formal rebinding.'

Assert-True (
    $formalHostMeleeText -match 'ApplyPostLoadFallbackSurface'
) 'Formal host melee shell must expose a post-load fallback surface hook so vanilla does not kill the restored verb before formal rebinding.'

Assert-True (
    $hostManagerText -match 'ApplyPostLoadFallbackSurface'
) 'TriggerBodyVerbHostManager must prime restored formal host shells with fallback surfaces during post-load restore.'

Write-Output 'FormalHostVerbSmokeTests PASS'
