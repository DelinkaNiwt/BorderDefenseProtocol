# 成品芯片原版信息卡发布边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$publisherPath = Join-Path $modRoot "Source\BDP.Content\Information\ManufacturedChipInfoCardPublisher.cs"
$compPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Thing\CompManufacturedChip.cs"
$surfacePath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurface.cs"
$factoryPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceFactory.cs"
$readerPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceReader.cs"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\StatsReport.xml"

foreach ($path in @($publisherPath, $compPath, $surfacePath, $factoryPath, $readerPath, $languagePath)) {
    Assert-True (Test-Path -LiteralPath $path) ("缺少路线 E 文件：" + $path)
}

$publisherText = Get-Utf8Text $publisherPath
$compText = Get-Utf8Text $compPath
$factoryText = Get-Utf8Text $factoryPath
$readerText = Get-Utf8Text $readerPath
$languageText = Get-Utf8Text $languagePath

foreach ($marker in @(
    "ManufacturedChipInformationModelBuilder",
    "ChipExpressionContractSurfaceAccess",
    "ChipExpressionCapabilityResolver",
    "VanillaWeaponStatSurfaceFactory",
    "VanillaWeaponStatSurfaceReader",
    "PublishedFieldKeys",
    "StatDrawEntry"
)) {
    Assert-True ($publisherText -match [regex]::Escape($marker)) ("发布器缺少通用接线：" + $marker)
}

Assert-True ($publisherText -match 'HashSet<string>\s+PublishedFieldKeys') "原版信息卡必须有明确普通字段白名单。"
Assert-True ($publisherText -match 'HasExecutableRangedSurface') "发布器必须按事实判定远程能力。"
Assert-True ($publisherText -match 'HasExecutableMeleeSurface') "发布器必须按事实判定近战能力。"
Assert-True ($publisherText -notmatch 'MeleeWeapon_AverageDPS|MeleeWeapon_AverageArmorPenetration') "Content 发布器不得复制或硬编码原版近战公式入口。"
Assert-True ($publisherText -notmatch 'new\s+ChipCombinationResolver') "发布器不得重新解析组合记录。"
Assert-True ($publisherText -notmatch 'ChipInformationDetailRenderer') "发布器不得依赖装配台详情渲染器。"

Assert-True ($compText -match 'override\s+IEnumerable<StatDrawEntry>\s+SpecialDisplayStats') "成品组件必须覆盖原版 SpecialDisplayStats。"
Assert-True ($compText -match 'ManufacturedChipInfoCardPublisher\.Publish') "成品组件必须调用统一发布器。"
Assert-True ($compText -match 'TryGetCurrentResolution') "成品组件必须向发布器提供一次性解析结果。"

foreach ($forbidden in @(
    "DefDatabase<ThingDef>.Add",
    "GenSpawn.Spawn",
    "Scribe.",
    "HarmonyPatch"
)) {
    Assert-True ($factoryText -notmatch [regex]::Escape($forbidden)) ("原版表面工厂不得包含持久化或注入设施：" + $forbidden)
}

Assert-True ($readerText -match 'SpecialDisplayStats') "原版读取适配器必须调用 ThingDef.SpecialDisplayStats。"
Assert-True ($readerText -match 'MeleeWeapon_AverageDPS') "原版读取适配器必须读取近战平均 DPS。"
Assert-True ($readerText -match 'MeleeWeapon_AverageArmorPenetration') "原版读取适配器必须读取近战平均护甲穿透。"
Assert-True ($factoryText -match 'CloneProjectileDef') "原版表面工厂必须为投射物覆盖建立临时副本。"
Assert-True ($factoryText -match 'ProjectileOverrides') "原版表面工厂必须消费正规化投射物覆盖。"
Assert-True ($languageText -match 'BDP_VanillaWeaponStatSurfaceLabel') "临时原版表面标签必须有语言键。"

Write-Output 'ManufacturedChipInfoCardPublisherBoundarySmokeTests PASS'
