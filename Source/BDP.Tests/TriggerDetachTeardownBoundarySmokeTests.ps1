$ErrorActionPreference = "Stop"

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $sourceRoot

$compPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\State\CompTriggerBody.cs'
$detachPath = Join-Path $repoRoot 'Source\BDP\Core\Trigger\Runtime\TriggerDetachTeardownTransaction.cs'

$compText = Get-Content -LiteralPath $compPath -Raw -Encoding utf8
$detachText = Get-Content -LiteralPath $detachPath -Raw -Encoding utf8

Assert-True (
    $compText -match 'public override void Notify_Unequipped\(Pawn pawn\)'
) 'CompTriggerBody must own trigger-body unequip teardown.'

Assert-True (
    $compText -match 'RequestRelease\(\)'
) 'Unequipping a trigger body while combat body is active must request release first.'

Assert-True (
    $compText -match 'SyncReservedTrion\(pawn,\s*0f\)'
) 'Unequipping a trigger body must clear reserved Trion.'

Assert-True (
    $compText -match 'ForceTeardownOnDetach\(pawn\)'
) 'Unequipping a trigger body must run detach teardown.'

Assert-True (
    ($detachText -match 'setSwitchContext\?\.Invoke\(TriggerSide\.Main,\s*null\)') -and
    ($detachText -match 'setSwitchContext\?\.Invoke\(TriggerSide\.Sub,\s*null\)') -and
    ($detachText -match 'setSwitchContext\?\.Invoke\(TriggerSide\.Special,\s*null\)')
) 'Detach teardown must clear all switch contexts.'

Assert-True (
    $detachText -notmatch 'UnregisterSlotDrain'
) 'Detach teardown must leave expression drain cleanup to published projection reconciliation.'

Assert-True (
    $detachText -match 'slot\.SetActive\(false\)'
) 'Detach teardown must deactivate slots instead of restoring them on re-equip.'

Assert-True (
    $detachText -match 'UnregisterCombatBodyMaintenanceDrain'
) 'Detach teardown must unregister combat body maintenance drain.'

Assert-True (
    $detachText -match 'ClearPublishedProjection'
) 'Detach teardown must clear published projection.'

Write-Output 'TriggerDetachTeardownBoundary PASS'
