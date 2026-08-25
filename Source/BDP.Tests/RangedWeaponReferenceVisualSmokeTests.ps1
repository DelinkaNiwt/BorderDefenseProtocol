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
$workspaceRoot = Split-Path -Parent (Split-Path -Parent $repoRoot)
$sourceTexturePath = Join-Path $workspaceRoot '参考资源\通用资源\占位贴图\远程武器测试图.png'
$targetTexturePath = Join-Path $repoRoot '1.6\Textures\Things\Trigger\Visual\RangedWeaponReference.png'
$visualDefsPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$armamentFormDefsPath = Join-Path $repoRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'

Assert-True (Test-Path -LiteralPath $targetTexturePath) '远程武器基准参考贴图必须存在。'
Assert-True (
    (Get-FileHash -Algorithm SHA256 -LiteralPath $sourceTexturePath).Hash -eq
    (Get-FileHash -Algorithm SHA256 -LiteralPath $targetTexturePath).Hash
) '模组内参考贴图必须与已确认的源图完全一致。'

Add-Type -AssemblyName System.Drawing
$bitmap = [System.Drawing.Bitmap]::FromFile($targetTexturePath)
try {
    Assert-True ($bitmap.Width -eq 512 -and $bitmap.Height -eq 512) '远程武器基准参考贴图必须保持 512 × 512。'
}
finally {
    $bitmap.Dispose()
}

[xml]$visualDefs = Get-Content -Raw -Encoding utf8 -LiteralPath $visualDefsPath
$mediumBase = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedMedium']")
$mediumDualBase = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedMedium_Dual']")
$preset = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='BDP_Visual_RangedWeaponReference']")
Assert-True ($null -ne $mediumBase) '必须定义中型枪械单武器抽象视觉基准。'
Assert-True ($null -ne $mediumDualBase) '必须定义中型枪械双武器抽象视觉基准。'
Assert-True ($null -ne $preset) '必须定义 BDP_Visual_RangedWeaponReference 视觉预设。'
Assert-True (
    $preset.ParentName -eq 'BDP_VisualBase_RangedMedium' -and
    $preset.GraphicData.texPath -eq 'Things/Trigger/Visual/RangedWeaponReference' -and
    $preset.GraphicData.graphicClass -eq 'Graphic_Single'
) '基准视觉预设必须引用指定贴图。'
Assert-True (
    $null -eq $preset.GraphicData.drawSize -and
    $null -eq $preset.DrawScale -and
    $null -eq $preset.SouthNorthPose -and
    $null -eq $preset.EastWestPose -and
    $null -eq $preset.Grip -and
    $null -eq $preset.Muzzle
) '具体单武器预设必须只覆盖贴图，公共缩放、锚点和姿态由中型枪械基准提供。'

$dualPreset = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='BDP_Visual_RangedWeaponReference_Dual']")
Assert-True ($null -ne $dualPreset) '必须定义双武器专用的远程武器基准视觉预设。'
Assert-True (
    $dualPreset.ParentName -eq 'BDP_VisualBase_RangedMedium_Dual' -and
    $dualPreset.GraphicData.texPath -eq $preset.GraphicData.texPath -and
    $dualPreset.GraphicData.graphicClass -eq $preset.GraphicData.graphicClass -and
    $null -eq $dualPreset.SouthNorthPose -and
    $null -eq $dualPreset.EastWestPose -and
    $null -eq $dualPreset.Grip -and
    $null -eq $dualPreset.Muzzle
) '具体双武器预设必须只覆盖贴图，公共双持姿态和锚点由中型枪械双武器基准提供。'
Assert-True (
    $mediumDualBase.ParentName -eq 'BDP_VisualBase_RangedMedium' -and
    $mediumDualBase.SouthNorthPose.DefaultOffset -eq '(0.24, 0, 0.03)' -and
    $mediumDualBase.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true' -and
    $null -eq $mediumDualBase.SouthNorthPose.DecorativeAngleOnlyWhenIdle -and
    $null -eq $mediumDualBase.SouthNorthPose.DefaultAngle -and
    $mediumDualBase.EastWestPose.SideBaseX -eq '0.04' -and
    $mediumDualBase.EastWestPose.SideBaseZ -eq '0.03' -and
    $mediumDualBase.EastWestPose.SideDeltaX -eq '0.03' -and
    $mediumDualBase.EastWestPose.SideDeltaZ -eq '0.10' -and
    $mediumDualBase.EastWestPose.FrontAltitudeOffset -eq '0.05' -and
    $mediumDualBase.EastWestPose.BackAltitudeOffset -eq '-0.05' -and
    $mediumDualBase.Grip.UseAsPoseOrigin -eq 'true'
) '中型枪械双武器基准必须保存已确认的四朝向姿态和握持定位。'
Assert-True (
    $mediumBase.Muzzle.IsRangedWeapon -eq 'true' -and
    $mediumBase.Muzzle.MuzzleOffset -eq '(0, 0, 0.48828125)' -and
    $null -eq $mediumBase.Muzzle.HasSubHandMuzzleOffsetOverride -and
    $null -eq $mediumBase.Muzzle.SubHandMuzzleOffsetOverride -and
    $null -eq $mediumBase.Muzzle.ExtraWorldOffset
) '枪口必须位于矩形右边缘中心，其他枪口值保持默认。'
Assert-True (
    $mediumBase.DrawScale -eq '1' -and
    $mediumBase.Grip.GripOffset -eq '(0, 0, -0.1953125)'
) '握持锚点必须位于枪尾向前 30% 的中心线上。'

[xml]$armamentFormDefs = Get-Content -Raw -Encoding utf8 -LiteralPath $armamentFormDefsPath
$assaultRifle = $armamentFormDefs.SelectSingleNode(
    "/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef[defName='BDP_GunClass_AssaultRifle']")
Assert-True ($null -ne $assaultRifle) '必须保留突击步枪枪壳预设。'
Assert-True (
    $assaultRifle.overrides.visualPresetDefName -eq 'BDP_Visual_RangedWeaponReference' -and
    $assaultRifle.overrides.compositeVisualPresetDefName -eq 'BDP_Visual_RangedWeaponReference_Dual'
) '突击步枪枪壳必须分别使用单武器基准视觉和双武器专用基准视觉。'

Write-Output 'RangedWeaponReferenceVisualSmokeTests PASS'
