# 验证随机未命中可以保留路线形状，但不会重新吸附到原定目标。
$ErrorActionPreference = "Stop"

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
$routeRoot = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\RoutePath'
$resolverText = Read-Source (Join-Path $routeRoot 'RouteSegmentResolver.cs')
$stateText = Read-Source (Join-Path $routeRoot 'RoutePathState.cs')
$moduleText = Read-Source (Join-Path $routeRoot 'RoutePathModule.cs')
$resolutionText = Read-Source (Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\ProjectileLaunchResolution.cs')

Assert-True (
    ($resolutionText -match 'WildMissOffset') -and
    ($moduleText -match 'WildMissOffsetRoute')
) 'WildMiss must carry a frozen offset route mode instead of rerolling at arrival.'

Assert-True (
    ($stateText -match 'WildMissOffset') -and
    ($stateText -match 'HasWildMissOffset') -and
    ($stateText -match 'ExposeData')
) 'RoutePathContext must persist the frozen wild-miss offset.'

Assert-True (
    ($resolverText -match 'WildMissOffset') -and
    ($resolverText -match 'nominalDestination') -and
    ($resolverText -match 'nominalDestination\s*\+\s*sampledOffset') -and
    ($resolverText -notmatch 'TryResolveSafeSpreadDestination|GenSight|CanBeSeenOverFast')
) '随机偏射应保留名义路线位移与额外散布，不能感知墙体回收偏移。'

Assert-True (
    ($resolverText -match 'CreateLinear') -and
    ($resolverText -notmatch 'IsWithinArrivalTolerance|currentNominalDestination') -and
    ($moduleText -match 'WildMissOffsetRoute')
) '随机偏射续段不能通过回接名义锚点恢复为意图命中。'

Write-Output 'WildMissOffsetRouteSmokeTests PASS'
