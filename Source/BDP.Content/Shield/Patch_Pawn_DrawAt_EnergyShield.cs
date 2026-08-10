using HarmonyLib;
using UnityEngine;
using Verse;

namespace BDP.Content.Shield
{
    /// <summary>
    /// 在 RimWorld 原版 Pawn 绘制完成后叠加正式能量护盾球。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "DrawAt")]
    public static class Patch_Pawn_DrawAt_EnergyShield
    {
        /// <summary>
        /// 查找表达系统同步出的聚合护盾 Hediff，并只绘制一次护盾球。
        /// </summary>
        public static void Postfix(Pawn __instance, Vector3 drawLoc)
        {
            if (__instance?.health?.hediffSet?.hediffs == null)
            {
                return;
            }

            foreach (Hediff hediff in __instance.health.hediffSet.hediffs)
            {
                HediffComp_EnergyShield shield = hediff?.TryGetComp<HediffComp_EnergyShield>();
                if (shield == null)
                {
                    continue;
                }

                shield.DrawShieldBubble(drawLoc);
                return;
            }
        }
    }
}
