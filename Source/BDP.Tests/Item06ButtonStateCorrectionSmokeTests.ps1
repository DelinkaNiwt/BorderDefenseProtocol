$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$developmentRoot = Join-Path $repoRoot 'Source\BDP.Development'

$combatButtonPath = Join-Path $coreRoot 'CombatBody\External\CombatBodyTriggerGizmoProvider.cs'
$trionBridgePath = Join-Path $coreRoot 'Genes\TrionGeneGizmoBridge.cs'
$emissionDiagnosticsPath = Join-Path $coreRoot 'Trigger\Visual\Diagnostics\TriggerVisualEmissionDiagnosticsAccess.cs'
$markerButtonPath = Join-Path $developmentRoot 'Trigger\Diagnostics\TriggerVisualMarkerGizmoProvider.cs'
$markerMapPath = Join-Path $developmentRoot 'Trigger\Diagnostics\MapComponent_TriggerVisualMarkerOverlay.cs'

$combatButtonText = Get-Content -LiteralPath $combatButtonPath -Raw -Encoding utf8
$trionBridgeText = Get-Content -LiteralPath $trionBridgePath -Raw -Encoding utf8
$emissionDiagnosticsText = Get-Content -LiteralPath $emissionDiagnosticsPath -Raw -Encoding utf8
$markerButtonText = Get-Content -LiteralPath $markerButtonPath -Raw -Encoding utf8
$markerMapText = Get-Content -LiteralPath $markerMapPath -Raw -Encoding utf8

Assert-True (
    ($trionBridgeText -match 'DebugSettings\.godMode') -and
    ($trionBridgeText -notmatch 'Prefs\.DevMode')
) 'Trion value debug buttons must be visible only in god mode.'

Assert-True (
    ($markerButtonText -match 'DebugSettings\.godMode') -and
    ($markerButtonText -notmatch 'Prefs\.DevMode') -and
    ($markerMapText -match 'DebugSettings\.godMode') -and
    ($markerMapText -notmatch 'Prefs\.DevMode') -and
    ($emissionDiagnosticsText -match 'DebugSettings\.godMode') -and
    ($emissionDiagnosticsText -notmatch 'Prefs\.DevMode')
) 'Draw-point button, rendering, and launch-point recording must all follow god mode.'

Assert-True (
    ([regex]::Matches($combatButtonText, 'new Command_Action')).Count -eq 1 -and
    ($combatButtonText -match 'switch \(reader\.Phase\)') -and
    ($combatButtonText -match 'case CombatBodyPhase\.Inactive:[\s\S]*BDP_Command_CombatBody_Activate[\s\S]*commands\.TryActivate\(\)[\s\S]*!reader\.CanActivate\(\)[\s\S]*BDP_Command_CombatBody_TransformLocked') -and
    ($combatButtonText -match 'case CombatBodyPhase\.Active:[\s\S]*BDP_Command_CombatBody_Release[\s\S]*commands\.RequestRelease[\s\S]*!reader\.CanManualDeactivate\(\)[\s\S]*BDP_Command_CombatBody_TransformLocked') -and
    ($combatButtonText -match 'case CombatBodyPhase\.Collapsing:[\s\S]*command\.Disable\(') -and
    ($combatButtonText -match 'case CombatBodyPhase\.Cooldown:[\s\S]*command\.Disable\(')
) 'One combat-body action must follow phase truth and disable during the short transform lock, collapse, or cooldown.'

Write-Output 'Item06ButtonStateCorrectionSmokeTests PASS'
