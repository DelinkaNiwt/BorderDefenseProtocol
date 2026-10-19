# NIT 投影版本刷新边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$providerPath = Join-Path $modRoot "Source\BDP\Core\Information\TriggerBodyAutomaticAttackDisplayProvider.cs"
$targetsPath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabDiscovery.cs"
$adapterPath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabAdapter.cs"
$runtimePath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabRuntime.cs"
$projectionPath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabProjectionAdapter.cs"

foreach ($path in @($providerPath, $targetsPath, $adapterPath, $runtimePath, $projectionPath)) {
    Assert-True (Test-Path -LiteralPath $path) ("缺少 NIT 同步修复文件：" + $path)
}

$providerText = Get-Utf8Text $providerPath
$targetsText = Get-Utf8Text $targetsPath
$adapterText = Get-Utf8Text $adapterPath
$runtimeText = Get-Utf8Text $runtimePath
$projectionText = Get-Utf8Text $projectionPath

Assert-True ($providerText -match "TryGetProjectionVersion") "Core 必须提供只读投影版本读取入口。"
Assert-True ($runtimeText -match "EnsureEquippedItemSynchronized") "NIT 绘制前必须检查装备对象的投影版本。"
Assert-True ($runtimeText -match "EnsureInfoWidgetSynchronized") "NIT 悬浮信息绘制前必须检查投影版本。"
Assert-True ($runtimeText -match "ProjectionVersion") "NIT 同步必须以投影版本为失效依据。"
Assert-True ($projectionText -match "UpdateStats") "自动攻击失效时必须能恢复 NIT 原版统计。"
Assert-True ($targetsText -match "InfoWidgetSetItem" -and $targetsText -match "InfoWidgetDraw") "必须发现 NIT 信息栏重建和绘制入口。"
Assert-True ($adapterText -match "InfoWidgetSetItem" -and $adapterText -match "InfoWidgetDraw") "适配器必须安装信息栏同步补丁。"
Assert-True ($runtimeText -match "ConditionalWeakTable") "同步标记必须使用弱引用容器，不能长期持有 NIT UI 对象。"

Write-Output 'NiceInventoryTabProjectionRefreshSmokeTests PASS'
