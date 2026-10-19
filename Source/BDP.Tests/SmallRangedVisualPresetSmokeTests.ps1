# 小型枪械单持、双持视觉基类与左轮继承关系冒烟测试。

$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Equal {
    param([object]$Actual, [object]$Expected, [string]$Message)
    if ([string]$Actual -ne [string]$Expected) {
        throw "$Message 实际值：$Actual；期望值：$Expected。"
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$visualPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$armamentPath = Join-Path $repoRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'

Assert-True (Test-Path -LiteralPath $visualPath) '正式视觉预设 XML 必须存在。'
Assert-True (Test-Path -LiteralPath $armamentPath) '武装构型 XML 必须存在。'

[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding utf8
[xml]$armamentXml = Get-Content -LiteralPath $armamentPath -Raw -Encoding utf8
$visualType = 'BDP.Core.Expressions.ExpressionVisualPresetDef'
$formType = 'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef'

$mediumBase = $visualXml.SelectSingleNode(
    "/Defs/$visualType[@Name='BDP_VisualBase_RangedMedium']")
$smallBase = $visualXml.SelectSingleNode(
    "/Defs/$visualType[@Name='BDP_VisualBase_RangedSmall']")
$smallDualBase = $visualXml.SelectSingleNode(
    "/Defs/$visualType[@Name='BDP_VisualBase_RangedSmall_Dual']")
$pistol = $visualXml.SelectSingleNode(
    "/Defs/$visualType[defName='BDP_Visual_Pistol']")
$pistolDual = $visualXml.SelectSingleNode(
    "/Defs/$visualType[defName='BDP_Visual_Pistol_Dual']")

Assert-True ($null -ne $mediumBase) '中型枪械基类必须存在，作为同级关系参照。'
Assert-True ($null -ne $smallBase -and $null -ne $smallDualBase) '小型枪械单/双武器基类必须存在。'
Assert-Equal $smallBase.GetAttribute('Name') 'BDP_VisualBase_RangedSmall' `
    '小型枪械单武器基类名称不正确。'
Assert-Equal $smallBase.GetAttribute('Abstract') 'True' `
    '小型枪械单武器基类必须是抽象预设。'
Assert-True ($null -eq $smallBase.ParentName) `
    '小型枪械单武器基类必须与中型枪械基类同级，不能继承中型基类。'
Assert-Equal $smallBase.DrawScale '1' '小型枪械单武器基类必须保持标准缩放。'
Assert-Equal ([string]$smallBase.Grip.GripOffset).Trim() '(0, 0, -0.1953125)' `
    '小型枪械基类必须保留标准握持局部偏移。'
Assert-Equal ([string]$smallBase.Muzzle.IsRangedWeapon).Trim() 'true' `
    '小型枪械基类必须声明为远程枪口来源。'
Assert-Equal ([string]$smallBase.Muzzle.MuzzleOffset).Trim() '(0, 0, 0.3203125)' `
    '小型枪械基类必须使用小型枪口局部偏移。'
Assert-True ($null -eq $smallBase.SouthNorthPose -and $null -eq $smallBase.EastWestPose) `
    '小型枪械单武器基类不得新增双持姿态覆盖。'

Assert-Equal $smallDualBase.GetAttribute('Name') 'BDP_VisualBase_RangedSmall_Dual' `
    '小型枪械双武器基类名称不正确。'
Assert-Equal $smallDualBase.GetAttribute('Abstract') 'True' `
    '小型枪械双武器基类必须是抽象预设。'
Assert-Equal $smallDualBase.ParentName 'BDP_VisualBase_RangedSmall' `
    '小型枪械双武器基类必须继承小型枪械单武器基类。'
Assert-Equal $smallDualBase.SouthNorthPose.DefaultOffset '(0.24, 0, 0.13)' `
    '小型枪械南北握持目标必须按最新白圈方向向内并保持上移。'
Assert-Equal $smallDualBase.SouthNorthPose.NorthZAdjust '0.26' `
    '小型枪械朝北补偿必须保持南北握持目标对称。'
Assert-Equal $smallDualBase.SouthNorthPose.HandMirrorOnlyWhenIdle 'true' `
    '小型枪械双持必须保留静默状态手侧镜像规则。'
Assert-Equal $smallDualBase.EastWestPose.SideBaseX '0.04' `
    '小型枪械双持东西共同横向偏移必须沿用中型基准。'
Assert-Equal $smallDualBase.EastWestPose.SideBaseZ '0.03' `
    '小型枪械双持东西共同纵向偏移必须沿用中型基准。'
Assert-Equal $smallDualBase.EastWestPose.SideDeltaX '0.03' `
    '小型枪械双持东西横向透视差必须沿用中型基准。'
Assert-Equal $smallDualBase.EastWestPose.SideDeltaZ '0.10' `
    '小型枪械双持东西纵向透视差必须沿用中型基准。'
Assert-Equal $smallDualBase.EastWestPose.FrontAltitudeOffset '0.05' `
    '小型枪械双持东西前景层级必须沿用中型基准。'
Assert-Equal $smallDualBase.EastWestPose.BackAltitudeOffset '-0.05' `
    '小型枪械双持东西背景层级必须沿用中型基准。'
Assert-Equal $smallDualBase.Grip.UseAsPoseOrigin 'true' `
    '小型枪械双持必须使用握持点作为姿态原点。'
Assert-True ($null -eq $smallDualBase.Grip.GripOffset -and $null -eq $smallDualBase.Muzzle) `
    '小型枪械双持基类不得重复复制单持握持和枪口数据。'

Assert-True ($null -ne $pistol -and $null -ne $pistolDual) '左轮单/双武器预设必须存在。'
Assert-Equal $pistol.ParentName 'BDP_VisualBase_RangedSmall' `
    '左轮单武器视觉必须继承小型枪械单武器基类。'
Assert-Equal $pistolDual.ParentName 'BDP_VisualBase_RangedSmall_Dual' `
    '左轮双武器视觉必须继承小型枪械双武器基类。'
Assert-True ($null -eq $pistol.Muzzle -and $null -eq $pistolDual.Muzzle) `
    '左轮预设必须消费小型基类枪口数据，不得重复声明。'

$revolver = $armamentXml.SelectSingleNode(
    "/Defs/$formType[defName='BDP_ArmamentForm_Revolver']")
Assert-True ($null -ne $revolver) '左轮手枪构型必须存在。'
Assert-Equal $revolver.overrides.visualPresetDefName 'BDP_Visual_Pistol' `
    '左轮构型单持视觉引用必须保持为左轮单持预设。'
Assert-Equal $revolver.overrides.compositeVisualPresetDefName 'BDP_Visual_Pistol_Dual' `
    '左轮构型双持视觉引用必须保持为左轮双持预设。'

Write-Output 'SmallRangedVisualPresetSmokeTests PASS'
