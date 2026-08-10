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
$mainSamplesRoot = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\RangedModules\Samples'
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$samplesRoot = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples'
$moduleDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'
$projectileDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Projectiles\Test\ThingDefs_TestProjectiles.xml'
$comboDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Combos\Test\ComboDefs_TestCombos.xml'

$legacyPreviewPath = Join-Path $samplesRoot 'PreviewOnlySampleModule.cs'
$legacyAimPath = Join-Path $samplesRoot 'AimAdjustSampleModule.cs'
$legacyImpactPath = Join-Path $samplesRoot 'ImpactPlanSampleModule.cs'

$moduleDefsText = Get-Content -LiteralPath $moduleDefsPath -Raw -Encoding utf8
$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8
$projectileDefsText = Get-Content -LiteralPath $projectileDefsPath -Raw -Encoding utf8
$comboDefsText = if (Test-Path -LiteralPath $comboDefsPath) { Get-Content -LiteralPath $comboDefsPath -Raw -Encoding utf8 } else { '' }

Assert-True (-not (Test-Path -LiteralPath $mainSamplesRoot)) 'Main mod must not carry concrete sample modules.'

Assert-True (
    (-not (Test-Path -LiteralPath $legacyPreviewPath)) -and
    (-not (Test-Path -LiteralPath $legacyAimPath)) -and
    (-not (Test-Path -LiteralPath $legacyImpactPath))
) 'Legacy split sample modules must be removed after replacing them with the teaching skeleton.'

Assert-True (
    ($moduleDefsText -notmatch 'ExecutionStyle') -and
    ($moduleDefsText -notmatch 'SharedStateKey') -and
    ($moduleDefsText -notmatch 'BDP_TestPreviewOnlySampleModule') -and
    ($moduleDefsText -notmatch 'BDP_TestAimAdjustSampleModule') -and
    ($moduleDefsText -notmatch 'BDP_TestImpactPlanSampleModule')
) 'DevHarness ranged module defs must not restore obsolete authoring fields or retired split samples.'

Assert-True (
    ($chipDefsText -notmatch 'BDP_TestPreviewOnlySampleModule') -and
    ($chipDefsText -notmatch 'BDP_TestAimAdjustSampleModule') -and
    ($chipDefsText -notmatch 'BDP_TestImpactPlanSampleModule')
) 'DevHarness standalone ranged chips must not restore retired split sample modules.'

Assert-True (
    ($projectileDefsText -match 'BDP_TestBulletSemantic') -and
    ($projectileDefsText -match '<thingClass>BDP\.Core\.Projectiles\.BdpProjectile</thingClass>') -and
    ($projectileDefsText -notmatch 'BdpBullet') -and
    ($projectileDefsText -notmatch 'BdpExplosiveProjectile') -and
    ($projectileDefsText -notmatch 'BDP_TestExplosiveProjectileSemantic')
) 'DevHarness projectile defs must use only the unified BdpProjectile host and must not keep legacy explosive projectile samples.'

Assert-True (
    (-not (Test-Path -LiteralPath $comboDefsPath)) -or
    (
        ($comboDefsText -notmatch 'BDP_TestCombo_RangedVolleyExplosive') -and
        ($comboDefsText -notmatch 'BDP_TestExplosiveProjectileSemantic')
    )
) 'DevHarness must not keep combo samples that still depend on the legacy explosive projectile path.'

Write-Output 'RangedModuleSampleModulesSmokeTests PASS'
