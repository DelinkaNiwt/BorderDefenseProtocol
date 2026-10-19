$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$formation = Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation'
$required = @('Defs\BlackTriggerEmphasisDef.cs','Defs\BlackTriggerFormationRoute.cs','Defs\BlackTriggerFormationExtension.cs','Defs\BlackTriggerEligibilityCondition.cs','Defs\BlackTriggerEligibilityConditionGroup.cs','Defs\BlackTriggerFormationSettingsDef.cs','Model\BlackTriggerCandidatePlan.cs','Model\BlackTriggerMatchedRouteSnapshot.cs','Model\BlackTriggerTraitOptionSnapshot.cs','Matching\BlackTriggerFormationMatcher.cs','Matching\BlackTriggerEmphasisAssigner.cs','Validation\BlackTriggerFormationDefValidator.cs')
foreach ($path in $required) { Assert-True (Test-Path -LiteralPath (Join-Path $formation $path)) "缺少形成设施：$path" }
$extension = Get-Content -LiteralPath (Join-Path $formation 'Defs\BlackTriggerFormationExtension.cs') -Raw -Encoding utf8
$settings = Get-Content -LiteralPath (Join-Path $formation 'Defs\BlackTriggerFormationSettingsDef.cs') -Raw -Encoding utf8
$testDef = Get-Content -LiteralPath (Join-Path $root '1.6\Development\Defs\ThingDef\BlackTriggerTest.xml') -Raw -Encoding utf8
foreach ($field in @('formationRoutes','eligibilityGroups')) { Assert-True ($extension -match $field) "形成扩展缺少字段：$field" }
foreach ($field in @('pendingEventTicks','eventScanIntervalTicks','normalCooldownTicks','crisisBleedoutTicks','crisisLethalSeverityRatio')) { Assert-True ($settings -match $field) "设置缺少字段：$field" }
Assert-True ($settings -notmatch 'minimumMakerPotential') '形成系统不得再使用来源人物固定 Trion 门槛。'
Assert-True ($testDef -match 'formationRoutes' -and $testDef -match '<emphasis>') '测试 Def 必须声明路线与单一侧重。'
Assert-True ($testDef -notmatch 'formationGroups|finalWill|inclination') '测试 Def 不得保留旧形成模型。'
Write-Output 'BlackTriggerFormationDefinitionSmokeTests PASS'
exit 0
