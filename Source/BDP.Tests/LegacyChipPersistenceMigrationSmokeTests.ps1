# 旧版制造芯片持久化格式迁移测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$thingCompPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Thing\CompManufacturedChip.cs"
$collectorPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Migration\InvalidChipItemCollector.cs"
$replacementPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Migration\InvalidChipReplacementService.cs"

$thingCompText = Get-Utf8Text $thingCompPath
$collectorText = Get-Utf8Text $collectorPath
$replacementText = Get-Utf8Text $replacementPath

Assert-True ($thingCompText -match 'sourcePresetDefNames') "成品组件必须读取旧版制造来源字段。"
Assert-True ($thingCompText -match 'sourceGunClassDefName') "成品组件必须读取旧版武器类别字段。"
Assert-True ($thingCompText -match 'customLabel') "成品组件必须识别旧版实例名称字段。"
Assert-True ($thingCompText -match 'LegacyPersistence') "成品组件必须公开旧版持久化格式标记。"
Assert-True ($collectorText -match 'LegacyPersistence') "迁移收集器必须收集没有当前组合记录但命中旧格式的芯片。"
Assert-True ($replacementText -match 'LegacyPersistence') "旧版持久化格式必须直接按非法芯片替换。"

Write-Host "PASS: 旧版制造来源格式可被识别、收集并进入非法芯片迁移。"
