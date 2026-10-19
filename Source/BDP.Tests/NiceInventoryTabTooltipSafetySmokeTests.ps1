# NIT 动态武器悬浮提示安全边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$targetsPath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabDiscovery.cs"
$patchesPath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabPatches.cs"
$adapterPath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabAdapter.cs"
$runtimePath = Join-Path $modRoot "Source\BDP.Compatibility\NiceInventoryTab\NiceInventoryTabRuntime.cs"

foreach ($path in @($targetsPath, $patchesPath, $adapterPath, $runtimePath)) {
    Assert-True (Test-Path -LiteralPath $path) ("缺少 NIT 提示安全文件：" + $path)
}

$targetsText = Get-Utf8Text $targetsPath
$patchesText = Get-Utf8Text $patchesPath
$adapterText = Get-Utf8Text $adapterPath
$runtimeText = Get-Utf8Text $runtimePath

foreach ($methodName in @(
    "WeaponReloadTimeShort",
    "WeaponStoppingPower",
    "WeaponFireRate",
    "WeaponMeleeAttackSpeed",
    "WeaponMeleeAccuracy",
    "WeaponMeleeDamage"
)) {
    Assert-True ($targetsText -match [regex]::Escape($methodName)) ("必须发现 NIT 提示方法：" + $methodName)
}

Assert-True ($patchesText -match "TooltipPrefix") "必须存在 NIT 提示前缀补丁入口。"
Assert-True ($adapterText -match "WeaponStoppingPower" -and $adapterText -match "WeaponFireRate") "适配器必须安装可空远程提示补丁。"
Assert-True ($adapterText -match "PatchCore\s*\(") "NIT 核心补丁必须与可选提示补丁分离。"
Assert-True ($adapterText -match "PatchOptionalTooltips\s*\(") "NIT 必须有独立的可选提示补丁阶段。"
Assert-True ($adapterText -match "TryPatchTooltip\s*\(") "单个 NIT 提示补丁失败时必须局部隔离。"
Assert-True ($adapterText -match "TryPatchTooltip[\s\S]*?catch\s*\(") "NIT 单个提示补丁必须捕获安装异常，不能回滚核心补丁。"
Assert-True ($runtimeText -match "TryBuildTooltip") "兼容层必须从 BDP 快照构建安全提示文本。"
Assert-True ($runtimeText -match "TryGetSnapshot") "动态提示必须读取当前 BDP 快照。"
Assert-True ($runtimeText -match "RangedStoppingPower" -and $runtimeText -match "RangedFireRate") "远程停止能力和射速提示必须有独立安全分支。"

Write-Output 'NiceInventoryTabTooltipSafetySmokeTests PASS'
