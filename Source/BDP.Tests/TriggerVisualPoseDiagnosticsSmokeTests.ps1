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
$diagnosticsAccessPath = Join-Path $bdpCoreRoot 'Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'
$diagnosticsSnapshotPath = Join-Path $bdpCoreRoot 'Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsSnapshot.cs'
$externalContextPath = Join-Path $bdpCoreRoot 'Trigger\External\TriggerExternalGizmoContext.cs'
$gizmoServicePath = Join-Path $bdpCoreRoot 'Trigger\External\TriggerEquippedGizmoService.cs'

Assert-True (Test-Path -LiteralPath $diagnosticsAccessPath) 'Main mod must expose a switch-free visual pose diagnostics access surface.'
Assert-True (Test-Path -LiteralPath $diagnosticsSnapshotPath) 'Main mod must expose public visual pose diagnostics snapshot DTOs.'

$diagnosticsAccessText = Get-Content -LiteralPath $diagnosticsAccessPath -Raw -Encoding utf8
$diagnosticsSnapshotText = Get-Content -LiteralPath $diagnosticsSnapshotPath -Raw -Encoding utf8
$externalContextText = Get-Content -LiteralPath $externalContextPath -Raw -Encoding utf8
$gizmoServiceText = Get-Content -LiteralPath $gizmoServicePath -Raw -Encoding utf8

Assert-True (
    ($diagnosticsAccessText -match 'public static class TriggerVisualPoseDiagnosticsAccess') -and
    ($diagnosticsAccessText -match 'CaptureSnapshot\(Pawn pawn\)') -and
    ($diagnosticsAccessText -match 'VisualPoseResolver') -and
    ($diagnosticsAccessText -match 'ResolvedVisualPose') -and
    ($diagnosticsAccessText -match 'ResolvePresetDefName')
) 'Visual diagnostics access must resolve the same pose path as the draw patch, not duplicate a parallel formula.'

Assert-True (
    ($diagnosticsSnapshotText -match 'public sealed class TriggerVisualPoseDiagnosticsSnapshot') -and
    ($diagnosticsSnapshotText -match 'public sealed class TriggerVisualResidentPoseDiagnosticsSnapshot') -and
    ($diagnosticsSnapshotText -match 'public Vector3 DrawLoc') -and
    ($diagnosticsSnapshotText -match 'public float AimAngle') -and
    ($diagnosticsSnapshotText -match 'public Vector3 ResolvedDrawPosition') -and
    ($diagnosticsSnapshotText -match 'public float ResolvedDrawAngle') -and
    ($diagnosticsSnapshotText -match 'public string MeshKind') -and
    ($diagnosticsSnapshotText -match 'public bool HandMirror') -and
    ($diagnosticsSnapshotText -match 'public bool SouthNorthMirrorOnNorth') -and
    ($diagnosticsSnapshotText -match 'public float SideBaseZ') -and
    ($diagnosticsSnapshotText -match 'public string WeaponActionStage') -and
    ($diagnosticsSnapshotText -match 'public bool WeaponStageVisible')
) 'Visual diagnostics snapshots must expose facing, aim angle, drawLoc, resolved position, final angle, mesh, and mirror flags.'

Assert-True (
    $diagnosticsAccessText -match 'snapshot\.SideBaseZ = eastWestPose\.SideBaseZ;'
) 'Visual diagnostics must expose the East/West shared screen-height baseline from the resolved preset.'

Assert-True (
    ($externalContextText -notmatch 'TriggerVisualPoseDiagnosticsSnapshot|VisualPoseDiagnostics') -and
    ($gizmoServiceText -notmatch 'TriggerVisualPoseDiagnosticsAccess|VisualPoseDiagnostics')
) 'Removed pose-window plumbing must not remain in the external gizmo context.'

Write-Output 'TriggerVisualPoseDiagnosticsSmokeTests PASS'
