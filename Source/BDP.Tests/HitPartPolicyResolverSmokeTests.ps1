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

$contentRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'BDP.Content'
$resolverDir = Join-Path $contentRoot 'HitPartPolicy'
$resolverPath = Join-Path $resolverDir 'HitPartPolicyResolver.cs'
$candidatePath = Join-Path $resolverDir 'HitPartCandidate.cs'
$adapterPath = Join-Path $resolverDir 'HitPartPolicyResolverAdapter.cs'
$modulePath = Join-Path $contentRoot 'RangedModules\VitalHit\VitalHitModule.cs'
$configPath = Join-Path $contentRoot 'RangedModules\VitalHit\VitalHitConfig.cs'

foreach ($path in @($resolverPath, $candidatePath, $adapterPath)) {
    Assert-True (Test-Path -LiteralPath $path) "Common hit-part resolver file is missing: $path"
}

$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$moduleText = Get-Content -LiteralPath $modulePath -Raw -Encoding utf8
$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8

Assert-True (($resolverText -match 'PreferVital') -and ($resolverText -match 'AvoidVital') -and ($resolverText -notmatch 'Preserve')) 'Common resolver must expose only the two explicit selection modes.'
Assert-True (($resolverText -match 'PartIsMissing') -and ($resolverText -match 'destroyableByDamage') -and ($resolverText -match 'GetPartHealth')) 'Common resolver must filter unavailable body parts.'
Assert-True (($resolverText -match 'BreathingSource') -and ($resolverText -match 'MetabolismSource') -and ($resolverText -match 'IsFlesh')) 'Common resolver must gate flesh-only vital sources.'
Assert-True (($resolverText -match 'corePart') -and ($resolverText -match 'Fallback')) 'Common resolver must provide a core-part fallback.'
Assert-True ($resolverText -match 'AvoidVital[\s\S]{0,500}(null|false)') 'AvoidVital must return no override when no non-vital candidate exists.'
Assert-True (($adapterPath -and (Get-Content -LiteralPath $adapterPath -Raw -Encoding utf8) -match 'IHitPartPolicyResolver') -and ((Get-Content -LiteralPath $adapterPath -Raw -Encoding utf8) -match 'HitPartPolicyResolver')) 'Content must adapt the common resolver to Core.'
Assert-True (($moduleText -match 'HitPartPolicyResolver') -and ($moduleText -notmatch 'private static BodyPartRecord ResolveVitalPart')) 'VitalHitModule must delegate candidate resolution instead of keeping a private resolver.'
Assert-True (($configText -match 'SelectionMode') -and ($configText -match 'VitalHitChance')) 'Remote vital-hit config must expose mode and preserve the existing chance field.'

Write-Output 'HitPartPolicyResolverSmokeTests PASS'
