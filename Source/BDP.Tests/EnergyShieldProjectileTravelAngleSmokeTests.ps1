$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$policyPath = Join-Path $sourceRoot 'BDP.Content\Shield\EnergyShieldBlockPolicy.cs'
$projectilePath = Join-Path $sourceRoot 'BDP\Core\Projectiles\BdpProjectile.cs'

$policyText = Get-Content -LiteralPath $policyPath -Raw -Encoding UTF8
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding UTF8

Assert-True (
    $policyText -match 'if\s*\(damageInfo\.Def\s*!=\s*null\s*&&\s*damageInfo\.Def\.isRanged\)\s*\{\s*return false;'
) '远程伤害必须优先排除触发体武器类型带来的近战误判。'

Assert-True (
    ($policyText -match 'IsMeleeDamage\(damageInfo\)') -and
    ($policyText -match 'return damageInfo\.Angle;')
) '护盾方向解析必须让非近战伤害继续使用 DamageInfo.Angle（伤害方向）。'

Assert-True (
    $projectileText -match 'ExactRotation\.eulerAngles\.y'
) 'BDP 投射物必须把当前实际飞行朝向写入 DamageInfo.Angle（伤害方向）。'

Write-Output 'EnergyShieldProjectileTravelAngleSmokeTests PASS'
