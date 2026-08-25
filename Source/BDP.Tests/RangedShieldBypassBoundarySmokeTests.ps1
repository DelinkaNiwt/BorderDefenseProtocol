$ErrorActionPreference = 'Stop'

$sourceRoot = Join-Path $PSScriptRoot '..\BDP\Core'
$corePolicyPath = Join-Path $sourceRoot 'Projectiles\Interaction\ProjectileInteractionPolicy.cs'
$interceptorPatchPath = Join-Path $PSScriptRoot '..\BDP\Patches\Patch_CompProjectileInterceptor_BdpPolicy.cs'
$contentShieldPath = Join-Path $PSScriptRoot '..\BDP.Content\Shield\Patch_CompShield_BdpPolicy.cs'
$bdpShieldPath = Join-Path $PSScriptRoot '..\BDP.Content\Shield\Patch_Pawn_PreApplyDamage_EnergyShield.cs'

$policy = Get-Content -LiteralPath $corePolicyPath -Raw -Encoding UTF8
$interceptor = Get-Content -LiteralPath $interceptorPatchPath -Raw -Encoding UTF8
$contentShield = Get-Content -LiteralPath $contentShieldPath -Raw -Encoding UTF8
$bdpShield = Get-Content -LiteralPath $bdpShieldPath -Raw -Encoding UTF8

foreach ($member in @('BypassProjectileInterceptors', 'BypassRegisteredDamageShields')) {
    if ($policy -notmatch ('\b' + $member + '\b')) {
        throw ('交互策略缺少成员：' + $member)
    }
}

if ($interceptor -notmatch 'CompProjectileInterceptor' -or $interceptor -notmatch 'CurrentInteractionPolicy') {
    throw '原版投射物拦截器没有接入 BDP 冻结策略。'
}

if ($contentShield -notmatch 'CompShield' -or $contentShield -notmatch 'BypassRegisteredDamageShields') {
    throw '原版伤害护盾适配器没有接入 BDP 冻结策略。'
}

if ($bdpShield -notmatch 'BypassRegisteredDamageShields') {
    throw 'BDP 自有能量护盾没有接入 BDP 冻结策略。'
}

Write-Output 'RangedShieldBypassBoundarySmokeTests PASS'
