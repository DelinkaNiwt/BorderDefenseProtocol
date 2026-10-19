# 蚱蜢/弹射砍击共享链式落地回调边界冒烟测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$effectPath = Join-Path $modRoot "Source\BDP.Content\Grasshopper\CompAbilityEffect_GrasshopperJump.cs"
$effectText = Get-Utf8Text $effectPath

Assert-True (Test-Path -LiteralPath $effectPath) "缺少蚱蜢共享跳跃效果源码。"

# 链式落地回调属于 GroundDash（中性地面冲刺）公共设施，不能绑定蚱蜢专属飞行器。
Assert-True (
    $effectText -match 'PawnFlyer_GroundDash\s+\w+\s*=\s*flyer\s+as\s+PawnFlyer_GroundDash'
) "链式落地回调必须按中性 PawnFlyer_GroundDash 接线。"
Assert-True (
    $effectText -notmatch 'flyer\s+as\s+PawnFlyer_Grasshopper'
) "链式落地回调不得继续依赖蚱蜢专属飞行器类型。"

Write-Output 'GrasshopperChainCallbackSmokeTests PASS'
