# 芯片动态零需求材料槽补丁边界测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$patchPath = Join-Path $modRoot "Source\BDP.Content\Assembly\ChipManufacturing\Patches\Patch_WorkGiver_DoBill_ZeroChipIngredients.cs"
Assert-True (Test-Path -LiteralPath $patchPath) "缺少零需求材料槽补丁。"

$patchText = Get-Utf8Text $patchPath
Assert-True ($patchText -match 'TryFindBestIngredientsInSet_NoMixHelper') "补丁必须只接入原版禁止混料分支的私有辅助方法。"
Assert-True ($patchText -match 'bill\s+is\s+Bill_ChipProduction') "补丁必须只处理芯片生产账单。"
Assert-True ($patchText -match 'new\s+List<IngredientCount>') "芯片账单必须复制材料槽列表，不能修改原列表。"
Assert-True ($patchText -match 'GetIngredientCount\s*\([^\)]*\)\s*>\s*0') "复制列表只应保留当前需求大于零的槽位。"
Assert-True ($patchText -notmatch 'TryFindBestBillIngredientsInSet_AllowMix') "补丁不得介入允许混料路径。"
Assert-True ($patchText -match 'return;') "普通原版账单必须立即放行。"

Write-Host "PASS: 零需求槽补丁只复制过滤芯片账单的禁止混料材料列表，普通账单不变。"
