using HarmonyLib;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 只在装配台肖像捕获期间屏蔽原版伤口覆盖层。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderNodeWorker_OverlayWounds), nameof(PawnRenderNodeWorker_OverlayWounds.CanDrawNow))]
    internal static class Patch_PawnRenderNodeWorker_OverlayWounds_AssemblyPortrait
    {
        /// <summary>
        /// 阻止伤口节点进入本次肖像的绘制请求，不影响 Pawn 健康数据和其他绘制。
        /// </summary>
        private static bool Prefix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (!AssemblyPawnPortraitCapture.IsCapturingWithoutWounds(parms.pawn))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
