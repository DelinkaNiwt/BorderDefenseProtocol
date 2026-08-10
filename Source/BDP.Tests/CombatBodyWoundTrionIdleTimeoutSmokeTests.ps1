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

$policyDefPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundPolicyDef.cs'
$policyXmlPath = Join-Path $repoRoot '1.6\Defs\Pawn\CombatBody\CombatBodyWoundPolicyDefs.xml'
$runtimePath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundRuntime.cs'
$bindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'
$notifyPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_HealthTracker_NotifyHediffChanged_CombatBodyWounds.cs'

Assert-True (Test-Path -LiteralPath $policyDefPath) 'CombatBodyWoundPolicyDef.cs must exist.'
Assert-True (Test-Path -LiteralPath $policyXmlPath) 'CombatBodyWoundPolicyDefs.xml must exist.'
Assert-True (Test-Path -LiteralPath $runtimePath) 'CombatBodyWoundRuntime.cs must exist.'
Assert-True (Test-Path -LiteralPath $bindingPath) 'CombatBodyWoundTrionBinding.cs must exist.'
Assert-True (Test-Path -LiteralPath $notifyPatchPath) 'Notify_HediffChanged wound patch must exist.'

$policyDefText = Get-Content -LiteralPath $policyDefPath -Raw -Encoding utf8
$runtimeText = Get-Content -LiteralPath $runtimePath -Raw -Encoding utf8
$bindingText = Get-Content -LiteralPath $bindingPath -Raw -Encoding utf8
$notifyPatchText = Get-Content -LiteralPath $notifyPatchPath -Raw -Encoding utf8
$tickStart = $runtimeText.IndexOf('internal void Tick(Pawn pawn)')
$tickEnd = $runtimeText.IndexOf('private void ScheduleNextCalibration', $tickStart)
Assert-True (($tickStart -ge 0) -and ($tickEnd -gt $tickStart)) 'Wound runtime must keep a Tick method before ScheduleNextCalibration.'
$tickText = $runtimeText.Substring($tickStart, $tickEnd - $tickStart)
$restoreStart = $runtimeText.IndexOf('internal void RestoreAfterLoad(Pawn pawn)')
$restoreEnd = $runtimeText.IndexOf('internal void Tick(Pawn pawn)', $restoreStart)
Assert-True (($restoreStart -ge 0) -and ($restoreEnd -gt $restoreStart)) 'Wound runtime must keep RestoreAfterLoad before Tick.'
$restoreText = $runtimeText.Substring($restoreStart, $restoreEnd - $restoreStart)

[xml]$policyXml = Get-Content -LiteralPath $policyXmlPath -Raw -Encoding utf8
$policy = $policyXml.Defs.'BDP.Core.CombatBody.Wounds.CombatBodyWoundPolicyDef' |
    Where-Object { $_.defName -eq 'BDP_DefaultCombatBodyWoundPolicy' } |
    Select-Object -First 1

Assert-True ($policyDefText -match 'trionDrainIdleTimeoutTicks\s*=\s*600') 'Policy def must expose a default 600 tick wound drain idle timeout.'
Assert-True ($policy.trionDrainIdleTimeoutTicks -eq '600') 'Default policy XML must set wound drain idle timeout to 600 ticks.'
Assert-True ($bindingText -match 'expiryTickByHediffLoadId') 'Wound drain binding must track each wound drain expiry tick.'
Assert-True ($bindingText -match 'ExpireIdleDrains\s*\(') 'Wound drain binding must expose idle drain expiry processing.'
Assert-True ($bindingText -match 'currentTick\s*\+\s*Math\.Max\(1,\s*idleTimeoutTicks\)') 'Wound drain registration must refresh expiry to current tick plus timeout.'
Assert-True ($bindingText -match 'UnregisterDrain') 'Idle expiry must unregister wound drains from Trion.'
Assert-True ($bindingText -match 'ResolveNextExpiryTick') 'Wound drain binding must report the next active expiry tick after each expiry pass.'
Assert-True ($tickText -match 'ExpireIdleDrains\(pawn,\s*ticksGame,\s*expiredDrainIds\)') 'Wound runtime tick must expire idle drains and collect expired ids.'
Assert-True ($runtimeText -match 'ScheduleNextExpiry\(expiryTick\)') 'Wound runtime must schedule the next check at the refreshed wound expiry.'
Assert-True ($runtimeText -match 'ScheduleNextExpiry\(nextExpiryTick\)') 'Wound runtime must reschedule to the next active expiry after an expiry pass.'
Assert-True ($tickText -notmatch 'RebuildActiveWounds\(pawn\);') 'Wound runtime tick must not rebuild and re-register old wounds after idle timeout.'
Assert-True ($restoreText -notmatch 'RebuildActiveWounds\(pawn\);') 'Wound runtime restore must not clear saved wound drains by rebuilding.'
Assert-True ($notifyPatchText -match 'Notify_HediffChanged') 'Wound notify patch must still document the vanilla notification boundary.'
Assert-True ($notifyPatchText -match 'hediff\s*!=\s*null') 'Wound notify patch must explicitly ignore non-null Hediff changes.'
Assert-True ($notifyPatchText -notmatch 'NotifyWoundAddedOrChanged') 'Generic Notify_HediffChanged must not refresh wound drains; vanilla natural healing calls it for random injuries.'

Write-Output 'CombatBodyWoundTrionIdleTimeoutSmokeTests PASS'
