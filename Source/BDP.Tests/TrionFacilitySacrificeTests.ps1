$ErrorActionPreference = 'Stop'
# 编译正式收益和最终提交逻辑；只隔离原版宿主，不模拟寻路或完整存档。
$facilityRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'BDP.Content/Trion/Facility'
$utilityPath = Join-Path $facilityRoot 'Sacrifice/TrionFacilitySacrificeUtility.cs'
$paths = @(
    (Join-Path $PSScriptRoot 'TrionFacilityLifecycleTests.cs'),
    (Join-Path $PSScriptRoot 'TrionFacilitySacrificeTests.cs'),
    (Join-Path $PSScriptRoot 'TrionFacilitySacrificeGroupingTests.cs'),
    (Join-Path $facilityRoot 'CompTrionFacilityResource.cs'),
    (Join-Path $facilityRoot 'CompProperties_TrionFacilityResource.cs'),
    (Join-Path $facilityRoot 'CompTrionFacilityControl.cs'),
    (Join-Path $facilityRoot 'TrionFacilityDrainRateRegistry.cs'),
    (Join-Path $facilityRoot 'ITrionFacilityDrainRateProvider.cs'),
    (Join-Path $facilityRoot 'Sacrifice/TrionFacilitySacrificeSettingsDef.cs'),
    (Join-Path $facilityRoot 'Sacrifice/TrionFacilitySacrificeSupplyGroup.cs'),
    $utilityPath)
Add-Type -Path $paths
[BDP.Tests.TrionFacilitySacrificeTests]::Run()
[BDP.Tests.TrionFacilitySacrificeGroupingTests]::Run()

# 正式配置仅提供科技例外和收益区间，不再设置类别白名单。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$defsRoot = Join-Path $modRoot '1.6/Content/Defs'
[xml]$settingsXml = Get-Content -Raw -LiteralPath (Join-Path $defsRoot 'TrionFacilitySacrificeSettingsDef/Settings.xml')
$settings = $settingsXml.SelectSingleNode('/Defs/*')
if ($settings.Name -ne [BDP.Content.Trion.Facility.Sacrifice.TrionFacilitySacrificeSettingsDef].FullName -or
    $settings.defName -ne 'BDP_TrionFacilitySacrificeSettings' -or [float]$settings.referenceMarketValue -le 0) { throw '正式献祭定义或参考市值缺失。' }
if ($null -ne $settings.SelectSingleNode('allowedThings')) { throw '纯科技准入不能残留类别过滤配置。' }
$config = [BDP.Content.Trion.Facility.Sacrifice.TrionFacilitySacrificeSettingsDef]::new()
$config.referenceMarketValue = [float]$settings.referenceMarketValue
foreach ($tier in $settings.yieldTiers.li) {
    $range = [BDP.Content.Trion.Facility.Sacrifice.TrionFacilitySacrificeYieldTier]::new()
    $range.techLevel = [RimWorld.TechLevel]([Enum]::Parse([RimWorld.TechLevel], $tier.techLevel))
    $range.minYield = [float]$tier.minYield
    $range.maxYield = [float]$tier.maxYield
    $config.yieldTiers.Add($range)
}
if (@($config.ConfigErrors()).Count -ne 0) { throw '实际XML收益区间没有通过正式配置校验。' }
if (@($settings.techLevelOverrides.li).Count -ne 2) { throw '只保留普通和高级零部件两项例外。' }
foreach ($componentEntry in @(@('ComponentIndustrial', 'Industrial'), @('ComponentSpacer', 'Spacer'))) {
    $component = @($settings.techLevelOverrides.li | Where-Object thing -EQ $componentEntry[0])
    if ($component.Count -ne 1 -or $component[0].techLevel -ne $componentEntry[1]) { throw ('组件缺少正确科技例外：' + $componentEntry[0]) }
}
# 用户已指定BDP成品芯片与便携检测器为原生太空档，献祭直接读取该字段。
foreach ($productEntry in @(@('Items/Chips.xml', 'BDP_Chip_Custom'), @('Items/Trion.xml', 'BDP_TrionPortableDetector'))) {
    [xml]$productXml = Get-Content -Raw -LiteralPath (Join-Path $defsRoot ('ThingDef/' + $productEntry[0]))
    $product = $productXml.SelectSingleNode("/Defs/ThingDef[defName='$($productEntry[1])']")
    if ($null -eq $product -or $product.techLevel -ne 'Spacer') { throw ('BDP成品缺少太空科技标注：' + $productEntry[1]) }
}
[xml]$jobsXml = Get-Content -Raw -LiteralPath (Join-Path $defsRoot 'JobDef/TrionFacility.xml')
$job = $jobsXml.Defs.JobDef | Where-Object defName -EQ 'BDP_SacrificeToTrionFacility'
if ($job.driverClass -ne 'BDP.Content.Trion.Facility.Jobs.JobDriver_SacrificeToTrionFacility') { throw '献祭按钮没有正式任务驱动配置。' }
[xml]$jobLanguageXml = Get-Content -Raw -LiteralPath (Join-Path $modRoot 'Languages/ChineseSimplified (简体中文)/DefInjected/JobDef/TrionFacility.xml')
if ($null -eq $jobLanguageXml.LanguageData.SelectSingleNode('BDP_SacrificeToTrionFacility.reportString')) { throw '献祭任务报告缺少中文语言键。' }
Write-Output '通过：科技准入配置、组件例外、BDP科技标注、正式任务和语言包接线。'
