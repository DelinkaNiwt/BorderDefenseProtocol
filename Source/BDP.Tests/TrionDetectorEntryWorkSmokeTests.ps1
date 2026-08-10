$ErrorActionPreference = 'Stop'

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$enterPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\WorkGivers\WorkGiver_EnterTrionDetector.cs'
$carryPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\WorkGivers\WorkGiver_CarryToTrionDetector.cs'
$defsPath = Join-Path $root '1.6\Content\Defs\WorkGivers\Trion\WorkGiverDefs_TrionDetector.xml'

foreach ($path in @($enterPath, $carryPath, $defsPath))
{
    Assert-True (Test-Path $path) "缺少固定检测仪入舱工作文件：$path"
}

$enter = Get-Content -Raw $enterPath
$carry = Get-Content -Raw $carryPath
$defs = Get-Content -Raw $defsPath

Assert-True ($enter -match 'WorkGiver_EnterTrionDetector\s*:\s*WorkGiver_EnterBuilding') '自行入舱必须复用原版 WorkGiver_EnterBuilding。'
Assert-True ($carry -match 'WorkGiver_CarryToTrionDetector\s*:\s*WorkGiver_CarryToBuilding') '搬运入舱必须复用原版 WorkGiver_CarryToBuilding。'
Assert-True ($enter -match 'ThingRequest\.ForDef\(TrionDetectorDef\)') '自行入舱工作必须只扫描固定 Trion 检测仪。'
Assert-True ($carry -match 'ThingRequest\.ForDef\(TrionDetectorDef\)') '搬运入舱工作必须只扫描固定 Trion 检测仪。'
Assert-True ($defs -match '<defName>BDP_EnterTrionDetector</defName>') '必须声明自行入舱工作分配器。'
Assert-True ($defs -match '<defName>BDP_CarryToTrionDetector</defName>') '必须声明搬运入舱工作分配器。'
Assert-True ($defs -match '<workType>Hauling</workType>') '搬运入舱必须属于原版搬运工作类型。'

Write-Host 'PASS: 固定 Trion 检测仪复用原版自行入舱和搬运工作。'
