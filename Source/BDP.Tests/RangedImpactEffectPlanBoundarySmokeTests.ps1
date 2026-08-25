$ErrorActionPreference = 'Stop'

$sourceRoot = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol'
$contributionPath = Join-Path $sourceRoot 'Impact\ImpactContribution.cs'
$impactPlanPath = Join-Path $sourceRoot 'Model\ImpactPlan.cs'
$extraEffectPath = Join-Path $sourceRoot 'Model\ExtraEffectPlan.cs'
$dispositionPath = Join-Path $sourceRoot 'Impact\DamageDisposition.cs'

$contribution = Get-Content -LiteralPath $contributionPath -Raw -Encoding UTF8
$impactPlan = Get-Content -LiteralPath $impactPlanPath -Raw -Encoding UTF8
$extraEffect = Get-Content -LiteralPath $extraEffectPath -Raw -Encoding UTF8

if (-not (Test-Path -LiteralPath $dispositionPath)) {
    throw 'Impact 伤害处置枚举尚未建立。'
}

$disposition = Get-Content -LiteralPath $dispositionPath -Raw -Encoding UTF8
foreach ($name in @('Preserve', 'SuppressBaselineImpact', 'SuppressModuleExtraDamage', 'SuppressAllProjectileImpact')) {
    if ($disposition -notmatch ('\b' + $name + '\b')) {
        throw ('DamageDisposition 缺少处置值：' + $name)
    }
}

if ($contribution -notmatch 'ExtraEffectsToAppend') {
    throw 'ImpactContribution 未提供额外效果追加集合。'
}

if ($impactPlan -notmatch 'ExtraEffects' -or $impactPlan -notmatch 'DamageDisposition') {
    throw 'ImpactPlan 未同时保存额外效果和伤害处置。'
}

if ($extraEffect -match 'Hediff|BDP\.Content') {
    throw 'Core ExtraEffectPlan 越过了 Content 业务边界。'
}

Write-Output 'RangedImpactEffectPlanBoundarySmokeTests PASS'
