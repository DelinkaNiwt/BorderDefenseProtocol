$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message) if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot
$compText = Get-Content -Raw (Join-Path $root 'BDP\Core\Trion\CompTrion.cs')
$readerText = Get-Content -Raw (Join-Path $root 'BDP\Core\Trion\ITrionReader.cs')
$serviceText = Get-Content -Raw (Join-Path $root 'BDP\Core\Trion\TrionService.cs')
Assert-True ($compText -match 'private int trionCapacityPotential;') 'CompTrion 必须保存潜在容量。'
Assert-True ($compText -match 'private bool trionCapacityPotentialInitialized;') 'CompTrion 必须保存初始化状态。'
Assert-True ($compText -match 'Scribe_Values\.Look\(ref trionCapacityPotential') '潜在容量必须存档。'
Assert-True ($compText -match 'EnsureTrionCapacityPotentialInitialized') '必须有单一潜在容量初始化入口。'
Assert-True ($readerText -match 'TrionCapacityPotential') '只读面必须公开潜在容量。'
Assert-True ($compText -notmatch 'TrionTalentAssessmentCompleted|TryMarkTrionTalentAssessmentCompleted') 'Core CompTrion 不得保存检测业务状态。'
Assert-True ($readerText -notmatch 'TrionTalentAssessmentCompleted') 'Core 只读面不得暴露检测业务状态。'
Assert-True ($serviceText -notmatch 'TrionTalentAssessmentCompleted|TryMarkTrionTalentAssessmentCompleted') 'Core TrionService 不得转发检测业务状态。'
Assert-True ($compText -notmatch 'GetComp<CompTrionPotential>') '不得增加平行潜质 Comp。'
Write-Host 'PASS: Trion 潜在容量直接持久化于 CompTrion。'
