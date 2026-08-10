using BDP.Core.CombatBody;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 战斗体健康全量变化事件接线。
    /// 非空 Hediff 变化可能来自原版自然愈合或治疗，不能当成伤口新增或加重。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.Notify_HediffChanged))]
    public static class Patch_Pawn_HealthTracker_NotifyHediffChanged_CombatBodyWounds
    {
        /// <summary>
        /// Pawn_HealthTracker 私有 Pawn 字段访问器。
        /// </summary>
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> pawnAccessor =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        /// <summary>
        /// 原版全量健康变化后通知战斗体伤口运行层。
        /// </summary>
        public static void Postfix(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff != null)
            {
                return;
            }

            Pawn pawn = ResolvePawn(__instance);
            if (pawn == null)
            {
                return;
            }

            CompCombatBodyHost host = pawn.GetComp<CompCombatBodyHost>();
            if (host == null)
            {
                return;
            }

            host.WoundRuntime.RebuildActiveWounds(pawn);
        }

        /// <summary>
        /// 从健康追踪器解析宿主 Pawn。
        /// </summary>
        private static Pawn ResolvePawn(Pawn_HealthTracker healthTracker)
        {
            return healthTracker != null ? pawnAccessor(healthTracker) : null;
        }
    }
}
