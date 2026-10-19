$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    Assert-True ($Text.Contains($Expected)) $Message
}

function Get-BracedBlock {
    param([string]$Text, [string]$Marker)
    $start = $Text.IndexOf($Marker, [System.StringComparison]::Ordinal)
    Assert-True ($start -ge 0) "找不到代码块：$Marker"
    $open = $Text.IndexOf('{', $start)
    Assert-True ($open -ge 0) "代码块缺少左括号：$Marker"
    $depth = 0
    for ($i = $open; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -eq '{') { $depth++ }
        elseif ($Text[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $Text.Substring($open, $i - $open + 1) }
        }
    }
    throw "代码块缺少右括号：$Marker"
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$windowRoot = Join-Path $sourceRoot 'BDP.Content\Assembly\Window'
$layout = Get-Content -LiteralPath (Join-Path $windowRoot 'Panel_SlotLayout.cs') -Raw -Encoding utf8
$window = Get-Content -LiteralPath (Join-Path $windowRoot 'Window_TriggerAssembly.cs') -Raw -Encoding utf8

$drawLayout = Get-BracedBlock $layout 'internal void DrawSlotLayout('
Assert-True ($drawLayout -match 'Vector2 mousePosition = Event\.current\.mousePosition;') `
    '槽位布局必须在滚动视图变换前捕获本帧鼠标位置。'

$stage = Get-BracedBlock $layout 'private void DrawAssemblyStage('
$mainCardsCall = $stage.IndexOf('DrawSlotColumn(', [System.StringComparison]::Ordinal)
$subCardsCall = $stage.IndexOf('DrawSlotColumn(', $mainCardsCall + 1, [System.StringComparison]::Ordinal)
$stagePreviewCall = $stage.IndexOf('DrawStagePlacementPreview(', [System.StringComparison]::Ordinal)
Assert-True ($mainCardsCall -ge 0 -and $subCardsCall -gt $mainCardsCall -and $stagePreviewCall -gt $subCardsCall) `
    '主副手槽位的基础卡片必须先于悬停结果预览绘制。'

$specialSection = Get-BracedBlock $layout 'private void DrawSpecialSection('
$specialHit = $specialSection.IndexOf('AddVisibleHit(slotRect, slot, viewportRect)', [System.StringComparison]::Ordinal)
$specialPreview = $specialSection.IndexOf('DrawSpecialPlacementPreview(', [System.StringComparison]::Ordinal)
Assert-True ($specialHit -ge 0 -and $specialPreview -gt $specialHit) `
    '特殊槽位必须在登记可见命中区域后绘制悬停预览。'

$validPreview = Get-BracedBlock $layout 'private static bool IsValidDropPreviewTarget('
Assert-Contains $validPreview 'dragState.IsDragging' '预览只能在拖拽已经开始后显示。'
Assert-Contains $validPreview 'TriggerAssemblyChipCompatibility.IsCompatibleWithSlot' '预览必须复用现有槽位兼容性判断。'
Assert-Contains $validPreview 'TriggerAssemblyChipCompatibility.HasAvailablePairedPartner' '预览必须验证成对芯片的伙伴槽可用。'
Assert-Contains $validPreview 'IsSameBindingSelection' '拖回原成对槽位时不能显示误导性结果预览。'

$pairedPreview = Get-BracedBlock $layout 'private void DrawStagePlacementPreview('
Assert-Contains $pairedPreview 'HitAt(mousePosition)' '舞台预览必须通过现有槽位命中区域定位悬停槽。'
Assert-Contains $pairedPreview 'ChipSlotOccupancy.PairedHands' '成对芯片预览必须依据真实占槽类型判断。'
Assert-Contains $pairedPreview 'DrawStageSlotPlacementPreview' '主副槽区必须实际绘制半透明预览卡片。'
Assert-Contains $pairedPreview 'DrawStageEmptySlotPlacementPreview' '普通芯片替换成对芯片时必须预览伙伴槽释放为空。'
Assert-Contains $pairedPreview 'BindingPartnerSide' '镜像预览必须依据槽位绑定侧别定位。'

$specialPreviewMethod = Get-BracedBlock $layout 'private void DrawSpecialPlacementPreview('
Assert-Contains $specialPreviewMethod 'DrawSlotPlacementPreview' '特殊槽区必须绘制同样的半透明结果卡片。'

$previewCard = Get-BracedBlock $layout 'private static void DrawSlotPlacementPreview('
Assert-Contains $previewCard 'Widgets.DrawBoxSolidWithOutline' '结果预览必须绘制半透明卡片底色与边框。'
Assert-Contains $previewCard 'DrawSlotContent(rect, chip, textMetrics, PreviewOpacity)' '预览卡片必须复用正式槽位的图文布局。'
Assert-Contains $previewCard 'finally' '预览绘制必须通过 finally 恢复图形状态。'
Assert-Contains $previewCard 'GUI.color = oldGuiColor' '预览结束后必须恢复原 GUI 颜色。'

$emptyPreviewCard = Get-BracedBlock $layout 'private static void DrawEmptySlotPlacementPreview('
Assert-Contains $emptyPreviewCard 'BDP_Window_TriggerAssembly_EmptySlot' '镜像位被释放时必须用已有空槽文案表示结果。'
Assert-Contains $emptyPreviewCard 'Widgets.DrawBoxSolidWithOutline' '空槽结果也必须用低对比卡片表现。'

Assert-True ($layout -notmatch 'TryLoadFromStorage|TryReplaceFromStorage|TryMoveOrSwapSlot') `
    '槽位面板预览只绘制结果，不能调用装配事务。'
$release = Get-BracedBlock $window 'private void HandleDragRelease('
Assert-Contains $release 'transaction.TryMoveOrSwapSlot(' '槽位间装载操作仍应只在松开拖拽时执行。'
Assert-Contains $release 'transaction.TryReplaceFromStorage(' '库存替换仍应只在松开拖拽时执行。'

Write-Output 'AssemblySlotHoverPreview PASS'
