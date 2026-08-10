$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message) if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$defsPath = Join-Path $root '1.6\Content\Defs\Trion\Talent\TrionCapacityPotentialBandDefs.xml'
$resolverPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\Capacity\TrionCapacityPotentialBandResolver.cs'
$assessmentPath = Join-Path $root 'Source\BDP.Content\Trion\Talent\CompTrionTalentAssessment.cs'
Assert-True (Test-Path $defsPath) '缺少检测档位定义。'
Assert-True (Test-Path $resolverPath) '缺少检测档位解析器。'
$bandDefs = Get-Content -Raw $defsPath
$bandXml = [xml]$bandDefs
$bands = @($bandXml.SelectNodes('//BDP.Content.Trion.Talent.Capacity.TrionCapacityPotentialBandDef'))
$assessmentText = Get-Content -Raw $assessmentPath
Assert-True ($bands.Count -eq 9) '必须恰好定义九个检测档位。'
Assert-True ($bandDefs -match '<label>微弱</label>') '必须提供微弱档。'
Assert-True ($bandDefs -match '<label>破格</label>') '必须提供破格档。'
Assert-True ($bandDefs -match '<label>略懂\(\?\)</label>') '必须提供5000专属的略懂(?)档。'
Assert-True ($bandDefs -match '<minimumCapacity>100</minimumCapacity>') '档位必须从100开始。'
Assert-True ($bandDefs -match '<maximumCapacity>5000</maximumCapacity>') '档位必须覆盖5000。'

$exceptional = $bands | Where-Object { $_.label -eq '破格' }
$slightlyUnderstands = $bands | Where-Object { $_.label -eq '略懂(?)' }
Assert-True (
    ([int]$exceptional.minimumCapacity -eq 4000) -and
    ([int]$exceptional.maximumCapacity -eq 4900)
) '破格档必须只覆盖4000～4900。'
Assert-True (
    ([int]$slightlyUnderstands.minimumCapacity -eq 5000) -and
    ([int]$slightlyUnderstands.maximumCapacity -eq 5000)
) '略懂(?)档必须只覆盖5000。'

Assert-True ($assessmentText -match 'SpecialDisplayStats') '检测结果必须走原版信息页入口。'
Assert-True ($assessmentText -match 'baseStats != null') 'ThingComp 基类可能返回 null，信息页投影必须显式防护。'
Assert-True ($assessmentText -match 'IsCompleted') '信息页必须受 Content 检测状态控制。'
Assert-True (($assessmentText -match 'HasActiveTrionGland') -and ($assessmentText -match 'TrionIntensityUtility\.GetEffective')) '植入腺体后必须继续显示容量潜质，并使用有效释放力。'
Assert-True ($assessmentText -match 'Trion容量潜质') '角色信息页必须使用正式容量潜质名称。'
Write-Host 'PASS: Trion 容量潜质档位和持续显示约束成立。'
