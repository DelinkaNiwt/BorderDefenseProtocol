using BDP.Core.CombatBody.Wounds;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 战斗体 Active 期间压制新鲜缺失部位单项流血表现。
    /// </summary>
    [HarmonyPatch(typeof(Hediff_MissingPart), nameof(Hediff_MissingPart.BleedRate), MethodType.Getter)]
    public static class Patch_Hediff_MissingPart_BleedRate_CombatBodyWounds
    {
        /// <summary>
        /// 在原版计算完成后按战斗体伤口策略压制有效流血率。
        /// </summary>
        public static void Postfix(Hediff_MissingPart __instance, ref float __result)
        {
            if (CombatBodyWoundRawMetrics.IsBypassingBleedSuppression)
            {
                return;
            }

            if (CombatBodyWoundPolicy.ShouldSuppressIndividualBleeding(__instance))
            {
                __result = 0f;
            }
        }
    }
}
