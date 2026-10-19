$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$presetPath = Join-Path $repoRoot 'Common\Content\Defs\ChipManufacturingPresetDef\Presets.xml'
$languagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\ChipInformation.xml'
$text = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8
$languageText = Get-Content -LiteralPath $languagePath -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

$document = [xml]$text
$defs = @($document.Defs.ChildNodes | Where-Object { $_.NodeType -eq 'Element' })
Assert-True ($defs.Count -eq 21) "正式制造预设数量应保持 21，实际为 $($defs.Count)。"
$noIndependentFeature = @(
    'BDP_ChipFunction_Thruster',
    'BDP_ChipFunction_Senku',
    'BDP_ChipFunction_LeadShotModified',
    'BDP_ChipFunction_LeadShotOppositeSide'
)
foreach ($definition in $defs) {
    Assert-True ($null -ne $definition.config.Features) "$($definition.defName) 必须显式写出功能声明块。"
    $features = @($definition.config.Features.li | Where-Object { $null -ne $_ })
    if ($noIndependentFeature -contains [string]$definition.defName) {
        Assert-True ($features.Count -eq 0) "$($definition.defName) 不得声明不存在的独立玩家功能。"
        continue
    }

    Assert-True ($features.Count -gt 0) "$($definition.defName) 必须声明已确认的玩家语义。"
    foreach ($feature in $features) {
        Assert-True ([string]$feature.Id -notmatch '^(Verb|Ability|Hediff|Passive|Tool)') "$($definition.defName) 的功能 Id 不得以技术类型命名。"
        Assert-True (-not [string]::IsNullOrWhiteSpace([string]$feature.Feature)) "$($definition.defName)/$($feature.Id) 缺少语义 FeatureDef。"
    }
}

Assert-True ($text -match 'BDP_ChipFunction_Asteroid[\s\S]*asteroid_ranged_attack') '小行星必须声明远程攻击。'
Assert-True ($text -match 'BDP_ChipFunction_Meteora[\s\S]*meteora_ranged_attack[\s\S]*RangedAttackFeatureSourceConfig') '美特拉爆炸半径必须作为远程攻击本体信息读取。'
Assert-True ($text -match 'BDP_ChipFunction_LightSoul[\s\S]*light_soul_defense_shield[\s\S]*ShieldFeatureSourceConfig[\s\S]*BDP_FeatureGroup_MeleeAttackMethod[\s\S]*light_soul_heavy_blade_cutting_method') '光魂必须直接声明护盾来源和分开的多 Tool 攻击方式。'
Assert-True ($text -match 'BDP_ChipFunction_Chameleon[\s\S]*chameleon_ability[\s\S]*BDP_Feature_GenericAbility') '变色龙必须声明泛用能力，而非隐蔽技术语义。'
Assert-True ($text -match 'BDP_ChipFunction_FujinRangedStrike[\s\S]*BDP_Feature_GenericAbility[\s\S]*BDP_Feature_RangedAttack') '风刃远程斩击必须是能力包含远程攻击。'
Assert-True ($text -match 'BDP_ChipFunction_Grasshopper[\s\S]*BDP_Feature_GenericAbility[\s\S]*BDP_Feature_Movement') '蚱蜢必须是能力包含机动。'
foreach ($labelKey in @(
    'BDP_Info_ExtraEffect_BodyPartDisintegration',
    'BDP_Info_ExtraEffect_LightSoulDefenseMovementReduction',
    'BDP_Info_ExtraEffect_LightSoulDefenseAttackRestriction'
)) {
    Assert-True ($languageText -match "<$labelKey>[^<]+</$labelKey>") "额外效果实例名称缺少本地化键：$labelKey"
}
foreach ($obsolete in @('BDP_Feature_Stealth', 'BDP_Feature_AttackModifier', 'BDP_Feature_ComboSupport')) {
    Assert-True (-not $text.Contains($obsolete)) "芯片预设仍引用废弃语义：$obsolete"
}

Write-Host 'ChipFeaturePresetDeclaration PASS'
