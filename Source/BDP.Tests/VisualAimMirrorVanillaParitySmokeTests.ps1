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
$bdpRoot = Join-Path $repoRoot 'Source\BDP'
$southNorthPath = Join-Path $bdpRoot 'Core\Expressions\Config\ExpressionVisualSouthNorthPoseConfig.cs'
$eastWestPath = Join-Path $bdpRoot 'Core\Expressions\Config\ExpressionVisualEastWestPoseConfig.cs'
$resolverPath = Join-Path $bdpRoot 'Core\Trigger\Visual\VisualPoseResolver.cs'
$snapshotPath = Join-Path $bdpRoot 'Core\Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsSnapshot.cs'
$accessPath = Join-Path $bdpRoot 'Core\Trigger\Visual\Diagnostics\TriggerVisualPoseDiagnosticsAccess.cs'

$southNorthText = Get-Content -LiteralPath $southNorthPath -Raw -Encoding utf8
$eastWestText = Get-Content -LiteralPath $eastWestPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$snapshotText = Get-Content -LiteralPath $snapshotPath -Raw -Encoding utf8
$accessText = Get-Content -LiteralPath $accessPath -Raw -Encoding utf8

Assert-True (
    ($southNorthText -notmatch '\bAimMirror\b') -and
    ($eastWestText -notmatch '\bAimMirror\b')
) '姿态作者配置不得再暴露 AimMirror（瞄准镜像）开关。'

Assert-True (
    ($resolverText -match 'aimAngle > 20f && aimAngle < 160f') -and
    ($resolverText -match 'aimAngle > 200f && aimAngle < 340f') -and
    ($resolverText -match 'meshKind = VisualMeshKind\.PlaneFlipped') -and
    ($resolverText -match 'AimMirror = aimMirror')
) '视觉姿态解析器必须保留原版角度分区与实际镜像结果。'

Assert-True (
    ($snapshotText -match 'public bool AimMirror \{ get; set; \}') -and
    ($snapshotText -notmatch 'SouthNorthAimMirror') -and
    ($snapshotText -notmatch 'EastWestAimMirror') -and
    ($accessText -notmatch 'SouthNorthAimMirror') -and
    ($accessText -notmatch 'EastWestAimMirror')
) '画点共用的只读测量面只保留实际发生的 AimMirror，不得继续暴露无效作者配置。'

Write-Output 'VisualAimMirrorVanillaParitySmokeTests PASS'
