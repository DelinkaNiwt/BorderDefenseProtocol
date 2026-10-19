$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$trigger = Get-Content -LiteralPath (Join-Path $root '1.6\Content\Defs\ThingDef\Equipment\Trigger.xml') -Raw -Encoding utf8
$test = Get-Content -LiteralPath (Join-Path $root '1.6\Development\Defs\ThingDef\BlackTriggerTest.xml') -Raw -Encoding utf8
$traitService = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Inheritance\BlackTriggerInheritedTraitService.cs') -Raw -Encoding utf8
$traitClass = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Inheritance\Trait_BlackTriggerInherited.cs') -Raw -Encoding utf8
$validator = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Validation\BlackTriggerFormationDefValidator.cs') -Raw -Encoding utf8
$matcher = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Matching\BlackTriggerFormationMatcher.cs') -Raw -Encoding utf8
$compatibility = Get-Content -LiteralPath (Join-Path $root 'Source\BDP.Content\Trigger\Black\Formation\Binding\BlackTriggerCompatibilityEvaluator.cs') -Raw -Encoding utf8

Assert-True ($trigger -notmatch '<skill>Medical</skill>' -and $test -notmatch '<skill>Medical</skill>') '黑触发器 Def 不得引用不存在的 Medical 技能 Def。'
Assert-True (($trigger + $test) -match '<skill>Medicine</skill>') '黑触发器医疗条件必须引用运行时存在的 Medicine 技能 Def。'
Assert-True ($traitClass -match ':\s*Trait' -and $traitService -match 'GainTrait') '继承必须使用来源人物原 TraitDef 与 degree 创建真实临时特性。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $root '1.6\Content\Defs\BlackTrigger\TraitInheritance.xml'))) '旧特性映射 Def 必须删除。'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $root '1.6\Content\Defs\HediffDef\BlackTriggerInheritance.xml'))) '旧继承 Hediff Def 必须删除。'
Assert-True ($validator -match 'anyCount\s*>\s*0' -and $validator -match 'anyOfMinimum') '形成校验器必须只在有 anyOf 时校验最低数量。'
Assert-True ($matcher -match 'any\.Count\s*==\s*0\s*\?\s*0' -and $matcher -match 'anyOfMinimum') '形成匹配器必须允许纯 allOf 条件组通过。'
Assert-True ($compatibility -match 'anyCount\s*==\s*0' -or $compatibility -match 'anyOfMinimum' -and $compatibility -match 'anyOf\?\.Count') '适格判断器必须允许纯 allOf 条件组通过。'
Write-Output 'BlackTriggerStartupErrorRegressionSmokeTests PASS'
exit 0
