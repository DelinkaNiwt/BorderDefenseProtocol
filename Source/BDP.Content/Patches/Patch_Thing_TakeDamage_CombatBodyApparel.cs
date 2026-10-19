using HarmonyLib;
using BDP.Core.CombatBody.External;
using RimWorld;
using Verse;

namespace BDP.Content.Patches
{
    /// <summary>
    /// 屏蔽战斗体前台衣物的原版耐久伤害。
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class Patch_Thing_TakeDamage_CombatBodyApparel
    {
        /// <summary>
        /// 已登记的战斗体前台衣物不进入原版耐久伤害链。
        /// </summary>
        public static bool Prefix(
            Thing __instance,
            ref DamageWorker.DamageResult __result)
        {
            if (!CombatBodyFrontApparelRuntime.IsActive(__instance))
            {
                return true;
            }

            __result = default(DamageWorker.DamageResult);
            return false;
        }
    }
}
