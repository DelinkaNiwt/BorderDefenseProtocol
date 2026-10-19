using HarmonyLib;
using BDP.Core.CombatBody.External;
using Verse;

namespace BDP.Content.Patches
{
    /// <summary>
    /// 隐藏战斗体前台衣物检查提示中的耐久行。
    /// </summary>
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetTooltip))]
    public static class Patch_ThingWithComps_GetTooltip_CombatBodyApparel
    {
        /// <summary>
        /// 从原版提示文本中移除由 `useHitPoints` 生成的耐久行。
        /// </summary>
        public static void Postfix(ThingWithComps __instance, ref TipSignal __result)
        {
            if (!CombatBodyFrontApparelRuntime.IsActive(__instance)
                || __instance.def == null
                || !__instance.def.useHitPoints
                || string.IsNullOrEmpty(__result.text))
            {
                return;
            }

            string hitPointsLine = __instance.HitPoints + " / " + __instance.MaxHitPoints;
            __result.text = __result.text.Replace("\n" + hitPointsLine, string.Empty);
        }
    }
}
