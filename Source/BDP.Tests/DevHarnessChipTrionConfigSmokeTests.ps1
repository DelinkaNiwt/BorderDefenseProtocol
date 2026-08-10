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
$devHarnessDefsRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\1.6\Defs'

$chipDefsPaths = @(
    (Join-Path $devHarnessDefsRoot 'Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'),
    (Join-Path $devHarnessDefsRoot 'Things\Items\Chips\Test\ThingDefs_TestChips_AbilityHediff.xml'),
    (Join-Path $devHarnessDefsRoot 'Things\Items\Chips\Test\ThingDefs_TestChips_PassiveMixed.xml')
)
$hediffDefsPath = Join-Path $devHarnessDefsRoot 'Health\Expression\Test\HediffDefs_TestExpressionOnly.xml'

foreach ($chipDefsPath in $chipDefsPaths) {
    Assert-True (Test-Path -LiteralPath $chipDefsPath) "候选芯片定义文件必须存在：$chipDefsPath"
}

$chipDefsText = ($chipDefsPaths | ForEach-Object { Get-Content -LiteralPath $_ -Raw -Encoding utf8 }) -join "`n"
$hediffDefsText = Get-Content -LiteralPath $hediffDefsPath -Raw -Encoding utf8

$chipNames = @(
    'BDP_TestChipRanged',
    'BDP_TestChipMelee',
    'BDP_TestChipRangedVolley',
    'BDP_TestChipAbility',
    'BDP_TestChipHediff',
    'BDP_TestChipPassive',
    'BDP_TestChipMixed',
    'BDP_TestChipDualWieldRanged'
)

$expectedActivationCostByChip = @{
    'BDP_TestChipPassive' = '0'
}

foreach ($chipName in $chipNames) {
    $chipMatch = [regex]::Match(
        $chipDefsText,
        "(?s)<defName>$chipName</defName>.*?<Trion>(.*?)</Trion>")

    Assert-True $chipMatch.Success "$chipName must expose a chip-level Trion block."

    $chipTrionText = $chipMatch.Groups[1].Value
    $expectedActivationCost = if ($expectedActivationCostByChip.ContainsKey($chipName)) {
        $expectedActivationCostByChip[$chipName]
    } else {
        '10'
    }

    Assert-True (
        ($chipTrionText -match '<CapacityCost>100</CapacityCost>') -and
        ($chipTrionText -match "<ActivationCost>$expectedActivationCost</ActivationCost>") -and
        (-not ($chipTrionText -match '<PowerRequirement>'))
    ) "$chipName must set CapacityCost=100 and the expected chip-level ActivationCost without the obsolete PowerRequirement field."
}

$abilityChipMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<defName>BDP_TestChipAbility</defName>.*?<Entries>\s*<li>(.*?)</li>\s*</Entries>')

Assert-True $abilityChipMatch.Success 'BDP_TestChipAbility must continue existing as the first-round independent Ability business sample.'
Assert-True (
    ($abilityChipMatch.Groups[1].Value -match '<Kind>Ability</Kind>') -and
    ($abilityChipMatch.Groups[1].Value -match '<AbilityDefName>BDP_TestAbility_ExpressionOnly</AbilityDefName>') -and
    ($abilityChipMatch.Groups[1].Value -match '<UseCost>50</UseCost>') -and
    ($abilityChipMatch.Groups[1].Value -match '<MinimumRequired>50</MinimumRequired>')
) 'BDP_TestChipAbility must keep an independent Ability expression entry and declare expression-level Trion cost.'

$hediffChipMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<defName>BDP_TestChipHediff</defName>.*?<Entries>\s*<li>(.*?)</li>\s*</Entries>')

Assert-True $hediffChipMatch.Success 'BDP_TestChipHediff must continue existing as the first-round independent Hediff business sample.'
Assert-True (
    ($hediffChipMatch.Groups[1].Value -match '<Kind>Hediff</Kind>') -and
    ($hediffChipMatch.Groups[1].Value -match '<HediffDefName>BDP_TestHediff_ExpressionOnly</HediffDefName>') -and
    ($hediffChipMatch.Groups[1].Value -match '<HediffApplyModeKey>countToSeverity</HediffApplyModeKey>') -and
    (-not ($hediffChipMatch.Groups[1].Value -match '<SustainCost>'))
) 'BDP_TestChipHediff must keep an independent Hediff expression entry without expression-level continuous drain.'

Assert-True (
    $hediffDefsText -match '<defName>BDP_TestHediff_ExpressionOnly</defName>' -and
    $hediffDefsText -match '<hediffClass>BDP\.Core\.Expressions\.BdpExpressionHostHediff</hediffClass>'
) 'BDP_TestHediff_ExpressionOnly must declare BdpExpressionHostHediff as the formal expression host class.'

$rangedEntryMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<defName>BDP_TestChipRanged</defName>.*?<Entries>\s*<li>(.*?)</li>\s*</Entries>')

Assert-True $rangedEntryMatch.Success 'BDP_TestChipRanged must keep its primary expression entry.'
Assert-True (
    $rangedEntryMatch.Groups[1].Value -match '<Trion>\s*<UseCost>5</UseCost>\s*</Trion>'
) 'BDP_TestChipRanged primary expression entry must declare UseCost=5.'

$volleyEntryMatch = [regex]::Match(
    $chipDefsText,
    '(?s)<defName>BDP_TestChipRangedVolley</defName>.*?<Entries>\s*<li>(.*?)</li>\s*</Entries>')

Assert-True $volleyEntryMatch.Success 'BDP_TestChipRangedVolley must keep its primary expression entry.'
Assert-True (
    $volleyEntryMatch.Groups[1].Value -match '<Trion>\s*<UseCost>5</UseCost>\s*</Trion>'
) 'BDP_TestChipRangedVolley primary expression entry must declare UseCost=5.'

Write-Output 'CandidateChipTrionConfig PASS'
