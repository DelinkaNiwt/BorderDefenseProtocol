# 验证续段准入不会把已经进入的起点阻挡格再次裁成零长度路径。
$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Read-Source {
    param([string]$Path)
    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$projectileText = Read-Source (Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs')
$collisionText = Read-Source (Join-Path $repoRoot 'Source\BDP\Core\Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs')

$continuationMatch = [regex]::Match(
    $projectileText,
    'private ProjectileFlightPathSnapshot ResolveContinuationBindingFlightPathSnapshot[\s\S]*?private static Vector3 ResolveContinuationObjectiveBlockerTerminalPosition')
$continuationText = $continuationMatch.Value

Assert-True (
    $collisionText -match 'ignoreInitialCellForObjectiveBlocker' -and
    $collisionText -match 'TryPopulateFirstObjectiveBlockerFacts[\s\S]*ignoreInitialCellForObjectiveBlocker[\s\S]*traversedCells'
) '碰撞扫描服务必须提供仅影响客观阻挡事实的首个起点格忽略选项。'

Assert-True (
    $continuationText -match 'IsContinuationOriginObjectiveBlocker' -and
    $continuationText -match 'FirstObjectiveBlockerProgress[\s\S]*nextOrigin\.ToIntVec3\(\)' -and
    $continuationText -match 'ScanSegment\(\s*this,\s*nextFlightPathSnapshot,\s*true\)'
) '宿主只能在续段首个阻挡位于当前起点格且进度为零时重扫，并不得改变其它续段扫描。'

Assert-True (
    $continuationText -match 'blockerProgress\s*>\s*0\.0001f[\s\S]*CreatePrefix'
) '续段只有在阻挡确实位于前方时才允许裁剪路径，不能把零进度阻挡直接裁成起点到起点。'

Assert-True (
    $projectileText -match 'ShouldBypassObjectiveBlockers' -and
    $projectileText -match 'flyOverhead' -and
    $continuationText -match 'ShouldBypassObjectiveBlockers'
) '当前段与续段必须共享越顶飞行和客观阻挡放行判断。'

Write-Output 'ProjectileContinuationOriginCellAdmissionSmokeTests PASS'
