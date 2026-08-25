$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\VisualPoseResolver.cs'
$muzzleConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualMuzzleConfig.cs'
$poseConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualSouthNorthPoseConfig.cs'

$resolverText = Get-Content -Raw -Encoding utf8 -LiteralPath $resolverPath
$muzzleConfigText = Get-Content -Raw -Encoding utf8 -LiteralPath $muzzleConfigPath
$poseConfigText = Get-Content -Raw -Encoding utf8 -LiteralPath $poseConfigPath
$muzzleMethod = [regex]::Match(
    $resolverText,
    '(?s)private static ResolvedMuzzleAnchor ResolveMuzzleAnchor\(.*?\r?\n        \}\r?\n\r?\n        /// <summary>').Value

Assert-True (-not [string]::IsNullOrWhiteSpace($muzzleMethod)) `
    '必须保留单一的枪口锚点解析成员。'
Assert-True (
    ($muzzleMethod -match 'TransformGraphicLocalOffset\(localOffset, calculation\)') -and
    ($muzzleMethod -notmatch 'Quaternion\.AngleAxis\(request\.PoseSample\.AimAngle') -and
    ($muzzleMethod -notmatch 'IsAimMirrored\(request\.PoseSample\.AimAngle\)')
) '枪口锚点必须复用最终贴图角度与网格镜像变换，不得继续走独立瞄准角公式。'

Assert-True (
    ($muzzleConfigText -match '最终贴图姿态') -and
    ($muzzleConfigText -notmatch '枪口偏移始终按 aimAngle') -and
    ($poseConfigText -notmatch '该角度只影响贴图旋转，不参与枪口偏移旋转')
) '枪口和装饰角的作者注释必须反映贴图、握持点与枪口共用最终姿态。'

Write-Output 'VisualMuzzleGraphicTransformSmokeTests PASS'
