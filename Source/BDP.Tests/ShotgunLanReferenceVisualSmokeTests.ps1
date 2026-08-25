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
$sourceTexturePath = Join-Path $workspaceRoot '参考资源\通用资源\占位贴图\测试图-岚.png'
$targetTexturePath = Join-Path $repoRoot '1.6\Textures\Things\Trigger\Visual\ShotgunReferenceLan.png'
$visualDefsPath = Join-Path $repoRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$armamentFormDefsPath = Join-Path $repoRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'

Assert-True (Test-Path -LiteralPath $sourceTexturePath) '岚参考源贴图必须存在。'
Assert-True (Test-Path -LiteralPath $targetTexturePath) '模组内必须部署独立的岚参考贴图。'
Assert-True (
    (Get-FileHash -Algorithm SHA256 -LiteralPath $sourceTexturePath).Hash -eq
    (Get-FileHash -Algorithm SHA256 -LiteralPath $targetTexturePath).Hash
) '模组内岚参考贴图必须与源图完全一致。'

Add-Type -AssemblyName System.Drawing
$bitmap = [System.Drawing.Bitmap]::FromFile($targetTexturePath)
try {
    Assert-True ($bitmap.Width -eq 512 -and $bitmap.Height -eq 512) '岚参考贴图必须保持 512 × 512。'
}
finally {
    $bitmap.Dispose()
}

[xml]$visualDefs = Get-Content -Raw -Encoding utf8 -LiteralPath $visualDefsPath
$singlePreset = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='BDP_Visual_Shotgun']")
$dualPreset = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='BDP_Visual_Shotgun_Dual']")
$referenceDualPreset = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName='BDP_Visual_RangedWeaponReference_Dual']")
$mediumBase = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedMedium']")
$mediumDualBase = $visualDefs.SelectSingleNode(
    "/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[@Name='BDP_VisualBase_RangedMedium_Dual']")

Assert-True ($null -ne $singlePreset) '必须保留霰弹枪单武器视觉预设。'
Assert-True (
    $singlePreset.ParentName -eq 'BDP_VisualBase_RangedMedium' -and
    $singlePreset.GraphicData.texPath -eq 'Things/Trigger/Visual/ShotgunReferenceLan' -and
    $singlePreset.GraphicData.graphicClass -eq 'Graphic_Single' -and
    $null -eq $singlePreset.SouthNorthPose -and
    $null -eq $singlePreset.EastWestPose -and
    $null -eq $singlePreset.Grip -and
    $null -eq $singlePreset.Muzzle
) '霰弹枪单武器预设必须只覆盖贴图，并继承保持原版姿态的中型枪械基准。'

Assert-True ($null -ne $dualPreset) '必须定义霰弹枪独立双武器视觉预设。'
Assert-True ($null -ne $referenceDualPreset) '必须保留突击步枪使用的双武器参考预设。'
Assert-True ($null -ne $mediumBase -and $null -ne $mediumDualBase) '必须保留中型枪械单／双武器抽象基准。'
Assert-True (
    $dualPreset.ParentName -eq 'BDP_VisualBase_RangedMedium_Dual' -and
    $referenceDualPreset.ParentName -eq 'BDP_VisualBase_RangedMedium_Dual' -and
    $dualPreset.GraphicData.texPath -eq 'Things/Trigger/Visual/ShotgunReferenceLan' -and
    $dualPreset.GraphicData.graphicClass -eq $referenceDualPreset.GraphicData.graphicClass -and
    $null -eq $dualPreset.SouthNorthPose -and
    $null -eq $dualPreset.EastWestPose -and
    $null -eq $dualPreset.Grip -and
    $null -eq $dualPreset.Muzzle -and
    $null -eq $dualPreset.StageVisuals -and
    $mediumBase.Muzzle.MuzzleOffset -eq '(0, 0, 0.48828125)' -and
    $mediumDualBase.SouthNorthPose.DefaultOffset -eq '(0.24, 0, 0.03)'
) '霰弹枪双武器预设必须只覆盖贴图，共用中型枪械双持姿态和锚点，且不继承突击步枪阶段试验。'

[xml]$armamentFormDefs = Get-Content -Raw -Encoding utf8 -LiteralPath $armamentFormDefsPath
$shotgun = $armamentFormDefs.SelectSingleNode(
    "/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef[defName='BDP_GunClass_Shotgun']")
$assaultRifle = $armamentFormDefs.SelectSingleNode(
    "/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef[defName='BDP_GunClass_AssaultRifle']")

Assert-True ($null -ne $shotgun) '必须保留霰弹枪枪壳预设。'
Assert-True (
    $shotgun.overrides.visualPresetDefName -eq 'BDP_Visual_Shotgun' -and
    $shotgun.overrides.compositeVisualPresetDefName -eq 'BDP_Visual_Shotgun_Dual'
) '霰弹枪枪壳必须分别绑定岚参考单武器和双武器视觉。'
Assert-True (
    $assaultRifle.overrides.visualPresetDefName -eq 'BDP_Visual_RangedWeaponReference' -and
    $assaultRifle.overrides.compositeVisualPresetDefName -eq 'BDP_Visual_RangedWeaponReference_Dual'
) '突击步枪枪壳视觉绑定不得被霰弹枪试验改动。'

Write-Output 'ShotgunLanReferenceVisualSmokeTests PASS'
