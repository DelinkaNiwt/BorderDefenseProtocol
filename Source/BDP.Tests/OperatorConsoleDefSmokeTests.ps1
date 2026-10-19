$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$thingPath = Join-Path $root '1.6\Content\Defs\ThingDef\Buildings\OperatorConsole.xml'
$jobPath = Join-Path $root '1.6\Content\Defs\JobDef\OperatorConsole.xml'
$workPath = Join-Path $root '1.6\Content\Defs\WorkGiverDef\OperatorConsole.xml'
foreach ($path in @($thingPath,$jobPath,$workPath)) { Assert-True (Test-Path $path) "缺少控制台 Def 文件：$path" }
$thing = Get-Content -Raw $thingPath
$work = Get-Content -Raw $workPath
Assert-True ($thing -match '<size>\(3,2\)</size>' -and $thing -match 'CommsConsole' -and $thing -match 'CompPowerTrader') '建筑 Def 必须固定 3×2、通信台贴图和原版电力组件。'
Assert-True ($thing -match '<defaultPlacingRot>South</defaultPlacingRot>' -and $thing -match '<interactionCellOffset>\(0,0,2\)</interactionCellOffset>') '控制台必须沿用原版通信台的朝向和交互点方向。'
Assert-True ($work -match 'directOrderable') 'WorkGiverDef 必须标记为可直接指派。'

Write-Host 'OperatorConsoleDef PASS'
