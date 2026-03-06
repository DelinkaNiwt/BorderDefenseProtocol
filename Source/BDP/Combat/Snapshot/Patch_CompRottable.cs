using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Combat.Snapshot
{
    /// <summary>
    /// 阻止战斗体快照容器中的物品腐烂。
    /// 通过让 Active 属性返回 false 来阻止腐烂计算。
    /// </summary>
    [HarmonyPatch(typeof(CompRottable), nameof(CompRottable.Active), MethodType.Getter)]
    public static class Patch_CompRottable_PreventRotInSnapshot
    {
        [HarmonyPostfix]
        public static void Postfix(CompRottable __instance, ref bool __result)
        {
            // 如果物品在战斗体快照容器中，强制 Active = false
            if (__result && __instance.parent?.holdingOwner?.Owner is CombatBodySnapshot)
            {
                __result = false;
            }
        }
    }
}
