$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }

$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$route = Get-Content -LiteralPath (Join-Path $formation 'Defs\BlackTriggerFormationRoute.cs') -Raw -Encoding utf8
$evaluation = Get-Content -LiteralPath (Join-Path $formation 'Matching\BlackTriggerRouteEvaluation.cs') -Raw -Encoding utf8
$matched = Get-Content -LiteralPath (Join-Path $formation 'Model\BlackTriggerMatchedRouteSnapshot.cs') -Raw -Encoding utf8
$matcher = Get-Content -LiteralPath (Join-Path $formation 'Matching\BlackTriggerFormationMatcher.cs') -Raw -Encoding utf8
$validator = Get-Content -LiteralPath (Join-Path $formation 'Validation\BlackTriggerFormationDefValidator.cs') -Raw -Encoding utf8
$testDef = Get-Content -LiteralPath (Join-Path $root '1.6\Development\Defs\ThingDef\BlackTriggerTest.xml') -Raw -Encoding utf8

Assert-True ($route -match 'public BlackTriggerEmphasisDef emphasis;') '形成路线必须声明单个战斗侧重。'
Assert-True ($route -notmatch 'List<BlackTriggerEmphasisDef>\s+emphases') '形成路线不能声明多个战斗侧重列表。'
Assert-True ($evaluation -match 'public BlackTriggerEmphasisDef Emphasis;') '路线求值必须保留单个战斗侧重。'
Assert-True ($matched -match 'public BlackTriggerEmphasisDef Emphasis;') '冻结路线快照必须保留单个战斗侧重。'
Assert-True ($matched -match 'Scribe_Defs\.Look\(ref Emphasis, "emphasis"\)') '冻结路线快照必须按单个 Def 保存侧重。'
Assert-True ($matcher -match 'Emphasis\s*=\s*route\?\.emphasis') '匹配器必须读取路线的单个侧重。'
Assert-True ($validator -match 'route\.emphasis\s*==\s*null') '校验器必须校验路线的单个侧重。'
Assert-True ($testDef -notmatch '<emphases>') '测试路线不能再使用多侧重字段。'
Assert-True ($testDef -notmatch '<routeId>') '测试路线不应再写语义化路线名称。'

Write-Output 'BlackTriggerSingleRouteEmphasisSmokeTests PASS'
exit 0
