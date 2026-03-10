using HarmonyLib;
using UnityEngine;
using Verse;

namespace BDP.Trigger.Shield
{
    /// <summary>
    /// Postfix patch on Pawn.DrawAt
    /// 用途：为带有护盾hediff的pawn绘制护盾球体
    /// 参考：VEF的ShieldsSystem实现
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "DrawAt")]
    public static class Patch_Pawn_DrawAt_Shield
    {
        /// <summary>
        /// Postfix: 在pawn绘制后绘制护盾球体
        /// </summary>
        public static void Postfix(Pawn __instance, Vector3 drawLoc)
        {
            // 检查pawn健康系统
            if (__instance?.health?.hediffSet == null) return;

            // 遍历所有hediff，查找护盾
            foreach (var hediff in __instance.health.hediffSet.hediffs)
            {
                // 检查是否是护盾hediff
                if (hediff is Hediff_BDPShield shieldHediff)
                {
                    // 获取护盾组件并绘制
                    var shieldComp = shieldHediff.TryGetComp<HediffComp_BDPShield>();
                    shieldComp?.DrawShieldBubble(drawLoc);
                }
            }
        }
    }
}
