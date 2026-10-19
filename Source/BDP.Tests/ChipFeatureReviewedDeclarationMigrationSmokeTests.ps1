$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$presetPath = Join-Path $modRoot "Common\Content\Defs\ChipManufacturingPresetDef\Presets.xml"
$developmentPath = Join-Path $modRoot "Common\Development\Defs\ChipManufacturingPresetDef\ExactBodyPartRemoval.xml"
$featureCatalogPath = Join-Path $modRoot "Common\Content\Defs\ChipFeatureDef\Features.xml"
$featureLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Core.Information.ChipFeatureDef\Features.xml"
[xml]$presets = Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8
[xml]$development = Get-Content -LiteralPath $developmentPath -Raw -Encoding UTF8
$featureCatalogText = Get-Content -LiteralPath $featureCatalogPath -Raw -Encoding UTF8
[xml]$featureLanguageXml = Get-Content -LiteralPath $featureLanguagePath -Raw -Encoding UTF8

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Find-Definition([xml]$document, [string]$defName) {
    foreach ($definition in $document.Defs.ChildNodes) {
        if ($definition.NodeType -eq "Element" -and [string]$definition.defName -eq $defName) {
            return $definition
        }
    }

    return $null
}

function Find-Feature($featureNodes, [string]$id) {
    foreach ($feature in $featureNodes) {
        if ($feature.NodeType -eq "Element" -and [string]$feature.Id -eq $id) {
            return $feature
        }
    }

    return $null
}

function Assert-Feature($featureNodes, [string]$id, [string]$featureDef) {
    $feature = Find-Feature $featureNodes $id
    Assert-True ($null -ne $feature) "缺少功能实例：$id"
    Assert-True ([string]$feature.Feature -eq $featureDef) "$id 应声明 $featureDef，实际为 $($feature.Feature)"
    return $feature
}

function Assert-Group($feature, [string]$groupDef) {
    $groups = @($feature.Groups.ChildNodes | Where-Object { $_.NodeType -eq "Element" })
    Assert-True (@($groups | Where-Object { [string]$_.Group -eq $groupDef }).Count -gt 0) "$($feature.Id) 缺少信息组 $groupDef"
}

function Assert-AllowedDeclarationFields($feature) {
    $allowed = @("Id", "Feature", "LabelKey", "Sources", "Groups", "Subgroups", "Children")
    foreach ($child in $feature.ChildNodes) {
        if ($child.NodeType -eq "Element") {
            Assert-True ($allowed -contains $child.LocalName) "$($feature.Id) 不得内嵌实例值字段 $($child.LocalName)"
        }
    }

    foreach ($child in @($feature.Children.li)) {
        if ($null -ne $child) { Assert-AllowedDeclarationFields $child }
    }
}

$presetDefs = @($presets.Defs.ChildNodes | Where-Object { $_.NodeType -eq "Element" })
$chameleon = Find-Definition $presets "BDP_ChipFunction_Chameleon"
$chameleonFeatures = @($chameleon.config.Features.ChildNodes | Where-Object { $_.NodeType -eq "Element" })
$chameleonAbility = Assert-Feature $chameleonFeatures "chameleon_ability" "BDP_Feature_GenericAbility"
Assert-True (@($chameleonAbility.Sources.li | Where-Object { $_.GetAttribute("Class").EndsWith("AbilityLifecycleFeatureSourceConfig") }).Count -eq 1) "变色龙能力生命周期须读取 Ability 生命周期来源。"
Assert-True ($null -eq $chameleonAbility.SelectSingleNode("Children")) "变色龙当前没有确认的标准子语义。"

$fujinRanged = Find-Definition $presets "BDP_ChipFunction_FujinRangedStrike"
$fujinRangedFeatures = @($fujinRanged.config.Features.ChildNodes | Where-Object { $_.NodeType -eq "Element" })
$fujinAbility = Assert-Feature $fujinRangedFeatures "fujin_ranged_strike_ability" "BDP_Feature_GenericAbility"
Assert-True ([string]$fujinAbility.Children.li.Feature -eq "BDP_Feature_RangedAttack") "风刃远程斩击应为能力包含远程攻击。"

$grasshopper = Find-Definition $presets "BDP_ChipFunction_Grasshopper"
$grasshopperFeatures = @($grasshopper.config.Features.ChildNodes | Where-Object { $_.NodeType -eq "Element" })
$grasshopperAbility = Assert-Feature $grasshopperFeatures "grasshopper_ability" "BDP_Feature_GenericAbility"
Assert-True ([string]$grasshopperAbility.Children.li.Feature -eq "BDP_Feature_Movement") "蚱蜢应为能力包含机动。"

$meteora = Find-Definition $presets "BDP_ChipFunction_Meteora"
$meteoraFeatures = @($meteora.config.Features.ChildNodes | Where-Object { $_.NodeType -eq "Element" })
$meteoraRanged = Assert-Feature $meteoraFeatures "meteora_ranged_attack" "BDP_Feature_RangedAttack"
Assert-True ($null -eq $meteoraRanged.SelectSingleNode("Children")) "美特拉的爆炸是远程攻击本体结果，不声明攻击增强子功能。"

$lightSoul = Find-Definition $presets "BDP_ChipFunction_LightSoul"
$lightSoulFeatures = @($lightSoul.config.Features.ChildNodes | Where-Object { $_.NodeType -eq "Element" })
foreach ($id in @("light_soul_defense_shield")) {
    $shield = Assert-Feature $lightSoulFeatures $id "BDP_Feature_Shield"
    $shieldSources = @($shield.Sources.li | Where-Object {
        $_.GetAttribute("Class").EndsWith("ShieldFeatureSourceConfig")
    })
    Assert-True ($shieldSources.Count -eq 1) "$id 必须直接声明一条护盾来源。"
    Assert-True ([string]$shieldSources[0].EntryId -eq $id) "$id 必须读取自身姿态对应的表达条目。"
    Assert-True ($null -eq $shield.SelectSingleNode("Groups")) "$id 不得创建护盾规则信息组。"
}

foreach ($preset in $presetDefs) {
    foreach ($feature in @($preset.config.Features.li)) {
        if ([string]$feature.Feature -ne "BDP_Feature_Shield") {
            continue
        }

        $shieldSources = @($feature.Sources.li | Where-Object {
            $_.GetAttribute("Class").EndsWith("ShieldFeatureSourceConfig")
        })
        Assert-True ($shieldSources.Count -eq 1) "$($feature.Id) 必须直接声明一条护盾来源。"
        Assert-True (-not [string]::IsNullOrWhiteSpace([string]$shieldSources[0].EntryId)) "$($feature.Id) 必须指定护盾表达条目。"
        Assert-True ($null -eq $feature.SelectSingleNode("Groups")) "$($feature.Id) 不得创建护盾规则信息组。"
    }
}

foreach ($id in @("light_soul_punch_melee", "light_soul_heavy_blade_melee")) {
    $melee = Assert-Feature $lightSoulFeatures $id "BDP_Feature_MeleeAttack"
    Assert-Group $melee "BDP_FeatureGroup_MeleeAttackMethod"
}

foreach ($preset in $presetDefs) {
    foreach ($feature in @($preset.config.Features.li)) {
        Assert-AllowedDeclarationFields $feature
    }
}

$combinedText = (Get-Content -LiteralPath $presetPath -Raw -Encoding UTF8) + (Get-Content -LiteralPath $developmentPath -Raw -Encoding UTF8)
foreach ($obsolete in @("BDP_Feature_Stealth", "BDP_Feature_AttackModifier", "BDP_Feature_ComboSupport")) {
    Assert-True (-not $combinedText.Contains($obsolete)) "芯片或 Development 原型仍引用废弃语义 $obsolete"
}
foreach ($obsolete in @("BDP_Feature_Stealth", "BDP_Feature_AttackModifier", "BDP_Feature_ComboSupport")) {
    Assert-True (-not $featureCatalogText.Contains("<defName>$obsolete</defName>")) "已否决的功能语义 Def 不应继续保留：$obsolete"
    Assert-True (-not $featureLanguageXml.LanguageData.InnerXml.Contains("$obsolete.label")) "已否决的功能语义语言项不应继续保留：$obsolete"
}

Write-Host "PASS: 芯片声明按已审议语义迁移，未把实例值写回声明。"
