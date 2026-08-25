using System;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 只记录原版 Thing.TakeDamage（目标承伤）结果，不改写原版伤害计算。
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class Patch_Thing_TakeDamage_BdpResolution
    {
        /// <summary>
        /// 在原版承伤入口开始时建立短生命周期捕获记录。
        /// </summary>
        public static void Prefix(
            Thing __instance,
            DamageInfo dinfo,
            out DamageResolutionRuntime.Capture __state)
        {
            __state = DamageResolutionRuntime.Begin(__instance, dinfo);
        }

        /// <summary>
        /// 原版正常返回后发布承伤结果。
        /// </summary>
        public static void Postfix(
            DamageWorker.DamageResult __result,
            DamageResolutionRuntime.Capture __state)
        {
            DamageResolutionRuntime.Complete(__state, __result, true);
        }

        /// <summary>
        /// 原版异常退出时清理捕获状态。
        /// </summary>
        public static Exception Finalizer(
            Exception __exception,
            DamageResolutionRuntime.Capture __state)
        {
            if (__exception != null)
            {
                DamageResolutionRuntime.Abort(__state);
            }

            return __exception;
        }
    }
}
