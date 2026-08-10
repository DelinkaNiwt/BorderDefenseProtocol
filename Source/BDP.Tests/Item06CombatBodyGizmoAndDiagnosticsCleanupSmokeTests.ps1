$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$workspaceRoot = Split-Path -Parent (Split-Path -Parent $repoRoot)
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$developmentRoot = Join-Path $repoRoot 'Source\BDP.Development'
$candidateRoot = Join-Path $workspaceRoot '模组工程\BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness'

$combatBodyProviderPath = Join-Path $coreRoot 'CombatBody\External\CombatBodyTriggerGizmoProvider.cs'
$externalContextPath = Join-Path $coreRoot 'Trigger\External\TriggerExternalGizmoContext.cs'
$equippedGizmoServicePath = Join-Path $coreRoot 'Trigger\External\TriggerEquippedGizmoService.cs'
$integrityContractPath = Join-Path $coreRoot 'Trigger\Access\Contracts\ITriggerIntegrityDiagnostics.cs'
$formalSurfacesPath = Join-Path $coreRoot 'Trigger\Access\Surfaces\TriggerFormalSurfaces.cs'
$surfaceAccessPath = Join-Path $coreRoot 'Trigger\Access\Surfaces\TriggerSurfaceAccess.cs'
$triggerBodyPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.cs'
$triggerReadsPath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Reads.cs'
$triggerLifecyclePath = Join-Path $coreRoot 'Trigger\State\CompTriggerBody.Lifecycle.cs'
$visualAccessPath = Join-Path $coreRoot 'Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'
$visualSnapshotPath = Join-Path $coreRoot 'Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsSnapshot.cs'

$contentBootstrapPath = Join-Path $contentRoot 'ContentBootstrap.cs'
$contentDiagnosticsRoot = Join-Path $contentRoot 'Trigger\Diagnostics'
$developmentDiagnosticsRoot = Join-Path $developmentRoot 'Trigger\Diagnostics'
$developmentBootstrapPath = Join-Path $developmentRoot 'DevelopmentBootstrap.cs'
$markerProviderPath = Join-Path $developmentDiagnosticsRoot 'TriggerVisualMarkerGizmoProvider.cs'
$markerSettingsPath = Join-Path $developmentDiagnosticsRoot 'TriggerVisualMarkerSettings.cs'
$markerMapComponentPath = Join-Path $developmentDiagnosticsRoot 'MapComponent_TriggerVisualMarkerOverlay.cs'
$markerDrawerPath = Join-Path $developmentDiagnosticsRoot 'TriggerVisualMarkerOverlayDrawer.cs'

$candidateRemovedFiles = @(
    'DevHarnessTriggerGizmoProvider.cs',
    'DevHarnessVisualMarkerSettings.cs',
    'MapComponent_TriggerVisualMarkerOverlay.cs',
    'TriggerVisualMarkerOverlayDrawer.cs',
    'Window_TriggerLoadoutDiagnostics.cs',
    'Window_CombatBodyDiagnostics.cs',
    'Window_TriggerVisualPoseDiagnostics.cs',
    'Gizmo_LegacyTriggerStatus.cs',
    'Window_LegacyTriggerSlots.cs',
    'Gizmo_TriggerLoadoutSummary.cs'
)

Assert-True (Test-Path -LiteralPath $combatBodyProviderPath) 'Core formal combat-body gizmo provider must exist.'
Assert-True (Test-Path -LiteralPath $contentBootstrapPath) 'Content bootstrap must exist.'
Assert-True (Test-Path -LiteralPath $visualAccessPath) 'Draw-point diagnostics still require the shared visual pose access surface.'
Assert-True (Test-Path -LiteralPath $visualSnapshotPath) 'Draw-point diagnostics still require the shared visual pose snapshot.'

$combatBodyProviderText = Get-Content -LiteralPath $combatBodyProviderPath -Raw -Encoding utf8
$contentBootstrapText = Get-Content -LiteralPath $contentBootstrapPath -Raw -Encoding utf8
$developmentBootstrapText = Get-Content -LiteralPath $developmentBootstrapPath -Raw -Encoding utf8
$externalContextText = Get-Content -LiteralPath $externalContextPath -Raw -Encoding utf8
$equippedGizmoServiceText = Get-Content -LiteralPath $equippedGizmoServicePath -Raw -Encoding utf8
$formalSurfacesText = Get-Content -LiteralPath $formalSurfacesPath -Raw -Encoding utf8
$surfaceAccessText = Get-Content -LiteralPath $surfaceAccessPath -Raw -Encoding utf8
$triggerBodyText = Get-Content -LiteralPath $triggerBodyPath -Raw -Encoding utf8
$triggerReadsText = Get-Content -LiteralPath $triggerReadsPath -Raw -Encoding utf8
$triggerLifecycleText = Get-Content -LiteralPath $triggerLifecyclePath -Raw -Encoding utf8

Assert-True (
    ($combatBodyProviderText -match 'TrionGlandEligibility\.HasActiveTrionGland\(pawn\)') -and
    ($combatBodyProviderText -match 'BDP_Command_CombatBody_Activate') -and
    ($combatBodyProviderText -match 'BDP_Command_CombatBody_Release') -and
    ([regex]::Matches($combatBodyProviderText, 'new Command_Action')).Count -eq 1
) 'Core must present one phase-aware formal combat-body command to eligible pawns.'

Assert-True (
    (Test-Path -LiteralPath $markerProviderPath) -and
    (Test-Path -LiteralPath $markerSettingsPath) -and
    (Test-Path -LiteralPath $markerMapComponentPath) -and
    (Test-Path -LiteralPath $markerDrawerPath)
) 'The retained draw-point diagnostics must live in BDP.Development.'
Assert-True (-not (Test-Path -LiteralPath $contentDiagnosticsRoot)) 'Content must not retain pure development marker diagnostics.'

$markerProviderText = if (Test-Path -LiteralPath $markerProviderPath) {
    Get-Content -LiteralPath $markerProviderPath -Raw -Encoding utf8
} else {
    ''
}

Assert-True (
    ($contentBootstrapText -notmatch 'TriggerVisualMarkerGizmoProvider') -and
    ($developmentBootstrapText -match 'TriggerExternalGizmoRegistry\.Register\(new TriggerVisualMarkerGizmoProvider\(\)\);') -and
    ($markerProviderText -match 'DebugSettings\.godMode') -and
    ($markerProviderText -match 'BDP_Command_TriggerDiagnostics_DrawMarkers') -and
    ($markerProviderText -notmatch '姿态诊断|Trigger诊断|看战体|开战体|关战体|DevHarness')
) 'Development must register one god-mode-only draw-point command without discarded test commands or test naming.'

foreach ($fileName in $candidateRemovedFiles) {
    Assert-True (
        -not (Test-Path -LiteralPath (Join-Path $candidateRoot $fileName))
    ) "Candidate obsolete Item 06 file must be physically deleted: $fileName"
}

Assert-True (-not (Test-Path -LiteralPath $integrityContractPath)) 'Unused Trigger integrity diagnostics contract must be deleted.'
Assert-True (
    ($externalContextText -notmatch 'VisualPoseDiagnostics|IntegrityDiagnostics') -and
    ($equippedGizmoServiceText -notmatch 'VisualPoseDiagnostics|IntegrityDiagnostics|TriggerVisualPoseDiagnosticsAccess') -and
    ($formalSurfacesText -notmatch 'TriggerIntegrityDiagnosticsSurface|ITriggerIntegrityDiagnostics') -and
    ($surfaceAccessText -notmatch 'ResolveIntegrityDiagnostics|IntegrityDiagnosticsSurface') -and
    ($triggerBodyText -notmatch 'TriggerIntegrityDiagnosticsSurface|IntegrityDiagnosticsSurface|integrityDiagnosticsSurface') -and
    ($triggerReadsText -notmatch 'GetHeldChips\(\)|IsSlotContainerConsistent\(') -and
    ($triggerLifecycleText -notmatch 'IntegrityDiagnosticsSurface')
) 'No-caller Trigger integrity diagnostics and pose-window context plumbing must be removed from Core.'

Write-Output 'Item06CombatBodyGizmoAndDiagnosticsCleanupSmokeTests PASS'
