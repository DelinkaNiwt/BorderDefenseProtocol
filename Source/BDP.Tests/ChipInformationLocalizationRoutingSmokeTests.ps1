# 芯片信息值必须按明确来源类型走原版译名路径。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$valuePath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationValue.cs"
$displaySourcePath = Join-Path $modRoot "Source\BDP\Core\Information\Model\ChipInformationValueDisplaySourceKind.cs"
$knownValueFactoryPath = Join-Path $modRoot "Source\BDP\Core\Information\Input\ChipInformationKnownValueFactory.cs"
$definitionAdapterPath = Join-Path $modRoot "Source\BDP\Core\Information\Input\ChipInformationDefinitionInputAdapter.cs"
$expressionInputAdapterPath = Join-Path $modRoot "Source\BDP\Core\Information\Input\ChipInformationExpressionInputAdapter.cs"
$expressionSnapshotAdapterPath = Join-Path $modRoot "Source\BDP\Core\Information\Input\ChipInformationExpressionSnapshotAdapter.cs"
$formatterPath = Join-Path $modRoot "Source\BDP.Content\Information\ChipInformationTextFormatter.cs"
$defResolverPath = Join-Path $modRoot "Source\BDP.Content\Information\ChipInformationDefLabelResolverRegistry.cs"
$mergeServicePath = Join-Path $modRoot "Source\BDP\Core\Information\Rules\ChipInformationMergeService.cs"
$featureMergeServicePath = Join-Path $modRoot "Source\BDP\Core\Information\Features\Resolution\ChipFeatureContributionMergeService.cs"
$detailRendererPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs"
$manufacturingBuilderPath = Join-Path $modRoot "Source\BDP.Content\Assembly\Window\ChipAssemblyDetailModelBuilder.cs"
$featureRendererPath = Join-Path $modRoot "Source\BDP.Content\Information\Features\Rendering\GenericChipFeatureRenderer.cs"
$rangedWorkerPath = Join-Path $modRoot "Source\BDP.Content\Information\Features\Workers\RangedAttackFeatureSourceWorker.cs"
$meleeWorkerPath = Join-Path $modRoot "Source\BDP.Content\Information\Features\Workers\MeleeAttackMethodFeatureSourceWorker.cs"
$abilityMeleeWorkerPath = Join-Path $modRoot "Source\BDP.Content\Information\Features\Workers\AbilityMeleeAttackFeatureSourceWorker.cs"
$enumLocalizationTestPath = Join-Path $PSScriptRoot "ChipFeatureEnumLocalizationSmokeTests.ps1"

foreach ($path in @(
    $valuePath,
    $displaySourcePath,
    $knownValueFactoryPath,
    $definitionAdapterPath,
    $expressionInputAdapterPath,
    $expressionSnapshotAdapterPath,
    $formatterPath,
    $defResolverPath,
    $mergeServicePath,
    $featureMergeServicePath,
    $detailRendererPath,
    $manufacturingBuilderPath,
    $featureRendererPath,
    $rangedWorkerPath,
    $meleeWorkerPath,
    $abilityMeleeWorkerPath))
{
    Assert-True (Test-Path -LiteralPath $path) "缺少译名路径相关实现文件：$path"
}

$valueText = Get-Utf8Text $valuePath
$displaySourceText = Get-Utf8Text $displaySourcePath
$knownValueFactoryText = Get-Utf8Text $knownValueFactoryPath
$definitionAdapterText = Get-Utf8Text $definitionAdapterPath
$expressionInputAdapterText = Get-Utf8Text $expressionInputAdapterPath
$expressionSnapshotAdapterText = Get-Utf8Text $expressionSnapshotAdapterPath
$formatterText = Get-Utf8Text $formatterPath
$defResolverText = Get-Utf8Text $defResolverPath
$mergeServiceText = Get-Utf8Text $mergeServicePath
$featureMergeServiceText = Get-Utf8Text $featureMergeServicePath
$detailRendererText = Get-Utf8Text $detailRendererPath
$manufacturingBuilderText = Get-Utf8Text $manufacturingBuilderPath
$featureRendererText = Get-Utf8Text $featureRendererPath
$rangedWorkerText = Get-Utf8Text $rangedWorkerPath
$meleeWorkerText = Get-Utf8Text $meleeWorkerPath
$abilityMeleeWorkerText = Get-Utf8Text $abilityMeleeWorkerPath
$enumLocalizationTestText = Get-Utf8Text $enumLocalizationTestPath

Assert-True ($displaySourceText -match "Raw" -and $displaySourceText -match "TranslationKey" -and $displaySourceText -match "DefReference") `
    "值的译名来源必须明确区分原始标识、文本键和 Def 引用。"
Assert-True ($valueText -match "DisplaySourceKind" -and $valueText -match "DefinitionTypeKey") `
    "中性信息值必须保存译名来源及 Def 类型标识。"
Assert-True ($valueText -match "FromTranslationKey\(" -and $valueText -match "FromDefReference\(Type") `
    "信息值必须提供显式文本键与 Def 引用工厂。"
Assert-True ($valueText -match "FromEnum\(string enumKey\)" -and $valueText -match "DisplaySourceKind\s*=\s*ChipInformationValueDisplaySourceKind\.Raw") `
    "普通枚举工厂不得默认把枚举名当作可翻译文本键。"
Assert-True ($formatterText -match "FormatValue\(" -and $formatterText -match "TranslationKey" -and $formatterText -match "DefReference") `
    "共享格式器必须按值的明确来源分派译名读取路径。"
Assert-True ($formatterText -match "value\.Items\[index\]" -and $formatterText -match "FormatValue\(value\.Items\[index\]") `
    "列表及嵌套列表必须递归采用同一译名来源规则。"
Assert-True ($formatterText -notmatch "EnumKey\.Translate\(\)") `
    "共享格式器不得对任意枚举键无条件调用 Translate。"
Assert-True ($defResolverText -match "public static bool Register<TDef>" -and $defResolverText -match "DefDatabase<TDef>" -and $defResolverText -match "LabelCap") `
    "Def 标签解析器必须允许扩展模组注册 Def 类型，并使用原版 LabelCap。"
Assert-True ($defResolverText -match "Register<DamageDef>\(\)" -and $defResolverText -match "Register<ThingDef>\(\)" -and $defResolverText -match "Register<AbilityDef>\(\)" -and $defResolverText -match "Register<HediffDef>\(\)" -and $defResolverText -match "Register<ChipCategoryDef>\(\)" -and $defResolverText -match "Register<ChipTagDef>\(\)" -and $defResolverText -match "Register<ChipExclusionGroupDef>\(\)" -and $defResolverText -match "Register<ChipProfessionDef>\(\)") `
    "Def 标签解析器必须登记当前模型实际引用的原版 Def 类型。"
Assert-True ($mergeServiceText -match "DisplaySourceKind" -and $mergeServiceText -match "DefinitionTypeKey" -and $featureMergeServiceText -match "DisplaySourceKind" -and $featureMergeServiceText -match "DefinitionTypeKey") `
    "合并相等判断必须纳入译名来源和 Def 类型，避免不同类型的同名 Def 被误合并。"
Assert-True ($knownValueFactoryText -match "FromWeaponMode" -and $knownValueFactoryText -match "FromHediffApplyMode" -and $knownValueFactoryText -match "FromHediffControlMode" -and $knownValueFactoryText -match "FromEnum\(value\)") `
    "已知模式必须集中映射文本键，未知模式保留原始值。"
Assert-True ($definitionAdapterText -match "FromDefReference\(\s*typeof\(ChipCategoryDef\)" -and $definitionAdapterText -match "FromDefReference\(\s*typeof\(ChipTagDef\)" -and $definitionAdapterText -match "FromDefReference\(\s*typeof\(ChipExclusionGroupDef\)") `
    "分类、标签和排斥组 Def 必须保留各自类型后再查译名。"
Assert-True ($definitionAdapterText -match "FromDefReference\(typeof\(AbilityDef\)" -and $definitionAdapterText -match "FromDefReference\(typeof\(HediffDef\)") `
    "静态定义中的 Ability 与 Hediff Def 必须保留类型身份。"
Assert-True ($expressionInputAdapterText -match "FromDefReference\(typeof\(HediffDef\)" -and $expressionInputAdapterText -match "FromHediffApplyMode" -and $expressionSnapshotAdapterText -match "FromHediffApplyMode") `
    "运行结果适配也必须保留 Def 类型并对已知 Hediff 模式走显式翻译键。"
Assert-True ($detailRendererText -match "ChipInformationTextFormatter\.FormatValueWithUnit\(") `
    "芯片详情必须使用统一值格式化入口。"
Assert-True ($detailRendererText -notmatch "EnumKey\.Translate\(\)" -and $manufacturingBuilderText -notmatch "EnumKey\.Translate\(\)") `
    "两个面板不得各自把普通枚举键当成文本键。"
Assert-True ($rangedWorkerText -match "FromDefReference\(\s*typeof\(DamageDef\)" -and $meleeWorkerText -match "FromDefReference\(\s*typeof\(DamageDef\)" -and $abilityMeleeWorkerText -match "FromDefReference\(\s*typeof\(DamageDef\)") `
    "远程、近战与能力近战伤害类型必须以 DamageDef 类型引用保存。"
Assert-True ($featureRendererText -match "LabelCap") `
    "语义属性名称必须从 Def.LabelCap 读取，而非直接取 Def.label。"
Assert-True ($enumLocalizationTestText -match "FromTranslationKey" -and $enumLocalizationTestText -notmatch "FromEnum\(simultaneous") `
    "已有功能文本键必须显式走 Keyed 翻译工厂。"

$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipInformation.xml"
[xml]$language = Get-Content -LiteralPath $languagePath -Raw -Encoding UTF8
$translationKeys = @($language.LanguageData.ChildNodes | Where-Object { $_.NodeType -eq [System.Xml.XmlNodeType]::Element } | ForEach-Object { $_.Name })
foreach ($key in @(
    "BDP_Info_WeaponMode_None",
    "BDP_Info_WeaponMode_Melee",
    "BDP_Info_WeaponMode_Ranged",
    "BDP_Info_HediffApplyMode_CountToSeverity",
    "BDP_Info_HediffControlMode_Automatic",
    "BDP_Info_HediffControlMode_AbilityButton"))
{
    Assert-True ($translationKeys -contains $key) "缺少芯片信息译名键：$key"
}

Write-Output "ChipInformationLocalizationRouting PASS"
