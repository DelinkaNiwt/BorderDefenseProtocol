using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Trigger;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口中间芯片库存面板。
    /// </summary>
    internal sealed class Panel_ChipInventory
    {
        /// <summary>
        /// 最近一次绘制生成的库存行命中区域。
        /// </summary>
        private readonly List<TriggerAssemblyInventoryHit> inventoryHits = new List<TriggerAssemblyInventoryHit>();

        /// <summary>
        /// 当前库存滚动位置。
        /// </summary>
        private Vector2 scrollPosition;

        /// <summary>
        /// 当前筛选模式。
        /// </summary>
        private TriggerAssemblyChipFilter filter = TriggerAssemblyChipFilter.All;

        /// <summary>
        /// 当前搜索文本。
        /// </summary>
        private string searchText = string.Empty;

        /// <summary>
        /// 绘制芯片库存。
        /// </summary>
        internal void DrawChipInventory(
            Rect rect,
            IReadOnlyList<Thing> chips,
            Thing selectedChip,
            bool hasSelectedSlot,
            TriggerSide selectedSide)
        {
            inventoryHits.Clear();
            Widgets.DrawMenuSection(rect);

            Rect inner = rect.ContractedBy(10f);
            Text.Font = GameFont.Small;
            Widgets.Label(
                new Rect(inner.x, inner.y, inner.width, 24f),
                "BDP_Window_TriggerAssembly_Inventory".Translate());
            Text.Font = GameFont.Tiny;

            DrawFilters(new Rect(inner.x, inner.y + 30f, inner.width, 28f));
            searchText = Widgets.TextField(new Rect(inner.x, inner.y + 64f, inner.width, 26f), searchText ?? string.Empty);

            Rect outRect = new Rect(inner.x, inner.y + 98f, inner.width, inner.height - 98f);
            int visibleCount = CountVisibleChips(chips, hasSelectedSlot, selectedSide);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, visibleCount * 56f + 8f));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float currentY = 0f;
            for (int i = 0; i < chips.Count; i++)
            {
                Thing chip = chips[i];
                if (!ShouldShowChip(chip, hasSelectedSlot, selectedSide))
                {
                    continue;
                }

                bool compatible = IsCompatibleWithSlot(chip, hasSelectedSlot, selectedSide);
                Rect rowRect = new Rect(viewRect.x, currentY, viewRect.width, 50f);
                DrawChipRow(rowRect, chip, selectedChip, compatible);
                Rect hitRect = new Rect(outRect.x + rowRect.x, outRect.y + rowRect.y - scrollPosition.y, rowRect.width, rowRect.height);
                inventoryHits.Add(new TriggerAssemblyInventoryHit(hitRect, chip));
                currentY += 56f;
            }

            if (currentY <= 0f)
            {
                Widgets.Label(
                    new Rect(viewRect.x, viewRect.y, viewRect.width, 22f),
                    "BDP_Window_TriggerAssembly_NoChips".Translate());
            }

            Widgets.EndScrollView();
        }

        /// <summary>
        /// 按鼠标坐标查找库存行命中区域。
        /// </summary>
        internal TriggerAssemblyInventoryHit HitAt(Vector2 mousePosition)
        {
            for (int i = 0; i < inventoryHits.Count; i++)
            {
                if (inventoryHits[i].Rect.Contains(mousePosition))
                {
                    return inventoryHits[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 绘制筛选按钮组。
        /// </summary>
        private void DrawFilters(Rect rect)
        {
            float buttonWidth = (rect.width - 8f) / 3f;
            DrawFilterButton(
                new Rect(rect.x, rect.y, buttonWidth, rect.height),
                TriggerAssemblyChipFilter.All,
                "BDP_Window_TriggerAssembly_FilterAll".Translate());
            DrawFilterButton(
                new Rect(rect.x + buttonWidth + 4f, rect.y, buttonWidth, rect.height),
                TriggerAssemblyChipFilter.Hands,
                "BDP_Window_TriggerAssembly_FilterHands".Translate());
            DrawFilterButton(
                new Rect(rect.x + (buttonWidth + 4f) * 2f, rect.y, buttonWidth, rect.height),
                TriggerAssemblyChipFilter.Special,
                "BDP_Window_TriggerAssembly_FilterSpecial".Translate());
        }

        /// <summary>
        /// 绘制单个筛选按钮。
        /// </summary>
        private void DrawFilterButton(Rect rect, TriggerAssemblyChipFilter targetFilter, string label)
        {
            bool selected = filter == targetFilter;
            if (selected)
            {
                Widgets.DrawOptionSelected(rect);
            }

            if (Widgets.ButtonText(rect, label))
            {
                filter = targetFilter;
            }
        }

        /// <summary>
        /// 绘制芯片库存行。
        /// </summary>
        private static void DrawChipRow(Rect rect, Thing chip, Thing selectedChip, bool compatible)
        {
            bool selected = selectedChip == chip;
            if (selected)
            {
                Widgets.DrawOptionSelected(rect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            Color oldColor = GUI.color;
            if (!compatible)
            {
                GUI.color = new Color(0.65f, 0.65f, 0.65f, 0.72f);
            }

            Widgets.ThingIcon(new Rect(rect.x + 6f, rect.y + 6f, 38f, 38f), chip);
            Widgets.Label(new Rect(rect.x + 50f, rect.y + 4f, rect.width - 56f, 20f), chip.LabelShortCap);
            Widgets.Label(new Rect(rect.x + 50f, rect.y + 26f, rect.width - 56f, 18f), BuildChipStatsLine(chip));

            GUI.color = oldColor;
            TooltipHandler.TipRegion(rect, BuildChipTooltip(chip, compatible));
        }

        /// <summary>
        /// 判断芯片是否应该显示。
        /// </summary>
        private bool ShouldShowChip(Thing chip, bool hasSelectedSlot, TriggerSide selectedSide)
        {
            if (chip == null)
            {
                return false;
            }

            string label = chip.LabelShortCap;
            if (!string.IsNullOrWhiteSpace(searchText)
                && (label == null || !label.ToLowerInvariant().Contains(searchText.ToLowerInvariant())))
            {
                return false;
            }

            ChipDefinitionSnapshot snapshot = ReadDefinition(chip);
            if (filter == TriggerAssemblyChipFilter.Hands)
            {
                return snapshot != null && snapshot.SlotRegion == ChipSlotRegion.MainSub;
            }

            if (filter == TriggerAssemblyChipFilter.Special)
            {
                return snapshot != null && snapshot.SlotRegion == ChipSlotRegion.Special;
            }

            return IsCompatibleWithSlot(chip, hasSelectedSlot, selectedSide) || !hasSelectedSlot;
        }

        /// <summary>
        /// 统计当前筛选下可见芯片数量。
        /// </summary>
        private int CountVisibleChips(IReadOnlyList<Thing> chips, bool hasSelectedSlot, TriggerSide selectedSide)
        {
            int count = 0;
            for (int i = 0; i < chips.Count; i++)
            {
                if (ShouldShowChip(chips[i], hasSelectedSlot, selectedSide))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 判断芯片是否兼容当前选中槽位。
        /// </summary>
        private static bool IsCompatibleWithSlot(Thing chip, bool hasSelectedSlot, TriggerSide selectedSide)
        {
            if (!hasSelectedSlot)
            {
                return true;
            }

            ChipDefinitionSnapshot snapshot = ReadDefinition(chip);
            if (snapshot == null)
            {
                return false;
            }

            bool targetSpecial = selectedSide == TriggerSide.Special;
            return targetSpecial
                ? snapshot.SlotRegion == ChipSlotRegion.Special
                : snapshot.SlotRegion == ChipSlotRegion.MainSub;
        }

        /// <summary>
        /// 读取通过正式校验的芯片定义快照。
        /// </summary>
        private static ChipDefinitionSnapshot ReadDefinition(Thing chip)
        {
            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            return snapshot != null && snapshot.IsValid
                ? snapshot
                : null;
        }

        /// <summary>
        /// 构建芯片数值摘要。
        /// </summary>
        private static string BuildChipStatsLine(Thing chip)
        {
            ChipDefinitionSnapshot snapshot = ReadDefinition(chip);
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_TrionCost".Translate();
            }

            return "BDP_Window_TriggerAssembly_Occupancy".Translate(
                snapshot.CapacityCost.ToString("0.#"),
                snapshot.ActivationCost.ToString("0.#"));
        }

        /// <summary>
        /// 构建芯片提示文本。
        /// </summary>
        private static string BuildChipTooltip(Thing chip, bool compatible)
        {
            ChipDefinitionSnapshot snapshot = ReadDefinition(chip);
            return "BDP_Window_TriggerAssembly_ChipTooltip".Translate(
                chip.LabelShortCap,
                FormatSlotRegion(snapshot),
                FormatSlotOccupancy(snapshot),
                compatible ? "BDP_Yes".Translate() : "BDP_No".Translate());
        }

        /// <summary>
        /// 把槽位占用方式转换成玩家可读文字。
        /// </summary>
        private static string FormatSlotOccupancy(ChipDefinitionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_Unknown".Translate();
            }

            switch (snapshot.SlotOccupancy)
            {
                case ChipSlotOccupancy.Single:
                    return "BDP_Window_TriggerAssembly_SingleSlot".Translate();
                case ChipSlotOccupancy.PairedHands:
                    return "BDP_Window_TriggerAssembly_PairedSlots".Translate();
                default:
                    return "BDP_Window_TriggerAssembly_Unspecified".Translate();
            }
        }

        /// <summary>把槽位区域转换成玩家可读文字。</summary>
        private static string FormatSlotRegion(ChipDefinitionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_Unknown".Translate();
            }

            return snapshot.SlotRegion == ChipSlotRegion.Special
                ? "BDP_Window_TriggerAssembly_SpecialRegion".Translate()
                : "BDP_Window_TriggerAssembly_MainSubRegion".Translate();
        }

    }

    /// <summary>
    /// 芯片库存筛选模式。
    /// </summary>
    internal enum TriggerAssemblyChipFilter
    {
        /// <summary>
        /// 显示全部芯片。
        /// </summary>
        All,

        /// <summary>
        /// 只显示主副手芯片。
        /// </summary>
        Hands,

        /// <summary>
        /// 只显示特殊槽芯片。
        /// </summary>
        Special
    }

    /// <summary>
    /// 库存行命中区域。
    /// </summary>
    internal sealed class TriggerAssemblyInventoryHit
    {
        /// <summary>
        /// 命中区域。
        /// </summary>
        internal readonly Rect Rect;

        /// <summary>
        /// 命中的芯片。
        /// </summary>
        internal readonly Thing Chip;

        /// <summary>
        /// 构造库存行命中区域。
        /// </summary>
        internal TriggerAssemblyInventoryHit(Rect rect, Thing chip)
        {
            Rect = rect;
            Chip = chip;
        }
    }
}
