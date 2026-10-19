using HarmonyLib;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>仅提前清理本地图的 BDP 波次，原版地图卸载照常执行。</summary>
    [HarmonyPatch(typeof(MapDeiniter), nameof(MapDeiniter.Deinit))]
    internal static class Patch_MapDeiniter_GateArrival
    {
        /// <summary>必须早于原版递归对象快照；部件 MapRemoved 或 Thing 通知都已太晚。</summary>
        [HarmonyPrefix]
        private static void Prefix(Map map)
        {
            foreach (Thing_GateArrival wave in GateArrivalHooks.ActiveWaves(map)) wave.Cancel();
        }
    }
}
