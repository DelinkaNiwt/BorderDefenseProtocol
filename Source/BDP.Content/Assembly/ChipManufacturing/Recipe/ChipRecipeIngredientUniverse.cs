using System;
using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Recipe
{
    /// <summary>
    /// 保存芯片通用配方的原始基础材料，并把所有可能材料展开为原版固定材料槽全集。
    /// </summary>
    public static class ChipRecipeIngredientUniverse
    {
        /// <summary>每份芯片配方在材料槽扩展前的基础材料快照。</summary>
        private static readonly Dictionary<RecipeDef, List<ThingDefCountClass>> BaseIngredients =
            new Dictionary<RecipeDef, List<ThingDefCountClass>>();

        /// <summary>初始化全部使用芯片配方工作器的通用配方。</summary>
        public static void InitializeAll()
        {
            List<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int index = 0; index < recipes.Count; index++)
            {
                if (recipes[index].workerClass == typeof(RecipeWorker_ChipProduction))
                {
                    Initialize(recipes[index]);
                }
            }
        }

        /// <summary>保存基础槽并把当前配方扩展为所有可能具体材料的固定槽集合。</summary>
        public static void Initialize(RecipeDef recipe)
        {
            if (recipe == null || BaseIngredients.ContainsKey(recipe))
            {
                return;
            }

            List<ThingDefCountClass> baseCosts = ReadConcreteIngredients(recipe);
            BaseIngredients.Add(recipe, baseCosts);

            List<ThingDef> universe = new List<ThingDef>();
            AppendDefs(universe, baseCosts);
            List<ChipActionPresetDef> actions =
                DefDatabase<ChipActionPresetDef>.AllDefsListForReading;
            for (int index = 0; index < actions.Count; index++)
            {
                AppendDefs(universe, actions[index].costList);
            }

            List<ChipGunShellDef> gunShells =
                DefDatabase<ChipGunShellDef>.AllDefsListForReading;
            for (int index = 0; index < gunShells.Count; index++)
            {
                AppendDefs(universe, gunShells[index].additionalCost);
            }

            recipe.ingredients = new List<IngredientCount>();
            recipe.fixedIngredientFilter = new ThingFilter();
            recipe.defaultIngredientFilter = new ThingFilter();
            for (int index = 0; index < universe.Count; index++)
            {
                ThingDef thingDef = universe[index];
                IngredientCount slot = new IngredientCount();
                slot.filter.SetAllow(thingDef, true);
                slot.SetBaseCount(1f);
                slot.ResolveReferences();
                recipe.ingredients.Add(slot);
                recipe.fixedIngredientFilter.SetAllow(thingDef, true);
                recipe.defaultIngredientFilter.SetAllow(thingDef, true);
            }

            recipe.allowMixingIngredients = false;
        }

        /// <summary>读取配方扩展前保存的基础材料；尚未初始化时直接读取当前固定槽。</summary>
        public static List<ThingDefCountClass> GetBaseIngredients(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return new List<ThingDefCountClass>();
            }

            List<ThingDefCountClass> source;
            if (!BaseIngredients.TryGetValue(recipe, out source))
            {
                source = ReadConcreteIngredients(recipe);
            }

            List<ThingDefCountClass> copy = new List<ThingDefCountClass>();
            for (int index = 0; index < source.Count; index++)
            {
                copy.Add(new ThingDefCountClass(source[index].thingDef, source[index].count));
            }

            return copy;
        }

        /// <summary>把当前配方中的单一具体材料槽转换为物品数量。</summary>
        private static List<ThingDefCountClass> ReadConcreteIngredients(RecipeDef recipe)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            if (recipe.ingredients == null)
            {
                return result;
            }

            for (int index = 0; index < recipe.ingredients.Count; index++)
            {
                IngredientCount ingredient = recipe.ingredients[index];
                if (ingredient == null || !ingredient.IsFixedIngredient)
                {
                    Log.Error("[BDP.ChipManufacturing] 芯片通用配方的每个基础材料槽必须只允许一种具体 ThingDef。");
                    continue;
                }

                int count = Mathf.CeilToInt(ingredient.CountFor(recipe));
                if (count > 0)
                {
                    result.Add(new ThingDefCountClass(ingredient.FixedIngredient, count));
                }
            }

            return result;
        }

        /// <summary>按首次出现顺序收集非零具体材料 Def。</summary>
        private static void AppendDefs(
            List<ThingDef> target,
            IList<ThingDefCountClass> source)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                ThingDefCountClass entry = source[index];
                if (entry?.thingDef != null
                    && entry.count > 0
                    && !target.Contains(entry.thingDef))
                {
                    target.Add(entry.thingDef);
                }
            }
        }
    }
}
