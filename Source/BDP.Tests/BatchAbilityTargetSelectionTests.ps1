param([Parameter(Mandatory = $true)][ValidateSet('1.6', '1.7')][string]$RimWorldVersion)
$ErrorActionPreference = 'Stop'
# 验证所选版本实际程序集的有序目标筛选，不加载另一版本或模拟游戏地图。
$selectionModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$selectionAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $selectionModRoot "$RimWorldVersion/Assemblies/BDP.Core.dll"))
$selectionType = $selectionAssembly.GetType('BDP.Core.Abilities.BatchAbilityTargetSelection', $false)
if ($null -eq $selectionType) { throw '缺少有序目标筛选入口：尚不能仅跳过非法中间格。' }
$selectionMethod = $selectionType.GetMethod('TrySelect')
function Assert-TargetSelection([bool[]]$validity, [bool]$skip, [bool]$expected, [string]$indices, [string]$description) {
    $selectionArgs = [object[]]@($validity, $skip, $null)
    $selectionResult = $selectionMethod.Invoke($null, $selectionArgs)
    if ($selectionResult -ne $expected -or ($selectionArgs[2] -join ',') -ne $indices) { throw "$description：结果或有序目标不符合预期。" }
}
Assert-TargetSelection @() $true $false '' '空请求必须拒绝'
Assert-TargetSelection @($true) $true $true '0' '单格合法目标可以生成'
Assert-TargetSelection @($false) $true $false '' '单格非法端点不能被跳过'
Assert-TargetSelection @($false,$true,$true) $true $false '' '起点非法不能缩短请求'
Assert-TargetSelection @($true,$true,$false) $true $false '' '终点非法不能缩短请求'
Assert-TargetSelection @($true,$false,$true,$false,$true) $true $true '0,2,4' '两端合法时只跳过中间障碍'
Assert-TargetSelection @($true,$false,$true) $false $false '' '默认严格模式仍整批拒绝'
Assert-TargetSelection @($true,$true,$true) $false $true '0,1,2' '严格模式合法请求保持顺序'
$selectionArgs = [object[]]@([bool[]]@($true,$true,$false,$true,$true,$false,$true,$true), $true, $null)
if (-not $selectionMethod.Invoke($null, $selectionArgs)) { throw '八格中间两处阻挡应允许生成六格。' }
$selectionBudget = $selectionAssembly.GetType('BDP.Core.Abilities.BatchAbilityBudget').GetMethod('TotalCost')
$selectionCost = $selectionBudget.Invoke($null, [object[]]@([single]20,[int]$selectionArgs[2].Count))
if ($selectionArgs[2].Count -ne 6 -or $selectionCost -ne 120) { throw '只能按实际六格支付六次和一百二十触力能。' }
Write-Output "BatchAbilityTargetSelectionTests $RimWorldVersion PASS"
