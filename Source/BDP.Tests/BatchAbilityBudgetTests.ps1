param([Parameter(Mandatory = $true)][ValidateSet('1.6', '1.7')][string]$RimWorldVersion)
$ErrorActionPreference = 'Stop'
# 直接验证所选版本的实际 Core 预算入口；不借用另一版本产物。
$batchModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$batchAssemblyPath = Join-Path $batchModRoot "$RimWorldVersion/Assemblies/BDP.Core.dll"
if (-not (Test-Path -LiteralPath $batchAssemblyPath)) { throw "缺少 $RimWorldVersion Core 程序集。" }
$batchAssembly = [Reflection.Assembly]::LoadFrom($batchAssemblyPath)
$batchType = $batchAssembly.GetType('BDP.Core.Abilities.BatchAbilityBudget', $false)
if ($null -eq $batchType) { throw '缺少批量能力预算入口：不能按目标数量结算。' }
$batchTotal = $batchType.GetMethod('TotalCost')
$batchRequired = $batchType.GetMethod('RequiredCost')
if ($null -eq $batchTotal -or $null -eq $batchRequired) { throw '预算入口不完整。' }
function Assert-BatchEqual($actual, $expected, [string]$reason) {
    if ([Math]::Abs([double]$actual - [double]$expected) -gt 0.0001) { throw "$reason：实际 $actual，预期 $expected。" }
}
Assert-BatchEqual ($batchTotal.Invoke($null, [object[]]@([single]10, [int]5))) 50 '五个目标必须支付五份费用'
Assert-BatchEqual ($batchTotal.Invoke($null, [object[]]@([single]0, [int]8))) 0 '免费能力仍接受有效数量'
Assert-BatchEqual ($batchRequired.Invoke($null, [object[]]@([single]10, [single]80, [int]5))) 80 '最低门槛不因批量被忽略'
Assert-BatchEqual ($batchRequired.Invoke($null, [object[]]@([single]10, [single]20, [int]5))) 50 '总费用超过最低门槛时按总费用检查'
foreach ($batchInvalid in @(@([single]10,[int]0), @([single]10,[int]-1), @([single]-1,[int]1), @([single]::NaN,[int]1), @([single]::PositiveInfinity,[int]1), @([single]::MaxValue,[int]2))) {
    $batchRejected = $false
    try { $null = $batchTotal.Invoke($null, [object[]]$batchInvalid) } catch { $batchRejected = $true }
    if (-not $batchRejected) { throw '不合法数量或非有限总费用必须被拒绝。' }
}
foreach ($batchMinimum in @([single]-1, [single]::NaN, [single]::PositiveInfinity)) {
    $batchRejected = $false
    try { $null = $batchRequired.Invoke($null, [object[]]@([single]10, $batchMinimum, [int]1)) } catch { $batchRejected = $true }
    if (-not $batchRejected) { throw '不合法最低门槛必须被拒绝。' }
}
Write-Output "BatchAbilityBudgetTests $RimWorldVersion PASS"
