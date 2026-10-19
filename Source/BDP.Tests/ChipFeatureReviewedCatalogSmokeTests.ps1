# 已审议芯片功能、属性与信息组目录冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$defsRoot = Join-Path $modRoot "1.6\Content\Defs"
$languageRoot = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected"
$featurePath = Join-Path $defsRoot "ChipFeatureDef\Features.xml"
$groupPath = Join-Path $defsRoot "ChipFeatureGroupDef\Groups.xml"
$propertyPaths = @(
    (Join-Path $defsRoot "ChipFeaturePropertyDef\CommonProperties.xml"),
    (Join-Path $defsRoot "ChipFeaturePropertyDef\CombatProperties.xml"),
    (Join-Path $defsRoot "ChipFeaturePropertyDef\ShieldProperties.xml")
)

foreach ($path in @($featurePath, $groupPath) + $propertyPaths) {
    Assert-True (Test-Path -LiteralPath $path) "缺少已审议语义目录文件：$path"
}

$featureXml = [xml](Get-Utf8Text $featurePath)
$groupXml = [xml](Get-Utf8Text $groupPath)
$featureLanguageXml = [xml](Get-Utf8Text (Join-Path $languageRoot "ChipFeatureDef\Features.xml"))
$groupLanguageXml = [xml](Get-Utf8Text (Join-Path $languageRoot "ChipFeatureGroupDef\Groups.xml"))
$propertyLanguageRoot = Join-Path $languageRoot "ChipFeaturePropertyDef"
$propertyNames = @{}
$groupNames = @{}
$propertyLanguageNames = @{}
$featureLanguageNames = @{}
$featureLanguageDescriptions = @{}

foreach ($path in $propertyPaths) {
    $propertyXml = [xml](Get-Utf8Text $path)
    foreach ($property in $propertyXml.Defs.ChildNodes) {
        if ($property.defName) {
            $propertyNames[$property.defName] = $property
        }
    }
}

foreach ($node in $groupLanguageXml.LanguageData.ChildNodes) {
    if ($node.Name -match "\.label$") {
        $groupNames[$node.Name.Substring(0, $node.Name.Length - 6)] = $true
    }
}

foreach ($node in $featureLanguageXml.LanguageData.ChildNodes) {
    if ($node.Name -match "\.label$") {
        $featureLanguageNames[$node.Name.Substring(0, $node.Name.Length - 6)] = $true
    }
    elseif ($node.Name -match "\.description$") {
        $featureLanguageDescriptions[$node.Name.Substring(0, $node.Name.Length - 12)] = $true
    }
}

foreach ($path in Get-ChildItem -LiteralPath $propertyLanguageRoot -Filter "*.xml" -File) {
    $propertyLanguageXml = [xml](Get-Utf8Text $path.FullName)
    foreach ($node in $propertyLanguageXml.LanguageData.ChildNodes) {
        if ($node.Name -match "\.label$") {
            $propertyLanguageNames[$node.Name.Substring(0, $node.Name.Length - 6)] = $true
        }
    }
}

$features = @{}
foreach ($feature in $featureXml.Defs.ChildNodes) {
    if ($feature.defName) {
        $features[$feature.defName] = $feature
    }
}

$groups = @{}
foreach ($group in $groupXml.Defs.ChildNodes) {
    if ($group.defName) {
        $groups[$group.defName] = $group
    }
}

$requiredFeatures = @(
    "BDP_Feature_RangedAttack",
    "BDP_Feature_MeleeAttack",
    "BDP_Feature_Shield",
    "BDP_Feature_Movement",
    "BDP_Feature_GenericAbility"
)

foreach ($featureName in $requiredFeatures) {
    Assert-True $features.ContainsKey($featureName) "缺少已审议功能语义：$featureName"
}

foreach ($obsoleteFeature in @(
    "BDP_Feature_Stealth",
    "BDP_Feature_AttackModifier",
    "BDP_Feature_ComboSupport"
)) {
    Assert-True (-not $features.ContainsKey($obsoleteFeature)) "已否决的功能语义 Def 不应继续保留：$obsoleteFeature"
    Assert-True (-not $featureLanguageNames.ContainsKey($obsoleteFeature)) "已否决的功能语义语言项不应继续保留：$obsoleteFeature"
}

$requiredGroups = @(
    "BDP_FeatureGroup_MeleeAttackMethod",
    "BDP_FeatureGroup_MovementMethod",
    "BDP_FeatureGroup_ExtraEffect"
)

foreach ($groupName in $requiredGroups) {
    Assert-True $groups.ContainsKey($groupName) "缺少标准信息组：$groupName"
    Assert-True $groupNames.ContainsKey($groupName) "缺少信息组中文语言项：$groupName"
}

Assert-True (-not $groups.ContainsKey("BDP_FeatureGroup_ShieldRule")) "护盾抵挡属性不得再使用规则信息组。"
Assert-True (-not $groupNames.ContainsKey("BDP_FeatureGroup_ShieldRule")) "护盾规则信息组不得保留中文语言项。"

$requiredProperties = @(
    "BDP_Property_TargetType",
    "BDP_Property_RequiresLineOfSight",
    "BDP_Property_CastRange",
    "BDP_Property_MinimumCastRange",
    "BDP_Property_EffectRadius",
    "BDP_Property_MaxTargets",
    "BDP_Property_WarmupDuration",
    "BDP_Property_ActionCooldownDuration",
    "BDP_Property_AbilityCooldownDuration",
    "BDP_Property_EffectDuration",
    "BDP_Property_ChargeCapacity",
    "BDP_Property_ChargeRecoveryDuration",
    "BDP_Property_UseCost",
    "BDP_Property_SustainCost",
    "BDP_Property_DamageAmount",
    "BDP_Property_DamageType",
    "BDP_Property_ArmorPenetration",
    "BDP_Property_AttackBurstCount",
    "BDP_Property_AttackBurstInterval",
    "BDP_Property_RangedShotRhythm",
    "BDP_Property_AccuracyTouch",
    "BDP_Property_AccuracyShort",
    "BDP_Property_AccuracyMedium",
    "BDP_Property_AccuracyLong",
    "BDP_Property_ProjectileSpeed",
    "BDP_Property_ProjectileFlightFeatures",
    "BDP_Property_StoppingPower",
    "BDP_Property_ShieldCapacity",
    "BDP_Property_ShieldRecovery",
    "BDP_Property_RecoveryDelay",
    "BDP_Property_BlockArc",
    "BDP_Property_BlockChance",
    "BDP_Property_BlockAttackTypes",
    "BDP_Property_DamageAbsorptionRatio",
    "BDP_Property_BlockCost",
    "BDP_Property_AppliedStatus",
    "BDP_Property_StatusStrength",
    "BDP_Property_StackLimit"
)

foreach ($propertyName in $requiredProperties) {
    Assert-True $propertyNames.ContainsKey($propertyName) "缺少已审议公共属性：$propertyName"
    Assert-True $propertyLanguageNames.ContainsKey($propertyName) "缺少功能属性简体中文名称：$propertyName"
}

$removedPropertyNames = @(
    "BDP_Property_UsageMode",
    "BDP_Property_TriggerCondition",
    "BDP_Property_UsageChance",
    "BDP_Property_MaxUses",
    "BDP_Property_RechargeDuration",
    "BDP_Property_TrionCost",
    "BDP_Property_EndCondition",
    "BDP_Property_MovementDistance",
    "BDP_Property_EffectAngle",
    "BDP_Property_IntervalDuration",
    "BDP_Property_PathConstraints",
    "BDP_Property_CooldownDuration",
    "BDP_Property_SpawnThing",
    "BDP_Property_SpawnCount",
    "BDP_Property_AppliedHediff",
    "BDP_Property_HediffSeverity",
    "BDP_Property_BurstCount",
    "BDP_Property_ProjectileDef",
    "BDP_Property_TrackingMode",
    "BDP_Property_GuidanceMode",
    "BDP_Property_MeleeAttackMethod",
    "BDP_Property_MeleeHitCount",
    "BDP_Property_MeleeInterval",
    "BDP_Property_HitWeight",
    "BDP_Property_BlockTypes",
    "BDP_Property_ShieldDuration",
    "BDP_Property_ShieldBreakBehavior",
    "BDP_Property_ShieldBypass",
    "BDP_Property_ShieldReflection",
    "BDP_Property_ShieldConversion"
)

foreach ($propertyName in $removedPropertyNames) {
    Assert-True (-not $propertyNames.ContainsKey($propertyName)) "已否决或已被语义组替代的属性 Def 不应继续保留：$propertyName"
    Assert-True (-not $propertyLanguageNames.ContainsKey($propertyName)) "已否决或已被语义组替代的属性语言项不应继续保留：$propertyName"
}

foreach ($featureName in $requiredFeatures) {
    $feature = $features[$featureName]
    Assert-True $featureLanguageNames.ContainsKey($featureName) "缺少功能语义简体中文名称：$featureName"
    Assert-True $featureLanguageDescriptions.ContainsKey($featureName) "缺少功能语义简体中文说明：$featureName"

    foreach ($propertySlot in @($feature.Properties.li)) {
        Assert-True (-not ($removedPropertyNames -contains [string]$propertySlot.Property)) "$featureName 不得再接收已否决属性 $($propertySlot.Property)"
    }

    foreach ($groupSlot in @($feature.Groups.li | Where-Object { $_ -and $_.Group })) {
        Assert-True $groups.ContainsKey([string]$groupSlot.Group) "$featureName 引用了未定义的信息组 $($groupSlot.Group)"
    }

    foreach ($subgroupSlot in @($feature.Subgroups.li | Where-Object { $_ -and $_.Group })) {
        Assert-True $groups.ContainsKey([string]$subgroupSlot.Group) "$featureName 引用了未定义的功能级子组 $($subgroupSlot.Group)"
    }
}

$groupSlotExpectations = @{
    "BDP_Feature_MeleeAttack" = "BDP_FeatureGroup_MeleeAttackMethod"
    "BDP_Feature_Movement" = "BDP_FeatureGroup_MovementMethod"
}

foreach ($featureName in $groupSlotExpectations.Keys) {
    $expectedGroup = $groupSlotExpectations[$featureName]
    Assert-True (@($features[$featureName].Groups.li | ForEach-Object { [string]$_.Group }) -contains $expectedGroup) "$featureName 必须声明自己的 $expectedGroup 信息组"
}

$shieldPropertySlots = @(
    "BDP_Property_BlockAttackTypes",
    "BDP_Property_BlockArc",
    "BDP_Property_BlockChance",
    "BDP_Property_DamageAbsorptionRatio",
    "BDP_Property_BlockCost"
)
$shieldDeclaredProperties = @($features["BDP_Feature_Shield"].Properties.li | ForEach-Object { [string]$_.Property })
foreach ($propertyName in $shieldPropertySlots) {
    Assert-True ($shieldDeclaredProperties -contains $propertyName) "护盾功能必须直接接收属性 $propertyName"
}

foreach ($featureName in $requiredFeatures) {
    Assert-True (@($features[$featureName].Subgroups.li | ForEach-Object { [string]$_.Group }) -contains "BDP_FeatureGroup_ExtraEffect") "$featureName 必须允许功能整体拥有额外效果子组"
}

foreach ($groupName in @(
    "BDP_FeatureGroup_MeleeAttackMethod",
    "BDP_FeatureGroup_MovementMethod"
)) {
    Assert-True (@($groups[$groupName].Subgroups.li | ForEach-Object { [string]$_.Group }) -contains "BDP_FeatureGroup_ExtraEffect") "$groupName 必须允许其具体实例拥有额外效果子组"
}

Write-Host "PASS: 已审议功能、公共属性、信息组与中英文目录一致，且新语义未引用已否决属性。"
