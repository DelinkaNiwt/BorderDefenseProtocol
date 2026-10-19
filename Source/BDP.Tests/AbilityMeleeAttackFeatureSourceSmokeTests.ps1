$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$informationRoot = Join-Path $contentRoot 'Information\Features'
$contractPath = Join-Path $informationRoot 'AbilityFeatureInformationContracts.cs'
$sourceConfigPath = Join-Path $informationRoot 'Sources\AbilityMeleeAttackFeatureSourceConfig.cs'
$sourceWorkerPath = Join-Path $informationRoot 'Workers\AbilityMeleeAttackFeatureSourceWorker.cs'
$bounceSlashPath = Join-Path $contentRoot 'BounceSlash\CompAbilityEffect_BounceSlash.cs'
$senkuPath = Join-Path $contentRoot 'SenkuKogetsu\CompAbilityEffect_SenkuKogetsuWave.cs'
$bounceComboPath = Join-Path $repoRoot '1.6\Content\Defs\ComboDef\BounceSlash.xml'
$senkuComboPath = Join-Path $repoRoot '1.6\Content\Defs\ComboDef\SenkuKogetsu.xml'

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

function Read-Source([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return '' }
    return Get-Content -LiteralPath $path -Raw -Encoding UTF8
}

$contract = Read-Source $contractPath
$sourceConfig = Read-Source $sourceConfigPath
$sourceWorker = Read-Source $sourceWorkerPath
$bounceSlash = Read-Source $bounceSlashPath
$senku = Read-Source $senkuPath
$bounceCombo = Read-Source $bounceComboPath
$senkuCombo = Read-Source $senkuComboPath

Assert-True ($contract -match 'interface IAbilityMeleeAttackInformation') 'Ability 自定义实现必须通过明确的只读信息契约暴露近战结果。'
Assert-True ($bounceSlash -match 'IAbilityMeleeAttackInformation' -and $senku -match 'IAbilityMeleeAttackInformation') '弹射砍击与旋空弧月必须从各自 Ability 组件权威配置提供近战攻击信息。'
Assert-True ($sourceConfig -match 'ExpressionEntryFeatureSourceConfig' -and $sourceConfig -match 'EffectIndex') 'Ability 近战来源必须按正式表达条目和效果顺序定位。'
Assert-True ($sourceWorker -match 'IAbilityMeleeAttackInformation' -and $sourceWorker -match 'DamageAmount' -and $sourceWorker -match 'DamageDef' -and $sourceWorker -match 'ArmorPenetration') 'Ability 近战来源 Worker 必须投影权威伤害、类型与护甲穿透。'
Assert-True ($bounceCombo -match 'AbilityMeleeAttackFeatureSourceConfig') '弹射砍击的近战子语义必须从 Ability 自定义攻击组件取值。'
Assert-True ($senkuCombo -match 'AbilityMeleeAttackFeatureSourceConfig') '旋空弧月的近战语义必须从 Ability 自定义攻击组件取值。'

Write-Output 'AbilityMeleeAttackFeatureSource PASS'
