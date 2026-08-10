using System.Collections.Generic;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Recipe
{
    /// <summary>制造单枚当前组合芯片所需的动态成本。</summary>
    public sealed class ChipManufacturingCost
    {
        /// <summary>按首次出现顺序合并后的具体材料。</summary>
        public List<ThingDefCountClass> Ingredients { get; set; } =
            new List<ThingDefCountClass>();

        /// <summary>制造单枚芯片所需的总工作量。</summary>
        public float WorkAmount { get; set; }

        /// <summary>读取某一种具体材料的需求数量。</summary>
        public int CountOf(ThingDef thingDef)
        {
            for (int index = 0; index < Ingredients.Count; index++)
            {
                if (Ingredients[index].thingDef == thingDef)
                {
                    return Ingredients[index].count;
                }
            }

            return 0;
        }
    }
}
