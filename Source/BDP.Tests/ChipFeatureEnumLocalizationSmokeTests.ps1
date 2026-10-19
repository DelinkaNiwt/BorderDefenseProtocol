$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot "TestSupport\ChipManufacturingTestSupport.ps1")

$modRoot = Get-BdpModRoot
$workerRoot = Join-Path $modRoot 'Source\BDP.Content\Information\Features\Workers'
$rangedWorker = Get-Utf8Text (Join-Path $workerRoot 'RangedAttackFeatureSourceWorker.cs')
$sourceSupport = Get-Utf8Text (Join-Path $workerRoot 'ChipFeatureSourceWorkerSupport.cs')
$shieldWorker = Get-Utf8Text (Join-Path $workerRoot 'ShieldFeatureSourceWorker.cs')
$languagePath = Join-Path $modRoot 'Languages\ChineseSimplified (简体中文)\Keyed\ChipInformation.xml'
$languageText = Get-Utf8Text $languagePath

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
}

$requiredKeys = @(
    'BDP_TargetType_Self',
    'BDP_TargetType_Pawn',
    'BDP_TargetType_Building',
    'BDP_TargetType_Location',
    'BDP_TargetType_Thing',
    'BDP_BlockAttackType_Melee',
    'BDP_BlockAttackType_Ranged',
    'BDP_BlockAttackType_Explosive',
    'BDP_RangedShotRhythm_Sequential',
    'BDP_RangedShotRhythm_Simultaneous',
    'BDP_ProjectileFlightFeature_Homing',
    'BDP_ProjectileFlightFeature_Guided',
    'BDP_ProjectileFlightFeature_Turning',
    'BDP_ProjectileFlightFeature_Arcing'
)

foreach ($key in $requiredKeys) {
    Assert-True ($languageText -match [regex]::Escape("<$key>")) "缺少已定义功能枚举值的简体中文键：$key"
}

Assert-True ($sourceSupport -match 'BDP_TargetType_Self' -and $sourceSupport -match 'BDP_TargetType_Pawn') `
    '能力、机动和远程攻击目标类别必须输出对应的本地化稳定键。'
Assert-True ($shieldWorker -match 'BDP_BlockAttackType_Ranged' -and $shieldWorker -match 'BDP_BlockAttackType_Explosive' -and $shieldWorker -match 'BDP_BlockAttackType_Melee') `
    '护盾抵挡类型必须输出对应的本地化稳定键。'
Assert-True ($rangedWorker -match 'BDP_RangedShotRhythm_Simultaneous' -and $rangedWorker -match 'BDP_RangedShotRhythm_Sequential') `
    '射击节奏必须输出对应的本地化稳定键。'
Assert-True ($rangedWorker -match 'BDP_ProjectileFlightFeature_Homing' -and $rangedWorker -match 'BDP_ProjectileFlightFeature_Guided' -and $rangedWorker -match 'BDP_ProjectileFlightFeature_Turning' -and $rangedWorker -match 'BDP_ProjectileFlightFeature_Arcing') `
    '投射物飞行特征必须输出对应的本地化稳定键。'
Assert-True ($sourceSupport -notmatch 'values\.Add\("(Self|Pawn|Building|Location|Thing)"\)' -and $shieldWorker -notmatch '"(Ranged|Explosive|Melee)"' -and $rangedWorker -notmatch 'FromEnum\(simultaneous \? "Simultaneous" : "Sequential"\)') `
    '标准来源不得输出会直接显示为原始英文的短枚举键。'
Assert-True ($sourceSupport -match 'FromTranslationKey\(values\[index\]\)' -and $rangedWorker -match 'FromTranslationKey\(') `
    '已定义的语义文本键必须显式标记为 Keyed（文本键）来源。'

Write-Output 'ChipFeatureEnumLocalization PASS'
