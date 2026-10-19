using System;
using System.Collections.Generic;
using BDP.Core.Trigger;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>触发体制造页只选择原版配方并提交账单，不参与芯片组合解析。</summary>
    public sealed class TriggerBodyManufacturingPanel
    {
        /// <summary>本次窗口选择的触发体配方。</summary>
        private RecipeDef selectedRecipe;

        /// <summary>待提交的有限制作次数。</summary>
        private int quantity = 1;

        /// <summary>原版数量控件的文本缓冲。</summary>
        private string quantityBuffer = "1";

        /// <summary>触发体列表滚动位置。</summary>
        private Vector2 listScroll;

        /// <summary>物品说明滚动位置。</summary>
        private Vector2 detailScroll;

        /// <summary>材料与数量区域滚动位置。</summary>
        private Vector2 orderScroll;

        /// <summary>按当前工作台的挂接配方和原版解锁状态列出触发体，不硬编码产品名单。</summary>
        public static List<RecipeDef> GetRecipes(Building_ChipFabricator building)
        {
            List<RecipeDef> result = new List<RecipeDef>();
            if (building == null)
            {
                return result;
            }

            foreach (RecipeDef recipe in building.def.AllRecipes)
            {
                if (recipe.AvailableNow
                    && recipe.ProducedThingDef?.GetCompProperties<CompProperties_TriggerBody>() != null)
                {
                    result.Add(recipe);
                }
            }
            return result;
        }

        /// <summary>使用原版工厂创建有限账单，自动沿配方选择半成品流程；材料和核心只限制实际开工。</summary>
        public static Bill_Production Enqueue(Building_ChipFabricator building, RecipeDef recipe, int count)
        {
            if (count <= 0 || building == null || building.BillStack.Count >= BillStack.MaxCount
                || !GetRecipes(building).Contains(recipe))
            {
                return null;
            }

            Bill_Production bill = recipe.MakeNewBill() as Bill_Production;
            if (bill == null)
            {
                return null;
            }
            bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
            bill.repeatCount = count;
            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor, null);
            building.BillStack.AddBill(bill);
            return bill;
        }

        /// <summary>绘制列表、说明、材料和共用队列；从队列载入芯片时交还芯片导航。</summary>
        public void Draw(Rect left, Rect middle, Rect right, Building_ChipFabricator building,
            ChipManufacturingEditorState editorState, ChipManufacturingQueuePanel queuePanel,
            Action onConfigurationLoaded)
        {
            List<RecipeDef> recipes = GetRecipes(building);
            if (!recipes.Contains(selectedRecipe))
            {
                selectedRecipe = recipes.Count > 0 ? recipes[0] : null;
            }
            DrawList(left, recipes);
            DrawDetails(middle);

            Widgets.DrawMenuSection(right);
            Rect inner = right.ContractedBy(8f);
            float maximumOrderHeight = Mathf.Max(0f, inner.height - 120f - 13f);
            float orderHeight = DrawOrder(new Rect(inner.x, inner.y, inner.width, maximumOrderHeight), building);
            float separatorY = inner.y + orderHeight + 6f;
            Widgets.DrawLineHorizontal(inner.x, separatorY, inner.width);
            float queueY = separatorY + 7f;
            queuePanel.Draw(new Rect(inner.x, queueY, inner.width, Mathf.Max(0f, inner.yMax - queueY)),
                building, editorState, onConfigurationLoaded);
        }

        /// <summary>左栏按产物名称展示可制造触发体，列表可滚动。</summary>
        private void DrawList(Rect rect, List<RecipeDef> recipes)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 24f),
                "BDP_TriggerBodyManufacturing_List".Translate());
            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, Mathf.Max(0f, inner.height - 28f));
            if (recipes.Count == 0)
            {
                Widgets.Label(outRect, "BDP_TriggerBodyManufacturing_Empty".Translate());
                return;
            }
            if (outRect.height <= 0f)
            {
                return;
            }
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, outRect.width - 16f),
                Mathf.Max(outRect.height, recipes.Count * 54f));
            Widgets.BeginScrollView(outRect, ref listScroll, viewRect);
            for (int index = 0; index < recipes.Count; index++)
            {
                RecipeDef recipe = recipes[index];
                ThingDef product = recipe.ProducedThingDef;
                Rect row = new Rect(0f, index * 54f, viewRect.width, 50f);
                if (recipe == selectedRecipe)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else if (Mouse.IsOver(row))
                {
                    Widgets.DrawHighlight(row);
                }
                Widgets.ThingIcon(new Rect(row.x + 4f, row.y + 5f, 40f, 40f), product);
                Widgets.Label(new Rect(row.x + 48f, row.y + 4f, row.width - 52f, row.height - 8f), product.LabelCap);
                if (Widgets.ButtonInvisible(row))
                {
                    selectedRecipe = recipe;
                    detailScroll = Vector2.zero;
                    orderScroll = Vector2.zero;
                }
            }
            Widgets.EndScrollView();
        }

        /// <summary>中栏直接使用成品语言说明和实际槽位配置，不建立另一套产品规格。</summary>
        private void DrawDetails(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            if (selectedRecipe == null)
            {
                return;
            }
            Rect inner = rect.ContractedBy(8f);
            ThingDef product = selectedRecipe.ProducedThingDef;
            Widgets.ThingIcon(new Rect(inner.x, inner.y, 64f, 64f), product);
            Widgets.Label(new Rect(inner.x + 72f, inner.y, Mathf.Max(0f, inner.width - 72f), 48f), product.LabelCap);
            CompProperties_TriggerBody slots = product.GetCompProperties<CompProperties_TriggerBody>();
            string slotSummary = "BDP_TriggerBodyManufacturing_Slots".Translate(
                slots.mainSlotCount, slots.subSlotCount, slots.specialSlotCount);
            float slotHeight = Mathf.Max(24f, Text.CalcHeight(slotSummary, inner.width));
            Widgets.Label(new Rect(inner.x, inner.y + 72f, inner.width, slotHeight), slotSummary);
            float descriptionY = inner.y + 80f + slotHeight;
            Rect outRect = new Rect(inner.x, descriptionY, inner.width, Mathf.Max(0f, inner.yMax - descriptionY));
            if (outRect.height <= 0f)
            {
                return;
            }
            string description = product.description ?? string.Empty;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, outRect.width - 16f),
                Mathf.Max(outRect.height, Text.CalcHeight(description, Mathf.Max(1f, outRect.width - 16f))));
            Widgets.BeginScrollView(outRect, ref detailScroll, viewRect);
            Widgets.Label(viewRect, description);
            Widgets.EndScrollView();
        }

        /// <summary>材料按原版配方计数，缺料只警告；使用滚动区域为共用队列保留空间。</summary>
        private float DrawOrder(Rect rect, Building_ChipFabricator building)
        {
            if (selectedRecipe == null || rect.width <= 0f || rect.height <= 0f)
            {
                return 0f;
            }
            List<string> materialLines = new List<string>();
            List<bool> shortages = new List<bool>();
            float contentWidth = Mathf.Max(1f, rect.width - 16f);
            float contentHeight = 26f + 6f + 36f + 32f;
            foreach (IngredientCount ingredient in selectedRecipe.ingredients)
            {
                string line;
                bool shortage = false;
                if (ingredient.IsFixedIngredient)
                {
                    ThingDef material = ingredient.FixedIngredient;
                    int count = ingredient.CountRequiredOfFor(material, selectedRecipe);
                    int available = building.Map?.resourceCounter?.GetCount(material) ?? 0;
                    shortage = available < count * quantity;
                    line = "BDP_TriggerBodyManufacturing_MaterialLine".Translate(material.LabelCap, count, available, count * quantity);
                }
                else
                {
                    line = "BDP_TriggerBodyManufacturing_MaterialSummary".Translate(ingredient.SummaryFor(selectedRecipe), quantity);
                }
                materialLines.Add(line);
                shortages.Add(shortage);
                contentHeight += Mathf.Max(24f, Text.CalcHeight(line, contentWidth));
            }
            float visibleHeight = Mathf.Min(rect.height, contentHeight);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
            Widgets.BeginScrollView(new Rect(rect.x, rect.y, rect.width, visibleHeight), ref orderScroll, viewRect);
            Widgets.Label(new Rect(0f, 0f, contentWidth, 24f), "BDP_ChipManufacturing_Order_Materials".Translate());
            float y = 26f;
            for (int index = 0; index < materialLines.Count; index++)
            {
                float lineHeight = Mathf.Max(24f, Text.CalcHeight(materialLines[index], contentWidth));
                Rect lineRect = new Rect(0f, y, contentWidth, lineHeight);
                Color oldColor = GUI.color;
                GUI.color = shortages[index] ? Color.yellow : oldColor;
                Widgets.Label(lineRect, materialLines[index]);
                GUI.color = oldColor;
                if (shortages[index])
                {
                    TooltipHandler.TipRegion(lineRect, "BDP_ChipManufacturing_Order_ShortageTooltip".Translate());
                }
                y += lineHeight;
            }
            y += 6f;
            Widgets.TextFieldNumeric(new Rect(0f, y, 72f, 30f), ref quantity, ref quantityBuffer, 1, 999);
            Widgets.Label(new Rect(80f, y, Mathf.Max(0f, contentWidth - 80f), 30f),
                "BDP_ChipManufacturing_Order_Quantity".Translate());
            y += 36f;
            if (Widgets.ButtonText(new Rect(0f, y, contentWidth, 32f),
                "BDP_ChipManufacturing_Order_Enqueue".Translate(), active: building.BillStack.Count < BillStack.MaxCount))
            {
                Enqueue(building, selectedRecipe, quantity);
            }
            Widgets.EndScrollView();
            return visibleHeight;
        }
    }
}
