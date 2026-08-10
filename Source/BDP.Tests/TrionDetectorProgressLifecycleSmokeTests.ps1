$ErrorActionPreference = 'Stop'

function Assert-True
{
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$buildingPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Building_TrionDetector.cs'
$driverPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Jobs\JobDriver_OperateTrionDetector.cs'

$building = Get-Content -Raw $buildingPath
$driver = Get-Content -Raw $driverPath

Assert-True ($building -match 'WorkRequired\s*=\s*2500f') '研究速度 100% 时的基准工作量必须为 2,500 tick。'
Assert-True ($building -match 'Scribe_Values\.Look\(ref\s+completedWork') '固定检测仪必须存档累计工作量。'
Assert-True ($building -match 'EffecterDefOf\.ProgressBar') '固定检测仪必须显示原版进度条。'
Assert-True ($driver -notmatch '\.WithProgressBar') '进度条必须只由建筑持有，避免操作时叠出两条。'
Assert-True ($building -match 'TryCommit\(operatorPawn,\s*occupant\)') '达到工作量后必须调用统一检测提交服务。'
Assert-True ($building -match 'Messages\.Message\(result\.Message,\s*occupant') '检测成功后必须向玩家显示受检者结果。'
Assert-True ($building -match 'EjectContents\(\)') '检测完成后必须走统一弹出路径。'
Assert-True ($building -match 'completedWork\s*=\s*0f') '取消或完成后必须清空本次累计工作量。'
Assert-True ($building -match 'Mathf\.Clamp\(completedWork,\s*0f,\s*WorkRequired\)') '读档后必须夹紧异常工作量。'
Assert-True ($driver -match 'if\s*\(\s*detector\.AddWork\(') '操作驱动必须根据建筑返回值识别结算完成。'
Assert-True ($building -match '基准耗时：研究速度100%时1游戏小时') '检查信息必须说明 1 小时是研究速度 100% 时的基准。'

Write-Host 'PASS: 固定 Trion 检测仪具备持久进度、结果提交和自动弹出闭环。'
