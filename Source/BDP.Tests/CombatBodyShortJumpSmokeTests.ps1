# 战斗体短距跳跃内容边界冒烟测试。
# 该测试只验证正式 Def、语言包和 Content 侧 Verb 的最小接线，不启动游戏。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$activeHediffPath = Join-Path $modRoot "1.6\Defs\HediffDef\CombatBody.xml"
$abilityDefPath = Join-Path $modRoot "1.6\Content\Defs\AbilityDef\CombatBodyShortJump.xml"
$flyerDefPath = Join-Path $modRoot "1.6\Content\Defs\ThingDef\PawnFlyer_CombatBodyShortJump.xml"
$verbPath = Join-Path $modRoot "Source\BDP.Content\CombatBody\ShortJump\Verb_CastAbilityCombatBodyShortJump.cs"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\AbilityDef\CombatBodyShortJump.xml"

Assert-True (Test-Path -LiteralPath $activeHediffPath) "缺少战斗体激活 Hediff Def。"
Assert-True (Test-Path -LiteralPath $abilityDefPath) "缺少短距跳跃 AbilityDef。"
Assert-True (Test-Path -LiteralPath $flyerDefPath) "缺少短距跳跃 PawnFlyer Def。"
Assert-True (Test-Path -LiteralPath $verbPath) "缺少短距跳跃 Verb。"
Assert-True (Test-Path -LiteralPath $languagePath) "缺少短距跳跃中文语言包。"

$activeText = Get-Utf8Text $activeHediffPath
$abilityText = Get-Utf8Text $abilityDefPath
$flyerText = Get-Utf8Text $flyerDefPath
$verbText = Get-Utf8Text $verbPath
$languageText = Get-Utf8Text $languagePath
$activeSection = ($activeText -split '<HediffDef>')[1]

Assert-True ($activeText -match '<defName>BDP_CombatBodyActive</defName>[\s\S]*<li Class="HediffCompProperties_GiveAbility">[\s\S]*<abilityDef>BDP_Ability_CombatBodyShortJump</abilityDef>') `
    "战斗体激活 Hediff 必须通过原版 HediffComp_GiveAbility 授予短距跳跃。"
Assert-True ($activeSection -notmatch 'HediffCompProperties_Disappears') `
    "战斗体激活 Hediff 不得误挂崩解后遗症的自动消退组件。"

Assert-True ($abilityText -match '<defName>BDP_Ability_CombatBodyShortJump</defName>') `
    "短距跳跃 AbilityDef 必须使用正式 DefName。"
Assert-True ($abilityText -match '<cooldownTicksRange>1200</cooldownTicksRange>') `
    "短距跳跃冷却必须为 1200 ticks（20 秒）。"
Assert-True ($abilityText -match '<aiCanUse>false</aiCanUse>') `
    "短距跳跃不得允许 AI 使用。"
Assert-True ($abilityText -match '<displayGizmoWhileUndrafted>false</displayGizmoWhileUndrafted>') `
    "短距跳跃未征召时必须隐藏。"
Assert-True ($abilityText -match '<showWhenDrafted>true</showWhenDrafted>') `
    "短距跳跃征召时必须显示。"
Assert-True ($abilityText -match '<verbClass>BDP\.Content\.CombatBody\.Verb_CastAbilityCombatBodyShortJump</verbClass>') `
    "短距跳跃必须使用 Content 侧专用 Verb。"
Assert-True ($abilityText -match '<warmupTime>0\.2</warmupTime>') `
    "短距跳跃预热时间必须为 0.2 秒。"
Assert-True ($abilityText -match '<range>15\.9</range>') `
    "短距跳跃射程必须为 15.9。"
Assert-True ($abilityText -match '<onlyManualCast>true</onlyManualCast>') `
    "短距跳跃必须仅允许手动施放。"
Assert-True ($abilityText -match '<canTargetLocations>true</canTargetLocations>' -and
             $abilityText -match '<canTargetPawns>false</canTargetPawns>' -and
             $abilityText -match '<canTargetBuildings>false</canTargetBuildings>') `
    "短距跳跃必须只允许选择地面位置。"
Assert-True ($abilityText -notmatch 'CompAbilityEffect_HemogenCost|CompProperties_AbilityHemogenCost|BdpTrionCost|CompProperties_AbilityEffect_BdpTrionCost') `
    "短距跳跃不得配置血液素、Trion 或其他资源消耗组件。"

Assert-True ($verbText -match 'class Verb_CastAbilityCombatBodyShortJump\s*:\s*Verb_CastAbilityJump') `
    "短距跳跃 Verb 必须继承原版 Verb_CastAbilityJump。"
Assert-True ($verbText -match 'BDP_PawnFlyer_CombatBodyShortJump') `
    "短距跳跃 Verb 必须选择 BDP 专用飞行器。"

Assert-True ($flyerText -match '<ThingDef ParentName="PawnFlyerBase">') `
    "短距跳跃飞行器必须继承原版 PawnFlyerBase。"
Assert-True ($flyerText -match '<thingClass>PawnFlyer</thingClass>') `
    "短距跳跃飞行器必须复用原版 PawnFlyer 类。"
Assert-True ($flyerText -match '<flightSpeed>24</flightSpeed>') `
    "短距跳跃飞行速度必须为 24。"
Assert-True ($flyerText -match '<flightDurationMin>0\.25</flightDurationMin>') `
    "短距跳跃最短飞行时间必须为 0.25 秒。"

Assert-True ($languageText -match '<BDP_Ability_CombatBodyShortJump\.label>短距跳跃</BDP_Ability_CombatBodyShortJump\.label>') `
    "短距跳跃能力名称必须来自中文语言包。"

Write-Output "CombatBodyShortJumpSmokeTests PASS"
