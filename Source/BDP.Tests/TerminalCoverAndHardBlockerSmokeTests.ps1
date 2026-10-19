# 验证终端掩体与路线中间硬阻挡不再被混成一种判定。
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
$verbText = Read-Source (Join-Path $repoRoot 'Source\BDP\Core\Verbs\BdpVerb_Shoot.cs')
$projectileText = Read-Source (Join-Path $repoRoot 'Source\BDP\Core\Projectiles\BdpProjectile.cs')
$coverText = Read-Source (Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedProtocol\Model\TrajectoryCoverSnapshot.cs')
$resolverText = Read-Source (Join-Path $repoRoot 'Source\BDP.Content\RangedModules\RoutePath\RouteSegmentResolver.cs')
$autoText = Read-Source (Join-Path $repoRoot 'Source\BDP.Content\RangedModules\RoutePath\RouteAutoResolver.cs')

Assert-True (
    ($verbText -match 'ResolveCoverOriginCell') -and
    ($verbText -match 'resultingLine\.Source') -and
    ($coverText -match 'CalculateCoverGiverSet')
) '无锚点直射必须以实际发射源作为最终段起点，并继续使用原版掩体算法。'

Assert-True (
    ($verbText -match 'TrajectoryCover\.OverallBlockChance') -and
    ($verbText -match 'TrajectoryCover\.RandomCover')
) '终端掩体概率和随机拦截物必须来自同一份最终路径掩体快照。'

Assert-True (
    ($projectileText -match 'IsPassedTerminalCoverBlocker') -and
    ($projectileText -match 'Disposition\s*!==?\s*ProjectileLaunchDisposition\.IntendedTarget') -or
    ($projectileText -match 'Disposition\s*!=\s*ProjectileLaunchDisposition\.IntendedTarget')
) '目标贴墙/山体的终端掩体必须有明确的“原版已通过掩体”例外，而不是全局忽略阻挡。'

Assert-True (
    ($projectileText -match 'SegmentCollisionService\.ScanSegment') -and
    ($projectileText -match 'CreatePrefix')
) '路线中间硬阻挡仍必须先扫描并裁短真实路径。'

Assert-True (
    ($autoText -match 'LineOfSightToEdges') -and
    ($autoText -match 'HasOriginalTerminalVisibility')
) '自动路线必须保留原版终端边缘可见性，避免把可互射的最外格强制绕行。'

Assert-True (
    ($resolverText -notmatch 'caster\.Position') -and
    ($verbText -notmatch 'CalculateCoverGiverSet\(target,\s*caster\.Position')
) '最终掩体判定不得重新引入射击者语义。'

Write-Output 'TerminalCoverAndHardBlockerSmokeTests PASS'
