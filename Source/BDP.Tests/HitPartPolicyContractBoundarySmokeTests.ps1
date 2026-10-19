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
$coreRoot = Join-Path $sourceRoot 'BDP\Core'

$configPath = Join-Path $coreRoot 'AttackExecution\HitPart\HitPartPolicyConfig.cs'
$resolverPath = Join-Path $coreRoot 'AttackExecution\HitPart\IHitPartPolicyResolver.cs'
$registryPath = Join-Path $coreRoot 'AttackExecution\HitPart\HitPartPolicyResolverRegistry.cs'
$accessPath = Join-Path $coreRoot 'AttackExecution\HitPart\HitPartPolicyResolverAccess.cs'
$entryPath = Join-Path $coreRoot 'Expressions\Config\ChipExpressionEntryConfig.cs'
$contractPath = Join-Path $coreRoot 'Expressions\Contract\ChipExpressionEntryContract.cs'
$declarationPath = Join-Path $coreRoot 'Expressions\Model\ExpressionSourceDeclaration.cs'
$materialPath = Join-Path $coreRoot 'Expressions\Model\ExpressionSourceMaterial.cs'
$resultPath = Join-Path $coreRoot 'Expressions\Model\FormalExpressionResult.cs'

Assert-True (Test-Path -LiteralPath $configPath) 'Core must define the neutral hit-part policy configuration.'
Assert-True (Test-Path -LiteralPath $resolverPath) 'Core must define the neutral hit-part resolver contract.'
Assert-True (Test-Path -LiteralPath $registryPath) 'Core must expose a resolver registration bridge.'
Assert-True (Test-Path -LiteralPath $accessPath) 'Core must expose a safe resolver access boundary.'

$configText = Get-Content -LiteralPath $configPath -Raw -Encoding utf8
$resolverText = Get-Content -LiteralPath $resolverPath -Raw -Encoding utf8
$registryText = Get-Content -LiteralPath $registryPath -Raw -Encoding utf8
$accessText = Get-Content -LiteralPath $accessPath -Raw -Encoding utf8

Assert-True (($configText -match 'PolicyKey') -and ($configText -match 'Chance') -and ($configText -notmatch 'FallbackToVanilla')) 'Policy config must contain only the required neutral routing fields.'
Assert-True ($configText -notmatch 'Vital|Organ|Heart|Brain') 'Core policy config must not encode vital or organ semantics.'
Assert-True (($resolverText -match 'BodyPartRecord') -and ($resolverText -match 'DamageDef') -and ($resolverText -match 'BodyPartGroupDef')) 'Resolver contract must return a body part and receive vanilla damage context.'
Assert-True (($registryText -match 'Register') -and ($registryText -match 'Unregister|Clear')) 'Registry must support lifecycle-safe provider registration.'
Assert-True (($accessText -match 'TryResolve') -and ($accessText -match 'HitPartPolicyConfig')) 'Access boundary must safely resolve an optional policy.'

foreach ($path in @($entryPath, $contractPath, $declarationPath, $materialPath, $resultPath)) {
    Assert-True (Test-Path -LiteralPath $path) "Expression boundary file is missing: $path"
    $text = Get-Content -LiteralPath $path -Raw -Encoding utf8
    Assert-True ($text -match 'HitPartPolicyConfig|HitPartPolicy') "Expression boundary must carry the optional hit-part policy: $path"
}

Write-Output 'HitPartPolicyContractBoundarySmokeTests PASS'
