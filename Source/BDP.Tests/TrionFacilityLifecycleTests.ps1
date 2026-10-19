$ErrorActionPreference = 'Stop'
# 编译真实资源与绑定源码；只隔离原版宿主，完整寻路和存档解析留游戏验证。
$facilityRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'BDP.Content/Trion/Facility'
$paths = @(
    (Join-Path $PSScriptRoot 'TrionFacilityLifecycleTests.cs'),
    (Join-Path $facilityRoot 'CompTrionFacilityResource.cs'),
    (Join-Path $facilityRoot 'CompProperties_TrionFacilityResource.cs'),
    (Join-Path $facilityRoot 'TrionFacilityDrainRateRegistry.cs'),
    (Join-Path $facilityRoot 'ITrionFacilityDrainRateProvider.cs'),
    (Join-Path $facilityRoot 'CompTrionFacilityControl.cs'),
    (Join-Path $facilityRoot 'Sacrifice/TrionFacilitySacrificeSettingsDef.cs'),
    (Join-Path $facilityRoot 'Sacrifice/TrionFacilitySacrificeUtility.cs'))
Add-Type -Path $paths
[BDP.Tests.TrionFacilityLifecycleTests]::Run()

# 正式定义和语言键接线检查；不以纯文本说明代替实际组件及任务配置。
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$defsRoot = Join-Path $modRoot '1.6/Content/Defs'
$languageRoot = Join-Path $modRoot 'Languages/ChineseSimplified (简体中文)'
[xml]$facilityXml = Get-Content -Raw -LiteralPath (Join-Path $defsRoot 'ThingDef/Buildings/TrionFacility.xml')
$controlConfig = $facilityXml.SelectSingleNode('/Defs/ThingDef/comps/li[@Class="BDP.Content.Trion.Facility.CompProperties_TrionFacilityControl"]')
if ($null -eq $controlConfig) { throw '实际核心必须挂载皇室归属业务组件。' }
$sacrificeConfig = $facilityXml.SelectSingleNode('/Defs/ThingDef/comps/li[@Class="BDP.Content.Trion.Facility.Sacrifice.CompProperties_TrionFacilitySacrifice"]')
if ($null -eq $sacrificeConfig -or $facilityXml.Defs.ThingDef.inspectorTabs.li -notcontains 'BDP.Content.Trion.Facility.Sacrifice.ITab_TrionFacilitySacrifice') {
    throw '协作搬运必须接通实际容器组件与供物内容页。'
}
[xml]$geneXml = Get-Content -Raw -LiteralPath (Join-Path $defsRoot 'GeneDef/RoyalBloodline.xml')
[xml]$contractXml = Get-Content -Raw -LiteralPath (Join-Path $defsRoot 'HediffDef/RoyalContract.xml')
if ($controlConfig.royalGene -ne $geneXml.Defs.GeneDef.defName -or $controlConfig.contractHediff -ne $contractXml.Defs.HediffDef.defName) {
    throw '核心身份及契约配置必须引用真实正式定义。'
}
[xml]$jobsXml = Get-Content -Raw -LiteralPath (Join-Path $defsRoot 'JobDef/TrionFacility.xml')
[xml]$jobLanguageXml = Get-Content -Raw -LiteralPath (Join-Path $languageRoot 'DefInjected/JobDef/TrionFacility.xml')
foreach ($jobName in @('BDP_BindTrionFacility', 'BDP_UnbindTrionFacility')) {
    $jobConfig = $jobsXml.Defs.JobDef | Where-Object defName -EQ $jobName
    if ($null -eq $jobConfig -or $jobConfig.driverClass -ne 'BDP.Content.Trion.Facility.Jobs.JobDriver_ChangeTrionFacilityBinding') { throw ('未接通正式任务：' + $jobName) }
    if ($null -eq $jobLanguageXml.LanguageData.SelectSingleNode($jobName + '.reportString')) { throw ('任务缺少本地化报告：' + $jobName) }
}
$languageKeys = @{}
foreach ($languageFile in @('Keyed/Gameplay.xml', 'Keyed/TrionFacilityOperations.xml')) {
    [xml]$keyXml = Get-Content -Raw -LiteralPath (Join-Path $languageRoot $languageFile)
    foreach ($node in $keyXml.LanguageData.ChildNodes) { if ($node.NodeType -eq 'Element') { $languageKeys[$node.Name] = $true } }
}
foreach ($codePath in @('CompTrionFacilityResource.cs', 'CompTrionFacilityControl.cs', 'Jobs/JobDriver_ChangeTrionFacilityBinding.cs',
    'Jobs/JobDriver_SacrificeToTrionFacility.cs', 'Sacrifice/Dialog_TrionFacilitySacrifice.cs', 'Sacrifice/TrionFacilitySacrificeSettingsDef.cs',
    'Sacrifice/CompTrionFacilitySacrifice.cs', 'Sacrifice/ITab_TrionFacilitySacrifice.cs')) {
    $codeText = Get-Content -Raw -LiteralPath (Join-Path $facilityRoot $codePath)
    foreach ($match in [regex]::Matches($codeText, '"(BDP_TrionFacility_[A-Za-z0-9_]+)"')) {
        if (-not $languageKeys.ContainsKey($match.Groups[1].Value)) { throw ('正式功能缺少语言键：' + $match.Groups[1].Value) }
    }
}
Write-Output '通过：正式组件、身份定义、绑定任务与简体中文语言键接线。'
