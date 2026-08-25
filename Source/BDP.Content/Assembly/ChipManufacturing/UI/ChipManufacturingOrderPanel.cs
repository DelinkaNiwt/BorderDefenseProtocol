using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Recipe;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>右栏上半区：材料、地图存量、数量和提交按钮。</summary>
    public sealed class ChipManufacturingOrderPanel
    {
        /// <summary>当前数量文本输入缓冲。</summary>
        private string quantityBuffer = "1";

        /// <summary>缓冲当前对应的草稿，用于路径切换时同步。</summary>
        private ChipManufacturingDraft bufferedDraft;

        /// <summary>较矮窗口中材料与数量区域的滚动位置。</summary>
        private Vector2 scrollPosition;

        /// <summary>绘制当前订单并返回实际占用高度；按钮点击时加入真实账单栈。</summary>
        public float Draw(
            Rect rect,
            Building_ChipFabricator building,
            ChipManufacturingDraft draft,
            ChipCombinationResolution resolution)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return 0f;
            }

            ChipManufacturingCost cost = ResolveCost(resolution);
            float fullContentHeight = CalculateContentHeight(
                rect.width,
                building,
                draft,
                cost);
            bool needsScroll = fullContentHeight > rect.height;
            float viewWidth = Mathf.Max(1f, rect.width - (needsScroll ? 16f : 0f));
            float contentHeight = CalculateContentHeight(
                viewWidth,
                building,
                draft,
                cost);
            float visibleHeight = Mathf.Min(rect.height, contentHeight);
            if (needsScroll)
            {
                Rect viewRect = new Rect(0f, 0f, viewWidth, contentHeight);
                Widgets.BeginScrollView(
                    new Rect(rect.x, rect.y, rect.width, visibleHeight),
                    ref scrollPosition,
                    viewRect);
                DrawContent(viewRect, building, draft, resolution, cost);
                Widgets.EndScrollView();
            }
            else
            {
                DrawContent(
                    new Rect(rect.x, rect.y, rect.width, contentHeight),
                    building,
                    draft,
                    resolution,
                    cost);
            }

            return visibleHeight;
        }

        /// <summary>在给定内容坐标中绘制材料、数量和加入任务按钮。</summary>
        private void DrawContent(
            Rect inner,
            Building_ChipFabricator building,
            ChipManufacturingDraft draft,
            ChipCombinationResolution resolution,
            ChipManufacturingCost cost)
        {
            float y = inner.y;
            Widgets.Label(new Rect(inner.x, y, inner.width, 24f),
                "BDP_ChipManufacturing_Order_Materials".Translate());
            y += 26f;

            if (cost != null)
            {
                for (int index = 0; index < cost.Ingredients.Count; index++)
                {
                    ThingDefCountClass ingredient = cost.Ingredients[index];
                    int available = building?.Map?.resourceCounter?.GetCount(ingredient.thingDef) ?? 0;
                    int total = ingredient.count * (draft?.Quantity ?? 1);
                    bool shortage = available < total;
                    Color oldColor = GUI.color;
                    string line = BuildMaterialLine(ingredient, available, total);
                    float lineHeight = Mathf.Max(24f, Text.CalcHeight(line, inner.width));
                    Rect lineRect = new Rect(inner.x, y, inner.width, lineHeight);
                    try
                    {
                        if (shortage)
                        {
                            GUI.color = Color.yellow;
                        }

                        Widgets.Label(lineRect, line);
                    }
                    finally
                    {
                        GUI.color = oldColor;
                    }

                    if (shortage)
                    {
                        TooltipHandler.TipRegion(
                            lineRect,
                            "BDP_ChipManufacturing_Order_ShortageTooltip".Translate());
                    }
                    y += lineHeight;
                }
            }

            y += 6f;
            SyncQuantityBuffer(draft);
            int quantity = draft?.Quantity ?? 1;
            Rect quantityRect = new Rect(inner.x, y, 72f, 30f);
            Widgets.TextFieldNumeric(quantityRect, ref quantity, ref quantityBuffer, 1, 999);
            draft?.SetQuantity(quantity);
            Widgets.Label(new Rect(quantityRect.xMax + 8f, y, inner.width - 80f, 30f),
                "BDP_ChipManufacturing_Order_Quantity".Translate());
            y += 36f;

            bool canEnqueue = building != null
                && draft != null
                && ChipBillQueueOperations.CanEnqueue(
                    resolution,
                    draft.Quantity,
                    building.BillStack.Count);
            Rect buttonRect = new Rect(inner.x, y, inner.width, 32f);
            if (Widgets.ButtonText(
                buttonRect,
                "BDP_ChipManufacturing_Order_Enqueue".Translate(),
                active: canEnqueue))
            {
                ChipBillQueueOperations.Enqueue(building, draft, resolution);
            }

        }

        /// <summary>按当前有效组合计算一次材料清单。</summary>
        private static ChipManufacturingCost ResolveCost(
            ChipCombinationResolution resolution)
        {
            if (resolution == null
                || resolution.Status != ChipCombinationResolutionStatus.Valid)
            {
                return null;
            }

            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(
                "BDP_Recipe_ProduceChip");
            return ChipManufacturingCostCalculator.Calculate(
                recipe,
                resolution.Actions,
                resolution.ArmamentForm);
        }

        /// <summary>按当前栏宽计算完整材料区内容高度。</summary>
        private static float CalculateContentHeight(
            float width,
            Building_ChipFabricator building,
            ChipManufacturingDraft draft,
            ChipManufacturingCost cost)
        {
            float height = 26f;
            if (cost != null)
            {
                for (int index = 0; index < cost.Ingredients.Count; index++)
                {
                    ThingDefCountClass ingredient = cost.Ingredients[index];
                    int available = building?.Map?.resourceCounter?.GetCount(
                        ingredient.thingDef) ?? 0;
                    int total = ingredient.count * (draft?.Quantity ?? 1);
                    string line = BuildMaterialLine(ingredient, available, total);
                    height += Mathf.Max(24f, Text.CalcHeight(line, width));
                }
            }

            return height + 6f + 36f + 32f;
        }

        /// <summary>建立一条材料数量显示文本。</summary>
        private static string BuildMaterialLine(
            ThingDefCountClass ingredient,
            int available,
            int total)
        {
            return "BDP_ChipManufacturing_Order_MaterialLine".Translate(
                ingredient.thingDef.LabelCap,
                ingredient.count,
                available,
                total);
        }

        /// <summary>切换草稿路径时同步数量输入文本。</summary>
        private void SyncQuantityBuffer(ChipManufacturingDraft draft)
        {
            if (draft != bufferedDraft)
            {
                bufferedDraft = draft;
                quantityBuffer = (draft?.Quantity ?? 1).ToString();
            }
        }
    }
}
