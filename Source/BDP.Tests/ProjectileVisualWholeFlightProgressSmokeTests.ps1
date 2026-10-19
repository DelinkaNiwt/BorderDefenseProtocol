# BDP 投射物整发高角视觉进度冒烟测试。
# 该测试锁定物理路径进度与整发视觉进度必须分离，避免续飞路径重复播放高角弧线。

$ErrorActionPreference = "Stop"

function Assert-True
{
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition)
    {
        throw $Message
    }
}

function Read-Source
{
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8
}

$modRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$coreRoot = Join-Path $modRoot "Source\BDP\Core"
$profilePath = Join-Path $coreRoot "Projectiles\Visual\ProjectileVisualTrajectoryProfile.cs"
$projectilePath = Join-Path $coreRoot "Projectiles\BdpProjectile.cs"

Assert-True (Test-Path -LiteralPath $profilePath) "缺少投射物视觉轨迹求值文件。"
Assert-True (Test-Path -LiteralPath $projectilePath) "缺少 BDP 投射物宿主。"

$profileSource = Read-Source $profilePath
$projectileSource = Read-Source $projectilePath

# 契约：当前物理路径的进度与整发视觉弧线的进度必须是两个独立输入。
Assert-True (
    ($profileSource -match "float\s+pathProgress") -and
    ($profileSource -match "float\s+trajectoryProgress")
) "视觉轨迹求值必须区分当前物理路径进度和整发视觉进度。"

# 契约：水平基准沿用当前物理段，高角偏移只能使用整发进度。
Assert-True (
    ($profileSource -match "EvaluateBasePosition\([\s\S]*?pathProgress") -and
    ($profileSource -match "EvaluateOffset\([\s\S]*?trajectoryProgress")
) "视觉轨迹必须让水平位置使用物理段进度，让高角偏移使用整发进度。"

# 契约：投射物宿主必须提供不会随续飞重置的整发视觉进度。
Assert-True (
    ($projectileSource -match "ResolveWholeFlightVisualProgress") -and
    ($projectileSource -match "ResolveCurrentFlightProgress")
) "投射物宿主必须同时提供当前段进度和整发视觉进度。"

# 契约：绘制和视觉采样都必须消费整发视觉进度，不能只把当前段进度传给高角求值。
Assert-True (
    ($projectileSource -match "ProjectileVisualTrajectoryUtility\.Evaluate\([\s\S]*?ResolveCurrentFlightProgress\(\)[\s\S]*?ResolveWholeFlightVisualProgress\(\)")
) "绘制入口必须同时传入当前段进度和整发视觉进度。"
Assert-True (
    ($projectileSource -match "ResolveVisualFlightSamplePoint\([\s\S]*?ResolveWholeFlightVisualProgress\(\)")
) "视觉采样入口必须使用整发视觉进度。"

# 契约：路径重绑定只是物理生命周期操作，不得重置整发高角视觉进度。
$bindStart = $projectileSource.IndexOf("private void BindFlightPathSnapshot(")
$bindEnd = $projectileSource.IndexOf("private ProjectileFlightPathSnapshot AlignFlightPathSnapshotStart(", $bindStart)
$bindSource = if ($bindStart -ge 0 -and $bindEnd -gt $bindStart) {
    $projectileSource.Substring($bindStart, $bindEnd - $bindStart)
} else {
    ""
}
Assert-True ($bindSource -notmatch "ResolveWholeFlightVisualProgress|wholeFlightVisualProgress\s*=\s*0") "绑定新物理路径不得重置整发高角视觉进度。"

Write-Output "ProjectileVisualWholeFlightProgressSmokeTests PASS"
