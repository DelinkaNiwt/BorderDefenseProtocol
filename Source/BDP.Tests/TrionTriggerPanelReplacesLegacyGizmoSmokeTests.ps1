$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'

$panelProviderPath = Join-Path $repoRoot 'Source\BDP.Content\Trigger\UI\TriggerLoadoutPanelProvider.cs'
$legacyStatusPath = Join-Path $devHarnessRoot 'Gizmo_LegacyTriggerStatus.cs'
$legacySlotsPath = Join-Path $devHarnessRoot 'Window_LegacyTriggerSlots.cs'
$legacySummaryPath = Join-Path $devHarnessRoot 'Gizmo_TriggerLoadoutSummary.cs'
$legacyDiagnosticsPath = Join-Path $devHarnessRoot 'Window_TriggerLoadoutDiagnostics.cs'

$panelProviderText = Get-Content -LiteralPath $panelProviderPath -Raw -Encoding utf8

Assert-True ($panelProviderText -match 'TriggerLoadoutPanelProvider') 'Formal Trion panel provider must remain after retiring legacy standalone GUI.'
Assert-True (-not (Test-Path -LiteralPath $legacyStatusPath)) 'Legacy standalone Trigger status gizmo must be deleted.'
Assert-True (-not (Test-Path -LiteralPath $legacySlotsPath)) 'Legacy standalone Trigger slot window must be deleted.'
Assert-True (-not (Test-Path -LiteralPath $legacySummaryPath)) 'Unused Trigger summary gizmo must be deleted.'
Assert-True (-not (Test-Path -LiteralPath $legacyDiagnosticsPath)) 'Retired Trigger loadout diagnostics window must be deleted.'

Write-Output 'TrionTriggerPanelReplacesLegacyGizmoSmokeTests PASS'
