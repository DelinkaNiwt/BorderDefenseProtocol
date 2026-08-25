$ErrorActionPreference = 'Stop'

$projectilePath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\BdpProjectile.cs'
$explosionPatchPath = Join-Path $PSScriptRoot '..\BDP\Patches\Patch_DamageWorker_ExplosionDamageThing_BdpSemantics.cs'

foreach ($path in @($projectilePath, $explosionPatchPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('缺少命中反馈时序实现文件：' + $path)
    }
}

$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8
$explosionPatchText = Get-Content -LiteralPath $explosionPatchPath -Raw -Encoding UTF8

$impactStart = $projectileText.IndexOf('private void ExecuteImpact(')
$damageStart = $projectileText.IndexOf('private DamageWorker.DamageResult ApplyDirectDamage')
if ($impactStart -lt 0 -or $damageStart -lt 0 -or $damageStart -le $impactStart) {
    throw '无法定位投射物的命中执行与原版伤害入口。'
}

$impactText = $projectileText.Substring($impactStart, $damageStart - $impactStart)
if ($impactText -match 'ApplyProjectileHitReaction\s*\(') {
    throw '普通命中路径在进入原版 TakeDamage（承受伤害）前手动触发了受击反馈。'
}

if ($projectileText -notmatch 'ApplySuppressedHitFeedback\s*\(') {
    throw '无伤害铅弹路径缺少独立的、命中裁决完成后的反馈出口。'
}

if ($projectileText -notmatch '(?s)DamageResolutionOutcome\.ModuleIntercepted.*?ImpactHitFeedbackMode\.VanillaPawn.*?ApplySuppressedHitFeedback') {
    throw '无伤害反馈没有绑定“模块拦截结果”与显式 Pawn 反馈策略。'
}

if ($explosionPatchText -match '(?s)AttackTargetEventDispatcher\.Dispatch\(.*?ApplyProjectileHitReaction\s*\(') {
    throw '范围命中路径仍在原版爆炸伤害裁决前直接调用通用受击反馈。'
}

if ($explosionPatchText -notmatch '(?s)public static void Postfix.*?DamageResolutionRuntime\.ConsumeLast.*?InterceptedHitFeedback == ImpactHitFeedbackMode\.VanillaPawn.*?ApplySuppressedHitFeedback\s*\(') {
    throw '范围无伤害铅弹缺少“原版爆炸结算后、模块拦截策略明确”的反馈出口。'
}

Write-Output 'RangedHitFeedbackShieldOrderingSmokeTests PASS'
