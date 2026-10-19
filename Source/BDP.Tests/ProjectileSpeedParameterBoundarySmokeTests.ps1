$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$source = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP/Core/Projectiles/BdpProjectile.cs') -Raw -Encoding UTF8

# 参数层契约：不能继续靠每刻回补倒计时模拟低速，也不能留下旧补偿状态。
foreach ($obsolete in @('ApplySpeedFactorToFlight', 'speedTickRemainder')) {
    if ($source.Contains($obsolete)) { throw "投射物速度仍保留逐刻倒计时补偿：$obsolete" }
}
if ($source -notmatch 'public float SpeedTilesPerTick' -or
    $source -notmatch 'def\.projectile\.SpeedTilesPerTick\s*\*\s*speedFactor') {
    throw '投射物必须提供基础速度乘实例倍率的实际速度参数。'
}

# 首段、续段和读档重绑定必须共用按实际速度计算时长的入口。
$computeStart = $source.IndexOf('private float ComputeFlightPathStartingTicksToImpact(')
$resetStart = $source.IndexOf('private void ResetFlightDurationFromCurrentSegment()')
if ($computeStart -lt 0 -or $resetStart -le $computeStart) { throw '缺少路径时长计算入口。' }
$compute = $source.Substring($computeStart, $resetStart - $computeStart)
if ($compute -notmatch 'pathLength / SpeedTilesPerTick' -or $compute -match 'def\.projectile\.SpeedTilesPerTick') {
    throw '路径时长必须除以实例实际速度，不能继续除以原始定义速度。'
}
$reset = $source.Substring($resetStart, $source.IndexOf('private static string SafeDiagnosticId', $resetStart) - $resetStart)
if ($reset -notmatch 'ComputeFlightPathStartingTicksToImpact\(currentFlightPathSnapshot\)') {
    throw '初始化和续段必须复用同一速度计算，不能重复维护时长公式。'
}
$homing = Get-Content -LiteralPath (Join-Path $sourceRoot 'BDP.Content/RangedModules/Homing/HomingModule.cs') -Raw -Encoding UTF8
if ($homing -notmatch 'bdpProjectile\.SpeedTilesPerTick') { throw '制导预测必须读取同一投射物的实际速度。' }
Write-Output '通过：投射物速度参数、共用路径计时、旧逐刻补偿移除及制导预测速度边界。'
