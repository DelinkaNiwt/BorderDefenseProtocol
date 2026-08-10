using System.Collections.Generic;
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
        }

        /// <summary>
        /// 固定窗口尺寸；后续三栏 UI 以这个尺寸为基准分配布局。
        /// </summary>
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1180f, 720f);
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
            const float assemblyWidth = 390f;
            const float inventoryWidth = 420f;
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
            TriggerAssemblyPreviewSnapshot preview = previewService.BuildSnapshot(
                pawn,
                loadoutReader,
                hasSelectedSlot,
                selectedSide,
                selectedIndex,
                selectedChip);

            slotLayoutPanel.DrawSlotLayout(
                rect,
                loadoutReader,
                preview,
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex);
        }

        /// <summary>
        /// 绘制中间库存区。
        /// </summary>
        private void DrawInventoryPanel(Rect rect)
        {
            IReadOnlyList<Thing> chips = facilityProvider != null
                ? facilityProvider.GetAvailableChips()
                : new List<Thing>();

            chipInventoryPanel.DrawChipInventory(
                rect,
                chips,
                selectedChip,
                hasSelectedSlot,
                selectedSide);
        }

        /// <summary>
        /// 绘制右侧详情区。
        /// </summary>
        private void DrawDetailPanel(Rect rect)
        {
            ITriggerSlotState selectedSlot = hasSelectedSlot
                ? FindSlot(selectedSide, selectedIndex)
                : null;

            int availableChipCount = facilityProvider != null
                ? facilityProvider.GetAvailableChips().Count
                : 0;

            chipDetailPanel.DrawChipDetail(
                rect,
                selectedChip,
                selectedSlot,
                loadoutReader,
                availableChipCount);
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
                if (slotHit.Slot.LoadedChip != null && !slotHit.Slot.IsBindingMirror)
                {
                    dragState.BeginSlotChip(slotHit.Slot.LoadedChip, slotHit.Slot.Side, slotHit.Slot.Index, current.mousePosition);
                }

                current.Use();
                return;
            }

            TriggerAssemblyInventoryHit inventoryHit = chipInventoryPanel.HitAt(current.mousePosition);
            if (inventoryHit != null)
            {
                selectedChip = inventoryHit.Chip;
                dragState.BeginInventoryChip(inventoryHit.Chip, current.mousePosition);
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
            if (slot != null && slot.LoadedChip != null)
            {
                selectedChip = slot.LoadedChip;
            }
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
