using HarmonyLib;
using Verse;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 防护形态下，把正式注视警戒 Verb 暴露为人物当前有效 Verb。
    /// 等待态索敌读取警戒射程，有目标的合法近战查询保留原版或正式武器结果。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_Pawn_TryGetAttackVerb_LightSoulDefenseWatch
    {
        /// <summary>
        /// 只在防护形态生效；其余人物和姿态完整保留既有返回值。
        /// </summary>
        public static void Postfix(Pawn __instance, Thing target, ref Verb __result)
        {
            Verb_LightSoulDefenseWatch watchVerb =
                LightSoulDefenseWatchUtility.ResolveVerb(__instance);
            if (watchVerb != null
                && (target == null || __result == null || !__result.verbProps.IsMeleeAttack)
                && watchVerb.Available())
            {
                __result = watchVerb;
            }
        }
    }
}
