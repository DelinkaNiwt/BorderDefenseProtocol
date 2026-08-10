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

function Get-ModuleDefBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<BDP\.Core\.AttackExecution\.BdpRangedAttackModuleDef>.*?<defName>$DefName</defName>.*?</BDP\.Core\.AttackExecution\.BdpRangedAttackModuleDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

function Get-ChipBlock {
    param(
        [string]$Text,
        [string]$DefName
    )

    $match = [regex]::Match(
        $Text,
        "(?s)<ThingDef\s+ParentName=""ResourceBase"">.*?<defName>$DefName</defName>.*?</ThingDef>")

    if (-not $match.Success) {
        return $null
    }

    return $match.Value
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$devHarnessRoot = Join-Path (Split-Path -Parent $repoRoot) 'BorderDefenseProtocol.DevHarness'
$samplesRoot = Join-Path $devHarnessRoot 'Source\BDP.DevHarness\RangedModules\Samples'
$moduleDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Pawn\Expressions\Test\RangedAttackModuleDefs_Test.xml'
$chipDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Items\Chips\Test\ThingDefs_TestChips_Combat.xml'
$projectileDefsPath = Join-Path $devHarnessRoot '1.6\Defs\Things\Projectiles\Test\ThingDefs_TestProjectiles.xml'
$moduleSourcePath = Join-Path $samplesRoot 'ExplosiveModule.cs'

$moduleDefsText = Get-Content -LiteralPath $moduleDefsPath -Raw -Encoding utf8
$chipDefsText = Get-Content -LiteralPath $chipDefsPath -Raw -Encoding utf8
$projectileDefsText = Get-Content -LiteralPath $projectileDefsPath -Raw -Encoding utf8
$moduleSourceText = if (Test-Path -LiteralPath $moduleSourcePath) { Get-Content -LiteralPath $moduleSourcePath -Raw -Encoding utf8 } else { '' }

$moduleDefBlock = Get-ModuleDefBlock $moduleDefsText 'BDP_TestRangedExplosiveModule'
$chipBlock = Get-ChipBlock $chipDefsText 'BDP_TestChipExplosiveRanged'
$pathLatchVolleyBlock = Get-ChipBlock $chipDefsText 'BDP_TestChipPathLatchVolley'

Assert-True (
    (Test-Path -LiteralPath $moduleSourcePath) -and
    ($moduleSourceText -match 'class\s+ExplosiveModule') -and
    ($moduleSourceText -match 'IRangedAttackModuleRuntime') -and
    ($moduleSourceText -match 'IPreviewStageModule') -and
    ($moduleSourceText -match 'IImpactStageModule') -and
    ($moduleSourceText -match 'class\s+ExplosiveModuleConfig') -and
    ($moduleSourceText -match 'ExplosionRadius') -and
    ($moduleSourceText -match 'DamageDef') -and
    ($moduleSourceText -match 'DamageAmount') -and
    ($moduleSourceText -match 'ArmorPenetration') -and
    ($moduleSourceText -match 'SuppressBaselineImpact') -and
    ($moduleSourceText -match 'Color') -and
    ($moduleSourceText -notmatch 'PreviewColor') -and
    ($moduleSourceText -notmatch 'ProjectileDef')
) 'DevHarness must provide a dedicated explosive ranged module source with preview, impact, and exactly the agreed config surface.'

Assert-True (
    ($moduleDefBlock -ne $null) -and
    ($moduleDefBlock -match '<runtimeClass>BDP\.DevHarness\.RangedModules\.Samples\.ExplosiveModule</runtimeClass>') -and
    ($moduleDefBlock -match '<defaultConfig Class="BDP\.DevHarness\.RangedModules\.Samples\.ExplosiveModuleConfig">') -and
    ($moduleDefBlock -match '<ExplosionRadius>') -and
    ($moduleDefBlock -match '<DamageDef>') -and
    ($moduleDefBlock -match '<DamageAmount>') -and
    ($moduleDefBlock -match '<ArmorPenetration>') -and
    ($moduleDefBlock -match '<SuppressBaselineImpact>') -and
    ($moduleDefBlock -notmatch 'PreviewColor') -and
    ($moduleDefBlock -notmatch 'ProjectileDef')
) 'DevHarness explosive module Def must expose only the agreed XML config fields.'

Assert-True (
    ($chipBlock -ne $null) -and
    ($chipBlock -match '<defaultProjectile>BDP_TestBulletSemantic</defaultProjectile>') -and
    ($chipBlock -match '<moduleDef>BDP_TestRangedExplosiveModule</moduleDef>')
) 'DevHarness explosive test chip must mount the explosive module while reusing the shared ranged test projectile.'

Assert-True (
    ($pathLatchVolleyBlock -ne $null) -and
    ($pathLatchVolleyBlock -match '<moduleDef>BDP_TestRangedPathLatchModule</moduleDef>') -and
    ($pathLatchVolleyBlock -match '<moduleDef>BDP_TestRangedExplosiveModule</moduleDef>') -and
    ($pathLatchVolleyBlock -match '<ExplosionRadius>1\.5</ExplosionRadius>') -and
    ($pathLatchVolleyBlock -match '<DamageDef>Bomb</DamageDef>') -and
    ($pathLatchVolleyBlock -match '<DamageAmount>0</DamageAmount>') -and
    ($pathLatchVolleyBlock -match '<ArmorPenetration>-1</ArmorPenetration>') -and
    ($pathLatchVolleyBlock -match '<SuppressBaselineImpact>true</SuppressBaselineImpact>') -and
    ($pathLatchVolleyBlock -match '<defaultProjectile>BDP_TestBulletSemantic</defaultProjectile>')
) 'PathLatch volley chip must keep the path module and shared projectile, then mount the explosive module with diameter-3-cell explosion config.'

Assert-True (
    $projectileDefsText -notmatch 'BDP_TestBulletExplosiveModule'
) 'DevHarness must not add a dedicated explosive projectile Def for the explosive ranged module.'

Write-Output 'DevHarnessExplosiveRangedModuleSmokeTests PASS'
