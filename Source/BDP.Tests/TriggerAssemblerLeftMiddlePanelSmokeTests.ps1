$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $sourceRoot
$windowPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Window_TriggerAssembly.cs'
$slotPanelPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Panel_SlotLayout.cs'
$operatorPortraitCapturePath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\AssemblyPawnPortraitCapture.cs'
$operatorPortraitSnapshotPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\AssemblyPawnPortraitSnapshot.cs'
$operatorPortraitWoundPatchPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Patch_PawnRenderNodeWorker_OverlayWounds_AssemblyPortrait.cs'
$inventoryPanelPath = Join-Path $sourceRoot 'BDP.Content\Assembly\Window\Panel_ChipInventory.cs'
$languagePath = Join-Path $projectRoot 'Languages\ChineseSimplified (简体中文)\Keyed\Windows.xml'

$window = Get-Content -Raw $windowPath
$slotPanel = Get-Content -Raw $slotPanelPath
$operatorPortraitCapture = Get-Content -Raw $operatorPortraitCapturePath
$operatorPortraitSnapshot = Get-Content -Raw $operatorPortraitSnapshotPath
$operatorPortraitWoundPatch = Get-Content -Raw $operatorPortraitWoundPatchPath
$inventoryPanel = Get-Content -Raw $inventoryPanelPath
$language = Get-Content -Raw $languagePath

function Assert-Contains([string]$text, [string]$needle, [string]$message) {
    if (-not $text.Contains($needle)) {
        throw "FAIL: $message"
    }
}

function Assert-NotContains([string]$text, [string]$needle, [string]$message) {
    if ($text.Contains($needle)) {
        throw "FAIL: $message"
    }
}

# 三栏宽度必须优先保证左栏构型区和右栏详情区，中栏保持紧凑固定宽度。
Assert-Contains $window 'private const float TargetWidth = 1360f;' '装配台窗口必须扩宽到参考布局所需的目标尺寸。'
Assert-Contains $window 'const float assemblyWidth = 500f' '左栏必须扩宽以容纳横向卡片。'
Assert-Contains $window 'const float inventoryWidth = 300f' '中栏必须保持紧凑固定宽度。'
Assert-Contains $window 'Verse.UI.screenWidth - ScreenMargin' '扩宽窗口必须受当前屏幕边界限制。'

# 左栏必须包含原型中的稳定结构，并用内部滚动承载可变槽位数量。
Assert-Contains $slotPanel 'BeginScrollView' '左栏可变槽位内容必须使用内部滚动区。'
Assert-Contains $slotPanel 'AssemblyProfile' '左栏必须显示原型编号标题。'
Assert-Contains $slotPanel 'DrawOperatorMeta' '左栏必须显示操作者摘要。'
Assert-Contains $slotPanel 'DrawAssemblyStage' '左栏必须显示中央构型示意区。'
Assert-Contains $slotPanel 'DrawSpecialSection' '左栏必须显示特殊槽位区。'
Assert-Contains $slotPanel 'DrawTrionCapacity' '左栏必须显示 Trion 容量区。'
Assert-Contains $slotPanel 'DrawSlotColumn' '主手/副手槽位必须使用原型卡片列。'
Assert-Contains $slotPanel 'GUI.DrawTexture' '中央操作者必须使用实际 Pawn 肖像纹理。'
Assert-NotContains $slotPanel 'private static void DrawOperatorFigure' '左栏不得继续使用旧版粗糙人物绘制入口。'
Assert-NotContains $slotPanel 'private static void DrawCircle' '人物头部不得继续使用面板内的低分段近似圆。'
Assert-NotContains $slotPanel 'DrawBodyPlaceholder' '左栏不得继续使用旧版粗略占位绘制入口。'
Assert-Contains $operatorPortraitCapture 'RenderPawn(' '中央操作者必须复用 RimWorld 原版 Pawn 渲染管线。'
Assert-Contains $operatorPortraitCapture 'Rot4.South' '中央操作者必须固定为面朝用户的正面朝向。'
Assert-Contains $operatorPortraitWoundPatch 'PawnRenderNodeWorker_OverlayWounds' '中央操作者必须屏蔽伤口覆盖层。'
Assert-Contains $operatorPortraitWoundPatch 'IsCapturingWithoutWounds' '伤口覆盖层只能在装配台肖像捕获期间被屏蔽。'
Assert-Contains $operatorPortraitSnapshot 'RenderTexture' '中央操作者必须使用运行时纹理而不是 PNG。'
Assert-NotContains $slotPanel 'OperatorFigureRenderer' '左栏不得继续依赖已放弃的矢量人物绘制器。'
Assert-Contains $slotPanel 'contentLeft' '左栏主体区块必须有相对整个左栏的水平居中偏移。'
Assert-Contains $slotPanel '(rect.height - columnHeight) * 0.5f' '主手/副手槽位列必须在舞台内垂直居中。'
Assert-NotContains $slotPanel 'MainColumnNote' '主手槽下方说明属于标记的冗余元素，必须移除。'
Assert-NotContains $slotPanel 'SubColumnNote' '副手槽下方说明属于标记的冗余元素，必须移除。'
Assert-NotContains $slotPanel 'OperatorFront' '中央操作者正面文字属于标记的冗余元素，必须移除。'

# 已确认的左栏冗余信息必须移除，但操作者名称、槽位卡片和容量主体仍保留。
foreach ($needle in @(
    'BDP_Window_TriggerAssembly_OperatorRole',
    'BDP_Window_TriggerAssembly_OperatorStatus',
    'BuildSlotCode',
    'indexRect',
    'BDP_Window_TriggerAssembly_TrionDelta')) {
    Assert-NotContains $slotPanel $needle "左栏不应继续绘制 $needle。"
}

# 槽位比例与已装配卡片文字必须遵循参考图的层级，并且不得静默裁切名称。
foreach ($needle in @(
    'private const float SlotCardAspectRatio = 3.3f;',
    'private const float SlotTextWidthRatio = 0.72f;',
    'private const float SlotTextLeftRatio = 0.26f;',
    'private const float SlotCategoryFontScale = 0.78f;',
    'private const float SlotCategoryHeight = 14f;',
    'private const float SlotNameHeight = 22f;',
    'private const float SlotTextVerticalPadding = 2f;',
    'private const float SlotTextLineGap = 0f;',
    'private const float SpecialSlotMaxWidth = 136f;',
    'private const float StageInset = 8f;',
    'ChipSnapshotAccess.Read',
    'DefDatabase<ChipCategoryDef>.GetNamedSilentFail',
    'ChipInstanceSurfaceAccess.ReadSourceReference',
    'ChipManufacturingPresetDef',
    'SlotTextMetrics',
    'BuildSlotTextMetrics',
    'GetCommonSlotFontSize',
    'DrawSlotLabel',
    'GUI.BeginGroup(rect)',
    'GUI.Label(new Rect(0f, 0f, rect.width, rect.height), content, labelStyle)',
    'labelStyle.fontSize = fontSize',
    'labelStyle.CalcSize',
    'labelStyle.CalcHeight',
    'Text.WordWrap')) {
    Assert-Contains $slotPanel $needle "左栏槽位卡片缺少 $needle 布局契约。"
}
Assert-NotContains $slotPanel 'DrawClippedLabel' '已装配槽位卡片不得继续用裁切标签隐藏文字。'
Assert-NotContains $slotPanel 'MainSubSlotHeight' '槽位卡片高度不得继续使用固定胖卡片高度。'
Assert-NotContains $slotPanel 'MeasureSlotTextHeight' '槽位卡片高度不得由已装配文字内容动态撑高。'
Assert-NotContains $slotPanel 'WrapSlotText' '槽位卡片文字不得通过换行破坏固定长宽比。'
Assert-NotContains $slotPanel 'Text.CalcHeight' '槽位卡片不得用换行高度计算改变卡片高度。'
Assert-NotContains $slotPanel 'DrawMeasuredLabel' '槽位卡片不得继续使用旧的动态测量文字绘制入口。'
Assert-NotContains $slotPanel 'FitSlotLabelStyle' '槽位卡片不得按单张卡片独立缩放字号。'
Assert-NotContains $slotPanel 'GUIUtility.ScaleAroundPivot' '槽位卡片文字不得通过 GUI 矩阵缩放破坏滚动区坐标。'
Assert-NotContains $slotPanel 'GUI.matrix' '槽位卡片文字不得修改全局 GUI 矩阵。'
Assert-Contains $slotPanel 'Mathf.Min(SpecialSlotMaxWidth' '特殊槽位卡片宽度必须受独立上限控制。'
Assert-Contains $slotPanel 'specialContentWidth' '特殊槽位卡片必须按双列总宽度居中，而不是直接填满两半。'
Assert-Contains $slotPanel 'stageWidth - StageInset * 2f - centerWidth - SlotGap * 2f' '两侧槽位宽度计算必须扣除舞台左右内边距，避免副槽越出舞台。'
Assert-Contains $slotPanel 'float sideWidth = GetStageSideWidth(rect.width);' '舞台实际绘制必须复用扣除内边距后的两侧槽位宽度。'
Assert-Contains $slotPanel 'TextAnchor.UpperLeft' '芯片主分类必须在文字区左上对齐。'
Assert-Contains $slotPanel 'TextAnchor.MiddleLeft' '芯片名称必须在右侧文字区左下对齐。'
Assert-NotContains $slotPanel 'TextAnchor.UpperRight' '芯片主分类不得继续右对齐。'
Assert-Contains $slotPanel 'textMetrics.CategoryFontSize' '所有芯片分类必须使用本轮统一字号。'
Assert-Contains $slotPanel 'textMetrics.NameFontSize' '所有芯片名称必须使用本轮统一字号。'
Assert-Contains $slotPanel 'float contentHeight' '槽位文字区必须按分类和名称的真实行高计算，而不是把名称矩形撑满剩余空间。'
Assert-Contains $slotPanel 'float textTop' '槽位文字区上下留白必须围绕实际内容组均衡计算。'
Assert-Contains $slotPanel 'float nameY = categoryY + categoryHeight + SlotTextLineGap;' '分类和名称必须使用真实的零间隔相邻布局。'
Assert-NotContains $slotPanel 'rect.yMax - SlotTextVerticalPadding - nameHeight' '芯片名称不得再次独立贴在卡片底部。'
Assert-Contains $slotPanel 'cardWidth / SlotCardAspectRatio' '槽位卡片高度必须按参考图长宽比由宽度推导。'
Assert-Contains $slotPanel 'cardWidth * SlotTextWidthRatio' '卡片文字区宽度必须按卡片宽度比例计算。'
Assert-Contains $slotPanel 'return GetBaseSlotHeight(cardWidth);' '空槽和有芯片槽位必须共用固定比例高度。'
Assert-Contains $slotPanel 'GameFont.Medium' '芯片名称必须使用大于分类文字的字号层级。'
Assert-Contains $slotPanel 'FontStyle.Bold' '芯片名称必须使用黑体视觉层级。'

# 中栏必须是原型的库存工具条 + 紧凑卡片列表，列表高度固定并独立滚动。
Assert-Contains $inventoryPanel 'ChipDepotNetwork' '中栏必须显示原型编号标题。'
Assert-Contains $inventoryPanel 'DrawSearchBox' '中栏必须显示原型搜索框。'
Assert-Contains $inventoryPanel 'DrawFilterButton' '中栏必须保留分类筛选。'
Assert-NotContains $inventoryPanel 'showUnavailable' '中栏不得按选中槽位提供兼容性筛选开关。'
Assert-Contains $inventoryPanel 'ShouldShowChip(Thing chip)' '中栏列表筛选不得接收选中槽位状态。'
Assert-Contains $inventoryPanel 'CountVisibleChips(IReadOnlyList<Thing> chips)' '中栏可见数量统计不得接收选中槽位状态。'
Assert-Contains $inventoryPanel 'TriggerAssemblyChipCompatibility.IsCompatibleWithSlot' '中栏卡片兼容性必须使用共享判断。'
Assert-Contains $inventoryPanel 'DrawChipCard' '中栏必须使用紧凑芯片卡片。'
Assert-Contains $inventoryPanel 'BeginScrollView' '中栏芯片列表必须使用内部滚动区。'
Assert-NotContains $inventoryPanel 'ShowUnavailable' '中栏不得继续保留已删除的兼容性切换文案入口。'
Assert-NotContains $inventoryPanel 'chip.LabelShortCap == "' '中栏不得按具体芯片名称编写业务分支。'

# 已确认的中栏冗余信息必须移除；兼容性变量仍由卡片样式和筛选逻辑使用。
foreach ($needle in @(
    'DrawInventorySummary',
    'BDP_Window_TriggerAssembly_InventorySummary',
    'BDP_Window_TriggerAssembly_InventoryCount',
    'BDP_Window_TriggerAssembly_DragToAssemble',
    'BDP_Window_TriggerAssembly_UnavailableFlag',
    'BDP_Window_TriggerAssembly_NotCompatible',
    'DrawInventoryFooter',
    'BDP_Window_TriggerAssembly_DragToSlot',
    'BDP_Window_TriggerAssembly_ReturnToInventory')) {
    Assert-NotContains $inventoryPanel $needle "中栏不应继续绘制 $needle。"
}

# 新增文案必须来自语言包。
foreach ($key in @(
    'BDP_Window_TriggerAssembly_AssemblyProfile',
    'BDP_Window_TriggerAssembly_OperatorName',
    'BDP_Window_TriggerAssembly_SpecialChannel',
    'BDP_Window_TriggerAssembly_TrionCapacity')) {
    Assert-Contains $language $key "语言包缺少 $key。"
}

Assert-Contains $language '<BDP_Window_TriggerAssembly_AssemblyProfile>01 // ASSEMBLY STATUS</BDP_Window_TriggerAssembly_AssemblyProfile>' '左栏英文标题必须同步改为装配状态。'
Assert-Contains $language '<BDP_Window_TriggerAssembly_AssemblyProfileTitle>装配状态</BDP_Window_TriggerAssembly_AssemblyProfileTitle>' '左栏中文标题必须改为装配状态。'
Assert-Contains $language '<BDP_Window_TriggerAssembly_OperatorName>触发器操作者 · {0}</BDP_Window_TriggerAssembly_OperatorName>' '操作者摘要必须先显示角色再显示名字。'
Assert-NotContains $language 'BDP_Window_TriggerAssembly_MainColumnNote' '语言包不得保留主手槽冗余说明。'
Assert-NotContains $language 'BDP_Window_TriggerAssembly_SubColumnNote' '语言包不得保留副手槽冗余说明。'
Assert-NotContains $language 'BDP_Window_TriggerAssembly_OperatorFront' '语言包不得保留中央操作者冗余说明。'

foreach ($key in @(
    'BDP_Window_TriggerAssembly_OperatorRole',
    'BDP_Window_TriggerAssembly_OperatorStatus',
    'BDP_Window_TriggerAssembly_TrionDelta',
    'BDP_Window_TriggerAssembly_InventorySummary',
    'BDP_Window_TriggerAssembly_InventoryCount',
    'BDP_Window_TriggerAssembly_DragToAssemble',
    'BDP_Window_TriggerAssembly_UnavailableFlag',
    'BDP_Window_TriggerAssembly_NotCompatible',
    'BDP_Window_TriggerAssembly_DragToSlot',
    'BDP_Window_TriggerAssembly_ReturnToInventory',
    'BDP_Window_TriggerAssembly_ShowUnavailable')) {
    Assert-NotContains $language $key "语言包不应继续保留 $key。"
}

Write-Output 'TriggerAssemblerLeftMiddlePanel PASS'
