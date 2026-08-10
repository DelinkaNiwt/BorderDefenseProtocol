using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Bill;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Patches
{
    /// <summary>在原版禁止混料分支中移除当前组合需求为零的芯片材料槽。</summary>
    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsInSet_NoMixHelper")]
    public static class Patch_WorkGiver_DoBill_ZeroChipIngredients
    {
        /// <summary>芯片账单改传过滤后的新列表；普通账单不改参数直接放行。</summary>
        private static void Prefix(ref List<IngredientCount> ingredients, RimWorld.Bill bill)
        {
            if (!(bill is Bill_ChipProduction) || ingredients == null)
            {
                return;
            }

            List<IngredientCount> requiredIngredients = new List<IngredientCount>();
            for (int index = 0; index < ingredients.Count; index++)
            {
                IngredientCount ingredient = ingredients[index];
                if (bill.recipe.Worker.GetIngredientCount(ingredient, bill) > 0)
                {
                    requiredIngredients.Add(ingredient);
                }
            }

            ingredients = requiredIngredients;
        }
    }
}
