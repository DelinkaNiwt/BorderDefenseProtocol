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

$hostManagerPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\TriggerBodyVerbHostManager.cs'
$formalHostShootPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_FormalHostShoot.cs'
$formalHostMeleePath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_FormalHostMelee.cs'
$shootVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$meleeVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_MeleeAttackDamage.cs'

$hostManagerText = Get-Content -LiteralPath $hostManagerPath -Raw -Encoding utf8
$formalHostShootText = Get-Content -LiteralPath $formalHostShootPath -Raw -Encoding utf8
$formalHostMeleeText = Get-Content -LiteralPath $formalHostMeleePath -Raw -Encoding utf8
$shootVerbText = Get-Content -LiteralPath $shootVerbPath -Raw -Encoding utf8
$meleeVerbText = Get-Content -LiteralPath $meleeVerbPath -Raw -Encoding utf8
$tickMethodStart = $hostManagerText.IndexOf('public void Tick()')
$tickMethodEnd = if ($tickMethodStart -ge 0) {
    $hostManagerText.IndexOf('private void RepairActiveQueueIfLoadedRuntimeMissing', $tickMethodStart)
} else {
    -1
}
$tickMethodBody = if ($tickMethodStart -ge 0 -and $tickMethodEnd -gt $tickMethodStart) {
    $hostManagerText.Substring($tickMethodStart, $tickMethodEnd - $tickMethodStart)
} else {
    ''
}

Assert-True (
    $hostManagerText -match 'activeVerbsForTick'
) 'TriggerBodyVerbHostManager must maintain an explicit active formal-host tick queue.'

Assert-True (
    $hostManagerText -match 'NotifyFormalHostSessionStarted'
) 'TriggerBodyVerbHostManager must expose a session-start entry for formal host verbs to enter the active tick queue.'

Assert-True (
    $hostManagerText -match 'public void Tick\(\)\s*\{[\s\S]*activeVerbsForTick'
) 'TriggerBodyVerbHostManager.Tick() must advance only the active formal-host queue.'

Assert-True (
    $hostManagerText -match 'public void Tick\(\)\s*\{[\s\S]*for \(int i = activeVerbsForTick\.Count - 1; i >= 0; i--\)'
) 'TriggerBodyVerbHostManager.Tick() must iterate the active queue directly instead of scanning the full binding table.'

Assert-True (
    -not [string]::IsNullOrWhiteSpace($tickMethodBody)
) 'FormalHostActiveTick smoke test must be able to isolate TriggerBodyVerbHostManager.Tick() for steady-state assertions.'

Assert-True (
    $tickMethodBody -notmatch 'RebuildActiveVerbQueue\(\)'
) 'TriggerBodyVerbHostManager.Tick() must not rebuild the active queue by rescanning all bindings during steady-state ticking.'

Assert-True (
    $tickMethodBody -notmatch 'foreach \(KeyValuePair<BdpFormalVerbHostSlot, BdpFormalVerbBinding> pair in bindings\)'
) 'TriggerBodyVerbHostManager.Tick() must not rescan the full binding dictionary during steady-state ticking.'

Assert-True (
    $hostManagerText -match 'RepairActiveQueueIfLoadedRuntimeMissing'
) 'TriggerBodyVerbHostManager must repair a missing active queue when loaded formal-host runtime state is still present.'

Assert-True (
    $tickMethodBody -match 'RepairActiveQueueIfLoadedRuntimeMissing\(\)'
) 'TriggerBodyVerbHostManager.Tick() must run the loaded-runtime queue repair before the active-queue tick loop.'

Assert-True (
    $hostManagerText -match 'activeVerbsForTick\.Count > 0[\s\S]*return;[\s\S]*foreach \(KeyValuePair<BdpFormalVerbHostSlot, BdpFormalVerbBinding> pair in bindings\)'
) 'Loaded-runtime queue repair must keep the steady-state active queue path O(active) and only scan bindings when the active queue is empty.'

Assert-True (
    $shootVerbText -match 'RequiresFormalHostRuntimeTick'
) 'BdpVerb_Shoot must expose a minimal runtime-tick activity predicate for formal host management.'

Assert-True (
    $meleeVerbText -match 'RequiresFormalHostRuntimeTick'
) 'BdpVerb_MeleeAttackDamage must expose a minimal runtime-tick activity predicate for formal host management.'

Assert-True (
    $formalHostShootText -match 'ShouldTickAsFormalHost'
) 'BdpVerb_FormalHostShoot must expose a formal-host-specific active tick query.'

Assert-True (
    $formalHostMeleeText -match 'ShouldTickAsFormalHost'
) 'BdpVerb_FormalHostMelee must expose a formal-host-specific active tick query.'

Assert-True (
    $formalHostShootText -match 'NotifyFormalHostSessionStarted'
) 'BdpVerb_FormalHostShoot must notify the manager when a ranged formal host session starts.'

Assert-True (
    $formalHostMeleeText -match 'NotifyFormalHostSessionStarted'
) 'BdpVerb_FormalHostMelee must notify the manager when a melee formal host session starts.'

Write-Output 'FormalHostActiveTick PASS'
