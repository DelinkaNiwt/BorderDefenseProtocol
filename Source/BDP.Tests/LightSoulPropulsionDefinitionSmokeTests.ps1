# 光魂推进组合技 Def 与语言包契约冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$presetPath = Join-Path $modRoot "Common\Content\Defs\ChipManufacturingPresetDef\Presets.xml"
$abilityPath = Join-Path $modRoot "Common\Content\Defs\AbilityDef\LightSoulPropulsion.xml"
$flyerPath = Join-Path $modRoot "Common\Content\Defs\ThingDef\PawnFlyer_LightSoulPropulsion.xml"
$comboPath = Join-Path $modRoot "Common\Content\Defs\ComboDef\LightSoulPropulsion.xml"
$abilityLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\AbilityDef\LightSoul.xml"
$comboLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Core.Combos.ComboDef\LightSoulPropulsion.xml"
$chipLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef\Presets.xml"
$gameplayLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml"

foreach ($path in @($presetPath, $abilityPath, $flyerPath, $comboPath, $abilityLanguagePath, $comboLanguagePath, $chipLanguagePath, $gameplayLanguagePath))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少光魂推进组合技定义或语言包文件：$path"
}

$presetText = Get-Utf8Text $presetPath
$abilityText = Get-Utf8Text $abilityPath
$flyerText = Get-Utf8Text $flyerPath
$comboText = Get-Utf8Text $comboPath
$abilityLanguageText = Get-Utf8Text $abilityLanguagePath
$comboLanguageText = Get-Utf8Text $comboLanguagePath
$chipLanguageText = Get-Utf8Text $chipLanguagePath
$gameplayLanguageText = Get-Utf8Text $gameplayLanguagePath

# 精确按定义身份读取，避免跨条目正则把其他功能的 Ability 混入推进器。
[xml]$presetsXml = $presetText
$thrusterNode = $presetsXml.SelectSingleNode('/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef[defName="BDP_ChipFunction_Thruster"]')
$lightSoulNode = $presetsXml.SelectSingleNode('/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef[defName="BDP_ChipFunction_LightSoul"]')
Assert-True ($null -ne $thrusterNode) "缺少推进器芯片功能定义。"
Assert-True ($null -ne $lightSoulNode) "缺少光魂芯片功能定义。"
$thrusterBlock = $thrusterNode.OuterXml
$lightSoulBlock = $lightSoulNode.OuterXml

Assert-True ($thrusterBlock -match '<label>推进器</label>') "推进器默认名称必须为推进器。"
Assert-True ($thrusterBlock -match '<Category>BDP_ChipCategory_Accessory</Category>') "推进器必须属于配件分类。"
Assert-True ($thrusterBlock -match '<SlotRegion>MainSub</SlotRegion>') "推进器必须进入主副槽区域。"
Assert-True ($thrusterBlock -match '<SlotOccupancy>Single</SlotOccupancy>') "推进器必须占用单槽。"
Assert-True ($thrusterBlock -match '<EntryType>Passive</EntryType>') "推进器必须以被动条目作为组合来源。"
Assert-True ($thrusterBlock -match '<PassiveIdentityKey>[^<]+</PassiveIdentityKey>') "推进器必须有独立被动身份键。"
Assert-True ($thrusterBlock -notmatch '<EntryType>Ability</EntryType>') "推进器不得直接生成主动能力。"

Assert-True ($lightSoulBlock -notmatch 'light_soul_propulsion') "光魂两个形态不得继续直连光魂推进条目。"

Assert-True ($abilityText -match '<defName>BDP_Ability_LightSoulPropulsion</defName>') "光魂推进结果 Ability 缺少正式 DefName。"
Assert-True ($abilityText -match '<verbClass>BDP\.Content\.LightSoul\.Propulsion\.Verb_CastAbilityLightSoulPropulsion</verbClass>') "光魂推进必须使用光魂专用目标预览 Verb。"
Assert-True ($abilityText -match 'CompProperties_LightSoulPropulsion') "光魂推进必须使用光魂专属 Effect 配置。"
Assert-True ($abilityText -match '<pawnFlyerDef>BDP_PawnFlyer_LightSoulPropulsion</pawnFlyerDef>') "光魂推进必须配置专属地面冲刺飞行器。"
Assert-True ($abilityText -match '<stunTicks>120</stunTicks>') "光魂推进撞击眩晕必须配置为 120 tick（约 2 秒）。"
Assert-True ($abilityText -match '<defaultCooldownTime>1\.0</defaultCooldownTime>') "光魂推进释放者必须使用原版 Verb 的 1 秒冷却。"
Assert-True ($abilityText -notmatch 'CastJump|Longjump_Jump|Longjump_Land|CombatBodyShortJump') "光魂推进不得残留跳跃 Job、跳跃音效或旧短跳 Verb。"
Assert-True ($flyerText -notmatch '<stunDurationTicksRange>') "光魂推进释放者不得通过 PawnFlyer 的眩晕字段实现行动冷却。"

Assert-True ($comboText -match '<defName>BDP_Combo_LightSoulPropulsion</defName>') "缺少光魂推进组合技 Def。"
Assert-True ($comboText -match '<firstSourceKey>BDP_ChipFunction_LightSoul</firstSourceKey>') "光魂推进组合技第一来源必须是光魂。"
Assert-True ($comboText -match '<secondSourceKey>BDP_ChipFunction_Thruster</secondSourceKey>') "光魂推进组合技第二来源必须是推进器。"
Assert-True ($comboText -match '(?s)<FirstSourceScope>\s*<ModeKey>defense</ModeKey>\s*</FirstSourceScope>') "光魂推进组合技第一来源必须限制为防护形态。"
Assert-True ($comboText -notmatch '(?s)<FirstSourceScope>.*?<StanceKey>') "光魂推进组合技不应限制防护形态的子姿态。"
Assert-True ($comboText -notmatch 'heavy_blade') "光魂推进组合技不得匹配进攻形态。"
Assert-True ($comboText -match '<AbilityDefName>BDP_Ability_LightSoulPropulsion</AbilityDefName>') "组合技必须产出光魂推进 Ability。"
Assert-True ($comboText -match '<UseCostResolve>FollowFirstSource</UseCostResolve>') "光魂推进费用必须跟随光魂第一来源。"
Assert-True ($comboText -match '<MinimumRequiredResolve>FollowFirstSource</MinimumRequiredResolve>') "光魂推进最低 Trion 要求必须跟随光魂第一来源。"

Assert-True ($abilityLanguageText -match '<BDP_Ability_LightSoulPropulsion\.description>[^<]*(冲刺|推挤|钝伤)[^<]*</BDP_Ability_LightSoulPropulsion\.description>') "光魂推进能力说明必须反映新的冲刺战斗机制。"
Assert-True ($comboLanguageText -match '<BDP_Combo_LightSoulPropulsion\.label>光魂推进</BDP_Combo_LightSoulPropulsion\.label>') "缺少光魂推进组合技中文名称。"
Assert-True ($comboLanguageText -match '<BDP_Combo_LightSoulPropulsion\.description>[^<]+</BDP_Combo_LightSoulPropulsion\.description>') "缺少光魂推进组合技中文说明。"
Assert-True ($chipLanguageText -match '<BDP_ChipFunction_Thruster\.label>推进器</BDP_ChipFunction_Thruster\.label>') "缺少推进器中文名称语言键。"
Assert-True ($gameplayLanguageText -match '<BDP_ChipFunction_Thruster>推进器</BDP_ChipFunction_Thruster>') "缺少推进器芯片解析所需的中文名称键。"
Assert-True ($gameplayLanguageText -match '<BDP_ChipFunction_Thruster_Description>[^<]+</BDP_ChipFunction_Thruster_Description>') "缺少推进器中文说明语言键。"

Write-Host "PASS: 光魂推进组合技 Def、光魂直连移除、推进器配件定义和中文语言键契约正确。"
