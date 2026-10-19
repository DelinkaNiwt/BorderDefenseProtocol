# 左轮手枪单武器与双武器视觉配置冒烟测试。

$ErrorActionPreference = 'Stop'

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition)
    {
        throw $Message
    }
}

function Assert-Equal
{
    param([object]$Actual, [object]$Expected, [string]$Message)
    if ([string]$Actual -ne [string]$Expected)
    {
        throw "$Message 实际值：$Actual；期望值：$Expected。"
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$armamentPath = Join-Path $modRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$visualLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Core.Expressions.ExpressionVisualPresetDef\Pistol.xml'
$texturePath = Join-Path $modRoot '1.6\Textures\Things\Trigger\Visual\RevolverReference.png'

Assert-True (Test-Path -LiteralPath $armamentPath) '左轮武装构型 Def 文件必须存在。'
Assert-True (Test-Path -LiteralPath $visualPath) '正式视觉预设 XML 必须存在。'
Assert-True (Test-Path -LiteralPath $visualLanguagePath) '左轮双武器视觉中文语言包必须存在。'
Assert-True (Test-Path -LiteralPath $texturePath) '左轮目标贴图必须存在。'

[xml]$armamentXml = Get-Content -LiteralPath $armamentPath -Raw -Encoding utf8
[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding utf8
$visualLanguageText = Get-Content -LiteralPath $visualLanguagePath -Raw -Encoding utf8

$formType = 'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef'
$revolver = @($armamentXml.Defs.$formType) |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_Revolver' } |
    Select-Object -First 1
Assert-True ($null -ne $revolver) '左轮手枪武装型必须存在。'
Assert-Equal $revolver.overrides.visualPresetDefName 'BDP_Visual_Pistol' `
    '左轮单武器视觉预设绑定必须保持为 BDP_Visual_Pistol。'
Assert-Equal $revolver.overrides.compositeVisualPresetDefName 'BDP_Visual_Pistol_Dual' `
    '左轮双武器视觉预设绑定必须为 BDP_Visual_Pistol_Dual。'

$visualPresetType = 'BDP.Core.Expressions.ExpressionVisualPresetDef'
$singlePreset = @($visualXml.Defs.$visualPresetType) |
    Where-Object { $_.defName -eq 'BDP_Visual_Pistol' } |
    Select-Object -First 1
$dualPreset = @($visualXml.Defs.$visualPresetType) |
    Where-Object { $_.defName -eq 'BDP_Visual_Pistol_Dual' } |
    Select-Object -First 1
Assert-True ($null -ne $singlePreset) '左轮单武器视觉预设必须存在。'
Assert-True ($null -ne $dualPreset) '左轮双武器视觉预设必须存在。'
Assert-Equal $singlePreset.ParentName 'BDP_VisualBase_RangedSmall' `
    '左轮单武器视觉必须继承小型枪械单武器视觉。'
Assert-Equal $dualPreset.ParentName 'BDP_VisualBase_RangedSmall_Dual' `
    '左轮双武器视觉必须继承小型枪械双武器视觉。'

foreach ($preset in @($singlePreset, $dualPreset))
{
    Assert-Equal $preset.GraphicData.texPath 'Things/Trigger/Visual/RevolverReference' `
        '左轮单/双武器视觉必须使用目标左轮贴图。'
    Assert-Equal $preset.GraphicData.graphicClass 'Graphic_Single' `
        '左轮单/双武器视觉必须使用 Graphic_Single。'
    Assert-True ($null -eq $preset.Muzzle) `
        '左轮单/双武器视觉必须消费小型基类枪口数据，不得重复声明。'
}

$smallBase = @($visualXml.Defs.$visualPresetType) |
    Where-Object { $_.GetAttribute('Name') -eq 'BDP_VisualBase_RangedSmall' } |
    Select-Object -First 1
Assert-True ($null -ne $smallBase) '小型枪械单武器视觉基类必须存在。'
Assert-Equal ([string]$smallBase.Muzzle.IsRangedWeapon) 'true' `
    '小型枪械基类必须声明为远程武器枪口来源。'
Assert-Equal ([string]$smallBase.Muzzle.MuzzleOffset).Trim() '(0, 0, 0.3203125)' `
    '小型枪械基类枪口锚点必须落在左轮枪管前端附近。'

$submachineGun = @($visualXml.Defs.$visualPresetType) |
    Where-Object { $_.defName -eq 'BDP_Visual_SubmachineGun' } |
    Select-Object -First 1
$submachineGunDual = @($visualXml.Defs.$visualPresetType) |
    Where-Object { $_.defName -eq 'BDP_Visual_SubmachineGun_Dual' } |
    Select-Object -First 1
Assert-Equal $submachineGun.GetAttribute('Name') 'BDP_Visual_SubmachineGun' `
    '冲锋枪单武器视觉必须声明可供 ParentName 查找的 Name。'
Assert-Equal $submachineGunDual.GetAttribute('Name') 'BDP_Visual_SubmachineGun_Dual' `
    '冲锋枪双武器视觉必须声明可供 ParentName 查找的 Name。'
Assert-Equal ([string]$submachineGun.Muzzle.MuzzleOffset).Trim() '(0, 0, 0.359375)' `
    '冲锋枪单武器枪口锚点不得被左轮改写。'
Assert-Equal ([string]$submachineGunDual.Muzzle.MuzzleOffset).Trim() '(0, 0, 0.359375)' `
    '冲锋枪双武器枪口锚点不得被左轮改写。'
Assert-True (
    (Get-Content -LiteralPath $visualPath -Raw -Encoding utf8) -match '<MuzzleOffset>\(0, 0, 0\.48828125\)</MuzzleOffset>'
) '中型枪械共享基准枪口锚点不得被左轮改写。'
Assert-True (
    $visualLanguageText -match '<BDP_Visual_Pistol_Dual\.label>左轮手枪双武器手持视觉</BDP_Visual_Pistol_Dual\.label>'
) '左轮双武器视觉必须有中文诊断名称。'

Add-Type -AssemblyName System.Drawing
$texture = [System.Drawing.Image]::FromFile($texturePath)
try
{
    Assert-Equal $texture.Width '512' '左轮贴图宽度必须为 512px。'
    Assert-Equal $texture.Height '512' '左轮贴图高度必须为 512px。'
}
finally
{
    $texture.Dispose()
}

Write-Output 'RevolverWeaponVisualSmokeTests PASS'
