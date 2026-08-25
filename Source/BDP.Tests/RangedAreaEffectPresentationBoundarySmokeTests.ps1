$ErrorActionPreference = 'Stop'

$areaPlanPath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol\Model\AreaEffectPlan.cs'
$presentationPath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\RangedFlightProtocol\Model\ExplosionPresentationPolicy.cs'
$bridgePath = Join-Path $PSScriptRoot '..\BDP\Core\Semantics\BdpDamageSemanticBridge.cs'
$startPatchPath = Join-Path $PSScriptRoot '..\BDP\Patches\Patch_Explosion_StartExplosion_BdpSemantics.cs'
$damagePatchPath = Join-Path $PSScriptRoot '..\BDP\Patches\Patch_DamageWorker_ExplosionDamageThing_BdpSemantics.cs'

$areaPlan = Get-Content -LiteralPath $areaPlanPath -Raw -Encoding UTF8
$presentation = Get-Content -LiteralPath $presentationPath -Raw -Encoding UTF8
$bridge = Get-Content -LiteralPath $bridgePath -Raw -Encoding UTF8
$startPatch = Get-Content -LiteralPath $startPatchPath -Raw -Encoding UTF8
$damagePatch = Get-Content -LiteralPath $damagePatchPath -Raw -Encoding UTF8

if ($areaPlan -notmatch 'PresentationPolicy') {
    throw 'AreaEffectPlan 未提供独立范围表现策略。'
}

foreach ($member in @('SuppressVanillaVisualEffects', 'SuppressVanillaSoundEffects', 'OverrideScreenShakeFactor')) {
    if ($presentation -notmatch ('\b' + $member + '\b')) {
        throw ('范围表现策略缺少成员：' + $member)
    }
}

if ($bridge -notmatch 'AssignExplosionImpactContext' -or $bridge -notmatch 'GetExplosionImpactContext') {
    throw '爆炸语义桥没有保存逐目标命中上下文。'
}

if ($startPatch -notmatch 'doVisualEffects' -or $startPatch -notmatch 'doSoundEffects') {
    throw '爆炸启动入口没有消费视觉和音效策略。'
}

if ($damagePatch -notmatch 'ExecuteAreaEffects' -or $damagePatch -notmatch 'ExplosionDamageThing') {
    throw '范围爆炸逐目标派发入口未建立。'
}

Write-Output 'RangedAreaEffectPresentationBoundarySmokeTests PASS'
