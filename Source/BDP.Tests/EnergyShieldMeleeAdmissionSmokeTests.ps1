$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$contentRoot = Join-Path $sourceRoot 'BDP.Content'
$propsPath = Join-Path $contentRoot 'Shield\HediffCompProperties_EnergyShield.cs'
$compPath = Join-Path $contentRoot 'Shield\HediffComp_EnergyShield.cs'
$policyPath = Join-Path $contentRoot 'Shield\EnergyShieldBlockPolicy.cs'

$propsText = Get-Content -LiteralPath $propsPath -Raw -Encoding UTF8
$compText = Get-Content -LiteralPath $compPath -Raw -Encoding UTF8
$policyText = Get-Content -LiteralPath $policyPath -Raw -Encoding UTF8

Assert-True (
    ($propsText -match 'bool\s+allowMeleeDamage\s*=\s*false') -and
    ($propsText -match 'CanAbsorb\(DamageInfo damageInfo\)')
) '护盾必须有默认关闭的近战准入开关，并按完整 DamageInfo（伤害信息）判定。'

Assert-True (
    ($policyText -match 'IsMeleeDamage\(DamageInfo damageInfo\)') -and
    ($policyText -match 'damageInfo\.Tool\s*!=\s*null') -and
    ($policyText -match 'damageInfo\.Weapon.*IsMeleeWeapon')
) '近战伤害识别必须复用原版 Tool（工具）和近战武器来源语义。'

Assert-True (
    ($compText -match 'Props\.CanAbsorb\(damageInfo\)') -and
    ($compText -notmatch 'Props\.CanAbsorb\(damageInfo\.Def\)')
) '运行时护盾必须把完整伤害信息交给准入策略。'

Write-Output 'EnergyShieldMeleeAdmissionSmokeTests PASS'
