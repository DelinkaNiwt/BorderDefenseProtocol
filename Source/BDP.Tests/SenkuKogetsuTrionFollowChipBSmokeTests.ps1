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
$comboDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Pawn\Combos\SenkuKogetsu\ComboDefs_SenkuKogetsu.xml'
$abilityDefsPath = Join-Path $repoRoot '1.6\Content\Defs\Abilities\SenkuKogetsu\AbilityDefs_SenkuKogetsu.xml'
$abilityVerbPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\BdpVerb_CastAbility.cs'
$abilityCostPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\CompAbilityEffect_BdpTrionCost.cs'
$snapshotBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSnapshotBuilder.cs'
$compositeResolverPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\CompositeExpressionResolver.cs'

Assert-True (Test-Path -LiteralPath $chipDefsPath) '主模组必须保留正式弧月与旋空芯片定义。'
Assert-True (Test-Path -LiteralPath $comboDefsPath) '主模组必须保留正式旋空弧月组合定义。'
Assert-True (Test-Path -LiteralPath $abilityDefsPath) '主模组必须保留正式旋空弧月能力定义。'
Assert-True (Test-Path -LiteralPath $abilityVerbPath) 'Main mod must keep BdpVerb_CastAbility.'
Assert-True (Test-Path -LiteralPath $abilityCostPath) 'Main mod must keep CompAbilityEffect_BdpTrionCost.'
Assert-True (Test-Path -LiteralPath $snapshotBuilderPath) 'Main mod must keep ExpressionSnapshotBuilder.'
Assert-True (Test-Path -LiteralPath $compositeResolverPath) 'Main mod must keep CompositeExpressionResolver.'

$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8
$comboDefsText = Get-Content -LiteralPath $comboDefsPath -Raw -Encoding utf8
$abilityDefsText = Get-Content -LiteralPath $abilityDefsPath -Raw -Encoding utf8
$abilityVerbText = Get-Content -LiteralPath $abilityVerbPath -Raw -Encoding utf8
$abilityCostText = Get-Content -LiteralPath $abilityCostPath -Raw -Encoding utf8
$snapshotBuilderText = Get-Content -LiteralPath $snapshotBuilderPath -Raw -Encoding utf8
$compositeResolverText = Get-Content -LiteralPath $compositeResolverPath -Raw -Encoding utf8

$kogetsuMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<ThingDef.*?<defName>BDP_Chip_Kogetsu</defName>(.*?)</ThingDef>')
$senkuMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<ThingDef.*?<defName>BDP_Chip_Senku</defName>(.*?)</ThingDef>')
$comboMatch = [regex]::Match(
    $comboDefsText,
    '(?s)<BDP\.Core\.Combos\.ComboDef>.*?<defName>BDP_Combo_SenkuKogetsu</defName>(.*?)</BDP\.Core\.Combos\.ComboDef>')
$abilityMatch = [regex]::Match(
    $abilityDefsText,
    '(?s)<AbilityDef>.*?<defName>BDP_Ability_SenkuKogetsu</defName>(.*?)</AbilityDef>')

Assert-True $kogetsuMatch.Success 'BDP_Chip_Kogetsu must exist.'
Assert-True $senkuMatch.Success 'BDP_Chip_Senku must exist.'
Assert-True $comboMatch.Success 'BDP_Combo_SenkuKogetsu must exist.'
Assert-True $abilityMatch.Success 'BDP_Ability_SenkuKogetsu must exist.'

Assert-True (
    ($kogetsuMatch.Groups[1].Value -match '<UseCost>5</UseCost>')
) 'BDP_Chip_Kogetsu must keep the lower UseCost on chipA so cost-following can be observed.'

Assert-True (
    ($senkuMatch.Groups[1].Value -match '<UseCost>50</UseCost>') -and
    ($senkuMatch.Groups[1].Value -match '<MinimumRequired>50</MinimumRequired>') -and
    ($senkuMatch.Groups[1].Value -match '<Kind>Passive</Kind>') -and
    ($senkuMatch.Groups[1].Value -match '<PassiveKey>bdp\.senku\.passive</PassiveKey>') -and
    (-not ($senkuMatch.Groups[1].Value -match '<AbilityDefName>')) -and
    (-not ($senkuMatch.Groups[1].Value -match '<ConditionKey>mode\.senku_placeholder_never_true</ConditionKey>'))
) 'BDP_Chip_Senku must keep the higher UseCost/MinimumRequired on chipB and stay a passive source entry instead of an unpublished ability placeholder.'

Assert-True (
    ($comboMatch.Groups[1].Value -match '<chipA>BDP_Chip_Kogetsu</chipA>') -and
    ($comboMatch.Groups[1].Value -match '<chipB>BDP_Chip_Senku</chipB>') -and
    ($comboMatch.Groups[1].Value -match '<AbilityDefName>BDP_Ability_SenkuKogetsu</AbilityDefName>') -and
    ($comboMatch.Groups[1].Value -match '<UseCostResolve>FollowChipSub</UseCostResolve>') -and
    ($comboMatch.Groups[1].Value -match '<MinimumRequiredResolve>FollowChipSub</MinimumRequiredResolve>') -and
    (-not ($comboMatch.Groups[1].Value -match '<Trion>'))
) 'BDP_Combo_SenkuKogetsu must keep combo cost resolution explicitly following chipB Senku.'

Assert-True (
    ($abilityMatch.Groups[1].Value -match '<verbClass>BDP\.Core\.Abilities\.BdpVerb_CastAbility</verbClass>') -and
    ($abilityMatch.Groups[1].Value -match 'Class\s*=\s*\"BDP\.Core\.Abilities\.CompProperties_AbilityEffect_BdpTrionCost\"') -and
    (-not ($abilityMatch.Groups[1].Value -match 'BDP\.Trigger\.CompProperties_AbilityTrionCost')) -and
    (-not ($abilityMatch.Groups[1].Value -match '<costSourceChipDef>'))
) 'BDP_Ability_SenkuKogetsu must stay on the formal Bdp ability cost path and must not reintroduce legacy chip-def cost routing.'

Assert-True (
    ($abilityVerbText -match 'TryCommitCastCost') -and
    ($abilityCostText -match 'TryResolveBoundAbilityResult') -and
    ($abilityCostText -match 'MinimumRequired') -and
    ($abilityCostText -match 'UseCost')
) 'Main mod must keep the formal ability bound-result trion gate and cast-cost commit path.'

Assert-True (
    ($snapshotBuilderText -match 'IReadOnlyList<ExpressionSourceMaterial>\s+collected\s*=\s*sourceCollector\.Collect') -and
    ($snapshotBuilderText -match 'compositeExpressionResolver\.Resolve\(\s*pawn,\s*resolvedMainSet,\s*resolvedSubSet,\s*triggerLoadoutReader,\s*materialIndex,\s*collected\s*\)')
) 'ExpressionSnapshotBuilder must pass unfiltered source materials into combo resolution so chipB source data survives even when chipB is a passive-only source entry.'

Assert-True (
    ($compositeResolverText -match 'ResolveSourceMaterial\(sourceMaterials,\s*TriggerSide\.Main\)') -and
    ($compositeResolverText -match 'ResolveSourceMaterial\(sourceMaterials,\s*TriggerSide\.Sub\)') -and
    ($compositeResolverText -match 'private static ExpressionSourceMaterial ResolveSourceMaterial\(\s*IReadOnlyList<ExpressionSourceMaterial>\s+sourceMaterials,\s*TriggerSide\s+side\s*\)') -and
    ($compositeResolverText -match 'material\.Side\s*!=\s*side')
) 'CompositeExpressionResolver must resolve combo source materials by active side material, not by requiring an already-published side result.'

Write-Output 'SenkuKogetsuTrionFollowChipSubSmokeTests PASS'
