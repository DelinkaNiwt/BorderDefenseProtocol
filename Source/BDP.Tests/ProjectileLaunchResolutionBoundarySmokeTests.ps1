# 本脚本使用 UTF-8 编码，验证远程投射物首发结果的中性边界。
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

function Read-OptionalSource {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modelRoot = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model'
$projectileRoot = Join-Path $repoRoot 'Source\BDP\Core\Projectiles'
$verbPath = Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs'
$routePath = Join-Path $repoRoot 'Source\BDP.Content\RangedModules\RoutePath\RoutePathModule.cs'
$dispositionText = Read-OptionalSource (Join-Path $modelRoot 'ProjectileLaunchDisposition.cs')
$resolutionText = Read-OptionalSource (Join-Path $modelRoot 'ProjectileLaunchResolution.cs')
$planText = Read-OptionalSource (Join-Path $modelRoot 'ProjectileInitPlan.cs')
$arrivalText = Read-OptionalSource (Join-Path $projectileRoot 'RangedFlightProtocol\Arrival\ArrivalStageContext.cs')
$verbText = Read-OptionalSource $verbPath
$launchServiceText = Read-OptionalSource (Join-Path $projectileRoot 'BdpProjectileLaunchService.cs')
$routeText = Read-OptionalSource $routePath

Assert-True (
    ($dispositionText -match 'enum\s+ProjectileLaunchDisposition') -and
    ($dispositionText -match '\bForcedMiss\b') -and
    ($dispositionText -match '\bWildMiss\b') -and
    ($dispositionText -match '\bCoverIntercept\b') -and
    ($dispositionText -match '\bIntendedTarget\b')
) 'Core must define all launch dispositions without binding the route module to ProjectileHitFlags.'

Assert-True (
    ($resolutionText -match 'class\s+ProjectileLaunchResolution') -and
    ($resolutionText -match 'ProjectileLaunchDisposition') -and
    ($resolutionText -match 'CanHitIntendedTarget') -and
    ($resolutionText -match 'Miss')
) 'Launch resolution must carry the disposition, intended-target permission, and frozen miss information.'

Assert-True (
    ($planText -match 'ProjectileLaunchResolution\s+LaunchResolution') -and
    ($arrivalText -match 'ProjectileLaunchResolution\s+LaunchResolution')
) 'The frozen launch resolution must travel through ProjectileInitPlan and ArrivalStageContext.'

Assert-True (
    ($verbText -match 'ProjectileLaunchDisposition\.ForcedMiss') -and
    ($verbText -match 'ProjectileLaunchDisposition\.WildMiss') -and
    ($launchServiceText -match 'ProjectileLaunchDisposition\.CoverIntercept') -and
    ($verbText -match 'BdpProjectileLaunchService\.FreezeCoverLaunchResolution') -and
    ($verbText -match 'ProjectileLaunchDisposition\.IntendedTarget')
) '远程发射宿主及共用发射服务必须显式记录每项原版发射分支。'

Assert-True (
    ($routeText -notmatch 'ProjectileHitFlags') -and
    ($routeText -notmatch 'ShotReport\.HitReportFor') -and
    ($routeText -match 'LaunchResolution')
) 'RoutePath must consume the neutral launch resolution and must not infer it from raw vanilla flags or rerun ShotReport.'

Write-Output 'ProjectileLaunchResolutionBoundarySmokeTests PASS'
