using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 战斗体伤口运行时适用阶段压制原版伤口覆盖贴图。
    /// 只拦 PawnWoundDrawer，不影响疤痕和灭火泡沫覆盖层。
    /// </summary>
    [HarmonyPatch(typeof(PawnOverlayDrawer), nameof(PawnOverlayDrawer.RenderPawnOverlay))]
    public static class Patch_PawnWoundDrawer_RenderPawnOverlay_CombatBodySuppress
    {
        /// <summary>
        /// 读取 PawnOverlayDrawer 保护字段 pawn。
        /// </summary>
        private static readonly AccessTools.FieldRef<PawnOverlayDrawer, Pawn> pawnAccessor =
            AccessTools.FieldRefAccess<PawnOverlayDrawer, Pawn>("pawn");

        /// <summary>
        /// 伤口运行时适用时只跳过伤口 overlay，其他 overlay 继续走原版。
        /// </summary>
        public static bool Prefix(PawnOverlayDrawer __instance)
        {
            if (!(__instance is PawnWoundDrawer))
            {
                return true;
            }

            Pawn pawn = __instance != null ? pawnAccessor(__instance) : null;
            return !CombatBodyWoundPolicy.IsCombatBodyWoundRuntimeApplicable(pawn);
        }
    }
}
