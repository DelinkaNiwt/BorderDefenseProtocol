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

$payloadPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionRuntimePayload.cs'
$formalResultPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\FormalExpressionResult.cs'
$materialPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Model\ExpressionSourceMaterial.cs'
$collectorPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ExpressionSourceCollector.cs'
$singleBuilderPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\SingleSideExpressionBuilder.cs'
$comboFactoryPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Pipeline\ComboFormalExpressionResultFactory.cs'
$comboEntryPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboExpressionEntryConfig.cs'
$comboConfigPath = Join-Path $repoRoot 'Source\BDP\Core\Combos\Config\ComboExpressionConfig.cs'
$verbBindingPath = Join-Path $repoRoot 'Source\BDP\Core\VerbHosting\BdpFormalVerbBindingState.cs'
$abilitySyncPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionAbilityHostSynchronizer.cs'
$abilityCostPath = Join-Path $repoRoot 'Source\BDP\Core\Abilities\CompAbilityEffect_BdpTrionCost.cs'
$hostHediffPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\BdpExpressionHostHediff.cs'
$hediffSyncPath = Join-Path $repoRoot 'Source\BDP\Core\Expressions\Projection\DefaultExpressionHediffHostSynchronizer.cs'

Assert-True (Test-Path -LiteralPath $payloadPath) 'ExpressionRuntimePayload must exist.'

$payloadText = Get-Content -LiteralPath $payloadPath -Raw -Encoding utf8
$formalResultText = Get-Content -LiteralPath $formalResultPath -Raw -Encoding utf8
$materialText = Get-Content -LiteralPath $materialPath -Raw -Encoding utf8
$collectorText = Get-Content -LiteralPath $collectorPath -Raw -Encoding utf8
$singleBuilderText = Get-Content -LiteralPath $singleBuilderPath -Raw -Encoding utf8
$comboFactoryText = Get-Content -LiteralPath $comboFactoryPath -Raw -Encoding utf8
$comboEntryText = Get-Content -LiteralPath $comboEntryPath -Raw -Encoding utf8
$comboConfigText = Get-Content -LiteralPath $comboConfigPath -Raw -Encoding utf8
$verbBindingText = Get-Content -LiteralPath $verbBindingPath -Raw -Encoding utf8
$abilitySyncText = Get-Content -LiteralPath $abilitySyncPath -Raw -Encoding utf8
$abilityCostText = Get-Content -LiteralPath $abilityCostPath -Raw -Encoding utf8
$hostHediffText = Get-Content -LiteralPath $hostHediffPath -Raw -Encoding utf8
$hediffSyncText = Get-Content -LiteralPath $hediffSyncPath -Raw -Encoding utf8

Assert-True ($payloadText -match 'class ExpressionRuntimePayload') 'Runtime payload class must be declared.'
Assert-True ($payloadText -match 'class ExpressionChipSnapshot') 'Runtime payload must include chip snapshot.'
Assert-True ($payloadText -match 'class ExpressionComboSnapshot') 'Runtime payload must include combo snapshot.'
Assert-True ($payloadText -match 'class ExpressionEntrySnapshot') 'Runtime payload must include expression entry snapshot.'
Assert-True ($payloadText -match 'ChipTrion') 'Runtime payload must carry chip-level Trion.'
Assert-True ($payloadText -match 'ExpressionTrion') 'Runtime payload must carry expression-level Trion.'
Assert-True (
    $payloadText -notmatch 'ExpressionChipExtensionSnapshot'
) 'Runtime payload must not carry arbitrary static chip extensions.'
Assert-True (
    ($payloadText -notmatch '\bInitialModeKey\b') -and
    ($collectorText -notmatch '\bInitialModeKey\b')
) 'Runtime chip snapshots must not carry the removed loadout initial mode key.'
Assert-True (-not ($formalResultText -match 'RuntimePayload')) 'FormalExpressionResult must not carry runtime payload.'
Assert-True ($materialText -match 'RuntimePayload') 'ExpressionSourceMaterial must carry runtime payload.'
Assert-True ($collectorText -match 'BuildRuntimePayload') 'ExpressionSourceCollector must build runtime payload.'
Assert-True ($collectorText -match 'ChipSurfaceAccess\.Read') 'ExpressionSourceCollector must read chip contract through ChipSurfaceAccess.'
Assert-True (-not ($singleBuilderText -match 'RuntimePayload')) 'SingleSideExpressionBuilder must not pass runtime payload into formal results.'
Assert-True ($comboConfigText -match 'ComboExpressionTrionResolutionConfig') 'Combo config must expose Trion resolve config.'
Assert-True ($comboEntryText -match 'TrionResolve') 'Combo entry must expose TrionResolve.'
Assert-True ($comboFactoryText -notmatch 'RuntimePayload\s*=') 'Combo factory must not project runtime payload onto formal results.'
Assert-True ($comboFactoryText -match 'ResolveResultTrion') 'Combo factory must resolve expression Trion centrally.'
Assert-True (-not ($verbBindingText -match 'RuntimePayload')) 'BdpFormalVerbBindingState must not carry runtime payload.'
Assert-True ($abilitySyncText -match 'TryResolveBoundAbilityResult') 'Ability synchronizer must expose expression binding lookup.'
Assert-True ($abilityCostText -match 'TryResolveBoundAbilityResult') 'Ability Trion cost must read expression binding.'
Assert-True (-not ($abilityCostText -match 'Props\.TrionCost')) 'Ability Trion cost must not read BDP cost from AbilityDef props.'
Assert-True ($hostHediffText -match 'SyncExpressionResults') 'Expression host Hediff must bind expression results.'
Assert-True ($hediffSyncText -match 'SyncExpressionResults') 'Hediff synchronizer must push expression results into host Hediff.'
Write-Output 'ExpressionRuntimePayloadBoundarySmokeTests PASS'
