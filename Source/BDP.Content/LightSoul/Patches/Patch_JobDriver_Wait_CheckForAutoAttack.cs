using HarmonyLib;
using Verse.AI;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 在原版自动攻击检查完成后，以同一时点刷新光魂防护 Verb 的自动警戒目标。
    /// </summary>
    [HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
    public static class Patch_JobDriver_Wait_CheckForAutoAttack
    {
        /// <summary>
        /// 原版仍优先处理邻近近战；后置警戒只在不忙碌的等待上下文中选目标。
        /// </summary>
        public static void Postfix(JobDriver_Wait __instance)
        {
            if (__instance?.pawn?.jobs?.curDriver != __instance)
            {
                return;
            }

            LightSoulDefenseWatchUtility.ResolveVerb(__instance.pawn)
                ?.RefreshAutomaticWatchTarget(__instance);
        }
    }
}
