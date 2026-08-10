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
$collisionServicePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs'
$collisionRecordPath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Model\SegmentCollisionRecord.cs'
$projectilePath = Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs'
$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'

$collisionServiceExists = Test-Path -LiteralPath $collisionServicePath
$collisionRecordExists = Test-Path -LiteralPath $collisionRecordPath
$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8
$collisionServiceText = if ($collisionServiceExists) { Get-Content -LiteralPath $collisionServicePath -Raw -Encoding utf8 } else { '' }
$collisionRecordText = if ($collisionRecordExists) { Get-Content -LiteralPath $collisionRecordPath -Raw -Encoding utf8 } else { '' }

Assert-True $collisionServiceExists '续段客观阻挡基础设施必须提供 SegmentCollisionService。'
Assert-True $collisionRecordExists '续段客观阻挡基础设施必须提供 SegmentCollisionRecord。'

Assert-True (
    ($collisionServiceText -match 'class\s+SegmentCollisionService') -and
    ($collisionServiceText -match 'ScanSegment') -and
    ($collisionServiceText -match 'FillCategory\.Full') -and
    ($collisionServiceText -match 'Building_Door')
) 'SegmentCollisionService 必须显式扫描飞行段，并识别满格阻挡物与关闭门体。'

Assert-True (
    ($collisionRecordText -match 'class\s+SegmentCollisionRecord') -and
    ($collisionRecordText -match 'SegmentStart') -and
    ($collisionRecordText -match 'SegmentEnd') -and
    ($collisionRecordText -match 'TraversedCells') -and
    ($collisionRecordText -match 'CrossedObjectiveBlocker') -and
    ($collisionRecordText -match 'FirstObjectiveBlockerCell') -and
    ($collisionRecordText -match 'FirstObjectiveBlockerThing')
) 'SegmentCollisionRecord 必须承载段起终点、穿过格子与首个客观阻挡事实。'

Assert-True (
    ($projectileText -match 'SegmentCollisionService') -and
    ($projectileText -match 'pendingObjectiveBlockerImpactThing') -and
    ($projectileText -match 'pendingObjectiveBlockerImpactCell') -and
    ($projectileText -match 'pendingObjectiveBlockerExactPosition')
) 'BdpProjectile 必须在宿主层持有续段客观阻挡服务与阻挡命中锚点状态。'

Assert-True (
    $projectileText -match 'ArrivalRecord\s+arrival\s*=\s*rangedFlightProtocolService\.ExecuteArrival\(this,\s*launchPlan,\s*currentFlightRecord\)[\s\S]*SegmentCollisionRecord\s+\w+\s*=\s*ResolveCurrentSegmentCollisionRecord\([\s\S]*if\s*\(\s*TryStartObjectiveBlockerImpact\([\s\S]*if\s*\(\s*ShouldContinueFlight\(arrival\)\s*\)'
) 'BdpProjectile.ImpactSomething 必须先做 Arrival，再做续段客观阻挡裁定，最后才决定是否继续飞行。'

Assert-True (
    ($projectileText -match 'ApplyPendingObjectiveBlockerImpactAnchor\(') -and
    ($projectileText -match 'ClearPendingObjectiveBlockerImpactAnchor\(') -and
    ($projectileText -match 'ResolveImpactPosition\(')
) 'BdpProjectile 必须显式锚定阻挡命中的位置，并在 Impact 收束后清理临时状态。'

Assert-True (
    ($verbText -notmatch 'SegmentCollisionService') -and
    ($verbText -notmatch 'ObjectiveBlocker')
) '发射层 BdpVerb_Shoot 不得反向承担续段客观阻挡职责。'

Write-Output 'ProjectileContinuationObjectiveBlockingSmokeTests PASS'
