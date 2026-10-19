# 触发体动态自动攻击信息边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$publisherPath = Join-Path $modRoot "Source\BDP\Core\Information\TriggerBodyWeaponInfoPublisher.cs"
$factoryPath = Join-Path $modRoot "Source\BDP\Core\Information\Vanilla\VanillaWeaponStatSurfaceFactory.cs"
$patchPath = Join-Path $modRoot "Source\BDP.Content\Patches\Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo.cs"
$triggerPath = Join-Path $modRoot "Source\BDP\Core\Trigger\State\CompTriggerBody.cs"
$executionPath = Join-Path $modRoot "Source\BDP\Core\AttackExecution\AttackExecutionSurfaceAccess.cs"

Assert-True (Test-Path -LiteralPath $publisherPath) "必须存在触发体动态武器信息发布器。"
Assert-True (Test-Path -LiteralPath $patchPath) "必须存在信息卡边界补丁。"

$publisherText = Get-Utf8Text $publisherPath
$factoryText = Get-Utf8Text $factoryPath
$patchText = Get-Utf8Text $patchPath
$triggerText = Get-Utf8Text $triggerPath
$executionText = Get-Utf8Text $executionPath

Assert-True ($publisherText -match "PublishedCombatProjection") "动态信息必须读取已发布战斗投影。"
Assert-True ($publisherText -match "PrimaryMelee" -and $publisherText -match "PrimaryRanged") "动态信息必须使用默认主近战或远程结果。"
Assert-True ($publisherText -match "VanillaWeaponStatSurfaceFactory" -and $publisherText -match "VanillaWeaponStatSurfaceReader") "动态信息必须复用原版武器统计表面设施。"
Assert-True ($factoryText -match "FormalExpressionResult") "原版统计表面必须支持正式表达结果输入。"
Assert-True ($patchText -match "CompTriggerBody" -and $patchText -match "StatsToDraw") "补丁必须只在真实原版物品统计边界接线。"
Assert-True ($patchText -match "nativeWeaponEntries" -and $patchText -match "replacementStats") "补丁必须按原版产出实例和实际统计定义替换，不能按武器分类整体删除。"
Assert-True ($patchText -match "TryBuild|TryPublish|TryCreate") "动态读取失败时必须有明确的成功判定入口。"
Assert-True ($triggerText -notmatch "ThingDef\.tools\s*=|\.tools\s*=\s*new") "触发体信息改动不得写回 ThingDef 工具列表。"
Assert-True ($publisherText -notmatch "TryExecuteAuto|AttackExecutionSurfaceAccess") "信息发布器不得调用攻击执行入口。"
Assert-True ($executionText -notmatch "TriggerBodyWeaponInfoPublisher|SpecialDisplayStats") "攻击执行表面不得反向依赖信息面板。"

Write-Output 'TriggerBodyDynamicWeaponInfoSmokeTests PASS'
