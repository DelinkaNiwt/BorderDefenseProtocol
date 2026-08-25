$ErrorActionPreference = 'Stop'

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
$poseResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\VisualPoseResolver.cs'
$eastWestPoseConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualEastWestPoseConfig.cs'
$poseResolverText = Get-Content -LiteralPath $poseResolverPath -Raw -Encoding utf8
$eastWestPoseConfigText = Get-Content -LiteralPath $eastWestPoseConfigPath -Raw -Encoding utf8

Assert-True (
    $poseResolverText -match 'float signX = sample\.Facing == Rot4\.South \? -1f : 1f;'
) 'South（南向）时主手必须投影到玩家左侧，North（北向）时自动反转到玩家右侧。'

Assert-True (
    ($poseResolverText -match 'float sideDistanceX = Mathf\.Abs\(pose\.DefaultOffset\.x\);') -and
    ($poseResolverText -match 'float finalX = \(isSubHand \? -sideDistanceX : sideDistanceX\) \* signX;')
) '南北朝向必须把配置解释为无方向分离距离，再由主副手和人物朝向唯一确定屏幕左右。'

Assert-True (
    ($poseResolverText -match '&& \(isSubHand \^ sample\.Facing == Rot4\.South\);') -and
    ($poseResolverText -notmatch '&& \(isSubHand \^ sample\.Facing == Rot4\.North\);')
) '南北朝向必须镜像屏幕左侧武器：South（南向）镜像主手，North（北向）镜像副手。'

Assert-True (
    ($eastWestPoseConfigText -match 'public bool MainHandAlwaysFront = false;') -and
    ($poseResolverText -match 'pose\.MainHandAlwaysFront\s*\?\s*!isSubHand\s*:\s*facingWest \? isSubHand : !isSubHand;')
) '东西姿态默认必须保持 East（东向）主手、West（西向）副手处于前景；作者显式开启时才允许主手恒用前景。'

Assert-True (
    $poseResolverText -match 'float finalX = signBase \* \(pose\.SideBaseX \+ xDelta\);'
) '东西朝向的前后手 X 分离必须随 East/West 一起反转，保证前景手始终更靠近人物。'

Assert-True (
    ($eastWestPoseConfigText -match 'public float SideBaseZ = 0f;') -and
    ($poseResolverText -match 'float finalZ = pose\.SideBaseZ \+ \(isFront \? -pose\.SideDeltaZ : pose\.SideDeltaZ\);')
) '东西朝向必须在共同 Z 基准上叠加前后手透视差。'

Write-Output 'DualWeaponHandProjectionSmokeTests PASS'
