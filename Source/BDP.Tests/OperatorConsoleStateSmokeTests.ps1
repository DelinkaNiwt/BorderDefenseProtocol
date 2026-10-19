$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$consoleRoot = Join-Path $sourceRoot 'BDP.Content\OperatorConsole'
foreach ($file in @('Building_OperatorConsole.cs','CompOperatorConsole.cs','OperatorConsolePermissionProvider.cs')) {
    Assert-True (Test-Path (Join-Path $consoleRoot $file)) "缺少控制台状态文件：$file"
}
$compText = Get-Content -Raw (Join-Path $consoleRoot 'CompOperatorConsole.cs')
$providerText = Get-Content -Raw (Join-Path $consoleRoot 'OperatorConsolePermissionProvider.cs')
Assert-True ($compText -match 'forcedOperational' -and $compText -match 'CompPowerTrader' -and $compText -match 'HasPermission') '控制台必须组合强制运行、电力和上游许可状态。'
Assert-True ($compText -match 'operator|操作员') '控制台必须持有运行时操作员心跳。'
Assert-True ($compText -match 'IsOperational') '控制台必须暴露统一运作状态。'
Assert-True ($providerText -match 'IsOperational' -and $providerText -notmatch 'CompPowerTrader|HasPermission|operatorPawn') '下游许可提供器只能读取 IsOperational。'

Write-Host 'OperatorConsoleState PASS'
