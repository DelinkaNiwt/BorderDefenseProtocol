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
$collisionServiceText = Get-Content -LiteralPath $collisionServicePath -Raw -Encoding utf8
$collisionRecordText = Get-Content -LiteralPath $collisionRecordPath -Raw -Encoding utf8

Assert-True (
    ($collisionServiceText -match 'ProjectileFlightPathSnapshot') -and
    ($collisionServiceText -match 'ProjectileFlightPathUtility\.EvaluatePosition') -and
    (
        ($collisionServiceText -match 'ProjectileFlightPathKind\.CubicBezier') -or
        ($collisionServiceText -match 'snapshot\.Kind')
    )
) '曲线续段客观阻挡扫描不能只看起点终点直线，必须显式基于 ProjectileFlightPathSnapshot 与真实路径采样。'

Assert-True (
    ($collisionRecordText -match 'PathKind') -and
    ($collisionRecordText -match 'SamplePointCount')
) 'SegmentCollisionRecord 必须回传本次扫描使用的路径类型与采样点数量，便于日志证据化。'

Assert-True (
    ($projectileText -match 'SegmentCollisionService\.ScanSegment\(this,\s*currentFlightPathSnapshot\)') -or
    ($projectileText -match 'SegmentCollisionService\.ScanSegment\(this,\s*flightPathSnapshot\)')
) 'BdpProjectile 必须把当前 flight path snapshot 原样交给客观阻挡扫描服务，而不是只传起点终点。'

Assert-True (
    ($projectileText -match 'segmentPathKind=') -and
    ($projectileText -match 'segmentSamplePointCount=') -and
    ($projectileText -match 'nextSegmentPathKind=') -and
    ($projectileText -match 'impactSegmentPathKind=')
) '宿主诊断日志必须显式暴露续段扫描使用的路径类型与采样点数量。'

Write-Output 'ProjectileCurvedContinuationObjectiveBlockingSmokeTests PASS'
