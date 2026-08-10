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
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs'

$abilityDefsPath = Join-Path $devHarnessRoot 'Abilities\Expression\Test\AbilityDefs_TestExpressionOnly.xml'
$comboDefsPath = Join-Path $devHarnessRoot 'Pawn\Combos\Test\ComboDefs_TestCombos.xml'
$hediffDefsPath = Join-Path $devHarnessRoot 'Health\Expression\Test\HediffDefs_TestExpressionOnly.xml'
$chipDefsPath = Join-Path $devHarnessRoot 'Things\Items\Chips\Test\ThingDefs_TestChips_AbilityHediff.xml'

Assert-True (Test-Path -LiteralPath $abilityDefsPath) 'DevHarness must define BDP_TestAbility_ExpressionOnly in a dedicated AbilityDefs xml.'
Assert-True (Test-Path -LiteralPath $comboDefsPath) 'DevHarness must define combo downstream samples in a dedicated ComboDefs xml.'
Assert-True (Test-Path -LiteralPath $hediffDefsPath) 'DevHarness must define BDP_TestHediff_ExpressionOnly in a dedicated HediffDefs xml.'
Assert-True (Test-Path -LiteralPath $chipDefsPath) 'DevHarness must define Ability/Hediff test chips in a dedicated chip xml.'

$abilityDefsText = Get-Content -LiteralPath $abilityDefsPath -Raw -Encoding utf8
$comboDefsText = Get-Content -LiteralPath $comboDefsPath -Raw -Encoding utf8
$hediffDefsText = Get-Content -LiteralPath $hediffDefsPath -Raw -Encoding utf8
$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8

$abilityMatch = [regex]::Match(
    $abilityDefsText,
    '(?s)<AbilityDef>.*?<defName>BDP_TestAbility_ExpressionOnly</defName>(.*?)</AbilityDef>')

Assert-True $abilityMatch.Success 'BDP_TestAbility_ExpressionOnly must exist as a concrete DevHarness ability.'
Assert-True (
    ($abilityMatch.Groups[1].Value -match '<targetRequired>false</targetRequired>') -and
    ($abilityMatch.Groups[1].Value -match '<canTargetSelf>true</canTargetSelf>') -and
    ($abilityMatch.Groups[1].Value -match '<verbClass>BDP\.Core\.Abilities\.BdpVerb_CastAbility</verbClass>') -and
    ($abilityMatch.Groups[1].Value -match 'Class\s*=\s*\"BDP\.Core\.Abilities\.CompProperties_AbilityEffect_BdpTrionCost\"') -and
    (-not ($abilityMatch.Groups[1].Value -match '<TrionCost>')) -and
    ($abilityMatch.Groups[1].Value -match 'Class\s*=\s*\"CompProperties_AbilitySmokepop\"') -and
    ($abilityMatch.Groups[1].Value -match '<smokeRadius>3\.5</smokeRadius>')
) 'BDP_TestAbility_ExpressionOnly must be a self-cast smokepop ability whose BDP Trion cost comes from expression results.'

$abilityChipMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<defName>BDP_TestChipAbility</defName>.*?<Entries>\s*<li>(.*?)</li>\s*</Entries>')

Assert-True $abilityChipMatch.Success 'BDP_TestChipAbility must exist as the Ability business sample chip.'
Assert-True (
    ($abilityChipMatch.Groups[1].Value -match '<AbilityDefName>BDP_TestAbility_ExpressionOnly</AbilityDefName>') -and
    ($abilityChipMatch.Groups[1].Value -match '<UseCost>50</UseCost>') -and
    ($abilityChipMatch.Groups[1].Value -match '<MinimumRequired>50</MinimumRequired>')
) 'BDP_TestChipAbility must declare Ability Trion UseCost and MinimumRequired on the expression entry.'

$comboMatch = [regex]::Match(
    $comboDefsText,
    '(?s)<BDP\.Core\.Combos\.ComboDef>.*?<defName>BDP_TestCombo_ExpressionOnlyDownstream</defName>(.*?)</BDP\.Core\.Combos\.ComboDef>')

Assert-True $comboMatch.Success 'BDP_TestCombo_ExpressionOnlyDownstream must exist as the combo downstream business sample.'
Assert-True (
    ($comboMatch.Groups[1].Value -match '<chipA>BDP_TestChipAbility</chipA>') -and
    ($comboMatch.Groups[1].Value -match '<chipB>BDP_TestChipHediff</chipB>') -and
    ($comboMatch.Groups[1].Value -match '<Kind>Ability</Kind>') -and
    ($comboMatch.Groups[1].Value -match '<AbilityDefName>BDP_TestAbility_ExpressionOnly</AbilityDefName>') -and
    ($comboMatch.Groups[1].Value -match '<UseCostResolve>FollowChipMain</UseCostResolve>') -and
    ($comboMatch.Groups[1].Value -match '<MinimumRequiredResolve>FollowChipMain</MinimumRequiredResolve>') -and
    ($comboMatch.Groups[1].Value -match '<Kind>Hediff</Kind>') -and
    ($comboMatch.Groups[1].Value -match '<HediffDefName>BDP_TestHediff_ExpressionOnly</HediffDefName>') -and
    (-not ($comboMatch.Groups[1].Value -match '<SustainCostResolve>')) -and
    ($comboMatch.Groups[1].Value -match '<Kind>Passive</Kind>') -and
    ($comboMatch.Groups[1].Value -match '<PassiveKey>EmergencyEscape</PassiveKey>')
) 'BDP_TestCombo_ExpressionOnlyDownstream must cover combo Ability, Hediff, and Passive samples that still read source-side Trion context without source verbs.'

$hediffMatch = [regex]::Match(
    $hediffDefsText,
    '(?s)<HediffDef>.*?<defName>BDP_TestHediff_ExpressionOnly</defName>(.*?)</HediffDef>')

Assert-True $hediffMatch.Success 'BDP_TestHediff_ExpressionOnly must exist as a concrete DevHarness hediff.'
Assert-True (
    ($hediffMatch.Groups[1].Value -match '<hediffClass>BDP\.Core\.Expressions\.BdpExpressionHostHediff</hediffClass>') -and
    (-not ($hediffMatch.Groups[1].Value -match 'BDP\.Core\.Hediffs\.HediffCompProperties_BdpTrionDrain')) -and
    (-not ($hediffMatch.Groups[1].Value -match '<DrainStages>')) -and
    (-not ($hediffMatch.Groups[1].Value -match '<DrainPerSecond>')) -and
    ($hediffMatch.Groups[1].Value -match '<minSeverity>0</minSeverity>') -and
    ($hediffMatch.Groups[1].Value -match '<minSeverity>2</minSeverity>') -and
    ($hediffMatch.Groups[1].Value -match '<MoveSpeed>2\.0</MoveSpeed>') -and
    ($hediffMatch.Groups[1].Value -match '<MoveSpeed>5\.0</MoveSpeed>')
) 'BDP_TestHediff_ExpressionOnly must declare BdpExpressionHostHediff, keep vanilla stages, and leave BDP drain cost to expression results.'

$hediffChipMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<defName>BDP_TestChipHediff</defName>.*?<Entries>\s*<li>(.*?)</li>\s*</Entries>')

Assert-True $hediffChipMatch.Success 'BDP_TestChipHediff must exist as the Hediff business sample chip.'
Assert-True (
    ($hediffChipMatch.Groups[1].Value -match '<HediffDefName>BDP_TestHediff_ExpressionOnly</HediffDefName>') -and
    (-not ($hediffChipMatch.Groups[1].Value -match '<SustainCost>'))
) 'BDP_TestChipHediff must remain a Hediff sample without a duplicate expression-level continuous drain.'

Write-Output 'DevHarnessAbilityHediffBusinessSmokeTests PASS'
