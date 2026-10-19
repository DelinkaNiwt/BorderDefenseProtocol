using HarmonyLib;
using BDP.Core.CombatBody.External;
using Verse;

namespace BDP.Content.Patches
{
    /// <summary>
    /// 隐藏战斗体前台衣物标签中的耐久信息。
    /// </summary>
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetCustomLabelNoCount))]
    public static class Patch_Thing_GetCustomLabelNoCount_CombatBodyApparel
    {
        /// <summary>
        /// 禁止原版标签生成耐久后缀。
        /// </summary>
        public static void Prefix(ThingWithComps __instance, ref bool includeHp)
        {
            if (CombatBodyFrontApparelRuntime.IsActive(__instance))
            {
                includeHp = false;
            }
        }
    }
}
