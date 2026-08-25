$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $modRoot 'Source\BDP\Core'
$configPath = Join-Path $coreRoot 'Expressions\Config\ExpressionVisualEastWestPoseConfig.cs'
$resolverPath = Join-Path $coreRoot 'Trigger\Visual\VisualPoseResolver.cs'
$resolvedPosePath = Join-Path $coreRoot 'Trigger\Visual\ResolvedVisualPose.cs'
$resolvedOverlayPath = Join-Path $coreRoot 'Trigger\Visual\ResolvedVisualOverlayPose.cs'
$drawPatchPath = Join-Path $modRoot 'Source\BDP\Patches\Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs'

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding UTF8
$resolvedPoseText = Get-Content -LiteralPath $resolvedPosePath -Raw -Encoding UTF8
$resolvedOverlayText = Get-Content -LiteralPath $resolvedOverlayPath -Raw -Encoding UTF8
$drawPatchText = Get-Content -LiteralPath $drawPatchPath -Raw -Encoding UTF8

# 新策略必须默认关闭，避免改变既有武器的东西前后手规律。
Assert-True ($configText -match 'public bool MainHandAlwaysFront = false;') `
    '东西姿态必须提供默认关闭的主手固定前景能力。'
Assert-True ($resolverText -match 'pose\.MainHandAlwaysFront\s*\?\s*!isSubHand') `
    '主手固定前景必须只在作者显式开启时接管前后景裁定。'

# 最终镜像只看手位是显式可选策略；默认不得改变其它武器的东西镜像行为。
Assert-True ($configText -match 'public bool FinalMirrorByHandOnly = false;') `
    '东西姿态必须提供默认关闭的最终手位镜像策略。'
Assert-True ($resolverText -match 'pose\.FinalMirrorByHandOnly\s*\?\s*isSubHand \^ facingWest') `
    '最终手位镜像策略必须把朝西基础镜像纳入额外翻转裁决。'

# 东西基础瞄准已经负责西向镜像；手侧镜像只需给副手额外翻转一次。
Assert-True ($resolverText -match 'bool handMirror = pose\.HandMirror') `
    '东西姿态必须继续由统一手侧镜像裁决控制额外翻转。'
Assert-True ($resolverText -match 'HandMirror = handMirror') `
    '东西姿态必须把副手镜像裁定传给统一角度解析器。'
Assert-True ($resolverText -match 'ForceHandMirror = pose\.HandMirror') `
    '固定东西瞄准角也必须允许作者声明的副手镜像生效。'

# 多朝向材质必须在姿态解析阶段完成，正式绘制不得退回固定 South 材质。
Assert-True (($resolvedPoseText -match 'public Material DrawMaterial') -and
    ($resolvedOverlayText -match 'public Material DrawMaterial')) `
    '主姿态和附加层姿态必须携带最终方向材质。'
Assert-True ($resolverText -match 'graphic\.MatAt\(facing, sourceThing\)') `
    '完整姿态必须按人物朝向解析 Graphic_Multi 材质。'
Assert-True (($drawPatchText -match 'pose\.DrawMaterial') -and
    ($drawPatchText -match 'overlay\.DrawMaterial')) `
    '正式绘制必须使用姿态解析出的主贴图与附加层方向材质。'

Write-Output 'DirectionalVisualMaterialSmokeTests PASS'
