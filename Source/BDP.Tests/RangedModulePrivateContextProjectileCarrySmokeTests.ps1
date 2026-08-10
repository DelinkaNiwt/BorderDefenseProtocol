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

    return Get-Content -LiteralPath $Path -Raw -Encoding utf8
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot
$bdpSourceRoot = Join-Path $repoRoot "Source\\BDP\\Core"

$projectilePath = Join-Path $bdpSourceRoot "Projectiles\\BdpProjectile.cs"
$planPath = Join-Path $bdpSourceRoot "AttackExecution\\RangedProtocol\\Model\\ProjectileInitPlan.cs"
$flightContextPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Flight\\FlightStageContext.cs"
$arrivalContextPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Arrival\\ArrivalStageContext.cs"
$hitContextPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Hit\\HitStageContext.cs"
$impactContextPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Impact\\ImpactStageContext.cs"
$flightServicePath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\RangedFlightProtocolService.cs"
$protocolPath = Join-Path $bdpSourceRoot "AttackExecution\\RangedProtocol\\RangedAttackProtocolService.cs"

$projectileText = Read-Source $projectilePath
$planText = Read-Source $planPath
$flightContextText = Read-Source $flightContextPath
$arrivalContextText = Read-Source $arrivalContextPath
$hitContextText = Read-Source $hitContextPath
$impactContextText = Read-Source $impactContextPath
$flightServiceText = Read-Source $flightServicePath
$protocolText = Read-Source $protocolPath

Assert-True (
    $planText -match "AttackContextSnapshot"
) "ProjectileInitPlan must carry AttackContextSnapshot."

Assert-True (
    ($projectileText -match "AttackContextSnapshot") -or
    ($projectileText -match "launchPlan")
) "BdpProjectile must consume the carried frozen plan and its AttackContextSnapshot."

Assert-True (
    ($flightContextText -match "AttackContextSnapshot") -and
    ($arrivalContextText -match "AttackContextSnapshot") -and
    ($hitContextText -match "AttackContextSnapshot") -and
    ($impactContextText -match "AttackContextSnapshot")
) "Projectile tail contexts must expose AttackContextSnapshot."

Assert-True (
    ($flightServiceText -match "AttackContextSnapshot") -and
    ($flightServiceText -match "CreateModuleSession")
) "RangedFlightProtocolService must rebuild the stage session from the frozen AttackContextSnapshot when needed."

Assert-True (
    ($protocolText.Contains("AttackContext = AttackContext.FromSnapshot")) -and
    ($protocolText.Contains("ProjectilePlans = mergedProjectilePlans"))
) "Dual ranged protocol merge must rebuild AttackContext from snapshot and publish the merged projectile plans."

Write-Output "RangedModulePrivateContextProjectileCarrySmokeTests PASS"
