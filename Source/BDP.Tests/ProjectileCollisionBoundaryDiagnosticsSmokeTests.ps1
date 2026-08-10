# 该脚本包含中文字面量，必须以 UTF-8 BOM 保存，避免 Windows PowerShell 误判编码。
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
$projectilePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
$collisionServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs'
$collisionRecordPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Model\SegmentCollisionRecord.cs'
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8
$collisionServiceText = if (Test-Path -LiteralPath $collisionServicePath) { Get-Content -LiteralPath $collisionServicePath -Raw -Encoding utf8 } else { '' }
$collisionRecordText = if (Test-Path -LiteralPath $collisionRecordPath) { Get-Content -LiteralPath $collisionRecordPath -Raw -Encoding utf8 } else { '' }

Assert-True (
    ($projectileText -match 'LogArrivalBoundaryDecision\(arrival,\s*true\)') -and
    ($projectileText -match 'LogArrivalBoundaryDecision\(arrival,\s*false\)')
) 'BdpProjectile must log both continue-flight and vanilla-impact arrival boundaries.'

Assert-True (
    ($projectileText -match 'event=projectile_arrival_boundary') -and
    ($projectileText -match 'vanillaFreeInterceptWouldSkipEndCell=') -and
    ($projectileText -match 'endCellAudit=') -and
    ($projectileText -match 'endCellHasBlockingThing=') -and
    ($projectileText -match 'endCellHasClosedDoor=') -and
    ($projectileText -match 'segmentTraversedCellCount=') -and
    ($projectileText -match 'segmentTraversedCells=') -and
    ($projectileText -match 'segmentCrossedObjectiveBlocker=') -and
    ($projectileText -match 'segmentFirstObjectiveBlockerCell=') -and
    ($projectileText -match 'segmentFirstObjectiveBlockerAudit=') -and
    ($projectileText -match 'segmentCrossedBlockingThing=') -and
    ($projectileText -match 'segmentFirstBlockingCell=') -and
    ($projectileText -match 'segmentFirstBlockingAudit=')
) 'BdpProjectile arrival-boundary diagnostics must expose full segment traversal evidence together with objective-blocker facts.'

Assert-True (
    ($projectileText -match 'event=projectile_real_impact') -and
    ($projectileText -match 'LogImpactResolution\(') -and
    ($projectileText -match 'hitThing=') -and
    ($projectileText -match 'impactSegmentTraversedCellCount=') -and
    ($projectileText -match 'impactSegmentTraversedCells=') -and
    ($projectileText -match 'impactSegmentCrossedObjectiveBlocker=') -and
    ($projectileText -match 'impactSegmentFirstObjectiveBlockerCell=') -and
    ($projectileText -match 'impactSegmentCrossedBlockingThing=') -and
    ($projectileText -match 'impactSegmentFirstBlockingCell=')
) 'BdpProjectile must log the real impact resolution together with impact-segment traversal evidence.'

Assert-True (
    ($projectileText -match 'event=projectile_continue_flight_bound') -and
    ($projectileText -match 'nextSegmentTraversedCellCount=') -and
    ($projectileText -match 'nextSegmentTraversedCells=') -and
    ($projectileText -match 'nextSegmentCrossedObjectiveBlocker=') -and
    ($projectileText -match 'nextSegmentFirstObjectiveBlockerCell=') -and
    ($projectileText -match 'nextSegmentCrossedBlockingThing=') -and
    ($projectileText -match 'nextSegmentFirstBlockingCell=')
) 'BdpProjectile must log continuation-bound traversal evidence for the newly bound segment.'

Assert-True (
    ($collisionServiceText -match 'class\s+SegmentCollisionService') -and
    ($collisionServiceText -match 'ScanSegment') -and
    ($collisionRecordText -match 'class\s+SegmentCollisionRecord') -and
    ($projectileText -match 'SegmentCollisionService') -and
    ($projectileText -match 'BuildSegmentTraversalAudit\(') -and
    ($projectileText -match 'DescribeCellAudit\(') -and
    ($projectileText -match 'ResolveCanHitReason\(')
) '运行态与诊断态必须共享 SegmentCollisionService 的段扫描事实，并保留宿主级 cell audit 与 can-hit reason tracing。'

Write-Output 'ProjectileCollisionBoundaryDiagnosticsSmokeTests PASS'
