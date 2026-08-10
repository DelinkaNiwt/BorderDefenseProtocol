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

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$modProjectsRoot = Split-Path -Parent $repoRoot
$trackingModulePath = Join-Path $modProjectsRoot 'BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\RangedModules\Samples\TrackingModule.cs'
$trackingModuleText = Read-Source $trackingModulePath

Assert-True (
    ($trackingModuleText -match 'context\.HitThing\s*==\s*null') -and
    ($trackingModuleText -match 'distanceToTarget\s*<=\s*frozenConfig\.HitWindow') -and
    ($trackingModuleText -match 'HasOverrideHitThing\s*=\s*true') -and
    ($trackingModuleText -match 'OverrideHitThing\s*=\s*state\.LockedTarget\.Thing') -and
    ($trackingModuleText -match 'HasOverrideHitCell\s*=\s*true') -and
    ($trackingModuleText -match 'OverrideHitCell\s*=\s*state\.LockedTarget\.Cell')
) 'TrackingModule must recover the locked target hit when vanilla hit flags filtered the target out but the projectile has already returned into the tracking hit window.'

Write-Output 'TrackingRecoveredTargetHitSmokeTests PASS'
