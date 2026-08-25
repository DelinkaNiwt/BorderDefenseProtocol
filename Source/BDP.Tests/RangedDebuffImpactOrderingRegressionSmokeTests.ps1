$ErrorActionPreference = 'Stop'

$projectilePath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\BdpProjectile.cs'
$debuffModulePath = Join-Path $PSScriptRoot '..\BDP.Content\RangedModules\Debuff\RangedDebuffModule.cs'
$explosionPatchPath = Join-Path $PSScriptRoot '..\BDP\Patches\Patch_DamageWorker_ExplosionDamageThing_BdpSemantics.cs'
$impactContributionPath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol\Impact\ImpactContribution.cs'

$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8
$debuffModuleText = Get-Content -LiteralPath $debuffModulePath -Raw -Encoding UTF8
$explosionPatchText = Get-Content -LiteralPath $explosionPatchPath -Raw -Encoding UTF8
$impactContributionText = Get-Content -LiteralPath $impactContributionPath -Raw -Encoding UTF8

$impactStart = $projectileText.IndexOf('private void ExecuteImpact(')
$damageStart = $projectileText.IndexOf('private DamageWorker.DamageResult ApplyDirectDamage')
if ($impactStart -lt 0 -or $damageStart -lt 0 -or $damageStart -le $impactStart) {
    throw '无法定位投射物命中执行边界。'
}

$impactText = $projectileText.Substring($impactStart, $damageStart - $impactStart)
if ($impactText -match 'ExecuteDirectExtraEffects\s*\(') {
    throw '额外减益仍然位于原版伤害入口之前。'
}

if ($debuffModuleText -match 'HasAreaEffect\s*=\s*true' -or
    $debuffModuleText -match 'OverrideAreaEffect\s*=' -or
    $debuffModuleText -match 'BuildAreaEffect\s*\(') {
    throw '远程减益仍在制造第二个范围生产者，范围爆炸必须由基线或 AreaExplosionModule 唯一生产。'
}

if ($explosionPatchText -notmatch 'Postfix') {
    throw '范围逐目标效果缺少伤害/护盾裁决完成后的派发出口。'
}

if ($impactContributionText -notmatch 'InterceptedHitFeedback' -and
    $impactContributionText -notmatch 'HitFeedbackPolicy') {
    throw '命中反馈仍然只有颜色字段，没有独立的反馈策略。'
}

Write-Output 'RangedDebuffImpactOrderingRegressionSmokeTests PASS'
