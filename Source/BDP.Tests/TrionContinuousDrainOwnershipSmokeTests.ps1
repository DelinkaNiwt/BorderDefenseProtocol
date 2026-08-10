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

$chipConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Config\ChipTrionConfig.cs'
$chipContractPath = Join-Path $repoRoot 'Source\BDP\Core\Chips\Contract\ChipTrionContract.cs'
$triggerBindingPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerTrionBindingService.cs'
$expressionRoot = Join-Path $repoRoot 'Source\BDP\Core\Expressions'
$comboRoot = Join-Path $repoRoot 'Source\BDP\Core\Combos'
$hediffDrainPath = Join-Path $repoRoot 'Source\BDP\Core\Hediffs\HediffComp_BdpTrionDrain.cs'
$hediffDrainPropsPath = Join-Path $repoRoot 'Source\BDP\Core\Hediffs\HediffCompProperties_BdpTrionDrain.cs'
$combatBodyBindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBodySession\CombatBodySessionTrionBinding.cs'
$woundBindingPath = Join-Path $repoRoot 'Source\BDP\Core\CombatBody\Wounds\CombatBodyWoundTrionBinding.cs'

$chipConfigText = Get-Content -LiteralPath $chipConfigPath -Raw -Encoding utf8
$chipContractText = Get-Content -LiteralPath $chipContractPath -Raw -Encoding utf8
$triggerBindingText = Get-Content -LiteralPath $triggerBindingPath -Raw -Encoding utf8
$combatBodyBindingText = Get-Content -LiteralPath $combatBodyBindingPath -Raw -Encoding utf8
$woundBindingText = Get-Content -LiteralPath $woundBindingPath -Raw -Encoding utf8
$expressionText = (Get-ChildItem -LiteralPath $expressionRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"
$comboText = (Get-ChildItem -LiteralPath $comboRoot -Filter '*.cs' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"
$devHarnessXmlText = (Get-ChildItem -LiteralPath (Join-Path $devHarnessRoot '1.6') -Filter '*.xml' -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw -Encoding utf8
}) -join "`n"

# 芯片本体只负责装载、激活与功率门槛，不再拥有持续消耗入口。
Assert-True (
    $chipConfigText -notmatch 'ActiveDrainPerSecond'
) 'ChipTrionConfig must not expose ActiveDrainPerSecond.'
Assert-True (
    $chipContractText -notmatch 'ActiveDrainPerSecond'
) 'ChipTrionContract must not carry ActiveDrainPerSecond.'
Assert-True (
    ($triggerBindingText -notmatch 'ActiveDrainPerSecond') -and
    ($triggerBindingText -notmatch 'CreateChip') -and
    ($triggerBindingText -notmatch '\.RegisterDrain\(')
) 'TriggerTrionBindingService must stop registering chip sustain drains.'

# 持续消耗统一归最终有效表达所有，按有效来源数选择总费用档位。
Assert-True (
    $expressionText -match '\bSustainCostBySourceCount\b'
) 'Expression system must own SustainCostBySourceCount.'
Assert-True (
    ($expressionText -match 'ExpressionSustainDrainService') -and
    ($expressionText -match 'GetDrainSnapshot\(')
) 'Expression runtime must reconcile final sustain drains against the central ledger.'
Assert-True (
    $comboText -notmatch 'SustainCostBySourceCountResolve|SustainCostResolve'
) 'Combo sustain tiers must be explicit and must not have an inheritance switch.'
Assert-True (-not (Test-Path -LiteralPath $hediffDrainPath)) 'Expression-only Hediff Trion drain component must be removed.'
Assert-True (-not (Test-Path -LiteralPath $hediffDrainPropsPath)) 'Expression-only Hediff Trion drain properties must be removed.'
Assert-True (
    $devHarnessXmlText -match '<SustainCostBySourceCount>'
) 'DevHarness XML must configure expression sustain tiers.'
Assert-True (
    $devHarnessXmlText -notmatch '<SustainCost>'
) 'DevHarness XML must not restore the obsolete scalar SustainCost.'
Assert-True (
    $devHarnessXmlText -notmatch 'BDP\.Core\.Hediffs\.HediffCompProperties_BdpTrionDrain'
) 'DevHarness XML must not attach the removed expression-only Hediff drain component.'

# 另外两类正式来源仍由各自生命周期登记，不能被本次清理误删。
Assert-True (
    ($combatBodyBindingText -match 'owner\.MaintenanceDrainPerSecond') -and
    ($combatBodyBindingText -match 'RegisterDrain\(combatBodyMaintenanceDrainKey')
) 'Combat-body maintenance drain must remain intact.'
Assert-True (
    $woundBindingText -match 'commands\.RegisterDrain\(key,\s*drainPerSecond\)'
) 'Combat-body wound leakage drain must remain intact.'

Write-Output 'TrionContinuousDrainOwnershipSmokeTests PASS'
