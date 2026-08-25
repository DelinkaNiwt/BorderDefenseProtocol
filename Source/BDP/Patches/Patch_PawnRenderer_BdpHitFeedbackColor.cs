using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 将 BDP 远程模块提交的命中反馈颜色接入原版 PawnRenderer（角色绘制器）的整体染色入口。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class Patch_PawnRenderer_GetDrawParms_BdpHitFeedbackColor
    {
        /// <summary>
        /// 在原版绘制参数生成后，把 Pawn tint（整体染色）从红色替换为模块颜色。
        /// </summary>
        public static void Postfix(
            PawnRenderer __instance,
            PawnRenderFlags flags,
            ref PawnDrawParms __result)
        {
            if (flags.FlagSet(PawnRenderFlags.Cache) || flags.FlagSet(PawnRenderFlags.Portrait))
            {
                return;
            }

            Color color;
            if (!BdpPawnRendererHitFeedbackColorUtility.TryResolveColor(__instance, out color))
            {
                return;
            }

            float flashFactor = BdpPawnRendererHitFeedbackColorUtility.ResolveFlashFactor(__instance);
            Color tint = Color.Lerp(Color.white, color, flashFactor);
            tint.a = __result.tint.a;
            __result.tint = tint;
        }
    }

    /// <summary>
    /// 将 BDP 远程模块提交的命中反馈颜色接入原版受击材质入口。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderer), "OverrideMaterialIfNeeded")]
    public static class Patch_PawnRenderer_OverrideMaterialIfNeeded_BdpHitFeedbackColor
    {
        /// <summary>
        /// 原版材质返回后，把受击材质的红色终点替换为模块颜色。
        /// </summary>
        public static void Postfix(
            PawnRenderer __instance,
            Material original,
            PawnRenderFlags flags,
            ref Material __result)
        {
            if (original == null
                || __result == null
                || flags.FlagSet(PawnRenderFlags.Cache)
                || flags.FlagSet(PawnRenderFlags.Portrait))
            {
                return;
            }

            Color color;
            if (!BdpPawnRendererHitFeedbackColorUtility.TryResolveColor(__instance, out color))
            {
                return;
            }

            __result.color = Color.Lerp(
                original.color,
                color,
                BdpPawnRendererHitFeedbackColorUtility.ResolveFlashFactor(__instance));
        }
    }

    /// <summary>
    /// 复用 PawnRenderer 颜色覆盖补丁所需的原版字段和闪烁进度解析。
    /// </summary>
    internal static class BdpPawnRendererHitFeedbackColorUtility
    {
        /// <summary>
        /// 读取原版 PawnRenderer 私有 Pawn 字段的访问器。
        /// </summary>
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnAccessor =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        /// <summary>
        /// 读取当前 Pawn 是否有有效颜色覆盖。
        /// </summary>
        internal static bool TryResolveColor(PawnRenderer renderer, out Color color)
        {
            return HitFeedbackColorRuntime.TryGetColor(
                renderer != null ? PawnAccessor(renderer) : null,
                out color);
        }

        /// <summary>
        /// 按原版红色闪烁 tint 的绿色通道反推当前闪烁进度。
        /// </summary>
        internal static float ResolveFlashFactor(PawnRenderer renderer)
        {
            if (renderer == null || renderer.flasher == null || !renderer.flasher.FlashingNowOrRecently)
            {
                return 0f;
            }

            return Mathf.Clamp01(1f - renderer.flasher.CurColor.g);
        }
    }
}
