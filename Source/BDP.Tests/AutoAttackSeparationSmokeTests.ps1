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

$formalHostsPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.FormalHosts.cs'
$hostManagerPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$hostSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\VerbHostSurfaceAccess.cs'
$expressionSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Access\Surfaces\ExpressionFormalSurfaces.cs'
$defaultPrimarySelectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultPrimaryExpressionSelector.cs'
$attackSurfacePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs'
$attackStagesPath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionService.Stages.cs'
$attackPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_TryGetAttackVerb.cs'
$meleePatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs'

$formalHostsText = Get-Content -LiteralPath $formalHostsPath -Raw -Encoding utf8
$hostManagerText = Get-Content -LiteralPath $hostManagerPath -Raw -Encoding utf8
$hostSurfaceText = Get-Content -LiteralPath $hostSurfacePath -Raw -Encoding utf8
$expressionSurfaceText = Get-Content -LiteralPath $expressionSurfacePath -Raw -Encoding utf8
$defaultPrimarySelectorText = Get-Content -LiteralPath $defaultPrimarySelectorPath -Raw -Encoding utf8
$attackSurfaceText = Get-Content -LiteralPath $attackSurfacePath -Raw -Encoding utf8
$attackStagesText = Get-Content -LiteralPath $attackStagesPath -Raw -Encoding utf8
$attackPatchText = Get-Content -LiteralPath $attackPatchPath -Raw -Encoding utf8
$meleePatchText = Get-Content -LiteralPath $meleePatchPath -Raw -Encoding utf8

Assert-True (
    $formalHostsText -notmatch 'public new List<VerbProperties> VerbProperties'
) 'CompTriggerBody must stop overriding the vanilla VerbProperties declaration surface.'

Assert-True (
    $formalHostsText -notmatch 'public new List<Tool> Tools'
) 'CompTriggerBody must stop overriding the vanilla Tools declaration surface.'

Assert-True (
    $formalHostsText -notmatch 'IVerbOwner\.VerbProperties'
) 'CompTriggerBody must not explicitly reroute IVerbOwner.VerbProperties to formal hosts anymore.'

Assert-True (
    $formalHostsText -notmatch 'IVerbOwner\.Tools'
) 'CompTriggerBody must not explicitly reroute IVerbOwner.Tools to formal hosts anymore.'

Assert-True (
    $hostManagerText -notmatch 'owner\.VerbTracker\.AllVerbs'
) 'TriggerBodyVerbHostManager must no longer harvest formal host shells from vanilla VerbTracker.'

Assert-True (
    $hostManagerText -notmatch 'trackerVerbs'
) 'TriggerBodyVerbHostManager must no longer depend on tracker verb enumeration for formal host ownership.'

Assert-True (
    $hostManagerText -match 'new BdpVerb_FormalHostShoot'
) 'TriggerBodyVerbHostManager must own internal ranged formal host shells itself.'

Assert-True (
    $hostManagerText -match 'new BdpVerb_FormalHostMelee'
) 'TriggerBodyVerbHostManager must own internal melee formal host shells itself.'

Assert-True (
    $hostSurfaceText -notmatch 'TryGetPrimaryRangedBinding'
) 'VerbHostSurfaceAccess must no longer expose auto-primary host pickers.'

Assert-True (
    $hostSurfaceText -notmatch 'TryGetPrimaryMeleeBinding'
) 'VerbHostSurfaceAccess must no longer expose auto-primary melee pickers.'

Assert-True (
    ($expressionSurfaceText -notmatch 'BuildSelectedSnapshot\(Pawn pawn\)\s*\{') -and
    ($expressionSurfaceText -match 'BuildSelectedSnapshot\(Pawn pawn, ITriggerLoadoutReader triggerLoadoutReader\)')
) 'Expression service must keep only the owner-supplied selected snapshot build path.'

Assert-True (
    $defaultPrimarySelectorText -match 'snapshot\.PrimaryRanged = firstDualRanged \?\? firstPrimaryRanged;'
) 'DefaultPrimaryExpressionSelector must remain the source of PrimaryRanged selection.'

Assert-True (
    $defaultPrimarySelectorText -match 'snapshot\.PrimaryMelee = firstDualMelee \?\? firstPrimaryMelee;'
) 'DefaultPrimaryExpressionSelector must remain the source of PrimaryMelee selection.'

Assert-True (
    $attackSurfaceText -notmatch 'BuildSelectedSnapshot\s*\('
) 'AttackExecutionSurfaceAccess must not rebuild expression snapshots for auto attack anymore.'

Assert-True (
    ($attackSurfaceText -match 'PublishedCombatProjection') -or
    ($attackSurfaceText -match 'ProjectionVersion')
) 'AttackExecutionSurfaceAccess must read the published combat projection for auto attack.'

Assert-True (
    ($attackSurfaceText -match 'ExpressionSnapshot snapshot = projection\.Snapshot;') -and
    ($attackSurfaceText -notmatch 'GetSnapshot\(Pawn pawn\)')
) 'AttackExecutionSurfaceAccess auto attack bridge must consume the snapshot already published inside TriggerCombatProjectionState.'

Assert-True (
    $attackSurfaceText -match 'snapshot\.PrimaryMelee'
) 'Auto-melee bridge must read snapshot.PrimaryMelee.'

Assert-True (
    $attackSurfaceText -match 'TryGetByResultId\(pawn,\s*result\.Id'
) 'Auto-ranged bridge must resolve execution hosts by the published primary result id.'

Assert-True (
    $attackSurfaceText -match 'AttackSessionToken\.Create\(\s*pawn,\s*result\.Id,\s*projection\.ProjectionVersion\)'
) 'Auto-melee bridge must execute by the published primary result id through the formal attack session token.'

Assert-True (
    ($attackSurfaceText -match 'ResolveActiveVerb\(\)') -and
    ($attackSurfaceText -notmatch 'TryGetSelectedResult\s*\(')
) 'Auto attack bridge must resolve hosted execution verbs from published result bindings instead of re-running expression selection.'

Assert-True (
    $attackStagesText -match 'CanExecuteDualRangedSide'
) 'Auto attack execution bridge must keep the dual per-side legality gate in the staged planner.'

Assert-True (
    $attackSurfaceText -notmatch 'TryGetPrimaryRangedBinding'
) 'AttackExecutionSurfaceAccess must stop using formal-host primary binding shortcuts for auto-ranged selection.'

Assert-True (
    $attackSurfaceText -notmatch 'TryGetPrimaryMeleeBinding'
) 'AttackExecutionSurfaceAccess must stop using formal-host primary binding shortcuts for auto-melee selection.'

Assert-True (
    $attackPatchText -match 'TryGetAutoRangedVerb'
) 'Patch_Pawn_TryGetAttackVerb must still bridge through the dedicated auto-ranged accessor.'

Assert-True (
    $attackPatchText -match 'bool allowManualCastWeapons'
) 'Patch_Pawn_TryGetAttackVerb must receive original allowManualCastWeapons from Pawn.TryGetAttackVerb.'

Assert-True (
    $attackPatchText -match 'TryGetAutoRangedVerb\(__instance,\s*allowManualCastWeapons,'
) 'Patch_Pawn_TryGetAttackVerb must pass allowManualCastWeapons into BDP auto-ranged selection.'

Assert-True (
    $attackSurfaceText -match 'PassesVanillaManualAutoGate'
) 'AttackExecutionSurfaceAccess must apply the vanilla manual/auto gate before returning a BDP auto-ranged verb.'

Assert-True (
    ($attackSurfaceText -match 'onlyManualCast') -and
    ($attackSurfaceText -match 'JobDefOf\.Wait_Combat') -and
    ($attackSurfaceText -match 'allowManualCastWeapons')
) 'BDP auto-ranged selection must preserve the vanilla onlyManualCast / Wait_Combat / allowManualCastWeapons boundary.'

Assert-True (
    $attackSurfaceText -notmatch 'pawn\.TryGetAttackVerb\s*\('
) 'AttackExecutionSurfaceAccess must not call Pawn.TryGetAttackVerb from inside the TryGetAttackVerb patch path.'

Assert-True (
    $attackPatchText -notmatch '__result != null && !__result\.IsMeleeAttack'
) 'Patch_Pawn_TryGetAttackVerb must not preserve vanilla ranged picks when expression PrimaryRanged exists.'

Assert-True (
    $meleePatchText -match 'TryExecuteAutoMelee'
) 'Patch_Pawn_MeleeVerbs_TryMeleeAttack must still bridge through the dedicated auto-melee accessor.'

Assert-True (
    $meleePatchText -match 'if \(!AttackExecutionSurfaceAccess\.TryExecuteAutoMelee\(__instance\.Pawn, target\)\)'
) 'Auto-melee patch must only fall back to vanilla when no valid expression PrimaryMelee is available.'

Assert-True (
    $meleePatchText -match 'jobs\.curJob\.playerForced'
) 'Auto-melee patch must not treat player-forced vanilla melee orders as auto-melee takeover candidates.'

Write-Output 'AutoAttackSeparation PASS'
