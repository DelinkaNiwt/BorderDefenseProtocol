using HarmonyLib;
using ProjectTrion.Components;
using Verse;

namespace ProjectTrion.HarmonyPatches
{
    /// <summary>
    /// Harmony补丁：检测部位移除事件，更新泄漏缓存。
    ///
    /// Harmony patch: Detect hediff removal, update leak cache.
    /// 当伤口被治愈等情况时，更新泄漏速率。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    [HarmonyPriority(Priority.High)]
    public static class Patch_Pawn_HealthTracker_RemoveHediff
    {
        /// <summary>
        /// 后置补丁：在Hediff移除后执行。
        /// Postfix patch: Execute after hediff is removed.
        /// </summary>
        public static void Postfix(Pawn ___pawn, Hediff hediff)
        {
            if (___pawn == null || hediff == null)
                return;

            // 获取Trion组件
            var compTrion = ___pawn.GetComp<CompTrion>();
            if (compTrion == null || !compTrion.IsInCombat)
                return;

            // 只有伤口类的Hediff移除才会影响泄漏
            var injury = hediff as Hediff_Injury;
            if (injury != null)
            {
                #if DEBUG
                Log.Message($"ProjectTrion: {___pawn.Name}的伤口被治疗或移除，更新泄漏缓存");
                #endif

                // 更新泄漏缓存
                compTrion.InvalidateLeakCache();
            }
        }
    }
}
