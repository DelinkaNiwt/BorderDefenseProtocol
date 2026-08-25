$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-RequiredNode {
    param([System.Xml.XmlNode]$Parent, [string]$XPath, [string]$Message)
    $node = $Parent.SelectSingleNode($XPath)
    Assert-True ($null -ne $node) $Message
    return $node
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Split-Path -Parent $sourceRoot
$presetPath = Join-Path $modRoot '1.6\Content\Defs\ChipActionPresetDef\Presets.xml'
$abilityPath = Join-Path $modRoot '1.6\Content\Defs\AbilityDef\LightSoulPropulsion.xml'
$hediffPath = Join-Path $modRoot '1.6\Content\Defs\HediffDef\LightSoul.xml'
$visualPath = Join-Path $modRoot '1.6\Content\Defs\ExpressionDef\Visual.xml'
$energyShieldFleckPath = Join-Path $modRoot '1.6\Content\Defs\FleckDef\EnergyShield.xml'
$curvedShieldTexturePaths = @(
    'north',
    'east',
    'south',
    'west') | ForEach-Object {
        Join-Path $modRoot ('1.6\Textures\Effects\Shield\energy_shield_block_curved_' + $_ + '.png')
    }
$presetLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\ChipActionPresetDef\Presets.xml'
$abilityLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\AbilityDef\LightSoul.xml'
$hediffLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\HediffDef\LightSoul.xml'
$visualLanguagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\DefInjected\ExpressionVisualPresetDef\Kogetsu.xml'
$keyedPath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml'
$shieldPropertiesPath = Join-Path $modRoot 'Source\BDP.Content\Shield\HediffCompProperties_EnergyShield.cs'
$shieldCompPath = Join-Path $modRoot 'Source\BDP.Content\Shield\HediffComp_EnergyShield.cs'
$effectPlayerPath = Join-Path $modRoot 'Source\BDP.Content\Shield\EnergyShieldEffectPlayer.cs'
$entryConfigPath = Join-Path $modRoot 'Source\BDP\Core\Expressions\Config\ChipExpressionEntryConfig.cs'
$contractInterpreterPath = Join-Path $modRoot 'Source\BDP\Core\Expressions\Contract\ChipExpressionContractInterpreter.cs'
$manufacturingClonePath = Join-Path $modRoot 'Source\BDP.Content\Assembly\ChipManufacturing\Resolution\ChipArmamentFormExpressionService.cs'

foreach ($path in @($presetPath, $abilityPath, $hediffPath, $visualPath, $energyShieldFleckPath, $presetLanguagePath, $abilityLanguagePath, $hediffLanguagePath, $visualLanguagePath, $keyedPath)) {
    Assert-True (Test-Path -LiteralPath $path) ('光魂正式内容缺少文件：' + $path)
}

[xml]$presetXml = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8
[xml]$abilityXml = Get-Content -LiteralPath $abilityPath -Raw -Encoding UTF8
[xml]$hediffXml = Get-Content -LiteralPath $hediffPath -Raw -Encoding UTF8
[xml]$visualXml = Get-Content -LiteralPath $visualPath -Raw -Encoding UTF8
[xml]$energyShieldFleckXml = Get-Content -LiteralPath $energyShieldFleckPath -Raw -Encoding UTF8

$preset = Get-RequiredNode $presetXml '/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipActionPresetDef[defName="BDP_Preset_LightSoul"]' '缺少光魂动作预设。'
Assert-True ($preset.profession -eq 'BDP_ChipProfession_Attacker') '光魂职业必须是攻击手。'
Assert-True ($preset.config.Profile.Category -eq 'BDP_ChipCategory_Defense') '光魂主分类必须是防护。'
Assert-True ($preset.labelKey -eq 'BDP_ChipAction_LightSoul') '光魂成品名必须显式引用语言键，不能依赖自定义 Def 注入。'
Assert-True ($preset.descriptionKey -eq 'BDP_ChipAction_LightSoul_Description') '光魂说明必须显式引用语言键，不能依赖自定义 Def 注入。'

$entries = $preset.config.Expression.Entries.li
$entryById = @{}
foreach ($entry in $entries) { $entryById[[string]$entry.Id] = $entry }
foreach ($requiredId in @(
    'light_soul_propulsion',
    'light_soul_shield_mobile',
    'light_soul_shield_guard',
    'light_soul_shield_weapon',
    'light_soul_heavy_blade')) {
    Assert-True $entryById.ContainsKey($requiredId) ('光魂缺少表达条目：' + $requiredId)
}
$expectedEntryLabelKeys = @{
    light_soul_propulsion = 'BDP_Expression_LightSoulPropulsion'
    light_soul_shield_mobile = 'BDP_Expression_LightSoulShieldMobile'
    light_soul_shield_guard = 'BDP_Expression_LightSoulShieldGuard'
    light_soul_shield_weapon = 'BDP_Expression_LightSoulShieldBash'
    light_soul_heavy_blade = 'BDP_Expression_LightSoulHeavyBlade'
}
foreach ($entryId in $expectedEntryLabelKeys.Keys) {
    Assert-True ($entryById[$entryId].DisplayLabelKey -eq $expectedEntryLabelKeys[$entryId]) ('光魂表达缺少稳定显示键：' + $entryId)
}

$propulsionEntry = $entryById['light_soul_propulsion']
Assert-True (
    ($propulsionEntry.Kind -eq 'Ability') -and
    ($propulsionEntry.AbilityDefName -eq 'BDP_Ability_LightSoulPropulsion') -and
    ([single]$propulsionEntry.Trion.UseCost -eq 20)
) '光魂推进必须是所有形态共用的 20 Trion 跳跃能力。'

$modes = $preset.config.Expression.Modes.li
Assert-True ($preset.config.Expression.DefaultModeKey -eq 'shield') '光魂默认大形态必须是大盾。'
$shieldMode = $modes | Where-Object { $_.ModeKey -eq 'shield' }
$bladeMode = $modes | Where-Object { $_.ModeKey -eq 'heavy_blade' }
Assert-True (($shieldMode.ActiveEntryIds.li -join '|') -eq 'light_soul_propulsion|light_soul_shield_weapon') '大盾形态公共条目必须包含光魂推进与唯一真实盾牌武器。'
Assert-True (($bladeMode.ActiveEntryIds.li -join '|') -eq 'light_soul_propulsion|light_soul_heavy_blade') '重刃形态必须包含推进与近战重刃。'
Assert-True ($shieldMode.DefaultStanceKey -eq 'mobile') '大盾形态默认姿态必须是灵活姿态。'
Assert-True ($null -eq $bladeMode.Stances) '重刃形态不得残留护盾姿态。'
$mobileStance = $shieldMode.Stances.li | Where-Object { $_.StanceKey -eq 'mobile' }
$guardStance = $shieldMode.Stances.li | Where-Object { $_.StanceKey -eq 'guard' }
Assert-True (($mobileStance.ActiveEntryIds.li -join '|') -eq 'light_soul_shield_mobile') '灵活姿态必须只追加灵活盾防御 Hediff。'
Assert-True (($guardStance.ActiveEntryIds.li -join '|') -eq 'light_soul_shield_guard') '举盾姿态必须只追加举盾防御 Hediff。'

$shieldWeapon = $entryById['light_soul_shield_weapon']
Assert-True (
    ($shieldWeapon.Kind -eq 'PrimaryVerb') -and
    ($shieldWeapon.WeaponMode -eq 'Melee') -and
    ([int]$shieldWeapon.Execution.HitCount -eq 1) -and
    ($null -eq $shieldWeapon.Presentation)
) '公共盾牌武器必须是真实的一击近战表达且不得重复绘制盾面。'
$shieldTool = $shieldWeapon.tools.li
Assert-True (
    ([string]$shieldTool.capacities.li -eq 'Blunt') -and
    ([single]$shieldTool.power -eq 5) -and
    ([single]$shieldTool.armorPenetration -eq 0) -and
    ([string]$shieldWeapon.ToolLabelKeys.li -eq 'BDP_Tool_LightSoulShieldBash')
) '公共盾牌武器必须使用语言化的 5 点无穿透钝击。'

$bladeEntry = $entryById['light_soul_heavy_blade']
Assert-True (
    ($bladeEntry.Kind -eq 'PrimaryVerb') -and
    ($bladeEntry.WeaponMode -eq 'Melee') -and
    ($bladeEntry.DisplayLabelKey -eq 'BDP_Expression_LightSoulHeavyBlade') -and
    ([int]$bladeEntry.Execution.HitCount -eq 1) -and
    ($bladeEntry.Presentation.VisualPresetDefName -eq 'BDP_Visual_LightSoulHeavyBlade')
) '重刃必须是一击近战武器表达并使用独立视觉预设。'
$tools = $bladeEntry.tools.li
Assert-True ($tools.Count -eq 2) '重刃必须用两把互斥 Tool 表达钝伤与切割。'
$toolLabelKeys = @($bladeEntry.ToolLabelKeys.li)
Assert-True (
    (($toolLabelKeys -join '|') -eq 'BDP_Tool_LightSoulHeavyBladeBash|BDP_Tool_LightSoulHeavyBladeEdge')
) '重刃两把 Tool 必须按声明顺序提供稳定语言键。'
$bluntTool = $tools | Where-Object { [string]$_.capacities.li -eq 'Blunt' }
$cutTool = $tools | Where-Object { [string]$_.capacities.li -eq 'Cut' }
Assert-True (([single]$bluntTool.power -eq 20) -and ([single]$bluntTool.armorPenetration -eq 0) -and ([single]$bluntTool.chanceFactor -eq [single]1.7)) '重刃钝伤必须是 20 伤、0 穿透、较高权重 1.7。'
Assert-True (([single]$cutTool.power -eq 15) -and ([single]$cutTool.armorPenetration -eq [single]0.10) -and ([single]$cutTool.chanceFactor -eq [single]1.0)) '重刃切割必须是 15 伤、10% 穿透、较低权重 1.0。'

$ability = Get-RequiredNode $abilityXml '/Defs/AbilityDef[defName="BDP_Ability_LightSoulPropulsion"]' '缺少光魂推进 AbilityDef。'
Assert-True (
    ($ability.verbProperties.verbClass -eq 'BDP.Content.CombatBody.Verb_CastAbilityCombatBodyShortJump') -and
    ($ability.jobDef -eq 'CastJump') -and
    ($ability.comps.li.Class -contains 'BDP.Core.Abilities.CompProperties_AbilityEffect_BdpTrionCost')
) '光魂推进必须复用短距跳跃 Verb/Job，并挂接表达 Trion 成本组件。'

$mobileHediff = Get-RequiredNode $hediffXml '/Defs/HediffDef[defName="BDP_Hediff_LightSoulShieldMobile"]' '缺少灵活姿态护盾 Hediff。'
$guardHediff = Get-RequiredNode $hediffXml '/Defs/HediffDef[defName="BDP_Hediff_LightSoulShieldGuard"]' '缺少举盾姿态护盾 Hediff。'
$mobileComp = $mobileHediff.comps.li | Where-Object { $_.Class -eq 'BDP.Content.Shield.HediffCompProperties_EnergyShield' }
$guardComp = $guardHediff.comps.li | Where-Object { $_.Class -eq 'BDP.Content.Shield.HediffCompProperties_EnergyShield' }
Assert-True (([single]$mobileComp.blockAngleRange -eq 180) -and ([single]$mobileComp.blockChance -eq [single]0.5) -and ([string]$mobileComp.allowMeleeDamage -eq 'false')) '灵活姿态必须是前方 180°、50%、不挡近战。'
Assert-True (([single]$guardComp.blockAngleRange -eq 120) -and ([single]$guardComp.blockChance -eq [single]0.98) -and ([string]$guardComp.allowMeleeDamage -eq 'true')) '举盾姿态必须是前方 120°、98%、允许挡近战。'
Assert-True (([single]$guardHediff.stages.li.statFactors.MoveSpeed -eq [single]0.6) -and ([string]$guardHediff.stages.li.disabledWorkTags.li -eq 'Violent')) '举盾姿态必须移动速度 ×0.6，并禁止暴力攻击。'
foreach ($lightSoulShieldComp in @($mobileComp, $guardComp)) {
    Assert-True ([string]$lightSoulShieldComp.showBlockGraphic -eq 'false') '光魂格挡不得弹出额外六边形护盾贴图。'
    Assert-True ([single]$lightSoulShieldComp.impactEffectRadius -eq [single]0.4) '光魂格挡特效必须沿来袭方向贴近到 0.4 格。'
}

$shieldPropertiesText = Get-Content -LiteralPath $shieldPropertiesPath -Raw -Encoding UTF8
$shieldCompText = Get-Content -LiteralPath $shieldCompPath -Raw -Encoding UTF8
$effectPlayerText = Get-Content -LiteralPath $effectPlayerPath -Raw -Encoding UTF8
Assert-True ($shieldPropertiesText -match 'public bool showBlockGraphic = true;') '通用护盾必须默认保留格挡六边形贴图。'
Assert-True ($shieldPropertiesText -match 'public float impactEffectRadius = -1f;') '通用护盾命中特效距离必须默认回退既有半径。'
Assert-True ($shieldCompText -match 'Props\.ResolveImpactEffectRadius\(\)') '格挡命中位置必须读取独立特效距离。'
Assert-True ($effectPlayerText -match 'bool showBlockGraphic') '护盾特效播放器必须支持独立关闭六边形贴图。'
Assert-True (
    $effectPlayerText -match 'if \(!showBlockGraphic\)[\s\S]*PlayScaledBlockEffect\([\s\S]*?false,[\s\S]*?blockFlashFleckDef\)'
) '关闭六边形必须优先走可拆分的闪光与音效链，并传递分层后的白闪定义。'
Assert-True ($effectPlayerText -match 'else if \(showBlockGraphic\)') '关闭六边形后，资源缺失回退也不得生成替代大贴图。'

foreach ($visualDefName in @(
    'BDP_Visual_LightSoulShieldMobile',
    'BDP_Visual_LightSoulShieldMobile_Dual',
    'BDP_Visual_LightSoulShieldGuard',
    'BDP_Visual_LightSoulShieldGuard_Dual',
    'BDP_Visual_LightSoulHeavyBlade')) {
    $visual = Get-RequiredNode $visualXml ('/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="' + $visualDefName + '"]') ('缺少独立视觉预设：' + $visualDefName)
    Assert-True ($null -ne $visual.GraphicData.texPath) ('视觉预设缺少可加载贴图路径：' + $visualDefName)
}
$mobileVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldMobile"]' '缺少光魂灵活姿态视觉预设。'
$mobileDualVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldMobile_Dual"]' '缺少光魂灵活姿态双武器视觉预设。'
$guardVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldGuard"]' '缺少光魂举盾姿态视觉预设。'
$guardDualVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulShieldGuard_Dual"]' '缺少光魂举盾姿态双武器视觉预设。'
$heavyBladeVisual = Get-RequiredNode $visualXml '/Defs/BDP.Core.Expressions.ExpressionVisualPresetDef[defName="BDP_Visual_LightSoulHeavyBlade"]' '缺少光魂重刃形态视觉预设。'
$blockFleck = Get-RequiredNode $energyShieldFleckXml '/Defs/FleckDef[defName="BDP_Fleck_EnergyShieldBlock"]' '缺少通用能量护盾格挡 Fleck。'
foreach ($curvedShieldTexturePath in $curvedShieldTexturePaths) {
    Assert-True (Test-Path -LiteralPath $curvedShieldTexturePath) ('光魂举盾姿态缺少方向贴图：' + $curvedShieldTexturePath)
}
Assert-True ([string]$mobileVisual.GraphicData.texPath -eq 'Effects/Shield/energy_shield_block') '光魂灵活姿态必须继续使用原六边形贴图。'
Assert-True ([string]$mobileDualVisual.GraphicData.texPath -eq 'Effects/Shield/energy_shield_block') '光魂灵活姿态双武器视觉必须复用原六边形贴图。'
Assert-True ([string]$guardVisual.GraphicData.texPath -eq 'Effects/Shield/energy_shield_block_curved') '光魂举盾姿态必须独占弧面矩形盾贴图。'
Assert-True ([string]$guardDualVisual.GraphicData.texPath -eq 'Effects/Shield/energy_shield_block_curved') '光魂举盾姿态双武器视觉必须复用弧面矩形盾贴图。'
Assert-True (([string]$guardVisual.GraphicData.graphicClass -eq 'Graphic_Multi') -and ([string]$guardDualVisual.GraphicData.graphicClass -eq 'Graphic_Multi')) '光魂举盾单双武器必须共同使用原版多朝向贴图。'
Assert-True ([single]$guardVisual.SouthNorthPose.DefaultAngle -eq -68) '光魂举盾南北姿态必须让竖向源贴图形成约 15° 向内斜握。'
Assert-True ([string]$guardVisual.SouthNorthPose.HandMirrorOnlyWhenIdle -eq 'true') '光魂举盾南北屏幕左侧必须应用静默手侧镜像。'
Assert-True ([single]$guardVisual.SouthNorthPose.NorthZAdjust -eq [single]0.51) '光魂举盾单持朝北必须在统一高度上再额外抬高 0.05 格。'
Assert-True ([single]$guardVisual.EastWestPose.DefaultAngle -eq -53) '光魂举盾东西姿态必须抵消原版 53° 持械角，使竖向侧视图保持竖直。'
Assert-True (([string]$guardVisual.EastWestPose.HandMirror -eq 'false') -and
    ([string]$guardVisual.EastWestPose.FinalMirrorByHandOnly -eq 'false') -and
    ([string]$guardVisual.EastWestPose.MainHandAlwaysFront -eq 'false')) `
    '光魂举盾单持必须关闭额外手位镜像，只保留原版朝西基础镜像。'
# 注视警戒沿用原版固定持械角；南北屏幕左侧再执行一次手侧镜像。
$southNorthRawAngle = (143 - 90 + [single]$guardVisual.SouthNorthPose.DefaultAngle + 360) % 360
$southNorthMirroredAngle = (-$southNorthRawAngle + 360) % 360
$eastFinalAngle = (143 - 90 + [single]$guardVisual.EastWestPose.DefaultAngle + 360) % 360
$westFinalAngle = (217 - 90 - 180 - [single]$guardVisual.EastWestPose.DefaultAngle + 360) % 360
Assert-True (($southNorthRawAngle -eq 345) -and ($southNorthMirroredAngle -eq 15) -and ($eastFinalAngle -eq 0) -and ($westFinalAngle -eq 0)) '光魂举盾南北必须形成正负 15° 斜握，东西必须保持屏幕竖直。'
# 原版南北持械点分别为 -0.22 与 -0.11；朝北还会反转 DefaultOffset.z。
$guardDefaultOffsetParts = ([string]$guardVisual.SouthNorthPose.DefaultOffset).Trim('(', ')').Split(',')
$guardDefaultOffsetZ = [single]$guardDefaultOffsetParts[2]
$southFinalZ = [single]-0.22 + $guardDefaultOffsetZ
$northFinalZ = [single]-0.11 - $guardDefaultOffsetZ + [single]$guardVisual.SouthNorthPose.NorthZAdjust
Assert-True ([Math]::Abs(($northFinalZ - $southFinalZ) - [single]0.16) -lt 0.0001) '光魂举盾朝北最终必须比朝南高出原版 0.11 格加业务额外 0.05 格。'
Assert-True ([string]$heavyBladeVisual.GraphicData.texPath -eq 'Things/Trigger/Chip/Kogetsu/kogetsu_handle') '光魂重刃形态必须继续使用原手柄贴图。'
Assert-True ([string]$blockFleck.graphicData.texPath -eq 'Effects/Shield/energy_shield_block') '通用格挡 Fleck 必须继续使用原六边形贴图。'
$curvedShieldVisuals = $visualXml.SelectNodes('//BDP.Core.Expressions.ExpressionVisualPresetDef[GraphicData/texPath="Effects/Shield/energy_shield_block_curved"]')
$curvedShieldVisualNames = @($curvedShieldVisuals | ForEach-Object { [string]$_.defName } | Sort-Object)
Assert-True ($curvedShieldVisuals.Count -eq 2) '弧面矩形盾贴图必须只由举盾单侧与双持两个视觉预设引用。'
Assert-True (($curvedShieldVisualNames -join '|') -eq 'BDP_Visual_LightSoulShieldGuard|BDP_Visual_LightSoulShieldGuard_Dual') '弧面矩形盾贴图不得被光魂举盾以外的视觉引用。'

Add-Type -AssemblyName System.Drawing
foreach ($curvedShieldTexturePath in $curvedShieldTexturePaths) {
    $curvedShieldImage = [System.Drawing.Image]::FromFile($curvedShieldTexturePath)
    try {
        Assert-True (($curvedShieldImage.Width -eq 512) -and ($curvedShieldImage.Height -eq 512)) ('光魂举盾方向贴图必须是 512×512：' + $curvedShieldTexturePath)
        Assert-True ($curvedShieldImage.PixelFormat.ToString() -match 'Argb') ('光魂举盾方向贴图必须包含 Alpha 透明度通道：' + $curvedShieldTexturePath)
    }
    finally {
        $curvedShieldImage.Dispose()
    }
}

foreach ($languagePath in @($presetLanguagePath, $abilityLanguagePath, $hediffLanguagePath, $visualLanguagePath, $keyedPath)) {
    [xml](Get-Content -LiteralPath $languagePath -Raw -Encoding UTF8) | Out-Null
}
$presetLanguageText = Get-Content -LiteralPath $presetLanguagePath -Raw -Encoding UTF8
Assert-True ($presetLanguageText -notmatch 'BDP_Preset_LightSoul\.') '光魂不得继续依赖失效的自定义 DefInjected 嵌套字段。'
$keyedText = Get-Content -LiteralPath $keyedPath -Raw -Encoding UTF8
foreach ($key in @(
    'BDP_ChipAction_LightSoul',
    'BDP_ChipAction_LightSoul_Description',
    'BDP_Expression_LightSoulPropulsion',
    'BDP_Expression_LightSoulShieldMobile',
    'BDP_Expression_LightSoulShieldGuard',
    'BDP_Expression_LightSoulShieldBash',
    'BDP_Expression_LightSoulHeavyBlade',
    'BDP_Tool_LightSoulHeavyBladeBash',
    'BDP_Tool_LightSoulHeavyBladeEdge',
    'BDP_Tool_LightSoulShieldBash',
    'BDP_ChipMode_LightSoulShield',
    'BDP_ChipMode_LightSoulHeavyBlade',
    'BDP_ChipStance_LightSoulMobile',
    'BDP_ChipStance_LightSoulGuard'
)) {
    Assert-True ($keyedText -match ('<' + $key + '>')) ('语言包缺少光魂形态/姿态文本：' + $key)
}

$entryConfigText = Get-Content -LiteralPath $entryConfigPath -Raw -Encoding UTF8
$contractInterpreterText = Get-Content -LiteralPath $contractInterpreterPath -Raw -Encoding UTF8
$manufacturingCloneText = Get-Content -LiteralPath $manufacturingClonePath -Raw -Encoding UTF8
Assert-True ($entryConfigText -match 'List<string>\s+ToolLabelKeys') '表达条目配置必须提供与 Tool 顺序对应的语言键。'
Assert-True ($contractInterpreterText -match 'ResolveDeclaredTools\(config\.Tool,\s*config\.tools,\s*config\.ToolLabelKeys\)') '表达解释器必须在建立正式运行时表面前统一解析 Tool 名称。'
Assert-True ($contractInterpreterText -match 'resolvedLabelKey\.Translate\(\)') 'Tool 名称必须通过 Keyed 语言键解析。'
Assert-True ($contractInterpreterText -match 'ResolveDeclaredTools\(config\.Tool,\s*config\.tools,\s*null\)') '定义校验阶段不得提前解析 Tool 名称语言键。'
Assert-True ($manufacturingCloneText -match 'ToolLabelKeys\s*=\s*source\.ToolLabelKeys') '制造表达克隆必须保留 Tool 名称语言键。'

Write-Output 'LightSoulChipSmokeTests PASS'
