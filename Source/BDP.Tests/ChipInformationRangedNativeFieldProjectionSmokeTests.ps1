# 远程原版字段投影冒烟测试：精度四档和远程冷却交给原版统计系统显示。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$factoryPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceFactory.cs"
$readerPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceReader.cs"
$publisherPath = Join-Path $modRoot "Source\BDP.Content\Information\ManufacturedChipInfoCardPublisher.cs"

$factoryText = Get-Utf8Text $factoryPath
$readerText = Get-Utf8Text $readerPath
$publisherText = Get-Utf8Text $publisherPath

foreach ($marker in @(
    "StatDefOf.AccuracyTouch",
    "StatDefOf.AccuracyShort",
    "StatDefOf.AccuracyMedium",
    "StatDefOf.AccuracyLong",
    "RangedWeapon_Cooldown",
    "statBases"
)) {
    Assert-True ($factoryText -match [regex]::Escape($marker)) ("远程临时定义缺少原版字段投影：" + $marker)
}

Assert-True ($readerText -match "SpecialDisplayStats") "远程字段仍必须由原版 SpecialDisplayStats 读取。"
Assert-True ($readerText -match "StatsReportUtility" -and $readerText -match "StatsToDraw") "远程 statBases 字段必须经过原版 StatsReportUtility 统计读取链。"
Assert-True ($publisherText -notmatch "AccuracyTouch|AccuracyShort|AccuracyMedium|AccuracyLong|RangedWeapon_Cooldown") "Content 发布器不得复制远程原版字段计算。"

Write-Output 'ChipInformationRangedNativeFieldProjectionSmokeTests PASS'
