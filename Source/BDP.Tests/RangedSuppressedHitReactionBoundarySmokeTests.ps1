$ErrorActionPreference = 'Stop'

$modRoot = Join-Path $PSScriptRoot '..\..'
$projectilePath = Join-Path $modRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
if (-not (Test-Path -LiteralPath $projectilePath)) {
    throw ('缺少 BDP 投射物宿主：' + $projectilePath)
}

$text = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8
if ($text -notmatch '(?s)ApplySuppressedHitFeedback\(\s*hit\.HitThing,') {
    throw '抑制伤害的直接命中路径没有独立保留投射物受击抖动。'
}

if ($text -notmatch '(?s)internal void ApplySuppressedHitFeedback\(\s*Thing hitThing') {
    throw '缺少独立的投射物受击反馈方法。'
}

if ($text -notmatch 'Drawer\??\.Notify_DamageApplied\(') {
    throw '抑制伤害的直接命中路径没有独立保留 Pawn 受击抖动反馈。'
}

Write-Output 'RangedSuppressedHitReactionBoundarySmokeTests PASS'
