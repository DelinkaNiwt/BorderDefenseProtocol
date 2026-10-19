$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$consoleRoot = Join-Path $sourceRoot 'BDP.Content\OperatorConsole'
foreach ($file in @('JobDriver_OperateOperatorConsole.cs','WorkGiver_OperateOperatorConsole.cs')) {
    Assert-True (Test-Path (Join-Path $consoleRoot $file)) "缺少操作工作文件：$file"
}
$workGiverText = Get-Content -Raw (Join-Path $consoleRoot 'WorkGiver_OperateOperatorConsole.cs')
$driverText = Get-Content -Raw (Join-Path $consoleRoot 'JobDriver_OperateOperatorConsole.cs')
Assert-True ($workGiverText -match 'DirectOrder|direct|直接指派') '工作分配器必须保留玩家直接指派入口。'
Assert-True ($driverText -match 'Reserve' -and $driverText -match 'InteractionCell') '操作工作必须预留建筑和交互格。'
Assert-True ($driverText -match 'Heartbeat|heartbeat|心跳' -and $driverText -match 'ClearOperator|清除') '操作工作必须持续心跳并在结束时清理操作员。'

Write-Host 'OperatorConsoleManualWork PASS'
