using HarmonyLib;
using Verse;
using BDP.Core;
using UnityEngine;

namespace BDP.Combat.UI
{
    /// <summary>
    /// 覆盖健康面板部位颜色显示，使用影子HP计算颜色。
    /// </summary>
    [HarmonyPatch(typeof(HealthUtility), "GetPartConditionLabel")]
    public static class Patch_HealthUtility_GetPartConditionLabel
    {
        // 用于防止重复日志的集合
        private static readonly System.Collections.Generic.HashSet<string> _loggedDestroyedParts =
            new System.Collections.Generic.HashSet<string>();

        static void Postfix(Pawn pawn, BodyPartRecord part, ref Pair<string, Color> __result)
        {
            // 基础检查
            if (pawn == null || part == null) return;

            // 检查是否有战斗体运行时且已激活
            var runtime = CombatBodyRuntime.Of(pawn);
            if (runtime == null || !runtime.IsActive) return;

            // 获取影子HP追踪器
            var shadowHP = runtime.ShadowHP;
            if (shadowHP == null)
            {
                Log.Warning($"[BDP-UI] ShadowHP为null: {pawn.LabelShort}");
                return;
            }

            // 获取影子HP百分比
            float healthPercentage = shadowHP.GetHealthPercentage(part);

            // 根据影子HP计算颜色
            Color color = GetColorFromHealthPercentage(healthPercentage);

            // 【关键日志】只在部位被破坏或严重受损时输出
            if (healthPercentage <= 0f || healthPercentage < 0.4f)
            {
                string key = $"{pawn.ThingID}_{part.def.defName}";
                if (!_loggedDestroyedParts.Contains(key))
                {
                    _loggedDestroyedParts.Add(key);
                    // 输出调用堆栈，查看是谁在调用这个方法
                    var stackTrace = new System.Diagnostics.StackTrace(1, true);
                    Log.Warning($"[BDP-UI] {pawn.LabelShort}.{part.LabelShort}: HP={healthPercentage:P1} 原色={__result.Second} 新色={color} (R={color.r:F2} G={color.g:F2} B={color.b:F2})");
                    Log.Warning($"[BDP-UI] 调用堆栈:\n{stackTrace}");
                }
            }

            // 覆盖返回值的颜色部分（保留原版的文本标签）
            __result = new Pair<string, Color>(__result.First, color);
        }

        /// <summary>
        /// 根据健康百分比计算颜色（完全照搬原版逻辑）。
        /// </summary>
        private static Color GetColorFromHealthPercentage(float percentage)
        {
            if (percentage <= 0f)
            {
                // 被破坏部位：灰色
                return Color.gray;
            }
            else if (percentage < 0.4f)
            {
                // 严重受损：红色
                return HealthUtility.RedColor;
            }
            else if (percentage < 0.7f)
            {
                // 受损：橙色
                return HealthUtility.ImpairedColor;
            }
            else if (percentage < 0.999f)
            {
                // 轻微受损：黄色
                return HealthUtility.SlightlyImpairedColor;
            }
            else
            {
                // 良好：绿色
                return HealthUtility.GoodConditionColor;
            }
        }
    }
}
