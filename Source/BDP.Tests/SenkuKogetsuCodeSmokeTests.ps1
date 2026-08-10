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
$waveCompPath = Join-Path $repoRoot 'Source\BDP.Content\SenkuKogetsu\CompAbilityEffect_SenkuKogetsuWave.cs'
$waveThingPath = Join-Path $repoRoot 'Source\BDP.Content\SenkuKogetsu\BdpSenkuKogetsuWave.cs'
$abilityDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Abilities\SenkuKogetsu\AbilityDefs_SenkuKogetsu.xml'
$waveThingDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Things\Effects\SenkuKogetsu\ThingDefs_SenkuKogetsuWave.xml'

Assert-True (Test-Path -LiteralPath $waveCompPath) 'Content must define CompAbilityEffect_SenkuKogetsuWave.cs.'
Assert-True (Test-Path -LiteralPath $waveThingPath) 'Content must define BdpSenkuKogetsuWave.cs.'
Assert-True (Test-Path -LiteralPath $abilityDefsPath) 'Content must define AbilityDefs_SenkuKogetsu.xml.'
Assert-True (Test-Path -LiteralPath $waveThingDefsPath) 'Content must define ThingDefs_SenkuKogetsuWave.xml.'

$waveCompText = Get-Content -LiteralPath $waveCompPath -Raw -Encoding utf8
$waveThingText = Get-Content -LiteralPath $waveThingPath -Raw -Encoding utf8
$abilityDefsText = Get-Content -LiteralPath $abilityDefsPath -Raw -Encoding utf8
$waveThingDefsText = Get-Content -LiteralPath $waveThingDefsPath -Raw -Encoding utf8

Assert-True (
    ($waveCompText -match 'namespace\s+BDP\.Content\.SenkuKogetsu') -and
    ($waveCompText -match 'class\s+CompProperties_SenkuKogetsuWave\s*:\s*CompProperties_AbilityEffect') -and
    ($waveCompText -match 'class\s+CompAbilityEffect_SenkuKogetsuWave\s*:\s*CompAbilityEffect') -and
    ($waveCompText -match 'BdpSenkuKogetsuWave') -and
    ($waveCompText -match 'GetCrescentParams') -and
    ($waveCompText -match 'CrescentOuterPoint')
) 'CompAbilityEffect_SenkuKogetsuWave.cs must define the formal wave comp properties, effect comp, and shared crescent parameter helpers.'

Assert-True (
    ($waveThingText -match 'namespace\s+BDP\.Content\.SenkuKogetsu') -and
    ($waveThingText -match 'class\s+BdpSenkuKogetsuWave\s*:\s*ThingWithComps') -and
    ($waveThingText -match 'void\s+Launch\s*\(') -and
    ($waveThingText -match 'DamageCrescentSweep') -and
    ($waveThingText -match 'DamageThingsInCell') -and
    ($waveThingText -match 'DrawAt')
) 'BdpSenkuKogetsuWave.cs must define the formal wave entity with launch, damage sweep, and draw logic.'

$abilityMatch = [regex]::Match(
    $abilityDefsText,
    '(?s)<AbilityDef>.*?<defName>BDP_Ability_SenkuKogetsu</defName>(.*?)</AbilityDef>')

Assert-True $abilityMatch.Success 'BDP_Ability_SenkuKogetsu must exist as the formal ability.'
Assert-True (
    ($abilityMatch.Groups[1].Value -match 'Class\s*=\s*\"BDP\.Content\.SenkuKogetsu\.CompProperties_SenkuKogetsuWave\"') -and
    ($abilityMatch.Groups[1].Value -match '<waveDef>BDP_Projectile_SenkuKogetsuWave</waveDef>')
) 'BDP_Ability_SenkuKogetsu must attach the formal wave comp and point it at BDP_Projectile_SenkuKogetsuWave.'

$waveThingMatch = [regex]::Match(
    $waveThingDefsText,
    '(?s)<ThingDef>.*?<defName>BDP_Projectile_SenkuKogetsuWave</defName>(.*?)</ThingDef>')

Assert-True $waveThingMatch.Success 'BDP_Projectile_SenkuKogetsuWave must exist as the wave thing.'
Assert-True (
    ($waveThingMatch.Groups[1].Value -match '<thingClass>BDP\.Content\.SenkuKogetsu\.BdpSenkuKogetsuWave</thingClass>')
) 'BDP_Projectile_SenkuKogetsuWave must point at the Content wave entity class.'

Write-Output 'SenkuKogetsuCodeSmokeTests PASS'
