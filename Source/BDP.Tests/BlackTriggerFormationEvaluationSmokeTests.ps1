$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$matcher = Get-Content -LiteralPath (Join-Path $formation 'Matching\BlackTriggerFormationMatcher.cs') -Raw -Encoding utf8
$route = Get-Content -LiteralPath (Join-Path $formation 'Defs\BlackTriggerFormationRoute.cs') -Raw -Encoding utf8
$assigner = Get-Content -LiteralPath (Join-Path $formation 'Matching\BlackTriggerEmphasisAssigner.cs') -Raw -Encoding utf8
$random = Get-Content -LiteralPath (Join-Path $formation 'Matching\BlackTriggerStableRandom.cs') -Raw -Encoding utf8
Assert-True ($matcher -match 'Evaluate\(BlackTriggerMakerSnapshot') '匹配器必须直接评估完整冻结快照。'
Assert-True ($matcher -match 'MatchedRoutes' -and $matcher -match 'ThingDef') '匹配器必须按物品聚合所有通过路线。'
Assert-True ($route -match 'allOf' -and $route -match 'anyOf' -and $route -match 'noneOf') '路线必须支持与、或、排斥。'
Assert-True ($assigner -match 'AssignedEmphasis') '侧重分配器必须给每个候选写入唯一侧重。'
Assert-True ($random -match 'Pick') '最终物品必须使用稳定随机。'
Assert-True (($matcher + $assigner) -notmatch '\bscore\b|\bthreshold\b|\bpriority\b') '匹配与分配不得使用分数、固定门槛或路线优先级。'
Write-Output 'BlackTriggerFormationEvaluationSmokeTests PASS'
exit 0
