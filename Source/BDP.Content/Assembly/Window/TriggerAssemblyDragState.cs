using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口拖拽状态。
    /// 它只记录一次鼠标拖拽的来源与芯片，不直接执行装配事务。
    /// </summary>
    internal sealed class TriggerAssemblyDragState
    {
        /// <summary>
        /// 库存到槽位拖拽路径名称。
        /// </summary>
        internal const string InventoryToSlot = "InventoryToSlot";

        /// <summary>
        /// 槽位到库存拖拽路径名称。
        /// </summary>
        internal const string SlotToInventory = "SlotToInventory";

        /// <summary>
        /// 槽位到槽位拖拽路径名称。
        /// </summary>
        internal const string SlotToSlot = "SlotToSlot";

        /// <summary>
        /// 鼠标移动超过该平方距离后视为拖拽。
        /// </summary>
        private const float DragStartDistanceSquared = 36f;

        /// <summary>
        /// 当前拖拽来源类型。
        /// </summary>
        internal TriggerAssemblyDragSourceType SourceType { get; private set; }

        /// <summary>
        /// 当前拖拽芯片。
        /// </summary>
        internal Thing Chip { get; private set; }

        /// <summary>
        /// 来源槽位侧别。
        /// </summary>
        internal TriggerSide SourceSide { get; private set; }

        /// <summary>
        /// 来源槽位索引。
        /// </summary>
        internal int SourceIndex { get; private set; }

        /// <summary>
        /// 鼠标按下位置。
        /// </summary>
        internal Vector2 MouseDownPosition { get; private set; }

        /// <summary>
        /// 当前是否已经超过拖拽阈值。
        /// </summary>
        internal bool IsDragging { get; private set; }

        /// <summary>
        /// 当前是否存在拖拽候选。
        /// </summary>
        internal bool Active
        {
            get
            {
                return SourceType != TriggerAssemblyDragSourceType.None && Chip != null;
            }
        }

        /// <summary>
        /// 从库存芯片开始拖拽。
        /// </summary>
        internal void BeginInventoryChip(Thing chip, Vector2 mousePosition)
        {
            SourceType = TriggerAssemblyDragSourceType.InventoryChip;
            Chip = chip;
            SourceSide = TriggerSide.Main;
            SourceIndex = -1;
            MouseDownPosition = mousePosition;
            IsDragging = false;
        }

        /// <summary>
        /// 从槽位芯片开始拖拽。
        /// </summary>
        internal void BeginSlotChip(Thing chip, TriggerSide side, int index, Vector2 mousePosition)
        {
            SourceType = TriggerAssemblyDragSourceType.SlotChip;
            Chip = chip;
            SourceSide = side;
            SourceIndex = index;
            MouseDownPosition = mousePosition;
            IsDragging = false;
        }

        /// <summary>
        /// 根据鼠标位置更新拖拽阈值。
        /// </summary>
        internal void UpdateDragging(Vector2 mousePosition)
        {
            if (!Active || IsDragging)
            {
                return;
            }

            Vector2 delta = mousePosition - MouseDownPosition;
            IsDragging = delta.sqrMagnitude >= DragStartDistanceSquared;
        }

        /// <summary>
        /// 判断当前事件是否应释放拖拽。
        /// </summary>
        internal bool ShouldRelease(Event current)
        {
            return Active
                && current.type == EventType.MouseUp
                && current.button == 0
                && IsDragging;
        }

        /// <summary>
        /// 清空拖拽状态。
        /// </summary>
        internal void Clear()
        {
            SourceType = TriggerAssemblyDragSourceType.None;
            Chip = null;
            SourceSide = TriggerSide.Main;
            SourceIndex = -1;
            MouseDownPosition = Vector2.zero;
            IsDragging = false;
        }
    }

    /// <summary>
    /// 装配拖拽来源类型。
    /// </summary>
    internal enum TriggerAssemblyDragSourceType
    {
        /// <summary>
        /// 没有拖拽来源。
        /// </summary>
        None,

        /// <summary>
        /// 来源是库存芯片。
        /// </summary>
        InventoryChip,

        /// <summary>
        /// 来源是槽位芯片。
        /// </summary>
        SlotChip
    }
}
