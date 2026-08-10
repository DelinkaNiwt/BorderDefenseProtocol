$ErrorActionPreference = 'Stop'

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$workGiverPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\WorkGivers\WorkGiver_OperateTrionDetector.cs'
$driverPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Jobs\JobDriver_OperateTrionDetector.cs'
$buildingPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Building_TrionDetector.cs'
$jobDefsPath = Join-Path $root '1.6\Content\Defs\Jobs\Trion\JobDefs_TrionTalentAssessment.xml'
$workDefsPath = Join-Path $root '1.6\Content\Defs\WorkGivers\Trion\WorkGiverDefs_TrionDetector.xml'

foreach ($path in @($workGiverPath, $driverPath, $buildingPath, $jobDefsPath, $workDefsPath))
{
    Assert-True (Test-Path $path) "缺少固定检测仪研究操作文件：$path"
}

$workGiver = Get-Content -Raw $workGiverPath
$driver = Get-Content -Raw $driverPath
$building = Get-Content -Raw $buildingPath
$jobDefs = Get-Content -Raw $jobDefsPath
$workDefs = Get-Content -Raw $workDefsPath

Assert-True ($workGiver -match 'Level\s*<\s*10') '研究操作员必须有智识 10 级硬门槛。'
Assert-True ($workGiver -match 'CanBeOperatedBy') '工作分配器必须让建筑统一判断当前可操作状态。'
Assert-True ($driver -match 'StatDefOf\.ResearchSpeed') '操作速度必须受操作员研究速度影响。'
Assert-True ($driver -match 'StatDefOf\.ResearchSpeedFactor') '操作速度必须受建筑研究速度系数影响。'
Assert-True ($driver -match 'AddWork') '操作工作必须把累计工作量写回建筑。'
Assert-True ($driver -match 'SkillDefOf\.Intellectual') '操作过程必须使用并训练智识技能。'
Assert-True ($building -match 'CompletedWork') '固定检测仪必须持有跨操作员保留的累计工作量。'
Assert-True ($jobDefs -match '<defName>BDP_OperateTrionDetector</defName>') '必须声明固定检测仪操作工作。'
Assert-True ($workDefs -match '<workType>Research</workType>') '固定检测仪操作必须属于研究工作类型。'
Assert-True ($workDefs -match '<priorityInType>110</priorityInType>') '固定检测仪操作优先级必须高于原版普通研究的 100。'

Write-Host 'PASS: 智识 10 级研究员会优先自动操作固定 Trion 检测仪。'
