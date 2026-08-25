$ErrorActionPreference = 'Stop'

$projectilePath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\BdpProjectile.cs'
$impactPath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol\Impact\ImpactStageService.cs'
$projectile = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8
$impact = Get-Content -LiteralPath $impactPath -Raw -Encoding UTF8

if ($projectile -notmatch 'ExecuteDirectExtraEffects') {
    throw '直接命中路径没有调用独立额外效果执行器。'
}

if ($projectile -notmatch 'SuppressAllProjectileImpact') {
    throw '直接命中路径没有经过全量伤害处置门。'
}

if ($projectile -notmatch 'ExtraEffectTargetScope\.DirectHitThing') {
    throw '直接命中路径没有区分 DirectHitThing 目标范围。'
}

if ($impact -notmatch 'ExtraEffects\.AddRange') {
    throw 'Impact 结果没有携带模块提交的额外效果。'
}

Write-Output 'RangedDirectDebuffImpactSmokeTests PASS'
