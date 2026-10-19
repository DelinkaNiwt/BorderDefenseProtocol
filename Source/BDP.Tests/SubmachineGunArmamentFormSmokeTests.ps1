# 冲锋枪武装构型、视觉锚点与 P90 贴图边界冒烟测试。

$ErrorActionPreference = "Stop"

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
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
$armamentLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\ChipArmamentFormDef\Presets.xml'
$keyedLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml'
$visualLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Core.Expressions.ExpressionVisualPresetDef\SubmachineGun.xml'
$texturePath = Join-Path $modRoot '1.6\Textures\Things\Trigger\Visual\P90Reference.png'
$definitionPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Defs\ChipArmamentFormDef.cs'
$labelResolverPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\UI\ChipManufacturingDefLabelResolver.cs'
$combinationResolverPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipCombinationResolver.cs'
$manufacturedChipPath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Thing\CompManufacturedChip.cs'

Assert-True (Test-Path -LiteralPath $armamentPath) '冲锋枪武装型 Def 文件必须存在。'
Assert-True (Test-Path -LiteralPath $visualPath) '正式视觉预设 XML 必须存在。'
Assert-True (-not (Test-Path -LiteralPath $armamentLanguagePath)) '武装型不得继续依赖自定义 DefInjected 语言包。'
Assert-True (Test-Path -LiteralPath $keyedLanguagePath) '武装型 Keyed 中文语言包必须存在。'
Assert-True (Test-Path -LiteralPath $visualLanguagePath) '视觉预设中文语言包必须存在。'
Assert-True (Test-Path -LiteralPath $texturePath) 'P90 目标贴图必须存在。'
Assert-True (Test-Path -LiteralPath $definitionPath) '武装型定义类必须存在。'
Assert-True (Test-Path -LiteralPath $labelResolverPath) '制造定义名称解析器必须存在。'
Assert-True (Test-Path -LiteralPath $combinationResolverPath) '芯片组合解析器必须存在。'
Assert-True (Test-Path -LiteralPath $manufacturedChipPath) '成品芯片组件必须存在。'

[xml]$armamentXml = Get-Content -LiteralPath $armamentPath -Raw -Encoding utf8
[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding utf8
$keyedLanguageText = Get-Content -LiteralPath $keyedLanguagePath -Raw -Encoding utf8
$visualLanguageText = Get-Content -LiteralPath $visualLanguagePath -Raw -Encoding utf8
$definitionText = Get-Content -LiteralPath $definitionPath -Raw -Encoding utf8
$labelResolverText = Get-Content -LiteralPath $labelResolverPath -Raw -Encoding utf8
$combinationResolverText = Get-Content -LiteralPath $combinationResolverPath -Raw -Encoding utf8
$manufacturedChipText = Get-Content -LiteralPath $manufacturedChipPath -Raw -Encoding utf8

$formType = 'BDP.Content.Assembly.ChipManufacturing.Defs.ChipArmamentFormDef'
$submachineGun = @($armamentXml.Defs.$formType) |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_SubmachineGun' } |
    Select-Object -First 1
Assert-True ($null -ne $submachineGun) '冲锋枪武装型必须存在。'
Assert-Equal $submachineGun.description '中近程高速型武装，四发连射，近距离命中可靠，远距离衰减明显。' '冲锋枪 Def 默认说明必须反映 4 发连射。'

$shotgun = @($armamentXml.Defs.$formType) |
    Where-Object { $_.defName -eq 'BDP_ArmamentForm_Shotgun' } |
    Select-Object -First 1
Assert-True ($null -ne $shotgun) '霰弹枪武装型必须存在。'
Assert-Equal $shotgun.description '极近程爆发型枪械。与原版链式霰弹枪参数一致，固定五发齐射。' '霰弹枪 Def 默认说明必须反映固定五发齐射。'
Assert-Equal $shotgun.overrides.burstShotCount '5' '霰弹枪必须使用绝对五发覆盖。'
Assert-True ([string]::IsNullOrWhiteSpace([string]$shotgun.overrides.burstShotCountMultiplier)) '霰弹枪不得继续使用弹数倍率覆盖。'
Assert-Equal $shotgun.overrides.rhythm 'Simultaneous' '霰弹枪必须保持同时齐射。'

$gunnerForms = @(
    @('BDP_ArmamentForm_Revolver', '左轮手枪'),
    @('BDP_ArmamentForm_AssaultRifle', '突击步枪'),
    @('BDP_ArmamentForm_SubmachineGun', '冲锋枪'),
    @('BDP_ArmamentForm_Shotgun', '霰弹枪')
)
foreach ($expected in $gunnerForms)
{
    $form = @($armamentXml.Defs.$formType) |
        Where-Object { $_.defName -eq $expected[0] } |
    Select-Object -First 1
    Assert-True ($null -ne $form) "$($expected[0]) 武装型必须存在。"
    Assert-Equal $form.label $expected[1] "$($expected[0]) Def 默认名称必须为中文。"
    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$form.description)) "$($expected[0]) Def 默认说明不能为空。"
    Assert-Equal $form.labelKey $expected[0] "$($expected[0]) 必须声明自身名称语言键。"
    Assert-Equal $form.descriptionKey ($expected[0] + '_Description') "$($expected[0]) 必须声明自身说明语言键。"
    Assert-True ($keyedLanguageText -match ('<' + [regex]::Escape($expected[0]) + '>' + [regex]::Escape($expected[1]) + '</' + [regex]::Escape($expected[0]) + '>')) "$($expected[0]) 缺少 Keyed 中文名称。"
    Assert-True ($keyedLanguageText -match ('<' + [regex]::Escape($expected[0] + '_Description') + '>[^<]+</' + [regex]::Escape($expected[0] + '_Description') + '>')) "$($expected[0]) 缺少 Keyed 中文说明。"
}
Assert-True ($keyedLanguageText -match '<BDP_ArmamentForm_SubmachineGun_Description>中近程高速型武装，四发连射，近距离命中可靠，远距离衰减明显。</BDP_ArmamentForm_SubmachineGun_Description>') '冲锋枪 Keyed 中文说明必须反映 4 发连射。'
Assert-True ($keyedLanguageText -match '<BDP_ArmamentForm_Shotgun_Description>极近程爆发型武装，固定五发齐射。</BDP_ArmamentForm_Shotgun_Description>') '霰弹枪 Keyed 中文说明必须反映固定五发齐射。'

Assert-True ($definitionText -match 'public string labelKey') '武装型定义必须提供名称语言键字段。'
Assert-True ($definitionText -match 'public string ResolvedLabel') '武装型定义必须提供当前语言名称解析。'
Assert-True ($definitionText -match 'public string descriptionKey') '武装型定义必须提供说明语言键字段。'
Assert-True ($definitionText -match 'public string ResolvedDescription') '武装型定义必须提供当前语言说明解析。'
Assert-True ($labelResolverText -match 'armamentForm\.ResolvedLabel') '制造台武装型名称必须读取当前语言名称。'
Assert-True ($labelResolverText -match 'armamentForm\.ResolvedDescription') '制造台武装型说明必须读取当前语言说明。'
Assert-True ($combinationResolverText -match 'armamentForm\.ResolvedLabel') '组合解析后的成品名称必须读取当前语言武装型名称。'
Assert-True ($manufacturedChipText -match 'ArmamentForm\.ResolvedLabel') '成品芯片来源标签必须读取当前语言武装型名称。'

$professionNames = @($submachineGun.compatibleProfessions.li | ForEach-Object { [string]$_ })
Assert-True ($professionNames -contains 'BDP_ChipProfession_Gunner') '冲锋枪武装型必须限制为枪手职业。'
Assert-Equal $submachineGun.maxFunctionCount '1' '冲锋枪武装型功能数量必须为 1。'
Assert-Equal $submachineGun.additionalWorkAmount '900' '冲锋枪追加工作量必须为 900。'
Assert-Equal $submachineGun.additionalCost.Steel '30' '冲锋枪追加钢材必须为 30。'

$overrides = $submachineGun.overrides
Assert-Equal $overrides.range '24.0' '冲锋枪射程必须为 24.0。'
Assert-Equal $overrides.accuracyTouch '0.83' '冲锋枪贴身精度必须为 0.83。'
Assert-Equal $overrides.accuracyShort '0.72' '冲锋枪近距精度必须为 0.72。'
Assert-Equal $overrides.accuracyMedium '0.44' '冲锋枪中距精度必须为 0.44。'
Assert-Equal $overrides.accuracyLong '0.22' '冲锋枪远距精度必须为 0.22。'
Assert-Equal $overrides.warmupTime '0.7' '冲锋枪预热时间必须为 0.7。'
Assert-Equal $overrides.defaultCooldownTime '1.15' '冲锋枪冷却时间必须为 1.15。'
Assert-Equal $overrides.burstShotCount '4' '冲锋枪连发数量必须为 4。'
Assert-Equal $overrides.ticksBetweenBurstShots '5' '冲锋枪连发间隔必须为 5 ticks。'
Assert-Equal $overrides.rhythm 'Sequential' '冲锋枪攻击节奏必须为 Sequential。'
Assert-Equal $overrides.soundCast 'Shot_MachinePistol' '冲锋枪必须沿用原版轻型冲锋枪音效。'
Assert-Equal $overrides.noiseRadius '14' '冲锋枪噪声半径必须为 14。'
Assert-Equal $overrides.muzzleFlashScale '0.8' '冲锋枪枪口闪光倍率必须为 0.8。'
Assert-Equal $overrides.visualPresetDefName 'BDP_Visual_SubmachineGun' '冲锋枪必须绑定单持视觉预设。'
Assert-Equal $overrides.compositeVisualPresetDefName 'BDP_Visual_SubmachineGun_Dual' '冲锋枪必须绑定双持视觉预设。'
Assert-True ($null -eq $overrides.rangedModulesMerge -and
    @($overrides.SelectNodes('rangedModules/li')).Count -eq 0) `
    '冲锋枪不得再追加部位崩溃远程模块。'

Assert-Equal $submachineGun.projectileOverrides.damageMultiplier '0.64' '冲锋枪伤害倍率必须为 0.64。'
Assert-Equal $submachineGun.projectileOverrides.stoppingPowerMultiplier '0.75' '冲锋枪停止力倍率必须为 0.75。'
$burstDamageMultiplier = [double]$overrides.burstShotCount * [double]$submachineGun.projectileOverrides.damageMultiplier
Assert-True ([math]::Abs($burstDamageMultiplier - 2.56) -lt 0.001) '冲锋枪当前单轮伤害倍率必须为 2.56。'

$visualPresetType = 'BDP.Core.Expressions.ExpressionVisualPresetDef'
$visualPresets = @($visualXml.Defs.$visualPresetType)
$singlePreset = $visualPresets | Where-Object { $_.defName -eq 'BDP_Visual_SubmachineGun' } | Select-Object -First 1
$dualPreset = $visualPresets | Where-Object { $_.defName -eq 'BDP_Visual_SubmachineGun_Dual' } | Select-Object -First 1
Assert-True ($null -ne $singlePreset) '冲锋枪单持视觉预设必须存在。'
Assert-True ($null -ne $dualPreset) '冲锋枪双持视觉预设必须存在。'
Assert-Equal $singlePreset.ParentName 'BDP_VisualBase_RangedMedium' '冲锋枪单持视觉必须继承中型枪械基准。'
Assert-Equal $dualPreset.ParentName 'BDP_VisualBase_RangedMedium_Dual' '冲锋枪双持视觉必须继承中型枪械双持基准。'

foreach ($preset in @($singlePreset, $dualPreset))
{
    Assert-Equal $preset.GraphicData.texPath 'Things/Trigger/Visual/P90Reference' '冲锋枪视觉必须使用 P90 贴图。'
    Assert-Equal ([string]$preset.Muzzle.IsRangedWeapon) 'true' '冲锋枪视觉必须声明为远程武器枪口来源。'
    Assert-Equal ([string]$preset.Muzzle.MuzzleOffset).Trim() '(0, 0, 0.359375)' '冲锋枪枪口锚点必须落在 440px 附近。'
}

Assert-True ((Get-Content -LiteralPath $visualPath -Raw -Encoding utf8) -match '<MuzzleOffset>\(0, 0, 0\.48828125\)</MuzzleOffset>') '突击步枪共享基准锚点不得被冲锋枪改写。'
Assert-True ($visualLanguageText -match '<BDP_Visual_SubmachineGun\.label>冲锋枪手持视觉</BDP_Visual_SubmachineGun\.label>') '冲锋枪单持视觉必须有中文名称。'
Assert-True ($visualLanguageText -match '<BDP_Visual_SubmachineGun_Dual\.label>冲锋枪双武器手持视觉</BDP_Visual_SubmachineGun_Dual\.label>') '冲锋枪双持视觉必须有中文名称。'

Add-Type -AssemblyName System.Drawing
$texture = [System.Drawing.Image]::FromFile($texturePath)
try
{
    Assert-Equal $texture.Width '512' 'P90 贴图宽度必须为 512px。'
    Assert-Equal $texture.Height '512' 'P90 贴图高度必须为 512px。'
}
finally
{
    $texture.Dispose()
}

Write-Host 'SubmachineGunArmamentFormSmokeTests PASS'
