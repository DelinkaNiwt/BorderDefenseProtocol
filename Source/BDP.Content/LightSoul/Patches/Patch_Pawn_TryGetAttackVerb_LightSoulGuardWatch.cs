using HarmonyLib;
using Verse;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 举盾姿态下，把正式注视警戒 Verb 暴露为人物当前有效 Verb。
    /// 这样原版自动目标查找器会自然读取它的 XML range，而攻击入口仍被“禁止暴力”拦截。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_Pawn_TryGetAttackVerb_LightSoulGuardWatch
    {
        /// <summary>
        /// 只在举盾姿态生效；其余人物和姿态完整保留既有返回值。
        /// </summary>
        public static void Postfix(Pawn __instance, ref Verb __result)
        {
            Verb_LightSoulGuardWatch watchVerb =
                LightSoulGuardWatchUtility.ResolveVerb(__instance);
            if (watchVerb != null
                && __instance.WorkTagIsDisabled(WorkTags.Violent)
                && watchVerb.Available())
            {
                __result = watchVerb;
            }
        }
    }
}
