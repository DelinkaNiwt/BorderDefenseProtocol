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
$bdpSourceRoot = Join-Path $repoRoot 'Source\BDP.Content'

$windowRoot = Join-Path $bdpSourceRoot 'Assembly\Window'
$windowPath = Join-Path $windowRoot 'Window_TriggerAssembly.cs'
$slotPanelPath = Join-Path $windowRoot 'Panel_SlotLayout.cs'
$inventoryPanelPath = Join-Path $windowRoot 'Panel_ChipInventory.cs'
$detailPanelPath = Join-Path $windowRoot 'Panel_ChipDetail.cs'
$dragStatePath = Join-Path $windowRoot 'TriggerAssemblyDragState.cs'

Assert-True (Test-Path -LiteralPath $windowPath) 'Window_TriggerAssembly must exist.'
Assert-True (Test-Path -LiteralPath $slotPanelPath) 'Panel_SlotLayout must exist.'
Assert-True (Test-Path -LiteralPath $inventoryPanelPath) 'Panel_ChipInventory must exist.'
Assert-True (Test-Path -LiteralPath $detailPanelPath) 'Panel_ChipDetail must exist.'
Assert-True (Test-Path -LiteralPath $dragStatePath) 'TriggerAssemblyDragState must exist.'

$windowText = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8
$slotPanelText = Get-Content -LiteralPath $slotPanelPath -Raw -Encoding utf8
$inventoryPanelText = Get-Content -LiteralPath $inventoryPanelPath -Raw -Encoding utf8
$detailPanelText = Get-Content -LiteralPath $detailPanelPath -Raw -Encoding utf8
$dragStateText = Get-Content -LiteralPath $dragStatePath -Raw -Encoding utf8

Assert-True (
    $windowText -match 'class\s+Window_TriggerAssembly\s*:\s*Window'
) 'Window_TriggerAssembly must inherit RimWorld Window.'

Assert-True (
    $windowText -match 'forcePause\s*=\s*true'
) 'Trigger assembly window must force-pause the game while open.'

Assert-True (
    ($windowText -match 'DrawAssemblyPanel') -and
    ($windowText -match 'DrawInventoryPanel') -and
    ($windowText -match 'DrawDetailPanel')
) 'Window_TriggerAssembly must keep fixed three-column draw methods.'

Assert-True (
    ($slotPanelText -match 'DrawSlotLayout') -and
    ($inventoryPanelText -match 'DrawChipInventory') -and
    ($detailPanelText -match 'DrawChipDetail')
) 'Slot, inventory, and detail panels must have dedicated draw entry points.'

Assert-True (
    $dragStateText -match 'class\s+TriggerAssemblyDragState'
) 'TriggerAssemblyDragState class must exist.'

Assert-True (
    ($dragStateText -match 'InventoryToSlot') -and
    ($dragStateText -match 'SlotToInventory') -and
    ($dragStateText -match 'SlotToSlot')
) 'Drag state must explicitly cover inventory-to-slot, slot-to-inventory, and slot-to-slot paths.'

Assert-True (
    $windowText -match 'TryLoadFromStorage|TryReplaceFromStorage'
) 'Window drag release must route inventory chips to load or replace transactions.'

Assert-True (
    $windowText -match 'TryUnloadToStorage'
) 'Window drag release must route slot chips back to storage.'

Assert-True (
    $windowText -match 'TryMoveOrSwapSlot'
) 'Window drag release must route slot-to-slot drags to move/swap transactions.'

Write-Output 'TriggerAssemblyWindow PASS'
