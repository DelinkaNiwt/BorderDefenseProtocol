$ErrorActionPreference = 'Stop'

# 断言形态结构、真实攻击和保留参数，而非只匹配源码写法。
function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$modRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$definitionRoot = Join-Path $modRoot 'Common/Content/Defs'
[xml]$presets = Get-Content (Join-Path $definitionRoot 'ChipManufacturingPresetDef/Presets.xml') -Raw -Encoding UTF8
[xml]$hediffs = Get-Content (Join-Path $definitionRoot 'HediffDef/LightSoul.xml') -Raw -Encoding UTF8
[xml]$visuals = Get-Content (Join-Path $definitionRoot 'ExpressionDef/Visual.xml') -Raw -Encoding UTF8
[xml]$combos = Get-Content (Join-Path $definitionRoot 'ComboDef/LightSoulPropulsion.xml') -Raw -Encoding UTF8
[xml]$jobs = Get-Content (Join-Path $definitionRoot 'JobDef/LightSoul.xml') -Raw -Encoding UTF8
[xml]$language = Get-Content (Join-Path $modRoot 'Languages/ChineseSimplified (简体中文)/Keyed/Gameplay.xml') -Raw -Encoding UTF8
$preset = $presets.SelectSingleNode('/Defs/BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef[defName="BDP_ChipFunction_LightSoul"]')
$expression = $preset.config.Expression

# 防护不下分姿态，进攻只在两个姿态中择一攻击。
Assert-True ($expression.DefaultModeKey -eq 'defense') '光魂必须默认进入防护形态。'
$entries = @{}
foreach ($entry in $expression.Entries.li) { $entries[[string]$entry.Id] = $entry }
Assert-True ((($entries.Keys | Sort-Object) -join '|') -eq 'light_soul_defense_shield|light_soul_defense_state|light_soul_heavy_blade|light_soul_punch') '必须恰好保留防护盾、防护状态、重刃和冲拳四种表达。'
$modes = @($expression.Modes.li)
Assert-True ((($modes.ModeKey | Sort-Object) -join '|') -eq 'attack|defense') '必须只有防护、进攻两个形态。'
$defense = $modes | Where-Object ModeKey -eq 'defense'
$attack = $modes | Where-Object ModeKey -eq 'attack'
Assert-True (($defense.ActiveEntryIds.li -join '|') -eq 'light_soul_defense_shield|light_soul_defense_state') '防护必须只发布护盾与独立限制状态。'
Assert-True (($null -eq $defense.Stances) -and ($null -eq $defense.DefaultStanceKey)) '防护不允许下分姿态。'
Assert-True (($null -ne $attack.SelectSingleNode('ActiveEntryIds')) -and (@($attack.SelectNodes('ActiveEntryIds/li')).Count -eq 0) -and ($attack.DefaultStanceKey -eq 'heavy_blade')) '进攻必须显式提供空公共选择表，进入时默认重刃。'
$stances = @($attack.Stances.li)
Assert-True ((($stances.StanceKey | Sort-Object) -join '|') -eq 'heavy_blade|punch') '进攻只能包含重刃、冲拳姿态。'
foreach ($stance in $stances) {
    Assert-True (($stance.ActiveEntryIds.li -join '|') -eq ('light_soul_' + $stance.StanceKey)) '每个进攻姿态必须只激活自己的攻击。'
}

# 冲拳完整承接旧钝击，不凭名称增加伤害或移除完好条件。
$punch = $entries['light_soul_punch']
Assert-True (($punch.EntryType -eq 'PrimaryVerb') -and ($punch.WeaponMode -eq 'Melee') -and ([int]$punch.Execution.HitCount -eq 1)) '冲拳必须是真实的一击近战攻击。'
$tool = @($punch.tools.li)
Assert-True (($tool.Count -eq 1) -and ($tool[0].capacities.li -eq 'Blunt') -and ([single]$tool[0].power -eq 5) -and ([single]$tool[0].cooldownTime -eq 2) -and ([single]$tool[0].armorPenetration -eq 0) -and ([single]$tool[0].chanceFactor -eq 1)) '冲拳必须保留旧钝击的5伤、2秒冷却、0穿透与权重。'
Assert-True (($punch.Conditions.li.Class -eq 'BDP.Content.Shield.ShieldIntactExpressionCondition') -and ($punch.Conditions.li.GroupKey -eq 'light_soul')) '冲拳必须保留迁移前的护盾完好条件。'
Assert-True ($punch.ToolLabelKeys.li -eq 'BDP_Tool_LightSoulPunch') '冲拳攻击名称必须使用新语言键。'
$blade = $entries['light_soul_heavy_blade']
$tools = @($blade.tools.li)
Assert-True (([int]$blade.Execution.HitCount -eq 1) -and ($tools.Count -eq 2)) '重刃应保持一轮从两种攻击中择一。'
Assert-True (($tools[0].capacities.li -eq 'Blunt') -and ([single]$tools[0].power -eq 20) -and ([single]$tools[0].armorPenetration -eq 0) -and ([single]$tools[0].chanceFactor -eq [single]1.7)) '重刃钝击参数不得变化。'
Assert-True (($tools[1].capacities.li -eq 'Cut') -and ([single]$tools[1].power -eq 15) -and ([single]$tools[1].armorPenetration -eq [single]0.1) -and ([single]$tools[1].chanceFactor -eq 1)) '重刃切割参数不得变化。'
Assert-True (([single]$tools[0].cooldownTime -eq 2) -and ([single]$tools[1].cooldownTime -eq 2)) '重刃冷却不得变化。'

# 防护完好状态与限制分开，破盾仍保留限制和警戒。
Assert-True (@($hediffs.Defs.HediffDef).Count -eq 2) '灵活健康状态必须移除，只保留防护盾与独立状态。'
$shield = $hediffs.SelectSingleNode('/Defs/HediffDef[defName="BDP_Hediff_LightSoulDefenseShield"]')
$state = $hediffs.SelectSingleNode('/Defs/HediffDef[defName="BDP_Hediff_LightSoulDefenseState"]')
$shieldComp = $shield.comps.li | Where-Object Class -eq 'BDP.Content.Shield.HediffCompProperties_EnergyShield'
Assert-True (([single]$shieldComp.blockAngleRange -eq 120) -and ([single]$shieldComp.blockChance -eq [single]0.98) -and ($shieldComp.allowMeleeDamage -eq 'true')) '防护必须沿用120度、98%抵挡和允许挡近战。'
Assert-True (([single]$shieldComp.maximumDurability -eq 480) -and ([int]$shieldComp.recoveryTicks -eq 600) -and ([single]$shieldComp.naturalRecoveryPerSecond -eq 48)) '护盾耐久和恢复参数不得变化。'
Assert-True (([single]$state.stages.li.statFactors.MoveSpeed -eq [single]0.6) -and ($null -eq $state.stages.li.disabledWorkTags) -and ($null -eq $shield.stages)) '减速应留在独立状态中，且不全面禁止暴力。'
Assert-True (($entries['light_soul_defense_shield'].Conditions.li.GroupKey -eq 'light_soul') -and ($null -eq $entries['light_soul_defense_state'].Conditions)) '破盾不得撤下独立防护限制。'
$watch = $state.comps.li | Where-Object Class -eq 'BDP.Content.LightSoul.HediffCompProperties_LightSoulDefenseWatch'
Assert-True ($watch.verbs.li.verbClass -eq 'BDP.Content.LightSoul.Verb_LightSoulDefenseWatch') '防护状态必须承载改名后的警戒行为。'
Assert-True ($jobs.Defs.JobDef.defName -eq 'BDP_LightSoulDefenseWatch') '警戒作业必须改名。'
$combo = $combos.Defs.'BDP.Core.Combos.ComboDef'
Assert-True (($combo.FirstSourceScope.ModeKey -eq 'defense') -and ($null -eq $combo.FirstSourceScope.StanceKey)) '光魂推进必须只匹配防护形态。'
Assert-True (($combo.UseRequirements.li.GroupKey -eq 'light_soul') -and ($combo.Expression.Entries.li.TrionResolve.UseCostResolve -eq 'FollowFirstSource')) '推进完好条件和费用来源不得变化。'

# 新名称有语言包；移除的灵活视觉不应再被加载。
foreach ($keyNode in $preset.SelectNodes('.//DisplayLabelKey|.//ToolLabelKeys/li')) {
    Assert-True ($null -ne $language.SelectSingleNode('/LanguageData/' + $keyNode.InnerText)) ('缺少语言键：' + $keyNode.InnerText)
}
$visualNames = @($visuals.Defs.'BDP.Core.Expressions.ExpressionVisualPresetDef' | Where-Object { $_.defName -like '*LightSoul*' } | ForEach-Object { [string]$_.defName } | Sort-Object)
Assert-True (($visualNames -join '|') -eq 'BDP_Visual_LightSoulDefenseShield|BDP_Visual_LightSoulDefenseShield_Dual|BDP_Visual_LightSoulHeavyBlade') '光魂视觉必须使用防护/重刃标识，移除灵活视觉。'
foreach ($entry in $entries.Values) {
    foreach ($name in @($entry.Presentation.VisualPresetDefName, $entry.Presentation.CompositeVisualPresetDefName)) {
        if (-not [string]::IsNullOrWhiteSpace($name)) { Assert-True ($visualNames -contains $name) ('缺少视觉定义：' + $name) }
    }
}
Write-Output 'LightSoulChipSmokeTests PASS'
