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
$detailRendererPath = Join-Path $windowRoot 'ChipInformationDetailRenderer.cs'
$displayNameFormatterPath = Join-Path $windowRoot 'ChipDisplayNameFormatter.cs'
$dragStatePath = Join-Path $windowRoot 'TriggerAssemblyDragState.cs'
$previewServicePath = Join-Path $windowRoot 'TriggerAssemblyPreviewService.cs'
$compatibilityPath = Join-Path $windowRoot 'TriggerAssemblyChipCompatibility.cs'
$facilityInterfacePath = Join-Path $bdpSourceRoot 'Assembly\Facility\IAssemblerFacilityProvider.cs'
$facilityProviderPath = Join-Path $bdpSourceRoot 'Assembly\Facility\DefaultAssemblerFacilityProvider.cs'
$chipContainerPath = Join-Path $bdpSourceRoot 'Assembly\Building\CompChipContainer.cs'
$languagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\ChipManufacturing.xml'
$windowLanguagePath = Join-Path $repoRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Windows.xml'

Assert-True (Test-Path -LiteralPath $windowPath) 'Window_TriggerAssembly must exist.'
Assert-True (Test-Path -LiteralPath $slotPanelPath) 'Panel_SlotLayout must exist.'
Assert-True (Test-Path -LiteralPath $inventoryPanelPath) 'Panel_ChipInventory must exist.'
Assert-True (Test-Path -LiteralPath $detailPanelPath) 'Panel_ChipDetail must exist.'
Assert-True (Test-Path -LiteralPath $detailRendererPath) 'ChipInformationDetailRenderer must exist.'
Assert-True (Test-Path -LiteralPath $displayNameFormatterPath) 'ChipDisplayNameFormatter must exist.'
Assert-True (Test-Path -LiteralPath $dragStatePath) 'TriggerAssemblyDragState must exist.'
Assert-True (Test-Path -LiteralPath $previewServicePath) 'TriggerAssemblyPreviewService must exist.'
Assert-True (Test-Path -LiteralPath $compatibilityPath) 'TriggerAssemblyChipCompatibility must exist.'
Assert-True (Test-Path -LiteralPath $facilityInterfacePath) 'IAssemblerFacilityProvider must exist.'
Assert-True (Test-Path -LiteralPath $facilityProviderPath) 'DefaultAssemblerFacilityProvider must exist.'
Assert-True (Test-Path -LiteralPath $chipContainerPath) 'CompChipContainer must exist.'

$windowText = Get-Content -LiteralPath $windowPath -Raw -Encoding utf8
$slotPanelText = Get-Content -LiteralPath $slotPanelPath -Raw -Encoding utf8
$inventoryPanelText = Get-Content -LiteralPath $inventoryPanelPath -Raw -Encoding utf8
$detailPanelText = Get-Content -LiteralPath $detailPanelPath -Raw -Encoding utf8
$detailRendererText = Get-Content -LiteralPath $detailRendererPath -Raw -Encoding utf8
$displayNameFormatterText = Get-Content -LiteralPath $displayNameFormatterPath -Raw -Encoding utf8
$dragStateText = Get-Content -LiteralPath $dragStatePath -Raw -Encoding utf8
$previewServiceText = Get-Content -LiteralPath $previewServicePath -Raw -Encoding utf8
$compatibilityText = Get-Content -LiteralPath $compatibilityPath -Raw -Encoding utf8
$facilityInterfaceText = Get-Content -LiteralPath $facilityInterfacePath -Raw -Encoding utf8
$facilityProviderText = Get-Content -LiteralPath $facilityProviderPath -Raw -Encoding utf8
$chipContainerText = Get-Content -LiteralPath $chipContainerPath -Raw -Encoding utf8
$chipManufacturingLanguageText = Get-Content -LiteralPath $languagePath -Raw -Encoding utf8
$windowLanguageText = Get-Content -LiteralPath $windowLanguagePath -Raw -Encoding utf8

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
    ($displayNameFormatterText -match 'class\s+ChipDisplayNameFormatter') -and
    ($displayNameFormatterText -match 'BDP_ChipManufacturing_ProductLabel')
) 'Chip display names must use a UI-only formatter based on the localized product prefix.'
Assert-True ($slotPanelText -match 'ChipDisplayNameFormatter') 'Loaded slot names and tooltips must use the shared chip display formatter.'
Assert-True ($inventoryPanelText -match 'ChipDisplayNameFormatter') 'Inventory names and tooltips must use the shared chip display formatter.'
Assert-True ($detailRendererText -match 'selectedChip\?\.LabelCap' -and
    $detailRendererText -match 'model\.Identity') '详情标题必须优先显示已选芯片名称并保留统一身份回退。'

$previewCall = [regex]::Match(
    $windowText,
    '(?s)previewService\.BuildSnapshot\(.*?\);').Value
Assert-True ($previewCall -match 'pawn' -and $previewCall -match 'loadoutReader') '装配区预览必须读取 pawn 和正式 loadout reader。'
Assert-True ($previewCall -notmatch 'hasSelectedSlot|selectedSide|selectedIndex|selectedChip') '选中状态不得传入预占用快照。'
Assert-True (
    $previewServiceText -match 'BuildSnapshot\(\s*Pawn pawn,\s*ITriggerLoadoutReader reader\s*\)'
) '预占用快照接口不得接收选中槽位或选中芯片。'
Assert-True ($previewServiceText -match 'PreviewReserved\s*=\s*loadedReserved') '预览预占用必须等于实际已装配芯片累计值。'
$previewMethod = [regex]::Match(
    $previewServiceText,
    '(?s)BuildSnapshot\(.*?\n\s*\}'
).Value
Assert-True ($previewMethod -notmatch 'hasPreviewSlot|previewSide|previewIndex|previewChip') '预览服务不得保留选中芯片替换模拟逻辑。'
Assert-True ($compatibilityText -match 'class\s+TriggerAssemblyChipCompatibility') '槽位兼容性必须由窗口层共享工具统一判断。'
Assert-True ($inventoryPanelText -notmatch 'showUnavailable') '中栏不得保留按槽位兼容性筛选的开关。'
Assert-True ($inventoryPanelText -match 'ShouldShowChip\(Thing chip\)') '中栏列表筛选方法不得接收选中槽位状态。'
Assert-True ($inventoryPanelText -match 'CountVisibleChips\(IReadOnlyList<Thing> chips\)') '中栏可见数量统计不得接收选中槽位状态。'
Assert-True ($inventoryPanelText -notmatch 'if\s*\(\s*!showUnavailable') '中栏列表不得按选中槽位隐藏不兼容芯片。'
Assert-True ($slotPanelText -match 'TriggerAssemblyChipCompatibility\.IsCompatibleWithSlot\(dragState\.Chip,\s*slot\.Side\)') '拖拽高亮必须按芯片和目标槽位侧别判断兼容性。'
Assert-True ($slotPanelText -notmatch 'dragState\.IsDragging\s*&&\s*!slot\.IsBindingMirror\s*;') '拖拽高亮不得只依据是否存在拖拽状态。'
Assert-True ($windowLanguageText -notmatch 'BDP_Window_TriggerAssembly_ShowUnavailable') '中栏不应保留无效的兼容性筛选文案。'
Assert-True ($facilityInterfaceText -match 'GetStorageCapacityLimit') '设施读取口必须提供芯片仓容量上限。'
Assert-True ($facilityProviderText -match 'GetStorageCapacityLimit' -and $facilityProviderText -match 'CapacityLimit') '设施读取器必须汇总有效连接芯片仓容量。'
Assert-True ($chipContainerText -match 'CapacityLimit') '芯片仓必须暴露自身容量上限。'
Assert-True ($windowText -match 'GetStorageCapacityLimit') '装配窗口必须读取芯片仓容量上限。'
Assert-True ($inventoryPanelText -match 'DrawPanelFrame\(rect,\s*chipCount,\s*capacityLimit\)') '中栏标题必须同时绘制当前数量和容量上限。'
Assert-True ($windowLanguageText -match 'BDP_Window_TriggerAssembly_ChipCount>[^<]*\{0\}[^<]*\{1\}') '芯片库数量文案必须包含当前值和上限值。'

Assert-True ($detailPanelText -notmatch 'Widgets\.DrawMenuSection') 'Detail panel must not retain RimWorld default gray section background.'
Assert-True ($detailPanelText -match '0\.035f,\s*0\.095f,\s*0\.115f') 'Detail panel must use the shared deep teal panel color.'

$trionBlock = [regex]::Match(
    $slotPanelText,
    '(?s)private static void DrawTrionCapacity\(.*?(?=\r?\n\s*private static )').Value
Assert-True ($trionBlock -match 'PreviewReserved') 'Trion capacity bar must display cumulative reserved capacity.'
Assert-True ($trionBlock -notmatch 'preview\.Cur') 'Trion capacity bar must not use current Trion as its main value.'
Assert-True ($trionBlock -notmatch 'TrionReserved|TrionAvailable') 'Trion capacity bar must remove the redundant lower summary labels.'
Assert-True ($trionBlock -match 'remaining|Remaining|reservedWidth|split|separator|DrawLine') 'Trion capacity bar must visibly split reserved and remaining capacity.'
Assert-True ($slotPanelText -match 'TrionCapacityHeight\s*=\s*6[0-9]f') 'Trion capacity card must be compressed after removing the lower labels.'
Assert-True ($windowLanguageText -match '<BDP_Window_TriggerAssembly_TrionCapacity>芯片预占用</BDP_Window_TriggerAssembly_TrionCapacity>') 'Trion capacity title must use the Chinese cumulative-reservation meaning.'
Assert-True ($chipManufacturingLanguageText -match 'BDP_ChipManufacturing_ProductLabel') 'Chip product prefix must remain sourced from the language package.'

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
