using System.Collections.Generic;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口左侧槽位布局面板。
    /// </summary>
    internal sealed class Panel_SlotLayout
    {
        /// <summary>
        /// 最近一次绘制生成的槽位命中区域。
        /// </summary>
        private readonly List<TriggerAssemblySlotHit> slotHits = new List<TriggerAssemblySlotHit>();

        /// <summary>
        /// 绘制槽位布局。
        /// </summary>
        internal void DrawSlotLayout(
            Rect rect,
            ITriggerLoadoutReader reader,
            TriggerAssemblyPreviewSnapshot preview,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex)
        {
            slotHits.Clear();
            Widgets.DrawMenuSection(rect);

            Rect inner = rect.ContractedBy(10f);
            DrawTrionPreview(new Rect(inner.x, inner.y, inner.width, 84f), preview);

            Rect bodyRect = new Rect(inner.x + 96f, inner.y + 120f, inner.width - 192f, 190f);
            DrawBodyPlaceholder(bodyRect);

            DrawSideSlots(
                reader,
                TriggerSide.Main,
                new Rect(inner.x, inner.y + 122f, 88f, 250f),
                "BDP_Side_Main".Translate().ToString(),
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex);

            DrawSideSlots(
                reader,
                TriggerSide.Sub,
                new Rect(inner.xMax - 88f, inner.y + 122f, 88f, 250f),
                "BDP_Side_Sub".Translate().ToString(),
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex);

            DrawSideSlots(
                reader,
                TriggerSide.Special,
                new Rect(inner.x + 58f, inner.yMax - 168f, inner.width - 116f, 150f),
                "BDP_Side_Special".Translate().ToString(),
                dragState,
                hasSelectedSlot,
                selectedSide,
                selectedIndex);
        }

        /// <summary>
        /// 按鼠标坐标查找最近一次绘制的槽位命中区域。
        /// </summary>
        internal TriggerAssemblySlotHit HitAt(Vector2 mousePosition)
        {
            for (int i = 0; i < slotHits.Count; i++)
            {
                if (slotHits[i].Rect.Contains(mousePosition))
                {
                    return slotHits[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 绘制 Trion 预览摘要。
        /// </summary>
        private static void DrawTrionPreview(Rect rect, TriggerAssemblyPreviewSnapshot preview)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.09f, 0.10f, 0.11f, 0.18f), new Color(0.55f, 0.68f, 0.78f, 0.55f));
            Text.Font = GameFont.Small;
            Widgets.Label(
                new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 24f),
                "BDP_Window_TriggerAssembly_Preview".Translate());
            Text.Font = GameFont.Tiny;

            string line1 = "BDP_Window_TriggerAssembly_PreviewLine1".Translate(
                Format(preview.Cur),
                Format(preview.Max),
                Format(preview.Available));
            string line2 = "BDP_Window_TriggerAssembly_PreviewLine2".Translate(
                Format(preview.Allocated),
                Format(preview.Reserved),
                Format(preview.PreviewReserved));
            string line3 = "BDP_Window_TriggerAssembly_PreviewLine3".Translate(
                FormatSigned(preview.ReservedDelta),
                Format(preview.TotalDrainPerSecond));

            Widgets.Label(new Rect(rect.x + 8f, rect.y + 30f, rect.width - 16f, 18f), line1);
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 48f, rect.width - 16f, 18f), line2);
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 66f, rect.width - 16f, 18f), line3);
        }

        /// <summary>
        /// 绘制简单人形占位图。
        /// </summary>
        private static void DrawBodyPlaceholder(Rect rect)
        {
            Color lineColor = new Color(0.60f, 0.68f, 0.72f, 0.65f);
            Widgets.DrawLine(new Vector2(rect.center.x, rect.y + 28f), new Vector2(rect.center.x, rect.y + 128f), lineColor, 2f);
            Widgets.DrawLine(new Vector2(rect.center.x - 42f, rect.y + 74f), new Vector2(rect.center.x + 42f, rect.y + 74f), lineColor, 2f);
            Widgets.DrawLine(new Vector2(rect.center.x, rect.y + 128f), new Vector2(rect.center.x - 34f, rect.yMax - 8f), lineColor, 2f);
            Widgets.DrawLine(new Vector2(rect.center.x, rect.y + 128f), new Vector2(rect.center.x + 34f, rect.yMax - 8f), lineColor, 2f);
            Rect headRect = new Rect(rect.center.x - 18f, rect.y, 36f, 36f);
            Widgets.DrawBoxSolidWithOutline(headRect, new Color(0.12f, 0.13f, 0.14f, 0.2f), lineColor);
        }

        /// <summary>
        /// 绘制某一侧的槽位组。
        /// </summary>
        private void DrawSideSlots(
            ITriggerLoadoutReader reader,
            TriggerSide side,
            Rect rect,
            string label,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(
                new Rect(rect.x, rect.y, rect.width, 18f),
                "BDP_Side_ColumnHeader".Translate(label));

            float currentY = rect.y + 22f;
            foreach (ITriggerSlotState slot in reader.GetSlots(side))
            {
                Rect slotRect = new Rect(rect.x, currentY, rect.width, 54f);
                DrawSlot(slotRect, slot, dragState, hasSelectedSlot, selectedSide, selectedIndex);
                slotHits.Add(new TriggerAssemblySlotHit(slotRect, slot));
                currentY += 60f;
            }
        }

        /// <summary>
        /// 绘制单个槽位。
        /// </summary>
        private static void DrawSlot(
            Rect rect,
            ITriggerSlotState slot,
            TriggerAssemblyDragState dragState,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int selectedIndex)
        {
            bool selected = hasSelectedSlot && slot.Side == selectedSide && slot.Index == selectedIndex;
            bool canDrop = dragState.IsDragging && !slot.IsBindingMirror;
            Color fillColor = selected
                ? new Color(0.24f, 0.36f, 0.42f, 0.45f)
                : new Color(0.08f, 0.09f, 0.10f, 0.20f);
            Color borderColor = canDrop
                ? new Color(0.42f, 0.70f, 0.62f, 0.85f)
                : new Color(0.50f, 0.55f, 0.58f, 0.65f);

            Widgets.DrawBoxSolidWithOutline(rect, fillColor, borderColor);
            if (slot.LoadedChip != null)
            {
                Widgets.ThingIcon(new Rect(rect.x + 6f, rect.y + 6f, 38f, 38f), slot.LoadedChip);
            }

            string label = slot.Index + "  " + (slot.LoadedChip != null
                ? slot.LoadedChip.LabelShortCap
                : "BDP_None".Translate().ToString());
            Widgets.Label(new Rect(rect.x + 48f, rect.y + 6f, rect.width - 52f, 18f), label);
            Widgets.Label(new Rect(rect.x + 48f, rect.y + 28f, rect.width - 52f, 18f), BuildSlotFlags(slot));

            TooltipHandler.TipRegion(rect, BuildSlotTooltip(slot));
        }

        /// <summary>
        /// 构建槽位状态短文本。
        /// </summary>
        private static string BuildSlotFlags(ITriggerSlotState slot)
        {
            if (slot.IsBindingMirror)
            {
                return "BDP_Window_TriggerAssembly_MirrorLocked".Translate();
            }

            if (slot.IsActive)
            {
                return "BDP_Window_TriggerAssembly_ActiveFlag".Translate();
            }

            return slot.IsDisabled
                ? "BDP_Window_TriggerAssembly_DisabledFlag".Translate()
                : "BDP_Window_TriggerAssembly_AvailableFlag".Translate();
        }

        /// <summary>
        /// 构建槽位提示文本。
        /// </summary>
        private static string BuildSlotTooltip(ITriggerSlotState slot)
        {
            return "BDP_Window_TriggerAssembly_SideInfo".Translate(
                BuildSideLabel(slot.Side),
                slot.Index,
                 slot.LoadedChip != null ? slot.LoadedChip.LabelShortCap : "BDP_None".Translate().ToString(),
                 slot.IsBindingMirror ? "BDP_Yes".Translate().ToString() : "BDP_No".Translate().ToString(),
                slot.HasBindingPartner
                    ? BuildSideLabel(slot.BindingPartnerSide) + ":" + slot.BindingPartnerIndex
                     : "BDP_None".Translate().ToString());
        }

        /// <summary>把槽位侧别转换为玩家可读名称。</summary>
        private static string BuildSideLabel(TriggerSide side)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    return "BDP_Side_MainFull".Translate();
                case TriggerSide.Sub:
                    return "BDP_Side_SubFull".Translate();
                case TriggerSide.Special:
                    return "BDP_Side_SpecialFull".Translate();
                default:
                    return side.ToString();
            }
        }

        /// <summary>
        /// 格式化资源数值。
        /// </summary>
        private static string Format(float value)
        {
            return value.ToString("0.#");
        }

        /// <summary>
        /// 格式化带符号资源变化。
        /// </summary>
        private static string FormatSigned(float value)
        {
            return (value >= 0f ? "+" : "") + value.ToString("0.#");
        }
    }

    /// <summary>
    /// 槽位命中区域。
    /// </summary>
    internal sealed class TriggerAssemblySlotHit
    {
        /// <summary>
        /// 命中区域。
        /// </summary>
        internal readonly Rect Rect;

        /// <summary>
        /// 命中的槽位。
        /// </summary>
        internal readonly ITriggerSlotState Slot;

        /// <summary>
        /// 构造槽位命中区域。
        /// </summary>
        internal TriggerAssemblySlotHit(Rect rect, ITriggerSlotState slot)
        {
            Rect = rect;
            Slot = slot;
        }
    }
}
