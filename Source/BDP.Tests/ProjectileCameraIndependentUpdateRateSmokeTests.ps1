$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$projectilePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8

Assert-True (
    $projectileText -match '(?s)public\s+override\s+int\s+UpdateRateTicks\s*\{\s*get\s*\{\s*return\s+launchPlan\s*!=\s*null\s*\?\s*1\s*:\s*base\.UpdateRateTicks\s*;\s*\}\s*\}'
) '已绑定 BDP 发射计划的投射物必须覆盖 UpdateRateTicks（更新间隔）为 1，并保留未绑定计划的原版回退。'

Assert-True (
    $projectileText -notmatch 'UpdateRateTicks[\s\S]{0,500}(Find\.CameraDriver|InViewOf)'
) 'UpdateRateTicks（更新间隔）策略不得再次读取镜头可见性。'

Write-Output 'ProjectileCameraIndependentUpdateRateSmokeTests PASS'
