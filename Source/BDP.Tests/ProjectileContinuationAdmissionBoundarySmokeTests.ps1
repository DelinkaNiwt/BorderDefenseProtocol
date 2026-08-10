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
$collisionRecordPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Model\SegmentCollisionRecord.cs'
$pathUtilityPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Projection\ProjectileFlightPathUtility.cs'

$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8
$collisionRecordText = Get-Content -LiteralPath $collisionRecordPath -Raw -Encoding utf8
$pathUtilityText = Get-Content -LiteralPath $pathUtilityPath -Raw -Encoding utf8

Assert-True (
    ($collisionRecordText -match 'FirstObjectiveBlockerProgress') -and
    ($collisionRecordText -match 'FirstObjectiveBlockerExactPosition')
) 'SegmentCollisionRecord 必须显式承载首个客观阻挡的大致进入进度与进入位置。'

Assert-True (
    ($pathUtilityText -match 'CreatePrefix') -and
    ($pathUtilityText -match 'ProjectileFlightPathKind\.CubicBezier') -and
    ($pathUtilityText -match 'Vector3\.Lerp')
) 'ProjectileFlightPathUtility 必须提供按进度裁出前缀路径的中性几何能力。'

Assert-True (
    $projectileText -match 'SegmentCollisionRecord\s+\w+\s*=\s*SegmentCollisionService\.ScanSegment\(this,\s*nextFlightPathSnapshot\)[\s\S]*CrossedObjectiveBlocker[\s\S]*FirstObjectiveBlockerProgress[\s\S]*ProjectileFlightPathUtility\.CreatePrefix'
) 'BdpProjectile 在绑定下一段前，必须先根据客观阻挡扫描结果裁短路径，而不是整段放行到墙后。'

Write-Output 'ProjectileContinuationAdmissionBoundarySmokeTests PASS'
