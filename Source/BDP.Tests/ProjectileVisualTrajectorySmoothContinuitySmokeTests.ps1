# BDP 追踪弹高角视觉轨迹连续性冒烟测试。
# 锁定整发视觉进度在脱靶换段时必须继续单调推进，且高角弧线起点必须保留斜上切线。

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
$profilePath = Join-Path $modRoot "Source\BDP\Core\Projectiles\Visual\ProjectileVisualTrajectoryProfile.cs"
$projectilePath = Join-Path $modRoot "Source\BDP\Core\Projectiles\BdpProjectile.cs"

Assert-True (Test-Path -LiteralPath $profilePath) "缺少投射物视觉轨迹参数文件。"
Assert-True (Test-Path -LiteralPath $projectilePath) "缺少 BDP 投射物宿主。"

$profileSource = Read-Source $profilePath
$projectileSource = Read-Source $projectilePath

# 契约：高角弧线起点直接进入反抛物线，不能用 SmoothStep（平滑步进）把初始切线压成零。
Assert-True (
    ($profileSource -match "GenMath\.InverseParabola\(clampedProgress\)") -and
    ($profileSource -notmatch "Mathf\.SmoothStep")
) "高角视觉弧线起点必须保留非零切线，不能使用 SmoothStep（平滑步进）端点参数化。"

# 契约：整发进度必须保留上一次真实物理点，用累计飞行量跨过脱靶绕行与续段边界。
Assert-True ($projectileSource -match "wholeFlightVisualLastPhysicalPoint") "整发视觉进度必须保存上一次真实物理点。"
Assert-True ($projectileSource -match "hasWholeFlightVisualLastPhysicalPoint") "整发视觉进度必须有上一次真实物理点的初始化状态。"

$resolveStart = $projectileSource.IndexOf("private float ResolveWholeFlightVisualProgress()")
$resolveEnd = $projectileSource.IndexOf("private void CompleteWholeFlightVisualProgress()", $resolveStart)
$resolveSource = if ($resolveStart -ge 0 -and $resolveEnd -gt $resolveStart) {
    $projectileSource.Substring($resolveStart, $resolveEnd - $resolveStart)
} else {
    ""
}

Assert-True ($resolveSource -match "travelDistance") "整发视觉进度必须计算当前物理点之间的累计飞行距离。"
Assert-True (
    ($resolveSource -match "wholeFlightVisualStartToEndDistance") -and
    ($resolveSource -match "Mathf\.Max\(")
) "累计飞行量必须以整发起终点距离归一化，并与原始终点接近度合并为单调进度。"

Write-Output "ProjectileVisualTrajectorySmoothContinuitySmokeTests PASS"
