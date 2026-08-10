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
$flightRecordPath = Join-Path $bdpSourceRoot "Projectiles\\RangedFlightProtocol\\Model\\FlightRecord.cs"
$semanticContextPath = Join-Path $bdpSourceRoot "Semantics\\SemanticContext.cs"
$attackContextSnapshotPath = Join-Path $bdpSourceRoot "AttackExecution\\Context\\AttackContextSnapshot.cs"

$projectileText = Read-Source $projectilePath
$planText = Read-Source $planPath
$flightRecordText = Read-Source $flightRecordPath
$semanticContextText = Read-Source $semanticContextPath
$attackContextSnapshotText = Read-Source $attackContextSnapshotPath

$hasPersistableProjectilePlan =
    ($planText -match "ProjectileInitPlan : IExposable") -and
    $planText.Contains("public void ExposeData()") -and
    $planText.Contains("AttackContextSnapshot")
Assert-True $hasPersistableProjectilePlan "ProjectileInitPlan must become a persistable frozen projectile plan."

$hasPersistableFlightRecord =
    ($flightRecordText -match "FlightRecord : IExposable") -and
    $flightRecordText.Contains("public void ExposeData()")
Assert-True $hasPersistableFlightRecord "FlightRecord must persist in-flight state for save/load continuity."

$hasProjectileExposeData =
    $projectileText.Contains("public override void ExposeData()") -and
    $projectileText.Contains("Scribe_Deep.Look(ref launchPlan") -and
    $projectileText.Contains("Scribe_Deep.Look(ref currentFlightRecord") -and
    $projectileText.Contains("Scribe_Values.Look(ref speedTickRemainder") -and
    $projectileText.Contains("SemanticContext = launchPlan != null ? launchPlan.SemanticContext : null")
Assert-True $hasProjectileExposeData "BdpProjectile must persist and restore the launch plan, flight record, and semantic bridge state."

$hasSemanticContextSaveLoad =
    ($semanticContextText -match "SemanticContext : ISemanticContext, IExposable") -and
    $semanticContextText.Contains("public void ExposeData()")
Assert-True $hasSemanticContextSaveLoad "Default SemanticContext must support save/load so projectile plans do not drop semantic payloads after load."

$hasAttackContextSnapshotSaveLoad =
    ($attackContextSnapshotText -match "AttackContextSnapshot : IExposable") -and
    $attackContextSnapshotText.Contains("public void ExposeData()")
Assert-True $hasAttackContextSnapshotSaveLoad "AttackContextSnapshot must support save/load when a projectile plan carries it."

Write-Output "RangedModulePrivateContextSaveLoadSmokeTests PASS"
