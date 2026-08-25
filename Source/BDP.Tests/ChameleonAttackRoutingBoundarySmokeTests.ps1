$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$genericPatch = Get-Content -LiteralPath (Join-Path $repoRoot 'Source\BDP\Patches\Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess.cs') -Raw -Encoding utf8
$meleePatch = Get-Content -LiteralPath (Join-Path $repoRoot 'Source\BDP\Patches\Patch_Verb_MeleeAttack_TryCastShot_BdpAttackActionSuccess.cs') -Raw -Encoding utf8
$abilityPatch = Get-Content -LiteralPath (Join-Path $repoRoot 'Source\BDP\Patches\Patch_Ability_Activate_BdpAttackActionSuccess.cs') -Raw -Encoding utf8

Assert-True ($genericPatch -match 'Verb_LaunchProjectile') `
    'Projectile weapon verbs must publish attack action completion.'
Assert-True ($genericPatch -match 'Verb_MeleeAttack') `
    'Melee weapon verbs must publish attack action completion.'
Assert-True ($genericPatch -match 'Verb_CastAbility') `
    'Ability verbs must be excluded from the generic weapon route.'
Assert-True ($meleePatch -match '!__result|__result\)') `
    'A melee miss or dodge must still close the stealth chip after the action executes.'
Assert-True ($abilityPatch -match 'LocalTargetInfo') `
    'Ability routing must cover local-target activation.'
Assert-True ($abilityPatch -match 'GlobalTargetInfo') `
    'Ability routing must cover world-target activation.'
Assert-True ($abilityPatch -match 'def\.hostile') `
    'Hostile abilities must count as attacks.'
Assert-True ($abilityPatch -match 'verbProperties\.violent') `
    'Violent abilities must count as attacks.'

Write-Output 'ChameleonAttackRoutingBoundarySmokeTests PASS'
