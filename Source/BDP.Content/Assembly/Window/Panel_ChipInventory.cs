using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Trigger;
using BDP.Core.UI.Charts;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配窗口中间芯片库面板。
    /// 工具条固定在列表上方，芯片数量只影响内部滚动内容高度。
    /// </summary>
    internal sealed class Panel_ChipInventory
    {
        /// <summary>最近一次绘制生成的库存卡片命中区域。</summary>
        private readonly List<TriggerAssemblyInventoryHit> inventoryHits = new List<TriggerAssemblyInventoryHit>();

        /// <summary>库存列表滚动位置。</summary>
        private Vector2 scrollPosition;

        /// <summary>当前筛选模式。</summary>
        private TriggerAssemblyChipFilter filter = TriggerAssemblyChipFilter.All;

        /// <summary>当前搜索文本。</summary>
        private string searchText = string.Empty;

        /// <summary>中栏标题高度。</summary>
        private const float HeaderHeight = 58f;

        /// <summary>芯片卡片标题与元数据组合块高度。</summary>
        private const float ChipCardTextHeight = 37f;

        /// <summary>中栏青绿色主色。</summary>
        private static readonly Color AccentColor = new Color(0.29f, 0.84f, 0.82f, 1f);

        /// <summary>中栏边框色。</summary>
        private static readonly Color BorderColor = new Color(0.18f, 0.42f, 0.46f, 0.92f);

        /// <summary>中栏底色。</summary>
        private static readonly Color PanelColor = new Color(0.035f, 0.095f, 0.115f, 0.96f);

        /// <summary>绘制原型结构的芯片库。</summary>
        internal void DrawChipInventory(
            Rect rect,
            IReadOnlyList<Thing> chips,
            Thing selectedChip,
            bool hasSelectedSlot,
            TriggerSide selectedSide,
            int capacityLimit)
        {
            inventoryHits.Clear();
            int chipCount = chips != null ? chips.Count : 0;
            DrawPanelFrame(rect, chipCount, capacityLimit);

            Rect bodyRect = new Rect(
                rect.x + 1f,
                rect.y + HeaderHeight + 1f,
                rect.width - 2f,
                rect.height - HeaderHeight - 2f);
            Rect contentRect = bodyRect.ContractedBy(10f);
            DrawSearchBox(new Rect(contentRect.x, contentRect.y, contentRect.width, 32f));
            DrawFilterBar(new Rect(contentRect.x, contentRect.y + 40f, contentRect.width, 30f));

            float listY = contentRect.y + 80f;
            float listHeight = Mathf.Max(44f, contentRect.yMax - listY);
            Rect outRect = new Rect(contentRect.x, listY, contentRect.width, listHeight);
            int visibleCount = CountVisibleChips(chips);
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, outRect.width - 16f), Mathf.Max(outRect.height, visibleCount * 62f + 8f));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float currentY = 0f;
            if (chips != null)
            {
                for (int i = 0; i < chips.Count; i++)
                {
                    Thing chip = chips[i];
                    if (!ShouldShowChip(chip))
                    {
                        continue;
                    }

                    bool compatible = !hasSelectedSlot
                        || TriggerAssemblyChipCompatibility.IsCompatibleWithSlot(chip, selectedSide);
                    Rect cardRect = new Rect(0f, currentY, viewRect.width, 56f);
                    DrawChipCard(cardRect, chip, selectedChip, compatible);
                    AddVisibleHit(cardRect, chip, outRect);
                    currentY += 62f;
                }
            }

            if (currentY <= 0f)
            {
                DrawNoResults(new Rect(0f, 0f, viewRect.width, 70f));
            }

            Widgets.EndScrollView();
        }

        /// <summary>按鼠标坐标查找库存卡片。</summary>
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

        /// <summary>绘制中栏外框和原型标题。</summary>
        private static void DrawPanelFrame(Rect rect, int chipCount, int capacityLimit)
        {
            Widgets.DrawBoxSolidWithOutline(rect, PanelColor, BorderColor);
            Rect headerRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, HeaderHeight);
            Widgets.DrawBoxSolid(headerRect, new Color(0.04f, 0.15f, 0.17f, 0.94f));
            DrawLabel(
                new Rect(headerRect.x + 12f, headerRect.y + 8f, headerRect.width - 24f, 14f),
                "BDP_Window_TriggerAssembly_ChipDepotNetwork".Translate().ToString(),
                GameFont.Tiny,
                AccentColor,
                TextAnchor.MiddleLeft);
            DrawLabel(
                new Rect(headerRect.x + 12f, headerRect.y + 25f, headerRect.width - 155f, 24f),
                "BDP_Window_TriggerAssembly_ChipInventoryTitle".Translate().ToString(),
                GameFont.Medium,
                new Color(0.86f, 0.95f, 0.93f, 1f),
                TextAnchor.MiddleLeft);
            DrawLabel(
                new Rect(headerRect.xMax - 143f, headerRect.y + 28f, 131f, 18f),
                "BDP_Window_TriggerAssembly_ChipCount".Translate(chipCount, capacityLimit).ToString(),
                GameFont.Tiny,
                new Color(0.42f, 0.66f, 0.66f, 1f),
                TextAnchor.MiddleRight);
            Widgets.DrawLineHorizontal(headerRect.x + 10f, headerRect.yMax, headerRect.width - 20f);
        }

        /// <summary>绘制搜索框和清除按钮。</summary>
        private void DrawSearchBox(Rect rect)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.015f, 0.055f, 0.07f, 0.92f), BorderColor);
            DrawSearchGlyph(new Vector2(rect.x + 14f, rect.center.y));
            Rect inputRect = new Rect(rect.x + 26f, rect.y + 1f, rect.width - 56f, rect.height - 2f);
            searchText = Widgets.TextField(inputRect, searchText ?? string.Empty);
            if (string.IsNullOrEmpty(searchText))
            {
                DrawLabel(
                    inputRect,
                    "BDP_Window_TriggerAssembly_SearchPlaceholder".Translate().ToString(),
                    GameFont.Tiny,
                    new Color(0.36f, 0.54f, 0.55f, 1f),
                    TextAnchor.MiddleLeft);
            }

            Rect clearRect = new Rect(rect.xMax - 29f, rect.y + 2f, 26f, rect.height - 4f);
            if (Widgets.ButtonInvisible(clearRect))
            {
                searchText = string.Empty;
            }

            DrawLabel(clearRect, "×", GameFont.Small, new Color(0.46f, 0.68f, 0.68f, 1f), TextAnchor.MiddleCenter);
        }

        /// <summary>绘制芯片分类按钮。</summary>
        private void DrawFilterBar(Rect rect)
        {
            float buttonWidth = Mathf.Max(42f, (rect.width - 8f) / 3f);
            DrawFilterButton(new Rect(rect.x, rect.y, buttonWidth, rect.height), TriggerAssemblyChipFilter.All, "BDP_Window_TriggerAssembly_FilterAll".Translate().ToString());
            DrawFilterButton(new Rect(rect.x + buttonWidth + 4f, rect.y, buttonWidth, rect.height), TriggerAssemblyChipFilter.Hands, "BDP_Window_TriggerAssembly_FilterHands".Translate().ToString());
            DrawFilterButton(new Rect(rect.x + (buttonWidth + 4f) * 2f, rect.y, buttonWidth, rect.height), TriggerAssemblyChipFilter.Special, "BDP_Window_TriggerAssembly_FilterSpecial".Translate().ToString());
        }

        /// <summary>绘制单个原型分类按钮。</summary>
        private void DrawFilterButton(Rect rect, TriggerAssemblyChipFilter targetFilter, string label)
        {
            bool selected = filter == targetFilter;
            Widgets.DrawBoxSolidWithOutline(
                rect,
                selected ? new Color(0.08f, 0.32f, 0.34f, 0.9f) : new Color(0.03f, 0.12f, 0.14f, 0.82f),
                selected ? AccentColor : BorderColor);
            if (Widgets.ButtonInvisible(rect))
            {
                filter = targetFilter;
                scrollPosition = Vector2.zero;
            }

            DrawLabel(rect, label, GameFont.Tiny, selected ? AccentColor : new Color(0.64f, 0.78f, 0.77f, 1f), TextAnchor.MiddleCenter);
        }

        /// <summary>绘制紧凑芯片卡片。</summary>
        private static void DrawChipCard(Rect rect, Thing chip, Thing selectedChip, bool compatible)
        {
            bool selected = selectedChip == chip;
            Color border = selected
                ? AccentColor
                : compatible
                    ? new Color(0.19f, 0.43f, 0.47f, 0.82f)
                    : new Color(0.40f, 0.31f, 0.20f, 0.72f);
            Color fill = selected
                ? new Color(0.08f, 0.32f, 0.34f, 0.92f)
                : new Color(0.04f, 0.14f, 0.16f, 0.88f);
            Widgets.DrawBoxSolidWithOutline(rect, fill, border);
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y, 3f, rect.height),
                compatible ? new Color(0.35f, 0.78f, 0.48f, 0.9f) : new Color(0.74f, 0.49f, 0.22f, 0.85f));

            Color oldColor = GUI.color;
            if (!compatible)
            {
                GUI.color = new Color(0.68f, 0.63f, 0.55f, 0.75f);
            }

            Widgets.ThingIcon(new Rect(rect.x + 9f, rect.y + 10f, 36f, 36f), chip);
            GUI.color = oldColor;

            float textTop = rect.y + (rect.height - ChipCardTextHeight) * 0.5f;
            Rect mainRect = new Rect(rect.x + 53f, textTop, Mathf.Max(20f, rect.width - 62f), ChipCardTextHeight);
            DrawClippedLabel(
                new Rect(mainRect.x, mainRect.y, mainRect.width, 19f),
                ChipDisplayNameFormatter.Format(chip.LabelShortCap),
                GameFont.Small,
                compatible ? new Color(0.86f, 0.95f, 0.92f, 1f) : new Color(0.72f, 0.68f, 0.61f, 1f),
                TextAnchor.MiddleLeft);
            ChipDefinitionSnapshot snapshot = ReadDefinition(chip);
            DrawClippedLabel(
                new Rect(mainRect.x, mainRect.y + 20f, mainRect.width, 17f),
                BuildChipMetaLine(snapshot),
                GameFont.Tiny,
                new Color(0.46f, 0.67f, 0.67f, 1f),
                TextAnchor.MiddleLeft);

            TooltipHandler.TipRegion(rect, BuildChipTooltip(chip, compatible));
        }

        /// <summary>绘制空结果状态。</summary>
        private static void DrawNoResults(Rect rect)
        {
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.02f, 0.08f, 0.10f, 0.6f), BorderColor);
            DrawLabel(
                rect,
                "BDP_Window_TriggerAssembly_NoChips".Translate().ToString(),
                GameFont.Tiny,
                new Color(0.42f, 0.62f, 0.63f, 1f),
                TextAnchor.MiddleCenter);
        }

        /// <summary>绘制搜索图标。</summary>
        private static void DrawSearchGlyph(Vector2 center)
        {
            Color color = new Color(0.38f, 0.63f, 0.64f, 1f);
#if BDP_RIMWORLD_1_7
            // 与其它公共图形共用原版列表及裁切；保留搜索图标现有十段轮廓和手柄尺寸。
            Rect bounds = new Rect(center.x - 7f, center.y - 7f, 18f, 18f);
            new ChartCanvas(bounds, bounds).Draw(context =>
            {
                context.DrawCircleOutline(new Vector2(7f, 7f), 5f, color, 1.5f, 10);
                context.DrawLine(new Vector2(11f, 11f), new Vector2(16f, 16f), color, 1.5f);
            });
#else
            float radius = 5f;
            const int segments = 10;
            Vector2 previous = center + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector2 current = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Widgets.DrawLine(previous, current, color, 1.5f);
                previous = current;
            }

            Widgets.DrawLine(center + new Vector2(4f, 4f), center + new Vector2(9f, 9f), color, 1.5f);
#endif
        }

        /// <summary>把可见卡片转换为窗口坐标命中区域。</summary>
        private void AddVisibleHit(Rect localRect, Thing chip, Rect viewportRect)
        {
            Rect screenRect = new Rect(
                viewportRect.x + localRect.x,
                viewportRect.y + localRect.y - scrollPosition.y,
                localRect.width,
                localRect.height);
            float xMin = Mathf.Max(screenRect.x, viewportRect.x);
            float yMin = Mathf.Max(screenRect.y, viewportRect.y);
            float xMax = Mathf.Min(screenRect.xMax, viewportRect.xMax);
            float yMax = Mathf.Min(screenRect.yMax, viewportRect.yMax);
            if (xMax > xMin && yMax > yMin)
            {
                inventoryHits.Add(new TriggerAssemblyInventoryHit(new Rect(xMin, yMin, xMax - xMin, yMax - yMin), chip));
            }
        }

        /// <summary>判断芯片是否应该显示。</summary>
        private bool ShouldShowChip(Thing chip)
        {
            if (chip == null)
            {
                return false;
            }

            string label = ChipDisplayNameFormatter.Format(chip.LabelShortCap);
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

            return true;
        }

        /// <summary>统计当前筛选下可见芯片数量。</summary>
        private int CountVisibleChips(IReadOnlyList<Thing> chips)
        {
            int count = 0;
            if (chips == null)
            {
                return count;
            }

            for (int i = 0; i < chips.Count; i++)
            {
                if (ShouldShowChip(chips[i]))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>读取通过正式校验的芯片定义快照。</summary>
        private static ChipDefinitionSnapshot ReadDefinition(Thing chip)
        {
            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(chip);
            return snapshot != null && snapshot.IsValid ? snapshot : null;
        }

        /// <summary>构建芯片卡片第二行摘要。</summary>
        private static string BuildChipMetaLine(ChipDefinitionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_TrionCost".Translate().ToString();
            }

            return "BDP_Window_TriggerAssembly_ChipCardMeta".Translate(
                snapshot.CapacityCost.ToString("0.#"),
                snapshot.ActivationCost.ToString("0.#"),
                FormatSlotRegion(snapshot)).ToString();
        }

        /// <summary>构建芯片提示文本。</summary>
        private static string BuildChipTooltip(Thing chip, bool compatible)
        {
            ChipDefinitionSnapshot snapshot = ReadDefinition(chip);
            return "BDP_Window_TriggerAssembly_ChipTooltip".Translate(
                ChipDisplayNameFormatter.Format(chip.LabelShortCap),
                FormatSlotRegion(snapshot),
                FormatSlotOccupancy(snapshot),
                compatible ? "BDP_Yes".Translate() : "BDP_No".Translate());
        }

        /// <summary>把槽位占用方式转换成玩家可读文字。</summary>
        private static string FormatSlotOccupancy(ChipDefinitionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_Unknown".Translate().ToString();
            }

            switch (snapshot.SlotOccupancy)
            {
                case ChipSlotOccupancy.Single:
                    return "BDP_Window_TriggerAssembly_SingleSlot".Translate().ToString();
                case ChipSlotOccupancy.PairedHands:
                    return "BDP_Window_TriggerAssembly_PairedSlots".Translate().ToString();
                default:
                    return "BDP_Window_TriggerAssembly_Unspecified".Translate().ToString();
            }
        }

        /// <summary>把槽位区域转换成玩家可读文字。</summary>
        private static string FormatSlotRegion(ChipDefinitionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "BDP_Window_TriggerAssembly_Unknown".Translate().ToString();
            }

            return snapshot.SlotRegion == ChipSlotRegion.Special
                ? "BDP_Window_TriggerAssembly_SpecialRegion".Translate().ToString()
                : "BDP_Window_TriggerAssembly_MainSubRegion".Translate().ToString();
        }

        /// <summary>绘制带裁切的文本。</summary>
        private static void DrawClippedLabel(Rect rect, string text, GameFont font, Color color, TextAnchor anchor)
        {
            GUI.BeginGroup(rect);
            DrawLabel(new Rect(0f, 0f, rect.width, rect.height), text, font, color, anchor);
            GUI.EndGroup();
        }

        /// <summary>绘制统一样式的文本并恢复 GUI 状态。</summary>
        private static void DrawLabel(Rect rect, string text, GameFont font, Color color, TextAnchor anchor)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = font;
            Text.Anchor = anchor;
            GUI.color = color;
            Widgets.Label(rect, text ?? string.Empty);
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }
    }

    /// <summary>芯片库存筛选模式。</summary>
    internal enum TriggerAssemblyChipFilter
    {
        /// <summary>显示全部芯片。</summary>
        All,

        /// <summary>只显示主副手芯片。</summary>
        Hands,

        /// <summary>只显示特殊槽芯片。</summary>
        Special
    }

    /// <summary>库存卡片命中区域。</summary>
    internal sealed class TriggerAssemblyInventoryHit
    {
        /// <summary>命中区域。</summary>
        internal readonly Rect Rect;

        /// <summary>命中的芯片。</summary>
        internal readonly Thing Chip;

        /// <summary>构造库存卡片命中区域。</summary>
        internal TriggerAssemblyInventoryHit(Rect rect, Thing chip)
        {
            Rect = rect;
            Chip = chip;
        }
    }
}
