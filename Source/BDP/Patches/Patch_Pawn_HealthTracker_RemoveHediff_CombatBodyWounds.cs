using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 战斗体伤口移除事件接线。
    /// 它负责在原版伤口移除后清理该伤口对应的运行时 drain。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    public static class Patch_Pawn_HealthTracker_RemoveHediff_CombatBodyWounds
    {
        /// <summary>
        /// 原版伤口移除后通知战斗体伤口运行层注销派生状态。
        /// </summary>
        public static void Postfix(Hediff hediff)
        {
            if (hediff == null || !CombatBodyWoundPolicy.IsSupportedWound(hediff))
            {
                return;
            }

            Pawn pawn = hediff.pawn;
            pawn?.GetComp<CompCombatBodyHost>()?.WoundRuntime.NotifyWoundRemoved(pawn, hediff);
        }
    }
}
