$ErrorActionPreference = 'Stop'

$projectilePath = Join-Path $PSScriptRoot '..\BDP\Core\Projectiles\BdpProjectile.cs'
$areaColorPatchPath = Join-Path $PSScriptRoot '..\BDP\Patches\Patch_DamageWorker_AddInjury_BdpHitFeedbackColor.cs'
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8

$damageStart = $projectileText.IndexOf('ApplyDirectDamage(')
$feedbackStart = $projectileText.IndexOf('internal void ApplySuppressedHitFeedback(')
if ($damageStart -lt 0 -or $feedbackStart -lt 0 -or $feedbackStart -le $damageStart) {
    throw '无法定位单体伤害与铅弹完整反馈入口。'
}

$damageText = $projectileText.Substring($damageStart, $feedbackStart - $damageStart)
$feedbackText = $projectileText.Substring($feedbackStart)

if ($damageText -notmatch 'DamageWorker\.DamageResult\s+damageResult') {
    throw '普通单体伤害没有保留原版 DamageResult（伤害结果）供反馈颜色判断。'
}

if ($damageText -notmatch '(?s)damageResult\.wounded.*?HitFeedbackColorRuntime\.Register') {
    throw '普通单体攻击没有在原版确认 wounded（实际受伤）后消费减益模块颜色。'
}

$registerIndex = $feedbackText.IndexOf('HitFeedbackColorRuntime.Register')
$drawerIndex = $feedbackText.IndexOf('Drawer?.Notify_DamageApplied')
if ($registerIndex -lt 0 -or $drawerIndex -lt 0 -or $registerIndex -ge $drawerIndex) {
    throw '铅弹完整反馈没有先登记颜色，再进入原版 Pawn Drawer（绘制器）完整受击反馈入口。'
}

if ($feedbackText -notmatch '(?s)Drawer\?\.Notify_DamageApplied.*?Notify_BulletImpact') {
    throw '铅弹单体完整反馈没有保留原版子弹僵直顺序。'
}

$impactStart = $projectileText.IndexOf('private void ExecuteImpact(')
if ($impactStart -lt 0 -or $impactStart -ge $damageStart) {
    throw '无法定位普通命中执行入口。'
}

$impactText = $projectileText.Substring($impactStart, $damageStart - $impactStart)
if ($impactText -match 'Drawer\?\.Notify_DamageApplied|ApplySuppressedHitFeedback\s*\(') {
    throw '普通命中执行入口仍然直接制造 Pawn 受击反馈。'
}

if (-not (Test-Path -LiteralPath $areaColorPatchPath)) {
    throw '范围原版伤害反馈颜色缺少原版受击结果后的消费入口。'
}

$areaColorPatchText = Get-Content -LiteralPath $areaColorPatchPath -Raw -Encoding UTF8
if (($areaColorPatchText -notmatch 'DamageWorker_AddInjury') -or ($areaColorPatchText -notmatch 'ExplosionImpactRuntimeScope\.Current') -or ($areaColorPatchText -notmatch '(?s)__result\.wounded.*?HitFeedbackColorRuntime\.Register')) {
    throw '范围伤害没有在原版确认 Pawn 实际受伤后消费减益模块颜色。'
}

Write-Output 'RangedHitFeedbackCompletionBoundarySmokeTests PASS'
