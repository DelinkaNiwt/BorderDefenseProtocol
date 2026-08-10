# 芯片半成品跨角色续作补丁测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$patchPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Patches\Patch_WorkGiver_DoBill_ChipUnfinishedThing.cs"
Assert-True (Test-Path -LiteralPath $patchPath) "缺少芯片半成品续作补丁。"
$patchText = Get-Utf8Text $patchPath

Assert-True ($patchText -match 'ClosestUnfinishedThingForBill') "补丁必须替换芯片分支的半成品寻找。"
Assert-True ($patchText -match 'FinishUftJob') "补丁必须替换芯片分支的续作 Job 构造。"
Assert-True ($patchText -match 'bill\s+is\s+Bill_ChipProduction') "补丁必须只处理芯片生产账单。"
Assert-True ($patchText -match 'SameConfigurationAs') "半成品与新账单必须按同组合记录匹配。"
Assert-True ($patchText -match 'IsFixedOrAllowedIngredient') "续作前必须确认半成品材料仍被账单允许。"
Assert-True ($patchText -match 'CanReserve') "续作候选必须可预留。"
Assert-True ($patchText -notmatch 'Creator\s*==|Creator\s*!=') "芯片续作不得比较原版 Creator。"
Assert-True ($patchText -match 'return\s+true;') "普通账单必须完整回退原版私有方法。"

Write-Host "PASS: 芯片半成品按配方、组合、材料与预留匹配，不绑定制作者。"
