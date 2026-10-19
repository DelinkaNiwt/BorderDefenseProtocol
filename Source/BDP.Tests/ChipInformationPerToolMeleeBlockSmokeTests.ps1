# 芯片信息面板逐 Tool（工具）近战信息块冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$publisherPath = Join-Path $modRoot "Source\BDP.Content\Information\ManufacturedChipInfoCardPublisher.cs"
$surfacePath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurface.cs"
$factoryPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceFactory.cs"
$projectionPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaMeleeStatProjection.cs"
$languagePath = Join-Path $modRoot "Languages\ChineseSimplified (简体中文)\Keyed\ChipInformation.xml"

foreach ($path in @($publisherPath, $surfacePath, $factoryPath, $projectionPath, $languagePath)) {
    Assert-True (Test-Path -LiteralPath $path) ("缺少逐 Tool 近战信息文件：" + $path)
}

$publisherText = Get-Utf8Text $publisherPath
$surfaceText = Get-Utf8Text $surfacePath
$factoryText = Get-Utf8Text $factoryPath
$projectionText = Get-Utf8Text $projectionPath
$languageText = Get-Utf8Text $languagePath

Assert-True ($publisherText -match "AddPerToolMeleeEntries") "发布器必须按工具逐个追加近战信息块。"
Assert-True ($publisherText -match "VanillaMeleeStatProjection\.DamageLabel" -and
    $publisherText -match "VanillaMeleeStatProjection\.ArmorPenetrationLabel") "每个工具块必须发布原版近战两个字段名称。"
Assert-True ($publisherText -match "武器\(近战\)|BDP_Info_Category_MeleeTool") "近战动态分类必须使用武器(近战)·工具名称格式。"
Assert-True ($publisherText -match "VanillaMeleeStatProjection") "近战新增块必须使用独立的多击计算适配器。"
Assert-True ($surfaceText -match "Tool") "近战统计表面必须能够标识单个 Tool。"
Assert-True ($factoryText -match "TryCreateMeleeTool") "原版表面工厂必须支持按单个 Tool 创建读取表面。"
Assert-True ($projectionText -match "HitCount|BurstShotCount" -and
    $projectionText -match "HitInterval|TicksBetweenBurstShots") "多击近战计算必须读取次数和间隔。"
Assert-True ($projectionText -match "AdjustedMeleeDamageAmount" -and
    $projectionText -match "AdjustedArmorPenetration") "近战计算必须复用原版伤害和护甲穿透调整入口。"
Assert-True ($languageText -match "BDP_Info_Category_MeleeTool") "逐工具近战分类标题必须使用语言键。"

Write-Output 'ChipInformationPerToolMeleeBlockSmokeTests PASS'
