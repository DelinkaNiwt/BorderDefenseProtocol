$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$compPath = Join-Path $sourceRoot 'BDP.Content\OperatorConsole\CompOperatorConsole.cs'
$languagePath = Join-Path (Split-Path -Parent $sourceRoot) 'Languages\ChineseSimplified (简体中文)\Keyed\Gameplay.xml'
$compText = Get-Content -Raw $compPath
$languageText = Get-Content -Raw $languagePath
Assert-True ($compText -match 'CompGetGizmosExtra' -and $compText -match 'Command_Toggle') '控制台必须提供强制运作切换按钮。'
Assert-True ($compText -match 'DebugSettings\.ShowDevGizmos' -and $compText -match 'DebugSettings\.godMode') '强制运作按钮必须受开发 Gizmo/上帝模式限制。'
Assert-True ($compText -match 'Scribe_Values\.Look' -and $compText -match 'forcedOperational') '强制运作状态必须持久化。'
Assert-True ($compText -notmatch 'CompPowerTrader.*PowerOn\s*=|PowerOn\s*=') '强制运行不得修改原版电力组件状态。'
Assert-True ($languageText -match 'BDP_OperatorConsole_Debug_ForceOperation') '强制运作按钮必须使用简体中文语言键。'

Write-Host 'OperatorConsoleDebugGizmo PASS'
