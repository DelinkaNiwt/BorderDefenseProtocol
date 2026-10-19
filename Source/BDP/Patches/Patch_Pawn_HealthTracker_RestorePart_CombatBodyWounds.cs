using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>原版部位恢复直接清理健康列表，需同步注销已有部位流失及表现。</summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RestorePart))]
    public static class Patch_Pawn_HealthTracker_RestorePart_CombatBodyWounds
    {
        /// <summary>访问原版健康追踪器宿主，不改写其状态。</summary>
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> pawnAccessor =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        /// <summary>仅在 BDP 伤口适用阶段核对现有来源；不会把旧伤重新注册。</summary>
        public static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = pawnAccessor(__instance);
            if (CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(pawn))
            {
                CombatBodyMaterialAccess.Wounds(pawn)?.ReconcileActiveWounds(pawn);
            }
        }
    }
}
