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
$configPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualGripConfig.cs'
$presetPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionVisualPresetDef.cs'
$anchorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\ResolvedGripAnchor.cs'
$posePath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\ResolvedVisualPose.cs'
$resolverPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\VisualPoseResolver.cs'

Assert-True (Test-Path -LiteralPath $configPath) '握持锚点配置类型必须存在。'
Assert-True (Test-Path -LiteralPath $anchorPath) '已解算握持锚点类型必须存在。'

$configText = Get-Content -Raw -Encoding utf8 -LiteralPath $configPath
$presetText = Get-Content -Raw -Encoding utf8 -LiteralPath $presetPath
$anchorText = Get-Content -Raw -Encoding utf8 -LiteralPath $anchorPath
$poseText = Get-Content -Raw -Encoding utf8 -LiteralPath $posePath
$resolverText = Get-Content -Raw -Encoding utf8 -LiteralPath $resolverPath

Assert-True (
    ($configText -match 'public sealed class ExpressionVisualGripConfig') -and
    ($configText -match 'public Vector3 GripOffset') -and
    ($configText -match 'public bool UseAsPoseOrigin = false;') -and
    ($presetText -match 'public ExpressionVisualGripConfig Grip') -and
    ($presetText -match 'public ExpressionVisualGripConfig ResolveGrip\(\)')
) '视觉预设必须公开可选握持锚点配置。'

Assert-True (
    ($anchorText -match 'internal sealed class ResolvedGripAnchor') -and
    ($anchorText -match 'public bool IsValid') -and
    ($anchorText -match 'public Vector3 WorldPosition') -and
    ($anchorText -match 'public Vector3 LocalOffset') -and
    ($poseText -match 'public ResolvedGripAnchor GripAnchor')
) '最终视觉姿态必须携带已解算握持锚点。'

Assert-True (
    ($resolverText -match 'GripAnchor = ResolveGripAnchor\(request, calculation\)') -and
    ($resolverText -match 'ExpressionVisualGripConfig grip = request\.Preset\.ResolveGrip\(\)') -and
    ($resolverText -match 'TransformGraphicLocalOffset\(localOffset, calculation\)') -and
    ($resolverText -match 'calculation\.MeshKind == VisualMeshKind\.PlaneFlipped') -and
    ($resolverText -match 'Quaternion\.AngleAxis\(calculation\.DrawAngle, Vector3\.up\)') -and
    ($resolverText -match 'WorldPosition = calculation\.DrawPosition \+ worldOffset')
) '握持锚点必须跟随最终绘制角和网格镜像解算。'

Assert-True (
    ($resolverText -match 'AlignDrawPositionToGrip\(request, calculation\);') -and
    ($resolverText -match 'private static void AlignDrawPositionToGrip') -and
    ($resolverText -match 'grip == null \|\| !grip\.UseAsPoseOrigin') -and
    ($resolverText -match 'calculation\.DrawPosition -= TransformGraphicLocalOffset\(\s*grip\.GripOffset,\s*calculation\s*\)')
) '仅在显式开启时，解析器必须从目标握持位置反推出主贴图中心。'

Write-Output 'VisualGripAnchorSmokeTests PASS'
