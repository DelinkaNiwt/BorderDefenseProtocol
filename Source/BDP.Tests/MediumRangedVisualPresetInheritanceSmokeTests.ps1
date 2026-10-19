$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$visualPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
[xml]$visualXml = Get-Content -Raw -Encoding utf8 -LiteralPath $visualPath

$singleBase = $visualXml.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedMedium']")
$dualBase = $visualXml.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedMedium_Dual']")
$smallBase = $visualXml.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedSmall']")
$smallDualBase = $visualXml.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedSmall_Dual']")

Assert-True (
    ($null -ne $singleBase) -and
    ($singleBase.Abstract -eq 'True') -and
    ($null -eq $singleBase.ParentName)
) '必须定义独立的中型枪械单武器抽象视觉基准。'
Assert-True (
    ($singleBase.DrawScale -eq '1') -and
    ($singleBase.Grip.GripOffset -eq '(0, 0, -0.1953125)') -and
    ($null -eq $singleBase.Grip.UseAsPoseOrigin) -and
    ($singleBase.Muzzle.IsRangedWeapon -eq 'true') -and
    ($singleBase.Muzzle.MuzzleOffset -eq '(0, 0, 0.48828125)') -and
    ($null -eq $singleBase.SouthNorthPose) -and
    ($null -eq $singleBase.EastWestPose)
) '中型枪械单武器基准必须只承载缩放、标准握持点和标准枪口点。'

Assert-True (
    ($null -ne $dualBase) -and
    ($dualBase.Abstract -eq 'True') -and
    ($dualBase.ParentName -eq 'BDP_VisualBase_RangedMedium')
) '中型枪械双武器抽象基准必须继承单武器基准。'
Assert-True (
    ($dualBase.SouthNorthPose.DefaultOffset -eq '(0.24, 0, 0.03)') -and
    ($dualBase.SouthNorthPose.NorthZAdjust -eq '0.06') -and
    ($null -eq $dualBase.SouthNorthPose.DefaultAngle) -and
    ($dualBase.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true') -and
    ($null -eq $dualBase.SouthNorthPose.DecorativeAngleOnlyWhenIdle) -and
    ($dualBase.EastWestPose.SideBaseX -eq '0.04') -and
    ($dualBase.EastWestPose.SideBaseZ -eq '0.03') -and
    ($dualBase.EastWestPose.SideDeltaX -eq '0.03') -and
    ($dualBase.EastWestPose.SideDeltaZ -eq '0.10') -and
    ($dualBase.EastWestPose.FrontAltitudeOffset -eq '0.05') -and
    ($dualBase.EastWestPose.BackAltitudeOffset -eq '-0.05') -and
    ($dualBase.Grip.UseAsPoseOrigin -eq 'true') -and
    ($null -eq $dualBase.Grip.GripOffset) -and
    ($null -eq $dualBase.Muzzle)
) '双武器基准必须只增加保留原版南北高度差的双持姿态、静默镜像与握持定位，不复制中型枪械锚点。'

Assert-True (
    ($null -ne $smallBase) -and
    ($smallBase.Abstract -eq 'True') -and
    ($null -eq $smallBase.ParentName) -and
    ($smallBase.GetAttribute('Name') -eq 'BDP_VisualBase_RangedSmall')
) '小型枪械单武器基准必须作为中型枪械基准的同级抽象预设。'
Assert-True (
    ($smallBase.DrawScale -eq '1') -and
    ($smallBase.Grip.GripOffset -eq '(0, 0, -0.1953125)') -and
    ($smallBase.Muzzle.IsRangedWeapon -eq 'true') -and
    ($smallBase.Muzzle.MuzzleOffset -eq '(0, 0, 0.3203125)') -and
    ($null -eq $smallBase.SouthNorthPose) -and
    ($null -eq $smallBase.EastWestPose)
) '小型枪械单武器基准必须提供小型枪口、握持点和原版姿态边界。'
Assert-True (
    ($null -ne $smallDualBase) -and
    ($smallDualBase.Abstract -eq 'True') -and
    ($smallDualBase.ParentName -eq 'BDP_VisualBase_RangedSmall') -and
    ($smallDualBase.GetAttribute('Name') -eq 'BDP_VisualBase_RangedSmall_Dual')
) '小型枪械双武器基准必须继承小型枪械单武器基准。'
Assert-True (
    ($smallDualBase.SouthNorthPose.DefaultOffset -eq '(0.24, 0, 0.13)') -and
    ($smallDualBase.SouthNorthPose.NorthZAdjust -eq '0.26') -and
    ($smallDualBase.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true') -and
    ($smallDualBase.EastWestPose.SideBaseX -eq '0.04') -and
    ($smallDualBase.EastWestPose.SideBaseZ -eq '0.03') -and
    ($smallDualBase.EastWestPose.SideDeltaX -eq '0.03') -and
    ($smallDualBase.EastWestPose.SideDeltaZ -eq '0.10') -and
    ($smallDualBase.EastWestPose.FrontAltitudeOffset -eq '0.05') -and
    ($smallDualBase.EastWestPose.BackAltitudeOffset -eq '-0.05') -and
    ($smallDualBase.Grip.UseAsPoseOrigin -eq 'true') -and
    ($null -eq $smallDualBase.Grip.GripOffset) -and
    ($null -eq $smallDualBase.Muzzle)
) '小型枪械双武器基准必须只改变南北握持目标，并沿用中型东西透视规则。'

$expectations = @(
    @{ DefName = 'BDP_Visual_AssaultRifle'; Parent = 'BDP_VisualBase_RangedMedium'; Texture = 'Things/Trigger/Visual/RangedWeaponReference'; AllowsStage = $false },
    @{ DefName = 'BDP_Visual_AssaultRifle_Dual'; Parent = 'BDP_VisualBase_RangedMedium_Dual'; Texture = 'Things/Trigger/Visual/RangedWeaponReference'; AllowsStage = $false },
    @{ DefName = 'BDP_Visual_Shotgun'; Parent = 'BDP_VisualBase_RangedMedium'; Texture = 'Things/Trigger/Visual/ShotgunReferenceLan'; AllowsStage = $false },
    @{ DefName = 'BDP_Visual_Shotgun_Dual'; Parent = 'BDP_VisualBase_RangedMedium_Dual'; Texture = 'Things/Trigger/Visual/ShotgunReferenceLan'; AllowsStage = $false }
)

foreach ($expected in $expectations) {
    $preset = $visualXml.SelectSingleNode(
        "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='$($expected.DefName)']")
    Assert-True ($null -ne $preset) "必须保留具体视觉预设 $($expected.DefName)。"
    Assert-True ($preset.ParentName -eq $expected.Parent) "$($expected.DefName) 必须继承正确的中型枪械基准。"
    Assert-True (
        ($preset.GraphicData.texPath -eq $expected.Texture) -and
        ($preset.GraphicData.graphicClass -eq 'Graphic_Single')
    ) "$($expected.DefName) 必须只覆盖自己的主贴图。"
    Assert-True (
        ($null -eq $preset.DrawScale) -and
        ($null -eq $preset.SouthNorthPose) -and
        ($null -eq $preset.EastWestPose) -and
        ($null -eq $preset.Grip) -and
        ($null -eq $preset.Muzzle)
    ) "$($expected.DefName) 不得重复声明中型枪械公共参数。"
    if (-not $expected.AllowsStage) {
        Assert-True ($null -eq $preset.StageVisuals) "$($expected.DefName) 不得继承突击步枪专属动作阶段试验。"
    }
}

Write-Output 'MediumRangedVisualPresetInheritanceSmokeTests PASS'
