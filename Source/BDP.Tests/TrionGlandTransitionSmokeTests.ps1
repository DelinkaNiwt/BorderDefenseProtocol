$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message) if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$comp = Get-Content -Raw (Join-Path $root 'Source\BDP\Core\Trion\CompTrion.cs')
$gene = Get-Content -Raw (Join-Path $root 'Source\BDP\Core\Genes\Gene_TrionGland.cs')
$reasonPath = Join-Path $root 'Source\BDP\Core\Trion\TrionEligibilityChangeReason.cs'
Assert-True (Test-Path $reasonPath) '缺少统一 Trion 资格变化原因。'
Assert-True ($comp -match 'RuntimeGranted') '运行中获得腺体必须使用明确迁移原因。'
Assert-True ($comp -match 'cur = 0f') '运行中获得腺体必须从0开始。'
Assert-True ($comp -match 'NotifyBoundaries\(oldAvailable, oldCur\)') '容量归零必须发布既有见底事件。'
Assert-True ($gene -match 'HasCompletedInitialResourceSetup') '基因加入必须区分创建阶段和运行阶段。'
Assert-True ($gene -match 'PostRemove[\s\S]*Lost') '基因移除必须走资格丢失迁移。'
Assert-True ($gene -match 'TickInterval') '基因失活状态变化必须被同步。'
Assert-True ($comp -notmatch 'trionCapacityPotential = 0') '资格变化不得清除永久潜在容量。'
Assert-True ($comp -notmatch 'trionTalentAssessmentCompleted = false') '资格变化不得清除检测记录。'
Assert-True ($comp -notmatch 'innateTrionIntensity = 0') '资格变化不得清除永久先天释放力。'
Write-Host 'PASS: Trion 腺体创建、运行加入、失活与移除迁移闭环。'
