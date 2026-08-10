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

$trionRoot = Join-Path $repoRoot 'Source\BDP\Core\Trion'
$woundBindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'
$chipConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Config\ChipTrionConfig.cs'
$chipContractPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Contract\ChipTrionContract.cs'
$expressionTrionConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Config\ExpressionSourceTrionConfig.cs'
$payloadPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionRuntimePayload.cs'
$combatBodyPropsPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Bridge\CompProperties_CombatBodyHost.cs'

$trionText = (Get-ChildItem -LiteralPath $trionRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"
$woundBindingText = Get-Content -LiteralPath $woundBindingPath -Raw -Encoding utf8
$chipConfigText = Get-Content -LiteralPath $chipConfigPath -Raw -Encoding utf8
$chipContractText = Get-Content -LiteralPath $chipContractPath -Raw -Encoding utf8
$expressionTrionConfigText = Get-Content -LiteralPath $expressionTrionConfigPath -Raw -Encoding utf8
$payloadText = Get-Content -LiteralPath $payloadPath -Raw -Encoding utf8
$combatBodyPropsText = Get-Content -LiteralPath $combatBodyPropsPath -Raw -Encoding utf8

Assert-True ($trionText -match 'TotalDrainPerSecond') 'Trion reader must expose aggregate drain per second.'
Assert-True ($trionText -notmatch 'TotalDrainPerDay') 'Trion core must stop naming aggregate drain as per day.'
Assert-True ($trionText -match 'RegisterDrain\s*\(\s*TrionDrainKey\s+key,\s*float\s+perSecond\s*\)') 'Trion drain registration must accept per-second values.'
Assert-True ($trionText -match 'TicksPerSecond\s*=\s*60f') 'Trion core must define the RimWorld tick-per-second conversion.'
Assert-True ($trionText -match 'totalDrainPerSecond\s*/\s*TicksPerSecond') 'Drain settlement must convert per-second drain by 60 ticks per second.'
Assert-True ($trionText -notmatch 'SecondsPerDay') 'Trion core display code must not convert drains through seconds per day.'

Assert-True ($woundBindingText -notmatch 'SecondsPerDay') 'Wound drain binding must not convert severity drain to per day.'
Assert-True ($woundBindingText -match 'ResolveDrainPerSecond') 'Wound drain binding must resolve per-second drain.'
Assert-True ($woundBindingText -match 'RegisterDrain\(key, drainPerSecond\)') 'Wound drain binding must register per-second drain.'

Assert-True ($chipConfigText -notmatch 'ActiveDrainPerSecond') 'ChipTrionConfig must not expose ActiveDrainPerSecond.'
Assert-True ($chipConfigText -notmatch 'DrainPerDay') 'ChipTrionConfig must stop exposing DrainPerDay.'
Assert-True ($chipContractText -notmatch 'ActiveDrainPerSecond') 'ChipTrionContract must not expose ActiveDrainPerSecond.'
Assert-True ($chipContractText -notmatch 'DrainPerDay') 'ChipTrionContract must stop exposing DrainPerDay.'
Assert-True (
    $expressionTrionConfigText -match 'SustainCostBySourceCount'
) 'Expression Trion config must expose sustain totals by effective source count.'
Assert-True ($payloadText -notmatch 'ActiveDrainPerSecond') 'Expression runtime payload must not carry chip active drain per second.'
Assert-True ($payloadText -match 'SustainCostBySourceCount') 'Expression runtime payload must carry expression sustain tiers.'
Assert-True ($payloadText -notmatch 'DrainPerDay') 'Expression runtime payload must stop carrying drain per day.'
Assert-True ($combatBodyPropsText -match 'maintenanceDrainPerSecond') 'Combat body maintenance drain config must be per second.'
Assert-True ($combatBodyPropsText -notmatch 'maintenanceDrainPerDay') 'Combat body maintenance drain config must stop using per-day naming.'

Write-Output 'TrionDrainPerSecondBoundarySmokeTests PASS'
