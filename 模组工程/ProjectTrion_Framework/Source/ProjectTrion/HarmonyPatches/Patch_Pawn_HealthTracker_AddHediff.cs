using HarmonyLib;
using ProjectTrion.Components;
using ProjectTrion.Utilities;
using Verse;

namespace ProjectTrion.HarmonyPatches
{
    /// <summary>
    /// Harmony补丁：检测部位添加事件，触发关键部位损毁处理。
    ///
    /// Harmony patch: Detect hediff addition, trigger vital part destruction handling.
    /// 当单位处于战斗体状态时，检测是否失去关键部位（心脏等），触发Bail Out。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff))]
    [HarmonyPriority(Priority.High)]
    public static class Patch_Pawn_HealthTracker_AddHediff
    {
        /// <summary>
        /// 后置补丁：在Hediff添加后执行。
        /// Postfix patch: Execute after hediff is added.
        /// </summary>
        public static void Postfix(Pawn ___pawn, Hediff hediff)
        {
            if (___pawn == null || hediff == null)
                return;

            // 获取Trion组件
            var compTrion = ___pawn.GetComp<CompTrion>();
            if (compTrion == null || !compTrion.IsInCombat)
                return;

            // 检测是否是部位缺失（MissingPart）
            var missingPart = hediff as Hediff_MissingPart;
            if (missingPart != null)
            {
                var part = missingPart.Part;

                // 检查是否是关键部位（Trion供给器官）
                if (VitalPartUtil.IsVitalPart(part))
                {
                    #if DEBUG
                    Log.Message($"ProjectTrion: {___pawn.Name}的关键部位被摧毁：{part.Label}");
                    #endif

                    // 触发关键部位损毁处理
                    compTrion.NotifyVitalPartDestroyed(part);
                }
                else
                {
                    // 非关键部位损毁，只更新泄漏缓存
                    #if DEBUG
                    Log.Message($"ProjectTrion: {___pawn.Name}的部位被摧毁：{part.Label}，增加泄漏速率");
                    #endif

                    compTrion.InvalidateLeakCache();
                }
            }
            else
            {
                // 其他Hediff添加（如伤口、疾病），更新泄漏缓存
                var injury = hediff as Hediff_Injury;
                if (injury != null)
                {
                    compTrion.InvalidateLeakCache();
                }
            }
        }
    }
}
