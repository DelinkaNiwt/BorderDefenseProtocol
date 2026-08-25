using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Recipe
{
    /// <summary>按基础配方、动作和可选武装型简单累加单枚芯片成本。</summary>
    public static class ChipManufacturingCostCalculator
    {
        /// <summary>从持久化组合记录解析当前来源并计算成本；非有效组合返回 null。</summary>
        public static ChipManufacturingCost Calculate(
            RecipeDef recipe,
            ChipCombinationRecord record)
        {
            ChipCombinationResolution resolution =
                new ChipCombinationResolver().Resolve(record);
            return resolution.Status == ChipCombinationResolutionStatus.Valid
                ? Calculate(recipe, resolution.Actions, resolution.ArmamentForm)
                : null;
        }

        /// <summary>从已解析动作和可选武装型计算成本。</summary>
        public static ChipManufacturingCost Calculate(
            RecipeDef recipe,
            IReadOnlyList<ChipActionPresetDef> actions,
            ChipArmamentFormDef armamentForm)
        {
            if (recipe == null)
            {
                return null;
            }

            Dictionary<ThingDef, int> counts = new Dictionary<ThingDef, int>();
            List<ThingDef> order = new List<ThingDef>();
            Add(counts, order, ChipRecipeIngredientUniverse.GetBaseIngredients(recipe));

            float workAmount = recipe.workAmount;
            if (actions != null)
            {
                for (int index = 0; index < actions.Count; index++)
                {
                    ChipActionPresetDef action = actions[index];
                    if (action == null)
                    {
                        continue;
                    }

                    Add(counts, order, action.costList);
                    workAmount += action.additionalWorkAmount;
                }
            }

            if (armamentForm != null)
            {
                Add(counts, order, armamentForm.additionalCost);
                workAmount += armamentForm.additionalWorkAmount;
            }

            ChipManufacturingCost result = new ChipManufacturingCost
            {
                WorkAmount = workAmount
            };
            for (int index = 0; index < order.Count; index++)
            {
                ThingDef thingDef = order[index];
                result.Ingredients.Add(new ThingDefCountClass(thingDef, counts[thingDef]));
            }

            return result;
        }

        /// <summary>把具体材料按 ThingDef 合并，并保持首次出现顺序。</summary>
        private static void Add(
            Dictionary<ThingDef, int> counts,
            List<ThingDef> order,
            IList<ThingDefCountClass> source)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ThingDefCountClass entry = source[index];
                if (entry?.thingDef == null || entry.count <= 0)
                {
                    continue;
                }

                if (!counts.ContainsKey(entry.thingDef))
                {
                    counts.Add(entry.thingDef, 0);
                    order.Add(entry.thingDef);
                }

                counts[entry.thingDef] += entry.count;
            }
        }
    }
}
