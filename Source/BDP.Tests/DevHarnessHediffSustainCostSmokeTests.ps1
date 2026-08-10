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
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_AbilityHediff.xml'

[xml]$document = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8
$chipDef = @($document.Defs.ThingDef) | Where-Object { $_.defName -eq 'BDP_TestChipHediff' } | Select-Object -First 1
Assert-True ($null -ne $chipDef) 'BDP_TestChipHediff must exist.'

$chipConfig = @($chipDef.modExtensions.li) | Select-Object -First 1
Assert-True ($null -ne $chipConfig) 'BDP_TestChipHediff must have a chip definition config.'
Assert-True ($null -eq $chipConfig.Trion.ActiveDrainPerSecond) 'State chip must not configure chip-level active drain.'

$expressionEntry = @($chipConfig.Expression.Entries.li) |
    Where-Object { $_.Id -eq 'test_hediff_primary' } |
    Select-Object -First 1
Assert-True ($null -ne $expressionEntry) 'State chip must keep test_hediff_primary.'

$tiers = @($expressionEntry.Trion.SustainCostBySourceCount.li)
Assert-True ($tiers.Count -eq 2) 'State expression must configure exactly two sustain tiers.'
Assert-True (
    ([int]$tiers[0].SourceCount -eq 1) -and
    ([float]$tiers[0].TotalPerSecond -eq 2)
) 'One effective state-expression source must cost 2 Trion per second.'
Assert-True (
    ([int]$tiers[1].SourceCount -eq 2) -and
    ([float]$tiers[1].TotalPerSecond -eq 5)
) 'Two effective state-expression sources must cost 5 Trion per second in total.'

Write-Output 'DevHarnessHediffSustainCostSmokeTests PASS'
