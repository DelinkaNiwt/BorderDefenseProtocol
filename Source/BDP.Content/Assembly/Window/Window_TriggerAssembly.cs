using System.Collections.Generic;
using BDP.Core.Information;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;
using RimWorld;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口。
    /// 它使用固定三栏布局：左侧装配区，中间芯片库存，右侧详情。
    /// </summary>
    public class Window_TriggerAssembly : Window
    {
        /// <summary>装配台窗口目标宽度；实际尺寸不会超过当前屏幕。</summary>
        private const float TargetWidth = 1360f;

        /// <summary>装配台窗口目标高度；实际尺寸不会超过当前屏幕。</summary>
        private const float TargetHeight = 720f;

        /// <summary>屏幕边缘与窗口之间保留的安全空隙。</summary>
        private const float ScreenMargin = 80f;

        /// <summary>
        /// 打开窗口的小人。
        /// </summary>
        private readonly Pawn pawn;

        /// <summary>
        /// 当前交互的装配台。
        /// </summary>
        private readonly Building_TriggerAssembler assembler;

        /// <summary>
        /// 左侧槽位面板。
        /// </summary>
        private readonly Panel_SlotLayout slotLayoutPanel = new Panel_SlotLayout();

        /// <summary>
        /// 中间芯片库存面板。
        /// </summary>
        private readonly Panel_ChipInventory chipInventoryPanel = new Panel_ChipInventory();

        /// <summary>
        /// 右侧详情面板。
        /// </summary>
        private readonly Panel_ChipDetail chipDetailPanel = new Panel_ChipDetail();

        /// <summary>
        /// Trion 预览服务。
        /// </summary>
        private readonly TriggerAssemblyPreviewService previewService = new TriggerAssemblyPreviewService();

        /// <summary>
        /// 当前拖拽状态。
        /// </summary>
        private readonly TriggerAssemblyDragState dragState = new TriggerAssemblyDragState();

        /// <summary>
        /// 当前选中的芯片。
        /// </summary>
        private Thing selectedChip;

        /// <summary>当前选中芯片对应的统一详情比较模型缓存。</summary>
        private ChipInformationComparisonModel selectedChipInformationComparison;

        /// <summary>详情模型缓存对应的芯片对象。</summary>
        private Thing selectedChipInformationSource;

        /// <summary>是否已经尝试构建当前详情模型。</summary>
        private bool selectedChipInformationResolved;

        /// <summary>
        /// 当前是否选中了槽位。
        /// </summary>
        private bool hasSelectedSlot;

        /// <summary>
        /// 当前选中的槽位侧别。
        /// </summary>
        private TriggerSide selectedSide;

        /// <summary>
        /// 当前选中的槽位索引。
        /// </summary>
        private int selectedIndex;

        /// <summary>
        /// 当前事务层。
        /// </summary>
        private TriggerAssemblyTransaction transaction;

        /// <summary>
        /// 当前 Trigger 读取口。
        /// </summary>
        private ITriggerLoadoutReader loadoutReader;

        /// <summary>
        /// 当前 Facility 读取口。
        /// </summary>
        private IAssemblerFacilityProvider facilityProvider;

        /// <summary>
        /// 当前窗口主体布局。
        /// </summary>
        private Rect inventoryPanelRect;

        /// <summary>装配台操作者的缓存正面肖像。</summary>
        private AssemblyPawnPortraitSnapshot operatorPortrait;

        /// <summary>是否已经完成本窗口生命周期内的肖像捕获尝试。</summary>
        private bool operatorPortraitCaptureAttempted;

        /// <summary>
        /// 构造触发器装配窗口。
        /// </summary>
        public Window_TriggerAssembly(Pawn pawn, Building_TriggerAssembler assembler)
        {
            this.pawn = pawn;
            this.assembler = assembler;
            forcePause = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            doCloseX = true;
            draggable = true;
        }

        /// <summary>
        /// 按当前 UI 屏幕限制窗口尺寸，由原版窗口栈负责居中。
        /// </summary>
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(
                    Mathf.Min(TargetWidth, Mathf.Max(1f, Verse.UI.screenWidth - ScreenMargin)),
                    Mathf.Min(TargetHeight, Mathf.Max(1f, Verse.UI.screenHeight - ScreenMargin)));
            }
        }

        /// <summary>
        /// 绘制固定三栏装配窗口。
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            RefreshRuntimeAccess();

            string rejectReason = assembler != null
                ? assembler.ResolveUseRejection(pawn, false)
                : "BDP_Message_TriggerAssembly_NoUse".Translate().ToString();
            if (!rejectReason.NullOrEmpty())
            {
                // 所有拖拽事务均在本次准入通过后同步提交；停运时取消尚未提交的拖拽。
                dragState.Clear();
                Widgets.Label(inRect, rejectReason);
                return;
            }

            if (loadoutReader == null || transaction == null)
            {
                Widgets.Label(inRect, "BDP_Message_TriggerAssembly_NoAvailableBody".Translate());
                return;
            }

            if (loadoutReader.LoadoutControlMode == TriggerLoadoutControlMode.PlayerNonConfigurable)
            {
                Widgets.Label(inRect, "BDP_Message_TriggerAssembly_FixedLoadout".Translate());
                return;
            }

            const float gap = 10f;
            const float assemblyWidth = 500f;
            // 芯片列表只承担检索和拖拽；把固定空间让给右栏图表，避免详情区被压扁。
            const float inventoryWidth = 300f;
            float detailWidth = inRect.width - assemblyWidth - inventoryWidth - gap * 2f;

            Rect assemblyRect = new Rect(inRect.x, inRect.y, assemblyWidth, inRect.height);
            inventoryPanelRect = new Rect(assemblyRect.xMax + gap, inRect.y, inventoryWidth, inRect.height);
            Rect detailRect = new Rect(inventoryPanelRect.xMax + gap, inRect.y, detailWidth, inRect.height);

            DrawAssemblyPanel(assemblyRect);
            DrawInventoryPanel(inventoryPanelRect);
            DrawDetailPanel(detailRect);

            HandleMouseInput();
            dragState.UpdateDragging(Event.current.mousePosition);
            HandleDragRelease();
            DrawDragGhost();
        }

        /// <summary>
        /// 绘制左侧装配区。
        /// </summary>
        private void DrawAssemblyPanel(Rect rect)
        {
            if (!operatorPortraitCaptureAttempted)
            {
                operatorPortrait = AssemblyPawnPortraitCapture.Capture(pawn);
                operatorPortraitCaptureAttempted = true;
            }

            TriggerAssemblyPreviewSnapshot preview = previewService.BuildSnapshot(
                pawn,
                loadoutReader);

            slotLayoutPanel.DrawSlotLayout(
                rect,
                pawn,
                operatorPortrait,
                loadoutReader,
                preview,
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex);
        }

        /// <summary>
        /// 关闭窗口并释放运行时 Pawn 肖像纹理。
        /// </summary>
        public override void PostClose()
        {
            if (operatorPortrait != null)
            {
                operatorPortrait.DestroyResources();
                operatorPortrait = null;
            }

            operatorPortraitCaptureAttempted = false;

            base.PostClose();
        }

        /// <summary>
        /// 绘制中间库存区。
        /// </summary>
        private void DrawInventoryPanel(Rect rect)
        {
            IReadOnlyList<Thing> chips = facilityProvider != null
                ? facilityProvider.GetAvailableChips()
                : new List<Thing>();
            int capacityLimit = facilityProvider != null
                ? facilityProvider.GetStorageCapacityLimit()
                : 0;

            chipInventoryPanel.DrawChipInventory(
                rect,
                chips,
                selectedChip,
                hasSelectedSlot,
                selectedSide,
                capacityLimit);
        }

        /// <summary>
        /// 绘制右侧详情区。
        /// </summary>
        private void DrawDetailPanel(Rect rect)
        {
            ChipInformationComparisonModel comparison = GetSelectedChipInformation();
            chipDetailPanel.DrawChipDetail(
                rect,
                selectedChip,
                comparison);
        }

        /// <summary>
        /// 每帧刷新正式读取口与事务层。
        /// </summary>
        private void RefreshRuntimeAccess()
        {
            loadoutReader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            ITriggerLoadoutCommands commands = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn);
            facilityProvider = new DefaultAssemblerFacilityProvider(assembler);
            transaction = loadoutReader != null && commands != null
                ? new TriggerAssemblyTransaction(loadoutReader, commands, facilityProvider)
                : null;
        }

        /// <summary>
        /// 处理鼠标按下选择与拖拽起点。
        /// </summary>
        private void HandleMouseInput()
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0)
            {
                return;
            }

            TriggerAssemblySlotHit slotHit = slotLayoutPanel.HitAt(current.mousePosition);
            if (slotHit != null)
            {
                SelectSlot(slotHit.Slot.Side, slotHit.Slot.Index);

                if (slotHit.Slot.LoadedChip != null)
                {
                    dragState.BeginSlotChip(slotHit.Slot.LoadedChip, slotHit.Slot.Side, slotHit.Slot.Index, current.mousePosition);
                }

                current.Use();
                return;
            }

            TriggerAssemblyInventoryHit inventoryHit = chipInventoryPanel.HitAt(current.mousePosition);
            if (inventoryHit != null)
            {
                if (selectedChip != inventoryHit.Chip)
                {
                    selectedChip = inventoryHit.Chip;
                    InvalidateSelectedChipInformation();
                }

                dragState.BeginInventoryChip(inventoryHit.Chip, current.mousePosition);
                current.Use();
                return;
            }

            if (slotLayoutPanel.IsBlankAreaAt(current.mousePosition))
            {
                ClearSlotSelection();
                current.Use();
            }
        }

        /// <summary>
        /// 处理拖拽释放并调用装配事务。
        /// </summary>
        private void HandleDragRelease()
        {
            Event current = Event.current;
            if (!dragState.ShouldRelease(current))
            {
                if (dragState.Active && current.type == EventType.MouseUp && current.button == 0)
                {
                    dragState.Clear();
                }

                return;
            }

            TriggerAssemblyOperationResult result = null;
            TriggerAssemblySlotHit slotHit = slotLayoutPanel.HitAt(current.mousePosition);
            bool releasedOnInventory = inventoryPanelRect.Contains(current.mousePosition);

            if (slotHit != null && dragState.SourceType == TriggerAssemblyDragSourceType.InventoryChip)
            {
                ITriggerSlotState slot = slotHit.Slot;
                result = slot.LoadedChip == null
                    ? transaction.TryLoadFromStorage(slot.Side, slot.Index, dragState.Chip)
                    : transaction.TryReplaceFromStorage(slot.Side, slot.Index, dragState.Chip);
            }
            else if (slotHit != null && dragState.SourceType == TriggerAssemblyDragSourceType.SlotChip)
            {
                result = transaction.TryMoveOrSwapSlot(
                    dragState.SourceSide,
                    dragState.SourceIndex,
                    slotHit.Slot.Side,
                    slotHit.Slot.Index);
            }
            else if (releasedOnInventory && dragState.SourceType == TriggerAssemblyDragSourceType.SlotChip)
            {
                result = transaction.TryUnloadToStorage(dragState.SourceSide, dragState.SourceIndex);
            }

            ShowOperationResult(result);
            if (result != null && result.Success)
            {
                InvalidateSelectedChipInformation();
            }

            dragState.Clear();
            current.Use();
        }

        /// <summary>
        /// 绘制拖拽中的鼠标旁芯片图标。
        /// </summary>
        private void DrawDragGhost()
        {
            if (!dragState.IsDragging || dragState.Chip == null)
            {
                return;
            }

            Rect iconRect = new Rect(Event.current.mousePosition.x + 12f, Event.current.mousePosition.y + 12f, 42f, 42f);
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            Widgets.ThingIcon(iconRect, dragState.Chip);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 显示一次装配事务的玩家提示。
        /// </summary>
        private void ShowOperationResult(TriggerAssemblyOperationResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.Message))
            {
                return;
            }

            MessageTypeDef messageType = result.Success
                ? MessageTypeDefOf.PositiveEvent
                : MessageTypeDefOf.RejectInput;
            Messages.Message(result.Message, assembler, messageType, false);
        }

        /// <summary>
        /// 记录当前选中的槽位。
        /// </summary>
        private void SelectSlot(TriggerSide side, int index)
        {
            hasSelectedSlot = true;
            selectedSide = side;
            selectedIndex = index;

            ITriggerSlotState slot = FindSlot(side, index);
            if (slot?.LoadedChip != null)
            {
                selectedChip = slot.LoadedChip;
                InvalidateSelectedChipInformation();
            }
        }

        /// <summary>
        /// 清除槽位板块的整组选中状态；空白区点击同时清除中栏卡片和右栏详情。
        /// </summary>
        private void ClearSlotSelection()
        {
            hasSelectedSlot = false;
            selectedChip = null;
            InvalidateSelectedChipInformation();
        }

        /// <summary>按真实芯片对象缓存统一详情模型，避免每帧重复解析。</summary>
        private ChipInformationComparisonModel GetSelectedChipInformation()
        {
            if (selectedChip == null)
            {
                InvalidateSelectedChipInformation();
                return null;
            }

            if (!selectedChipInformationResolved
                || selectedChipInformationSource != selectedChip)
            {
                selectedChipInformationSource = selectedChip;
                selectedChipInformationResolved = true;
                ChipInformationRuntimeState runtime = BuildSelectedChipRuntimeState(selectedChip);
                ExpressionPublishedProjectionSnapshot publishedProjection = pawn == null
                    ? null
                    : ExpressionSurfaceAccess.ResolvePublishedProjection(pawn);
                ChipAssemblyDetailModelBuilder.TryBuild(
                    selectedChip,
                    out selectedChipInformationComparison,
                    runtime,
                    publishedProjection);
            }

            return selectedChipInformationComparison;
        }

        /// <summary>从 Trigger 正式读取口附加当前芯片的形态、姿态和激活状态。</summary>
        private ChipInformationRuntimeState BuildSelectedChipRuntimeState(Thing chip)
        {
            if (chip == null || loadoutReader == null)
            {
                return ChipInformationRuntimeInputAdapter.CreateStatic();
            }

            bool isActive = false;
            IEnumerable<ITriggerSlotState> activeSlots = loadoutReader.GetActiveSlots();
            if (activeSlots != null)
            {
                foreach (ITriggerSlotState slot in activeSlots)
                {
                    if (slot != null && ReferenceEquals(slot.LoadedChip, chip))
                    {
                        isActive = true;
                        break;
                    }
                }
            }

            return ChipInformationRuntimeInputAdapter.Create(
                isActive,
                loadoutReader.GetChipModeKey(chip),
                loadoutReader.GetChipStanceKey(chip));
        }

        /// <summary>清除详情缓存；装配事务成功后必须重新读取真实模型。</summary>
        private void InvalidateSelectedChipInformation()
        {
            selectedChipInformationComparison = null;
            selectedChipInformationSource = null;
            selectedChipInformationResolved = false;
        }

        /// <summary>
        /// 按侧别和索引查找槽位。
        /// </summary>
        private ITriggerSlotState FindSlot(TriggerSide side, int index)
        {
            if (loadoutReader == null)
            {
                return null;
            }

            foreach (ITriggerSlotState slot in loadoutReader.GetAllSlots())
            {
                if (slot != null && slot.Side == side && slot.Index == index)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
