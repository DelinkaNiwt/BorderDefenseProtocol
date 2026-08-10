$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message) if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$operatorPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Jobs\JobDriver_TrionTalentAssessment.cs'
$subjectPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Jobs\JobDriver_WaitForTrionTalentAssessment.cs'
$utilityPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\TrionTalentAssessmentFloatMenuUtility.cs'
$defsPath = Join-Path $root '1.6\Content\Defs\Jobs\Trion\JobDefs_TrionTalentAssessment.xml'
foreach ($path in @($operatorPath,$subjectPath,$utilityPath,$defsPath)) { Assert-True (Test-Path $path) "缺少双人检测工作文件：$path" }
$operator = Get-Content -Raw $operatorPath
$subject = Get-Content -Raw $subjectPath
$utility = Get-Content -Raw $utilityPath
$defs = Get-Content -Raw $defsPath
Assert-True ($operator -match 'TargetIndex\.A') '操作员工作必须保存设备目标。'
Assert-True ($operator -match 'TargetIndex\.B') '操作员工作必须保存受检者目标。'
Assert-True ($operator -match 'BDP_WaitForTrionTalentAssessment') '必须给受检者下发独立等待工作。'
Assert-True (([regex]::Matches($operator, 'CanAssess').Count) -ge 2) '开始和完成前必须重新检查资格。'
Assert-True ($operator -match 'TryCommit') '完成必须调用统一提交服务。'
Assert-True (
    ($operator -match 'result\.Succeeded') -and
    ($operator -match 'Destroy\(DestroyMode\.Vanish\)') -and
    ($operator.IndexOf('result.Succeeded') -lt $operator.IndexOf('Destroy(DestroyMode.Vanish)'))
) '便携设备只能在成功提交后消耗。'
Assert-True ($subject -match 'JobDriver_WaitForTrionTalentAssessment') '必须存在受检者等待驱动。'
Assert-True ($utility -match 'BuildOptions\(\s*Pawn operatorPawn,\s*Thing_TrionPortableDetector device\)') '旧右键入口必须只接受便携检测仪。'
Assert-True ($utility -notmatch 'Building_TrionDetector') '便携检测入口不得再包含固定检测仪分支。'
Assert-True ($operator -notmatch 'UsesPortableDevice|Building_TrionDetector') '旧操作员工作必须只服务便携检测仪。'
Assert-True ($subject -notmatch 'Building_TrionDetector|GotoThing\(DeviceIndex') '旧受检者等待工作必须只服务便携检测仪。'
Assert-True ($defs -match 'BDP_TrionTalentAssessment' -and $defs -match 'BDP_WaitForTrionTalentAssessment') '必须声明两个工作定义。'
Assert-True ($defs -notmatch '<checkOverrideOnExpire>') 'RimWorld 1.6 JobDef 不存在 checkOverrideOnExpire 字段。'
Write-Host 'PASS: Trion 双人检测工作时序边界成立。'
