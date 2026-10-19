$ErrorActionPreference = 'Stop'
# 架构边界检查；真实输入帧行为由 TargetingInputBoundaryTests 验证，鼠标流程另行实测。
$taskSource = Split-Path -Parent $PSScriptRoot
$taskCore = Join-Path $taskSource 'BDP/Core/AttackExecution'
$taskFacts = Get-Content -LiteralPath (Join-Path $taskCore 'TargetingProtocol/Interaction/TargetingInputRuntimeFacts.cs') -Raw -Encoding utf8
$taskSingle = Get-Content -LiteralPath (Join-Path $taskCore 'AttackExecutionTargetingSource.cs') -Raw -Encoding utf8
$taskGroup = Get-Content -LiteralPath (Join-Path $taskCore 'GroupedAttackExecutionTargetingSource.cs') -Raw -Encoding utf8
$taskRoute = Get-Content -LiteralPath (Join-Path $taskSource 'BDP.Content/RangedModules/RoutePath/RoutePathModule.cs') -Raw -Encoding utf8
if (Test-Path -LiteralPath (Join-Path $taskCore 'TargetingProtocol/Interaction/TargetingInputRuntimeScope.cs')) { throw '全局输入作用域未删除。' }
if (Test-Path -LiteralPath (Join-Path $taskSource 'BDP/Patches/Patch_Targeter_OrderPawnForceTarget_TargetingInput.cs')) { throw '全局输入补丁未删除。' }
if ($taskFacts -notmatch 'EventType.MouseDown') { throw '重绘和布局不应伪造左键点击。' }
foreach ($taskMethod in @('CanHitTarget', 'ValidateTarget', 'OrderForceTarget')) {
    if ($taskSingle -notmatch ('internal (bool|void) ' + $taskMethod + '\([^)]*TargetingInputRuntimeFacts inputFacts')) { throw "成员缺少显式输入入口：$taskMethod" }
    if ($taskGroup -notmatch ('source\.' + $taskMethod + '\(target, (false, )?inputFacts\)')) { throw "分组未传递同一输入：$taskMethod" }
}
if ($taskRoute -match 'TargetingInputRuntimeScope|Event\.current' -or $taskRoute -notmatch 'record.InputFrame.Modifiers') { throw '路径预览未从记录读取输入。' }
Write-Output 'TargetingInputModifierBridgeSmokeTests PASS：自有界面采集，分组显式传递，无全局输入转运。'
