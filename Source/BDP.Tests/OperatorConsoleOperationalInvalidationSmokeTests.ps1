$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$consoleRoot = Join-Path $sourceRoot 'BDP.Content\OperatorConsole'
$jobPath = Join-Path $consoleRoot 'JobDriver_OperateOperatorConsole.cs'
$providerPath = Join-Path $consoleRoot 'OperatorConsolePermissionProvider.cs'
$jobText = Get-Content -Raw $jobPath
$providerText = Get-Content -Raw $providerPath
$toilMatch = [regex]::Match(
    $jobText,
    'operate\.tickIntervalAction\s*=\s*delegate\(int delta\)\s*\{(?<body>.*?)\n\s*\};',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)

Assert-True $toilMatch.Success '未找到操作员控制台持续值守 Toil。'
$toilBody = $toilMatch.Groups['body'].Value
Assert-True ($toilBody -match 'NotifyOperatorHeartbeat') '值守循环必须刷新操作员心跳。'
Assert-True ($toilBody -match 'IsOperational' -and $toilBody -match 'IsForcedOperational') '值守循环必须复查统一运作状态并识别强制运作。'
Assert-True ($toilBody -match 'EndJobWith\s*\(\s*JobCondition\.Incompletable') '上游许可、电力或操作员条件失效时必须结束值守工作。'
Assert-True ($providerText -match 'console\.IsOperational' -and $providerText -notmatch 'HasPower|HasPermission|operatorPawn') '下游许可提供器仍只能读取控制台统一运作状态。'

Write-Host 'OperatorConsoleOperationalInvalidation PASS'
