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
$bdpCoreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$bdpRoot = Join-Path $repoRoot 'Source\BDP'
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$developmentRoot = Join-Path $repoRoot 'Source\BDP.Development'
$contentDiagnosticsRoot = Join-Path $contentRoot 'Trigger\Diagnostics'
$developmentDiagnosticsRoot = Join-Path $developmentRoot 'Trigger\Diagnostics'

$diagnosticsSnapshotPath = Join-Path $bdpCoreRoot 'Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsSnapshot.cs'
$diagnosticsAccessPath = Join-Path $bdpCoreRoot 'Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'
$emissionDiagnosticsPath = Join-Path $bdpCoreRoot 'Trigger\Visual\Diagnostics\TriggerVisualEmissionDiagnosticsAccess.cs'
$verbShootPath = Join-Path $bdpCoreRoot 'Verbs\BdpVerb_Shoot.cs'
$contentProjectPath = Join-Path $contentRoot 'BDP.Content.csproj'
$contentBootstrapPath = Join-Path $contentRoot 'ContentBootstrap.cs'
$developmentBootstrapPath = Join-Path $developmentRoot 'DevelopmentBootstrap.cs'
$markerProviderPath = Join-Path $developmentDiagnosticsRoot 'TriggerVisualMarkerGizmoProvider.cs'
$markerSettingsPath = Join-Path $developmentDiagnosticsRoot 'TriggerVisualMarkerSettings.cs'
$markerDrawerPath = Join-Path $developmentDiagnosticsRoot 'TriggerVisualMarkerOverlayDrawer.cs'
$markerMapComponentPath = Join-Path $developmentDiagnosticsRoot 'MapComponent_TriggerVisualMarkerOverlay.cs'
$formalCommandsPath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Commands.xml'
$developmentCommandsPath = Join-Path $repoRoot '1.6\Development\Languages\ChineseSimplified (简体中文)\Keyed\Commands.xml'

Assert-True (Test-Path -LiteralPath $diagnosticsSnapshotPath) 'Visual pose diagnostics snapshot DTO must exist.'
Assert-True (Test-Path -LiteralPath $diagnosticsAccessPath) 'Visual pose diagnostics access surface must exist.'
Assert-True (Test-Path -LiteralPath $verbShootPath) 'BdpVerb_Shoot must exist to publish launch-origin diagnostics.'
Assert-True (Test-Path -LiteralPath $contentProjectPath) 'Content project file must exist.'
Assert-True (Test-Path -LiteralPath $contentBootstrapPath) 'Content bootstrap must exist.'
Assert-True (Test-Path -LiteralPath $developmentBootstrapPath) 'Development bootstrap must exist.'
Assert-True (Test-Path -LiteralPath $markerProviderPath) 'Development draw-point gizmo provider must exist.'
Assert-True (-not (Test-Path -LiteralPath $contentDiagnosticsRoot)) 'Content must not retain pure development marker diagnostics.'

$diagnosticsSnapshotText = Get-Content -LiteralPath $diagnosticsSnapshotPath -Raw -Encoding utf8
$diagnosticsAccessText = Get-Content -LiteralPath $diagnosticsAccessPath -Raw -Encoding utf8
$verbShootText = Get-Content -LiteralPath $verbShootPath -Raw -Encoding utf8
$contentProjectText = Get-Content -LiteralPath $contentProjectPath -Raw -Encoding utf8
$contentBootstrapText = Get-Content -LiteralPath $contentBootstrapPath -Raw -Encoding utf8
$developmentBootstrapText = Get-Content -LiteralPath $developmentBootstrapPath -Raw -Encoding utf8
$markerProviderText = Get-Content -LiteralPath $markerProviderPath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $emissionDiagnosticsPath) 'Main mod must provide a dedicated launch-origin diagnostics access surface.'
Assert-True (Test-Path -LiteralPath $markerSettingsPath) 'Development must provide a marker overlay toggle state.'
Assert-True (Test-Path -LiteralPath $markerDrawerPath) 'Development must provide a map marker overlay drawer.'
Assert-True (Test-Path -LiteralPath $markerMapComponentPath) 'Development must provide a dedicated map component to render marker overlays for all pawns.'
Assert-True (Test-Path -LiteralPath $developmentCommandsPath) 'Development language pack must provide marker command text.'

$emissionDiagnosticsText = Get-Content -LiteralPath $emissionDiagnosticsPath -Raw -Encoding utf8
$markerSettingsText = Get-Content -LiteralPath $markerSettingsPath -Raw -Encoding utf8
$markerDrawerText = Get-Content -LiteralPath $markerDrawerPath -Raw -Encoding utf8
$markerMapComponentText = Get-Content -LiteralPath $markerMapComponentPath -Raw -Encoding utf8

Assert-True (
    ($diagnosticsSnapshotText -match 'public Vector3 PawnDrawPosition') -and
    ($diagnosticsSnapshotText -match 'public List<TriggerVisualEmissionLaunchPointSnapshot> RecentLaunchPoints') -and
    ($diagnosticsSnapshotText -match 'public bool HasRecentLaunchOrigin') -and
    ($diagnosticsSnapshotText -match 'public Vector3 RecentLaunchOriginWorld') -and
    ($diagnosticsSnapshotText -match 'public int RecentLaunchTick')
) 'Diagnostics snapshot must expose Pawn.DrawPos and the complete recent launch point batch for marker overlays.'

Assert-True (
    ($diagnosticsAccessText -match 'PawnDrawPosition = pawn\.DrawPos') -and
    ($diagnosticsAccessText -match 'TriggerVisualEmissionDiagnosticsAccess\.CaptureSnapshot\(pawn\)')
) 'Diagnostics access must surface Pawn.DrawPos and bridge the latest launch-origin snapshot into the read-only DTO.'

Assert-True (
    ($emissionDiagnosticsText -match 'public static class TriggerVisualEmissionDiagnosticsAccess') -and
    ($emissionDiagnosticsText -match 'CaptureSnapshot\(Pawn pawn\)') -and
    ($emissionDiagnosticsText -match 'BeginBurstBatch') -and
    ($emissionDiagnosticsText -match 'RecordLaunchOrigin') -and
    ($emissionDiagnosticsText -match 'LaunchPoints') -and
    ($emissionDiagnosticsText -match 'RootOriginWorld') -and
    ($emissionDiagnosticsText -match 'RootOriginSourceKind') -and
    ($emissionDiagnosticsText -match 'TheoreticalCenterOriginWorld') -and
    ($emissionDiagnosticsText -match 'ActualLaunchOriginWorld') -and
    ($emissionDiagnosticsText -match 'BatchesByPawnId')
) 'Launch-origin diagnostics access must expose burst-batch capture, all actual points, and theoretical centers.'

Assert-True (
    ($emissionDiagnosticsText -match 'public int LaunchTick \{ get; set; \}') -and
    ($emissionDiagnosticsText -match 'Find\.TickManager\.TicksGame - point\.LaunchTick > RetainTicks') -and
    ($emissionDiagnosticsText -notmatch 'batch\.LaunchTick != launchTick')
) 'Launch-origin diagnostics must expire points individually by their own LaunchTick instead of replacing the whole batch every shot.'

Assert-True (
    ($verbShootText -match 'TriggerVisualEmissionDiagnosticsAccess\.RecordLaunchOrigin') -and
    ($verbShootText -match 'TriggerVisualEmissionDiagnosticsAccess\.BeginBurstBatch') -and
    ($verbShootText -match 'theoreticalOrigin') -and
    ($verbShootText -match 'launchOrigin') -and
    ($verbShootText -match 'ResolveRandomOriginSpreadOffset\(theoreticalOrigin')
) 'BdpVerb_Shoot must begin a fresh diagnostics batch per burst and publish every projectile theoretical center plus final actual launchOrigin.'

Assert-True (
    ($contentProjectText -match '<Reference Include="0Harmony">') -and
    ($contentBootstrapText -match 'new Harmony\("niwt\.bdp\.content"\)\.PatchAll\(\)') -and
    ($contentBootstrapText -notmatch 'TriggerVisualMarkerGizmoProvider') -and
    ($developmentBootstrapText -match 'TriggerExternalGizmoRegistry\.Register\(new TriggerVisualMarkerGizmoProvider\(\)\)')
) 'Development must register the retained draw-point diagnostics during bootstrap.'

$formalCommandsText = Get-Content -LiteralPath $formalCommandsPath -Raw -Encoding utf8
$developmentCommandsText = Get-Content -LiteralPath $developmentCommandsPath -Raw -Encoding utf8
Assert-True ($formalCommandsText -notmatch 'BDP_Command_TriggerDiagnostics') 'Formal language pack must not retain development marker text.'
Assert-True ($developmentCommandsText -match 'BDP_Command_TriggerDiagnostics_DrawMarkers') 'Development language pack must contain marker text.'

Assert-True (
    ($markerSettingsText -match 'public static class TriggerVisualMarkerSettings') -and
    ($markerSettingsText -match 'public static bool OverlayEnabled')
) 'Content marker settings must expose a shared overlay toggle.'

Assert-True (
    ($markerMapComponentText -match 'class MapComponent_TriggerVisualMarkerOverlay') -and
    ($markerMapComponentText -match 'override void MapComponentDraw\(\)') -and
    ($markerMapComponentText -match 'AllPawnsSpawned') -and
    ($markerMapComponentText -match 'TriggerVisualMarkerOverlayDrawer\.DrawForPawn') -and
    ($markerMapComponentText -notmatch 'IsSelected')
) 'Content must render marker overlays from a map-level draw hook so points stay visible even when the pawn is not selected.'

Assert-True (
    ($markerDrawerText -match 'PawnDrawPosition') -and
    ($markerDrawerText -match 'DrawLoc') -and
    ($markerDrawerText -match 'ResolvedDrawPosition') -and
    ($markerDrawerText -match 'MuzzleWorldPosition') -and
    ($markerDrawerText -match 'RecentLaunchPoints') -and
    ($markerDrawerText -match 'MainCenterMaterial') -and
    ($markerDrawerText -match 'SubCenterMaterial') -and
    ($markerDrawerText -match 'MainLaunchMaterial') -and
    ($markerDrawerText -match 'SubLaunchMaterial') -and
    ($markerDrawerText -match 'TheoreticalCenterOriginWorld') -and
    ($markerDrawerText -match 'ActualLaunchOriginWorld') -and
    ($markerDrawerText -match 'GenDraw\.DrawLineBetween')
) 'Marker overlay drawer must visualize Pawn, DrawLoc, resolved weapon, muzzle, per-side centers, and all actual launch points.'

Assert-True (
    ($markerDrawerText -match 'Graphics\.DrawMesh') -and
    ($markerDrawerText -match 'CreateMarkerTexture') -and
    ($markerDrawerText -match 'CreatePointMaterial') -and
    ($markerDrawerText -match 'PointDiameterScale') -and
    ($markerDrawerText -match 'diameter \* PointDiameterScale') -and
    ($markerDrawerText -match 'DrawPoint\(point\.ActualLaunchOriginWorld') -and
    ($markerDrawerText -notmatch 'diagA') -and
    ($markerDrawerText -notmatch 'diagB')
) 'Marker overlay drawer must render semi-transparent round dots with a shared downscale factor so overlapping actual launch points remain readable and more precise.'

Assert-True (
    ($markerProviderText -match 'Command_Toggle') -and
    ($markerProviderText -match 'TriggerVisualMarkerSettings\.OverlayEnabled') -and
    ($markerProviderText -match 'DebugSettings\.godMode') -and
    ($markerProviderText -match 'toggleAction')
) 'Content gizmo provider must offer one god-mode-only marker overlay toggle.'

Write-Output 'TriggerVisualMarkerOverlaySmokeTests PASS'
