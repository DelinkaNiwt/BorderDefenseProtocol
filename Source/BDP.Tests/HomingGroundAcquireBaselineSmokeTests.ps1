# 空地检索首段与捕获基线回归测试。

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$configText = Get-Utf8Text (Join-Path $modRoot "Source\BDP.Content\RangedModules\Homing\HomingConfig.cs")
$moduleText = Get-Utf8Text (Join-Path $modRoot "Source\BDP.Content\RangedModules\Homing\HomingModule.cs")
$defText = Get-Utf8Text (Join-Path $modRoot "1.6\Content\Defs\RangedModuleDef\Homing.xml")

Assert-True (
    ($configText -match '\bGroundTargetInitialSegmentTriggerRatio\b') -and
    ($defText -match '<GroundTargetInitialSegmentTriggerRatio>1</GroundTargetInitialSegmentTriggerRatio>') -and
    ($moduleText -match 'groundAcquirePending\s*\?\s*frozenConfig\.GroundTargetInitialSegmentTriggerRatio\s*:\s*frozenConfig\.InitialSegmentTriggerRatio')
) "空地目标首段比例必须由独立 Def 配置决定。"

Assert-True (
    ($moduleText -match 'state\.LockedTarget\s*=\s*acquired') -and
    ($moduleText -match 'state\.LastObservedTargetPos\s*=\s*acquiredPosition') -and
    ($moduleText -match 'state\.HasLastDistanceSample\s*=\s*false')
) "捕获实体后必须以捕获瞬间位置重建运动基线并清除旧距离样本。"

Write-Output "HomingGroundAcquireBaselineSmokeTests PASS"
