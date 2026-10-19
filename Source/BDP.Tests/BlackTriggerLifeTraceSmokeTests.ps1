$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$snapshot = Get-Content -LiteralPath (Join-Path $formation 'Model\BlackTriggerMakerSnapshot.cs') -Raw -Encoding utf8
$factory = Get-Content -LiteralPath (Join-Path $formation 'Snapshot\BlackTriggerMakerSnapshotFactory.cs') -Raw -Encoding utf8
$catalog = Get-Content -LiteralPath (Join-Path $formation 'Snapshot\BlackTriggerRecordCatalog.cs') -Raw -Encoding utf8
foreach ($field in @('MakerLoadId','MakerWasColonistPlayerControlled','Facts','HeirCandidates','TraitOptions','StableSeed')) { Assert-True ($snapshot -match "\b$field\b") "快照缺少字段：$field" }
Assert-True ($factory -match 'FillSkills' -and $factory -match 'Take\(3\)') '快照必须保留前三项有效技能。'
Assert-True ($factory -match 'FillRecords' -and $factory -match 'rank') '快照必须计算生涯记录排名。'
Assert-True ($factory -match 'DirectRelations' -and $factory -match 'opinion >= 100f') '传承人必须覆盖家庭关系和满好感关系。'
Assert-True ($catalog -match 'AllowedNames' -and $catalog -match 'ResolveAvailable') '记录目录必须提供白名单与运行时解析。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $formation 'Evaluation\BlackTriggerLifeTraceEvaluator.cs'))) '旧生命轨迹评估器必须删除。'
Write-Output 'BlackTriggerLifeTraceSmokeTests PASS'
exit 0
