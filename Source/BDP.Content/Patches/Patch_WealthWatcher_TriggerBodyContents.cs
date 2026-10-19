using BDP.Core.Trigger;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Patches
{
    /// <summary>
    /// 防止原版财富统计把触发体已计入宿主价值的内部芯片再次累计。
    /// </summary>
    [HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.WealthItemsFilter), new[] { typeof(IThingHolder) })]
    public static class Patch_WealthWatcher_TriggerBodyContents
    {
        /// <summary>
        /// 仅跳过 BDP 触发体组件的子容器；其它 holder 保留原版财富筛选结果。
        /// </summary>
        public static void Postfix(IThingHolder x, ref bool __result)
        {
            if (x is CompTriggerBody)
            {
                __result = false;
            }
        }
    }
}
