using HarmonyLib;
using Verse.AI;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 在原版自动攻击检查完成后，以同一时点刷新光魂举盾 Verb 的自动警戒目标。
    /// </summary>
    [HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
    public static class Patch_JobDriver_Wait_CheckForAutoAttack
    {
        /// <summary>
        /// 原版暴力攻击因举盾禁用而自然退出；后置阶段只让正式注视警戒 Verb 选目标。
        /// </summary>
        public static void Postfix(JobDriver_Wait __instance)
        {
            if (__instance?.pawn?.jobs?.curDriver != __instance)
            {
                return;
            }

            LightSoulGuardWatchUtility.ResolveVerb(__instance.pawn)
                ?.RefreshAutomaticWatchTarget(__instance);
        }
    }
}
