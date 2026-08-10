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

$dimensionKindPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Arbitration\ModuleDimensionKind.cs'
$dimensionClaimPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Arbitration\ModuleDimensionClaim.cs'
$freezeSetPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Arbitration\ModuleStageFreezeSet.cs'
$arbitratorPath = Join-Path $bdpSourceRoot 'AttackExecution\RangedModules\Arbitration\ModuleStageArbitrator.cs'
$flightPolicyPath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Flight\FlightStageDimensionPolicy.cs'
$flightServicePath = Join-Path $bdpSourceRoot 'Projectiles\RangedFlightProtocol\Flight\FlightStageService.cs'

$dimensionKindText = if (Test-Path -LiteralPath $dimensionKindPath) { Get-Content -LiteralPath $dimensionKindPath -Raw -Encoding utf8 } else { '' }
$dimensionClaimText = if (Test-Path -LiteralPath $dimensionClaimPath) { Get-Content -LiteralPath $dimensionClaimPath -Raw -Encoding utf8 } else { '' }
$freezeSetText = if (Test-Path -LiteralPath $freezeSetPath) { Get-Content -LiteralPath $freezeSetPath -Raw -Encoding utf8 } else { '' }
$arbitratorText = if (Test-Path -LiteralPath $arbitratorPath) { Get-Content -LiteralPath $arbitratorPath -Raw -Encoding utf8 } else { '' }
$flightPolicyText = Get-Content -LiteralPath $flightPolicyPath -Raw -Encoding utf8
$flightServiceText = Get-Content -LiteralPath $flightServicePath -Raw -Encoding utf8

Assert-True (Test-Path -LiteralPath $dimensionKindPath) 'ModuleDimensionKind.cs must exist.'
Assert-True (Test-Path -LiteralPath $dimensionClaimPath) 'ModuleDimensionClaim.cs must exist.'
Assert-True (Test-Path -LiteralPath $freezeSetPath) 'ModuleStageFreezeSet.cs must exist.'
Assert-True (Test-Path -LiteralPath $arbitratorPath) 'ModuleStageArbitrator.cs must exist.'

Assert-True (
    ($dimensionKindText -match 'enum\s+ModuleDimensionKind') -and
    ($dimensionKindText -match 'Override') -and
    ($dimensionKindText -match 'Additive') -and
    ($dimensionKindText -match 'Freeze')
) 'ModuleDimensionKind must declare Override, Additive, and Freeze.'

Assert-True (
    ($dimensionClaimText -match 'class\s+ModuleDimensionClaim|readonly struct\s+ModuleDimensionClaim|struct\s+ModuleDimensionClaim') -and
    ($dimensionClaimText -match 'ModuleDimensionKind') -and
    ($dimensionClaimText -match 'DimensionKey')
) 'ModuleDimensionClaim must describe a dimension key and its arbitration kind.'

Assert-True (
    ($freezeSetText -match 'class\s+ModuleStageFreezeSet') -and
    ($freezeSetText -match 'Freeze') -and
    ($freezeSetText -match 'IsFrozen')
) 'ModuleStageFreezeSet must expose freeze registration and lookup.'

Assert-True (
    ($arbitratorText -match 'class\s+ModuleStageArbitrator') -and
    ($arbitratorText -match 'TryClaimOverride') -and
    ($arbitratorText -match 'CanApply') -and
    ($arbitratorText -match 'ModuleStageFreezeSet')
) 'ModuleStageArbitrator must centralize dimension ownership and freeze checks.'

Assert-True (
    ($flightPolicyText -match 'ModuleStageArbitrator') -and
    ($flightPolicyText -match 'FlightDimension')
) 'FlightStageDimensionPolicy must delegate to ModuleStageArbitrator.'

Assert-True (
    ($flightServiceText -match 'FlightStageDimensionPolicy') -and
    ($flightServiceText -notmatch 'Dictionary<FlightDimension, int>\s+owners')
) 'FlightStageService must stop inlining exclusive-owner dictionaries and go through the shared policy.'

Write-Output 'RangedModuleArbitrationSmokeTests PASS'
