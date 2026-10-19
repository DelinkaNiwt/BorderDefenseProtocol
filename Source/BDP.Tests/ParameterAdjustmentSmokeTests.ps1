# BDP 本次战斗参数调整冒烟测试。

$ErrorActionPreference = "Stop"

function Assert-True
{
    param([bool]$Condition, [string]$Message)

    if (-not $Condition)
    {
        throw $Message
    }
}

function Assert-Equal
{
    param([object]$Actual, [object]$Expected, [string]$Message)

    if ([string]$Actual -ne [string]$Expected)
    {
        throw "$Message 实际值：$Actual；期望值：$Expected。"
    }
}

function Find-Def
{
    param([xml]$Document, [string]$DefName)

    return @($Document.Defs.ChildNodes | Where-Object { $_.defName -eq $DefName })[0]
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$projectilesPath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Projectiles\Projectiles.xml'
$fujinProjectilePath = Join-Path $repoRoot '1.6\Content\Defs\ThingDef\Projectiles\FujinRangedStrike.xml'
$armamentPath = Join-Path $repoRoot '1.6\Content\Defs\ChipArmamentFormDef\Presets.xml'
$senkuAbilityPath = Join-Path $repoRoot '1.6\Content\Defs\AbilityDef\SenkuKogetsu.xml'
$keyedLanguagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml'

[xml]$projectiles = Get-Content -LiteralPath $projectilesPath -Raw -Encoding utf8
[xml]$fujinProjectiles = Get-Content -LiteralPath $fujinProjectilePath -Raw -Encoding utf8
[xml]$armaments = Get-Content -LiteralPath $armamentPath -Raw -Encoding utf8
[xml]$senkuAbility = Get-Content -LiteralPath $senkuAbilityPath -Raw -Encoding utf8
$keyedLanguage = Get-Content -LiteralPath $keyedLanguagePath -Raw -Encoding utf8

$viper = Find-Def $projectiles 'BDP_Projectile_Variable'
$ibis = Find-Def $projectiles 'BDP_Projectile_SniperStandard'
$revolver = Find-Def $armaments 'BDP_ArmamentForm_Revolver'
$submachineGun = Find-Def $armaments 'BDP_ArmamentForm_SubmachineGun'
$senku = Find-Def $senkuAbility 'BDP_Ability_SenkuKogetsu'
$senkuWave = @($senku.comps.li | Where-Object { $_.waveDef -eq 'BDP_Projectile_SenkuKogetsuWave' })[0]
$fujin = Find-Def $fujinProjectiles 'BDP_Projectile_FujinRangedStrike'

Assert-True ($null -ne $viper) '毒蛇投射物 Def 必须存在。'
Assert-True ($null -ne $ibis) '朱鹮投射物 Def 必须存在。'
Assert-True ($null -ne $revolver) '左轮手枪武装构型 Def 必须存在。'
Assert-True ($null -ne $submachineGun) '冲锋枪武装构型 Def 必须存在。'
Assert-True ($null -ne $senku) '旋空弧月 Ability Def 必须存在。'
Assert-True ($null -ne $senkuWave) '旋空弧月波体组件必须存在。'
Assert-True ($null -ne $fujin) '风刃投射物 Def 必须存在。'

Assert-Equal $viper.projectile.speed '120' '毒蛇子弹飞行速度必须为 120。'
Assert-Equal $ibis.projectile.speed '150' '朱鹮子弹飞行速度必须为 150。'

Assert-Equal $revolver.overrides.range '20' '左轮手枪射程必须为 20。'
Assert-Equal $revolver.overrides.defaultCooldownTime '2.0' '左轮手枪远程冷却时间必须为 2 秒。'
Assert-Equal $revolver.projectileOverrides.damageMultiplier '4' '左轮手枪伤害倍率必须为 4。'

Assert-Equal $senku.verbProperties.range '25' '旋空弧月最大瞄准范围必须为 25。'
Assert-Equal $senkuWave.maxRange '25' '旋空弧月波体最大有效范围必须为 25。'

Assert-Equal $submachineGun.overrides.burstShotCount '4' '冲锋枪子弹数量必须为 4。'
Assert-Equal $submachineGun.description '中近程高速型武装，四发连射，近距离命中可靠，远距离衰减明显。' '冲锋枪 Def 说明必须反映四发连射。'
Assert-True ($keyedLanguage -match '<BDP_ArmamentForm_SubmachineGun_Description>中近程高速型武装，四发连射，近距离命中可靠，远距离衰减明显。</BDP_ArmamentForm_SubmachineGun_Description>') '冲锋枪 Keyed 中文说明必须反映四发连射。'

Assert-Equal $fujin.projectile.damageAmountBase '40' '风刃·远程斩击投射物伤害必须保持为 40。'
Assert-Equal $fujin.projectile.armorPenetrationBase '0.6' '风刃·远程斩击投射物护甲穿透必须保持为 60%。'
Assert-Equal $fujin.projectile.speed '180' '风刃·远程斩击投射物飞行速度必须保持为 180。'

Write-Output 'ParameterAdjustmentSmokeTests PASS'
