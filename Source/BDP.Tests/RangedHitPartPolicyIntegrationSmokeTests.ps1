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

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$contentRoot = Join-Path $repoRoot 'Source\BDP.Content'
$coreRoot = Join-Path $repoRoot 'Source\BDP\Core'
$moduleText = Get-Content -LiteralPath (Join-Path $contentRoot 'RangedModules\VitalHit\VitalHitModule.cs') -Raw -Encoding utf8
$configText = Get-Content -LiteralPath (Join-Path $contentRoot 'RangedModules\VitalHit\VitalHitConfig.cs') -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath (Join-Path $contentRoot 'ContentBootstrap.cs') -Raw -Encoding utf8
$projectileText = Get-Content -LiteralPath (Join-Path $coreRoot 'Projectiles\BdpProjectile.cs') -Raw -Encoding utf8
$xmlText = Get-Content -LiteralPath (Join-Path $repoRoot '1.6\Content\Defs\RangedModuleDef\VitalHit.xml') -Raw -Encoding utf8

Assert-True (($moduleText -match 'HitPartPolicyResolver\.TryResolve') -and ($moduleText -match 'HitPartOverride')) 'Remote module must use the common resolver and retain the existing impact override channel.'
Assert-True (($moduleText -match 'IsAreaProjectile') -and ($moduleText -match 'return;')) 'Remote vital-hit module must keep area-projectile exclusion.'
Assert-True (($configText -match 'SelectionMode') -and ($configText -match 'PreferVital')) 'Remote config must preserve PreferVital as its default mode.'
Assert-True (($bootstrapText -match 'HitPartPolicyResolverRegistry\.Register') -and ($bootstrapText -match 'HitPartPolicyResolverAdapter')) 'Content bootstrap must register the melee bridge adapter.'
Assert-True (($projectileText -match 'HitPartOverride') -and ($projectileText -match 'ApplyAreaEffect')) 'Projectile execution must retain the existing direct/area split.'
Assert-True (($xmlText -match '<SelectionMode>PreferVital</SelectionMode>') -and ($xmlText -match '<VitalHitChance>1</VitalHitChance>')) 'VitalHit XML must keep the 100% PreferVital default.'

Write-Output 'RangedHitPartPolicyIntegrationSmokeTests PASS'
