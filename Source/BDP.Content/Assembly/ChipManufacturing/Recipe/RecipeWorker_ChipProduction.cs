using BDP.Content.Assembly.ChipManufacturing.Bill;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Recipe
{
    /// <summary>让原版账单材料查询读取当前芯片组合的动态单枚需求。</summary>
    public sealed class RecipeWorker_ChipProduction : RecipeWorker
    {
        /// <summary>芯片账单返回当前具体槽需求；普通账单完整回退原版行为。</summary>
        public override float GetIngredientCount(IngredientCount ing, RimWorld.Bill bill)
        {
            Bill_ChipProduction chipBill = bill as Bill_ChipProduction;
            if (chipBill == null || ing == null || !ing.IsFixedIngredient)
            {
                return base.GetIngredientCount(ing, bill);
            }

            ChipManufacturingCost cost = ChipManufacturingCostCalculator.Calculate(
                bill.recipe,
                chipBill.CombinationRecord);
            if (cost == null)
            {
                return 0f;
            }

            ThingDef thingDef = ing.FixedIngredient;
            int itemCount = cost.CountOf(thingDef);
            return itemCount * bill.recipe.IngredientValueGetter.ValuePerUnitOf(thingDef);
        }
    }
}
