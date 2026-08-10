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

$equipmentTickPatchPath = Join-Path $repoRoot 'Source\BDP\Patches\Patch_Pawn_EquipmentTracker_EquipmentTrackerTick.cs'
$bodyPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$runtimeCoordinatorPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerRuntimeCoordinator.cs'
$runtimeStorePath = Join-Path $repoRoot 'Source\BDP\Core\AttackExecution\AttackExecutionPlanRuntimeStore.cs'

$equipmentTickPatchText = Get-Content -LiteralPath $equipmentTickPatchPath -Raw -Encoding utf8
$bodyText = Get-Content -LiteralPath $bodyPath -Raw -Encoding utf8
$runtimeCoordinatorText = Get-Content -LiteralPath $runtimeCoordinatorPath -Raw -Encoding utf8

Assert-True (
    $equipmentTickPatchText -notmatch 'AllEquipmentListForReading'
) 'Equipment tick bridge must stop scanning AllEquipmentListForReading.'

Assert-True (
    $equipmentTickPatchText -match '__instance\?\.Primary'
) 'Equipment tick bridge must read only equipment.Primary.'

Assert-True (
    $equipmentTickPatchText -match 'primaryEquipment\?\.TryGetComp<CompTriggerBody>\(\)'
) 'Equipment tick bridge must resolve CompTriggerBody only from the current primary weapon.'

Assert-True (
    $equipmentTickPatchText -match 'triggerBody\?\.RuntimeTick\(\);'
) 'Equipment tick bridge must delegate runtime advancement to CompTriggerBody.RuntimeTick().'

Assert-True (
    ($bodyText -match 'internal bool RuntimeTick\(\)') -or
    ($runtimeCoordinatorText -match 'internal bool RuntimeTick\(\)')
) 'Task 1 must introduce a unified RuntimeTick() entry for trigger runtime ownership.'

Assert-True (
    $runtimeCoordinatorText -match 'TryFinalizePostLoadProjectionRefresh'
) 'RuntimeTick owner must still keep post-load finalize capability in the unified runtime path.'

Assert-True (
    -not (Test-Path -LiteralPath $runtimeStorePath)
) 'Task 1 must delete AttackExecutionPlanRuntimeStore.cs completely.'

Write-Output 'PrimaryTriggerRuntimeOwnershipSmokeTests PASS'
