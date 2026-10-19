using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>只为显式登记的 BDP 结果补日志引用，不改变原版关联行为。</summary>
    [HarmonyPatch(typeof(DamageWorker.DamageResult), nameof(DamageWorker.DamageResult.AssociateWithLog))]
    internal static class Patch_DamageResult_AssociateWithLog_BdpEntry
    {
        /// <summary>原版关联完成后按结果对象严格匹配，避免串用另一目标。</summary>
        private static void Postfix(DamageWorker.DamageResult __instance, LogEntry_DamageResult log)
        {
            if (log != null)
            {
                DamageResolutionRuntime.AssociateLog(__instance, log);
            }
        }
    }
}
