using BDP.Content.FloatingFlight;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BDP.Content.FloatingFlight.Presentation
{
    /// <summary>
    /// 按原版 Pawn_DrawTracker 和 PawnRenderer 飞行分支接入浮空表现。
    /// 只改变 DrawPos 等视觉坐标，不改变 Pawn 的实际 Position。
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_FloatingFlightPresentation
    {
        /// <summary>让原版所有视觉消费者共享同一份 BDP 浮空偏移。</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Pawn_DrawTracker), "get_DrawPos")]
        private static void PawnDrawPosPostfix(
            Pawn ___pawn,
            ref Vector3 __result)
        {
            __result += FloatingFlightPresentationUtility.GetOffset(___pawn);
        }

        /// <summary>
        /// 复用原版飞行影子的绘制语义：影子留在地面，位置随浮空因子变化，
        /// 使用 PawnRenderer 的原版飞行影子材质。
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnRenderer), "DrawShadowInternal")]
        private static bool DrawShadowInternalPrefix(
            PawnRenderer __instance,
            Pawn ___pawn)
        {
            float factor = FloatingFlightPresentationUtility.GetPresentationFactor(___pawn);
            if (factor <= 0.001f || __instance.FlightShadowMaterial == null)
            {
                return true;
            }

            Vector3 drawPos = ___pawn.DrawPos;
            drawPos.y = AltitudeLayer.Filth.AltitudeFor();
            drawPos -= new Vector3(0f, 0f, factor);
            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.identity,
                Vector3.one * 0.75f);
            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix,
                __instance.FlightShadowMaterial,
                0);
            return false;
        }
    }
}
