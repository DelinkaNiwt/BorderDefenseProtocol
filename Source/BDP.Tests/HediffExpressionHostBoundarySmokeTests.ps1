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

$hostHediffPath = Join-Path $repoRoot "Source\BDP\Core\Expressions\Projection\BdpExpressionHostHediff.cs"
$validatorPath = Join-Path $repoRoot "Source\BDP\Core\Chips\Validation\DefaultChipDefinitionValidator.cs"
$hediffSyncPath = Join-Path $repoRoot "Source\BDP\Core\Expressions\Projection\DefaultExpressionHediffHostSynchronizer.cs"
$hediffDefsPath = Join-Path $devHarnessRoot "1.6\Defs\Health\Expression\Test\HediffDefs_TestExpressionOnly.xml"

$hostHediffText = ""
if (Test-Path -LiteralPath $hostHediffPath) {
    $hostHediffText = Get-Content -LiteralPath $hostHediffPath -Raw -Encoding utf8
}

$validatorText = Get-Content -LiteralPath $validatorPath -Raw -Encoding utf8
$hediffSyncText = Get-Content -LiteralPath $hediffSyncPath -Raw -Encoding utf8
$hediffDefsText = Get-Content -LiteralPath $hediffDefsPath -Raw -Encoding utf8

Assert-True (
    $hostHediffText.Contains("class BdpExpressionHostHediff") -and
    $hostHediffText.Contains(": HediffWithComps") -and
    $hostHediffText.Contains("SyncExpressionResults") -and
    $hostHediffText.Contains("ExpressionResults")
) "BdpExpressionHostHediff must inherit HediffWithComps and carry expression-bound results."

Assert-True (
    $hediffSyncText.Contains("SyncExpressionResults") -and
    $hediffSyncText.Contains("Results")
) "Hediff synchronizer must push expression results into the BDP host Hediff."

Assert-True (
    ($validatorText.Contains("typeof(BDP.Core.Expressions.BdpExpressionHostHediff)")) -or
    ($validatorText.Contains("typeof(BdpExpressionHostHediff)"))
) "Validator must check BdpExpressionHostHediff as the hediff host boundary."

Assert-True (
    $validatorText.Contains("hediffClass") -and
    (-not $validatorText.Contains("LooksLikeBdpDedicatedHediffDef"))
) "Validator must move from name-prefix rule to hediffClass protocol."

Assert-True (
    $validatorText.Contains("countToSeverity") -and
    $validatorText.Contains("HediffApplyModeKey") -and
    (-not $validatorText.Contains("退化为"))
) "Invalid HediffApplyModeKey must no longer fall back."

Assert-True (
    $hediffDefsText.Contains("<hediffClass>BDP.Core.Expressions.BdpExpressionHostHediff</hediffClass>")
) "DevHarness hediff must declare BdpExpressionHostHediff."

Write-Output "HediffExpressionHostBoundarySmokeTests PASS"
