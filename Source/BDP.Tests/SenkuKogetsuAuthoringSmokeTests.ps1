$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$chipDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Things\Items\Chips\Senku\ThingDefs_Chips_Senku.xml'
$abilityDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Abilities\SenkuKogetsu\AbilityDefs_SenkuKogetsu.xml'
$comboDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Pawn\Combos\SenkuKogetsu\ComboDefs_SenkuKogetsu.xml'
$waveThingDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Things\Effects\SenkuKogetsu\ThingDefs_SenkuKogetsuWave.xml'
$soundDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Sounds\SenkuKogetsu\SoundDefs_SenkuKogetsu.xml'

$waveTexturePath = Join-Path $repoRoot '1.6\Content\Textures\Things\Trigger\SenkuKogetsu\senku_kogetsu_wave.png'
$kogetsuHandleTexturePath = Join-Path $repoRoot '1.6\Content\Textures\Things\Trigger\Chip\Kogetsu\kogetsu_handle.png'
$kogetsuBladeTexturePath = Join-Path $repoRoot '1.6\Content\Textures\Things\Trigger\Chip\Kogetsu\kogetsu_blade.png'
$bladeGunSoundPath = Join-Path $repoRoot '1.6\Content\Sounds\Trigger\SenkuKogetsu\senku_kogetsu_cast.wav'

Assert-True (Test-Path -LiteralPath $chipDefsPath) '主模组必须定义正式弧月与旋空芯片。'
Assert-True (Test-Path -LiteralPath $abilityDefsPath) '主模组必须定义正式旋空弧月能力。'
Assert-True (Test-Path -LiteralPath $comboDefsPath) '主模组必须定义正式旋空弧月组合。'
Assert-True (Test-Path -LiteralPath $waveThingDefsPath) '主模组必须定义正式旋空弧月波体。'
Assert-True (Test-Path -LiteralPath $soundDefsPath) '主模组必须定义正式旋空弧月音效。'

Assert-True (Test-Path -LiteralPath $waveTexturePath) '主模组必须携带正式旋空弧月波体贴图。'
Assert-True (Test-Path -LiteralPath $kogetsuHandleTexturePath) '主模组必须携带正式弧月手柄贴图。'
Assert-True (Test-Path -LiteralPath $kogetsuBladeTexturePath) '主模组必须携带正式弧月刀刃贴图。'
Assert-True (Test-Path -LiteralPath $bladeGunSoundPath) '主模组必须携带正式旋空弧月音效。'

$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8
$abilityDefsText = Get-Content -LiteralPath $abilityDefsPath -Raw -Encoding utf8
$comboDefsText = Get-Content -LiteralPath $comboDefsPath -Raw -Encoding utf8
$waveThingDefsText = Get-Content -LiteralPath $waveThingDefsPath -Raw -Encoding utf8
$soundDefsText = Get-Content -LiteralPath $soundDefsPath -Raw -Encoding utf8

$kogetsuMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<ThingDef.*?<defName>BDP_Chip_Kogetsu</defName>(.*?)</ThingDef>')
$senkuMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<ThingDef.*?<defName>BDP_Chip_Senku</defName>(.*?)</ThingDef>')

Assert-True $kogetsuMatch.Success 'BDP_Chip_Kogetsu must exist as the main-side chip.'
Assert-True $senkuMatch.Success 'BDP_Chip_Senku must exist as the support-side chip.'

Assert-True (
    ($kogetsuMatch.Groups[1].Value -match '<Category>BDP_ChipCategory_Weapon</Category>') -and
    ($kogetsuMatch.Groups[1].Value -match '<SlotRegion>MainSub</SlotRegion>') -and
    ($kogetsuMatch.Groups[1].Value -match '<UseCost>5</UseCost>')
) 'BDP_Chip_Kogetsu must stay a hands-only chip with a lower UseCost than Senku.'

Assert-True (
    ($senkuMatch.Groups[1].Value -match '<Category>BDP_ChipCategory_Ability</Category>') -and
    ($senkuMatch.Groups[1].Value -match '<SlotRegion>MainSub</SlotRegion>') -and
    ($senkuMatch.Groups[1].Value -match '<UseCost>50</UseCost>') -and
    ($senkuMatch.Groups[1].Value -match '<Kind>Passive</Kind>') -and
    ($senkuMatch.Groups[1].Value -match '<PassiveKey>bdp\.senku\.passive</PassiveKey>') -and
    (-not ($senkuMatch.Groups[1].Value -match '<AbilityDefName>'))
) 'BDP_Chip_Senku must be a hands-only passive source chip whose UseCost drives combo casting.'

$abilityMatch = [regex]::Match(
    $abilityDefsText,
    '(?s)<AbilityDef>.*?<defName>BDP_Ability_SenkuKogetsu</defName>(.*?)</AbilityDef>')

Assert-True $abilityMatch.Success 'BDP_Ability_SenkuKogetsu must exist as the formal ability.'
Assert-True (
    ($abilityMatch.Groups[1].Value -match '<verbClass>BDP\.Core\.Abilities\.BdpVerb_CastAbility</verbClass>') -and
    ($abilityMatch.Groups[1].Value -match 'Class\s*=\s*\"BDP\.Core\.Abilities\.CompProperties_AbilityEffect_BdpTrionCost\"') -and
    ($abilityMatch.Groups[1].Value -match '<range>40</range>') -and
    ($abilityMatch.Groups[1].Value -match '<requireLineOfSight>false</requireLineOfSight>')
) 'BDP_Ability_SenkuKogetsu must bind to the formal Bdp ability cast/cost path and preserve the confirmed long-range no-LOS behavior.'

$comboMatch = [regex]::Match(
    $comboDefsText,
    '(?s)<BDP\.Core\.Combos\.ComboDef>.*?<defName>BDP_Combo_SenkuKogetsu</defName>(.*?)</BDP\.Core\.Combos\.ComboDef>')

Assert-True $comboMatch.Success 'BDP_Combo_SenkuKogetsu must exist as the formal combo.'
Assert-True (
    ($comboMatch.Groups[1].Value -match '<chipA>BDP_Chip_Kogetsu</chipA>') -and
    ($comboMatch.Groups[1].Value -match '<chipB>BDP_Chip_Senku</chipB>') -and
    ($comboMatch.Groups[1].Value -match '<Kind>Ability</Kind>') -and
    ($comboMatch.Groups[1].Value -match '<AbilityDefName>BDP_Ability_SenkuKogetsu</AbilityDefName>') -and
    ($comboMatch.Groups[1].Value -match '<UseCostResolve>FollowChipSub</UseCostResolve>') -and
    ($comboMatch.Groups[1].Value -match '<MinimumRequiredResolve>FollowChipSub</MinimumRequiredResolve>')
) 'BDP_Combo_SenkuKogetsu must emit an Ability result whose cast cost follows chipB Senku.'

$waveThingMatch = [regex]::Match(
    $waveThingDefsText,
    '(?s)<ThingDef>.*?<defName>BDP_Projectile_SenkuKogetsuWave</defName>(.*?)</ThingDef>')

Assert-True $waveThingMatch.Success 'BDP_Projectile_SenkuKogetsuWave must exist as the formal wave entity.'
Assert-True (
    ($waveThingMatch.Groups[1].Value -match '<texPath>Things/Trigger/SenkuKogetsu/senku_kogetsu_wave</texPath>') -and
    ($waveThingMatch.Groups[1].Value -match '<category>Projectile</category>')
) 'BDP_Projectile_SenkuKogetsuWave must point at the formal wave texture and keep projectile-like presentation.'

$soundDefMatch = [regex]::Match(
    $soundDefsText,
    '(?s)<SoundDef>.*?<defName>BDP_Sound_SenkuKogetsuCast</defName>(.*?)</SoundDef>')

Assert-True $soundDefMatch.Success 'BDP_Sound_SenkuKogetsuCast must exist as the formal cast sound.'
Assert-True (
    ($soundDefMatch.Groups[1].Value -match '<clipPath>Trigger/SenkuKogetsu/senku_kogetsu_cast</clipPath>')
) 'BDP_Sound_SenkuKogetsuCast must point at the formal cast clip.'

Write-Output 'SenkuKogetsuAuthoringSmokeTests PASS'
