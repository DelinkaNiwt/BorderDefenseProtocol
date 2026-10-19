using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.FloatingFlight.Patches
{
    /// <summary>
    /// 在原版征召属性执行工作清理前，优先收口 Pawn 的实际浮空 Hediff。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
    internal static class Patch_PawnDraftController_FloatingFlight
    {
        /// <summary>原版 setter 进入工作清理前同步实际浮空状态。</summary>
        private static void Prefix(Pawn_DraftController __instance, bool value)
        {
            if (value)
            {
                return;
            }

            FloatingFlightStateController.SynchronizeForDraftChange(
                __instance?.pawn,
                value);
        }
    }
}
