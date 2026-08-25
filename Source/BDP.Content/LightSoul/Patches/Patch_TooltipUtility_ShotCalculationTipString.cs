using HarmonyLib;
using Verse;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 阻止原版为已被禁止暴力的人物计算射击提示。
    /// 原版提示入口只查看所持武器，未检查射击精度属性是否已随暴力能力一起禁用。
    /// </summary>
    [HarmonyPatch(typeof(TooltipUtility), nameof(TooltipUtility.ShotCalculationTipString))]
    public static class Patch_TooltipUtility_ShotCalculationTipString
    {
        /// <summary>
        /// 暴力已禁用时只去掉射击命中率附加文本，目标自身的普通提示仍由原版继续绘制。
        /// </summary>
        public static bool Prefix(ref string __result)
        {
            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null || !selectedPawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return true;
            }

            __result = string.Empty;
            return false;
        }
    }
}
