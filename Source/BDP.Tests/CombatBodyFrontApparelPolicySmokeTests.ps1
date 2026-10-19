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
$coreBridgePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\Bridge\PawnCombatBodyBridge.cs'
$coreRuntimePath = Join-Path $sourceRoot 'BDP\Core\CombatBody\External\CombatBodyFrontApparelRuntime.cs'
$coreRegistryPath = Join-Path $sourceRoot 'BDP\Core\CombatBody\External\CombatBodyFrontApparelRegistry.cs'
$contentPolicyPath = Join-Path $sourceRoot 'BDP.Content\CombatBody\Apparel\CombatBodyFrontApparelPolicy.cs'
$contentDamagePatchPath = Join-Path $sourceRoot 'BDP.Content\Patches\Patch_Thing_TakeDamage_CombatBodyApparel.cs'
$contentTooltipPatchPath = Join-Path $sourceRoot 'BDP.Content\Patches\Patch_ThingWithComps_GetTooltip_CombatBodyApparel.cs'
$contentLabelPatchPath = Join-Path $sourceRoot 'BDP.Content\Patches\Patch_Thing_GetCustomLabelNoCount_CombatBodyApparel.cs'
$contentBootstrapPath = Join-Path $sourceRoot 'BDP.Content\ContentBootstrap.cs'

foreach ($path in @(
        $coreBridgePath,
        $coreRuntimePath,
        $coreRegistryPath,
        $contentPolicyPath,
        $contentDamagePatchPath,
        $contentTooltipPatchPath,
        $contentLabelPatchPath)) {
    Assert-True -Condition (Test-Path -LiteralPath $path) -Message ("Required source file is missing: " + $path)
}

$bridgeText = Get-Content -LiteralPath $coreBridgePath -Raw -Encoding utf8
$runtimeText = Get-Content -LiteralPath $coreRuntimePath -Raw -Encoding utf8
$registryText = Get-Content -LiteralPath $coreRegistryPath -Raw -Encoding utf8
$policyText = Get-Content -LiteralPath $contentPolicyPath -Raw -Encoding utf8
$damagePatchText = Get-Content -LiteralPath $contentDamagePatchPath -Raw -Encoding utf8
$tooltipPatchText = Get-Content -LiteralPath $contentTooltipPatchPath -Raw -Encoding utf8
$labelPatchText = Get-Content -LiteralPath $contentLabelPatchPath -Raw -Encoding utf8
$bootstrapText = Get-Content -LiteralPath $contentBootstrapPath -Raw -Encoding utf8

Assert-True -Condition ($bridgeText -match 'CombatBodyFrontApparelRegistry\.Prepare\(apparel\)') -Message 'Both front apparel branches must enter one common apparel preparation boundary.'
Assert-True -Condition ($bridgeText -match 'CombatBodyFrontApparelRuntime\.Register\(apparel\)') -Message 'Successfully worn front apparel must be registered as active combat-body apparel.'
Assert-True -Condition ($bridgeText -match 'CombatBodyFrontApparelRuntime\.Unregister\(apparel\)') -Message 'Restored front apparel must leave the active combat-body apparel registry.'
Assert-True -Condition ($bridgeText -match 'CombatBodyFrontApparelRuntime\.Rebuild') -Message 'Post-load reconciliation must rebuild active combat-body apparel identity.'
Assert-True -Condition ($runtimeText -match 'public static bool IsActive\(Thing thing\)') -Message 'Core must expose an instance-identity query for runtime policy patches.'
Assert-True -Condition ($registryText -match 'public static void Prepare\(Apparel apparel\)') -Message 'Core must expose a neutral common preparation registry.'
Assert-True -Condition ($policyText -match 'RemoveQualityComp') -Message 'Content policy must remove quality from every prepared combat-body apparel instance.'
Assert-True -Condition ($policyText -match 'HitPoints\s*=\s*apparel\.MaxHitPoints') -Message 'Content policy must normalize prepared combat-body apparel durability.'
Assert-True -Condition ($damagePatchText -match 'Thing\.TakeDamage') -Message 'Content must intercept vanilla apparel durability damage at Thing.TakeDamage.'
Assert-True -Condition ($damagePatchText -match 'CombatBodyFrontApparelRuntime\.IsActive') -Message 'Durability interception must be restricted to registered combat-body apparel.'
Assert-True -Condition ($tooltipPatchText -match 'ThingWithComps.*GetTooltip|GetTooltip') -Message 'Content must handle durability suppression in ThingWithComps.GetTooltip.'
Assert-True -Condition ($labelPatchText -match 'GetCustomLabelNoCount') -Message 'Content must handle durability suppression in Thing.GetCustomLabelNoCount.'
Assert-True -Condition ($bootstrapText -match 'CombatBodyFrontApparelRegistry\.Register') -Message 'Content bootstrap must register the formal combat-body apparel policy.'

$switchIndex = $bridgeText.IndexOf('switch (config.frontMode)', [System.StringComparison]::Ordinal)
$prepareIndex = $bridgeText.IndexOf('CombatBodyFrontApparelRegistry.Prepare(apparel)', [System.StringComparison]::Ordinal)
$wearIndex = $bridgeText.IndexOf('Pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: true)', [System.StringComparison]::Ordinal)
Assert-True -Condition ($switchIndex -ge 0 -and $prepareIndex -gt $switchIndex -and $prepareIndex -lt $wearIndex) -Message 'Common apparel preparation must run after either branch and before common locked wear.'

Write-Output 'CombatBodyFrontApparelPolicy PASS'
