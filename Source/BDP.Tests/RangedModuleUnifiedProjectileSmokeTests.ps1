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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP\Core'

$projectilePath = Join-Path $bdpSourceRoot 'Projectiles\BdpProjectile.cs'
$collisionServicePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Collision\SegmentCollisionService.cs'
$collisionRecordPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Model\SegmentCollisionRecord.cs'
$bulletPath = Join-Path $bdpSourceRoot 'Projectiles\BdpBullet.cs'
$explosiveProjectilePath = Join-Path $bdpSourceRoot 'Projectiles\BdpExplosiveProjectile.cs'
$verbPath = Join-Path $bdpSourceRoot 'Verbs\BdpVerb_Shoot.cs'
$impactStageServicePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactStageService.cs'

$projectileText = if (Test-Path -LiteralPath $projectilePath) { Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8 } else { '' }
$collisionServiceText = if (Test-Path -LiteralPath $collisionServicePath) { Get-Content -LiteralPath $collisionServicePath -Raw -Encoding utf8 } else { '' }
$collisionRecordText = if (Test-Path -LiteralPath $collisionRecordPath) { Get-Content -LiteralPath $collisionRecordPath -Raw -Encoding utf8 } else { '' }
$bulletText = if (Test-Path -LiteralPath $bulletPath) { Get-Content -LiteralPath $bulletPath -Raw -Encoding utf8 } else { '' }
$explosiveProjectileText = if (Test-Path -LiteralPath $explosiveProjectilePath) { Get-Content -LiteralPath $explosiveProjectilePath -Raw -Encoding utf8 } else { '' }
$verbText = Get-Content -LiteralPath $verbPath -Raw -Encoding utf8
$impactStageServiceText = Get-Content -LiteralPath $impactStageServicePath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $projectilePath) 'BdpProjectile.cs must exist.'

Assert-True (
    ($projectileText -match 'class\s+BdpProjectile\s*:\s*Bullet') -and
    ($projectileText -match 'BindLaunchPlan') -and
    ($projectileText -match 'ExecuteImpact')
) 'BdpProjectile must inherit Bullet and consume the neutral launch/impact plan chain.'

Assert-True (
    (Test-Path -LiteralPath $collisionServicePath) -and
    (Test-Path -LiteralPath $collisionRecordPath) -and
    ($projectileText -match 'SegmentCollisionService') -and
    ($collisionServiceText -notmatch 'Tracking') -and
    ($collisionRecordText -notmatch 'Tracking')
) '续段客观阻挡能力必须收敛在统一投射物宿主基础设施内，且不得带入追踪业务命名。'

Assert-True (
    (-not (Test-Path -LiteralPath $bulletPath)) -and
    (-not (Test-Path -LiteralPath $explosiveProjectilePath))
) 'Legacy BdpBullet and BdpExplosiveProjectile hosts must be removed.'

Assert-True (
    ($verbText -match 'projectileThing is BdpProjectile') -and
    ($verbText -notmatch 'BdpBullet') -and
    ($verbText -notmatch 'BdpExplosiveProjectile')
) 'BdpVerb_Shoot must bind only the unified BdpProjectile host.'

Assert-True (
    ($impactStageServiceText -match 'explosionRadius') -and
    ($impactStageServiceText -notmatch 'projectile is Projectile_Explosive')
) 'ImpactStageService baseline must derive area-effect planning from projectile facts, not host type.'

Write-Output 'RangedModuleUnifiedProjectileSmokeTests PASS'
