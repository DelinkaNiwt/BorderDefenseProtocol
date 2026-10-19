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

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contextPath = Join-Path $coreRoot 'AttackExecution\MeleeAttackExecutionContext.cs'
$verbPath = Join-Path $coreRoot 'Verbs\BdpVerb_MeleeAttackDamage.cs'

$contextText = Get-Content -LiteralPath $contextPath -Raw -Encoding utf8
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8

Assert-True ($contextText -match 'HitPartPolicy') 'Melee execution context must carry the frozen optional hit-part policy.'
Assert-True (($verbText -match 'HitPartPolicyResolverAccess\.TryResolve') -and ($verbText -match 'HitPartPolicy')) 'Melee primary damage must consult the Core resolver bridge only when a policy exists.'
Assert-True ($verbText -match 'new DamageInfo\(damageDef, num, armorPenetration[\s\S]{0,300}hitPart') 'Melee primary damage must pass the resolved part into the original DamageInfo constructor.'
Assert-True (($verbText -match 'extraMeleeDamage\.def[\s\S]{0,180}caster, null') -and ($verbText -match 'extraDamageInfo = new DamageInfo\(item\.def[\s\S]{0,180}caster, null')) 'Extra and surprise damage must retain null hit-part overrides.'
Assert-True (($verbText -match 'HitPartPolicyResolverAccess\.TryResolve') -and ($verbText -match 'BuildDamageInfosToApply')) 'Melee policy resolution must stay inside the primary damage construction path.'

Write-Output 'MeleeHitPartPolicySmokeTests PASS'
