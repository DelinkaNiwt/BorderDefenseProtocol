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
        /// 只为显式请求捕获结果；独立嵌套承伤建立空隔离边界，普通外层直接早退。
        /// </summary>
        [HarmonyPriority(Priority.First)]
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
            DamageResolutionRuntime.Abort(__state);
            return __exception;
        }
    }
}
