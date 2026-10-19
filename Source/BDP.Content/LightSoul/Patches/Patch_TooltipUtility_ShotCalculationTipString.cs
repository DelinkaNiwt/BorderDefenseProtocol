using HarmonyLib;
using Verse;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 只为本业务防护形态人物隐藏已禁用的远程射击提示，不改变其它人物的原版逻辑。
    /// </summary>
    [HarmonyPatch(typeof(TooltipUtility), nameof(TooltipUtility.ShotCalculationTipString))]
    public static class Patch_TooltipUtility_ShotCalculationTipString
    {
        /// <summary>
        /// 防护形态下只去掉射击命中率附加文本，目标自身的普通提示仍由原版继续绘制。
        /// </summary>
        public static bool Prefix(ref string __result)
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (LightSoulDefenseWatchUtility.ResolveVerb(selectedPawn) == null)
            {
                return true;
            }

            __result = string.Empty;
            return false;
        }
    }
}
