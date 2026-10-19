$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$emphasisDefPath = Join-Path $formation 'Defs\BlackTriggerEmphasisDef.cs'
$routePath = Join-Path $formation 'Defs\BlackTriggerFormationRoute.cs'
$planPath = Join-Path $formation 'Model\BlackTriggerCandidatePlan.cs'
$assignerPath = Join-Path $formation 'Matching\BlackTriggerEmphasisAssigner.cs'
foreach ($path in @($emphasisDefPath, $routePath, $planPath, $assignerPath)) { Assert-True (Test-Path -LiteralPath $path) "缺少侧重匹配设施：$path" }
$extension = Get-Content -LiteralPath (Join-Path $formation 'Defs\BlackTriggerFormationExtension.cs') -Raw -Encoding utf8
$route = Get-Content -LiteralPath $routePath -Raw -Encoding utf8
$plan = Get-Content -LiteralPath $planPath -Raw -Encoding utf8
$assigner = Get-Content -LiteralPath $assignerPath -Raw -Encoding utf8
Assert-True ($extension -match 'formationRoutes') '黑触发器定义必须声明形成路线。'
Assert-True ($route -match 'allOf' -and $route -match 'anyOf' -and $route -match 'noneOf' -and $route -match 'BlackTriggerEmphasisDef emphasis') '路线必须声明与、或、排斥和单一战斗侧重。'
Assert-True ($plan -match 'MatchedRoutes' -and $plan -match 'AssignedEmphasis') '候选计划必须保留通过路线和最终侧重。'
Assert-True ($assigner -match 'maximum|Maximize|最大覆盖') '侧重分配必须明确采用最大覆盖。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $formation 'Matching\BlackTriggerFactPair.cs'))) '玩家事实双选模型必须删除。'
Write-Output 'BlackTriggerEmphasisAssignmentSmokeTests PASS'
exit 0
