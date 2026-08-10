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

function Read-Source {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modRoot = Split-Path -Parent $repoRoot
$devHarnessRoot = Join-Path $modRoot 'BorderDefenseProtocol.DevHarness'
$pathBuilderPath = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples\TrackingPathBuilder.cs'
$pathBuilderText = Read-Source $pathBuilderPath

# 契约:追踪路径构建的起手方向必须直接沿用当前飞行朝向,保证段边界切线连续,
# 重定向折角只可能来自段内曲线,不再来自段与段的交接点。
Assert-True (
    $pathBuilderText -match 'Vector3 launchDir = NormalizeFlat\(forward\);'
) 'Tracking pursuit curve must start along the current flight forward, keeping segment-boundary tangents continuous.'

# 契约:起手方向不得再与目标方向混合(旧实现 Lerp(limitedForward, chaseDir) 会在段边界产生折角)。
Assert-True (
    $pathBuilderText -notmatch 'launchDir = NormalizeFlat\(Vector3\.Lerp\(limitedForward, chaseDir'
) 'Tracking pursuit curve must not blend the segment-start direction toward the target, which caused visible corner folds at segment boundaries.'

# 契约:渐进转向限制仍需保留——曲线终点方向继续受 maxTurnAngle 约束(段内平滑转向,多段拼接成连续弧线)。
Assert-True (
    $pathBuilderText -match 'ComputeProgressiveTurnDirection\(\s*launchDir,\s*toTargetDir'
) 'Progressive turn limiting must stay inside the segment (arrival direction), so large re-targets curve smoothly over multiple segments.'

# 契约:重新锁定路径(relock,急促转向场景)与常规追踪共用同一套平滑路径构建。
Assert-True (
    ($pathBuilderText -match 'public static ProjectileFlightPathSnapshot BuildRelockPath') -and
    ($pathBuilderText -match 'BuildPursuitCurve\(')
) 'Relock paths must reuse the same smooth pursuit-curve builder.'

Write-Output 'TrackingSegmentBoundaryContinuitySmokeTests PASS'
