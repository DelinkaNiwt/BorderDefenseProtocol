# BDP 首批功能与属性 Def 目录冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$featureDefPath = Join-Path $modRoot "1.6\Content\Defs\ChipFeatureDef\Features.xml"
$groupDefPath = Join-Path $modRoot "1.6\Content\Defs\ChipFeatureGroupDef\Groups.xml"
$propertyDefRoot = Join-Path $modRoot "1.6\Content\Defs\ChipFeaturePropertyDef"
$featureLanguagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Core.Information.ChipFeatureDef\Features.xml"
$propertyLanguageRoot = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\DefInjected\BDP.Core.Information.ChipFeaturePropertyDef"

Assert-True (Test-Path -LiteralPath $featureDefPath) "缺少首批 ChipFeatureDef 目录。"
Assert-True (Test-Path -LiteralPath $groupDefPath) "缺少标准 ChipFeatureGroupDef 目录。"
Assert-True (Test-Path -LiteralPath $propertyDefRoot) "缺少首批 ChipFeaturePropertyDef 目录。"
Assert-True (Test-Path -LiteralPath $featureLanguagePath) "缺少功能中文语言包。"
Assert-True (Test-Path -LiteralPath $propertyLanguageRoot) "缺少属性中文语言包目录。"

$featureText = Get-Utf8Text $featureDefPath
$groupText = Get-Utf8Text $groupDefPath
$propertyText = (Get-ChildItem -LiteralPath $propertyDefRoot -Filter "*.xml" |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 }) -join "`n"

foreach ($featureName in @(
    "BDP_Feature_RangedAttack",
    "BDP_Feature_MeleeAttack",
    "BDP_Feature_Shield",
    "BDP_Feature_Movement",
    "BDP_Feature_GenericAbility"
)) {
    Assert-True ($featureText -match [regex]::Escape("<defName>$featureName</defName>")) `
        "缺少首批功能 Def：$featureName"
}

foreach ($obsoleteFeature in @(
    "BDP_Feature_Stealth",
    "BDP_Feature_AttackModifier",
    "BDP_Feature_ComboSupport"
)) {
    Assert-True (-not ($featureText -match [regex]::Escape("<defName>$obsoleteFeature</defName>"))) `
        "已否决的功能语义 Def 不应继续保留：$obsoleteFeature"
}

foreach ($propertyName in @(
    "BDP_Property_CastRange",
    "BDP_Property_EffectRadius",
    "BDP_Property_ActionCooldownDuration",
    "BDP_Property_DamageAmount",
    "BDP_Property_DamageType",
    "BDP_Property_ArmorPenetration",
    "BDP_Property_ShieldCapacity",
    "BDP_Property_ShieldRecovery",
    "BDP_Property_BlockArc",
    "BDP_Property_BlockAttackTypes"
)) {
    Assert-True ($propertyText -match [regex]::Escape("<defName>$propertyName</defName>")) `
        "缺少公共属性 Def：$propertyName"
}

Assert-True ($featureText -match "<Properties>") "功能 Def 必须声明属性接收槽位。"
Assert-True ($featureText -match "<Property>BDP_Property_ShieldCapacity</Property>") `
    "护盾功能必须接收容量属性。"
Assert-True ($featureText -match "<Property>BDP_Property_BlockArc</Property>") `
    "护盾功能必须直接接收抵挡角度属性。"
Assert-True ($featureText -match "<Property>BDP_Property_BlockAttackTypes</Property>") `
    "护盾功能必须直接接收抵挡攻击类型属性。"
Assert-True ($featureText -match "<Property>BDP_Property_CastRange</Property>") `
    "远程/通用功能必须接收有效距离属性。"
Assert-True ($propertyText -match "<ValueKind>Duration</ValueKind>") `
    "公共属性目录必须支持持续时间。"
Assert-True ($propertyText -match "<ValueKind>Percentage</ValueKind>") `
    "公共属性目录必须支持百分比。"

Write-Host "PASS: BDP 首批功能与公共属性 Def 目录完整。"
