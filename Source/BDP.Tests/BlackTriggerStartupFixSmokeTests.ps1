$ErrorActionPreference = 'Stop'
function Assert-True { param([bool]$Condition, [string]$Message); if (-not $Condition) { throw $Message } }
$root = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
$defs = Join-Path $root '1.6\Content\Defs\BlackTrigger'
$settings = Get-Content -LiteralPath (Join-Path $defs 'FormationSettings.xml') -Raw -Encoding utf8
$emphases = Get-Content -LiteralPath (Join-Path $defs 'Emphases.xml') -Raw -Encoding utf8
$record = Get-Content -LiteralPath (Join-Path $root '1.6\Content\Defs\RecordDef\BlackTriggerFormation.xml') -Raw -Encoding utf8
$testDef = Get-Content -LiteralPath (Join-Path $root '1.6\Development\Defs\ThingDef\BlackTriggerTest.xml') -Raw -Encoding utf8
Assert-True ($settings -match 'BlackTriggerFormationSettingsDef' -and $settings -match 'pendingEventTicks') '形成设置 Def 未迁移到新模型。'
Assert-True ($emphases -match 'BlackTriggerEmphasisDef' -and $emphases -match 'BDP_BlackTriggerEmphasis_Support') '战斗侧重 Def 未加载。'
Assert-True ($record -match 'BDP_BlackTriggerLifeImprintExhausted') '生命印记耗尽记录缺失。'
Assert-True ($testDef -match 'BaseWeapon' -and $testDef -match 'BlackTriggerFormationExtension') 'Development 临时测试黑触发器缺失。'
foreach ($old in @('Functions.xml','LifeTraces.xml')) { Assert-True (-not (Test-Path -LiteralPath (Join-Path $defs $old))) "旧 Def 不应继续加载：$old" }
Assert-True (-not (Test-Path -LiteralPath (Join-Path $root '1.6\Content\Defs\ThingDef\Items\BlackTriggerFormation.xml'))) '未定形遗物 Def 必须删除。'
Write-Output 'BlackTriggerStartupFixSmokeTests PASS'
exit 0
