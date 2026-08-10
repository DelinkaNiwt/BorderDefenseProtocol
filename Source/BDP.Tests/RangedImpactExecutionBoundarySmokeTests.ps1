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
$impactPlanPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Model\ImpactPlan.cs'
$impactServicePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactStageService.cs'
$impactContributionPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Impact\ImpactContribution.cs'

$projectileText = Get-Content -LiteralPath $projectilePath -Raw -Encoding utf8
$impactPlanText = Get-Content -LiteralPath $impactPlanPath -Raw -Encoding utf8
$impactServiceText = Get-Content -LiteralPath $impactServicePath -Raw -Encoding utf8
$impactContributionText = Get-Content -LiteralPath $impactContributionPath -Raw -Encoding utf8

Assert-True (
    ($impactPlanText -match 'SuppressBaselineImpact') -and
    ($impactPlanText -notmatch 'SuppressVanillaImpact')
) 'ImpactPlan must describe baseline suppression explicitly.'

Assert-True (
    ($impactContributionText -match 'SuppressBaselineImpact') -and
    ($impactContributionText -notmatch 'SuppressVanillaImpact')
) 'ImpactContribution must describe baseline suppression explicitly.'

Assert-True (
    ($impactServiceText -match 'SuppressBaselineImpact') -and
    ($impactServiceText -notmatch 'SuppressVanillaImpact')
) 'ImpactStageService must assemble baseline suppression without legacy naming.'

Assert-True (
    ($projectileText -match 'if \(impactPlan == null\)') -and
    ($projectileText -notmatch 'impactPlan == null \|\| impactPlan\.Suppress') -and
    ($projectileText -notmatch 'SuppressVanillaImpact')
) 'BdpProjectile must not early-return the whole impact execution when baseline impact is suppressed.'

Assert-True (
    ($projectileText -match 'ApplyDirectDamage') -and
    ($projectileText -match 'ApplyAreaEffect')
) 'BdpProjectile must still execute neutral impact plans after baseline suppression.'

Write-Output 'RangedImpactExecutionBoundarySmokeTests PASS'
