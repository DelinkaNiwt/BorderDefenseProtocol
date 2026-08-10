$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message) if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$eligibilityPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\TrionTalentAssessmentEligibility.cs'
$servicePath = Join-Path $root 'Source\BDP.Content\Trion\Talent\TrionTalentAssessmentService.cs'
$resultPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\TrionTalentAssessmentResult.cs'
$statePath = Join-Path $root 'Source\BDP.Content\Trion\Talent\CompTrionTalentAssessment.cs'
Assert-True (Test-Path $eligibilityPath) '缺少统一检测资格。'
Assert-True (Test-Path $servicePath) '缺少统一检测服务。'
$eligibility = Get-Content -Raw $eligibilityPath
$service = Get-Content -Raw $servicePath
$result = Get-Content -Raw $resultPath
$commands = Get-Content -Raw (Join-Path $root 'Source\BDP\Core\Trion\ITrionCommands.cs')
$comp = Get-Content -Raw (Join-Path $root 'Source\BDP\Core\Trion\CompTrion.cs')
$state = Get-Content -Raw $statePath
Assert-True ($eligibility -match 'operatorPawn == subject') '操作员与受检者不能是同一人。'
Assert-True ($eligibility -match 'SkillDefOf\.Intellectual') '操作员资格必须读取智识技能。'
Assert-True ($eligibility -match '< 10') '智识10必须是硬门槛。'
Assert-True ($eligibility -match 'Humanlike') '受检者必须是人形。'
Assert-True ($eligibility -match 'HasActiveTrionGland') '已有有效腺体者不得检测。'
Assert-True ($eligibility -match 'CompTrionTalentAssessment') '已检测者判断必须读取 Content 侧检测状态。'
Assert-True ($service -match 'TryCommit') '必须提供统一提交入口。'
Assert-True ($service -match 'TryMarkCompleted') '提交必须通过 Content 侧状态组件原子标记。'
Assert-True ($service -match 'reader\.InnateTrionIntensity') '同一次检测提交必须读取先天 Trion 释放力。'
Assert-True ($commands -notmatch 'TryMarkTrionTalentAssessmentCompleted') 'Core 命令面不得提供检测业务标记请求。'
Assert-True ($comp -notmatch 'trionTalentAssessmentCompleted|TryMarkTrionTalentAssessmentCompleted') 'Core CompTrion 不得持有最终检测记录。'
Assert-True ($state -match 'TryMarkCompleted') 'Content 状态组件必须提供原子完成标记。'
Assert-True ($comp -match 'EnsureTrionCapacityPotentialInitialized\(\)[\s\S]*EnsureTrionIntensityInitialized\(\)') '提交检测记录前必须确保两项先天真值都已生成。'
Assert-True ($result -match 'CapacityPotentialBand') '成功结果必须携带容量潜质档位。'
Assert-True ($result -match 'int TrionIntensity') '成功结果必须携带先天释放力。'
Assert-True ($result -match 'Trion天赋检测完成') '完成消息必须使用正式检测名称。'
Assert-True ($result -match 'Trion容量潜质') '完成消息必须展示容量潜质。'
Assert-True ($result -match 'Trion释放力') '完成消息必须展示释放力。'
Write-Host 'PASS: Trion 天赋检测资格、双结果与原子提交入口成立。'
