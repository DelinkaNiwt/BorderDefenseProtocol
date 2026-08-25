$ErrorActionPreference = 'Stop'

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
$snapshotPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsSnapshot.cs'
$accessPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'
$drawerPath = Join-Path $repoRoot 'Source\BDP.Development\Trigger\Diagnostics\TriggerVisualMarkerOverlayDrawer.cs'
$providerPath = Join-Path $repoRoot 'Source\BDP.Development\Trigger\Diagnostics\TriggerVisualMarkerGizmoProvider.cs'
$mapComponentPath = Join-Path $repoRoot 'Source\BDP.Development\Trigger\Diagnostics\MapComponent_TriggerVisualMarkerOverlay.cs'

Assert-True (Test-Path -LiteralPath $snapshotPath) '现有视觉诊断快照必须存在。'
Assert-True (Test-Path -LiteralPath $accessPath) '现有视觉诊断读取入口必须存在。'
Assert-True (Test-Path -LiteralPath $drawerPath) '现有点位绘制器必须存在。'
Assert-True (Test-Path -LiteralPath $providerPath) '现有诊断按钮提供器必须保持存在。'
Assert-True (Test-Path -LiteralPath $mapComponentPath) '现有地图绘制组件必须保持存在。'

$snapshotText = Get-Content -Raw -Encoding utf8 -LiteralPath $snapshotPath
$accessText = Get-Content -Raw -Encoding utf8 -LiteralPath $accessPath
$drawerText = Get-Content -Raw -Encoding utf8 -LiteralPath $drawerPath

Assert-True (
    ($snapshotText -match 'public bool HasGripAnchor') -and
    ($snapshotText -match 'public Vector3 GripWorldPosition') -and
    ($snapshotText -match 'public Vector3 GripLocalOffset')
) '现有单武器诊断快照必须扩充握持锚点字段。'

Assert-True (
    ($accessText -match 'GripWorldPosition') -and
    ($accessText -match 'GripLocalOffset') -and
    ($accessText -match 'resolvedPose\.GripAnchor')
) '现有诊断读取入口必须复制 Core 已解算的握持锚点。'

Assert-True (
    ($drawerText -match 'public static void DrawForPawn') -and
    ($drawerText -match 'MainGripMaterial') -and
    ($drawerText -match 'SubGripMaterial') -and
    ($drawerText -match 'ResolveGripPointMaterial') -and
    ($drawerText -match 'ResolveGripLinkMaterial') -and
    ($drawerText -match 'DrawPoint\(resident\.GripWorldPosition') -and
    ($drawerText -match 'DrawLink\(resident\.ResolvedDrawPosition, resident\.GripWorldPosition')
) '现有点位绘制器必须按主副侧双色绘制握持点和中心连线。'

Write-Output 'TriggerVisualGripMarkerSmokeTests PASS'
