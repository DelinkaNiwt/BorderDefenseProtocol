$ErrorActionPreference = 'Stop'

# 断言指定条件成立。
function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$resolverPath = Join-Path $sourceRoot 'BDP\Core\Trigger\Visual\VisualPoseResolver.cs'
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'

$resolverText = Get-Content -Raw -Encoding utf8 -LiteralPath $resolverPath
$transformMethod = [regex]::Match(
    $resolverText,
    '(?s)private static Vector3 TransformGraphicLocalOffset\(.*?\r?\n        \}')

Assert-True (-not [string]::IsNullOrWhiteSpace($transformMethod.Value)) `
    '必须保留统一的贴图局部锚点变换。'
Assert-True (
    ($transformMethod.Value -match 'Graphic graphic') -and
    ($transformMethod.Value -match 'float drawScale') -and
    ($transformMethod.Value -match 'localOffset\.z\s*\*\s*drawSize\.x\s*\*\s*safeScale') -and
    ($transformMethod.Value -match 'localOffset\.x\s*\*\s*drawSize\.y\s*\*\s*safeScale')
) '锚点贴图平面坐标必须按实际贴图宽高和绘制缩放换算，纵向世界高度保持独立。'

Assert-True (
    ($resolverText -match 'AlignDrawPositionToGrip\(request, calculation, graphic, drawScale\)') -and
    ($resolverText -match 'GripAnchor = ResolveGripAnchor\(request, calculation, graphic, drawScale\)') -and
    ($resolverText -match 'MuzzleAnchor = ResolveMuzzleAnchor\(request, calculation, graphic, drawScale\)') -and
    ($resolverText -match 'MuzzleAnchor = ResolveMuzzleAnchor\(request, calculation, graphic, 1f\)')
) '握持点对齐、握持锚点和枪口锚点必须使用正在绘制的主贴图及其实际缩放。'

[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding utf8
$fujin = @($visualXml.Defs.ChildNodes | Where-Object { $_.defName -eq 'BDP_Visual_Fujin' })[0]
Assert-True ($null -ne $fujin) '必须找到风刃视觉预设。'
Assert-True (
    ($fujin.GraphicData.drawSize -eq '(1.2, 1.2)') -and
    ($fujin.Grip.GripOffset -eq '(0.15625, 0, -0.1)')
) '风刃握持锚点应使用归一化贴图坐标，同时保持 1.2 绘制尺寸下的既有位置。'

$normalizedGrip = @($fujin.Grip.GripOffset.Trim('(', ')').Split(',') | ForEach-Object { [single]::Parse($_.Trim(), [Globalization.CultureInfo]::InvariantCulture) })
$preservedImageX = $normalizedGrip[0] * [single]1.2
$preservedImageZ = $normalizedGrip[2] * [single]1.2
Assert-True (
    ([Math]::Abs($preservedImageX - [single]0.1875) -lt 0.00001) -and
    ([Math]::Abs($preservedImageZ - [single]-0.12) -lt 0.00001)
) '风刃握持锚点迁移后，在原绘制尺寸下必须保持原来的贴图位置。'

Write-Output 'VisualAnchorGraphicScaleSmokeTests PASS'
